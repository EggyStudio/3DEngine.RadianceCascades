// The scene's distance field as its readers take it, the distance from a point to the nearest
// surface and the march of a ray through it to the first surface it meets, through an interface
// an engine implements over its own images, its samplers and the table of its cascades.
module rc_field_read;

__exported import rc_field_code;

// The field's cascades as a reader reads them, which an engine implements over its images.
public interface IFieldVolume
{
    // The finest cascade that holds p with margin of its cells to every side of it, or -1 where
    // none does.
    int cascadeOf(float3 p, float margin);

    // A cascade's least corner of its least cell in xyz and the width of its cells in w.
    float4 cornerAndCell(int cascade);

    // The cells along each side of a cascade.
    int size();

    // A cascade's distance in x and the plane a double-sided face keeps in yzw (sheetOf), blended
    // between the eight cells about the point given in cells from the cascade's corner, as a
    // sampler reads an image between its texels.
    float4 between(int cascade, float3 cells);

    // A cell's distance and plane as the resolve wrote them.
    float4 cellAt(int cascade, int3 cell);

    // The plane of a second double-sided face of another way a cell keeps where one passes as near
    // it, or none.
    float3 secondAt(int cascade, int3 cell);
}

// The steps of a march as it takes them, which a caller that keeps them implements and NoSteps
// passes over.
public interface IMarchSteps
{
    // A step at t along the ray, where the distance read is d.
    [mutating]
    void at(float t, float d);
}

// The steps of a march no caller keeps.
public struct NoSteps : IMarchSteps
{
    [mutating]
    public void at(float t, float d) {}
}

// How far either side of a double-sided face's plane the probes' rays take it to stand, in cells.
// Held a tenth either side, a closed room of sheets on the grid lets in more of the sky's light
// than held a quarter.
public static const float SheetHalf = 0.25;

// How near a surface a march comes before it has met it, and the least step it takes, in cells.
public static const float MarchHit = 0.1;
public static const float MarchStep = 0.25;

// How many steps a march takes at most before it has met nothing.
public static const int MarchSteps = 64;

// How many cells from a surface a cascade's distances are exact, the band the plan's cascades
// resolve to (SceneFieldPlan.Band), past which a distance reads that many cells whatever lies
// farther.
public static const float FieldBand = 4.0;

// The point p in cells from cascade i's corner, held half a cell from every side, so cascades
// stacked in one image do not bleed into each other.
float3 cellsOf<F : IFieldVolume>(F field, int i, float3 p)
{
    float4 oc = field.cornerAndCell(i);
    return clamp((p - oc.xyz) / oc.w, 0.5, float(field.size()) - 0.5);
}

// The distance from p to the nearest surface in cascade i, in world units, blended between the
// eight cells about it. A double-sided face is held below zero within half a cell either side where
// the cells about keep its plane, so a march meets a sheet wherever it lies between the cells'
// middles.
public float fieldDistanceIn<F : IFieldVolume>(F field, int i, float3 p)
{
    float4 read = field.between(i, cellsOf(field, i, p));
    return read.r - 0.5 * field.cornerAndCell(i).w * saturate(length(read.yzw));
}

// The field's normal at p in cascade i, the way its distance (fieldDistanceIn) grows fastest,
// read across half a cell either side of p along each axis, of unit length, or none where the
// distance does not change about p. A binding that shades the surface a ray met reads it there.
public float3 fieldNormalIn<F : IFieldVolume>(F field, int i, float3 p)
{
    float h = field.cornerAndCell(i).w * 0.5;
    float3 g = float3(
        fieldDistanceIn(field, i, p + float3(h, 0.0, 0.0)) - fieldDistanceIn(field, i, p - float3(h, 0.0, 0.0)),
        fieldDistanceIn(field, i, p + float3(0.0, h, 0.0)) - fieldDistanceIn(field, i, p - float3(0.0, h, 0.0)),
        fieldDistanceIn(field, i, p + float3(0.0, 0.0, h)) - fieldDistanceIn(field, i, p - float3(0.0, 0.0, h)));
    float l = length(g);
    return l > 1e-6 ? g / l : float3(0.0);
}

// How far a point local off a cell's middle lies from the plane the cell keeps as sheet, held
// SheetHalf of a cell either side, or far where it keeps none.
public float sheetDistance(float3 sheet, float3 local, float cell)
{
    float kept = length(sheet);
    return kept < 0.5 ? 1e6 : abs(dot(local, sheet) / kept - (kept - SheetLength) * cell) - SheetHalf * cell;
}

// The distance from p to the nearest surface in cascade i as the probes' rays take it, the cells'
// blend or p's distance from the plane of a double-sided face the cell it lies in keeps, held
// SheetHalf of a cell either side, where nearer, and the second plane the cell keeps where two
// walls cross. A ray so meets a sheet thinner than a cell at its plane from either side wherever it
// lies between the cells' middles, and two sheets two cells apart leave the cells between them
// open.
public float fieldDistanceSheetIn<F : IFieldVolume>(F field, int i, float3 p)
{
    float4 oc = field.cornerAndCell(i);
    float3 cells = cellsOf(field, i, p);
    float4 read = field.between(i, cells);
    float d = read.r;
    // The planes blended with the rest read nothing where no cell about keeps one, so the cell's
    // own is read only near a sheet.
    [branch]
    if (d < oc.w * 0.75 && dot(read.yzw, read.yzw) >= 1e-6)
    {
        int3 at = int3(min(floor(cells), float(field.size()) - 1.0));
        float3 local = p - oc.xyz - (float3(at) + 0.5) * oc.w;
        d = min(d, min(sheetDistance(field.cellAt(i, at).yzw, local, oc.w), sheetDistance(field.secondAt(i, at), local, oc.w)));
    }
    return d;
}

// Where a ray from origin along way first comes within MarchHit of a cell of a surface between tMin
// and tMax, through the finest cascade that holds each point along it, each step the distance read
// or MarchStep of a cell at least, or -1 where it reaches tMax first, leaves every cascade or takes
// MarchSteps steps, each step handed to steps.
public float traceField<F : IFieldVolume, S : IMarchSteps>(F field, float3 origin, float3 way, float tMin, float tMax, inout S steps)
{
    float t = tMin;
    for (int i = 0; i < MarchSteps && t < tMax; i++)
    {
        float3 p = origin + way * t;
        int c = field.cascadeOf(p, 1.0);
        if (c < 0)
            return -1.0;
        float cell = field.cornerAndCell(c).w;
        float d = fieldDistanceIn(field, c, p);
        steps.at(t, d);
        if (d < cell * MarchHit)
            return t;
        t += max(d, cell * MarchStep);
    }
    return -1.0;
}

// The same with no steps kept.
public float traceField<F : IFieldVolume>(F field, float3 origin, float3 way, float tMin, float tMax)
{
    NoSteps none;
    return traceField(field, origin, way, tMin, tMax, none);
}

// The same as the probes' rays take the field (fieldDistanceSheetIn).
public float traceFieldSheets<F : IFieldVolume, S : IMarchSteps>(F field, float3 origin, float3 way, float tMin, float tMax, inout S steps)
{
    float t = tMin;
    for (int i = 0; i < MarchSteps && t < tMax; i++)
    {
        float3 p = origin + way * t;
        int c = field.cascadeOf(p, 1.0);
        if (c < 0)
            return -1.0;
        float cell = field.cornerAndCell(c).w;
        float d = fieldDistanceSheetIn(field, c, p);
        steps.at(t, d);
        if (d < cell * MarchHit)
            return t;
        t += max(d, cell * MarchStep);
    }
    return -1.0;
}

// The same with no steps kept.
public float traceFieldSheets<F : IFieldVolume>(F field, float3 origin, float3 way, float tMin, float tMax)
{
    NoSteps none;
    return traceFieldSheets(field, origin, way, tMin, tMax, none);
}
