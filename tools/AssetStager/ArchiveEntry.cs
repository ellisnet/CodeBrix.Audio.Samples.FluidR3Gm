using System.Globalization;

namespace CodeBrix.Audio.Samples.FluidR3Gm.AssetStager;

/// <summary>
/// One entry of the upstream archive, as the archive describes it: what it is called inside the
/// archive, what kind of entry it is, and how large it is.
/// </summary>
/// <remarks>
/// EVERY entry is recorded, not only the ones that are extracted. A provenance record that said
/// nothing about what else the archive held would leave the next person to open it themselves.
/// </remarks>
internal sealed class ArchiveEntry
{
    /// <summary>
    /// Initializes one recorded entry.
    /// </summary>
    /// <param name="entryPath">The entry's name inside the archive.</param>
    /// <param name="entryType">What kind of entry it is, in the archive format's own spelling.</param>
    /// <param name="bytes">How many bytes of content it carries.</param>
    /// <param name="extractedAs">The name it was written under, or <see langword="null"/> when it was not extracted.</param>
    internal ArchiveEntry(string entryPath, string entryType, long bytes, string extractedAs)
    {
        EntryPath = entryPath;
        EntryType = entryType;
        Bytes = bytes;
        ExtractedAs = extractedAs;
    }

    /// <summary>The entry's name inside the archive.</summary>
    internal string EntryPath { get; }

    /// <summary>What kind of entry it is, in the archive format's own spelling.</summary>
    internal string EntryType { get; }

    /// <summary>How many bytes of content the entry carries.</summary>
    internal long Bytes { get; }

    /// <summary>
    /// The name this entry was written under in the output folder, or <see langword="null"/> when the
    /// entry was read past and not extracted.
    /// </summary>
    internal string ExtractedAs { get; }

    /// <summary>Whether this entry was extracted.</summary>
    internal bool WasExtracted => !string.IsNullOrEmpty(ExtractedAs);

    /// <summary>
    /// The entry as one line of a listing.
    /// </summary>
    /// <returns>The line.</returns>
    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0,12:N0}  {1,-12}  {2}{3}",
            Bytes,
            EntryType,
            EntryPath,
            WasExtracted ? "   -> " + ExtractedAs : string.Empty);
    }
}
