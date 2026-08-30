using System.Text.Json;

namespace NexusKathFrontier.Launcher.Models;

public sealed class LauncherConfig
{
    public string LauncherName { get; init; } = "NEXUS KATH FRONTIER";
    public string LauncherVersion { get; init; } = "0.1.0";
    public string MinecraftVersion { get; init; } = "1.21.1";
    public string NeoForgeVersion { get; init; } = "21.1.248";
    public string LaunchVersion { get; init; } = "neoforge-21.1.248";
    public string ServerAddress { get; init; } = "127.0.0.1";
    public int ServerPort { get; init; } = 25565;
    public string ManifestUrl { get; init; } = string.Empty;
    public string NeoForgeInstallerUrl { get; init; } = string.Empty;
    public string MicrosoftClientId { get; init; } = string.Empty;
    public int MinimumJavaMajor { get; init; } = 21;
    public int DefaultRamMb { get; init; } = 6144;

    public static LauncherConfig Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            return new LauncherConfig();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<LauncherConfig>(json, JsonOptions.Default) ?? new LauncherConfig();
    }
}
