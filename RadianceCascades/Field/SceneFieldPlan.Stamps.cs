using System.Numerics;

namespace RadianceCascades;

// The shapes the moving meshes are stamped as, and the parts each is stamped as.
public sealed partial class SceneFieldPlan<TMesh, TTag> where TMesh : notnull
{
    // The parts a mesh is stamped as where there are no more than room for, which leaves a box each
    // for the meshes farther from the eye, and the box around all of it where there are, so a crowd
    // of figures past what a frame stamps is stamped as figures near the eye and boxes beyond.
    private FieldPart[] PartsWithin(Instance instance, int room)
    {
        if ((instance.Parts ?? _parts(instance.Vertices)) is { } parts && parts.Length <= room) return parts;
        if (_whole.TryGetValue(instance.Vertices, out var whole)) return whole;
        if (_whole.Count > 4096) _whole.Clear();
        return _whole[instance.Vertices] = [new FieldPart(_bounds(instance.Vertices), Matrix4x4.Identity)];
    }

    // The box around all of each mesh as its one part, kept for its vertices.
    private readonly Dictionary<TMesh, FieldPart[]> _whole = [];
}
