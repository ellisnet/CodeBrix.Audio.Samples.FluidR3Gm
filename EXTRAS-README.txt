================================================================================
EXTRAS-README: CodeBrix.Audio.Samples.FluidR3Gm
Samples, tools and other content in this repository that is not part of a NuGet
package
================================================================================

This repository ships no sample applications or demos. Exactly one project is
packable - src/CodeBrix.Audio.Samples.FluidR3Gm - and everything else listed
below exists only to test it, to document it, or to produce the SoundFont it
ships. None of it is included in the
CodeBrix.Audio.Samples.FluidR3Gm.MitLicenseForever package.

For runnable, compilable usage of the library, read the test project: the
"WORKING EXAMPLES ON GITHUB" section of AGENT-README.txt maps each feature area
to the test file that exercises it.


TEST PROJECT
============
    tests/CodeBrix.Audio.Samples.FluidR3Gm.Tests/

xUnit v3; run it as described in MAINTAINER-README.txt (TESTING). It reads the
staged SoundFont out of staging/output through a path found by walking up to the
repository root, and SKIPS the tests that need a sound - with a message saying
what to run - when nothing has been staged.


THE ASSET STAGER
================
    tools/AssetStager/

A console application that reproduces the SoundFont this package ships, from the
upstream source archive, on a machine that has nothing but .NET installed. It
references NO NuGet packages at all: the download is HttpClient, the
decompression is GZipStream and the archive is read by System.Formats.Tar.

    dotnet run --project tools/AssetStager -c Release

It is not packable and nothing in the shipping library refers to it. What it
does, what it costs and how it checks itself are in staging/README.txt; what it
produced is in ASSET-PROVENANCE.json at the repository root.

THE SOUNDFONT IS NOT IN GIT. It is far past the size a git host will accept, and
there is no reason to store in git what a recorded upstream address reproduces
byte for byte. staging/download and staging/output are ignored; only
staging/README.txt is checked in.
