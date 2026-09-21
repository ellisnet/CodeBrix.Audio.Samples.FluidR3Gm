using System;
using System.IO;
using CodeBrix.Audio.Samples.FluidR3Gm.Internal;
using SilverAssertions;
using SilverAssertions.Primitives;
using Xunit;

namespace CodeBrix.Audio.Samples.FluidR3Gm.Tests;

/// <summary>
/// Covers <see cref="SoundFontLocation"/> - where the SoundFont is looked for, and what a consumer
/// is told when it is not there.
/// </summary>
/// <remarks>
/// The message is worth a test of its own. A missing sample file is the one failure a consumer of
/// this package is likely to meet; it happens at run time, on somebody else's machine, and what
/// they need is the name of the build switch that left the file behind rather than a path.
/// </remarks>
public class SoundFontLocationTests
{
    [Fact]
    public void BesideTheApplication_is_the_folder_the_application_runs_from()
        => SoundFontLocation.BesideTheApplication("FluidR3_GM.sf2")
            .Should().Be(Path.Combine(AppContext.BaseDirectory, "FluidR3_GM.sf2"));

    [Fact]
    public void NotFound_names_the_file_and_where_it_was_looked_for()
    {
        //Act
        var error = SoundFontLocation.NotFound("/opt/game/FluidR3_GM.sf2", "FluidR3_GM.sf2");

        //Assert
        error.Message.Should().Contain("FluidR3_GM.sf2");
        error.Message.Should().Contain("/opt/game/FluidR3_GM.sf2");
        error.FileName.Should().Be("/opt/game/FluidR3_GM.sf2");
    }

    [Fact]
    public void NotFound_says_what_leaves_the_file_behind_and_how_to_point_somewhere_else()
    {
        //Act
        var error = SoundFontLocation.NotFound("/opt/game/FluidR3_GM.sf2", "FluidR3_GM.sf2");

        //Assert
        error.Message.Should().Contain("ExcludeAssets");
        error.Message.Should().Contain("CodeBrixFluidR3GmCopyAssetsToOutput");
        error.Message.Should().Contain("UseSoundFontAt");
    }
}
