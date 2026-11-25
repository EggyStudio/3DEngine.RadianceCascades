// Rough reflections read from the cascades, whole each frame and with no history. A rough surface
// reflects the light of a cone of ways about the mirror of the way toward the eye, the lobe of a
// GGX surface of its roughness, and a probe's merged texels are that light by way, so the read
// takes the cone from the probes about the point and traces nothing. The read stands behind a
// shape of its own (IConeRead), so a second way of reading the cone stands beside the first and
// is read against it as the merges are, and the module's tests read it against the cone traced
// with many rays. It gives the lobe's mean light, the light a mirror would reflect blurred by the
// lobe, which a binding scales by its own reflectance, as a split sum scales its filtered light.
module rc_cone_read;

__exported import rc_cascade_merge;

// The points whose cones are read, a thread a point.
public interface IConePoints
{
    // Where the point lies.
    float3 placeAt(uint index);

    // The normal of its surface.
    float3 normalAt(uint index);

    // The way from the point toward the eye.
    float3 viewAt(uint index);

    // Its surface's roughness, from 0 to 1, whose square is the lobe's alpha.
    float roughnessAt(uint index);

    // The level its gather reads, as gatheredLevel picks it at GatherRim.
    int levelAt(uint index);

    // Keeps the light its cone reflects.
    void keepReflected(uint index, float3 light);
}

// The shape every read of a cone takes, one kernel behind one name. A read brings the mean light
// of the cone a surface at p of normal n and roughness reflects toward v, the way toward the eye,
// from the levels about p, the finest of which its gather reads.
public interface IConeRead
{
    float3 light<L : IGatherLevels>(L levels, int finest, float3 p, float3 n, float3 v, float roughness);
}

// The share of the lobe's weight under which a texel is not read. The weight is taken at the
// texel's middle way against the lobe's at the mirror way.
public static const float ConeLeast = 0.001;

// The weight the lobe of alpha about the mirror of v in n gives a way l, the density over the ways
// with which a GGX surface reflects light toward v, D(h) times n.h over 4 v.h, and none for a way
// behind the surface. The traced cone samples ways by the same density, so the two read one lobe.
public float coneWeight(float3 n, float3 v, float3 l, float alpha)
{
    if (dot(n, l) <= 0.0)
        return 0.0;
    float3 h = normalize(v + l);
    float nh = max(dot(n, h), 0.0);
    float a2 = max(alpha * alpha, 1e-8);
    float d = nh * nh * (a2 - 1.0) + 1.0;
    return a2 / (3.14159265 * d * d) * nh / (4.0 * max(dot(v, h), 1e-6));
}

// The least alpha a cone is read at from texels of `texels` along a side, whose lobe gives a
// texel's neighbors a quarter of its own weight, so a cone narrower than a texel is read as wide
// as one and its light does not jump from texel to texel as the mirror way crosses them. It is half
// a texel's mean width, the square root of pi over the texels along a side.
public float coneLeastAlpha(uint texels)
{
    return 1.7724539 / float(max(texels, 1u));
}

// The level a cone of alpha is read from, the finest from `finest` up whose texels are as narrow
// as the cone (coneLeastAlpha), and where none is, the first of those with the most texels, the
// narrowest a cone can be read at. A coarser level's probes stand farther apart, so a cone is read
// no coarser than its width asks.
public int coneLevel<L : IGatherLevels>(L levels, int finest, float alpha)
{
    int best = finest;
    uint most = levels.cascadeOf(finest).texels;
    [loop]
    for (int level = finest; level < levels.levels(); level++)
    {
        uint texels = levels.cascadeOf(level).texels;
        if (coneLeastAlpha(texels) <= alpha)
            return level;
        if (texels > most)
        {
            best = level;
            most = texels;
        }
    }
    return best;
}

// The trilinear weight of the corner at offset of the eight probes about a place lying at f between
// them (farProbesAbout).
public float coneCornerWeight(float3 f, int3 offset)
{
    float3 lerps = lerp(1.0 - f, f, float3(offset));
    return lerps.x * lerps.y * lerps.z;
}

// The cone read at the probes about p. The level is the one the cone's width chooses (coneLevel),
// and the lobe is no narrower than that level's texels (coneLeastAlpha). Each probe is weighed by
// its trilinear weight on the level's grid, and by nothing where it stands inside a mesh or does
// not see p from its place (farProbeSees), as the gather weighs it. Each texel is weighed by the
// lobe at its middle way times its solid angle, the probes' light read only where that weight is
// above ConeLeast of the lobe's at the mirror way, and the light is their sum over the weights.
// None where no probe counts. A probe's light is the light at its own place, so a cone read a
// spacing off p takes the light at the probe's place, as the gather does.
public struct ConeAtProbes : IConeRead
{
    public float3 light<L : IGatherLevels>(L levels, int finest, float3 p, float3 n, float3 v, float roughness)
    {
        float alpha = roughness * roughness;
        int level = coneLevel(levels, finest, alpha);
        ProbeCascade cascade = levels.cascadeOf(level);
        float read = max(alpha, coneLeastAlpha(cascade.texels));
        GatherLevelProbes<L> probes = { levels, level };
        int3 base;
        float3 f;
        farProbesAbout(cascade, p, base, f);
        // The probes that count, a bit a corner, their weights made again from f where they are
        // read, so the read keeps no array in a thread's own memory.
        uint seen = 0u;
        float counted = 0.0;
        [loop]
        for (int corner = 0; corner < 8; corner++)
        {
            int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
            int3 probe = base + offset;
            float weight = coneCornerWeight(f, offset);
            if (any(probe < 0) || any(probe >= cascade.count) || weight <= 0.0
                || !farProbeSees(probes, probe, levels.placeOf(level, probe), p, SeeingSlack * cascade.spacing))
                continue;
            seen |= 1u << uint(corner);
            counted += weight;
        }
        if (counted <= 0.0)
            return float3(0.0);
        float3 toward = normalize(v);
        float3 mirror = reflect(-toward, n);
        float least = coneWeight(n, toward, mirror, read) * ConeLeast;
        float shares = octahedronShares(cascade.texels);
        float3 taken = float3(0.0);
        float lobes = 0.0;
        [loop]
        for (uint k = 0u; k < cascade.texels * cascade.texels; k++)
        {
            uint2 texel = uint2(k % cascade.texels, k / cascade.texels);
            float lobe = coneWeight(n, toward, texelWay(cascade.texels, texel), read);
            if (lobe <= least)
                continue;
            lobe *= texelSolidAngle(cascade.texels, texel, shares);
            float3 sum = float3(0.0);
            [loop]
            for (int corner = 0; corner < 8; corner++)
                if ((seen & (1u << uint(corner))) != 0u)
                {
                    int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
                    sum += levels.lightAt(level, base + offset, texel) * coneCornerWeight(f, offset);
                }
            taken += sum * lobe;
            lobes += lobe;
        }
        return lobes > 0.0 ? taken / (lobes * counted) : float3(0.0);
    }
}

// Reads the cone the point at index reflects by the read a binding names (IConeRead) and keeps
// it, a thread a point.
public void readCone<C : IConeRead, L : IGatherLevels, A : IConePoints>(C cone, L levels, A points, uint index)
{
    points.keepReflected(index, cone.light(levels, points.levelAt(index), points.placeAt(index), points.normalAt(index), points.viewAt(index),
        points.roughnessAt(index)));
}
