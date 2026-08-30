using System.Diagnostics;
using NexusKathFrontier.Launcher.Models;

namespace NexusKathFrontier.Launcher.Services;

public sealed class NeoForgeService(HttpClient httpClient, HttpDownloadService downloader)
{
    public async Task EnsureInstalledAsync(
        LauncherConfig config,
        string javawPath,
        IProgress<ProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        var versionJson = Path.Combine(
            AppPaths.Game,
            "versions",
            config.LaunchVersion,
            config.LaunchVersion + ".json");
        if (File.Exists(versionJson))
        {
            progress?.Report(new ProgressInfo("NeoForge detectado", 100, config.NeoForgeVersion));
            return;
        }

        progress?.Report(new ProgressInfo("Preparando NeoForge", 5, config.NeoForgeVersion));
        var installer = Path.Combine(AppPaths.Cache, $"neoforge-{config.NeoForgeVersion}-installer.jar");
        await downloader.DownloadAsync(
            config.NeoForgeInstallerUrl,
            installer,
            "Descargando NeoForge",
            progress,
            cancellationToken);

        await VerifyMavenChecksumAsync(config.NeoForgeInstallerUrl, installer, cancellationToken);
        progress?.Report(new ProgressInfo("Instalando NeoForge", 90, "Esto puede tardar unos minutos"));

        var profiles = Path.Combine(AppPaths.Game, "launcher_profiles.json");
        if (!File.Exists(profiles))
            await File.WriteAllTextAsync(profiles, "{\"profiles\":{}}", cancellationToken);

        var javaExe = Path.Combine(Path.GetDirectoryName(javawPath)!, "java.exe");
        if (!File.Exists(javaExe)) javaExe = javawPath;
        var startInfo = new ProcessStartInfo
        {
            FileName = javaExe,
            Arguments = $"-jar \"{installer}\" --installClient \"{AppPaths.Game}\"",
            WorkingDirectory = AppPaths.Game,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException("No se pudo iniciar el instalador de NeoForge.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0 || !File.Exists(versionJson))
            throw new InvalidOperationException(
                $"NeoForge no pudo instalarse (código {process.ExitCode}).\n{error}\n{output}");

        progress?.Report(new ProgressInfo("NeoForge instalado", 100, config.NeoForgeVersion));
    }

    private async Task VerifyMavenChecksumAsync(
        string installerUrl,
        string installerPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var expected = (await httpClient.GetStringAsync(installerUrl + ".sha256", cancellationToken))
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            var actual = await HashService.Sha256Async(installerPath, cancellationToken);
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("El instalador de NeoForge no superó la verificación SHA-256.");
        }
        catch (HttpRequestException)
        {
            // Algunos mirrors Maven no publican el sidecar. HTTPS sigue siendo obligatorio.
        }
    }
}
