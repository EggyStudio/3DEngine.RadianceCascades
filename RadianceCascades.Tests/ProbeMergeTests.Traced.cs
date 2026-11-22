using System.Numerics;

namespace RadianceCascades.Tests;

/// <summary>
/// The light traced to a point among boxes in the test's one cascade of the field, against which
/// the thin wall's rooms and the ladder's scenes read the merges.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // Where along way from point the ray first enters one of the boxes, or float.MaxValue where it
    // meets none.
    private static float FirstBox((Vector3 Low, Vector3 High)[] boxes, Vector3 point, Vector3 way)
    {
        var best = float.MaxValue;
        foreach (var (low, high) in boxes)
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
        return best;
    }

    // Where along way from point the ray leaves the test's one cascade of the field.
    private static float LeavesField(Vector3 point, Vector3 way)
    {
        var high = Corner + new Vector3(Size * Cell);
        var t = float.MaxValue;
        for (int axis = 0; axis < 3; axis++)
            if (way[axis] > 1e-9f)
                t = MathF.Min(t, (high[axis] - point[axis]) / way[axis]);
            else if (way[axis] < -1e-9f)
                t = MathF.Min(t, (Corner[axis] - point[axis]) / way[axis]);
        return t;
    }

    // What a point facing normal takes among the boxes, over 96 by 96 ways spread by their cosine,
    // each meeting a box taking the light of the cell of the field it meets it in, as the passes'
    // hits take it (CellLight), and each leaving the field first the sky's.
    private static float TracedAmongBoxes((Vector3 Low, Vector3 High)[] boxes, Vector3 point, Vector3 normal, float[] light, float sky)
    {
        var (u, v) = Basis(normal);
        const int side = 96;
        var rows = new double[side];
        Parallel.For(0, side, i =>
        {
            for (int j = 0; j < side; j++)
            {
                var way = CosineWay(normal, u, v, (i + 0.5f) / side, (j + 0.5f) / side);
                var t = FirstBox(boxes, point, way);
                rows[i] += t < LeavesField(point, way) ? light[ClosedCellOf(point + way * (t - 0.01f))] : sky;
            }
        });
        return (float)(MathF.PI * rows.Sum() / (side * side));
    }

    // The light each cell of the test's one cascade of the field takes by its middle's place.
    private static float[] CellLightBy(Func<Vector3, float> light)
    {
        var cells = new float[Size * Size * Size];
        for (int k = 0; k < cells.Length; k++)
            cells[k] = light(Corner + (new Vector3(k % Size, k / Size % Size, k / (Size * Size)) + new Vector3(0.5f)) * Cell);
        return cells;
    }
}
