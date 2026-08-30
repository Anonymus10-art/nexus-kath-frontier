namespace NexusKathFrontier.Launcher.Models;

public sealed class UserSettings
{
    public int MaximumRamMb { get; set; } = 6144;
    public string? JavaPath { get; set; }
    public string? LastPlayerName { get; set; }
    public bool CloseLauncherWhenPlaying { get; set; }
}
