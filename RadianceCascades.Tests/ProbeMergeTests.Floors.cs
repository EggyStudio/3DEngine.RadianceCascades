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

    // An engine's field for the slab's floor about its eye, two cascades of 64 cells from 0.15,
    // each its least corner and cell, and its two levels of probes 1.2 and 2.4 apart, traced and
    // read at 16 texels a side, the finer ending at four times its spacing and the coarser at the
    // field's edge. The field ends 5.4 units from the slab's middle along -z, short of the slab's
    // edge.
    private static readonly Vector4[] SmallSlabField = [new(-4.8f, -3.6f, 2.4f, 0.15f), new(-9.6f, -7.2f, -2.4f, 0.3f)];

    private static readonly Level[] SmallSlabLevels =
        [new(new(-4.2f, -3, 3), 1.2f, 8, 8, 8, 16, 4.8f), new(new(-8.4f, -6, -1.2f), 2.4f, 8, 8, 8, 16, 33.255f)];

    [CpuKernelFact]
    public void A_Probe_Under_A_Slab_Takes_The_Floor_Its_Ray_Marched_On_Meets_In_The_Field_Where_The_Coarsest_Probes_See_Past_It()
    {
        // The finest level's probe 0.6 over the floor under the solid slab, every surface black, in
        // the small field above, its faces toward -z and +x read by reach sight against the sky
        // traced along each texel's middle way. Its rays along the ways close under the horizon end
        // at 4.8 and would meet the floor at 6.4 to 8.4, and the coarsest level's probes, standing
        // 1.2 up, see past the floor along them to the field's edge and the sky. Where the floor
        // lies within the field the ray marched on to twice where it ended meets it and leaves
        // those probes out. Where the ray leaves the field before the floor, no march can know the
        // floor is there, and the face takes the sky the field's edge gives.
        var probe = new Vector3(0.6f, 0.6f, 3);
        var setting = EngineSetting(new Setting { Merge = Merge.ByReachSight }, probe) with { FieldCascades = SmallSlabField, Caps = [64, 0] };
        var read = Passes(Cells(SlabScene, SmallSlabField), SmallSlabLevels, probe, setting);
        var faces = new[] { -Vector3.UnitZ, Vector3.UnitX }.Select(n => FaceInAndPastField(read, probe, n)).ToArray();
        var said = string.Join("; ", faces.Select((f, i) => $"its face toward {(i == 0 ? "-z" : "+x")} takes {f.Within:0.000} where the trace "
            + $"gives none along the ways that meet a surface in the field, and {f.Past:0.000} against {f.PastTraced:0.000} along those that "
            + $"leave it first, {(f.Within + f.Past) / f.PastTraced:0.000} of its trace"));
        output.WriteLine(said);
        Assert.True(faces.All(f => f.Within < 0.15f * f.PastTraced) && (faces[1].Within + faces[1].Past) / faces[1].PastTraced < 1.3f, said);
    }

    // A probe's face of the small field's finest level, the light its texels' merge gives weighed
    // by their cosine, split into the ways along which the slab's boxes are met within the field,
    // where the trace gives nothing, and those that leave the field first, and the sky traced along
    // the latter, each over the face's whole weight.
    private static (float Within, float Past, float PastTraced) FaceInAndPastField(Chained read, Vector3 probe, Vector3 n)
    {
        var level = SmallSlabLevels[0];
        var index = Vector3.Round((probe - level.Corner) / level.Spacing);
        var at = (((int)index.Z * level.Y + (int)index.Y) * level.X + (int)index.X) * level.Texels * level.Texels;
        var field = SmallSlabField[^1];
        var (low, high) = (new Vector3(field.X, field.Y, field.Z), new Vector3(field.X, field.Y, field.Z) + new Vector3(64 * field.W));
        var (within, past, pastTraced, weights) = (0f, 0f, 0f, 0f);
        for (int k = 0; k < level.Texels * level.Texels; k++)
        {
            var way = Vector3.Normalize(OctahedronPoint((k % level.Texels + 0.5f) / level.Texels, (k / level.Texels + 0.5f) / level.Texels));
            var facing = MathF.Max(Vector3.Dot(n, way), 0);
            var leaves = float.MaxValue;
            for (int axis = 0; axis < 3; axis++)
                if (way[axis] != 0)
                    leaves = MathF.Min(leaves, ((way[axis] > 0 ? high[axis] : low[axis]) - probe[axis]) / way[axis]);
            var light = read.LevelLight[0][at + k] * facing;
            if (FirstBox(SlabScene, probe, way) < leaves)
                within += light;
            else
                (past, pastTraced) = (past + light, pastTraced + (FirstBox(SlabScene, probe, way) < float.MaxValue ? 0 : Sky) * facing);
            weights += facing;
        }
        return (within / weights, past / weights, pastTraced / weights);
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
        // reads 1.160 as bound, and 0.612 where a ray whose far cascade ends at the field's edge
        // takes every far probe that sees its probe with no march (reachSightLight).
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
