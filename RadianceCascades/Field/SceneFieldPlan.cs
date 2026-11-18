using System.Runtime.InteropServices;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RadianceCascades;

/// <summary>
/// What the scene's distance field is built from each frame and where, worked out on the CPU from
/// the meshes drawn that cast shadows, which says where each cascade lies around the eye, which
/// cascades are built again this frame and from which still meshes, and which meshes are stamped as
/// boxes because they moved.
/// </summary>
/// <remarks>
/// <para>
/// A cascade is <see cref="Resolution"/> cells a side, each cascade's cells twice as wide as the
/// one before's, and lies around the eye, with its corner on a grid of <see cref="SnapCells"/> of
/// its cells, so it moves only when the eye has gone that far, a turn moving none, and the cells it
/// keeps stay where they were. A cascade the eye moved by less than half its width shifts as a
/// clipmap does, the cells it keeps moved on the GPU and the slab it brings in built alone, one
/// axis at a time (SceneFieldPlan.Shifts.cs). One moved farther, or that a still mesh came into or
/// left, is built again whole, the finest first, as many a frame as the budget says, and until then
/// keeps the place and the meshes it was built with, which the info the passes read says.
/// </para>
/// <para>
/// The eye so stands 28 to 36 cells from each side of each cascade, past the outer bands the passes
/// fade across, from a cascade to the next and from the last to what lies past the field. A cascade
/// lying a share of its width ahead of the eye the way it looks would cross to the other side with
/// a turn and be built again whole, a large scene's triangles over many frames at the default
/// budget, what stands behind lying outside the fine cascades meanwhile. An engine whose camera
/// looks on the scene from afar gives the point the field lies around as the eye.
/// </para>
/// <para>
/// A mesh drawn the same, by its mesh, its vertices, its world matrix and its sides, for
/// <see cref="SettleFrames"/> frames running is still and built into the cascades around it, or for
/// <see cref="MobileSettleFrames"/> where it moved within as many frames, so a figure that stops
/// for a while is not built into the cascades and out again. One that moved more recently, a
/// skinned one, and a still one that moved before it settled whose cascades have not been built
/// again since it came, but for one that replaced a still mesh in place, which the cascades not yet
/// built still hold, are stamped each frame as boxes, the nearest the eye first to
/// <see cref="MaxShapes"/> boxes, into the bricks of cells each box comes within the band of, and
/// the bricks stamped the frame before are stamped again so a box that left one is gone. A skinned
/// mesh is a box for each joint around the vertices it holds most, posed by the joint, and one that
/// does not bend a box for each part its triangles were cut into, or the box around all of them. A
/// mesh that came and has not moved, as a scene's on its first load or a world's section streamed
/// in, stands in for nothing until it is built.
/// </para>
/// <para>
/// The plan reads no mesh's data. <typeparamref name="TMesh"/> is what the engine knows a mesh's
/// vertices by, and the plan asks the engine for the box around them, their triangles and their
/// parts. <typeparamref name="TTag"/> is the engine's own of each draw, as the textures a traced
/// reference reads, carried to the builds and compared with the rest of the draw, so a draw whose
/// tag changes is its mesh painted again where it stands.
/// </para>
/// <para>
/// Both are compared through <see cref="EqualityComparer{T}.Default"/>, which tells a class with
/// no equality of its own, an array among them, by its reference, and boxes a struct on every
/// comparison unless it implements <see cref="IEquatable{T}"/>, which a struct
/// <typeparamref name="TMesh"/> does so as not to be boxed. <typeparamref name="TTag"/> implements
/// it, so a tag made again for each draw, as a record is, is the same tag where its values are.
/// </para>
/// </remarks>
/// <typeparam name="TMesh">What the engine knows a mesh's vertices by.</typeparam>
/// <typeparam name="TTag">
/// The engine's own of each draw, which the plan carries and compares and reads nothing of.
/// </typeparam>
public sealed partial class SceneFieldPlan<TMesh, TTag> where TMesh : notnull where TTag : IEquatable<TTag>
{
    /// <summary>The cells along each side of a cascade.</summary>
    public const int Resolution = 64;

    /// <summary>
    /// How many cells from a surface the distances are exact, past which they hold that many.
    /// </summary>
    public const int Band = 4;

    /// <summary>How many frames running a mesh is drawn the same before it is still.</summary>
    public const int SettleFrames = 8;

    /// <summary>
    /// How many frames running a mesh is drawn the same before it is still where it moved within as
    /// many frames, as a walking figure that stops for a while.
    /// </summary>
    /// <remarks>
    /// A mesh that settles marks the cascades around it to be built again, and one that leaves
    /// marks them again, a build on the GPU each time, where stamping its box costs far less a
    /// frame, so a figure that stopped after walking is stamped on until it has stood this long.
    /// The count is past eight seconds at 120 frames a second.
    /// </remarks>
    public const int MobileSettleFrames = 1024;

    // What a mesh's frames drawn the same are once it has settled, past every count it settles at,
    // and the frame a mesh that never moved moved at.
    private const int Settled = int.MaxValue / 2;
    private const long Never = long.MinValue / 2;

    /// <summary>How many shapes are stamped a frame at most, the nearest the eye.</summary>
    public const int MaxShapes = 256;

    /// <summary>The cells along each side of a brick a stamp writes.</summary>
    public const int BrickCells = 4;

    /// <summary>How many of its cells a cascade's corner moves by at least.</summary>
    public const int SnapCells = 8;

    /// <summary>
    /// How many triangles the builds splat a frame at most unless the engine says.
    /// </summary>
    public const int DefaultTriangleBudget = 32_768;

    /// <summary>
    /// A mesh drawn this frame, the engine's number for it, what the engine knows its vertices by,
    /// where it is drawn, whether it has an inside, its surface's color and emission, linear, which
    /// the field paints its cells with, the parts it is stamped as where the engine gives them, its
    /// material's roughness, metallic share and reflectance, the share of light that passes a unit
    /// of its depth, the share of its diffuse light it sends through, and the engine's own of the
    /// draw.
    /// </summary>
    /// <param name="Mesh">
    /// The engine's number for the mesh, which tells a skinned mesh drawn again from another.
    /// </param>
    /// <param name="Vertices">What the engine knows the mesh's vertices by.</param>
    /// <param name="World">Where the mesh is drawn, from its own space to the world.</param>
    /// <param name="DoubleSided">
    /// Whether the mesh has no inside, its faces seen from both sides.
    /// </param>
    /// <param name="Color">The surface's color, linear.</param>
    /// <param name="Emission">The surface's emission, linear.</param>
    /// <param name="Parts">
    /// The parts the mesh is stamped as, or null for those the plan asks the engine for.
    /// </param>
    /// <param name="Roughness">The material's perceptual roughness.</param>
    /// <param name="Metallic">The material's metallic share.</param>
    /// <param name="LightThrough">
    /// The share of light that passes a unit of the surface's depth.
    /// </param>
    /// <param name="Reflectance">
    /// The material's reflectance, 0.5 for the 0.04 of most surfaces.
    /// </param>
    /// <param name="Transmission">
    /// The share of its diffuse light the surface sends through to its far side.
    /// </param>
    /// <param name="Tag">
    /// The engine's own of the draw, carried to the builds and compared with the rest of it, so a
    /// draw whose tag changes is the mesh painted again.
    /// </param>
    public readonly record struct Instance(int Mesh, TMesh Vertices, Matrix4x4 World, bool DoubleSided,
        Vector3 Color = default, Vector3 Emission = default, FieldPart[]? Parts = null, float Roughness = 1, float Metallic = 0, float LightThrough = 0,
        float Reflectance = 0.5f, float Transmission = 0, TTag? Tag = default);

    /// <summary>The settings the plan was made with.</summary>
    public FieldPlanOptions Options { get; }

    // The way the eye looks this frame, as Update was last given it, which a cascade lies ahead
    // along by Options.Lead.
    private Vector3 _ahead;

    /// <summary>
    /// A cascade's build, or the part of it this frame splats, which cascade it is, its corner, its
    /// cells' width, the still meshes within its band it splats, on its last part the box around
    /// the meshes that came or went within it since it was last built, the whole cascade at its
    /// first build, or null where nothing did, as where the eye moved it, and the cells it builds
    /// where it builds those alone.
    /// </summary>
    /// <remarks>
    /// A build of more triangles than a frame's budget is splatted over as many frames as it takes,
    /// its <paramref name="First"/> part clearing the cascade's codes and its
    /// <paramref name="Last"/> turning them into the distances the passes read, so until then the
    /// passes read the cascade as it was last built. A build of <paramref name="Cells"/> splats the
    /// still meshes within the band of those cells and a cell around them alone, and turns those
    /// cells alone into distances, the cells around what came or went within a built cascade, or
    /// the slab a shift brings in, whose first part moves its cascade by
    /// <paramref name="ShiftCells"/> of its cells along <paramref name="ShiftAxis"/>, 0 for x, 1
    /// for y and 2 for z, or -1 where it moves none, so the passes read the cascade at
    /// <paramref name="Origin"/> from that frame on, the cells it brought in empty until the
    /// build's last part.
    /// </remarks>
    public sealed record Build(int Cascade, Vector3 Origin, float Cell, Instance[] Instances, FieldBox? Changed = null, bool First = true, bool Last = true,
        FieldCells? Cells = null, int ShiftAxis = -1, int ShiftCells = 0);

    private readonly Func<TMesh, FieldBox> _bounds;
    private readonly Func<TMesh, int> _triangles;
    private readonly Func<TMesh, FieldPart[]?> _parts;
    private readonly Func<Instance, Vector3> _tint;
    private readonly Func<Instance, bool> _carried;
    private Dictionary<Instance, int> _seen = new(Keys.Shared);
    // In a frame read whole, the frame each mesh drawn the frame before last moved where that was
    // within the longer count, each drawn now, and the vertices of the meshes gone since.
    private readonly Dictionary<Instance, long> _movedBefore = new(Keys.Shared), _movedNow = new(Keys.Shared);
    private readonly HashSet<TMesh> _departed = [];
    // The world matrices of the meshes gone since, so a mesh drawn through one of them, as a mesh
    // uploaded again each frame or a section edited in place is, is no mesh that came (_movedIn).
    private readonly HashSet<Matrix4x4> _departedWorlds = [];
    // The frames each mesh gone since had been drawn the same and when it last moved, by its
    // vertices and its matrix, so a mesh drawn of those vertices through that matrix is that one
    // painted again and goes on from where it was.
    private readonly Dictionary<(TMesh Vertices, Matrix4x4 World), (int Frames, long MovedAt, Instance Gone)> _departedAt = [];
    private readonly Dictionary<Instance, (FieldBox Bounds, long Added)> _still = new(Keys.Shared);
    private readonly HashSet<Instance> _pending = new(Keys.Shared);
    // Still meshes that left, kept in the field for the meshes replacing them, by the frame they
    // left.
    private readonly Dictionary<Instance, long> _kept = new(Keys.Shared);
    // Still meshes that replaced a kept one in place and wait for cascades built since, which still
    // hold the one they replaced and so are not stamped. Stamped as boxes, a section of ground with
    // air above it would fill that air in the cascades the budget leaves for a later frame, and the
    // indirect light around it would go out for that frame and come back.
    private readonly HashSet<Instance> _replacing = new(Keys.Shared);
    // Still meshes that waited for their cascades having moved before they settled, which are
    // stamped meanwhile as they were while they moved. One that never moved, as a scene's meshes on
    // its first load or a world's sections streamed in, is not, since a box stands in for a mesh
    // badly where it is larger than what it holds, and the box around a building's outer walls
    // fills the building, which the sun's contact shadows then darken in every room.
    private readonly HashSet<Instance> _movedIn = new(Keys.Shared);
    private readonly Vector3?[] _built;
    private readonly long[] _builtFrame;
    private readonly bool[] _dirty;
    // The cascades the eye has gone past by their whole box since they were last built, as a cut
    // takes it, each until its whole build ends, which the builds' budgets are lifted for (Splat).
    private readonly bool[] _jumped;
    // The box around the meshes that came or went within each cascade since it was last built.
    private readonly FieldBox?[] _changed;
    // How many shapes come within each brick of each cascade this frame and the frame before, the
    // cascades one after another, which a figure of many boxes that come within the same bricks
    // counts at the cost of an add each, where a set of the bricks would cost a hash each, and the
    // bricks each shape comes within in each cascade, its first and last along each axis.
    private int[] _stamped = [], _stampedBefore = [];
    // The bricks stamped this frame and the frame before, each by its place in those counts, so a
    // frame reads and clears the bricks it stamped rather than every brick of every cascade.
    private List<int> _bricksAt = [], _bricksAtBefore = [];

    // The meshes drawn the frame before in the order drawn, with the frames each had been drawn the
    // same and whether it is skinned, and whether that order held each mesh once.
    private Instance[] _order = [];
    private int[] _orderFrames = [];
    private bool[] _orderSkinned = [];
    // Whether the mesh in each place of the order moved this frame, as SameOrder found it, and the
    // frame it last moved, which it settles after the longer count for as many frames.
    private bool[] _orderMoved = [];
    private long[] _orderMovedAt = [];
    private int _orderCount;
    private bool _orderWhole;
    private readonly List<(int FieldShape, int Cascade, int X0, int Y0, int Z0, int X1, int Y1, int Z1)> _reaches = [];
    // The meshes stamped this frame, and their distances from the eye with their places, sorted.
    private Instance[] _nearest = [];
    private long[] _byDistance = [];
    private long _frame;

    /// <summary>
    /// A plan of <paramref name="cascades"/> cascades, the first's cells
    /// <paramref name="cellSize"/> wide, building <paramref name="budget"/> a frame at most.
    /// </summary>
    /// <param name="cascades">How many cascades.</param>
    /// <param name="cellSize">The width of a cell of the finest.</param>
    /// <param name="budget">How many cascades are built a frame at most.</param>
    /// <param name="bounds">
    /// The box around a mesh's vertices in its own space, which the caller may keep.
    /// </param>
    /// <param name="triangles">How many triangles a mesh has.</param>
    /// <param name="parts">
    /// The parts a mesh that does not bend is stamped as, or null for its one box, which the caller
    /// may keep.
    /// </param>
    /// <param name="tint">
    /// What a mesh's boxes' color is multiplied by, its vertices' colors' mean in linear light,
    /// which the build blends across each face, or white for none.
    /// </param>
    /// <param name="triangleBudget">
    /// How many triangles the builds splat a frame at most, a mesh at least.
    /// </param>
    /// <param name="farCell">
    /// The width of a cell of the far cascade after the others, or 0 for none.
    /// </param>
    /// <param name="options">The settings the plan is made with, or null for the defaults.</param>
    /// <param name="carried">
    /// Whether global illumination carries an emissive mesh as a glow light, whose box is stamped
    /// with none of its emission.
    /// </param>
    public SceneFieldPlan(int cascades, float cellSize, int budget, Func<TMesh, FieldBox> bounds, Func<TMesh, int> triangles,
        Func<TMesh, FieldPart[]?>? parts = null, Func<Instance, Vector3>? tint = null, int triangleBudget = DefaultTriangleBudget,
        float farCell = 0, Func<Instance, bool>? carried = null, FieldPlanOptions? options = null)
    {
        Options = options ?? new();
        _parts = parts ?? (_ => null);
        _tint = tint ?? (_ => Vector3.One);
        _carried = carried ?? (_ => false);
        Counted = Math.Clamp(cascades, 1, 8);
        FarCell = farCell > 0 ? farCell : 0;
        Cascades = Counted + (FarCell > 0 ? 1 : 0);
        CellSize = cellSize;
        Budget = Math.Max(1, budget);
        TriangleBudget = BaseTriangleBudget = Math.Max(1, triangleBudget);
        _triangles = triangles;
        _bounds = bounds;
        _built = new Vector3?[Cascades];
        _builtFrame = new long[Cascades];
        _dirty = Enumerable.Repeat(true, Cascades).ToArray();
        _jumped = new bool[Cascades];
        _changed = new FieldBox?[Cascades];
        _regions = new FieldBox?[Cascades];
    }

    /// <summary>How many cascades, the far one among them where there is one.</summary>
    public int Cascades { get; }

    /// <summary>
    /// How many cascades there are but the far one, each twice as coarse as the one before.
    /// </summary>
    public int Counted { get; }

    /// <summary>
    /// The width of a cell of the far cascade, the last, or 0 where there is none.
    /// </summary>
    public float FarCell { get; }

    /// <summary>
    /// Whether the last cascade is a far one, its cells <see cref="FarCell"/> wide.
    /// </summary>
    public bool HasFar => FarCell > 0;

    /// <summary>The width of a cell of the finest cascade.</summary>
    public float CellSize { get; }

    /// <summary>How many cascades are built a frame at most.</summary>
    public int Budget { get; private set; }

    /// <summary>
    /// How many triangles the builds splat a frame at most, a mesh at least, raised where the frame
    /// has room (<see cref="Raise"/>).
    /// </summary>
    public int TriangleBudget { get; private set; }

    /// <summary>
    /// The cascades built this frame, or the parts of their builds splatted this frame.
    /// </summary>
    public List<Build> Builds { get; } = [];

    /// <summary>The shapes stamped this frame.</summary>
    public List<FieldShape> Shapes { get; } = [];

    /// <summary>The bricks stamped this frame, by cascade and place in bricks.</summary>
    public List<(int Cascade, int X, int Y, int Z)> Bricks { get; } = [];

    /// <summary>
    /// The shapes that come within each of <see cref="Bricks"/>, where its run of
    /// <see cref="BrickShapes"/> begins and how long it is, none for a brick only put back.
    /// </summary>
    public List<(int First, int Count)> BrickRanges { get; } = [];

    /// <summary>
    /// The shapes of each brick one run after another, by their place in <see cref="Shapes"/>.
    /// </summary>
    public List<int> BrickShapes { get; } = [];

    /// <summary>How many meshes are still.</summary>
    public int StillCount => _still.Count;

    /// <summary>
    /// Whether a mesh drawn this frame is still, built into the cascades around it rather than
    /// stamped.
    /// </summary>
    public bool IsStill(in Instance instance) => _still.ContainsKey(instance);

    /// <summary>
    /// Whether the last frame drew the meshes the frame before did in the same order, and was read
    /// by place.
    /// </summary>
    public bool ReadByPlace { get; private set; }

    /// <summary>
    /// Whether a frame has drawn a mesh twice in one place, the same mesh, matrix and material,
    /// which no frame read by place may, since the plan counts each mesh's frames once.
    /// </summary>
    /// <remarks>
    /// Two draws of one mesh where each other is add nothing to the field, so an engine that sees
    /// this set gathers the frames after without the second, and those are read by place, since
    /// one such pair would have every frame read whole, which costs the plan several times what a
    /// frame read by place does.
    /// </remarks>
    public bool DrawnTwiceInPlace { get; private set; }

    /// <summary>
    /// The cascade the probes of cascade <paramref name="probes"/> of <paramref name="cascades"/>
    /// stand in, its own number but for the last, which stands in the far cascade where there is
    /// one, as the field's readers look a probe's cascade up.
    /// </summary>
    public int CascadeOfProbes(int probes, int cascades) => HasFar && probes == cascades - 1 ? Cascades - 1 : probes;

    /// <summary>The width of a cell of a cascade.</summary>
    public float CellOf(int cascade) => cascade >= Counted && HasFar ? FarCell : CellSize * (1 << cascade);

    /// <summary>
    /// The width of a cell of a far cascade for <paramref name="reach"/>, 0.85 of its width, at
    /// least twice as wide as the last counted cascade's of <paramref name="cascades"/> from
    /// <paramref name="cellSize"/>, or 0 for a reach of none or past every finite distance.
    /// </summary>
    /// <remarks>
    /// The cascade is the reach over 0.85 wide. Lying around the eye, its sides lie 28 to 36 of its
    /// cells off, about half the reach, past which the passes take the light from beyond the field,
    /// and a cascade wide enough to hold the whole reach around the eye would hold the scene in
    /// cells nearly twice as wide.
    /// </remarks>
    /// <remarks>
    /// It is rounded up to a sixteenth of twice the last's cell, so a reach read from a camera's
    /// far plane, which rounding moves a little from frame to frame, gives the same cell and the
    /// field is not made again.
    /// </remarks>
    public static float FarCellFor(float reach, int cascades, float cellSize)
    {
        if (!(reach > 0) || !float.IsFinite(reach)) return 0;
        var doubled = cellSize * (1 << Math.Clamp(cascades, 1, 8));
        var step = doubled / 16;
        return Math.Max(doubled, MathF.Ceiling(reach / (0.85f * Resolution) / step) * step);
    }

    /// <summary>
    /// The eye the cascades lie around this frame, as <see cref="Update"/> was last given it.
    /// </summary>
    public Vector3 Eye { get; private set; }

    /// <summary>
    /// Where a cascade lies as it was last built, its corner, or null for one not yet built.
    /// </summary>
    public Vector3? BuiltOrigin(int cascade) => _built[cascade];

    /// <summary>
    /// Where a cascade's corner would lie around <paramref name="eye"/>, its middle on the snap's
    /// grid nearest the eye, or where <see cref="FieldPlanOptions.Lead"/> is above 0, that share of
    /// its width ahead of it along
    /// <paramref name="ahead"/>, a unit direction or zero.
    /// </summary>
    /// <remarks>
    /// Ahead of the eye, the point ahead is snapped down to the grid of <see cref="SnapCells"/>
    /// cells, which leaves the side behind an eye looking down an axis the negative way 1.6 to 9.6
    /// cells behind it and the positive way 9.6 to 17.6, so where a side lies within a step of the
    /// eye the cascade moves a step further that way. A layer of probes, which stand a step apart,
    /// then stands behind the eye, and the wall the eye backs onto lies inside the cascade.
    /// </remarks>
    public Vector3 OriginAround(int cascade, Vector3 eye, Vector3 ahead = default)
    {
        var cell = CellOf(cascade);
        var step = cell * SnapCells;
        if (Options.Lead == 0)
        {
            var snapped = new Vector3(MathF.Floor(eye.X / step + 0.5f), MathF.Floor(eye.Y / step + 0.5f), MathF.Floor(eye.Z / step + 0.5f)) * step;
            return snapped - new Vector3(Resolution / 2 * cell);
        }
        var middle = eye + ahead * (Options.Lead * Resolution * cell);
        var below = new Vector3(MathF.Floor(middle.X / step), MathF.Floor(middle.Y / step), MathF.Floor(middle.Z / step)) * step;
        var corner = below - new Vector3(Resolution / 2 * cell);
        return new Vector3(Clear(corner.X, eye.X), Clear(corner.Y, eye.Y), Clear(corner.Z, eye.Z));

        // The corner along one axis, a step further from the eye where it stands within a step of
        // a side.
        float Clear(float low, float at) =>
            at - low < step ? low - step : low + Resolution * cell - at < step ? low + step : low;
    }

    /// <summary>
    /// Marks every cascade to be built again at each of the next <paramref name="frames"/> frames,
    /// as many a frame as the budget allows, so a profile over them times a build.
    /// </summary>
    public void Rebuild(int frames) => _rebuildFrames = Math.Max(1, frames);

    private int _rebuildFrames;

    // The box a cascade covers from its corner.
    private FieldBox CascadeBox(int cascade, Vector3 origin) => new(origin, origin + new Vector3(Resolution * CellOf(cascade)));

    /// <summary>
    /// Works out the frame's builds, shapes and bricks from the meshes drawn this frame and the
    /// eye the cascades lie around.
    /// </summary>
    /// <param name="eye">The point the cascades lie around.</param>
    /// <param name="drawn">The meshes drawn this frame, each with whether it is skinned.</param>
    /// <param name="ahead">
    /// The way the eye looks, a unit direction, which the cascades lie ahead along where
    /// <see cref="FieldPlanOptions.Lead"/> is above 0.
    /// </param>
    public void Update(Vector3 eye, IReadOnlyList<(Instance Instance, bool Skinned)> drawn, Vector3 ahead = default)
    {
        _frame++;
        Eye = eye;
        _ahead = ahead;
        Builds.Clear();
        Shapes.Clear();
        Bricks.Clear();
        BrickRanges.Clear();
        BrickShapes.Clear();
        _reaches.Clear();

        var moving = new List<Instance>();
        // The meshes drawn this frame that have not settled and are not skinned, which a still
        // mesh that left may be replaced by.
        var unsettled = new List<Instance>();
        Arriving = 0;
        ReadByPlace = SameOrder(drawn);
        if (ReadByPlace)
        {
            // The meshes drawn the frame before, in the same order, each one frame more the same,
            // read by place, so a scene that did not change costs a comparison a mesh, the frames
            // of those not yet still counted in the order alone and those still in the dictionary
            // as well, which a frame read whole reads them from. A mesh in a place drawn the frame
            // before through another matrix moved, and where it stood leaves as one gone from a
            // scene read whole, so a figure walking among still meshes costs a comparison a mesh
            // too, where a frame read whole looks every mesh up in a dictionary.
            List<Instance>? left = null;
            var span = Span(drawn);
            for (int i = 0; i < _orderCount; i++)
            {
                ref readonly var instance = ref span[i].Instance;
                var skinned = span[i].Skinned;
                var frames = _orderFrames[i];
                if (_orderMoved[i])
                {
                    if (frames >= Settled)
                    {
                        _seen.Remove(_order[i]);
                        if (_still.ContainsKey(_order[i])) (left ??= []).Add(_order[i]);
                    }
                    _order[i] = instance;
                    _orderMovedAt[i] = _frame;
                    frames = 0;
                }
                if (frames >= Settled) continue;
                frames = skinned ? 0 : frames + 1 >= SettleAfter(_orderMovedAt[i]) ? Settled : frames + 1;
                _orderFrames[i] = frames;
                if (skinned) moving.Add(instance);
                else if (frames < Settled)
                {
                    var arrived = _orderMovedAt[i] == Never;
                    if (!arrived || Options.StampArrivals) moving.Add(instance);
                    if (arrived) Arriving++;
                    unsettled.Add(instance);
                }
                else
                {
                    _seen[instance] = frames;
                    Settle(instance, moved: _orderMovedAt[i] != Never);
                }
            }
            foreach (var instance in left ?? [])
            {
                if (ReplacedBy(instance, unsettled)) _kept[instance] = _frame;
                else Leave(instance);
            }
        }
        else
        {
            // The frames counted and the moves made in the order since a frame was last read whole.
            _movedBefore.Clear();
            _movedNow.Clear();
            for (int i = 0; i < _orderCount; i++)
            {
                if (_orderSkinned[i]) continue;
                if (_orderFrames[i] < Settled) _seen[_order[i]] = _orderFrames[i];
                if (_frame - _orderMovedAt[i] < MobileSettleFrames) _movedBefore[_order[i]] = _orderMovedAt[i];
            }
            // This frame's meshes, and the vertices of those drawn the frame before and not now, so
            // a mesh drawn now and not before of vertices one gone had is taken as that one moved,
            // whichever way the frame is read, and where it stands where that one stood, as that
            // one painted again, as a mesh is once its texture has loaded or a panel's light is put
            // out.
            const int unread = int.MinValue;
            var seen = new Dictionary<Instance, int>(drawn.Count, Keys.Shared);
            foreach (var (instance, _) in drawn) seen.TryAdd(instance, unread);
            _departed.Clear();
            _departedWorlds.Clear();
            _departedAt.Clear();
            for (int i = 0; i < _orderCount; i++)
                if (!_orderSkinned[i] && !seen.ContainsKey(_order[i]))
                {
                    _departed.Add(_order[i].Vertices);
                    _departedWorlds.Add(_order[i].World);
                    _departedAt[(_order[i].Vertices, _order[i].World)] = (_seen.GetValueOrDefault(_order[i]), _movedBefore.GetValueOrDefault(_order[i], Never), _order[i]);
                }
            // Each mesh's frames drawn the same, a skinned one never still. Where the engine has a
            // mesh painted again wait (Options.RepaintWaits), it counts from none, and is still at
            // once only where the one painted had stood still for as many frames as a mesh takes
            // to settle, stamped then as that one was, not at all where the cascades held it.
            List<(Instance Painted, bool Stands)>? repainted = null;
            foreach (var (instance, skinned) in drawn)
            {
                ref var counted = ref CollectionsMarshal.GetValueRefOrNullRef(seen, instance);
                if (counted != unread) continue;
                (int Frames, long MovedAt, Instance Gone) was = default;
                var painted = !skinned && !_seen.ContainsKey(instance) && _departedAt.TryGetValue((instance.Vertices, instance.World), out was);
                var movedAt = skinned ? Never : painted ? was.MovedAt
                    : !_seen.ContainsKey(instance) && _departed.Contains(instance.Vertices) ? _frame : _movedBefore.GetValueOrDefault(instance, Never);
                if (movedAt != Never) _movedNow[instance] = movedAt;
                var before = painted && !Options.RepaintWaits ? was.Frames : _seen.GetValueOrDefault(instance);
                var frames = skinned ? 0 : before >= Settled || before + 1 >= SettleAfter(movedAt) ? Settled : before + 1;
                if (Options.RepaintWaits && painted && frames < Settled && _still.TryGetValue(was.Gone, out var still) && _frame - still.Added >= SettleFrames)
                {
                    frames = Settled;
                    (repainted ??= []).Add((instance, !_pending.Contains(was.Gone) || _replacing.Contains(was.Gone)));
                }
                counted = frames;
                if (frames >= Settled) continue;
                // A mesh that came and has not moved casts nothing until it is built (_movedIn),
                // but where the engine has such meshes stamped until then.
                var arrived = !skinned && movedAt == Never && (painted || !_departedWorlds.Contains(instance.World));
                if (!arrived || Options.StampArrivals) moving.Add(instance);
                if (arrived) Arriving++;
                if (!skinned) unsettled.Add(instance);
            }
            _seen = seen;

            // Still meshes that came or went make the cascades around them dirty. One replaced in
            // place, by a mesh of other vertices drawn through the same matrix where it was, as an
            // edited section of a world, stays in the field as it was until the one replacing it is
            // still, since the boxes that one would be stamped as stand in for it badly.
            foreach (var (instance, frames) in seen)
                if (frames >= Settled && !_still.ContainsKey(instance)) Settle(instance, moved: _movedNow.ContainsKey(instance));
            foreach (var (instance, stands) in repainted ?? [])
                if (stands) _replacing.Add(instance);
            foreach (var instance in _kept.Keys.Where(seen.ContainsKey).ToArray()) _kept.Remove(instance);
            foreach (var instance in _still.Keys.Where(instance => !seen.ContainsKey(instance) && !_kept.ContainsKey(instance)).ToArray())
            {
                if (ReplacedBy(instance, unsettled)) _kept[instance] = _frame;
                else Leave(instance);
            }
            KeepOrder(drawn, seen);
        }

        // A mesh kept for one replacing it leaves once nothing unsettled replaces it, the one that
        // did having settled, or after twice the frames a mesh takes to settle.
        foreach (var (instance, since) in _kept.ToArray())
            if (!ReplacedBy(instance, unsettled) || _frame - since > 2 * SettleFrames)
            {
                foreach (var replacing in _pending)
                    if (Replacing(replacing, instance)) _replacing.Add(replacing);
                _kept.Remove(instance);
                Leave(instance);
            }

        // Cascades the eye has moved past, then as many dirty ones as the budget allows, finest
        // first.
        _forced = _rebuildFrames > 0;
        if (_forced)
        {
            _rebuildFrames--;
            Array.Fill(_dirty, true);
        }
        for (int c = 0; c < Cascades; c++)
        {
            var around = OriginAround(c, eye, _ahead);
            if (_built[c] == around) continue;
            _dirty[c] = true;
            if (_built[c] is { } built && Vector3.Abs(around - built) is var apart && MathF.Max(apart.X, MathF.Max(apart.Y, apart.Z)) >= Resolution * CellOf(c))
                (_jumped[c], _afterCut, _quietAfterCut) = (true, 0, 0);
        }
        var rebuilt = Splat(eye);
        _mostSplatted = Math.Max(_mostSplatted, Splatted);

        // A still mesh is stamped until every built cascade it comes within the band of has been
        // built since it came, but for one that replaced a mesh in place (_replacing).
        foreach (var instance in _pending.ToArray())
        {
            var (bounds, added) = _still[instance];
            var waiting = false;
            for (int c = 0; c < Cascades && !waiting; c++)
                waiting = _built[c] is { } origin && _builtFrame[c] < added && (_builtIn.GetValueOrDefault(instance) & 1 << c) == 0
                    && bounds.Overlaps(CascadeBox(c, origin).Grown(Band * CellOf(c)));
            if (waiting)
            {
                if (_movedIn.Contains(instance) || Options.StampArrivals) moving.Add(instance);
            }
            else
            {
                _pending.Remove(instance);
                _replacing.Remove(instance);
                _movedIn.Remove(instance);
                _builtIn.Remove(instance);
            }
        }
        FramesBuilt = Built ? FramesBuilt + 1 : 0;
        EndLift();

        // The nearest meshes' shapes, a box for each part of each, posed, to as many as a frame
        // stamps, and the bricks they come within the band of in each built cascade.
        // A brick's place in the counts is its place along each axis in four bits, x lowest, and
        // its cascade above them, read back by shifts rather than a division an axis.
        const int side = Resolution / BrickCells, axisBits = 4;
        const int perCascade = side * side * side;
        System.Diagnostics.Debug.Assert(side == 1 << axisBits, "a brick's place along an axis takes axisBits bits");
        if (_stamped.Length != Cascades * perCascade)
        {
            (_stamped, _stampedBefore) = (new int[Cascades * perCascade], new int[Cascades * perCascade]);
            (_bricksAt, _bricksAtBefore) = ([], []);
        }
        (_stamped, _stampedBefore) = (_stampedBefore, _stamped);
        (_bricksAt, _bricksAtBefore) = (_bricksAtBefore, _bricksAt);
        foreach (var at in _bricksAt) _stamped[at] = 0;
        _bricksAt.Clear();
        // A mesh replacing one the field still holds is not stamped, the one it replaces standing
        // in. The rest nearest the eye first, those as near in the order drawn, each sorted by its
        // distance's bits above its place, which order as the distances do since none is negative.
        var count = 0;
        if (_nearest.Length < moving.Count) (_nearest, _byDistance) = (new Instance[moving.Count * 2], new long[moving.Count * 2]);
        foreach (var instance in moving)
            if ((_kept.Count == 0 || !Replaces(instance)) && !_replacing.Contains(instance))
            {
                _byDistance[count] = (long)BitConverter.SingleToInt32Bits(Vector3.DistanceSquared(instance.World.Translation, eye)) << 32 | (uint)count;
                _nearest[count++] = instance;
            }
        Array.Sort(_byDistance, 0, count);
        for (int n = 0; n < count && Shapes.Count < MaxShapes; n++)
        {
            ref readonly var instance = ref _nearest[(int)(_byDistance[n] & 0xFFFFFFFF)];
            var tint = instance.Color * _tint(instance);
            var emission = instance.Emission == Vector3.Zero || _carried(instance) ? Vector3.Zero : instance.Emission;
            foreach (var (own, pose) in PartsWithin(instance, MaxShapes - Shapes.Count - (count - n - 1)))
            {
                var w = pose * instance.World;
                if (!FieldShape.Turns(w)) continue;
                var scale = MathF.Min(new Vector3(w.M11, w.M12, w.M13).Length(), MathF.Min(new Vector3(w.M21, w.M22, w.M23).Length(), new Vector3(w.M31, w.M32, w.M33).Length()));
                Shapes.Add(new FieldShape(w, (own.Min + own.Max) / 2, (own.Max - own.Min) / 2, scale, tint, instance.LightThrough, emission,
                    new Vector3(instance.Roughness, instance.Metallic, instance.Reflectance), instance.Transmission));
                var (min, max) = own.Transformed(w);
                for (int c = 0; c < Cascades; c++)
                {
                    if (_built[c] is not { } origin) continue;
                    var cell = CellOf(c);
                    var brick = BrickCells * cell;
                    var (low, high) = ((min - origin - new Vector3(Band * cell)) / brick, (max - origin + new Vector3(Band * cell)) / brick);
                    var (fx, fy, fz) = (MathF.Max(0, low.X), MathF.Max(0, low.Y), MathF.Max(0, low.Z));
                    var (lx, ly, lz) = (MathF.Min(side - 1, high.X), MathF.Min(side - 1, high.Y), MathF.Min(side - 1, high.Z));
                    if (fx > lx || fy > ly || fz > lz) continue;
                    var (x0, y0, z0, x1, y1, z1) = ((int)fx, (int)fy, (int)fz, (int)lx, (int)ly, (int)lz);
                    _reaches.Add((Shapes.Count - 1, c, x0, y0, z0, x1, y1, z1));
                    for (int z = z0; z <= z1; z++)
                        for (int y = y0; y <= y1; y++)
                        {
                            var at = ((c * side + z) * side + y) * side + x0;
                            for (int x = x0; x <= x1; x++, at++)
                                if (_stamped[at]++ == 0) _bricksAt.Add(at);
                        }
                }
            }
        }
        // The bricks stamped now, each with a run of its shapes, and last frame's, which go back to
        // the still field where no shape covers them now, but in a cascade built again this frame,
        // which the build wrote whole. The counts become where each brick's run begins.
        foreach (var at in _bricksAtBefore)
            if (_stamped[at] == 0 && _stampedBefore[at] > 0 && !rebuilt[at >> 3 * axisBits]) _bricksAt.Add(at);
        var runs = 0;
        foreach (var at in _bricksAt)
        {
            var shapes = _stamped[at];
            Bricks.Add((at >> 3 * axisBits, at & side - 1, at >> axisBits & side - 1, at >> 2 * axisBits & side - 1));
            BrickRanges.Add((runs, shapes));
            _stamped[at] = runs;
            runs += shapes;
        }
        CollectionsMarshal.SetCount(BrickShapes, runs);
        var filled = CollectionsMarshal.AsSpan(BrickShapes);
        foreach (var (shape, c, x0, y0, z0, x1, y1, z1) in _reaches)
            for (int z = z0; z <= z1; z++)
                for (int y = y0; y <= y1; y++)
                {
                    var at = ((c * side + z) * side + y) * side + x0;
                    for (int x = x0; x <= x1; x++, at++) filled[_stamped[at]++] = shape;
                }
        // Each brick's count put back where the fill left the end of its run, which the frame after
        // reads for the bricks stamped now.
        for (int b = 0; b < _bricksAt.Count; b++) _stamped[_bricksAt[b]] = BrickRanges[b].Count;
        Track();
    }

    // Meshes told apart as their records are and hashed by their vertices and where they stand
    // alone, since a record's hash of every field, the matrix's sixteen among them, is a call a
    // field in a build that is not optimized, and the plan hashes each mesh that moves a few times
    // a frame.
    private sealed class Keys : IEqualityComparer<Instance>
    {
        public static readonly Keys Shared = new();

        public bool Equals(Instance a, Instance b) => a.Equals(b);

        public int GetHashCode(Instance instance) =>
            HashCode.Combine(EqualityComparer<TMesh>.Default.GetHashCode(instance.Vertices), instance.World.M41, instance.World.M42, instance.World.M43);
    }

    // A still mesh that left, its cascades made dirty to be built again without it.
    private void Leave(Instance instance)
    {
        MarkAround(_still[instance].Bounds, Follow(instance, _still[instance].Bounds, came: false));
        _still.Remove(instance);
        _pending.Remove(instance);
        _replacing.Remove(instance);
        _movedIn.Remove(instance);
        _builtIn.Remove(instance);
    }

    // Whether a mesh of other vertices drawn through a still mesh's matrix, unsettled, lies where
    // it was.
    private bool ReplacedBy(Instance still, List<Instance> unsettled)
    {
        var bounds = _still[still].Bounds;
        foreach (var instance in unsettled)
            if (instance.World == still.World && Other(instance, still)
                && _bounds(instance.Vertices).Transformed(instance.World).Overlaps(bounds))
                return true;
        return false;
    }

    // Whether a mesh replaces one the field keeps for it.
    private bool Replaces(Instance instance)
    {
        foreach (var still in _kept.Keys)
            if (Replacing(instance, still))
                return true;
        return false;
    }

    // Whether a mesh of other vertices drawn through a still mesh's matrix lies where it was.
    private bool Replacing(Instance instance, Instance still) =>
        instance.World == still.World && Other(instance, still)
        && _bounds(instance.Vertices).Transformed(instance.World).Overlaps(_still[still].Bounds);

    // Whether a mesh drawn where a still one stood replaces it, one of other vertices, or where the
    // engine has a mesh painted again wait, any other draw, the same mesh painted again among them.
    private bool Other(in Instance instance, in Instance still) =>
        Options.RepaintWaits ? !instance.Equals(still) : !EqualityComparer<TMesh>.Default.Equals(instance.Vertices, still.Vertices);

    // A mesh drawn the same for SettleFrames frames running, which the cascades around it are built
    // again with and which, where it moved before it settled, is stamped until they have been.
    private void Settle(Instance instance, bool moved)
    {
        var bounds = _bounds(instance.Vertices).Transformed(instance.World);
        _still[instance] = (bounds, _frame);
        _pending.Add(instance);
        _builtIn.Remove(instance);
        if (moved) _movedIn.Add(instance);
        MarkAround(bounds, Follow(instance, bounds, came: true));
    }

    // Whether this frame draws the meshes the frame before did, in the same order, each by its
    // instance, or moved, as its instance drawn through another matrix that the frame before drew
    // nowhere, a skinned one by its mesh, whose parts are posed afresh each frame and which is
    // never still. A frame before that drew a mesh twice is not read by place.
    private bool SameOrder(IReadOnlyList<(Instance Instance, bool Skinned)> drawn)
    {
        if (!_orderWhole || drawn.Count != _orderCount) return false;
        if (_orderMoved.Length < _orderCount) _orderMoved = new bool[_order.Length];
        var span = Span(drawn);
        for (int i = 0; i < _orderCount; i++)
        {
            ref readonly var now = ref span[i];
            ref readonly var before = ref _order[i];
            if (now.Skinned != _orderSkinned[i]) return false;
            if (now.Skinned)
            {
                if (now.Instance.Mesh != before.Mesh) return false;
                _orderMoved[i] = false;
            }
            else if (_orderMoved[i] = !Same(in now.Instance, in before, true))
            {
                if (!Same(in now.Instance, in before, false) || _seen.ContainsKey(now.Instance)) return false;
            }
        }
        return true;
    }

    // Whether two meshes are drawn the same, through the same world matrix too where `placed` is
    // set, compared in place, where a record's equality copies each and calls a comparer a field.
    private static bool Same(in Instance a, in Instance b, bool placed) =>
        a.Mesh == b.Mesh && EqualityComparer<TMesh>.Default.Equals(a.Vertices, b.Vertices) && (!placed || a.World == b.World) && a.DoubleSided == b.DoubleSided
        && a.Color == b.Color && a.Emission == b.Emission && ReferenceEquals(a.Parts, b.Parts) && a.Roughness == b.Roughness && a.Metallic == b.Metallic
        && a.LightThrough == b.LightThrough && a.Reflectance == b.Reflectance && a.Transmission == b.Transmission
        && EqualityComparer<TTag>.Default.Equals(a.Tag, b.Tag);

    // The meshes drawn, read in place where they are a list or an array, as the field's are.
    private static ReadOnlySpan<(Instance Instance, bool Skinned)> Span(IReadOnlyList<(Instance Instance, bool Skinned)> drawn) => drawn switch
    {
        List<(Instance, bool)> list => CollectionsMarshal.AsSpan(list),
        (Instance, bool)[] array => array,
        _ => drawn.ToArray(),
    };

    // How many frames running a mesh that last moved at the frame given is drawn the same before it
    // is still, the longer count where that was within as many frames.
    private int SettleAfter(long movedAt) => _frame - movedAt < MobileSettleFrames ? MobileSettleFrames : SettleFrames;

    // This frame's meshes in the order drawn, with the frames each has been drawn the same, for
    // the frame after to read by place.
    private void KeepOrder(IReadOnlyList<(Instance Instance, bool Skinned)> drawn, Dictionary<Instance, int> seen)
    {
        if (_order.Length < drawn.Count)
        {
            var room = Math.Max(drawn.Count, _order.Length * 2);
            (_order, _orderFrames, _orderSkinned, _orderMovedAt) = (new Instance[room], new int[room], new bool[room], new long[room]);
        }
        _orderCount = drawn.Count;
        _orderWhole = seen.Count == drawn.Count;
        DrawnTwiceInPlace |= !_orderWhole;
        for (int i = 0; i < drawn.Count; i++)
        {
            var (instance, skinned) = drawn[i];
            (_order[i], _orderFrames[i], _orderSkinned[i]) = (instance, seen.GetValueOrDefault(instance), skinned);
            _orderMovedAt[i] = _movedNow.Count > 0 && _movedNow.TryGetValue(instance, out var movedAt) ? movedAt : Never;
        }
    }
}
