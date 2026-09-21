using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CodeBrix.Audio.Samples.FluidR3Gm.AssetStager;

/// <summary>
/// Fetches the upstream archive over HTTP with nothing but the framework's own client, reporting what
/// it has as it goes because the file is large enough that a silent minute looks like a hang.
/// </summary>
internal static class Downloader
{
    /// <summary>The buffer each read fills. Large enough that the progress arithmetic is not the cost.</summary>
    private const int BufferBytes = 1 << 20;

    /// <summary>
    /// Downloads a file unless it is already on disk at the size that was expected of it.
    /// </summary>
    /// <param name="address">The address to fetch.</param>
    /// <param name="destinationPath">Where the file goes.</param>
    /// <param name="expectedBytes">The size the file is expected to have, or 0 when it is not known.</param>
    /// <param name="cancellationToken">A token that cancels the download.</param>
    /// <returns>How many bytes this call fetched over the network; zero when the file was already there.</returns>
    /// <exception cref="InvalidOperationException">The server answered with a length other than the expected one.</exception>
    internal static async Task<long> FetchAsync(
        string address, string destinationPath, long expectedBytes, CancellationToken cancellationToken)
    {
        var existing = new FileInfo(destinationPath);
        if (existing.Exists && (expectedBytes <= 0 || existing.Length == expectedBytes))
        {
            Log.Detail("already downloaded, " + Log.Bytes(existing.Length));
            return 0;
        }

        if (existing.Exists)
        {
            //A file of the wrong size is a partial or a changed upstream, and either way the only honest
            //thing to do is fetch it again from the beginning.
            Log.Detail("a file of " + Log.Bytes(existing.Length) + " is there but "
                + expectedBytes.ToString("N0", CultureInfo.InvariantCulture)
                + " bytes were expected, so it is fetched again");
            File.Delete(destinationPath);
        }

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(30);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CodeBrix.Audio.Samples.FluidR3Gm-AssetStager/1.0");

        using HttpResponseMessage response = await client
            .GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        long reported = response.Content.Headers.ContentLength ?? -1;
        if (expectedBytes > 0 && reported > 0 && reported != expectedBytes)
        {
            throw new InvalidOperationException(
                "The server offers " + reported.ToString("N0", CultureInfo.InvariantCulture)
                    + " bytes where " + expectedBytes.ToString("N0", CultureInfo.InvariantCulture)
                    + " were expected. The source of record has changed and this run stops rather than"
                    + " staging something else under the same name.");
        }

        long total = 0;
        DateTime next = DateTime.MinValue;

        using (Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        using (var destination = new FileStream(
            destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferBytes))
        {
            byte[] buffer = new byte[BufferBytes];
            while (true)
            {
                int read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                total += read;

                if (DateTime.UtcNow >= next)
                {
                    Log.Detail(Describe(total, reported > 0 ? reported : expectedBytes));
                    next = DateTime.UtcNow + Log.ProgressInterval;
                }
            }
        }

        Log.Detail(Describe(total, reported > 0 ? reported : expectedBytes));
        return total;
    }

    /// <summary>
    /// One progress report as a line of text.
    /// </summary>
    /// <param name="completed">How many bytes have arrived.</param>
    /// <param name="total">How many are expected, or a value at or below zero when that is not known.</param>
    /// <returns>The line.</returns>
    private static string Describe(long completed, long total)
    {
        if (total <= 0)
        {
            return string.Format(
                CultureInfo.InvariantCulture, "downloading  {0:N0} bytes", completed);
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "downloading  {0:F1}%  ({1:N0} / {2:N0} bytes)",
            100.0 * completed / total,
            completed,
            total);
    }
}
