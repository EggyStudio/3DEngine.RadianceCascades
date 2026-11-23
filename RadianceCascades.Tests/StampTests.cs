using System.Numerics;
using Xunit;
using Xunit.Abstractions;

namespace RadianceCascades.Tests;

/// <summary>
/// The stamp of a moving mesh's box into a cascade of the field over what the still meshes made,
/// the shape written by <see cref="FieldShape.WriteStamp"/> and stamped on the CPU from the
/// library's Slang.
/// </summary>
public sealed class StampTests(ITestOutputHelper output)
{
    private const int Size = 32;
    private const float Cell = 0.25f, Reach = 4 * Cell;
    private static readonly Vector3 Red = new(0.8f, 0.1f, 0.1f);

    private static Vector3 Middle(int x, int y, int z) => new Vector3(-Size * Cell / 2) + (new Vector3(x, y, z) + new Vector3(0.5f)) * Cell;

    [CpuKernelFact]
    public void A_Turned_Box_Is_Stamped_At_Its_Distance_And_Its_Color_And_A_Nearer_Still_Surface_Keeps_Its_Own()
    {
        // A box of half sides 1, 0.5 and 0.75 turned 30 degrees about y and moved off the origin.
        var world = Matrix4x4.CreateRotationY(MathF.PI / 6) * Matrix4x4.CreateTranslation(0.3f, -0.2f, 0.1f);
        var half = new Vector3(1, 0.5f, 0.75f);
        var shape = new FieldShape(world, Vector3.Zero, half, 1, Red, 0, Vector3.Zero, new Vector3(0.4f, 0, 0.5f), 1.5f);
        var floats = new float[1 + FieldShape.StampFloats];
        floats[0] = 1;
        shape.WriteStamp(floats.AsSpan(1));

        var still = Enumerable.Repeat(Reach, Size * Size * Size).ToArray();
        var stillPaints = new float[4 * Size * Size * Size];
        // One cell beside the box held nearer a still surface of its own, painted green.
        var kept = (16 * Size + 16) * Size + 22;
        still[kept] = 0.01f;
        (stillPaints[kept * 4], stillPaints[kept * 4 + 1], stillPaints[kept * 4 + 2], stillPaints[kept * 4 + 3]) = (0.1f, 0.6f, 0.1f, 1);
        var (distances, paints, faces) = (new float[Size * Size * Size], new float[4 * Size * Size * Size], new uint[Size * Size * Size]);
        var through = new float[Size * Size * Size];
        CpuKernel.Run("stamp_cpu.slang", (Size / 4, Size / 4, Size / 4), floats, still, stillPaints, distances, paints, faces, through,
            new[] { Cell, Cell });

        Matrix4x4.Invert(world, out var toOwn);
        int within = 0, faults = 0;
        string first = "";
        for (int z = 0; z < Size; z++)
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var index = (z * Size + y) * Size + x;
                    if (index == kept)
                        continue;
                    var q = Vector3.Abs(Vector3.Transform(Middle(x, y, z), toOwn)) - half;
                    var exact = Vector3.Max(q, Vector3.Zero).Length() + MathF.Min(MathF.Max(q.X, MathF.Max(q.Y, q.Z)), 0);
                    if (MathF.Abs(exact) >= Reach - Cell)
                        continue;
                    within++;
                    var paint = new Vector3(paints[index * 4], paints[index * 4 + 1], paints[index * 4 + 2]);
                    // The box sends through 1.5 of its light, which the stamp holds to the whole.
                    if (MathF.Abs(distances[index] - exact) <= 1e-4f && Vector3.Distance(paint, Red) <= 1e-4f && through[index] == 1)
                        continue;
                    if (faults++ == 0)
                        first = $"cell ({x}, {y}, {z}) stamped {distances[index]} painted {paint} sending {through[index]} through where the box is {exact} away, red and sends all";
                }
        Assert.True(within > 1_000, $"only {within} cells lay within the band of the box");
        Assert.True(faults == 0, $"{faults} of {within} cells stamped otherwise, the first {first}");
        Assert.True(distances[kept] == 0.01f && paints[kept * 4 + 1] == 0.6f && through[kept] == 0.25f,
            $"the cell nearer its still surface took {distances[kept]}, {paints[kept * 4 + 1]} of green and sends {through[kept]} through");
        // The cell above the box's top, within the band, takes the box's face and its gloss.
        Assert.True((faces[(16 * Size + 18) * Size + 16] >> 8 & 255) == 102, "a stamped face keeps the shape's gloss above its way");
    }

    [CpuKernelFact]
    public void A_Moving_Wall_Thinner_Than_A_Cell_Is_Stamped_A_Cell_Across_At_Each_Cascade_So_A_March_Meets_It_Wherever_It_Stands()
    {
        // A wall a tenth of a unit thick, its middle at eight places across a cell, stamped into a
        // cascade of each cell an engine's field takes, 0.125 to 2. Stamped at its own thickness,
        // a march along a row of cells through it, meeting a surface within a tenth of a cell,
        // meets it only where a cell's middle lies in it or within that tenth, and passes it at
        // most places from the cell of 0.25 on. Stamped a cell across, a cell's middle lies in it
        // at every place, and the row reads the distance to a wall a cell thick, its faces standing
        // out from the wall's by half a cell less half the wall.
        const float Thick = 0.1f;
        var said = new List<string>();
        var faults = new List<string>();
        foreach (var cell in new[] { 0.125f, 0.25f, 0.5f, 1, 2 })
        {
            var reach = 4 * cell;
            int passed = 0;
            for (int k = 0; k < 8; k++)
            {
                var middle = k * cell / 8;
                var shape = new FieldShape(Matrix4x4.CreateTranslation(middle, 0, 0), Vector3.Zero, new Vector3(Thick / 2, 1000, 1000), 1, Red, 0,
                    Vector3.Zero, new Vector3(0.4f, 0, 0.5f), 0);
                var floats = new float[1 + FieldShape.StampFloats];
                floats[0] = 1;
                shape.WriteStamp(floats.AsSpan(1));
                float[] Row(float hold)
                {
                    var still = Enumerable.Repeat(reach, Size * Size * Size).ToArray();
                    var distances = new float[Size * Size * Size];
                    CpuKernel.Run("stamp_cpu.slang", (Size / 4, Size / 4, Size / 4), floats, still, new float[4 * Size * Size * Size], distances,
                        new float[4 * Size * Size * Size], new uint[Size * Size * Size], new float[Size * Size * Size], new[] { cell, hold });
                    return [.. Enumerable.Range(0, Size).Select(x => distances[(16 * Size + 16) * Size + x])];
                }
                if (Row(0).Min() >= 0.1f * cell)
                    passed++;
                var held = Row(cell);
                var half = MathF.Max(Thick / 2, cell / 2);
                for (int x = 0; x < Size; x++)
                {
                    var exact = Math.Clamp(MathF.Abs(-Size * cell / 2 + (x + 0.5f) * cell - middle) - half, -reach, reach);
                    if (MathF.Abs(held[x] - exact) > 1e-4f * cell)
                        faults.Add($"cell {cell} wall at {middle} cell {x} stamped {held[x]} where a wall a cell across is {exact} away");
                }
                if (held.Min() > 0)
                    faults.Add($"cell {cell} wall at {middle} no cell's middle in the held wall");
            }
            said.Add($"at a cell of {cell} a march passes the wall stamped at its own thickness at {passed} of 8 places, and its faces "
                + $"stamped a cell across stand {MathF.Max(cell - Thick, 0) / 2:0.0000} out");
            if (cell >= 0.25f && passed == 0)
                faults.Add($"cell {cell} the wall at its own thickness held at every place");
        }
        output.WriteLine(string.Join("; ", said));
        Assert.True(faults.Count == 0, string.Join("; ", faults.Take(4)));
    }
}
