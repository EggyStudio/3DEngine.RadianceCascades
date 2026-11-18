using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// What the octahedral map and the texels' marks compute, run on the CPU from the library's Slang.
/// </summary>
public sealed class OctahedronTests
{
    [CpuKernelFact]
    public void Every_Texels_Middle_Comes_Back_From_The_Way_It_Stands_For_And_Each_Mark_Reads_Back_Within_Its_Band()
    {
        // Filled before the run, so a texel the kernel left unwritten reads as missed.
        var missed = Enumerable.Repeat(1f, 16 * 16).ToArray();
        var nothing = new float[3 * 5];
        CpuKernel.Run("octahedron_cpu.slang", (1, 1, 1), missed, nothing);

        Assert.True(missed.Max() < 1e-5f, $"a texel's middle came back {missed.Max()} from itself");
        for (int none = 0; none <= 2; none++)
        {
            Assert.Equal(none, nothing[none]);
            Assert.Equal(none, nothing[5 + none]);
        }
        for (int none = 2; none <= 4; none++)
            Assert.Equal(none, nothing[10 + none]);
    }
}
