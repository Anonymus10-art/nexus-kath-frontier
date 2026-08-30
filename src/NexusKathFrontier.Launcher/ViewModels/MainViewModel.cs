using CommunityToolkit.Mvvm.ComponentModel;

namespace NexusKathFrontier.Launcher.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private string statusText = "Preparando launcher";
    private string statusDetail = "Comprobando instalación";
    private string accountName = "INICIAR SESIÓN";
    private string installedVersion = "Sin instalar";
    private double progressValue;
    private bool isBusy;
    private int maximumRamMb = 6144;

    public string LauncherName { get; init; } = "NEXUS KATH FRONTIER";
    public string MinecraftVersion { get; init; } = "1.21.1";
    public string NeoForgeVersion { get; init; } = "21.1.248";
    public string ServerText { get; init; } = "127.0.0.1:25565";
    public string LauncherVersion { get; init; } = "0.1.0";

    public string StatusText
    {
        get => statusText;
        set => SetProperty(ref statusText, value);
    }

    public string StatusDetail
    {
        get => statusDetail;
        set => SetProperty(ref statusDetail, value);
    }

    public string AccountName
    {
        get => accountName;
        set => SetProperty(ref accountName, value);
    }

    public string InstalledVersion
    {
        get => installedVersion;
        set => SetProperty(ref installedVersion, value);
    }

    public double ProgressValue
    {
        get => progressValue;
        set => SetProperty(ref progressValue, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        set
        {
            if (SetProperty(ref isBusy, value))
                OnPropertyChanged(nameof(PlayButtonText));
        }
    }

    public string PlayButtonText => IsBusy ? "PREPARANDO..." : "JUGAR AHORA";

    public int MaximumRamMb
    {
        get => maximumRamMb;
        set
        {
            if (SetProperty(ref maximumRamMb, value))
                OnPropertyChanged(nameof(RamText));
        }
    }

    public string RamText => $"{MaximumRamMb / 1024d:0.#} GB";
}
