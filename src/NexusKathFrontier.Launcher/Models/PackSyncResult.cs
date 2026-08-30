namespace NexusKathFrontier.Launcher.Models;

public sealed record PackSyncResult(string Version, int DownloadedFiles, int ReusedFiles, bool Changed);
