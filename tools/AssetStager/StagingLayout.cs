using System;
using System.Collections.Generic;
using System.IO;

namespace CodeBrix.Audio.Samples.FluidR3Gm.AssetStager;

/// <summary>
/// Where the stager keeps everything, and the rule that all of it is inside this repository: the
/// downloaded archive and the asset that ships. Nothing is written anywhere else on the machine.
/// </summary>
internal sealed class StagingLayout
{
    /// <summary>The file at the repository root that marks the root.</summary>
    private const string RepositoryMarker = "CodeBrix.Audio.Samples.FluidR3Gm.slnx";

    /// <summary>
    /// Initializes the layout of one repository.
    /// </summary>
    /// <param name="repositoryRoot">The absolute path of the repository root.</param>
    private StagingLayout(string repositoryRoot)
    {
        RepositoryRoot = repositoryRoot;
        StagingDirectory = Path.Combine(repositoryRoot, "staging");
        DownloadDirectory = Path.Combine(StagingDirectory, "download");
        OutputDirectory = Path.Combine(StagingDirectory, "output");
        ProvenancePath = Path.Combine(repositoryRoot, "ASSET-PROVENANCE.json");
    }

    /// <summary>The absolute path of the repository root.</summary>
    internal string RepositoryRoot { get; }

    /// <summary>The staging folder, which holds the two working folders.</summary>
    internal string StagingDirectory { get; }

    /// <summary>The upstream archive as it was downloaded. Clearing it costs a re-download, nothing else.</summary>
    internal string DownloadDirectory { get; }

    /// <summary>The asset that ships. Nothing in this tool ever clears it.</summary>
    internal string OutputDirectory { get; }

    /// <summary>The provenance file the stager writes at the repository root.</summary>
    internal string ProvenancePath { get; }

    /// <summary>
    /// Finds the repository this assembly was built inside by walking up from the assembly's own folder
    /// until the marker file appears.
    /// </summary>
    /// <returns>The layout of that repository.</returns>
    /// <exception cref="InvalidOperationException">No folder above the assembly holds the marker file.</exception>
    internal static StagingLayout Discover()
    {
        DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, RepositoryMarker)))
            {
                return new StagingLayout(directory.FullName);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "No folder above " + AppContext.BaseDirectory + " holds " + RepositoryMarker + ", so the"
                + " repository root cannot be found. Run the stager from inside its own repository.");
    }

    /// <summary>
    /// Creates the staging folders that are missing. An existing folder is left exactly as it is.
    /// </summary>
    internal void CreateDirectories()
    {
        Directory.CreateDirectory(StagingDirectory);
        Directory.CreateDirectory(DownloadDirectory);
        Directory.CreateDirectory(OutputDirectory);
    }

    /// <summary>
    /// Adds up the apparent size of everything under the staging folder.
    /// </summary>
    /// <returns>The sum of the lengths of every file under <see cref="StagingDirectory"/>.</returns>
    internal long MeasureStagingBytes()
    {
        return DiskUsage.DirectoryBytes(StagingDirectory);
    }

    /// <summary>
    /// Clears the download folder and leaves <see cref="OutputDirectory"/> untouched.
    /// </summary>
    /// <returns>How many bytes the folder held before it was cleared.</returns>
    internal long ClearWorkingDirectories()
    {
        long before = DiskUsage.DirectoryBytes(DownloadDirectory);
        Clear(DownloadDirectory);
        return before;
    }

    /// <summary>
    /// Empties one working folder and re-creates it, refusing anything that is not a working folder.
    /// </summary>
    /// <param name="directory">The folder to empty.</param>
    /// <exception cref="InvalidOperationException">The folder is not one of the working folders.</exception>
    private void Clear(string directory)
    {
        //A removal this tool performs is limited, by name, to the folder it made itself. Nothing else in
        //the repository - and above all not staging/output - can be reached from here.
        IReadOnlyList<string> allowed = new[] { DownloadDirectory };
        bool isAllowed = false;
        foreach (string candidate in allowed)
        {
            isAllowed |= string.Equals(candidate, directory, StringComparison.Ordinal);
        }

        if (!isAllowed)
        {
            throw new InvalidOperationException(
                "The stager only ever clears its own download folder, and " + directory + " is not it.");
        }

        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }

        Directory.CreateDirectory(directory);
    }
}
