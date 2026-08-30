using NexusKathFrontier.Launcher.Models;

namespace NexusKathFrontier.Launcher.Services;

public sealed class HttpDownloadService(HttpClient httpClient)
{
    public async Task DownloadAsync(
        string url,
        string destination,
        string message,
        IProgress<ProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        using var response = await httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? 0;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        var buffer = new byte[81920];
        long received = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            received += read;
            var percentage = total > 0 ? received * 100d / total : 0;
            progress?.Report(new ProgressInfo(message, percentage, FormatBytes(received, total)));
        }
    }

    private static string FormatBytes(long received, long total)
    {
        static double Mb(long value) => value / 1024d / 1024d;
        return total > 0
            ? $"{Mb(received):0.0} MB / {Mb(total):0.0} MB"
            : $"{Mb(received):0.0} MB";
    }
}
