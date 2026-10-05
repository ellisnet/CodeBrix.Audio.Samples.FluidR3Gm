================================================================================
MAINTAINER-README: CodeBrix.Audio.Samples.FluidR3Gm
Notes for people and agents MAINTAINING this repository - not for package
consumers
================================================================================

If you are CONSUMING the NuGet package, stop reading and open AGENT-README.txt
instead. Everything below is about the repository itself: how it is laid out,
how it builds, how it is tested, how it is packaged, and the conventions the
source follows.


STAGE THE SOUNDFONT BEFORE YOU BUILD ANYTHING
=============================================
A FRESH CHECKOUT DOES NOT BUILD. The SoundFont this package exists to ship is
not in git, and GeneratePackageOnBuild is on - as it is family-wide - so every
build packs, and the pack guard refuses to produce a package with no samples in
it. A checkout that has not been staged therefore FAILS AT BUILD TIME, not only
at `dotnet pack`, with an error naming the stager. Run this first, from the
repository root:

    dotnet run --project tools/AssetStager -c Release

That is deliberate. A CodeBrix.Audio.Samples package that shipped no samples
would install cleanly and then fail at run time on somebody else's machine,
which is far worse than a build that stops and says what to run. The cost of the
trade is that a contributor who only wants to change the documentation still
pays for the download - see BUILDING and PACKAGING below for the guard itself.

The TESTS do not need it: they skip, with the same message, when nothing has
been staged.


PURPOSE AND SCOPE
=================
PackageId:      CodeBrix.Audio.Samples.FluidR3Gm.MitLicenseForever
Assembly:       CodeBrix.Audio.Samples.FluidR3Gm
Project:        src/CodeBrix.Audio.Samples.FluidR3Gm
License:        MIT (both this repository's code and the SoundFont it ships)
Consumer doc:   AGENT-README.txt

This repository produces ONE package: a thin library plus a large SoundFont.
The code is small on purpose. CodeBrix.Audio owns the instrument-library
interface, the registry and the generic SoundFont library that does the actual
work; this package supplies a name, a description, where the file is, and the
file itself.

THE ONE DEPENDENCY IS CodeBrix.Audio.Core, AND IT STAYS THE ONLY ONE. This package
must not depend on anything named .MusicGeneration, and it does NOT need
CodeBrix.Audio.ModestSynth: the synthesized General MIDI library in that package
and this recorded one are alternatives to each other, not layers. Adding a
second dependency here would make "reference one package, get recorded
instruments" untrue.


REPOSITORY LAYOUT
=================
    .clinerules, .cursorrules, .windsurfrules, AGENTS.md, CLAUDE.md,
    .cursor/rules/agent-readme.mdc, .github/copilot-instructions.md,
    .junie/guidelines.md
                            the eight AI-agent pointer stubs; each one is a
                            thin redirect to README-INDEX.txt and is byte-for-
                            byte identical to the family's canonical copies.
                            Do not edit them per-repository.
    AGENT-README.txt        consumer documentation; packed into the nupkg.
    ASSET-PROVENANCE.json   written by the asset stager; checked in.
    EXTRAS-README.txt       what is in this repository that is not the package.
    global.json             selects the Microsoft.Testing.Platform test runner.
                            It pins no SDK version. Do not delete it - without
                            it `dotnet test` falls back to the VSTest bridge,
                            which fails on the .NET 10 SDK.
    icon-codebrix-128.png   the NuGet icon; packed into the nupkg.
    LICENSE                 MIT.
    MAINTAINER-README.txt   this file.
    README-INDEX.txt        the map every AI pointer stub points at.
    README.md               human-facing overview; packed into the nupkg.
    THIRD-PARTY-NOTICES.txt the SoundFont's provenance and its licence text,
                            verbatim; packed into the nupkg.
    CodeBrix.Audio.Samples.FluidR3Gm.slnx
                            the solution. Its "Solution Items" folder carries
                            the ten canonical family files plus
                            ASSET-PROVENANCE.json and staging/README.txt; it
                            has a "Tests" folder holding the test project and a
                            "Tools" folder holding the stager.
    src/CodeBrix.Audio.Samples.FluidR3Gm/
                            the library. buildTransitive/net10.0 holds the
                            targets file that lands the SoundFont in a
                            consumer's output folder.
    tests/CodeBrix.Audio.Samples.FluidR3Gm.Tests/
                            the test project.
    tools/AssetStager/      the console application that reproduces the
                            SoundFont from upstream. Not packable, and nothing
                            that ships refers to it.
    staging/                the stager's workshop; see staging/README.txt.
                            staging/download and staging/output are ignored by
                            git and only staging/README.txt is checked in.


THE SOUNDFONT IS NOT IN GIT
===========================
The SoundFont is about 141 MiB - past the file size a git host will accept, and
not something to keep in a repository in any case when a versioned upstream
address reproduces it byte for byte. So it is STAGED rather than committed:

    dotnet run --project tools/AssetStager -c Release

The stager downloads the upstream source archive, checks it against the byte
count and sha256 recorded in tools/AssetStager/Program.cs BEFORE unpacking
anything, reads it in one streaming pass, writes the SoundFont and the author's
COPYING and README text into staging/output, checks what it wrote against the
same kind of record, and writes ASSET-PROVENANCE.json at the repository root.

It references NO NuGet packages at all - HttpClient, GZipStream and
System.Formats.Tar are all in the framework - so reproducing the asset needs
nothing but .NET, now or in ten years.

WHAT MUST MATCH, and what to do when it does not:

    the archive        134,835,922 bytes, sha256
                       2621acaa1c78e4abdb24bdd163230cc577e61276936d6aa6e31805
                       82142f0343
                       A mismatch stops the run BEFORE anything is unpacked.
                       Either the download is damaged - delete
                       staging/download and run again - or upstream has
                       replaced the file at a versioned address, which is a
                       change of the source of record and wants a human
                       decision, not a new constant.

    FluidR3_GM.sf2     148,398,306 bytes, sha256
                       74594e8f4250680adf590507a306655a299935343583256f3b722c
                       48a1bc1cb0

    LICENSE-FluidR3_GM.txt   1,086 bytes, from the archive's COPYING
    README-FluidR3_GM.txt    1,042 bytes, from the archive's README

Re-staging reproduces those bytes, and that is the gate. The asset is COPIED out
of an archive rather than computed from one, so the same bytes are expected on
any machine - unlike a render or a quantization, which are never pinned across
platforms. ASSET-PROVENANCE.json still records the machine, because a hash with
no machine beside it says nothing about what a mismatch would mean.

A run from an empty staging folder downloads about 129 MiB, needs about 1 GiB
free, and takes well under a minute on a normal connection. staging/output is
NEVER cleared by the tool - not by --clean-after and not by the interactive
prompt.


BUILDING
========
    dotnet build --configuration Release CodeBrix.Audio.Samples.FluidR3Gm.slnx

0 warnings / 0 errors. GenerateDocumentationFile is on, so CS1591 is fixed by
writing the doc comment, never by NoWarn.

STAGE BEFORE YOU BUILD - see the section of that name at the top of this file.
GeneratePackageOnBuild is on, so every build packs, and the pack guard refuses
to build a package with no SoundFont in it (see PACKAGING below).

To check the staging folder without building anything:

    dotnet msbuild src/CodeBrix.Audio.Samples.FluidR3Gm -t:CheckFluidR3GmStagedAsset


TESTING
=======
xUnit v3 with SilverAssertions. The test runner IS Microsoft.Testing.Platform,
selected by the root global.json. No coverage collector is referenced - the
family dropped coverlet.collector.

    dotnet test --solution CodeBrix.Audio.Samples.FluidR3Gm.slnx

KNOWN GOTCHA: some .NET 10 SDK builds report "Zero tests ran" from `dotnet test`
although the tests exist. When that happens, run the test assembly directly and
quote its counts:

    dotnet tests/CodeBrix.Audio.Samples.FluidR3Gm.Tests/bin/Debug/net10.0/CodeBrix.Audio.Samples.FluidR3Gm.Tests.dll

THE TESTS READ THE STAGED SOUNDFONT out of staging/output, through a path found
by walking up to the repository root - no absolute path, nothing over the
network. When it has not been staged they SKIP, with a message saying what to
run, rather than fail.

THE CLASSES THAT SHARE THE SINGLETON RUN IN ONE COLLECTION, WITH
PARALLELIZATION OFF. FluidR3GmLibraryCollection carries
[CollectionDefinition(Name, DisableParallelization = true)] and every test class
that touches FluidR3GmInstrumentLibrary carries [Collection(...Name)]. The
library is a process-wide singleton that reads its SoundFont once and then
refuses to be pointed at another file, so tests that raced each other would be
testing the order they happened to run in - and xUnit runs different collections
in parallel by default. Keeping them in one collection also means the SoundFont
is read exactly once for the whole suite. A class that touches nothing
process-wide does not join the collection.

ONE TEST SKIPS ITSELF WHEN IT CANNOT SEE WHAT IT IS TESTING.
"Register_reads_nothing_until_a_sound_is_asked_for" can only observe lazy
loading before the first load in the process; if another test got there first it
says so and skips, rather than asserting something it cannot see.

NOTHING IN THE SUITE READS OR SETS THE REGISTRY'S DEFAULT LIBRARY. Which library
is the default depends on which registration ran first, which is a property of
the whole assembly rather than of any one test, so every lookup is by name.


PACKAGING / PUBLISHING
======================
The csproj carries the canonical date-stamped version block: the version is
1.<years since 2026>.<day of year>.<minute of day>, computed at build time from
UTC now. Never hardcode a literal <Version>.

What the nupkg contains:

    lib/net10.0/                     the assembly and its XML documentation
    assets/FluidR3_GM.sf2            the SoundFont, taken from staging/output
    assets/LICENSE-FluidR3_GM.txt    the author's licence text
    assets/README-FluidR3_GM.txt     the author's release statement
    buildTransitive/net10.0/CodeBrix.Audio.Samples.FluidR3Gm.MitLicenseForever.targets
    icon-codebrix-128.png, README.md, AGENT-README.txt, THIRD-PARTY-NOTICES.txt

THE ASSETS ARE LANDED FROM buildTransitive, NOT FROM contentFiles OR build.
NuGet treats contentFiles and build as PRIVATE assets: they reach the project
that names the PackageReference and stop there. An application is usually a head
project over a shared library, and the head is exactly the project that needs
the SoundFont beside its executable - so the targets file lives in
buildTransitive, which flows all the way down the reference chain. It is plain
MSBuild: a consumer does not have to be any particular kind of application.

Two properties a consumer may set, both documented in the targets file itself:
CodeBrixFluidR3GmCopyAssetsToOutput (false copies nothing) and
CodeBrixFluidR3GmAssetsDirectory (where to copy from).

THE PACK GUARD. Target CheckFluidR3GmStagedAsset runs before GenerateNuspec and
fails, with a message naming the stager and the command to run it, when
staging/output holds no SoundFont or no licence text. A package with nothing in
it can never be built.

SIZE. The SoundFont is about 141 MiB raw, and the whole .nupkg - assembly, XML
documentation, icon, the four doc files and the three assets - deflates to about
127 MiB. That is a little over half of nuget.org's 250 MB per-package limit, so
it fits in ONE package with room to spare and needs no splitting. Anything that
would add a second large asset here needs that arithmetic done again first;
measure the real .nupkg with `ls -l` rather than estimating from the raw file.

Jeremy publishes. Nothing in this repository is committed, tagged or pushed by
an agent.


CODING CONVENTIONS
==================
Every .cs file, source and tests alike:

  * no blank first line; a header comment only on a file ported from an
    upstream project that had one - never invented;
  * the using block is contiguous, one per line, System.* first then the rest,
    alphabetical within each group, fully qualified, never below the namespace
    line, and there are no global usings anywhere;
  * file-scoped namespaces only;
  * NRT is OFF: never write ? on a reference type and never use the !
    null-forgiveness operator. ? on a value type is fine;
  * XML doc comments on every public type and member, and on the internal types
    too in this repository - it is small enough that the habit costs nothing;
  * sub-folders map to sub-namespaces, with the entry-point type at the project
    root and an Internal/ folder for implementation detail;
  * test files are <Class>Tests.cs holding public class <Class>Tests, test
    methods are <MemberName>_<snake_case_description> or plain snake_case,
    multi-statement tests carry //Arrange //Act //Assert and single-statement
    tests are expression-bodied;
  * warnings are fixed at source. The build ends at 0 warnings.

//was previously: exists ONLY to tie ported code back to an upstream
repository. Nothing in this repository is ported, so nothing carries it.


PROVENANCE / VENDORED SOURCES
=============================
No source code is vendored: every .cs file here was written for this repository.

The SOUND CONTENT is third-party and is covered in full in
THIRD-PARTY-NOTICES.txt: the FluidR3_GM SoundFont by Frank Wen, MIT, taken
unmodified from the upstream source archive recorded in ASSET-PROVENANCE.json.
The author's own COPYING and README text ships beside it and is landed in every
consumer's output folder, because the notice travels with the samples.

The archive also holds a second, much smaller SoundFont. It is NOT shipped, NOT
extracted and NOT referred to anywhere in the package; the stager reads past it
and records that it was there.


NOTES
=====
LOADING IS LAZY AND HAPPENS ONCE. FluidR3GmInstrumentLibrary.Register() reads
nothing; the first call that needs a sound reads the file, inside the one lock,
and every synthesizer the library ever hands back renders from that one
SoundFont. There is deliberately no way to unload it: every part of every
arrangement shares it, so dropping it would silence music that is still playing.

WHAT THE LOAD ACTUALLY COSTS, MEASURED. The managed heap grows by very slightly
more than the file's size on disk - the sample data IS the file, held as managed
arrays - and the process working set grows by about the same. THE TIME IS I/O,
not parsing: the load is essentially a read of the whole file, so it is bounded
by what the disk can deliver and it is fast on a warm page cache and slower on a
cold one. Register() itself is a lock and a dictionary insert. Creating a
synthesizer once the file is loaded costs a few megabytes of voice state, not a
copy of the samples. Re-measure on the machine in question rather than quoting a
number from here: the figure a maintainer needs is their own disk's.

WHY THERE IS NO MODULE INITIALIZER. Registration is the consumer's job,
everywhere in this family. A module initializer only runs once something in the
assembly is touched, which trimming and lazy assembly loading make unreliable -
so the entry point is a static Register(), and the documentation says loudly to
call it.

UseSoundFontAt IS PART OF THE PUBLIC SURFACE ON PURPOSE. An application built
from several projects would otherwise keep a copy of a very large file in each
of their output folders; switching the copying off and naming one shared path is
the supported answer. It is also how the test project reaches the staged file,
which is a project reference away from the package's build assets.
