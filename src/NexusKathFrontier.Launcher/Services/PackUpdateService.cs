using System.Text.Json;
using NexusKathFrontier.Launcher.Models;

namespace NexusKathFrontier.Launcher.Services;

public sealed class PackUpdateService(HttpClient httpClient, HttpDownloadService downloader)
{
    public static bool IsConfigured(string manifestUrl) =>
        Uri.TryCreate(manifestUrl, UriKind.Absolute, out _) &&
        !manifestUrl.Contains("github.com/OWNER/", StringComparison.OrdinalIgnoreCase);

    public async Task<PackManifest?> GetRemoteManifestAsync(string manifestUrl, CancellationToken cancellationToken)
    {
        if (!IsConfigured(manifestUrl))
            return null;

        var json = await httpClient.GetStringAsync(manifestUrl, cancellationToken);
        return JsonSerializer.Deserialize<PackManifest>(json, JsonOptions.Default)
               ?? throw new InvalidDataException("El manifiesto remoto no tiene un formato válido.");
    }

    public PackManifest? GetInstalledManifest()
    {
        if (!File.Exists(AppPaths.LocalManifest)) return null;
        try
        {
            return JsonSerializer.Deserialize<PackManifest>(
                File.ReadAllText(AppPaths.LocalManifest), JsonOptions.Default);
        }
        catch
        {
            return null;
        }
    }

    public async Task<PackSyncResult> SynchronizeAsync(
        PackManifest manifest,
        bool forceRepair,
        IProgress<ProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        AppPaths.EnsureDirectories();
        var previous = GetInstalledManifest();
        var downloaded = 0;
        var reused = 0;
        var total = Math.Max(1, manifest.Files.Count);

        for (var index = 0; index < manifest.Files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = manifest.Files[index];
            ValidateManifestFile(file);
            var target = ResolveGamePath(file.Path);
            var isValid = !forceRepair && File.Exists(target) &&
                          (await HashService.Sha256Async(target, cancellationToken))
                          .Equals(file.Sha256, StringComparison.OrdinalIgnoreCase);

            if (isValid)
            {
                reused++;
                progress?.Report(new ProgressInfo(
                    "Verificando el modpack",
                    (index + 1) * 100d / total,
                    file.Path));
                continue;
            }

            var temp = target + ".nkf-download";
            var currentIndex = index;
            var perFileProgress = new Progress<ProgressInfo>(item =>
            {
                var global = (currentIndex + item.Percentage / 100d) * 100d / total;
                progress?.Report(new ProgressInfo("Actualizando NEXUS", global, file.Path));
            });

            try
            {
                await downloader.DownloadAsync(file.Url, temp, "Descargando", perFileProgress, cancellationToken);
                var downloadedHash = await HashService.Sha256Async(temp, cancellationToken);
                if (!downloadedHash.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Falló la verificación SHA-256 de {file.Path}.");

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Move(temp, target, true);
                downloaded++;
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        DeleteRemovedManagedFiles(previous, manifest);
        await File.WriteAllTextAsync(
            AppPaths.LocalManifest,
            JsonSerializer.Serialize(manifest, JsonOptions.Default),
            cancellationToken);

        progress?.Report(new ProgressInfo("Modpack listo", 100, $"Versión {manifest.PackVersion}"));
        return new PackSyncResult(manifest.PackVersion, downloaded, reused, downloaded > 0);
    }

    private static void DeleteRemovedManagedFiles(PackManifest? previous, PackManifest current)
    {
        if (previous is null) return;
        var currentPaths = current.Files.Select(x => x.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var oldFile in previous.Files.Where(x => !currentPaths.Contains(x.Path)))
        {
            var target = ResolveGamePath(oldFile.Path);
            if (File.Exists(target)) File.Delete(target);
        }
    }

    private static string ResolveGamePath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(AppPaths.Game, normalized));
        var root = Path.GetFullPath(AppPaths.Game) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Ruta insegura en el manifiesto: {relativePath}");
        return fullPath;
    }

    private static void ValidateManifestFile(PackFile file)
    {
        if (string.IsNullOrWhiteSpace(file.Path) || Path.IsPathRooted(file.Path))
            throw new InvalidDataException("El manifiesto contiene una ruta inválida.");
        if (!Uri.TryCreate(file.Url, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
            throw new InvalidDataException($"URL inválida para {file.Path}.");
        if (file.Sha256.Length != 64 || !file.Sha256.All(Uri.IsHexDigit))
            throw new InvalidDataException($"SHA-256 inválido para {file.Path}.");
    }
}
