================================================================================
staging/ - where the asset stager works
CodeBrix.Audio.Samples.FluidR3Gm
================================================================================

This folder is the asset stager's workshop. EVERYTHING the stager downloads and
extracts stays inside this repository, and the soundfont it produces is NEVER
committed: it is far past the size a git host will take, and there is no reason
to store in git what an upstream archive reproduces byte for byte.

Only this file is checked in. Everything else here is ignored by git.


THE FOLDERS
-----------
  download/   The upstream archive as it was fetched. It is working material:
              deleting it costs a re-download and nothing else. A run that finds
              the archive already here at the right size skips the download.

  output/     THE ASSET THAT SHIPS, and the only folder here that matters once a
              run is over. The package build takes the soundfont and its licence
              text from it, the test project reads the soundfont from it, and
              packing FAILS with a message naming this tool when it is empty.
              NEVER delete it. The stager itself never clears it.


RUNNING THE STAGER
------------------
From the repository root:

    dotnet run --project tools/AssetStager -c Release

It reports what it is doing as it goes, checks the archive against the size and
the sha256 recorded for it BEFORE unpacking anything, checks what it extracted
against the same kind of record, writes ASSET-PROVENANCE.json at the repository
root, and prints a usage report: what it downloaded, how large this folder grew,
and how long each step took.

    --clean-after     clear download/ when the run succeeds, without asking.
                      output/ is always kept.
    --keep            never ask and never clear; leave everything in place.
    --help            what the tool does and what it takes.

With neither flag, a run at an interactive console offers to clear the download
folder when it finishes.

A run from an EMPTY staging/ downloads about 129 MiB and needs roughly 1 GiB of
free disk space at its peak. It checks for that space before it starts.


WHAT THE STAGER DOES
--------------------
  1. Downloads the upstream source archive from the versioned address recorded
     in tools/AssetStager/Program.cs.
  2. Checks that archive against the byte count and the sha256 recorded for it.
     An archive that does not match is NOT unpacked: the run stops and says what
     the choice is.
  3. Reads the archive in one streaming pass and writes out only the General
     MIDI soundfont and the upstream licence and README text. Every other entry
     is read past, and every entry - taken or not - is recorded.
  4. Checks the soundfont against the byte count and the sha256 recorded for it.
  5. Writes ASSET-PROVENANCE.json: the address, the archive's size and sha256,
     everything the archive held, what was taken out of it and under which name,
     each staged file's size and sha256, where the licence text came from, and
     the operating system and processor the run was made on.

Nothing has to be installed and no NuGet package is referenced: the download is
HttpClient, the decompression is GZipStream and the archive is read by
System.Formats.Tar, all three of which are in .NET itself.

Re-staging reproduces the same bytes, and that is the gate. The asset is COPIED
out of an archive rather than computed from one, so the same bytes are expected
on any machine - the provenance still records the machine, because a hash with
no machine beside it says nothing about what a mismatch would mean.
