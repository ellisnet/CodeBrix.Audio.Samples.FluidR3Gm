using System;
using System.IO;

namespace CodeBrix.Audio.Samples.FluidR3Gm.Internal;

/// <summary>
/// Where the SoundFont is expected to be, and what to say when it is not there.
/// </summary>
/// <remarks>
/// The message matters more than the lookup. A missing sample file is the one failure a consumer of
/// this package is likely to meet, it happens at run time rather than at build time, and the thing
/// they need to be told is not "file not found" but which build switch or which packaging choice
/// left it behind - so the text is written out in full here rather than assembled at the throw site.
/// </remarks>
internal static class SoundFontLocation
{
    /// <summary>
    /// The path a file is expected at when it sits beside the running application, which is where
    /// this package's build assets put the SoundFont.
    /// </summary>
    /// <param name="fileName">The file's name.</param>
    /// <returns>The full path.</returns>
    internal static string BesideTheApplication(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, fileName);
    }

    /// <summary>
    /// The exception thrown when the SoundFont is not where it was looked for.
    /// </summary>
    /// <param name="soundFontPath">Where it was looked for.</param>
    /// <param name="fileName">The file's own name.</param>
    /// <returns>An exception whose message says what to do about it.</returns>
    internal static FileNotFoundException NotFound(string soundFontPath, string fileName)
    {
        return new FileNotFoundException(
            "The " + fileName + " SoundFont that the FluidR3Gm instrument library plays was not found"
                + " at '" + soundFontPath + "'. The package lands it in the output folder of every"
                + " project that references it - including an application that reaches the package"
                + " only through a library of its own - so a missing file almost always means one of"
                + " three things: the PackageReference excludes the package's build assets"
                + " (PrivateAssets or ExcludeAssets covering buildtransitive does that);"
                + " CodeBrixFluidR3GmCopyAssetsToOutput was set to false, which is the supported way"
                + " to ship the SoundFont some other way; or the application was deployed without its"
                + " content files. If the SoundFont is somewhere else on purpose, say so before using"
                + " the library: FluidR3GmInstrumentLibrary.UseSoundFontAt(path).",
            soundFontPath);
    }
}
