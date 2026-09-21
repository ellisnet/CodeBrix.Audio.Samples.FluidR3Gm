using Xunit;

namespace CodeBrix.Audio.Samples.FluidR3Gm.Tests;

/// <summary>
/// Serialises every test class that touches the process-wide <c>FluidR3GmInstrumentLibrary</c>
/// singleton.
/// </summary>
/// <remarks>
/// <para>
/// THE LIBRARY IS ONE PIECE OF PROCESS-WIDE STATE: the chosen SoundFont path, the loaded SoundFont
/// and the registration flag. A test that observes "nothing has been loaded yet" and a test that
/// asks for a synthesizer - which is what loads the file - cannot run at the same time, because the
/// second would decide the first one's answer. So can a test that points the library at a file and
/// one that asserts the pointing is refused once the file has been read. xUnit runs different
/// collections in parallel by default, and putting every class that touches the singleton in one
/// collection with parallelization off is how that is said.
/// </para>
/// <para>
/// This is NOT about the instrument registry's default. Nothing in this assembly reads or sets it:
/// which library is the default depends on which registration ran first anywhere in the process,
/// so every lookup here is by name.
/// </para>
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public class FluidR3GmLibraryCollection
{
    /// <summary>The collection's name.</summary>
    public const string Name = "FluidR3Gm instrument library singleton";
}
