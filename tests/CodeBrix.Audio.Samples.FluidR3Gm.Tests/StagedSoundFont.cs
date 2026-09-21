using System;
using System.IO;

namespace CodeBrix.Audio.Samples.FluidR3Gm.Tests;

/// <summary>
/// Finds the SoundFont the asset stager put in <c>staging/output</c> and points the library at it.
/// </summary>
/// <remarks>
/// <para>
/// THE SOUNDFONT IS NOT IN GIT and it is not copied beside this test assembly either: the package's
/// build assets land it in the output of a project that references the PACKAGE, and this project
/// references the library by project. So the tests read it where the stager leaves it, through a
/// path found by walking up to the repository root - which keeps the tests inside the repository,
/// with no absolute path and nothing reached over the network.
/// </para>
/// <para>
/// A CHECKOUT THAT HAS NOT BEEN STAGED SKIPS those tests with a message saying what to run, rather
/// than failing: a contributor working on the documentation should not have to download the archive
/// first.
/// </para>
/// </remarks>
internal static class StagedSoundFont
{
    /// <summary>The file at the repository root that marks the root.</summary>
    private const string RepositoryMarker = "CodeBrix.Audio.Samples.FluidR3Gm.slnx";

    private static readonly object Gate = new object();

    private static bool pointed;

    /// <summary>
    /// The full path of the staged SoundFont, or <see langword="null"/> when the repository root
    /// cannot be found from where this assembly is running.
    /// </summary>
    internal static string FullPath { get; } = Locate();

    /// <summary>Whether the SoundFont has been staged.</summary>
    internal static bool IsStaged => FullPath != null && File.Exists(FullPath);

    /// <summary>What to tell somebody whose checkout has not been staged.</summary>
    internal static string NotStagedMessage =>
        "The FluidR3_GM SoundFont has not been staged, so this test has nothing to play. Run the "
            + "asset stager from the repository root and then run the tests again:  "
            + "dotnet run --project tools/AssetStager -c Release";

    /// <summary>
    /// Points <see cref="FluidR3GmInstrumentLibrary"/> at the staged SoundFont, once per test run.
    /// </summary>
    /// <remarks>
    /// The library reads the file on FIRST USE and refuses to be pointed somewhere else afterwards,
    /// so this happens once and never again - which also means the whole suite pays for reading the
    /// SoundFont exactly once, however many tests ask for a sound.
    /// </remarks>
    internal static void PointTheLibraryAtIt()
    {
        lock (Gate)
        {
            if (pointed)
            {
                return;
            }

            FluidR3GmInstrumentLibrary.UseSoundFontAt(FullPath);
            pointed = true;
        }
    }

    /// <summary>
    /// Walks up from the test assembly until the repository root appears.
    /// </summary>
    /// <returns>The path of the staged SoundFont, or <see langword="null"/> when there is no root above.</returns>
    private static string Locate()
    {
        DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, RepositoryMarker)))
            {
                return Path.Combine(
                    directory.FullName, "staging", "output", FluidR3GmInstrumentLibrary.SoundFontFileName);
            }

            directory = directory.Parent;
        }

        return null;
    }
}
