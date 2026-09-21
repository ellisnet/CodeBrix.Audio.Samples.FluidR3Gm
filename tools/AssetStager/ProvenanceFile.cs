using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CodeBrix.Audio.Samples.FluidR3Gm.AssetStager;

/// <summary>
/// Writes ASSET-PROVENANCE.json at the repository root: where the archive came from and what it
/// weighs and hashes to, everything it held, what was taken out of it, where the licence text came
/// from, and which machine produced the record.
/// </summary>
/// <remarks>
/// THE MACHINE IS PART OF THE RECORD. Re-staging on the same machine reproduces the same bytes and
/// that is the gate. This particular asset is a file copied out of an archive rather than something
/// computed, so the same bytes are expected everywhere - but the record still says where it was
/// measured, because a hash with no machine beside it is not provenance.
/// </remarks>
internal static class ProvenanceFile
{
    /// <summary>What this file's own shape is called, so that a reader can tell one version from another.</summary>
    private const string Schema = "codebrix.asset-provenance/1";

    /// <summary>
    /// Writes the provenance file.
    /// </summary>
    /// <param name="path">Where to write it.</param>
    /// <param name="upstreamName">What the upstream asset is called.</param>
    /// <param name="address">The address the archive was fetched from.</param>
    /// <param name="licenseId">The SPDX identifier of the licence the asset is under.</param>
    /// <param name="licenseSource">Where that licence was read from.</param>
    /// <param name="archive">The downloaded archive, measured.</param>
    /// <param name="archiveEntries">Every entry the archive holds.</param>
    /// <param name="artifacts">Every file that ships, measured.</param>
    /// <param name="outputFolder">The folder the artifacts sit in, relative to the repository root.</param>
    internal static void Write(
        string path,
        string upstreamName,
        string address,
        string licenseId,
        string licenseSource,
        ArtifactFile archive,
        IReadOnlyList<ArchiveEntry> archiveEntries,
        IReadOnlyList<ArtifactFile> artifacts,
        string outputFolder)
    {
        //Nothing here is ever put in a web page, and a file a maintainer reads is better off with its
        //punctuation left alone, so the relaxed encoder is the right one.
        var options = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using FileStream stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, options);

        writer.WriteStartObject();
        writer.WriteString("schema", Schema);
        writer.WriteString("stagedAtUtc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));

        WriteUpstream(writer, upstreamName, address, licenseId, licenseSource, archive, archiveEntries);
        WriteArtifacts(writer, artifacts, outputFolder);
        WriteMachine(writer);

        writer.WriteEndObject();
        writer.Flush();
        stream.Write(new byte[] { (byte)'\n' }, 0, 1);
    }

    /// <summary>
    /// Writes where the asset came from and what the archive it came in held.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="upstreamName">What the upstream asset is called.</param>
    /// <param name="address">The address the archive was fetched from.</param>
    /// <param name="licenseId">The SPDX identifier of the licence the asset is under.</param>
    /// <param name="licenseSource">Where that licence was read from.</param>
    /// <param name="archive">The downloaded archive, measured.</param>
    /// <param name="archiveEntries">Every entry the archive holds.</param>
    private static void WriteUpstream(
        Utf8JsonWriter writer,
        string upstreamName,
        string address,
        string licenseId,
        string licenseSource,
        ArtifactFile archive,
        IReadOnlyList<ArchiveEntry> archiveEntries)
    {
        writer.WriteStartObject("upstream");
        writer.WriteString("name", upstreamName);
        writer.WriteString("address", address);
        writer.WriteString("licenseId", licenseId);
        writer.WriteString("licenseSource", licenseSource);

        writer.WriteStartObject("archive");
        writer.WriteString("name", archive.Name);
        writer.WriteNumber("bytes", archive.Bytes);
        writer.WriteString("sha256", archive.Sha256);
        writer.WriteEndObject();

        writer.WriteStartArray("archiveContents");
        foreach (ArchiveEntry entry in archiveEntries)
        {
            writer.WriteStartObject();
            writer.WriteString("path", entry.EntryPath);
            writer.WriteString("entryType", entry.EntryType);
            writer.WriteNumber("bytes", entry.Bytes);
            writer.WriteString("extractedAs", entry.ExtractedAs);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes what ships, with the size and the digest of every file of it.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="artifacts">The measured files.</param>
    /// <param name="outputFolder">The folder they sit in, relative to the repository root.</param>
    private static void WriteArtifacts(
        Utf8JsonWriter writer, IReadOnlyList<ArtifactFile> artifacts, string outputFolder)
    {
        writer.WriteStartObject("artifact");
        writer.WriteString("folder", outputFolder);

        long total = 0;
        writer.WriteStartArray("files");
        foreach (ArtifactFile file in artifacts)
        {
            total += file.Bytes;
            writer.WriteStartObject();
            writer.WriteString("name", file.Name);
            writer.WriteNumber("bytes", file.Bytes);
            writer.WriteString("sha256", file.Sha256);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteNumber("totalBytes", total);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes the machine the asset was staged on.
    /// </summary>
    /// <param name="writer">The writer.</param>
    private static void WriteMachine(Utf8JsonWriter writer)
    {
        writer.WriteStartObject("stagedOn");
        writer.WriteString("operatingSystem", RuntimeInformation.OSDescription);
        writer.WriteString("runtimeIdentifier", RuntimeInformation.RuntimeIdentifier);
        writer.WriteString("processArchitecture", RuntimeInformation.ProcessArchitecture.ToString());
        writer.WriteString("processor", ProcessorName());
        writer.WriteNumber("processorCount", Environment.ProcessorCount);
        writer.WriteString("framework", RuntimeInformation.FrameworkDescription);
        writer.WriteEndObject();
    }

    /// <summary>
    /// The processor's own name, read from where the operating system publishes it.
    /// </summary>
    /// <returns>The name, or <see langword="null"/> when this platform does not publish one.</returns>
    private static string ProcessorName()
    {
        const string CpuInfo = "/proc/cpuinfo";
        if (!File.Exists(CpuInfo))
        {
            return null;
        }

        try
        {
            foreach (string line in File.ReadLines(CpuInfo))
            {
                if (!line.StartsWith("model name", StringComparison.Ordinal))
                {
                    continue;
                }

                int colon = line.IndexOf(':');
                if (colon >= 0 && colon + 1 < line.Length)
                {
                    return line.Substring(colon + 1).Trim();
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return null;
    }
}
