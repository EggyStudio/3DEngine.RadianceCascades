[Devlog](https://github.com/EggyStudio/RadianceCascades/blob/main/DEVLOG.md) [Cheat sheet](https://github.com/EggyStudio/RadianceCascades/blob/main/CHEATSHEET.md)

# RadianceCascades

RadianceCascades is a library both engines, 3DEngine and BevyCSharp, take as a git submodule, for
the CPU side of radiance cascades, their layout, the placement of their probes, the build of the
scene field and the rules of the merge, and their Slang shaders. It references no engine, and each
engine implements the interfaces it declares where it binds the module.

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
WGSL, which `build/fetch-slang.sh` downloads into `build/tools` at the version both engines pin:

```bash
build/fetch-slang.sh
dotnet build RadianceCascades.slnx
dotnet test RadianceCascades.Tests
```

Without slangc those tests are reported as skipped, with their reason.

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
| `forms` | A `FieldForms` with each of its four forms named |
| `parts`, `tint`, `carried` | Where it gives them, the parts a rigid mesh is stamped as, each a `FieldPart`, which `FieldTriangles.Cut` cuts, the color a mesh's boxes are multiplied by, and whether it carries an emissive mesh as a glow light |
| `GlowForms` | For `GlowLights.Write`, a `GlowForms` with each of its three forms named, the box around a mesh's vertices, and its surface over its box's, which `GlowLights.ShareOf` works out from its corners or its area |
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

An engine new to the module names every form false. A cascade then lies around the eye, so a turn
builds nothing again, a mesh that came stands in for nothing until it is built, since its box stands
in badly where it is larger than what it holds, and regions and shifts are built, where the engine's
field kernels build them. Those are the forms most of the plan's tests hold. `RepaintWaits` set
keeps a mesh painted again every frame, as one whose paint is animated, from being built in each
frame, and fewer tests hold it. `WholeBuilds` set is for field kernels that build only whole
cascades.

The smallest binding, with `using System.Numerics;` and `using RadianceCascades;`, is a floor drawn
each frame and its triangles laid out for the splat. The kernels that splat a build's meshes into
its cascade and stamp the shapes into the bricks are the engine's.

```csharp
// This engine knows a mesh's vertices by their positions, which the plan tells apart by
// reference, and its own of a draw is an int it never sets.
var plan = new SceneFieldPlan<Vector3[], int>(cascades: 4, cellSize: 0.25f, budget: 1,
    bounds: positions => FieldBox.Of(positions),
    triangles: positions => positions.Length / 3,
    forms: new FieldForms { Ahead = false, StampsArrivals = false, WholeBuilds = false, RepaintWaits = false });

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
