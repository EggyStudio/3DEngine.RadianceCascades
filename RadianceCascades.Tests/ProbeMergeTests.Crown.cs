using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// A crown of leaves that passes more of the light at its edge than at its core, as a tree's
/// does, read through by a probe before it under the uniform sky on the CPU, against the sky it
/// passes traced along each way through the share of each point of its depth.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // The crown, a solid box 6 units a side passing CrownEdge of the light a unit of its depth
    // takes within a unit of its faces and CrownCore within its core.
    private static readonly Vector3 CrownLow = new(-3, 1, -3), CrownHigh = new(3, 7, 3);
    private const float CrownEdge = 0.9f, CrownCore = 0.3f;

    private static float CrownShare(Vector3 p)
    {
        var (low, high) = (CrownLow + Vector3.One, CrownHigh - Vector3.One);
        return p.X > low.X && p.Y > low.Y && p.Z > low.Z && p.X < high.X && p.Y < high.Y && p.Z < high.Z ? CrownCore : CrownEdge;
    }

    // The crown's field as a binding holds a crown's leaf cards that run together, one region with
    // no plane read half a cell inside it throughout, each cell within it taking the share of its
    // middle, the core's edge lying on the cells' edges, and each cell before its faces the edge's.
    // Built as a solid box, its cells deeper than the field's band would read open.
    private static float[] CrownField()
    {
        var cells = Field(Cuboid(CrownLow, CrownHigh, 1, CrownEdge));
        for (int i = 0; i < Size * Size * Size; i++)
        {
            var middle = Corner + (new Vector3(i % Size, i / Size % Size, i / (Size * Size)) + new Vector3(0.5f)) * Cell;
            if (middle.X > CrownLow.X && middle.Y > CrownLow.Y && middle.Z > CrownLow.Z && middle.X < CrownHigh.X && middle.Y < CrownHigh.Y
                && middle.Z < CrownHigh.Z)
                (cells[i * 5], cells[i * 5 + 4]) = (-0.5f * Cell, CrownShare(middle));
        }
        return cells;
    }

    // The share of light a ray from point along way carries past the crown, the share of each point
    // of its depth taken to the power of a step of a thousandth of a unit.
    private static double CrownPassed(Vector3 point, Vector3 way)
    {
        var (near, far) = (0f, float.MaxValue);
        for (int axis = 0; axis < 3; axis++)
        {
            if (MathF.Abs(way[axis]) < 1e-9f)
            {
                if (point[axis] <= CrownLow[axis] || point[axis] >= CrownHigh[axis])
                    return 1;
                continue;
            }
            var (a, b) = ((CrownLow[axis] - point[axis]) / way[axis], (CrownHigh[axis] - point[axis]) / way[axis]);
            (near, far) = (MathF.Max(near, MathF.Min(a, b)), MathF.Min(far, MathF.Max(a, b)));
        }
        if (far <= near)
            return 1;
        const float step = 1e-3f;
        var sum = 0.0;
        for (var t = near + step * 0.5f; t < far; t += step)
            sum += Math.Log(CrownShare(point + way * t)) * step;
        return Math.Exp(sum);
    }

    // What a point facing normal reflects of the sky the crown passes, over 64 by 64 ways spread by
    // their cosine.
    private static float CrownTraced(Vector3 point, Vector3 normal)
    {
        var (u, v) = Basis(normal);
        const int side = 64;
        var rows = new double[side];
        Parallel.For(0, side, i =>
        {
            for (int j = 0; j < side; j++)
                rows[i] += Sky * CrownPassed(point, CosineWay(normal, u, v, (i + 0.5f) / side, (j + 0.5f) / side));
        });
        return (float)(Albedo * rows.Sum() / (side * side));
    }

    [CpuKernelFact]
    public void A_Crown_That_Passes_Less_Light_At_Its_Core_Than_At_Its_Edge_Dims_The_Sky_A_Probe_Before_It_Takes_As_Its_Depth_Does()
    {
        // A probe 2 units before the crown's front at its middle height, facing it, its rays
        // reaching past the field, lit by the uniform sky alone, every leaf black.
        var at = new Vector3(0, 4, 5);
        var reading = Passes(CrownField(), [new(at, 1, 1, 1, 1, NearTexels, 32)], at, new Setting { Normal = -Vector3.UnitZ });
        var traced = CrownTraced(at, -Vector3.UnitZ);
        var said = $"a probe 2 units before the crown {reading}, where the sky the crown passes traced gives {traced:0.00000}, "
            + $"{reading.Reflected / traced:0.000} of it";
        output.WriteLine(said);
        // 0.63842 against 0.64336, 0.992 of it, each step of a crossing passing the share of the
        // cell it began in, where with the share the ray met the crown at taken for its whole depth
        // the crown read as thin as its edge and the probe 0.79267, 1.232 of it.
        Assert.True(Math.Abs(reading.Reflected / traced - 1) <= 0.05, said);
    }
}
