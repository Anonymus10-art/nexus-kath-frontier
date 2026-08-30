using System.Text.Json;
using NexusKathFrontier.Launcher.Models;

namespace NexusKathFrontier.Launcher.Services;

public sealed class SettingsService
{
    public UserSettings Load(int defaultRamMb)
    {
        AppPaths.EnsureDirectories();
        if (!File.Exists(AppPaths.SettingsFile))
            return new UserSettings { MaximumRamMb = defaultRamMb };

        try
        {
            return JsonSerializer.Deserialize<UserSettings>(
                File.ReadAllText(AppPaths.SettingsFile), JsonOptions.Default)
                ?? new UserSettings { MaximumRamMb = defaultRamMb };
        }
        catch
        {
            return new UserSettings { MaximumRamMb = defaultRamMb };
        }
    }

    public void Save(UserSettings settings)
    {
        AppPaths.EnsureDirectories();
        File.WriteAllText(AppPaths.SettingsFile, JsonSerializer.Serialize(settings, JsonOptions.Default));
    }
}
