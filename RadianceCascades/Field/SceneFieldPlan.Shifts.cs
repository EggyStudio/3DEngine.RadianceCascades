using System.Numerics;

namespace RadianceCascades;

// A cascade the eye moved by whole cells along one axis shifted as a clipmap is, the cells it keeps
// moved on the GPU and the slab of cells it brings in built alone from the still meshes within the
// band of them, rather than the whole cascade built again from every still mesh within its band.
public sealed partial class SceneFieldPlan<TMesh, TTag> where TMesh : notnull where TTag : IEquatable<TTag>
{
    // Whether Rebuild marked every cascade to be built whole this frame.
    private bool _forced;

    // The shift of cascade `cascade` toward `target`, its corner moved along the first axis that
    // differs and the slab it brings in built alone, or null where it is to be built whole, as one
    // never built, one a still mesh came into or left, one a rebuild was asked for, or one whose
    // corner moved half the cascade or more, past which the kept cells are fewer than the slab's.
    private Job? ShiftOf(int cascade, Vector3 target)
    {
        if (_forced || Options.WholeBuilds || _built[cascade] is not { } built || _changed[cascade] is not null) return null;
        var cell = CellOf(cascade);
        var moved = (target - built) / cell;
        int[] cells = [(int)MathF.Round(moved.X), (int)MathF.Round(moved.Y), (int)MathF.Round(moved.Z)];
        if (cells.Any(along => Math.Abs(along) >= Resolution / 2)) return null;
        var axis = Array.FindIndex(cells, along => along != 0);
        if (axis < 0) return null;
        var by = cells[axis];
        var origin = built;
        origin[axis] = target[axis];
        // The slab at the side the cascade moved toward, its cells new to it.
        var (from, to) = by > 0 ? (Resolution - by, Resolution - 1) : (0, -by - 1);
        var top = Resolution - 1;
        var slab = axis switch
        {
            0 => new FieldCells(from, 0, 0, to, top, top),
            1 => new FieldCells(0, from, 0, top, to, top),
            _ => new FieldCells(0, 0, from, top, top, to),
        };
        return new Job { Cascade = cascade, Origin = origin, Reach = ReachOf(cascade, origin, slab), Cells = slab, Shifting = true, ShiftAxis = axis, ShiftCells = by };
    }

    // Where the still meshes a build of cells splats lie, within the band of its cells and of the
    // cells around them, which a cell's distance is read beside where the resolve pinches it.
    private FieldBox ReachOf(int cascade, Vector3 origin, FieldCells cells)
    {
        var cell = CellOf(cascade);
        var low = origin + new Vector3(cells.X0 - 1, cells.Y0 - 1, cells.Z0 - 1) * cell;
        var high = origin + new Vector3(cells.X1 + 2, cells.Y1 + 2, cells.Z1 + 2) * cell;
        return new FieldBox(low, high).Grown(Band * cell);
    }

    // What a mesh a build of cells splats costs of a frame's triangle budget, its triangles times
    // the share of its bounds within the build's reach, and an eighth of them at least, since a
    // triangle whose band misses the cells is passed over at the start, at a small share of the
    // cost of one splatted, and a large mesh crossing a slab whole, counted whole, would spend the
    // budget on the few of its triangles within it.
    private int Cost(Instance instance, Job job)
    {
        var triangles = _triangles(instance.Vertices);
        if (job.Cells is null) return triangles;
        var bounds = _still.TryGetValue(instance, out var still) ? still.Bounds : _bounds(instance.Vertices).Transformed(instance.World);
        var cell = new Vector3(CellOf(job.Cascade));
        var size = Vector3.Max(bounds.Max - bounds.Min, cell);
        var within = Vector3.Clamp(Vector3.Min(bounds.Max, job.Reach.Max) - Vector3.Max(bounds.Min, job.Reach.Min), cell, size);
        var share = within.X / size.X * (within.Y / size.Y) * (within.Z / size.Z);
        return Math.Max(1, (int)MathF.Ceiling(triangles * MathF.Max(share, 0.125f)));
    }
}
