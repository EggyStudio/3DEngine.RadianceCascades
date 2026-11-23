// The stamp of the boxes the moving meshes are stamped as into one cascade of the scene's distance
// field, over what the still meshes made. Each cell takes the least of its still distance and its
// distance to each box that comes within its brick, and is painted the color and the emission of
// the nearer surface, the still meshes' or a box's, as the build paints a cell its nearest
// triangle's, so indirect light from a moving mesh takes its color and a cell a box painted takes
// the still meshes' again once the box has left. An engine runs a thread a cell of each brick and
// hands the stamp the shapes of that brick's run.
module rc_field_stamp;

__exported import rc_field_splat;

// A shape as the stamp reads it, as FieldShape.WriteStamp writes it in C#, eight groups of four.
public struct StampShape
{
    // The rows of the matrix from the world into the shape's own space, a 3 by 4.
    public float4 row0;
    public float4 row1;
    public float4 row2;

    // xyz: the box's middle in its own space. w: the least scale from its own space to the world,
    // which a distance there is turned into a world distance no longer than the true one by.
    public float4 centerAndScale;

    // xyz: the box's half extents in its own space.
    public float4 extents;

    // rgb: the mesh's color, linear. w: the share of light that passes a unit of its depth.
    public float4 color;

    // rgb: the mesh's emission, linear, none where the engine carries it as a glow light. w: the
    // share of its diffuse light it sends through to its far side.
    public float4 emission;

    // xyz: the mesh's perceptual roughness, metallic share and reflectance.
    public float4 gloss;
}

// One cell of the field as a reader reads it, its distance and the plane of a double-sided face it
// keeps (sheetOf), its paint, its emission times the paint's alpha, its face's way and gloss
// (glossBits), and the share of its diffuse light its surface sends through to its far side.
public struct FieldCell
{
    public float distance;
    public float3 sheet;
    public float4 paint;
    public float4 glow;
    public uint face;
    public float through;
}

// The shapes of a brick's run, which an engine implements over its buffers.
public interface IStampShapes
{
    // How many shapes come within the brick.
    uint count();

    // The shape at a place of the run.
    StampShape shape(uint i);
}

// The cell whose middle is p, of a cascade whose cells are cellWidth across, as the shapes given
// stamp it over the still cell, the distance held to reach either side, the nearer surface painting
// it and giving it its face and the share of its diffuse light it sends through, which a still cell
// keeps as it came. A box nearer than the still surface takes away the plane the still cell kept,
// and a box thinner than a cell along an axis of its own is stamped a cell across there.
public FieldCell stampCell<S : IStampShapes>(S shapes, float3 p, float cellWidth, float reach, FieldCell still)
{
    FieldCell cell = still;
    float nearest = abs(still.distance);
    for (uint i = 0; i < shapes.count(); i++)
    {
        StampShape shape = shapes.shape(i);
        float4 h = float4(p, 1.0);
        float3 own = float3(dot(shape.row0, h), dot(shape.row1, h), dot(shape.row2, h)) - shape.centerAndScale.xyz;
        // A march meets a surface within a tenth of a cell of it (MarchHit), so a box thinner than
        // a cell would hold only where a cell's middle lies in it or within that tenth, and a ray
        // would pass a moving wall a tenth of a unit thick between two cells' middles. Held a cell
        // across along each axis, it holds some cell's middle wherever it stands. A world length
        // along the way of an axis's row is that length times the row's length in its own space.
        float3 rows = float3(length(shape.row0.xyz), length(shape.row1.xyz), length(shape.row2.xyz));
        float3 q = abs(own) - max(shape.extents.xyz, 0.5 * cellWidth * rows);
        float box = (length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0)) * shape.centerAndScale.w;
        cell.distance = min(cell.distance, max(box, -reach));
        if (abs(box) >= min(nearest, reach))
            continue;
        nearest = abs(box);
        cell.sheet = float3(0.0);
        float coverage = lightThroughPaint(shape.color.w);
        cell.paint = float4(shape.color.rgb * coverage, coverage);
        cell.glow = float4(shape.emission.rgb * coverage, 0.0);
        cell.through = saturate(shape.emission.w);
        // The box's face nearest the cell, the axis it lies furthest out along, its way in the
        // world the gradient of that axis of its own space.
        int axis = q.x >= q.y && q.x >= q.z ? 0 : q.y >= q.z ? 1 : 2;
        float3 outward = float3(axis == 0 ? sign(own.x) : 0.0, axis == 1 ? sign(own.y) : 0.0, axis == 2 ? sign(own.z) : 0.0);
        float3 way = normalize(shape.row0.xyz * outward.x + shape.row1.xyz * outward.y + shape.row2.xyz * outward.z);
        cell.face = wayCode(way) | glossBits(shape.gloss.x, shape.gloss.y, shape.gloss.z);
    }
    return cell;
}
