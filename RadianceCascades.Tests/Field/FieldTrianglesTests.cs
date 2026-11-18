using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests.Field;

// The triangles laid out as the splat reads them, twelve uints a triangle, and the pool's size.
public sealed class FieldTrianglesTests
{
    private static readonly Vector3[] Corners = [new(1, 2, 3), new(-0.5f, 0, 4), new(0, 8, -2)];

    [Fact]
    public void A_Triangle_Is_Twelve_Uints_Each_Corner_Its_Place_In_Bits_Then_Its_Color_With_The_Open_Edge_Above()
    {
        uint[] colors = [FieldTriangles.Srgb(0x11, 0x22, 0x33), FieldTriangles.Srgb(0x44, 0x55, 0x66), FieldTriangles.Srgb(0x77, 0x88, 0x99)];
        var into = new uint[FieldTriangles.Uints + 1];
        Assert.Equal(1, FieldTriangles.Write(into, Corners, [0, 1, 2], colors));
        for (int corner = 0; corner < 3; corner++)
        {
            Assert.Equal(BitConverter.SingleToUInt32Bits(Corners[corner].X), into[corner * 4]);
            Assert.Equal(BitConverter.SingleToUInt32Bits(Corners[corner].Y), into[corner * 4 + 1]);
            Assert.Equal(BitConverter.SingleToUInt32Bits(Corners[corner].Z), into[corner * 4 + 2]);
            Assert.Equal(colors[corner], into[corner * 4 + 3] & FieldTriangles.White);
            Assert.True(into[corner * 4 + 3] >> 24 == 1, $"corner {corner}'s edge is open, a lone triangle's every edge being its own");
        }
        Assert.True(into[FieldTriangles.Uints] == 0, "a triangle writes its twelve uints and no more");
    }

    [Fact]
    public void A_Color_Is_Three_Srgb_Bytes_Red_Lowest()
    {
        Assert.Equal(0x332211u, FieldTriangles.Srgb(0x11, 0x22, 0x33));
        Assert.Equal(FieldTriangles.White, FieldTriangles.Srgb(255, 255, 255));
    }

    [Fact]
    public void Two_Triangles_Sharing_An_Edge_Mark_It_Closed_In_Both_And_Their_Rim_Open()
    {
        Vector3[] square = [new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0)];
        uint[] indices = [0, 1, 2, 0, 2, 3];
        var into = new uint[2 * FieldTriangles.Uints];
        FieldTriangles.Write(into, square, indices, []);
        var open = FieldTriangles.OpenEdges(square, indices);
        Assert.Equal([true, true, false, false, true, true], open);
        for (int corner = 0; corner < 6; corner++)
            Assert.True((into[corner * 4 + 3] >> 24 == 1) == open[corner], $"corner {corner}'s bit is its edge's to the next corner");
    }

    [Fact]
    public void A_Mesh_Given_No_Colors_Is_White()
    {
        var into = new uint[FieldTriangles.Uints];
        FieldTriangles.Write(into, Corners, [0, 1, 2], []);
        for (int corner = 0; corner < 3; corner++)
            Assert.Equal(FieldTriangles.White, into[corner * 4 + 3] & FieldTriangles.White);
    }

    [Fact]
    public void Colors_Of_Another_Count_Than_The_Vertices_Are_Refused_With_Both_Counts()
    {
        var into = new uint[FieldTriangles.Uints];
        var refused = Assert.Throws<ArgumentException>(() => FieldTriangles.Write(into, Corners, [0, 1, 2], [FieldTriangles.White]));
        Assert.Contains("1 color", refused.Message);
        Assert.Contains("3 vertices", refused.Message);
    }

    [Fact]
    public void A_Pool_For_A_Build_Past_Half_A_Billion_Triangles_Holds_The_Build()
    {
        const int building = 1 << 30;
        Assert.True(FieldTriangles.PoolCapacity(building, 1024, null) >= building, "twice the build is past the uints an index holds, and the pool holds the build at least");
        Assert.True(FieldTriangles.PoolCapacity(int.MaxValue, 1024, null) > 0, "no build overflows the pool's size");
    }
}
