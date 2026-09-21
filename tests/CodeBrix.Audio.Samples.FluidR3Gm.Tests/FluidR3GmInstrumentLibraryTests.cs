using System;
using System.Linq;
using CodeBrix.Audio.Instruments;
using CodeBrix.Audio.Midi;
using CodeBrix.Audio.Synth;
using SilverAssertions;
using SilverAssertions.Collections;
using SilverAssertions.Numeric;
using SilverAssertions.Primitives;
using SilverAssertions.Specialized;
using Xunit;

namespace CodeBrix.Audio.Samples.FluidR3Gm.Tests;

/// <summary>
/// Covers <see cref="FluidR3GmInstrumentLibrary"/> against the SoundFont the asset stager put in
/// <c>staging/output</c>.
/// </summary>
/// <remarks>
/// <para>
/// EVERY TEST THAT NEEDS A SOUND goes through <see cref="Library"/>, which skips the test with a
/// clear message when the SoundFont has not been staged and otherwise points the library at it. The
/// tests that only check an argument do NOT go through it, on purpose: the library checks its
/// arguments before it reads anything, and those tests are the fence that keeps it that way.
/// </para>
/// <para>
/// NOTHING HERE READS OR SETS THE REGISTRY'S DEFAULT. Which library is the default depends on which
/// registration ran first, which is a property of the whole test assembly rather than of any one
/// test, so every lookup here is by name.
/// </para>
/// </remarks>
[Collection(FluidR3GmLibraryCollection.Name)]
public class FluidR3GmInstrumentLibraryTests
{
    private const int Rate = 44100;

    private const int TicksPerQuarterNote = 480;

    // ----- identity -----

    [Fact]
    public void LibraryName_is_the_name_the_registry_knows_this_library_by()
        => FluidR3GmInstrumentLibrary.LibraryName.Should().Be("FluidR3Gm");

    [Fact]
    public void SoundFontFileName_is_the_name_the_package_ships_the_file_under()
        => FluidR3GmInstrumentLibrary.SoundFontFileName.Should().Be("FluidR3_GM.sf2");

    [Fact]
    public void Name_is_the_name_this_library_registers_under()
        => FluidR3GmInstrumentLibrary.Instance.Name.Should().Be(FluidR3GmInstrumentLibrary.LibraryName);

    [Fact]
    public void Description_says_the_instruments_are_recorded_rather_than_synthesized()
        => FluidR3GmInstrumentLibrary.Instance.Description.Should().Contain("recorded");

    [Fact]
    public void Instance_is_one_shared_instance()
        => FluidR3GmInstrumentLibrary.Instance.Should().BeSameAs(FluidR3GmInstrumentLibrary.Instance);

    [Fact]
    public void SupportsPerPart_is_true_because_the_library_voices_one_part_at_a_time()
        => FluidR3GmInstrumentLibrary.Instance.SupportsPerPart.Should().BeTrue();

    [Fact]
    public void SupportsMultiTimbral_is_true_because_the_library_plays_a_whole_piece_on_its_own()
        => FluidR3GmInstrumentLibrary.Instance.SupportsMultiTimbral.Should().BeTrue();

    // ----- registration -----

    [Fact]
    public void Register_puts_the_library_into_the_registry_under_its_own_name()
    {
        //Act
        FluidR3GmInstrumentLibrary.Register();

        //Assert
        InstrumentLibraryRegistry.Resolve(FluidR3GmInstrumentLibrary.LibraryName)
            .Should().BeSameAs(FluidR3GmInstrumentLibrary.Instance);
        FluidR3GmInstrumentLibrary.IsRegistered.Should().BeTrue();
    }

    [Fact]
    public void Register_is_idempotent()
    {
        //Arrange
        FluidR3GmInstrumentLibrary.Register();

        //Act
        FluidR3GmInstrumentLibrary.Register();
        FluidR3GmInstrumentLibrary.Register();

        //Assert
        InstrumentLibraryRegistry.RegisteredNames
            .Count(name => string.Equals(
                name, FluidR3GmInstrumentLibrary.LibraryName, StringComparison.OrdinalIgnoreCase))
            .Should().Be(1);
    }

    [Fact]
    public void Register_resolves_by_name_whatever_case_the_name_is_asked_for_in()
    {
        //Arrange
        FluidR3GmInstrumentLibrary.Register();

        //Act
        var resolved = InstrumentLibraryRegistry.Resolve("fluidr3gm");

        //Assert
        resolved.Should().BeSameAs(FluidR3GmInstrumentLibrary.Instance);
    }

    [Fact]
    public void Register_reads_nothing_until_a_sound_is_asked_for()
    {
        //Arrange
        Assert.SkipWhen(
            FluidR3GmInstrumentLibrary.IsLoaded,
            "another test in this assembly has already asked for a sound, and loading happens once "
                + "per process - so there is nothing left to observe here.");

        //Act
        FluidR3GmInstrumentLibrary.Register();

        //Assert
        FluidR3GmInstrumentLibrary.IsLoaded.Should().BeFalse();
    }

    // ----- where the file is -----

    [Fact]
    public void SoundFontPath_reports_the_file_the_library_was_pointed_at()
    {
        //Arrange
        Library();

        //Assert
        FluidR3GmInstrumentLibrary.SoundFontPath.Should().Be(StagedSoundFont.FullPath);
    }

    [Fact]
    public void IsSoundFontAvailable_is_true_once_the_library_is_pointed_at_a_staged_soundfont()
    {
        //Arrange
        Library();

        //Assert
        FluidR3GmInstrumentLibrary.IsSoundFontAvailable.Should().BeTrue();
    }

    [Fact]
    public void UseSoundFontAt_refuses_a_path_that_is_not_a_path()
    {
        //Act
        Action pointAtNothing = () => FluidR3GmInstrumentLibrary.UseSoundFontAt("   ");

        //Assert
        pointAtNothing.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UseSoundFontAt_refuses_to_change_the_file_once_it_has_been_read()
    {
        //Arrange - asking for a synthesizer is what reads the file
        Library().CreateMultiTimbralSynthesizer(Rate);

        //Act
        Action pointSomewhereElse = () =>
            FluidR3GmInstrumentLibrary.UseSoundFontAt("/somewhere/else/FluidR3_GM.sf2");

        //Assert
        pointSomewhereElse.Should().Throw<InvalidOperationException>();
    }

    // ----- coverage -----

    [Fact]
    public void Coverage_holds_every_general_midi_program()
    {
        //Arrange
        var coverage = Library().Coverage;

        //Assert
        coverage.Programs.Count.Should().Be(GeneralMidi.ProgramCount);
        Enumerable.Range(0, GeneralMidi.ProgramCount)
            .Where(program => !coverage.CoversProgram(program))
            .ToArray()
            .Should().BeEmpty();
    }

    [Fact]
    public void Coverage_holds_the_whole_general_midi_percussion_kit()
    {
        //Arrange
        var coverage = Library().Coverage;

        //Assert
        Enumerable
            .Range(
                GeneralMidi.LowestPercussionNote,
                GeneralMidi.HighestPercussionNote - GeneralMidi.LowestPercussionNote + 1)
            .Where(note => !coverage.CoversPercussionNote(note))
            .ToArray()
            .Should().BeEmpty();
    }

    // ----- both shapes, and the one shared SoundFont -----

    [Fact]
    public void CreateSynthesizer_hands_back_a_synthesizer_for_every_general_midi_program()
    {
        //Arrange
        var library = Library();

        //Act
        var synthesizers = Enumerable
            .Range(0, GeneralMidi.ProgramCount)
            .Select(program => library.CreateSynthesizer(program, Rate))
            .ToArray();

        //Assert
        synthesizers.Should().NotContainNulls();
        synthesizers.Length.Should().Be(GeneralMidi.ProgramCount);
    }

    [Fact]
    public void CreatePercussionSynthesizer_hands_back_the_kit()
        => Library().CreatePercussionSynthesizer(Rate).Should().NotBeNull();

    [Fact]
    public void CreateMultiTimbralSynthesizer_hands_back_one_synthesizer_for_a_whole_piece()
        => Library().CreateMultiTimbralSynthesizer(Rate).Should().NotBeNull();

    [Fact]
    public void SoundFont_is_the_one_shared_soundfont_every_part_renders_from()
    {
        //Arrange
        var library = Library();
        var soundFont = FluidR3GmInstrumentLibrary.Instance.SoundFont;

        //Act - twelve parts of an arrangement, which must not mean twelve copies of the file
        for (var part = 0; part < 12; part++)
        {
            library.CreateSynthesizer(part, Rate);
        }

        library.CreatePercussionSynthesizer(Rate);
        library.CreateMultiTimbralSynthesizer(Rate);

        //Assert
        FluidR3GmInstrumentLibrary.Instance.SoundFont.Should().BeSameAs(soundFont);
    }

    // ----- arguments are checked before anything is read -----

    [Theory]
    [InlineData(-1)]
    [InlineData(GeneralMidi.ProgramCount)]
    public void CreateSynthesizer_refuses_a_program_that_is_not_a_general_midi_program(int program)
    {
        //Act
        Action create = () => FluidR3GmInstrumentLibrary.Instance.CreateSynthesizer(program, Rate);

        //Assert
        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateSynthesizer_refuses_a_sample_rate_that_is_not_positive()
    {
        //Act
        Action create = () => FluidR3GmInstrumentLibrary.Instance.CreateSynthesizer(0, 0);

        //Assert
        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreatePercussionSynthesizer_refuses_a_sample_rate_that_is_not_positive()
    {
        //Act
        Action create = () => FluidR3GmInstrumentLibrary.Instance.CreatePercussionSynthesizer(-1);

        //Assert
        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateMultiTimbralSynthesizer_refuses_a_sample_rate_that_is_not_positive()
    {
        //Act
        Action create = () => FluidR3GmInstrumentLibrary.Instance.CreateMultiTimbralSynthesizer(-1);

        //Assert
        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ----- it really makes a sound -----

    [Theory]
    [InlineData((int)GeneralMidiProgram.AcousticGrandPiano)]
    [InlineData((int)GeneralMidiProgram.Celesta)]
    [InlineData((int)GeneralMidiProgram.Vibraphone)]
    [InlineData((int)GeneralMidiProgram.Cello)]
    [InlineData((int)GeneralMidiProgram.OrchestralHarp)]
    [InlineData((int)GeneralMidiProgram.ChoirAahs)]
    [InlineData((int)GeneralMidiProgram.Flute)]
    public void CreateSynthesizer_renders_a_non_silent_phrase(int program)
    {
        //Arrange
        var synthesizer = Library().CreateSynthesizer(program, Rate);

        //Act
        var samples = SoundFontRenderer.Render(synthesizer, Phrase(1, 60, 64, 67, 72));

        //Assert
        samples.Max(Math.Abs).Should().BeGreaterThan(0.0001f);
    }

    [Fact]
    public void CreatePercussionSynthesizer_renders_a_non_silent_phrase()
    {
        //Arrange
        var synthesizer = Library().CreatePercussionSynthesizer(Rate);

        //Act - kick, snare, closed hi-hat, snare
        var samples = SoundFontRenderer.Render(
            synthesizer, Phrase(GeneralMidi.PercussionChannel, 36, 38, 42, 38));

        //Assert
        samples.Max(Math.Abs).Should().BeGreaterThan(0.0001f);
    }

    [Fact]
    public void CreateMultiTimbralSynthesizer_renders_a_non_silent_phrase()
    {
        //Arrange
        var synthesizer = Library().CreateMultiTimbralSynthesizer(Rate);

        //Act
        var samples = SoundFontRenderer.Render(synthesizer, Phrase(1, 60, 64, 67, 72));

        //Assert
        samples.Max(Math.Abs).Should().BeGreaterThan(0.0001f);
    }

    /// <summary>
    /// The library, with the staged SoundFont behind it. Skips the test when nothing has been staged.
    /// </summary>
    /// <returns>The one shared library instance.</returns>
    private static IInstrumentLibrary Library()
    {
        Assert.SkipUnless(StagedSoundFont.IsStaged, StagedSoundFont.NotStagedMessage);
        StagedSoundFont.PointTheLibraryAtIt();
        return FluidR3GmInstrumentLibrary.Instance;
    }

    /// <summary>
    /// A short phrase: one note per beat, each held for its beat.
    /// </summary>
    /// <param name="channel">The MIDI channel to play on, counting from one.</param>
    /// <param name="notes">The note numbers, in order.</param>
    /// <returns>The phrase as a sequence a renderer can play.</returns>
    private static MidiSequence Phrase(int channel, params int[] notes)
    {
        var collection = new MidiEventCollection(1, TicksPerQuarterNote);
        long time = 0;

        foreach (var note in notes)
        {
            collection.AddEvent(
                new NoteOnEvent(time, channel, note, 100, TicksPerQuarterNote), 1);
            collection.AddEvent(
                new NoteEvent(
                    time + TicksPerQuarterNote, channel, MidiCommandCode.NoteOff, note, 0), 1);
            time += TicksPerQuarterNote;
        }

        collection.PrepareForExport();
        return MidiSequence.FromEvents(collection);
    }
}
