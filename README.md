[Devlog](https://github.com/EggyStudio/RadianceCascades/blob/main/DEVLOG.md) [Cheatsheet](https://github.com/EggyStudio/RadianceCascades/blob/main/CHEATSHEET.md)

# RadianceCascades

RadianceCascades is a library for any engine that binds it, taken as a git submodule, for the CPU
side of radiance cascades, their layout, the placement of their probes, the build of the scene field
and the rules of the merge, and their Slang shaders. It references no engine, window or graphics
API, and an engine gives it what it cannot work out where it binds the module.

## Taking it into an engine

An engine adds the repository as a git submodule under `modules/` and references its project, here
from a project one folder below the engine's root.

```bash
git submodule add https://github.com/EggyStudio/RadianceCascades.git modules/RadianceCascades
```

```xml
<ProjectReference Include="..\modules\RadianceCascades\RadianceCascades\RadianceCascades.csproj" />
```

The library's Slang lies under `RadianceCascades/Shaders`, a module a file, which an engine's
kernels import with that folder among the folders its slangc searches.

A clone of the engine fetches the module with `git clone --recurse-submodules`, or with
`git submodule update --init` in a clone made without it, and a workflow checks it out with
`submodules: true` on `actions/checkout`. A change the engine uses is committed and pushed here
before the engine's commit that moves its pointer, so the engine's history never names a commit
this repository does not have.

## Building and testing

The tests take the checks every module keeps from the Tools module, a git submodule under
`modules/Tools`, so a clone fetches it with `git clone --recurse-submodules`, or with
`git submodule update --init` in a clone made without it. An engine that takes the module builds
its library alone, which takes nothing of Tools', and clones with no recursion into it.

With the .NET 10 SDK, and slangc for the tests that compile the library's Slang to SPIR-V and to
WGSL, which `build/fetch-slang.sh` downloads into `build/tools` at the version the script pins:

```bash
build/fetch-slang.sh
dotnet build RadianceCascades.slnx
dotnet test RadianceCascades.Tests
```

Without slangc those tests are reported as skipped, with their reason. The tests that run a kernel
on the CPU take the system's C++ compiler as well, which slangc calls to build the kernel, and are
skipped with their reason where it finds none.

## Binding it in your engine

An engine binds the module by making a `SceneFieldPlan<TMesh, TTag>` and handing it each frame the
eye and the meshes it drew. The plan says which cascades of the scene's distance field are built and
from which still meshes, and which moving meshes are stamped as boxes. The module declares no
interface for an engine to implement, and asks what it cannot work out through the plan's type
parameters and the functions its constructor takes.

| Asked | What the engine gives |
|---|---|
| `TMesh` | What it knows a mesh's vertices by, compared through `EqualityComparer<TMesh>.Default`, by reference for an array or a class with no equality of its own, and boxed on each comparison where it is a struct that does not implement `IEquatable<TMesh>` |
| `TTag` | Its own of each draw, an `IEquatable<TTag>` carried to the builds and compared with the rest of the draw, so a draw whose tag changes is its mesh painted again |
| `bounds` | The box around a mesh's vertices in its own space, a `FieldBox` |
| `triangles` | How many triangles a mesh has |
| `options` | Where it gives them, the plan's settings, a `FieldPlanOptions`, each at a default it may leave, as the table below says |
| `parts`, `tint`, `carried` | Where it gives them, the parts a rigid mesh is stamped as, each a `FieldPart`, which `FieldTriangles.Cut` cuts, the color a mesh's boxes are multiplied by, and whether it carries an emissive mesh as a glow light |
| `GlowOptions` | For `GlowLights.Write`, the box around a mesh's vertices, its surface over its box's, which `GlowLights.ShareOf` works out from its corners or its area, and where it gives them, the glow lights' settings, a `GlowOptions` |
| `GlowPart` | Where it gives them, the parts an emissive mesh is carried as, each a `GlowPart`, which `GlowLights.PartsOf` cuts from its corners, and whose boxes `GlowLights.Carries` reads for the plan's `carried` |
| `FrameOrigin` | Where it takes its places against an origin of its own, the frame's origin in doubles, which the glow lights are gathered against and `GlowLights.Place` places them against, `FrameOrigin.Zero` where it takes none |

| Handed back | What it holds |
|---|---|
| `SceneFieldPlan<TMesh, TTag>.Builds` | Each cascade built this frame, or the part of its build splatted, with its corner, its cells' width and the still meshes it splats, and the `FieldCells` it builds where it builds a region or a shift's slab |
| `SceneFieldPlan<TMesh, TTag>.Shapes` | The moving meshes stamped as boxes, each a `FieldShape`, with the bricks they come within in `Bricks`, `BrickRanges` and `BrickShapes` |
| `FieldTriangles` | The layout of the triangles a build's splat reads, 48 bytes a triangle, written by `FieldTriangles.Write`, a corner's color made by `FieldTriangles.Srgb`, and the size of the buffer that holds them by `FieldTriangles.PoolCapacity` |
| `GlowLights` | The block the glow kernel reads, written by `GlowLights.Write` into the span it is given, a head of `GlowLights.Head` bytes ahead of `GlowLights.Most` lights of `GlowLights.Bytes` each, or the lights alone, and how many carried emitters there were |
| `GlowLight` | A light `GlowLights.Gather` finds, its box's middle and half sides, its light, its share and the faces it emits from |
| `GlowPlaces` | The place each light is written in, kept from frame to frame by the one the engine hands `GlowLights.Write`, with the count of places taken and the bits of those whose light is the frame before's |

The library's Slang, under `RadianceCascades/Shaders`, asks an engine for its own images and
buffers through interfaces its kernels implement, each a struct of the engine's own handed to the
library's functions, and the engine keeps its bindings, its threads and its atomics.

| Slang interface | What the engine's struct reads or writes | Read or written by |
|---|---|---|
| `ISplatCells` | Keeps a cell's least code and the least code of a face it lies straight behind, an atomic minimum on a GPU | `splatCodes` |
| `IPaintCells` | Reads the code a cell kept and paints it its nearest surface's color, emission and gloss | `splatPaint` |
| `IResolvedCells` | Reads a cascade's resolved distances about a cell | `pinched` |
| `IStampShapes` | Reads the shapes of a brick's run, each as `FieldShape.WriteStamp` writes it | `stampCell` |
| `IFieldCells` | Reads a cascade's cells' paint, faces and distances | `paintFacing`, `paintAt` |
| `IFieldVolume` | Reads the finest cascade holding a point, a cascade's corner and cell, its field between cells and a cell as it is | `fieldDistanceIn`, `fieldDistanceSheetIn`, `traceField`, `traceFieldSheets` |
| `IMarchSteps` | Takes each step a march makes, which `NoSteps` passes over | `traceField`, `traceFieldSheets` |
| `IProbeField` | Reads, beyond `IFieldVolume`, the light a cell's surface sends and the share of light that passes a unit of its depth | `traceProbeRay`, `mergedLight` |
| `IFarProbes` | Reads a far probe's light along a texel's way and how far its ray toward each way of its reach map went | `mergedLight`, `farProbeSees` |
| `IProbeLight` | Reads a probe's merged light at each of its texels | `lightTaken` |

The conventions the module stands on:

- Places, cells and reaches are in the engine's world unit, which the module's defaults and
  numbers take as meters, a first cell of 0.25 a quarter meter.
- A `Matrix4x4` of System.Numerics takes a point from a mesh's own space to the world as
  `Vector3.Transform` does, a row vector times the matrix, with the translation in `M41` to `M43`.
- The plan reads no up axis and no handedness, and the octahedron of a probe's directions folds
  along z as a mapping alone.
- `Color` and `Emission` are linear, and a corner's color is three sRGB bytes, red lowest.
- Emission is in the engine's own unit of light, which the plan carries and neither scales nor
  reads.

Each setting has a default an engine may leave, and each trades one thing for another.

| Setting | Default | What it does | What it trades |
|---|---|---|---|
| `FieldPlanOptions.Lead` | 0 | A cascade's middle lies that share of its width ahead of the eye along the way it looks | More of a cascade holds what the eye sees, and a turn of the eye builds the cascade again whole |
| `FieldPlanOptions.StampArrivals` | false | A mesh that came and has not moved is stamped as a box until it is built | The mesh stands in the field from its first frame, where a box stands in badly for a mesh larger than what it holds |
| `FieldPlanOptions.WholeBuilds` | false | Every build is of a whole cascade | The field's kernels build no region and shift no cascade, and each move past a snap or change within a cascade builds it again whole |
| `FieldPlanOptions.RepaintWaits` | false | A still mesh painted again settles at once only once it has stood still for eight frames | A mesh painted every frame is not built in each frame, and a new paint soon after a mesh settled waits a few frames |
| `GlowOptions.Faint` | a 300th | A glow light reaches as far as a surface takes this share of its light | A smaller share reaches farther, and more lights are weighed at each point |
| `GlowOptions.ReachFloor` | true | A light reaches at least as far as a surface half the first cascade's probe spacing from it | A small bulb lights the floor under it however dim it burns, past where its own light would end |
| `GlowOptions.NearestFirst` | false | The 64 lights written are those nearest the eye, not those that send the most about it | Faint emitters beside the eye take the places of a bright lantern down the street |
| `GlowOptions.LightsAlone` | false | The block is the lights alone, the count and the kept bits handed apart through `GlowPlaces` | A kernel reads a plain array, without the head's box that passes a point outside every light's reach |

The smallest binding, with `using System.Numerics;` and `using RadianceCascades;`, is a floor drawn
each frame and its triangles laid out for the splat. The kernels that splat a build's meshes into
its cascade and stamp the shapes into the bricks are the engine's.

```csharp
// This engine knows a mesh's vertices by their positions, which the plan tells apart by
// reference, and its own of a draw is an int it never sets.
var plan = new SceneFieldPlan<Vector3[], int>(cascades: 4, cellSize: 0.25f, budget: 1,
    bounds: positions => FieldBox.Of(positions),
    triangles: positions => positions.Length / 3);

// A floor eight meters a side, two triangles, its corners in its own space.
Vector3[] floor = [new(-4, 0, -4), new(4, 0, -4), new(4, 0, 4), new(-4, 0, -4), new(4, 0, 4), new(-4, 0, 4)];
uint[] corners = [0, 1, 2, 3, 4, 5];
var drawn = new List<(SceneFieldPlan<Vector3[], int>.Instance Instance, bool Skinned)>
{
    (new(Mesh: 1, Vertices: floor, World: Matrix4x4.Identity, DoubleSided: true, Color: new Vector3(0.8f)), false),
};

// Each frame the engine hands the plan the eye and what it drew, and lays out the
// triangles of each build's meshes for its splat.
var triangles = new uint[2 * FieldTriangles.Uints];
for (int frame = 0; frame < 32; frame++)
{
    plan.Update(new Vector3(0, 1.7f, 0), drawn);
    foreach (var build in plan.Builds)
        foreach (var instance in build.Instances)
            FieldTriangles.Write(triangles, instance.Vertices, corners, []);
}
```

## The kernels without the library

The Slang under `RadianceCascades/Shaders` needs nothing of the C# library, so an engine in any
language takes it with slangc alone, each file a module its kernels import with that folder among
the folders slangc searches. A kernel of the engine's own, here with its compute entry `run`,
compiles to each target slangc writes itself:

```bash
slangc kernel.slang -I RadianceCascades/Shaders -entry run -stage compute -target spirv -o kernel.spv
slangc kernel.slang -I RadianceCascades/Shaders -entry run -stage compute -target wgsl -o kernel.wgsl
slangc kernel.slang -I RadianceCascades/Shaders -entry run -stage compute -target hlsl -o kernel.hlsl
slangc kernel.slang -I RadianceCascades/Shaders -entry run -stage compute -target metal -o kernel.metal
```

Vulkan reads the SPIR-V and WebGPU the WGSL, and the HLSL and the Metal are compiled to DXIL and to
a Metal library by each platform's own compiler, dxc and Apple's metal, which slangc calls itself
where they are installed. Metal reserves the name `main`, so a kernel compiled for it names its
entry otherwise. The tests compile their own kernels, under `RadianceCascades.Tests/Shaders`, to the
four with the slangc `build/fetch-slang.sh` fetches.

## License

The Mozilla Public License 2.0, in [LICENSE](LICENSE).
