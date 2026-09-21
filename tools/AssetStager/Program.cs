using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CodeBrix.Audio.Samples.FluidR3Gm.AssetStager;

/// <summary>
/// Reproduces this repository's shipped soundfont asset from upstream: the FluidR3_GM source archive
/// is downloaded, checked against the size and the sha256 recorded for it, read in one pass, and the
/// soundfont and the upstream licence text are written to <c>staging/output</c> with a provenance
/// file beside the repository's LICENSE.
/// </summary>
/// <remarks>
/// <para>
/// NOTHING HAS TO BE INSTALLED AND NO PACKAGE IS REFERENCED. The download is
/// <see cref="System.Net.Http.HttpClient"/>, the decompression is
/// <see cref="System.IO.Compression.GZipStream"/> and the archive is read by
/// <see cref="System.Formats.Tar.TarReader"/> - all three are in the framework, so a machine with
/// nothing but .NET on it can reproduce the asset.
/// </para>
/// <para>
/// EVERYTHING STAYS INSIDE THE REPOSITORY, under <c>staging/</c>, and the soundfont is never
/// committed: it is far larger than a git host will take and there is no reason to store what an
/// upstream archive reproduces exactly.
/// </para>
/// </remarks>
internal static class Program
{
    /// <summary>What the upstream asset is called.</summary>
    private const string UpstreamName = "FluidR3_GM";

    /// <summary>
    /// THE SOURCE OF RECORD: Debian's archive copy of the upstream tarball. The address is versioned
    /// and stable, and the licence evidence for this soundfont was read from the copyright file of
    /// this same Debian source package - so the file and the licence share one origin.
    /// </summary>
    private const string ArchiveAddress =
        "https://deb.debian.org/debian/pool/main/f/fluid-soundfont/fluid-soundfont_3.1.orig.tar.gz";

    /// <summary>The name the archive is kept under in the download folder.</summary>
    private const string ArchiveFileName = "fluid-soundfont_3.1.orig.tar.gz";

    /// <summary>The archive's size. A server that offers another length is not serving this archive.</summary>
    private const long ExpectedArchiveBytes = 134_835_922L;

    /// <summary>
    /// The archive's sha256. It is checked BEFORE anything is unpacked: an archive that is not the one
    /// of record is not unpacked at all, because the point of staging is to reproduce a known file.
    /// </summary>
    private const string ExpectedArchiveSha256 =
        "2621acaa1c78e4abdb24bdd163230cc577e61276936d6aa6e3180582142f0343";

    /// <summary>The licence the soundfont is under, as an SPDX identifier.</summary>
    private const string LicenseId = "MIT";

    /// <summary>Where that licence was read from.</summary>
    private const string LicenseSource =
        "the COPYING file carried in this archive, which holds the MIT licence text, and the README "
            + "beside it, in which the author releases the work under that licence; both are extracted "
            + "into the output folder with the soundfont";

    /// <summary>The one soundfont this repository ships, as the archive spells it.</summary>
    private const string SoundFontEntryName = "FluidR3_GM.sf2";

    /// <summary>The name that soundfont is written under.</summary>
    private const string SoundFontOutputName = "FluidR3_GM.sf2";

    /// <summary>The name the upstream licence text is written under.</summary>
    private const string LicenseOutputName = "LICENSE-FluidR3_GM.txt";

    /// <summary>The name the upstream README is written under.</summary>
    private const string ReadmeOutputName = "README-FluidR3_GM.txt";

    /// <summary>
    /// The soundfont's size, measured on the machine the asset of record was staged on. The soundfont
    /// is COPIED out of the archive rather than computed from it, so the same bytes are expected on
    /// every machine - the record still names the machine, because a hash with no machine beside it
    /// says nothing about what a mismatch means.
    /// </summary>
    private const long ExpectedSoundFontBytes = 148_398_306L;

    /// <summary>The soundfont's sha256, measured on that same machine.</summary>
    private const string ExpectedSoundFontSha256 =
        "74594e8f4250680adf590507a306655a299935343583256f3b722c48a1bc1cb0";

    /// <summary>
    /// What the run needs free before it starts: the archive, the soundfont it unpacks to, the licence
    /// text, and room over.
    /// </summary>
    private const long RequiredFreeBytes = 1_073_741_824L;

    /// <summary>How many steps the run reports.</summary>
    private const int StepCount = 5;

    /// <summary>
    /// What is taken out of the archive, and the name each file is written under. The licence text is
    /// spelled differently by different upstream projects, so several names map onto one output file
    /// and the first entry that matches wins.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> Wanted =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SoundFontEntryName] = SoundFontOutputName,
            ["COPYING"] = LicenseOutputName,
            ["COPYING.txt"] = LicenseOutputName,
            ["LICENSE"] = LicenseOutputName,
            ["LICENSE.txt"] = LicenseOutputName,
            ["README"] = ReadmeOutputName,
            ["README.txt"] = ReadmeOutputName
        };

    /// <summary>
    /// Runs the staging.
    /// </summary>
    /// <param name="args">The command line.</param>
    /// <returns>0 when the asset was staged and matched, 1 on a failure, 2 on a mismatch.</returns>
    private static async Task<int> Main(string[] args)
    {
        StagingLayout layout;
        try
        {
            layout = StagingLayout.Discover();
            layout.CreateDirectories();
        }
        catch (InvalidOperationException error)
        {
            Log.Error(error.Message);
            return 1;
        }

        bool cleanAfter = false;
        bool keep = false;
        foreach (string argument in args)
        {
            switch (argument)
            {
                case "--clean-after":
                    cleanAfter = true;
                    break;
                case "--keep":
                    keep = true;
                    break;
                case "--help":
                case "-h":
                    WriteUsage();
                    return 0;
                default:
                    Log.Error("Unknown argument " + argument + ".");
                    WriteUsage();
                    return 1;
            }
        }

        Log.Line("FluidR3_GM asset stager - " + UpstreamName);
        Log.Detail("repository root   " + layout.RepositoryRoot);
        Log.Detail("download          " + layout.DownloadDirectory);
        Log.Detail("output            " + layout.OutputDirectory);

        var total = Stopwatch.StartNew();
        using var watcher = new StagingWatcher(layout);
        var report = new UsageReport();

        try
        {
            int result = await StageAsync(layout, watcher, report, CancellationToken.None)
                .ConfigureAwait(false);
            total.Stop();
            report.Write(watcher, total.Elapsed);

            if (result == 0)
            {
                Clean(layout, cleanAfter, keep);
                Log.Blank();
                Log.Line("DONE. The asset is in " + layout.OutputDirectory + ".");
            }

            return result;
        }
        catch (Exception error)
        {
            total.Stop();
            Log.Blank();
            Log.Error(error.GetType().Name + ": " + error.Message);
            Log.Detail("The staging folders are left exactly as they are, so a second run resumes.");
            return 1;
        }
    }

    /// <summary>
    /// Downloads the archive, checks it, takes the soundfont and the licence text out of it and writes
    /// the provenance.
    /// </summary>
    /// <param name="layout">Where everything goes.</param>
    /// <param name="watcher">The watcher that samples what the run costs.</param>
    /// <param name="report">Where the timings and the byte counts are collected.</param>
    /// <param name="cancellationToken">A token that cancels the run.</param>
    /// <returns>0 when the asset matched what was recorded, 1 on a failure, 2 on a mismatch.</returns>
    private static async Task<int> StageAsync(
        StagingLayout layout,
        StagingWatcher watcher,
        UsageReport report,
        CancellationToken cancellationToken)
    {
        var step = Stopwatch.StartNew();

        Log.Step(1, StepCount, "free space");
        long free = DiskUsage.AvailableFreeBytes(layout.StagingDirectory);
        Log.Detail("needed     " + Log.Bytes(RequiredFreeBytes));
        Log.Detail("available  " + (free < 0 ? "unknown on this platform" : Log.Bytes(free)));
        if (free >= 0 && free < RequiredFreeBytes)
        {
            Log.Error("There is not enough free space to stage this asset. Free some and run again.");
            return 1;
        }

        report.Record("free space", step.Elapsed);

        //DOWNLOAD. An archive already on disk at the right size is kept, so a second run costs no
        //  bandwidth and the two runs the gate asks for are cheap.
        step.Restart();
        Log.Step(2, StepCount, "download " + ArchiveFileName);
        Log.Detail("from       " + ArchiveAddress);
        string archivePath = Path.Combine(layout.DownloadDirectory, ArchiveFileName);
        report.DownloadedBytes = await Downloader
            .FetchAsync(ArchiveAddress, archivePath, ExpectedArchiveBytes, cancellationToken)
            .ConfigureAwait(false);
        watcher.Sample();
        report.Record("download", step.Elapsed);

        //CHECK THE ARCHIVE BEFORE OPENING IT. Unpacking an archive that is not the one of record would
        //  stage something nobody has checked, so this stops instead.
        step.Restart();
        Log.Step(3, StepCount, "check the archive");
        ArtifactFile archive = ArtifactFile.Measure(archivePath);
        Log.Detail("bytes      " + archive.Bytes.ToString("N0", CultureInfo.InvariantCulture)
            + "   expected " + ExpectedArchiveBytes.ToString("N0", CultureInfo.InvariantCulture));
        Log.Detail("sha256     " + archive.Sha256);
        Log.Detail("expected   " + ExpectedArchiveSha256);
        report.Record("check the archive", step.Elapsed);

        if (!archive.Matches(ExpectedArchiveBytes, ExpectedArchiveSha256))
        {
            Log.Blank();
            Log.Error("The downloaded archive is not the one recorded for this repository.");
            Log.Detail("NOTHING IS UNPACKED. Either the download is damaged - delete "
                + layout.DownloadDirectory + " and run again - or upstream has replaced the file at a"
                + " versioned address, which is a change of the source of record and wants a human"
                + " decision. If the new archive is genuinely the one to ship, put the measured size and"
                + " sha256 into ExpectedArchiveBytes and ExpectedArchiveSha256 in"
                + " tools/AssetStager/Program.cs and run again.");
            return 2;
        }

        Log.Detail("MATCH - this is the archive of record.");

        //EXTRACT, in one streaming pass. Only the soundfont and the upstream licence text come out;
        //  everything else in the archive is read past and recorded.
        step.Restart();
        Log.Step(4, StepCount, "extract the soundfont and the licence text");
        IReadOnlyList<ArchiveEntry> entries = TarballReader.Extract(
            archivePath, layout.OutputDirectory, Wanted);

        Log.Detail("the archive holds " + entries.Count + " entries:");
        foreach (ArchiveEntry entry in entries)
        {
            Log.Detail("    " + entry);
        }

        string soundFontPath = Path.Combine(layout.OutputDirectory, SoundFontOutputName);
        if (!File.Exists(soundFontPath))
        {
            Log.Error("The archive does not hold " + SoundFontEntryName + ", so there is nothing to ship."
                + " The listing above says what it does hold.");
            return 1;
        }

        string licensePath = Path.Combine(layout.OutputDirectory, LicenseOutputName);
        string readmePath = Path.Combine(layout.OutputDirectory, ReadmeOutputName);
        if (!File.Exists(licensePath) && !File.Exists(readmePath))
        {
            Log.Error("The archive holds no licence or README text, and this package may not ship a"
                + " soundfont without the notice that travels with it. The listing above says what the"
                + " archive does hold.");
            return 1;
        }

        var artifacts = new List<ArtifactFile> { ArtifactFile.Measure(soundFontPath) };
        if (File.Exists(licensePath))
        {
            artifacts.Add(ArtifactFile.Measure(licensePath));
        }

        if (File.Exists(readmePath))
        {
            artifacts.Add(ArtifactFile.Measure(readmePath));
        }

        foreach (ArtifactFile file in artifacts)
        {
            Log.Detail("staged     " + file.Name + "  " + Log.Bytes(file.Bytes));
            Log.Detail("           sha256 " + file.Sha256);
        }

        ArtifactFile soundFont = artifacts[0];
        Log.Detail("soundfont  " + soundFont.Bytes.ToString("N0", CultureInfo.InvariantCulture)
            + " bytes   expected " + ExpectedSoundFontBytes.ToString("N0", CultureInfo.InvariantCulture));
        Log.Detail("expected   " + ExpectedSoundFontSha256);
        watcher.Sample();
        report.Record("extract and hash", step.Elapsed);

        bool matched = soundFont.Matches(ExpectedSoundFontBytes, ExpectedSoundFontSha256);
        Log.Detail(matched
            ? "MATCH - this run reproduced the asset of record."
            : "MISMATCH - this run did NOT reproduce the asset of record.");

        //PROVENANCE. It is written whatever the comparison said, because a file that does not match is
        //  exactly the one whose origin and machine somebody will want to read.
        step.Restart();
        Log.Step(5, StepCount, "write ASSET-PROVENANCE.json");
        ProvenanceFile.Write(
            layout.ProvenancePath,
            UpstreamName,
            ArchiveAddress,
            LicenseId,
            LicenseSource,
            archive,
            entries,
            artifacts,
            "staging/output");
        Log.Detail("wrote      " + layout.ProvenancePath);
        report.Record("provenance", step.Elapsed);

        if (!matched)
        {
            Log.Blank();
            Log.Error("The staged soundfont is not the one recorded for this repository.");
            Log.Detail("The archive itself matched, so the difference came out of the unpacking rather"
                + " than out of the download. ASSET-PROVENANCE.json records what this run produced and"
                + " on which machine. This wants investigating before anything is published.");
            return 2;
        }

        return 0;
    }

    /// <summary>
    /// Clears the download folder when the run is allowed to, and never touches the output folder.
    /// </summary>
    /// <param name="layout">Where everything is.</param>
    /// <param name="cleanAfter">Whether the command line asked for it.</param>
    /// <param name="keep">Whether the command line forbade it.</param>
    private static void Clean(StagingLayout layout, bool cleanAfter, bool keep)
    {
        if (keep)
        {
            return;
        }

        bool clear = cleanAfter;
        if (!clear && !Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            Log.Blank();
            Console.Write("Clear staging/download now? staging/output is kept. [y/N] ");
            string answer = Console.ReadLine();
            clear = answer != null && answer.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);
        }

        if (!clear)
        {
            return;
        }

        long freed = layout.ClearWorkingDirectories();
        Log.Line("Cleared the download folder: " + Log.Bytes(freed) + " given back. staging/output is kept.");
    }

    /// <summary>
    /// Writes what the tool does and what it takes.
    /// </summary>
    private static void WriteUsage()
    {
        Console.WriteLine();
        Console.WriteLine("AssetStager - reproduces this repository's shipped soundfont from upstream.");
        Console.WriteLine();
        Console.WriteLine("  dotnet run --project tools/AssetStager -c Release [options]");
        Console.WriteLine();
        Console.WriteLine("  --clean-after   clear staging/download when the run succeeds.");
        Console.WriteLine("                  staging/output is always kept.");
        Console.WriteLine("  --keep          never ask and never clear.");
        Console.WriteLine("  --help, -h      this text.");
        Console.WriteLine();
        Console.WriteLine("Everything the tool writes stays inside the repository. See staging/README.txt.");
        Console.WriteLine();
    }
}
