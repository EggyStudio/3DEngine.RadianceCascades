using System.Numerics;
using System.Runtime.CompilerServices;

namespace RadianceCascades.Tests.Field;

/// <summary>
/// A mesh as the plan's tests hand it to the plan, its vertices' positions and its triangles'
/// indices, told apart by reference as an array is.
/// </summary>
internal sealed class TestMesh(Vector3[] positions, uint[]? indices = null)
{
    /// <summary>The places of the mesh's vertices in its own space.</summary>
    public Vector3[] Positions { get; } = positions;

    /// <summary>
    /// The vertices of the mesh's triangles, three a triangle, none for a mesh of corners alone.
    /// </summary>
    public uint[] Indices { get; } = indices ?? [];

    // Named by its identity, which a failure that compares two meshes by reference shows.
    public override string ToString() => $"mesh {RuntimeHelpers.GetHashCode(this)} of {Positions.Length} positions";
}
