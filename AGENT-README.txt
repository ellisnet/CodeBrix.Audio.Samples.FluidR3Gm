================================================================================
AGENT-README: CodeBrix.Audio.Samples.FluidR3Gm
A Guide for AI Coding Agents - CONSUMING the
CodeBrix.Audio.Samples.FluidR3Gm.MitLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Audio.Samples.FluidR3Gm gives a .NET 10 application RECORDED General
MIDI instruments. It carries the FluidR3_GM SoundFont - every General MIDI
program and the General MIDI percussion kit, played from recordings of real
instruments - and offers it to CodeBrix.Audio's instrument registry under the
name "FluidR3Gm".

ONE LINE CHANGES THE WHOLE SOUND OF AN APPLICATION. CodeBrix.Audio addresses
instruments the way MIDI does - by General MIDI program number, and by note
number on the percussion channel - and an instrument library is asked for BY
NAME. Two libraries cover that whole surface: the synthesized one in
CodeBrix.Audio.ModestSynth, registered as "ModestSynthGm", and this recorded
one, registered as "FluidR3Gm". An application that names the library it wants
swaps between them by changing the name, and nothing else about the application
changes.

WHAT IT SOUNDS LIKE. Recordings, with the strengths and the costs of
recordings. The acoustic instruments that synthesis finds hardest - a solo
violin, a concert piano, an orchestral harp, a cello - are where this library is
clearly better. It also costs real load time and real memory, where the
synthesized bank costs neither. Both are worth trying on the same music; they
are alternatives, not layers, and neither is "the good one".

THE SOUNDFONT ARRIVES WITH THE PACKAGE and is landed in the output folder of
every project that references it, including an application that reaches this
package only through a library of its own. Nothing has to be downloaded and
nothing has to be configured.


INSTALLATION
============
PackageId:  CodeBrix.Audio.Samples.FluidR3Gm.MitLicenseForever

    dotnet add package CodeBrix.Audio.Samples.FluidR3Gm.MitLicenseForever

The package ID and the namespace are different - there is no package named
plain CodeBrix.Audio.Samples.FluidR3Gm:

    NuGet package ID:  CodeBrix.Audio.Samples.FluidR3Gm.MitLicenseForever
    Assembly:          CodeBrix.Audio.Samples.FluidR3Gm
    Namespace:         CodeBrix.Audio.Samples.FluidR3Gm

DEPENDENCIES: CodeBrix.Audio.MitLicenseForever, and nothing else. It arrives
automatically; the consuming project does not pin it.

It does NOT depend on CodeBrix.Audio.ModestSynth. The synthesized General MIDI
library and this recorded one are alternatives to each other - an application
that wants both registers both packages.

LICENCE: MIT, for the library and for the SoundFont alike. The author's own
licence text and release statement ship in the package and are placed beside the
SoundFont in the consuming application's output folder, because the notice
travels with the samples. THIRD-PARTY-NOTICES.txt in the package has both texts
in full.

REQUIREMENTS: .NET 10 or later. No native library, no platform-specific asset,
no service, no network access at any point.

DISK AND DEPLOYMENT: the SoundFont is a large file and it is copied into the
output folder of every project that reaches the package. An application built
from several projects therefore keeps several copies unless it says otherwise -
see "KEEPING THE SOUNDFONT SOMEWHERE ELSE" below, which is one build property
and one line of code.


KEY NAMESPACES / USINGS
=======================
    using CodeBrix.Audio.Samples.FluidR3Gm;   // FluidR3GmInstrumentLibrary
    using CodeBrix.Audio.Instruments;         // the registry and the interface
    using CodeBrix.Audio.Midi;                // GeneralMidiProgram, GeneralMidi
    using CodeBrix.Audio.Synth;               // IMidiSynthesizer, rendering

This package adds exactly one public type, in one namespace. Everything it is
used with - the registry, the interface, the synthesizer, the renderer - belongs
to CodeBrix.Audio.


CORE API REFERENCE
==================
FluidR3GmInstrumentLibrary : IInstrumentLibrary
    A sealed class with a private constructor: there is ONE instance, and
    Instance is it. That is what makes registration idempotent for free.

CONSTANTS
    const string LibraryName        "FluidR3Gm" - the name it registers under.
    const string SoundFontFileName  the SoundFont's own file name.

STATIC MEMBERS
    static FluidR3GmInstrumentLibrary Instance
        The one shared instance.

    static void Register()
        Registers the library with InstrumentLibraryRegistry under LibraryName.
        Idempotent, safe from any thread, and it LOADS NOTHING.
        Throws InvalidOperationException if a DIFFERENT library is already
        registered under that name.

    static bool IsRegistered
        Whether Register() has run.

    static string SoundFontPath
        Where the SoundFont is read from: beside the running application, under
        SoundFontFileName, unless UseSoundFontAt named another file.

    static bool IsSoundFontAvailable
        Whether the file is actually there - so an application can find out at
        start-up instead of at the first note.

    static bool IsLoaded
        Whether the SoundFont has been read yet. False after Register(); true
        after the first sound is asked for.

    static void UseSoundFontAt(string soundFontPath)
        Points the library at a copy of the SoundFont somewhere else. Call it
        BEFORE the first sound.
        Throws ArgumentException for a blank path, and InvalidOperationException
        once the SoundFont has been read.

INSTANCE MEMBERS (the IInstrumentLibrary surface)
    string Name                      "FluidR3Gm".
    string Description               one sentence, for a user interface.
    InstrumentCoverage Coverage      what the library really plays. READING IT
                                     LOADS THE SOUNDFONT, because the answer is
                                     read out of the file rather than claimed.
    bool SupportsPerPart             true.
    bool SupportsMultiTimbral        true.
    SoundFont SoundFont              the one loaded SoundFont every synthesizer
                                     renders from.

    IMidiSynthesizer CreateSynthesizer(int program, int sampleRate)
        One part of an arrangement, PINNED to that program: a program change
        arriving in the music does not re-voice a part you voiced deliberately.
        Throws ArgumentOutOfRangeException outside program 0 to 127 or for a
        sample rate that is not positive - both checked BEFORE anything is read.

    IMidiSynthesizer CreatePercussionSynthesizer(int sampleRate)
        The kit. It sounds on whatever channel it is given, not only on
        channel 10, so a router may put the drums anywhere.

    IMidiSynthesizer CreateMultiTimbralSynthesizer(int sampleRate)
        ONE synthesizer that plays a whole piece, honouring the program changes
        the music carries and treating channel 10 as percussion. This is the
        "play this .mid with no configuration" road.

Every one of the three throws FileNotFoundException when the SoundFont is not at
SoundFontPath, with a message that names what usually causes it.


REGISTERING, AND WHICH LIBRARY IS THE DEFAULT
=============================================
CodeBrix.Audio SHIPS NO INSTRUMENTS AND REGISTERS NOTHING. With an empty
registry nothing plays and nothing renders, and the exception says so. Filling
it is always the consuming application's job:

    FluidR3GmInstrumentLibrary.Register();

THE RULES, which belong to CodeBrix.Audio's registry rather than to this
package:

  * Names are unique and matched CASE-INSENSITIVELY. "fluidr3gm" resolves.
  * Registering the SAME library again is a NO-OP, so Register() may be called
    on every start-up path without tracking whether it ran. Registering a
    DIFFERENT library under a name that is taken is an error.
  * THE FIRST LIBRARY REGISTERED IS THE DEFAULT. There is no priority and no
    other ordering. InstrumentLibraryRegistry.SetDefault(name) changes it by
    name afterwards.
  * ASK FOR A LIBRARY BY NAME when it matters. Which library is the default
    depends on which registration ran first, which is a fact about start-up
    order rather than about intent:

        var library = InstrumentLibraryRegistry.Resolve("FluidR3Gm");

  * Asking for a name that is not registered is an error that lists what is.

An application that registers both libraries and wants the recorded one to be
the default either registers this one FIRST, or says so explicitly:

    FluidR3GmInstrumentLibrary.Register();      // first, so it is the default
    GeneralMidiInstrumentLibrary.Register();    // CodeBrix.Audio.ModestSynth

    // or, whatever the order was:
    InstrumentLibraryRegistry.SetDefault("FluidR3Gm");


WHAT IT COVERS
==============
Every General MIDI program, and the General MIDI percussion kit. That is the
point of choosing this SoundFont: it covers exactly the same surface as the
synthesized library, so naming the other one is a complete substitution rather
than a partial one with holes in it.

The kit is in fact LARGER than General MIDI asks for - the file carries drum
notes above and below the range the specification pins down. Ask Coverage
rather than assuming either way, and an arrangement that only uses General MIDI
kit notes is safe whichever library plays it.

The Coverage report is READ FROM THE FILE, not claimed:

    var coverage = InstrumentLibraryRegistry.Resolve("FluidR3Gm").Coverage;

    coverage.Programs                     every program the file really holds
    coverage.PercussionNotes              every kit note it really holds
    coverage.CoversProgram(program)
    coverage.CoversPercussionNote(note)
    coverage.KeyRangeOf(program)          the notes that program answers to
    coverage.CoversNote(program, note)

Reading Coverage loads the SoundFont. An application that only wants to know
whether the library is available should read IsSoundFontAvailable instead, which
touches nothing.


LOADING IS LAZY, AND HAPPENS ONCE
=================================
Register() reads nothing. The first call that needs a sound - creating a
synthesizer, or reading Coverage - reads the file, and after that every
synthesizer this library ever hands back renders from that one SoundFont.

    FluidR3GmInstrumentLibrary.Register();
    FluidR3GmInstrumentLibrary.IsLoaded;        // false - nothing read yet

    var library = InstrumentLibraryRegistry.Resolve("FluidR3Gm");
    var flute = library.CreateSynthesizer((int)GeneralMidiProgram.Flute, 44100);
    FluidR3GmInstrumentLibrary.IsLoaded;        // true

WHY IT MATTERS: an application may register both instrument libraries at
start-up and let the player choose. Registering this one costs nothing until
the player actually chooses it.

ONE SOUNDFONT, SHARED. Twelve parts of an arrangement cost twelve voice engines
and ONE copy of the sample data. That is what makes a sample library this large
usable at all.

THERE IS NO WAY TO UNLOAD IT. Every part of every arrangement shares the one
SoundFont, so dropping it would silence music that is still playing. An
application that must reclaim the memory has to end the process.

TO TAKE THE DELAY WHEN IT SUITS YOU - a loading screen, a splash, a menu -
touch the library there:

    _ = InstrumentLibraryRegistry.Resolve("FluidR3Gm").Coverage;


THE TWO SHAPES
==============
PER PART - one synthesizer per voice. This is the shape an arrangement needs,
because a per-voice gain and a layered second instrument both REQUIRE separate
synthesizers: one multi-timbral synthesizer mixes internally at one level. The
parts are then played together through CodeBrix.Audio's routing synthesizer.

    var strings = library.CreateSynthesizer((int)GeneralMidiProgram.StringEnsemble1, 44100);
    var harp = library.CreateSynthesizer((int)GeneralMidiProgram.OrchestralHarp, 44100);
    var kit = library.CreatePercussionSynthesizer(44100);

MULTI-TIMBRAL - one synthesizer that honours the program changes the music
carries. This is the road for playing a MIDI file that already says what each
channel should sound like.

    var synthesizer = library.CreateMultiTimbralSynthesizer(44100);

This library offers both. A library that offered only one would throw
NotSupportedException for the other, which is what SupportsPerPart and
SupportsMultiTimbral are for.


SWAPPING VOICES ONE AT A TIME: THE MAPPED LIBRARY
=================================================
THE WORKFLOW THIS SERVES: start from a General MIDI library, hear the piece all
the way through, and then replace the voices you are not happy with, one at a
time, with instruments of your own - a Decent Sampler pack, an SFZ instrument,
another SoundFont. A complete General MIDI library is a fine place to start,
and this is one; nothing is silent while you work, and each substitution is one
line.

MappedInstrumentLibrary belongs to CodeBrix.Audio. It is a BASE library plus
per-program substitutions, and it is itself an instrument library, so it
registers under a name of its own and is asked for the same way:

    FluidR3GmInstrumentLibrary.Register();

    var mine = new MappedInstrumentLibrary(
        "MyGame",
        "FluidR3Gm, with a piano and a cello of my own.",
        FluidR3GmInstrumentLibrary.LibraryName);       // the base, by name

    mine.SetInstrument(GeneralMidiProgram.AcousticGrandPiano, "/packs/Piano.dspreset");
    mine.SetInstrument(GeneralMidiProgram.Cello, "/packs/Cello.sfz");
    mine.Register();

    var library = InstrumentLibraryRegistry.Resolve("MyGame");

Every program you have NOT set still plays from this library, so the piece is
never silent and the comparison is honest: you hear your new piano against the
same arrangement you were listening to a moment ago.

  * The base may be named as a string, as here - it is resolved on FIRST USE, so
    it may be registered afterwards - or passed as an IInstrumentLibrary.
  * SetInstrument takes a Decent Sampler preset, library or bundle, an .sfz, or
    a .sf2 whose program of the same number is used; or a factory of your own
    for a synthesizer you build yourself.
  * SetInstrumentFromLibrary takes one program from another registered library,
    which is how you keep this library's cello while everything else comes from
    somewhere else - or the other way round.
  * SetPercussion and SetPercussionFromSoundFont replace the kit.
  * ClearInstrument puts a program back to the base, so a substitution can be
    tried and undone without rebuilding anything.
  * Substitutions may be added and removed before OR after the mapped library is
    registered.

SUBSTITUTION IS BY PROGRAM. If two parts of a piece share a program and only one
of them should change, that belongs to the routing of the parts rather than to
the library.


KEEPING THE SOUNDFONT SOMEWHERE ELSE
====================================
The package lands the SoundFont in the output folder of every project that
reaches it. For a single executable that is exactly right. For an application
built from several projects it means several copies of a large file, and for an
application that keeps all its assets in one place it is simply the wrong place.

Switch the copying off in the build:

    <PropertyGroup>
      <CodeBrixFluidR3GmCopyAssetsToOutput>false</CodeBrixFluidR3GmCopyAssetsToOutput>
    </PropertyGroup>

and name the file at start-up, before the first sound:

    FluidR3GmInstrumentLibrary.UseSoundFontAt("/opt/mygame/assets/FluidR3_GM.sf2");
    FluidR3GmInstrumentLibrary.Register();

Ship the author's licence text with it. LICENSE-FluidR3_GM.txt and
README-FluidR3_GM.txt sit beside the SoundFont in the package's assets folder,
and the notice has to travel with the samples wherever they go.

CodeBrixFluidR3GmAssetsDirectory changes where the build copies the assets FROM,
for a build that relocates package content.


COMPLETE EXAMPLES
=================
RENDER A MIDI FILE TO A WAV FILE WITH RECORDED INSTRUMENTS

    using CodeBrix.Audio.Instruments;
    using CodeBrix.Audio.Samples.FluidR3Gm;
    using CodeBrix.Audio.Synth;

    FluidR3GmInstrumentLibrary.Register();

    var library = InstrumentLibraryRegistry.Resolve(FluidR3GmInstrumentLibrary.LibraryName);
    var synthesizer = library.CreateMultiTimbralSynthesizer(44100);

    SoundFontRenderer.RenderToWavFile(
        synthesizer, new MidiSequence("piece.mid"), "piece.wav");


OFFER THE PLAYER BOTH SOUNDS, AND SWITCH BETWEEN THEM

    using System;
    using CodeBrix.Audio.Instruments;
    using CodeBrix.Audio.ModestSynth;
    using CodeBrix.Audio.Samples.FluidR3Gm;

    GeneralMidiInstrumentLibrary.Register();    // synthesized; registered first,
    FluidR3GmInstrumentLibrary.Register();      // so it is the default

    foreach (var name in InstrumentLibraryRegistry.RegisteredNames)
    {
        var choice = InstrumentLibraryRegistry.Resolve(name);
        Console.WriteLine($"{choice.Name}: {choice.Description}");
    }

    // the player chose; nothing else in the application changes
    var library = InstrumentLibraryRegistry.Resolve(chosenName);


CHECK AT START-UP THAT THE SOUNDFONT IS THERE

    using System;
    using CodeBrix.Audio.Instruments;
    using CodeBrix.Audio.Samples.FluidR3Gm;

    FluidR3GmInstrumentLibrary.Register();

    if (!FluidR3GmInstrumentLibrary.IsSoundFontAvailable)
    {
        Console.Error.WriteLine(
            $"No SoundFont at {FluidR3GmInstrumentLibrary.SoundFontPath} - falling back to the "
            + "synthesized instruments.");
        InstrumentLibraryRegistry.SetDefault("ModestSynthGm");
    }


VOICE AN ARRANGEMENT PART BY PART

    using CodeBrix.Audio.Instruments;
    using CodeBrix.Audio.Midi;
    using CodeBrix.Audio.Samples.FluidR3Gm;

    FluidR3GmInstrumentLibrary.Register();
    var library = InstrumentLibraryRegistry.Resolve("FluidR3Gm");

    var melody = library.CreateSynthesizer((int)GeneralMidiProgram.Celesta, 44100);
    var pad = library.CreateSynthesizer((int)GeneralMidiProgram.ChoirAahs, 44100);
    var bass = library.CreateSynthesizer((int)GeneralMidiProgram.Cello, 44100);
    var kit = library.CreatePercussionSynthesizer(44100);

    // one SoundFont behind all four, whatever the arrangement does with them


MINIMUM VIABLE PROJECT TEMPLATE
===============================
MyMusic.csproj

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.Audio.Samples.FluidR3Gm.MitLicenseForever" />
      </ItemGroup>
    </Project>

Program.cs

    using System;
    using CodeBrix.Audio.Instruments;
    using CodeBrix.Audio.Midi;
    using CodeBrix.Audio.Samples.FluidR3Gm;

    FluidR3GmInstrumentLibrary.Register();

    var library = InstrumentLibraryRegistry.Resolve("FluidR3Gm");
    var piano = library.CreateSynthesizer((int)GeneralMidiProgram.AcousticGrandPiano, 44100);

    Console.WriteLine($"Playing from {FluidR3GmInstrumentLibrary.SoundFontPath}");
    Console.WriteLine(library.Coverage);

Nothing else is needed: the SoundFont is in the output folder beside the
executable, put there by the package.


PERFORMANCE TIPS
================
MEMORY IS THE REAL COST; TIME IS SMALLER THAN YOU EXPECT. The SoundFont is
about 140 MiB of recorded audio, and the whole of it is held for as long as the
library is in use - the sample data IS the file. The LOAD, by contrast, is
essentially one read of that file: it is bounded by the disk, so it is a
fraction of a second where the file is already in the operating system's cache
and longer on a cold cache or a slow disk. The synthesized General MIDI library
costs neither - its voices are oscillators and tables - so an application that
cares about its memory footprint should measure both and choose deliberately.
Measure on the hardware you ship to; a figure from somebody else's machine is a
figure about their disk.

TAKE THE READ WHERE IT DOES NOT SHOW. Touch the library during a loading screen,
a splash or a menu, rather than on the first note of the first piece - on a cold
cache that read is the difference between a menu that waits and a first note
that stutters:

    _ = InstrumentLibraryRegistry.Resolve("FluidR3Gm").Coverage;

REGISTER FREELY. Registering costs nothing at all, so an application may
register both libraries at start-up and decide later.

CREATE ONE SYNTHESIZER PER PART AND KEEP IT. Creating one is cheap - the samples
are already loaded and shared - but it is not free, and a synthesizer holds the
state of the notes that are sounding.

RENDER OFFLINE WHEN YOU CAN. An application that knows what it will play can
render to a file once and play the audio afterwards, which costs no
synthesis at play time at all.

THE SYNTHESIZERS ARE SINGLE-THREADED BY CONTRACT, although the library itself is
safe to call from several threads. One synthesizer per part, rendered by one
thread, is the shape everything here expects.


COMMON PITFALLS TO AVOID
========================
FORGETTING TO REGISTER. CodeBrix.Audio registers nothing on your behalf, and
there is no module initializer doing it quietly. With an empty registry nothing
plays. Call FluidR3GmInstrumentLibrary.Register() at start-up.

ASSUMING THIS LIBRARY IS THE DEFAULT. The FIRST library registered is the
default, so an application that also registers the synthesized one may get
whichever it happened to register first. Ask by name, or call
InstrumentLibraryRegistry.SetDefault("FluidR3Gm").

EXCLUDING THE PACKAGE'S BUILD ASSETS. A PackageReference carrying PrivateAssets
or ExcludeAssets that covers "buildtransitive" stops the SoundFont being copied,
and the failure shows up at the first note rather than at build time. The
FileNotFoundException message names this first because it is the usual cause.

READING Coverage TO FIND OUT WHETHER THE FILE IS THERE. Coverage loads the
SoundFont. IsSoundFontAvailable is the cheap question.

CALLING UseSoundFontAt TOO LATE. It refuses once the SoundFont has been read,
because every synthesizer already made is playing from it. Call it at start-up;
IsLoaded says whether that moment has passed.

PUTTING A MELODIC PART ON CHANNEL 10. The SoundFont format hard-wires MIDI
channel 10 to the drum bank, so a part routed there sounds as percussion
whatever program it was created with. This is a property of SoundFonts, not of
this package.

EXPECTING A PINNED PART TO FOLLOW THE MUSIC. A synthesizer from
CreateSynthesizer ignores program change on purpose - you decided what that part
sounds like. Use CreateMultiTimbralSynthesizer when the music should decide.

SHIPPING THE SAMPLES WITHOUT THE NOTICE. The SoundFont is MIT, which requires
the copyright notice and the permission notice to travel with it. The package
puts both text files beside the SoundFont; keep them there.


WHAT THIS PACKAGE DOES NOT DO
=============================
  * It does not register itself. Registration is always the application's job.
  * It does not play or schedule anything. It hands back synthesizers;
    CodeBrix.Audio plays, sequences and renders them.
  * It does not read or write MIDI files, and it does not encode audio.
  * It cannot unload the SoundFont.
  * It does not change any sound, mix, tune or re-map anything. The SoundFont is
    redistributed exactly as its author published it.
  * It does not choose instruments for a piece of music. Naming a voice for each
    part is the application's decision.
  * It is not a general SoundFont player: for a SoundFont of your own, use
    CodeBrix.Audio's SoundFontInstrumentLibrary directly, which takes a name, a
    description and a path.
  * It ships no second SoundFont, no variation banks and no alternative kits.
  * It does not depend on, and has nothing to do with, music generation.


WORKING EXAMPLES ON GITHUB
==========================
The test project exercises every feature area of this package:

  tests/CodeBrix.Audio.Samples.FluidR3Gm.Tests/FluidR3GmInstrumentLibraryTests.cs
      registration and its rules, resolving by name in any case, lazy loading,
      where the SoundFont is read from, coverage of every program and of the
      kit, both synthesizer shapes, the one shared SoundFont, argument checking,
      and non-silent offline renders of a short phrase on several programs and
      on the kit.

  tests/CodeBrix.Audio.Samples.FluidR3Gm.Tests/SoundFontLocationTests.cs
      where the SoundFont is looked for, and what the error says when it is not
      there.

https://github.com/ellisnet/CodeBrix.Audio.Samples.FluidR3Gm/tree/main/tests/CodeBrix.Audio.Samples.FluidR3Gm.Tests


QUICK REFERENCE CARD
====================
    using CodeBrix.Audio.Instruments;
    using CodeBrix.Audio.Midi;
    using CodeBrix.Audio.Samples.FluidR3Gm;
    using CodeBrix.Audio.Synth;

    FluidR3GmInstrumentLibrary.Register();              // idempotent; loads nothing

    FluidR3GmInstrumentLibrary.LibraryName              // "FluidR3Gm"
    FluidR3GmInstrumentLibrary.SoundFontFileName        // the file's own name
    FluidR3GmInstrumentLibrary.Instance                 // the one instance
    FluidR3GmInstrumentLibrary.IsRegistered
    FluidR3GmInstrumentLibrary.IsSoundFontAvailable     // cheap; touches nothing
    FluidR3GmInstrumentLibrary.IsLoaded
    FluidR3GmInstrumentLibrary.SoundFontPath
    FluidR3GmInstrumentLibrary.UseSoundFontAt(path)     // before the first sound

    var library = InstrumentLibraryRegistry.Resolve("FluidR3Gm");
    InstrumentLibraryRegistry.SetDefault("FluidR3Gm");
    InstrumentLibraryRegistry.RegisteredNames

    library.Coverage                                    // loads the SoundFont
    library.CreateSynthesizer(program, sampleRate)      // one part, pinned
    library.CreatePercussionSynthesizer(sampleRate)     // the kit
    library.CreateMultiTimbralSynthesizer(sampleRate)   // a whole piece

    // start here, then swap voices one at a time for instruments of your own
    var mine = new MappedInstrumentLibrary("MyGame", "...", "FluidR3Gm");
    mine.SetInstrument(GeneralMidiProgram.Cello, "/packs/Cello.dspreset");
    mine.Register();

    // build property: CodeBrixFluidR3GmCopyAssetsToOutput = false

================================================================================
END OF AGENT-README
================================================================================
