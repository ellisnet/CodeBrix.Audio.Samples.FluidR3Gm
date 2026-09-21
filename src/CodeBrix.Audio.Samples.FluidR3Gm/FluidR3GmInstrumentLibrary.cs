using System;
using System.IO;
using CodeBrix.Audio.Instruments;
using CodeBrix.Audio.Midi;
using CodeBrix.Audio.Samples.FluidR3Gm.Internal;
using CodeBrix.Audio.Synth;

namespace CodeBrix.Audio.Samples.FluidR3Gm;

/// <summary>
/// The FluidR3_GM General MIDI sound set - recorded instruments rather than synthesized ones -
/// offered to <see cref="InstrumentLibraryRegistry"/> under the name <see cref="LibraryName"/>.
/// </summary>
/// <remarks>
/// <para>
/// REGISTER IT YOURSELF: <c>FluidR3GmInstrumentLibrary.Register();</c>. CodeBrix.Audio ships no
/// instruments and registers nothing, so with an empty registry nothing plays and nothing renders.
/// The call is idempotent and safe from any thread, and there is deliberately no module initializer
/// doing it for you - a module initializer only runs once something in the assembly is touched,
/// which trimming and lazy assembly loading make unreliable.
/// </para>
/// <para>
/// ONE LINE CHANGES THE WHOLE SOUND OF AN APPLICATION. This library covers the same surface as the
/// synthesized General MIDI library in CodeBrix.Audio.ModestSynth - every General MIDI program and
/// the percussion kit - so an application that asks for its instruments by name swaps recordings for
/// synthesis, or the other way round, by naming the other library. Recordings are warmer on the
/// acoustic instruments synthesis finds hardest, a solo violin and a concert piano above all.
/// </para>
/// <para>
/// REGISTERING LOADS NOTHING. The SoundFont is a large file of recorded audio, and it is read on
/// FIRST USE rather than at <see cref="Register"/>: an application that registers this library and
/// then plays through another one never pays for it. The first call that needs a sound - creating a
/// synthesizer, or asking what <see cref="Coverage"/> holds - is where the time and the memory go.
/// <see cref="IsLoaded"/> says whether that has happened, and there is no way to unload it again:
/// every part of every arrangement shares the one SoundFont, so dropping it would silence music
/// that is still playing.
/// </para>
/// <para>
/// ONE SOUNDFONT, SHARED. The file is parsed once and every synthesizer this library hands back
/// renders from that one <see cref="CodeBrix.Audio.Synth.SoundFont"/> - twelve parts of an
/// arrangement cost twelve voice engines and ONE copy of the sample data. That is what makes a
/// sample library of this size
/// usable at all, and it is why this type is a thin wrapper over CodeBrix.Audio's own
/// <see cref="SoundFontInstrumentLibrary"/> rather than an implementation of its own.
/// </para>
/// <para>
/// WHERE THE FILE IS. The package lands <see cref="SoundFontFileName"/> in the output folder of
/// every project that references it, including an application that reaches the package only through
/// a library of its own, so the default - beside the running application - is normally right.
/// <see cref="UseSoundFontAt"/> points the library somewhere else for an application that keeps its
/// assets in one shared folder rather than a copy per executable.
/// </para>
/// <para>
/// BOTH SHAPES ARE OFFERED. <see cref="CreateSynthesizer"/> hands back a synthesizer PINNED to one
/// program - the per-part road, where the caller decided what each part sounds like and a program
/// change in the music must not undo that - and <see cref="CreateMultiTimbralSynthesizer"/> hands
/// back one synthesizer that honours program change inline, which is the "play this .mid with no
/// configuration" road. <see cref="CreatePercussionSynthesizer"/> is the kit, sounding on whatever
/// channel a router puts it on rather than only on channel 10.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// FluidR3GmInstrumentLibrary.Register();
///
/// var library = InstrumentLibraryRegistry.Resolve(FluidR3GmInstrumentLibrary.LibraryName);
/// var cello = library.CreateSynthesizer((int)GeneralMidiProgram.Cello, 44100);
/// var kit = library.CreatePercussionSynthesizer(44100);
/// var whole = library.CreateMultiTimbralSynthesizer(44100);
/// </code>
/// </example>
public sealed class FluidR3GmInstrumentLibrary : IInstrumentLibrary
{
    /// <summary>The name this library registers under, matched case-insensitively.</summary>
    public const string LibraryName = "FluidR3Gm";

    /// <summary>The SoundFont file this library plays, under the name the package ships it as.</summary>
    public const string SoundFontFileName = "FluidR3_GM.sf2";

    /// <summary>
    /// The one lock. It guards the chosen path, the loaded SoundFont and the registration flag
    /// together, because they are one piece of state: the path may only change while nothing is
    /// loaded. Loading happens inside it on purpose - callers racing for the first sound should wait
    /// for one read of the file rather than each start one of their own.
    /// </summary>
    private static readonly object Gate = new object();

    private static readonly FluidR3GmInstrumentLibrary Shared = new FluidR3GmInstrumentLibrary();

    private static string requestedSoundFontPath;

    private static SoundFontInstrumentLibrary loadedLibrary;

    private static bool registered;

    private FluidR3GmInstrumentLibrary()
    {
    }

    /// <summary>
    /// The one shared library instance. It is a single instance on purpose: the registry treats the
    /// same instance registered twice as a no-op but two different instances under one name as an
    /// error, so this is what makes <see cref="Register"/> idempotent for free.
    /// </summary>
    public static FluidR3GmInstrumentLibrary Instance => Shared;

    /// <summary>Whether <see cref="Register"/> has run.</summary>
    public static bool IsRegistered
    {
        get { lock (Gate) { return registered; } }
    }

    /// <summary>
    /// The full path this library reads the SoundFont from: beside the running application under
    /// <see cref="SoundFontFileName"/>, unless <see cref="UseSoundFontAt"/> named another file.
    /// </summary>
    public static string SoundFontPath
    {
        get
        {
            lock (Gate)
            {
                return requestedSoundFontPath ?? SoundFontLocation.BesideTheApplication(SoundFontFileName);
            }
        }
    }

    /// <summary>
    /// Whether the SoundFont is where <see cref="SoundFontPath"/> says it is - so an application can
    /// find out before the first note instead of at the first note.
    /// </summary>
    public static bool IsSoundFontAvailable => File.Exists(SoundFontPath);

    /// <summary>
    /// Whether the SoundFont has been read yet. It is read on first use, so this is false after
    /// <see cref="Register"/> and true after the first sound.
    /// </summary>
    public static bool IsLoaded
    {
        get { lock (Gate) { return loadedLibrary != null; } }
    }

    /// <inheritdoc />
    public string Name => LibraryName;

    /// <inheritdoc />
    public string Description =>
        "The FluidR3_GM General MIDI sound set - every General MIDI program and the percussion kit " +
        "played from recorded instruments rather than synthesized, which is warmer on the acoustic " +
        "instruments and costs time and memory to load.";

    /// <inheritdoc />
    /// <remarks>
    /// READING THIS LOADS THE SOUNDFONT, because the answer is read from the file rather than
    /// claimed: the programs reported are the presets the file really holds, each with the note
    /// range its regions really answer to, and the percussion notes are the ones its drum bank
    /// really holds.
    /// </remarks>
    /// <exception cref="FileNotFoundException">
    /// The SoundFont is not at <see cref="SoundFontPath"/>. The message says what to do about it.
    /// </exception>
    public InstrumentCoverage Coverage => Loaded.Coverage;

    /// <inheritdoc />
    public bool SupportsPerPart => true;

    /// <inheritdoc />
    public bool SupportsMultiTimbral => true;

    /// <summary>
    /// The one loaded SoundFont every synthesizer this library creates renders from.
    /// </summary>
    /// <remarks>Reading it loads the SoundFont if nothing has loaded it yet.</remarks>
    /// <exception cref="FileNotFoundException">
    /// The SoundFont is not at <see cref="SoundFontPath"/>. The message says what to do about it.
    /// </exception>
    public SoundFont SoundFont => Loaded.SoundFont;

    /// <summary>
    /// Registers this library with <see cref="InstrumentLibraryRegistry"/> under
    /// <see cref="LibraryName"/>.
    /// </summary>
    /// <remarks>
    /// Idempotent and safe to call from any thread; calling it more than once does nothing. IT LOADS
    /// NOTHING - the SoundFont is read on first use - so registering this library beside another one
    /// and then playing through the other costs nothing at all. The FIRST library registered in a
    /// process becomes the registry's default, so an application that registers more than one should
    /// ask for the one it wants by name.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A different instrument library is already registered under <see cref="LibraryName"/>.
    /// </exception>
    public static void Register()
    {
        lock (Gate)
        {
            if (registered) { return; }

            InstrumentLibraryRegistry.Register(Shared);
            registered = true;
        }
    }

    /// <summary>
    /// Points this library at a copy of the SoundFont somewhere other than beside the application.
    /// </summary>
    /// <param name="soundFontPath">
    /// The full path of the <c>.sf2</c> file to play. It does not have to exist yet - the file is
    /// read on first use - but it does have to exist by then.
    /// </param>
    /// <remarks>
    /// FOR AN APPLICATION THAT KEEPS ITS ASSETS IN ONE PLACE. The SoundFont is a large file, and an
    /// application built from several projects otherwise ends up with a copy in each of their output
    /// folders; set <c>CodeBrixFluidR3GmCopyAssetsToOutput</c> to <c>false</c> in the build, put the
    /// file wherever the application keeps its assets, and name it here at start-up.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="soundFontPath"/> is null or blank.</exception>
    /// <exception cref="InvalidOperationException">
    /// The SoundFont has already been read. Every part of every arrangement shares the one loaded
    /// SoundFont, so changing the file underneath them is not something this library pretends to do.
    /// </exception>
    public static void UseSoundFontAt(string soundFontPath)
    {
        if (string.IsNullOrWhiteSpace(soundFontPath))
        {
            throw new ArgumentException(
                "A path to the SoundFont file is required.", nameof(soundFontPath));
        }

        lock (Gate)
        {
            if (loadedLibrary != null)
            {
                throw new InvalidOperationException(
                    "The FluidR3Gm SoundFont has already been read from '" + SoundFontPathLocked() +
                    "', and every synthesizer this library has created is playing from it, so the " +
                    "file cannot be changed now. Call UseSoundFontAt at start-up, before the first " +
                    "sound is asked for - IsLoaded says whether that moment has passed.");
            }

            requestedSoundFontPath = Path.GetFullPath(soundFontPath);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The synthesizer is PINNED: it plays that one program on every one of the sixteen channels and
    /// ignores program change and bank select, because the caller - not the music - decided what
    /// this part sounds like. One sharp edge comes from the SoundFont format itself: MIDI channel 10
    /// is hard-wired to the drum bank, so a melodic part routed onto that channel sounds as
    /// percussion whatever program it was created with.
    /// </remarks>
    /// <exception cref="FileNotFoundException">
    /// The SoundFont is not at <see cref="SoundFontPath"/>. The message says what to do about it.
    /// </exception>
    public IMidiSynthesizer CreateSynthesizer(int program, int sampleRate)
    {
        //The arguments are checked BEFORE the SoundFont is read, so a mistyped program number costs
        //an exception rather than a large file read that is thrown away.
        if (program < 0 || program >= GeneralMidi.ProgramCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(program), program, "A General MIDI program number is 0 to 127.");
        }

        CheckSampleRate(sampleRate);

        return Loaded.CreateSynthesizer(program, sampleRate);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The kit sounds on ANY channel this synthesizer is given, not only on channel 10, because a
    /// router forwards whatever channel the music used and a rendition may put the drums elsewhere.
    /// </remarks>
    /// <exception cref="FileNotFoundException">
    /// The SoundFont is not at <see cref="SoundFontPath"/>. The message says what to do about it.
    /// </exception>
    public IMidiSynthesizer CreatePercussionSynthesizer(int sampleRate)
    {
        CheckSampleRate(sampleRate);

        return Loaded.CreatePercussionSynthesizer(sampleRate);
    }

    /// <inheritdoc />
    /// <remarks>
    /// This one honours program change on every channel and plays the kit on
    /// <see cref="GeneralMidi.PercussionChannel"/>, which is what a General MIDI file expects.
    /// </remarks>
    /// <exception cref="FileNotFoundException">
    /// The SoundFont is not at <see cref="SoundFontPath"/>. The message says what to do about it.
    /// </exception>
    public IMidiSynthesizer CreateMultiTimbralSynthesizer(int sampleRate)
    {
        CheckSampleRate(sampleRate);

        return Loaded.CreateMultiTimbralSynthesizer(sampleRate);
    }

    /// <summary>
    /// The wrapped library, reading the SoundFont the first time anything asks for it.
    /// </summary>
    private static SoundFontInstrumentLibrary Loaded
    {
        get
        {
            lock (Gate)
            {
                if (loadedLibrary != null)
                {
                    return loadedLibrary;
                }

                string path = SoundFontPathLocked();

                if (!File.Exists(path))
                {
                    throw SoundFontLocation.NotFound(path, SoundFontFileName);
                }

                //CodeBrix.Audio's own library does the work: it parses the file once, keeps ONE
                //SoundFont, and hands out synthesizers that all render from it. There is nothing for
                //this package to add beyond a name, a description and where the file is.
                loadedLibrary = new SoundFontInstrumentLibrary(LibraryName, Shared.Description, path);
                return loadedLibrary;
            }
        }
    }

    /// <summary>
    /// The chosen path, for a caller that already holds <see cref="Gate"/>.
    /// </summary>
    /// <returns>The full path of the SoundFont file.</returns>
    private static string SoundFontPathLocked()
    {
        return requestedSoundFontPath ?? SoundFontLocation.BesideTheApplication(SoundFontFileName);
    }

    /// <summary>
    /// Refuses a sample rate that cannot be rendered at.
    /// </summary>
    /// <param name="sampleRate">The rate to check.</param>
    /// <exception cref="ArgumentOutOfRangeException">The rate is not positive.</exception>
    private static void CheckSampleRate(int sampleRate)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampleRate), sampleRate, "The sample rate must be positive.");
        }
    }
}
