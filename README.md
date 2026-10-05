# CodeBrix.Audio.Samples.FluidR3Gm

A recorded General MIDI instrument library for .NET, ready to play. CodeBrix.Audio.Samples.FluidR3Gm carries the FluidR3_GM SoundFont and offers it to CodeBrix.Audio under the name `FluidR3Gm`, so an application that already plays MIDI swaps its whole sound for recorded instruments in one line. CodeBrix.Audio.Samples.FluidR3Gm is provided as a .NET 10 library and associated `CodeBrix.Audio.Samples.FluidR3Gm.MitLicenseForever` NuGet package.

CodeBrix.Audio.Samples.FluidR3Gm supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028.
Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

## Installation

```
dotnet add package CodeBrix.Audio.Samples.FluidR3Gm.MitLicenseForever
```

Note that the NuGet package ID and the namespace are different - there is no package named plain `CodeBrix.Audio.Samples.FluidR3Gm`:

* NuGet package ID: `CodeBrix.Audio.Samples.FluidR3Gm.MitLicenseForever`
* Assembly and primary namespace: `CodeBrix.Audio.Samples.FluidR3Gm` - i.e. `using CodeBrix.Audio.Samples.FluidR3Gm;`

XML documentation (IntelliSense) ships alongside the assembly.

The package pulls in `CodeBrix.Audio.Core.MitLicenseForever` automatically, and nothing else; no version pinning is needed in the consuming project. For device playback, the application also references `CodeBrix.Audio.MitLicenseForever` on desktop or `CodeBrix.Audio.Android.ApacheLicenseForever` on Android. Offline synthesis and rendering need only Core.

## CodeBrix.Audio.Samples.FluidR3Gm supports:

* Every General MIDI program and the General MIDI percussion kit, from recorded instruments
* Registration under the name `FluidR3Gm`, in one line, so instruments can be chosen by name
* One synthesizer per part, for an arrangement that voices each part deliberately
* One multi-timbral synthesizer, for playing a MIDI file that carries its own instrument assignments
* One SoundFont shared by every part, so a twelve-part arrangement holds one copy of the samples
* Loading on first use, so registering the library costs nothing until a sound is asked for
* A coverage report read from the SoundFont itself, rather than claimed
* Playing from a SoundFont kept wherever the application keeps its assets

## The SoundFont arrives with the package

The SoundFont is landed in the output folder of every project that references this package - including an application that reaches the package only through a library of its own - so the usual case needs no configuration at all. It is ordinary MSBuild, and the consuming project does not have to be any particular kind of application.

An application that would rather keep its assets in one shared place sets `CodeBrixFluidR3GmCopyAssetsToOutput` to `false` in its build and names the file at start-up:

```csharp
FluidR3GmInstrumentLibrary.UseSoundFontAt("/opt/mygame/assets/FluidR3_GM.sf2");
```

On Android, a file copied to the build output is not automatically included in the APK. Package the SoundFont and its licence notices as Android assets, extract them into app-private storage, and call `UseSoundFontAt` before the first sound.

## Sample Code

### Play with recorded instruments instead of synthesized ones

```csharp
using CodeBrix.Audio.Instruments;
using CodeBrix.Audio.Midi;
using CodeBrix.Audio.Samples.FluidR3Gm;

FluidR3GmInstrumentLibrary.Register();

var library = InstrumentLibraryRegistry.Resolve("FluidR3Gm");
var cello = library.CreateSynthesizer((int)GeneralMidiProgram.Cello, 44100);
var kit = library.CreatePercussionSynthesizer(44100);
```

### Play a MIDI file that carries its own instrument assignments

```csharp
using CodeBrix.Audio.Instruments;
using CodeBrix.Audio.Samples.FluidR3Gm;
using CodeBrix.Audio.Synth;

FluidR3GmInstrumentLibrary.Register();

var library = InstrumentLibraryRegistry.Resolve("FluidR3Gm");
var synthesizer = library.CreateMultiTimbralSynthesizer(44100);

SoundFontRenderer.RenderToWavFile(synthesizer, new MidiSequence("piece.mid"), "piece.wav");
```

## Documentation

The NuGet package includes `AGENT-README.txt`, a complete API reference and usage guide written for AI coding agents - point your agent at that file when it is writing code against this library.

Additional sample code and usage examples are available in the `CodeBrix.Audio.Samples.FluidR3Gm.Tests` project:
https://github.com/ellisnet/CodeBrix.Audio.Samples.FluidR3Gm/tree/main/tests/CodeBrix.Audio.Samples.FluidR3Gm.Tests

## License

CodeBrix.Audio.Samples.FluidR3Gm is licensed under the MIT License - see the
[LICENSE](https://github.com/ellisnet/CodeBrix.Audio.Samples.FluidR3Gm/blob/main/LICENSE) file.

For licensing and provenance information about the open source code and the sample content included in
this package, see [THIRD-PARTY-NOTICES.txt](https://github.com/ellisnet/CodeBrix.Audio.Samples.FluidR3Gm/blob/main/THIRD-PARTY-NOTICES.txt).
