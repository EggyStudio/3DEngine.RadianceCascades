using System.Globalization;
using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The closed room lit by one of its walls, drawn through the cascades beside the light traced to
/// each point, the picture the README opens with.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // The view from a corner of the room at half its height toward the far corner where the lit
    // wall meets the back wall, its half width at a unit along the view RoomSpan, and the gain the
    // sheet draws both captures at, which puts the walls' light about the middle of sRGB's range.
    private static readonly Vector3 RoomEye = new(3.6f, 4, 3.6f), RoomLook = new(-4, 3.6f, -4);
    private const float RoomSpan = 0.9f, RoomGain = 2.5f;
    private const int RoomWide = 560, RoomHigh = 420;

    // The gray of a capture's frame, 0.6 in sRGB at the sheet's gain, since the lit wall, drawn as
    // it sends, is white on the sheet's white ground and would lose the panel's edge.
    private const float RoomFrame = 0.13f;

    [CpuKernelFact]
    public void The_Room_Lit_By_A_Wall_Is_Written_Beside_Its_Trace_Where_RC_PICTURES_Names_A_Folder()
    {
        // dotnet test RadianceCascades.Tests with RC_PICTURES set to a folder writes there the room
        // as the cascades light it and as traced, each a PFM of linear light framed by a pixel of
        // gray, and room.json, the layout from which the Tools module's sheet.py sets the two side
        // by side, as python3 -I modules/Tools/sheets/sheet.py <folder>/room.json
        // <folder>/room.png. The room is the closed room's, its wall at -4 along x sending a light
        // of 1 and every other face lit by that wall alone. The chain is the closed room's with its
        // finest probes from 0.5 up, half a unit within the floor and the ceiling alike, since
        // points within a cell of the ceiling read nothing where the highest probes stand three
        // quarters of a unit under it. Each pixel's point stands half a cell off the room's edges,
        // as a surface's shading point stands off a seam, and 0.02 off its face.
        var folder = Environment.GetEnvironmentVariable("RC_PICTURES");
        if (string.IsNullOrEmpty(folder))
            return;
        Directory.CreateDirectory(folder);
        const float o = 4 + Wall;
        var cells = Field(OpenBox().Concat(Cuboid(new(-o, 8, -o), new(o, 8 + Wall, o), 6)));
        Level[] chain =
        [
            new(new(-3.5f, 0.5f, -3.5f), 1, 8, 8, 8, NearTexels, 2),
            new(FarCorner, FarSpacing, FarCount, FarCount, FarCount, FarTexels, 4),
            new(new(-14, -10, -14), 4, 8, 8, 8, 16, 8),
            new(new(-28, -28, -28), 8, 8, 8, 8, 16, FarEnd),
        ];
        var light = new float[Size * Size * Size];
        for (int k = 0; k < light.Length; k++)
            light[k] = (Corner + (new Vector3(k % Size, k / Size % Size, k / (Size * Size)) + new Vector3(0.5f)) * Cell).X < -3.75f ? 1 : 0;
        static (float Low, float High) Inside(int axis) => axis == 1 ? (0, 8) : (-4, 4);

        var forward = Vector3.Normalize(RoomLook - RoomEye);
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        var up = Vector3.Cross(right, forward);
        var (cascaded, traced) = (new float[RoomWide * RoomHigh], new float[RoomWide * RoomHigh]);
        var points = new List<(Vector3, Vector3)>();
        var pixels = new List<int>();
        for (int j = 0; j < RoomHigh; j++)
            for (int i = 0; i < RoomWide; i++)
            {
                var (u, v) = (((i + 0.5f) / RoomWide * 2 - 1) * RoomSpan, (1 - (j + 0.5f) / RoomHigh * 2) * RoomHigh / RoomWide * RoomSpan);
                var way = Vector3.Normalize(forward + right * u + up * v);
                var (t, face) = (float.MaxValue, 0);
                for (int a = 0; a < 3; a++)
                {
                    var (low, high) = Inside(a);
                    var s = way[a] > 1e-9f ? (high - RoomEye[a]) / way[a] : way[a] < -1e-9f ? (low - RoomEye[a]) / way[a] : float.MaxValue;
                    if (s < t)
                        (t, face) = (s, a);
                }
                if (face == 0 && way.X < 0)
                {
                    (cascaded[j * RoomWide + i], traced[j * RoomWide + i]) = (1, 1);
                    continue;
                }
                var normal = Vector3.Zero;
                normal[face] = way[face] > 0 ? -1 : 1;
                var point = RoomEye + way * t + normal * 0.02f;
                for (int a = 0; a < 3; a++)
                    if (a != face)
                        point[a] = Math.Clamp(point[a], Inside(a).Low + Cell / 2, Inside(a).High - Cell / 2);
                points.Add((point, normal));
                pixels.Add(j * RoomWide + i);
            }

        var all = points.ToArray();
        var reading = Passes(cells, chain, Floor, new Setting { Points = all, CellLight = light, SkyLight = 0 });
        var trace = new float[all.Length];
        Parallel.For(0, all.Length, k => trace[k] = TracedInClosedBox(all[k].Item1, all[k].Item2, light));
        for (int k = 0; k < all.Length; k++)
            (cascaded[pixels[k]], traced[pixels[k]]) = (Albedo / MathF.PI * reading.Taken[k], Albedo / MathF.PI * trace[k]);

        void Write(string name, float[] image)
        {
            var (wide, high) = (RoomWide + 2, RoomHigh + 2);
            using var file = File.Create(Path.Combine(folder, name));
            file.Write(System.Text.Encoding.ASCII.GetBytes($"Pf\n{wide} {high}\n-1.0\n"));
            // A PFM's rows run from the bottom up, little-endian where its scale is negative.
            for (int j = high - 1; j >= 0; j--)
                for (int i = 0; i < wide; i++)
                {
                    var inside = i > 0 && j > 0 && i < wide - 1 && j < high - 1;
                    var value = inside ? image[(j - 1) * RoomWide + i - 1] : RoomFrame;
                    var bytes = BitConverter.GetBytes(value);
                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);
                    file.Write(bytes);
                }
        }
        Write("cascades.pfm", cascaded);
        Write("traced.pfm", traced);
        File.WriteAllText(Path.Combine(folder, "room.json"), "{\"font\": 16, \"columns\": [\"lit by the cascades\", \"traced\"], "
            + $"\"rows\": [{{\"gain\": {RoomGain.ToString(CultureInfo.InvariantCulture)}, "
            + "\"panels\": [{\"image\": \"cascades.pfm\"}, {\"image\": \"traced.pfm\"}]}]}\n");

        // The cascades' light over every point within 5 percent of the trace's.
        var share = reading.Taken.Sum() / trace.Sum();
        var ratios = reading.Taken.Zip(trace, (a, b) => a / b).Order().ToArray();
        var said = $"over {all.Length} points the cascades take {share:0.000} of the trace's light, each point from "
            + $"{ratios[0]:0.00} to {ratios[^1]:0.00} of its own, the middle {ratios[ratios.Length / 2]:0.000}";
        output.WriteLine(said);
        Assert.InRange(share, 0.95f, 1.05f);
    }
}
