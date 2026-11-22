using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// A probe half a unit over a floor whose grazing way toward a step past the end of its ray is
/// handed to far probes that stand over the floor and see over the step, against the light traced
/// along the way.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // The floor up to 3.75 along x, a step half a unit high and a unit deep past it, and a court a
    // unit under the floor past the step with a wall across it at 7.25, the court and its wall
    // sending a light of 1 and the floor and the step a third.
    private static readonly (Vector3 Low, Vector3 High)[] StepScene =
    [
        (new(-8, -4, -8), new(3.75f, 0, 8)),
        (new(3.75f, -4, -8), new(4.75f, 0.5f, 8)),
        (new(4.75f, -4, -8), new(8, -1, 8)),
        (new(7.25f, -1, -8), new(8, 6, 8)),
    ];

    private static float StepSceneLight(Vector3 p) => p.X > 4.75f ? 1 : 1 / 3f;

    // The point of the octahedron at uv, as octahedronPoint gives it.
    private static Vector3 OctahedronPoint(float u, float v)
    {
        var (x, y) = (u * 2 - 1, v * 2 - 1);
        var z = 1 - MathF.Abs(x) - MathF.Abs(y);
        if (z < 0)
            (x, y) = ((1 - MathF.Abs(y)) * MathF.Sign(x), (1 - MathF.Abs(x)) * MathF.Sign(y));
        return new(x, y, z);
    }

    // The light along way from point, that of the surface it first meets, and none where it meets
    // none within the field.
    private static float StepSceneTraced(Vector3 point, Vector3 way)
    {
        var best = float.MaxValue;
        foreach (var (low, high) in StepScene)
        {
            var (near, far) = (0f, float.MaxValue);
            var miss = false;
            for (int axis = 0; axis < 3 && !miss; axis++)
            {
                if (MathF.Abs(way[axis]) < 1e-9f)
                {
                    miss = point[axis] < low[axis] || point[axis] > high[axis];
                    continue;
                }
                var (a, b) = ((low[axis] - point[axis]) / way[axis], (high[axis] - point[axis]) / way[axis]);
                (near, far) = (MathF.Max(near, MathF.Min(a, b)), MathF.Min(far, MathF.Max(a, b)));
            }
            if (!miss && far >= near && near < best)
                best = near;
        }
        if (best > 100)
            return 0;
        var at = point + way * (best - 0.01f);
        return Math.Abs(at.X) < 8 && Math.Abs(at.Z) < 8 && at.Y < 12 ? StepSceneLight(at) : 0;
    }

    // The light a texel of an octahedron of texels along each side brings to point, traced along 8
    // by 8 ways spread across it, each weighed by its solid angle (octahedronShare).
    private static float StepTexelTraced(Vector3 point, int x, int y, int texels)
    {
        const int side = 8;
        var (sum, shares) = (0.0, 0.0);
        for (int j = 0; j < side; j++)
            for (int i = 0; i < side; i++)
            {
                var o = OctahedronPoint((x + (i + 0.5f) / side) / texels, (y + (j + 0.5f) / side) / texels);
                var share = 1 / Math.Pow(o.Length(), 3);
                sum += share * StepSceneTraced(point, Vector3.Normalize(o));
                shares += share;
            }
        return (float)(sum / shares);
    }

    [CpuKernelFact]
    public void A_Grazing_Way_Handed_Off_Over_A_Floor_Takes_What_The_Far_Probes_Over_It_See_Past_A_Step_It_Meets_Beyond_Its_End()
    {
        // The field's finer cascade of 0.08 a cell about the probe, so that the ray's end 0.37 over
        // the floor stands in open space (openAt), and its coarser of 0.25 over the scene. The
        // probe stands 0.5 over the floor, its rays reaching 2, and hands off to probes 2 apart
        // whose lower row stands inside the floor and upper row 1.75 over it, then to those 4 and
        // 8 apart. Its texels toward the step are read, 3.8 degrees up, 3.8 and 4.3 down, which
        // reach their end, and 13 down, which marches on to the floor, merged and each marched on
        // alone, against the light traced across each.
        Vector4[] field = [new(-2.2f, -1.2f, -2.56f, 0.08f), new(-8, -4, -8, 0.25f)];
        var cells = Cells(StepScene, field);
        const int each = Size * Size * Size;
        var light = new float[each * field.Length];
        for (int c = 0; c < field.Length; c++)
            for (int k = 0; k < each; k++)
                light[c * each + k] = StepSceneLight(new Vector3(field[c].X, field[c].Y, field[c].Z)
                    + (new Vector3(k % Size, k / Size % Size, k / (Size * Size)) + new Vector3(0.5f)) * field[c].W);
        var at = new Vector3(-1, 0.5f, 0);
        Level[] chain =
        [
            new(at, 1, 1, 1, 1, NearTexels, 2),
            new(new(-7, -2.25f, -7), 2, 8, 8, 8, 16, 4),
            new(new(-14, -10, -14), 4, 8, 8, 8, 16, 8),
            new(new(-28, -28, -28), 8, 8, 8, 8, 16, FarEnd),
        ];
        var setting = new Setting { FieldCascades = field, CellLight = light, SkyLight = 0 };
        var (merged, alone) = (Passes(cells, chain, at, setting), Passes(cells, chain, at, setting with { Reference = true }));
        (int X, int Y, string Way)[] row = [(15, 8, "3.8 up"), (15, 7, "3.8 down"), (14, 7, "4.3 down"), (14, 6, "13 down")];
        var read = row.Select(t => (t.Way, Traced: StepTexelTraced(at, t.X, t.Y, NearTexels), Merged: merged.Light[t.Y * NearTexels + t.X],
            Alone: alone.Light[t.Y * NearTexels + t.X])).ToArray();
        var said = $"toward the step {string.Join(", ", read.Select(r => $"{r.Way} reads {r.Merged:0.000} merged and {r.Alone:0.000} alone against {r.Traced:0.000} traced"))}, "
            + $"{merged}";
        output.WriteLine(said);
        // 3.8 degrees down reads 1.000 merged against 0.333 traced, 3.0 times it, where the far
        // probes about its end that count, the upper row's, see over the step the ray meets 2.76
        // past its end and take the court's light. The other ways and every way marched alone read
        // their trace.
        Assert.True(read[1].Merged > 2.5f * read[1].Traced && read.All(r => Math.Abs(r.Alone - r.Traced) <= 0.01f * r.Traced)
            && read.Where((r, i) => i != 1).All(r => Math.Abs(r.Merged - r.Traced) <= 0.02f * r.Traced), said);
    }
}
