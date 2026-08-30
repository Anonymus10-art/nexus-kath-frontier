namespace NexusKathFrontier.Launcher.Models;

public sealed class PackManifest
{
    public string PackVersion { get; init; } = "0.0.0";
    public string MinecraftVersion { get; init; } = "1.21.1";
    public string NeoForgeVersion { get; init; } = "21.1.248";
    public List<PackFile> Files { get; init; } = [];
}

public sealed class PackFile
{
    public string Path { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Sha256 { get; init; } = string.Empty;
    public long Size { get; init; }
}
