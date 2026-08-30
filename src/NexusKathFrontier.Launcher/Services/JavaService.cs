using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using NexusKathFrontier.Launcher.Models;

namespace NexusKathFrontier.Launcher.Services;

public sealed class JavaService(HttpClient httpClient, HttpDownloadService downloader)
{
    private const string AdoptiumApi =
        "https://api.adoptium.net/v3/assets/latest/21/hotspot?architecture=x64&image_type=jre&os=windows&vendor=eclipse";

    public async Task<string> EnsureJavaAsync(
        int requiredMajor,
        string? preferredPath,
        IProgress<ProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report(new ProgressInfo("Buscando Java 21", 3, "Comprobando el equipo"));
        var existing = await FindCompatibleJavaAsync(requiredMajor, preferredPath, cancellationToken);
        if (existing is not null)
        {
            progress?.Report(new ProgressInfo("Java 21 detectado", 100, existing));
            return existing;
        }

        return await InstallPortableJavaAsync(requiredMajor, progress, cancellationToken);
    }

    private static async Task<string?> FindCompatibleJavaAsync(
        int requiredMajor,
        string? preferredPath,
        CancellationToken cancellationToken)
    {
        var candidates = new List<string?>
        {
            preferredPath,
            Path.Combine(AppPaths.JavaRuntime, "bin", "javaw.exe"),
            Path.Combine(Environment.GetEnvironmentVariable("JAVA_HOME") ?? string.Empty, "bin", "javaw.exe")
        };

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var adoptium = Path.Combine(programFiles, "Eclipse Adoptium");
        if (Directory.Exists(adoptium))
            candidates.AddRange(Directory.EnumerateFiles(adoptium, "javaw.exe", SearchOption.AllDirectories));

        var fromPath = await FindOnPathAsync(cancellationToken);
        candidates.Add(fromPath);

        foreach (var candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(candidate) && await GetJavaMajorAsync(candidate!, cancellationToken) >= requiredMajor)
                return candidate;
        }

        return null;
    }

    private async Task<string> InstallPortableJavaAsync(
        int requiredMajor,
        IProgress<ProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report(new ProgressInfo("Preparando Java 21", 5, "Consultando Eclipse Temurin"));
        var json = await httpClient.GetStringAsync(AdoptiumApi, cancellationToken);
        using var document = JsonDocument.Parse(json);
        var first = document.RootElement.EnumerateArray().FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException("No se encontró un paquete de Java 21 para Windows x64.");

        var package = first.GetProperty("binary").GetProperty("package");
        var url = package.GetProperty("link").GetString()
                  ?? throw new InvalidOperationException("Adoptium no devolvió una URL de Java.");
        var expectedHash = package.GetProperty("checksum").GetString()
                           ?? throw new InvalidOperationException("Adoptium no devolvió el checksum de Java.");

        var archive = Path.Combine(AppPaths.Cache, "temurin-21.zip");
        var extraction = Path.Combine(AppPaths.Cache, "temurin-21-extracted");
        await downloader.DownloadAsync(url, archive, "Instalando Java 21", progress, cancellationToken);

        progress?.Report(new ProgressInfo("Verificando Java 21", 92, "SHA-256"));
        var actualHash = await HashService.Sha256Async(archive, cancellationToken);
        if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("El archivo de Java no superó la verificación de seguridad.");

        if (Directory.Exists(extraction))
            Directory.Delete(extraction, true);
        Directory.CreateDirectory(extraction);
        ZipFile.ExtractToDirectory(archive, extraction, true);

        var javaw = Directory.EnumerateFiles(extraction, "javaw.exe", SearchOption.AllDirectories).FirstOrDefault()
                    ?? throw new InvalidDataException("El paquete descargado no contiene javaw.exe.");
        var root = Directory.GetParent(Directory.GetParent(javaw)!.FullName)!.FullName;

        if (Directory.Exists(AppPaths.JavaRuntime))
            Directory.Delete(AppPaths.JavaRuntime, true);
        Directory.Move(root, AppPaths.JavaRuntime);

        var installed = Path.Combine(AppPaths.JavaRuntime, "bin", "javaw.exe");
        if (await GetJavaMajorAsync(installed, cancellationToken) < requiredMajor)
            throw new InvalidDataException("La versión de Java instalada no es compatible.");

        progress?.Report(new ProgressInfo("Java 21 instalado", 100, "Runtime privado del launcher"));
        return installed;
    }

    private static async Task<string?> FindOnPathAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = "javaw.exe",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process is null) return null;
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<int> GetJavaMajorAsync(string javawPath, CancellationToken cancellationToken)
    {
        try
        {
            var javaExe = Path.Combine(Path.GetDirectoryName(javawPath)!, "java.exe");
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = File.Exists(javaExe) ? javaExe : javawPath,
                Arguments = "-version",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process is null) return 0;
            var text = await process.StandardError.ReadToEndAsync(cancellationToken);
            text += await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var match = Regex.Match(text, "version \\\"(?<major>\\d+)");
            return match.Success && int.TryParse(match.Groups["major"].Value, out var major) ? major : 0;
        }
        catch
        {
            return 0;
        }
    }
}
