using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installers;
using CmlLib.Core.ProcessBuilder;
using NexusKathFrontier.Launcher.Models;

namespace NexusKathFrontier.Launcher.Services;

public sealed class MinecraftService
{
    private readonly MinecraftLauncher launcher = new(new MinecraftPath(AppPaths.Game));

    public async Task InstallVersionAsync(
        string version,
        IProgress<ProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        EventHandler<InstallerProgressChangedEventArgs> fileHandler = (_, args) =>
        {
            var percentage = args.TotalTasks > 0 ? args.ProgressedTasks * 100d / args.TotalTasks : 0;
            progress?.Report(new ProgressInfo("Instalando Minecraft", percentage, args.Name ?? version));
        };
        EventHandler<ByteProgress> byteHandler = (_, args) =>
        {
            if (args.TotalBytes <= 0) return;
            progress?.Report(new ProgressInfo(
                "Descargando archivos oficiales",
                args.ToRatio() * 100d,
                version));
        };

        launcher.FileProgressChanged += fileHandler;
        launcher.ByteProgressChanged += byteHandler;
        try
        {
            await launcher.InstallAsync(version, cancellationToken).AsTask();
        }
        finally
        {
            launcher.FileProgressChanged -= fileHandler;
            launcher.ByteProgressChanged -= byteHandler;
        }
    }

    public async Task<int> LaunchAsync(
        string version,
        MSession session,
        string javaPath,
        int maximumRamMb,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        var process = await launcher.BuildProcessAsync(version, new MLaunchOption
        {
            Session = session,
            JavaPath = javaPath,
            MinimumRamMb = 1024,
            MaximumRamMb = maximumRamMb
        });

        var wrapper = new ProcessWrapper(process);
        wrapper.OutputReceived += (_, line) => log?.Invoke(line);
        wrapper.StartWithEvents();
        log?.Invoke($"Minecraft iniciado. PID: {process.Id}");
        cancellationToken.ThrowIfCancellationRequested();
        return await wrapper.WaitForExitTaskAsync();
    }
}
