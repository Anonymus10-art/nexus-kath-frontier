namespace NexusKathFrontier.Launcher.Services;

public static class AppPaths
{
    public static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NexusKathFrontier");

    public static readonly string Game = Path.Combine(Root, "game");
    public static readonly string Runtime = Path.Combine(Root, "runtime");
    public static readonly string JavaRuntime = Path.Combine(Runtime, "java");
    public static readonly string Cache = Path.Combine(Root, "cache");
    public static readonly string Logs = Path.Combine(Root, "logs");
    public static readonly string SettingsFile = Path.Combine(Root, "settings.json");
    public static readonly string LocalManifest = Path.Combine(Root, "installed-manifest.json");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Game);
        Directory.CreateDirectory(Runtime);
        Directory.CreateDirectory(Cache);
        Directory.CreateDirectory(Logs);
    }
}
