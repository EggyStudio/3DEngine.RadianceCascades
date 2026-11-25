using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The level a surface point is gathered from, as the library's <c>gatheredLevel</c> picks it on
/// the CPU from the library's Slang over three levels laid out as an engine's are.
/// </summary>
public sealed class GatherLevelTests
{
    // Three levels of 8 probes a side about the same middle, spaced 1, 2 and 4, each level's corner
    // half its spacing in from its cascade's, as an engine lays its levels out.
    private static readonly (Vector3 Corner, float Spacing)[] Levels =
        [(new Vector3(-3.5f), 1), (new Vector3(-7), 2), (new Vector3(-14), 4)];

    private static uint[] Chosen(Vector3[] points)
    {
        var floats = new List<float> { Levels.Length };
        foreach (var (corner, spacing) in Levels)
            floats.AddRange([corner.X, corner.Y, corner.Z, spacing, 8, 8, 8]);
        var chosen = new uint[2 * points.Length];
        CpuKernel.Run("gather_level_cpu.slang", ((uint)points.Length, 1u, 1u), floats.ToArray(),
            points.SelectMany(p => new[] { p.X, p.Y, p.Z }).ToArray(), chosen);
        return chosen;
    }

    [CpuKernelFact]
    public void A_Point_Within_Half_A_Spacing_Past_The_Finest_Probes_Is_Gathered_There_And_One_Past_That_From_The_Level_Above()
    {
        // The finest level's probes run from -3.5 to 3.5. A point among them, one a quarter of a
        // unit past its last probe as a court's wall lies, one 0.6 of a spacing past it and one
        // past every level's probes take the finest level, the finest, the level above and the
        // coarsest at the library's rim, and the second the level above with no rim, as a gather
        // holding a point by the probes about it alone takes it.
        Vector3[] points = [new(0.3f, -1.2f, 2), new(3.75f, 0, 0), new(0, 4.1f, 0), new(0, 0, 40)];
        var chosen = Chosen(points);
        var (atRim, without) = (chosen.Where((_, i) => i % 2 == 0).ToArray(), chosen.Where((_, i) => i % 2 == 1).ToArray());
        var said = $"at the rim the points take levels {string.Join(", ", atRim)}, with none {string.Join(", ", without)}";
        Assert.True(atRim.SequenceEqual(new uint[] { 0, 0, 1, 2 }) && without.SequenceEqual(new uint[] { 0, 1, 1, 2 }), said);
    }
}
