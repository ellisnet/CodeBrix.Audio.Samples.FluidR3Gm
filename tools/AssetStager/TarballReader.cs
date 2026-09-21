using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;

namespace CodeBrix.Audio.Samples.FluidR3Gm.AssetStager;

/// <summary>
/// Reads the upstream <c>.tar.gz</c> in ONE streaming pass with nothing but the framework:
/// <see cref="GZipStream"/> decompresses it and <see cref="TarReader"/> walks it. Every entry is
/// recorded and only the wanted ones are written out.
/// </summary>
/// <remarks>
/// <para>
/// A ONE-PASS READ IS THE POINT. The archive is well over a hundred megabytes compressed and the
/// soundfont inside it is larger still, so nothing is ever held in memory and nothing is unpacked
/// wholesale to be picked over afterwards: an entry is either written straight out or read past.
/// </para>
/// <para>
/// NOTHING IS EVER WRITTEN OUTSIDE THE OUTPUT FOLDER. An entry's own path inside the archive is
/// never used as a destination - the caller names every file it wants and the name it wants it
/// under - so a crafted archive cannot reach up out of the folder.
/// </para>
/// </remarks>
internal static class TarballReader
{
    /// <summary>The buffer each copy uses.</summary>
    private const int BufferBytes = 1 << 20;

    /// <summary>
    /// Walks the archive, extracting the wanted entries and recording all of them.
    /// </summary>
    /// <param name="archivePath">The <c>.tar.gz</c> to read.</param>
    /// <param name="outputDirectory">The folder the extracted files are written to.</param>
    /// <param name="wanted">
    /// What to extract: the key is an entry's file name without its folders, matched
    /// case-insensitively, and the value is the name to write it under. Several keys may name the
    /// same output file - upstream projects spell a licence file COPYING or LICENSE - and the FIRST
    /// entry that matches wins, so a later one never overwrites it.
    /// </param>
    /// <returns>Every entry the archive holds, in the order the archive holds them.</returns>
    internal static IReadOnlyList<ArchiveEntry> Extract(
        string archivePath, string outputDirectory, IReadOnlyDictionary<string, string> wanted)
    {
        var entries = new List<ArchiveEntry>();
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using FileStream file = new FileStream(
            archivePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferBytes,
            FileOptions.SequentialScan);
        using var decompressed = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new TarReader(decompressed);

        TarEntry entry;
        while ((entry = reader.GetNextEntry(copyData: false)) != null)
        {
            string name = Path.GetFileName(entry.Name.TrimEnd('/'));
            string extractedAs = null;

            if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile
                && wanted.TryGetValue(name, out string outputName)
                && taken.Add(outputName))
            {
                extractedAs = outputName;
                Write(entry, Path.Combine(outputDirectory, outputName));
            }

            entries.Add(new ArchiveEntry(
                entry.Name, entry.EntryType.ToString(), Math.Max(entry.Length, 0), extractedAs));
        }

        return entries;
    }

    /// <summary>
    /// Writes one entry's content to a file, replacing whatever was there.
    /// </summary>
    /// <param name="entry">The entry to write.</param>
    /// <param name="destinationPath">The full path to write it to.</param>
    /// <exception cref="InvalidDataException">The entry carries no content stream.</exception>
    private static void Write(TarEntry entry, string destinationPath)
    {
        Stream content = entry.DataStream;
        if (content == null)
        {
            throw new InvalidDataException(
                "The archive entry " + entry.Name + " carries no content, so it cannot be extracted.");
        }

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        using var destination = new FileStream(
            destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferBytes);
        content.CopyTo(destination, BufferBytes);
    }
}
