using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using NexusKathFrontier.Launcher.Models;
using NexusKathFrontier.Launcher.Services;
using NexusKathFrontier.Launcher.ViewModels;

namespace NexusKathFrontier.Launcher;

public partial class MainWindow : Window
{
    private readonly LauncherConfig config;
    private readonly MainViewModel viewModel;
    private readonly SettingsService settingsService = new();
    private readonly UserSettings settings;
    private readonly HttpClient httpClient = new();
    private readonly HttpDownloadService downloader;
    private readonly JavaService javaService;
    private readonly PackUpdateService packUpdateService;
    private readonly NeoForgeService neoForgeService;
    private readonly MinecraftService minecraftService = new();
    private readonly MicrosoftAuthService authService;
    private readonly CancellationTokenSource lifetime = new();

    public MainWindow()
    {
        config = LauncherConfig.Load();
        settings = settingsService.Load(config.DefaultRamMb);
        viewModel = new MainViewModel
        {
            LauncherName = config.LauncherName,
            LauncherVersion = config.LauncherVersion,
            MinecraftVersion = config.MinecraftVersion,
            NeoForgeVersion = config.NeoForgeVersion,
            ServerText = $"{config.ServerAddress}:{config.ServerPort}",
            MaximumRamMb = settings.MaximumRamMb
        };

        httpClient.Timeout = TimeSpan.FromMinutes(20);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"NexusKathFrontier/{config.LauncherVersion}");
        downloader = new HttpDownloadService(httpClient);
        javaService = new JavaService(httpClient, downloader);
        packUpdateService = new PackUpdateService(httpClient, downloader);
        neoForgeService = new NeoForgeService(httpClient, downloader);
        authService = new MicrosoftAuthService(config.MicrosoftClientId);

        DataContext = viewModel;
        InitializeComponent();
        Closed += (_, _) =>
        {
            lifetime.Cancel();
            httpClient.Dispose();
            lifetime.Dispose();
        };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AppPaths.EnsureDirectories();
        var installed = packUpdateService.GetInstalledManifest();
        viewModel.InstalledVersion = installed?.PackVersion ?? "Sin instalar";

        try
        {
            if (!PackUpdateService.IsConfigured(config.ManifestUrl))
            {
                SetStatus("Conecta el repositorio GitHub", "Configura appsettings.json", 0);
                return;
            }

            SetStatus("Buscando actualizaciones", "GitHub Releases", 20);
            var remote = await packUpdateService.GetRemoteManifestAsync(config.ManifestUrl, lifetime.Token);
            if (remote is not null && remote.PackVersion != installed?.PackVersion)
                SetStatus("Actualización disponible", $"Versión {remote.PackVersion}", 0);
            else
                SetStatus("Todo listo para jugar", $"Launcher {config.LauncherVersion}", 100);
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            SetStatus("No se pudo comprobar GitHub", "Puedes reintentarlo al jugar", 0);
        }
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (viewModel.IsBusy) return;
        if (!MicrosoftAuthService.IsConfigured(config.MicrosoftClientId))
        {
            MessageBox.Show(
                "Antes de compilar debes colocar tu Microsoft Client ID en appsettings.json. " +
                "El archivo GUIA-PASO-A-PASO.md explica cómo hacerlo.",
                "Falta conectar Microsoft",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        await RunBusyAsync(async cancellationToken =>
        {
            var progress = CreateProgress();
            var javaPath = await javaService.EnsureJavaAsync(
                config.MinimumJavaMajor, settings.JavaPath, progress, cancellationToken);
            settings.JavaPath = javaPath;
            settingsService.Save(settings);

            var manifest = await packUpdateService.GetRemoteManifestAsync(config.ManifestUrl, cancellationToken);
            if (manifest is not null)
            {
                var result = await packUpdateService.SynchronizeAsync(manifest, false, progress, cancellationToken);
                viewModel.InstalledVersion = result.Version;
            }

            SetStatus("Iniciando sesión", "Cuenta Microsoft", 20);
            var session = await authService.AuthenticateAsync();
            viewModel.AccountName = session.Username.ToUpperInvariant();
            settings.LastPlayerName = session.Username;
            settingsService.Save(settings);

            await minecraftService.InstallVersionAsync(config.MinecraftVersion, progress, cancellationToken);
            await neoForgeService.EnsureInstalledAsync(config, javaPath, progress, cancellationToken);
            await minecraftService.InstallVersionAsync(config.LaunchVersion, progress, cancellationToken);
            ServerListService.EnsureDefaultServer(config);

            SetStatus("Abriendo Minecraft", session.Username, 100);
            if (settings.CloseLauncherWhenPlaying) Hide();
            var exitCode = await minecraftService.LaunchAsync(
                config.LaunchVersion,
                session,
                javaPath,
                settings.MaximumRamMb,
                Log,
                cancellationToken);
            if (!IsVisible) Show();
            SetStatus(
                exitCode == 0 ? "Minecraft se cerró correctamente" : "Minecraft terminó con un error",
                $"Código {exitCode}",
                exitCode == 0 ? 100 : 0);
        });
    }

    private async void Repair_Click(object sender, RoutedEventArgs e)
    {
        if (viewModel.IsBusy) return;
        if (!PackUpdateService.IsConfigured(config.ManifestUrl))
        {
            MessageBox.Show("Primero configura la URL de GitHub en appsettings.json.", "NEXUS KATH FRONTIER");
            return;
        }

        await RunBusyAsync(async cancellationToken =>
        {
            var manifest = await packUpdateService.GetRemoteManifestAsync(config.ManifestUrl, cancellationToken)
                           ?? throw new InvalidOperationException("No se encontró el manifiesto del modpack.");
            var result = await packUpdateService.SynchronizeAsync(
                manifest, false, CreateProgress(), cancellationToken);
            viewModel.InstalledVersion = result.Version;
            SetStatus("Instalación reparada", $"{result.DownloadedFiles} archivos restaurados", 100);
        });
    }

    private async Task RunBusyAsync(Func<CancellationToken, Task> operation)
    {
        viewModel.IsBusy = true;
        PlayButton.IsEnabled = false;
        try
        {
            await operation(lifetime.Token);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Operación cancelada", "", 0);
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            SetStatus("No se pudo completar", ex.Message, 0);
            MessageBox.Show(ex.Message, "NEXUS KATH FRONTIER", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            viewModel.IsBusy = false;
            PlayButton.IsEnabled = true;
        }
    }

    private IProgress<ProgressInfo> CreateProgress() => new Progress<ProgressInfo>(item =>
        SetStatus(item.Message, item.Detail, item.Percentage));

    private void SetStatus(string message, string detail, double percentage)
    {
        viewModel.StatusText = message;
        viewModel.StatusDetail = detail;
        viewModel.ProgressValue = Math.Clamp(percentage, 0, 100);
    }

    private static void Log(string message)
    {
        try
        {
            AppPaths.EnsureDirectories();
            File.AppendAllText(
                Path.Combine(AppPaths.Logs, $"launcher-{DateTime.UtcNow:yyyy-MM-dd}.log"),
                $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}");
        }
        catch
        {
            // El log nunca debe impedir que el juego se abra.
        }
    }

    private void Home_Click(object sender, RoutedEventArgs e)
    {
        HomePanel.Visibility = Visibility.Visible;
        SettingsPanel.Visibility = Visibility.Collapsed;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        HomePanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Visible;
    }

    private void News_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("El panel de noticias se conectará al repositorio en la siguiente fase.", "Noticias");

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        AppPaths.EnsureDirectories();
        Process.Start(new ProcessStartInfo { FileName = AppPaths.Game, UseShellExecute = true });
    }

    private void RamSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (viewModel is null || settings is null) return;
        viewModel.MaximumRamMb = (int)e.NewValue;
        settings.MaximumRamMb = viewModel.MaximumRamMb;
        settingsService.Save(settings);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && e.GetPosition(this).Y <= 62)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
