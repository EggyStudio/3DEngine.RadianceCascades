# 3DEngine.RadianceCascades cheat sheet

Every public type of the library with its public members, in the order the README first names the
types, each member a line of its declaration and the first sentence of its summary. The list is
written by `Cheatsheet.Write` from the built library and checked against it by the tests, and the
text around it is written by hand.

<!-- written by Cheatsheet.Write from the built library -->
```csharp
class SceneFieldPlan<TMesh, TTag>                        // What the scene's distance field is built from each frame and where, worked out on the CPU from the meshes drawn that cast shadows, which says where each cascade lies around the eye, which cascades are built again this frame and from which still meshes, and which meshes are stamped as boxes because they moved
int MostRaise = 16;                                      // The most the triangle budget is raised to, as a multiple of BaseTriangleBudget
int CutQuietFrames = 30;                                 // The frames running that bring no mesh and leave nothing to build after which the lift a cut gave the builds' budgets ends
int CutLiftFrames = 300;                                 // The most frames after a cut the builds' budgets are lifted for
int RaiseFrames = 30;                                    // The frames over which the budget is set again from the room the frame has
int Resolution = 64;                                     // The cells along each side of a cascade
int Band = 4;                                            // How many cells from a surface the distances are exact, past which they hold that many
int SettleFrames = 8;                                    // How many frames running a mesh is drawn the same before it is still
int MobileSettleFrames = 1024;                           // How many frames running a mesh is drawn the same before it is still where it moved within as many frames, as a walking figure that stops for a while
int MaxShapes = 256;                                     // How many shapes are stamped a frame at most, the nearest the eye
int BrickCells = 4;                                      // The cells along each side of a brick a stamp writes
int SnapCells = 8;                                       // How many of its cells a cascade's corner moves by at least
int DefaultTriangleBudget = 32768;                       // How many triangles the builds splat a frame at most unless the engine says
float Lead = 0.35f;                                      // The share of its width a cascade's middle lies ahead of the eye where Ahead is set
SceneFieldPlan(int cascades, float cellSize, int budget, Func<TMesh, FieldBox> bounds, Func<TMesh, int> triangles, FieldForms forms, Func<TMesh, FieldPart[]?>? parts = null, Func<SceneFieldPlan<TMesh, TTag>.Instance<TMesh, TTag>, Vector3>? tint = null, int triangleBudget = 32768, float farCell = 0f, Func<SceneFieldPlan<TMesh, TTag>.Instance<TMesh, TTag>, bool>? carried = null);
int BaseTriangleBudget { get; }                          // The triangle budget the field was made with, which TriangleBudget is raised from
int BuildingTriangles { get; }                           // The triangles of the meshes the build in progress splats over its frames, or none
(int, FieldCells)? Unbuilt { get; }                      // The cascade a shift moved and the cells it brought in, which hold nothing until the shift's build ends in a later frame, or null where no shift waits
int Splatted { get; }                                    // How many triangles this frame's builds splat
int Waiting { get; }                                     // How many triangles of a build left for later frames wait to be splatted
int Arriving { get; }                                    // How many meshes drawn this frame came and have not moved and are not yet still, which the field holds nothing of until they settle and are built in, as a scene's on its first load
bool Whole { get; }                                      // Whether the field holds every mesh in reach, every cascade built and every mesh that came built in, so a ray that leaves it meeting nothing met no mesh there
bool Built { get; }                                      // Whether the field holds the scene as it stands, Whole with no build in progress and none waiting, every cascade built where the eye has it, so only what moves is stamped as boxes, which a picture of the scene waits for
int FramesBuilt { get; }                                 // How many frames running the field has been Built, 0 while it is not
FieldForms Forms { get; }                                // The forms the engine chose where the plan holds two
int Cascades { get; }                                    // How many cascades, the far one among them where there is one
int Counted { get; }                                     // How many cascades there are but the far one, each twice as coarse as the one before
float FarCell { get; }                                   // The width of a cell of the far cascade, the last, or 0 where there is none
bool HasFar { get; }                                     // Whether the last cascade is a far one, its cells FarCell wide
float CellSize { get; }                                  // The width of a cell of the finest cascade
int Budget { get; }                                      // How many cascades are built a frame at most
int TriangleBudget { get; }                              // How many triangles the builds splat a frame at most, a mesh at least, raised where the frame has room (Raise)
List<SceneFieldPlan<TMesh, TTag>.Build<TMesh, TTag>> Builds { get; }  // The cascades built this frame, or the parts of their builds splatted this frame
List<FieldShape> Shapes { get; }                         // The shapes stamped this frame
List<(int, int, int, int)> Bricks { get; }               // The bricks stamped this frame, by cascade and place in bricks
List<(int, int)> BrickRanges { get; }                    // The shapes that come within each of Bricks, where its run of BrickShapes begins and how long it is, none for a brick only put back
List<int> BrickShapes { get; }                           // The shapes of each brick one run after another, by their place in Shapes
int StillCount { get; }                                  // How many meshes are still
bool ReadByPlace { get; }                                // Whether the last frame drew the meshes the frame before did in the same order, and was read by place
bool DrewTwice { get; }                                  // Whether a frame has drawn a mesh twice in one place, the same mesh, matrix and material, which no frame read by place may, since the plan counts each mesh's frames once
Vector3 Eye { get; }                                     // The eye the cascades lie around this frame, as Update was last given it
int Spent { get; }                                       // How much of the triangle budget this frame's builds spent, each mesh its triangles by the share of its bounds within the cells built, an eighth of them at least
long Frame { get; }                                      // The frames the plan has been updated for
(long, long, int) LastChange { get; }                    // The frame the field last began to change, the frame after which it held the change whole, or -1 while it has not, and the triangles its builds splatted between
(int[], int, int) Unsettled { get; }                     // What keeps the field from holding a change whole, the cascades to be built whole or to have a region built, the still meshes that wait for their cascades and those kept for meshes replacing them
void SetBudgets(int cascades, int triangles);            // Sets the cascades built a frame and the base triangle budget to new ones, keeping what the field holds and the builds in progress, the raised budget held between the new base and MostRaise times it
void Raise(double roomMs, double fieldMs);               // Sets the triangle budget each RaiseFrames frames from roomMs, the milliseconds a frame waits for the target frame rate, and fieldMs, what the field costs a frame on the CPU and the GPU, both averaged over the frames before, called before each Update
int TakeMostSplatted();                                  // The most triangles a frame's builds splatted since this was last asked, started afresh
bool IsStill(in SceneFieldPlan<TMesh, TTag>.Instance<TMesh, TTag> instance);
int CascadeOfProbes(int probes, int cascades);           // The cascade the probes of cascade probes of cascades stand in, its own number but for the last, which stands in the far cascade where there is one, as the field's readers look a probe's cascade up
float CellOf(int cascade);                               // The width of a cell of a cascade
float FarCellFor(float reach, int cascades, float cellSize);  // The width of a cell of a far cascade for reach, 0.85 of its width, at least twice as wide as the last counted cascade's of cascades from cellSize, or 0 for a reach of none or past every finite distance
Vector3? BuiltOrigin(int cascade);                       // Where a cascade lies as it was last built, its corner, or null for one not yet built
Vector3 OriginAround(int cascade, Vector3 eye, Vector3 ahead = default);  // Where a cascade's corner would lie around eye, its middle on the snap's grid nearest the eye, or where Ahead is set, ahead of it along ahead, a unit direction or zero
void Rebuild(int frames);                                // Marks every cascade to be built again at each of the next frames frames, as many a frame as the budget allows, so a profile over them times a build
void Update(Vector3 eye, IReadOnlyList<ValueTuple<SceneFieldPlan<TMesh, TTag>.Instance<TMesh, TTag>, bool>> drawn, Vector3 ahead = default);
int Move(double x, double y, double z);                  // Moves every place the plan keeps by a whole step, in doubles, as a program moved every place it hands the engine by it

record struct FieldBox                                   // A box along the axes
FieldBox(Vector3 Min, Vector3 Max);
Vector3 Min { get; init; }                               // Its least corner
Vector3 Max { get; init; }                               // Its greatest corner
bool Overlaps(FieldBox other);                           // Whether this box and other share a point, a face touching among them
FieldBox Grown(float by);                                // The box grown by by on every side
FieldBox With(FieldBox other);                           // The box around this one and other
FieldBox Transformed(Matrix4x4 world);                   // The box around this one's eight corners moved by world
FieldBox Of(ReadOnlySpan<Vector3> positions);            // The box around positions, or a box of no size at the origin for none

record struct FieldForms                                 // The forms the plan holds two ways, each the engine's to name, none taken for it
bool Ahead { get; init; }                                // Whether a cascade's middle lies Lead of its width ahead of the eye the way it looks, its corner snapped down and stepped clear of the eye, so more of it holds what the eye sees, where otherwise it lies around the eye on the snap nearest it, so a turn builds nothing again
bool StampsArrivals { get; init; }                       // Whether a mesh that came and has not moved, and a still one that waits for its cascades, are stamped as boxes until they are built, where otherwise only a mesh that moved is, since a box stands in badly for a mesh larger than what it holds, as a building's outer walls
bool WholeBuilds { get; init; }                          // Whether every build is of a whole cascade, for field kernels that build no region and shift no cascade, where otherwise a cascade the eye moved by whole cells shifts and what came or went within a built one is built as a region of it
bool RepaintWaits { get; init; }                         // Whether a still mesh painted again where it stands is still at once only where it had stood still for SettleFrames frames since it settled, and otherwise stays in the field as it was until its new paint settles, as one replaced in place does, so a mesh painted each frame is not built in each frame, where otherwise the mesh painted again goes on from the frames the one painted had

record struct FieldPart                                  // A part of a mesh stamped as a box of its own, the box around the part's vertices at rest in the mesh's own space, and where the part's pose moves that space to, a joint's matrix for a skinned mesh's limb and none for a part of a mesh that does not bend
FieldPart(FieldBox Rest, Matrix4x4 Pose);
FieldBox Rest { get; init; }                             // The box around the part's vertices at rest, in the mesh's own space
Matrix4x4 Pose { get; init; }                            // Where the part's pose moves the mesh's space to, identity for none

class FieldTriangles                                     // The triangles the field's builds read, laid out as the splat reads them, with the open edges a mesh's corners carry and the parts a mesh that does not bend is stamped as
int Uints = 12;                                          // The uints a triangle takes, four a corner
uint White = 16777215;                                   // The color a corner takes where its mesh has none, white in sRGB bytes
uint Srgb(byte red, byte green, byte blue);              // A color as a corner takes it, three sRGB bytes, red in the lowest
int Write(Span<uint> into, ReadOnlySpan<Vector3> positions, ReadOnlySpan<uint> indices, ReadOnlySpan<uint> colors);  // Writes the corners of the triangles indices makes of positions into into, Uints a triangle, each corner's color from colors by its vertex, one a vertex as Srgb makes it, or White for every corner where it is empty, and the open edges OpenEdges finds
bool[] OpenEdges(ReadOnlySpan<Vector3> positions, ReadOnlySpan<uint> indices);  // Whether each edge of a mesh's triangles is open, its edge from each corner to the next, which no other triangle shares by the places of its ends, so a mesh whose faces keep vertices of their own, as a cube's do, is closed where its faces meet and a ground plane open at its rim
int PoolCapacity(int wanted, int needed, ulong? free);   // How many triangles a pool is made for, twice wanted, those of the build in progress, rounded up to a power of two, so its later parts fit, or where the device has less than twice that free, as many as half what it has free holds and those of this frame's meshes, needed, at least
FieldPart[]? Cut(ReadOnlySpan<Vector3> positions, ReadOnlySpan<uint> indices);  // A mesh that does not bend cut by its triangles into up to eight parts, each the box around its triangles, so a moving table or car is stamped as its shape rather than the box around all of it, or null for a mesh of fewer than 16 triangles, as a box or a slab, which is its one box

record struct GlowForms                                  // The forms the glow lights hold two ways, each the engine's to name, none taken for it
bool FaintThousandth { get; init; }                      // Whether a light reaches as far as a surface there takes a thousandth of the light it emits, where otherwise it reaches as far as a surface takes a 300th, or as far as a surface takes a 300th of what one half the first cascade's probe spacing from it takes where that is farther, so a lamp's small bulb reaches the floor under it however bright it burns
bool NearestFirst { get; init; }                         // Whether the Most lights written are those nearest the eye, where otherwise they are those that send the most about the eye, each one's light times the surface it emits from over its squared distance from the eye, so a bright lantern down the street is written before a faint emitter beside the eye
bool LightsAlone { get; init; }                          // Whether the block is the lights alone from its first byte, Most of Bytes each, the places they take and the bits of those kept from the frame before handed apart through GlowPlaces, where otherwise a head of Head bytes lies ahead of them with the count of places taken, the box every light's reach lies in and those bits

class GlowLights                                         // The small emissive surfaces, which global illumination carries as lights of their own rather than by the rays that meet them, which of them are lights, and the block the glow kernel reads them from, each light 80 bytes as the kernel's GlowLight reads it
int Most = 64;                                           // How many carried emitters are lights at most, as the glow kernel's MostGlowLights
int KeptVectors = 1;                                     // The uint4 values of a bit a place in the head, saying which places hold the frame before's light, as the glow kernel's GlowKeptVectors
int Bytes = 80;                                          // The bytes of a light, its middle and reach, its three half sides and its light, as the glow kernel's GlowLight
int Head = 48;                                           // The bytes ahead of the lights, the count and the least corner of the box every light's reach lies in, the box's most corner, and a bit a place saying whether it holds the frame before's light, as the glow kernel's GlowLightsHead
int Size = 5168;                                         // The bytes the head and the lights take
bool Carries(Vector3 emission, IReadOnlyList<GlowPart> parts, Matrix4x4 world, float firstCell);  // Whether an instance emitting emission is carried as lights, every part of its mesh carried as Carries carries a box
bool Carries(Vector3 emission, FieldBox box, Matrix4x4 world, float firstCell);  // Whether an instance emitting emission is carried as a light, its box in the world no longer than twice the first cascade's probe spacing
int Write<TMesh, TTag>(Span<byte> bytes, IReadOnlyList<ValueTuple<SceneFieldPlan<TMesh, TTag>.Instance<TMesh, TTag>, bool>> drawn, Vector3 eye, float firstCell, Func<TMesh, FieldBox> boxOf, Func<TMesh, float> shareOf, GlowForms forms, GlowPlaces? places = null, Func<TMesh, IReadOnlyList<GlowPart>>? partsOf = null, FrameOrigin origin = default);
void Place(Span<byte> bytes, FrameOrigin origin, GlowPlaces? places = null);  // The lights Write wrote with their middles and the box about their reach taken against origin, a place no light holds left empty
List<GlowLight> Gather<TMesh, TTag>(IReadOnlyList<ValueTuple<SceneFieldPlan<TMesh, TTag>.Instance<TMesh, TTag>, bool>> drawn, float firstCell, Func<TMesh, FieldBox> boxOf, Func<TMesh, float> shareOf, Func<TMesh, IReadOnlyList<GlowPart>>? partsOf = null, FrameOrigin origin = default);
GlowPart[] PartsOf(Vector3[]? corners, FieldBox box, float share);  // The parts of a mesh, its triangles joined where they share a corner, from its corners three a triangle in its own space, or the mesh whole, box with share, where its triangles are all joined or it has none
float ShareOf(Vector3[]? corners, FieldBox box);         // A mesh's surface over its box's, from its triangles' corners three a triangle in its own space, 1 where it has none
float ShareOf(double area, FieldBox box);                // A mesh's surface over its box's, from the area of its triangles in its own space, 1 where that is not known, 0 or less

record struct GlowPart                                   // A part of an emitter's mesh, the box around its triangles in the mesh's space, its surface over the box's faces it emits from, and which of those faces its triangles lie on
GlowPart(FieldBox Box, float Share, int Faces = 0);
FieldBox Box { get; init; }                              // The box around the part's triangles in the mesh's own space
float Share { get; init; }                               // The part's surface over that of the faces of its box it emits from
int Faces { get; init; }                                 // The faces of its box it emits from, as Faces names them, or 0 for every face

record struct FrameOrigin                                // The place a frame's places are taken against before the GPU sees them, in doubles, so a light far from the world's origin stands in numbers as small as its distance from this place, and a float's step there is not the step between neighboring points
FrameOrigin(double X, double Y, double Z);
double X { get; init; }                                  // The origin along x
double Y { get; init; }                                  // The origin along y
double Z { get; init; }                                  // The origin along z
FrameOrigin Zero { get; }                                // The world's own origin, against which every place is as given
Vector3 Against(Vector3 place);                          // The place given less the origin, worked in doubles
Matrix4x4 Against(Matrix4x4 world);                      // A world matrix with its translation taken against the origin, worked in doubles

record struct FieldCells                                 // The cells of a cascade from the first to the last along each axis, both within
FieldCells(int X0, int Y0, int Z0, int X1, int Y1, int Z1);
int X0 { get; init; }                                    // The first cell along x
int Y0 { get; init; }                                    // The first cell along y
int Z0 { get; init; }                                    // The first cell along z
int X1 { get; init; }                                    // The last cell along x
int Y1 { get; init; }                                    // The last cell along y
int Z1 { get; init; }                                    // The last cell along z
int Count { get; }                                       // How many cells
bool Holds(FieldCells other);                            // Whether every cell of other is one of these
FieldCells With(FieldCells other);                       // The cells around these and other

record struct FieldShape                                 // A mesh stamped as a box, from its own space to the world, the box's middle and half extents there, the least scale from there to the world, the mesh's color in linear light, the share of light that passes a unit of its depth, its emission, linear, none where the engine carries the mesh as a glow light, its gloss, and the share of its diffuse light it sends through
FieldShape(Matrix4x4 World, Vector3 Center, Vector3 Extents, float Scale, Vector3 Color = default, float LightThrough = 0f, Vector3 Emission = default, Vector3 Gloss = default, float Transmission = 0f);
Matrix4x4 World { get; init; }                           // From the box's own space to the world
Vector3 Center { get; init; }                            // The box's middle in its own space
Vector3 Extents { get; init; }                           // The box's half extents in its own space
float Scale { get; init; }                               // The least scale from its own space to the world
Vector3 Color { get; init; }                             // The mesh's color, linear
float LightThrough { get; init; }                        // The share of light that passes a unit of its depth
Vector3 Emission { get; init; }                          // The mesh's emission, linear
Vector3 Gloss { get; init; }                             // The mesh's perceptual roughness, metallic share and reflectance
float Transmission { get; init; }                        // The share of its diffuse light the mesh sends through to its far side
Matrix4x4 ToOwn { get; }                                 // From the world into its own space, or nothing where its matrix has no inverse
bool Turns(Matrix4x4 world);                             // Whether a matrix has an inverse, which its translation leaves as it is

record struct GlowLight                                  // A light as it is written, the middle of its box, half its side along each of the box's axes, its light, and its surface over that of the faces of its box it emits from
GlowLight(Vector3 Middle, Vector3 X, Vector3 Y, Vector3 Z, Vector3 Glow, float Share, int Faces = 0);
Vector3 Middle { get; init; }                            // The middle of its box, against the origin the lights were gathered against
Vector3 X { get; init; }                                 // Half its box's side along the box's first axis
Vector3 Y { get; init; }                                 // Half its box's side along the box's second axis
Vector3 Z { get; init; }                                 // Half its box's side along the box's third axis
Vector3 Glow { get; init; }                              // Its light, the emission of the meshes it is made of, linear
float Share { get; init; }                               // Its surface over that of the faces of its box it emits from
int Faces { get; init; }                                 // The faces of its box it emits from, or 0 for every face
Vector3 Key { get; init; }                               // Its middle in world numbers, in floats, worked from the world matrices as they are, which the lights are known by from frame to frame (GlowPlaces), ranked by and written with, the same wherever the frame's origin stands, where Middle takes a place against the origin the lights were gathered against

class GlowPlaces                                         // The place each glow light is written in, kept from frame to frame
GlowPlaces();
ReadOnlySpan<uint> Kept { get; }                         // The places whose light is the one the frame before had there, a bit a place, the first 32 in the first uint, as the head holds them
int Count { get; }                                       // How many places the lights the last frame wrote take, up to the last taken, as the head's count holds it
void Move(double x, double y, double z);                 // Knows every light by its middle moved by a whole step the program moved its world by, worked in doubles, so each light keeps its place through the move
void Forget();                                           // Lets every place go, for a frame that writes no glow light
```
