using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// A floor under a solid slab and one in a box open at its top, read by reach sight at an engine's
/// configuration against the sky traced at each.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // A floor 30 a side under a solid slab 20 a side whose underside stands 2 over it.
    private static readonly (Vector3 Low, Vector3 High)[] SlabScene =
        [(new(-15, -1, -15), new(15, 0, 15)), (new(-10, 2, -10), new(10, 3, 10))];

    // A box open at its top, its floor from -1.5 to 0 and its walls up to 8.
    private static (Vector3 Low, Vector3 High)[] OpenBoxScene()
    {
        const float o = 4 + Wall;
        return
        [
            (new(-o, -1.5f, -o), new(o, 0, o)), (new(4, 0, -o), new(o, 8, o)), (new(-o, 0, -o), new(-4, 8, o)),
            (new(-4, 0, 4), new(4, 8, o)), (new(-4, 0, -o), new(4, 8, -4)),
        ];
    }

    [CpuKernelFact]
    public void A_Floor_Under_A_Solid_Slab_And_One_In_An_Open_Box_Take_Near_The_Sky_Traced_There_By_Reach_Sight_At_An_Engines_Configuration()
    {
        var (place, center) = (new Vector3(0, 0, 3.5f), new Vector3(0, 1, 3.5f));
        var (slab, bound) = (FloorOverTrace(SlabScene, place, center, false), FloorOverTrace(SlabScene, place, center, true));
        var box = FloorOverTrace(OpenBoxScene(), new Vector3(0.3f, 0, 0.6f), new Vector3(0, 4, 0), true);
        var said = $"the floor under the solid slab reads {slab:0.000} of the sky traced there with the finest two levels read at the "
            + $"texels they trace and {bound:0.000} at 8 a side, and the open box's floor {box:0.000}";
        output.WriteLine(said);
        // The slab's floor reads 0.915 with the finest two levels read at the 16 texels a side they
        // trace, and 1.693 at 8, their 2 by 2 means, as an engine binds them. The floor takes the
        // sky through the gap under the slab's edge at a graze, along the texels beside the
        // horizon, and a mean of four rays read at its texel's middle way weighs the two that leave
        // under the edge at the middle's cosine, about twice their own. The finest level alone
        // reads it 1.589 at 8 a side, 0.805 at 16 and 0.591 at 64, where the sky traced half a unit
        // over the floor, its probes' height, reads 0.577. The merge at the ray's end reads 1.562
        // and 0.793, and 1.562 alike with each level's rays past the first begun at its spacing,
        // whether those meeting a surface short of it are kept dark or not. The open box's floor
        // reads 1.160 as bound, and 0.612 with the far probes taken that meet a surface short of
        // where the ray ended (sightedFarLight).
        Assert.True(MathF.Abs(MathF.Log(slab)) <= 0.15f && MathF.Abs(MathF.Log(box)) <= 0.2f, said);
    }

    // The light a floor point takes by reach sight at an engine's configuration about center, the
    // sky Sky and every surface black, over the sky traced at it, the finest two levels read at 8
    // texels a side as bound or at the 16 they trace.
    private static float FloorOverTrace((Vector3 Low, Vector3 High)[] scene, Vector3 place, Vector3 center, bool asBound)
    {
        var levels = EngineLevels(center).Select(level => asBound ? level : level with { Kept = 0 }).ToArray();
        var setting = EngineSetting(new Setting { Merge = Merge.ByReachSight }, center);
        var read = Passes(Cells(scene, EngineField(center)), levels, place, setting);
        return read.Reflected / (SkyTraced(scene, place + new Vector3(0, 1e-3f, 0), Vector3.UnitY, Sky) * Albedo / MathF.PI);
    }
}
