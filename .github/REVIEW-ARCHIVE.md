# Review archive

The replies a review no longer needs at hand, oldest first, moved here whole so `REVIEW.md` stays
under its size.

## Replies archived

Decision 6's first Reply, this file read against the tree at `6b7bcd8`, the last commit here and on
origin, and both engines' copies read in place at 3DEngine's `ee7ac712` and BevyCSharp's `8e7fa0f5`,
with the design read of 2026-11-14 beside them. Nothing was built or run, and nothing was written in
either engine.

**Done.** Items 1 and 2 at `8811a8e` and `8cc387d`, and items 5, 6 and 7 at `7806020`, `13d21db` and
`040353c` with `21761ff` and `ad7841e` after them, as the head says. The head does not name
`6b7bcd8` of 2026-11-17, the ignore file's note and the guides' spelling of canceled, which is on
origin.

**Open.** Item 3 has no part in the tree, the library being item 2's empty project with its one test
that it loads, and item 4 has none, 3DEngine's `.gitmodules` holding Sky alone and BevyCSharp's
Levels, Sky and Vegetation. DEVLOG.md ends at 2026-10-31, since items 5, 6 and 7 here carry no Done
at their commit, which Tools' `scripts/devlog.py` reads to write an item. KERNELS.md reads 3DEngine
at `3768c988` and BevyCSharp at `36840712`. Since those, 19 commits of 3DEngine changed 54 files of
the folders it reads, 3,215 lines added and 420 removed, and 16 of BevyCSharp changed 57, 2,627
added and 570 removed (`git rev-list --count` and `git diff --stat` from those commits to each head
over the same folders). The kernels' own files moved little, the most 3DEngine's `glow.slang` by 127
lines added and BevyCSharp's `gi_reference.slang` by 86 (`git diff --numstat` a file), so
KERNELS.md's tables stand but for the changes said below, and a kernel's lines are read again at the
heads as it moves.

**Each kernel, what differs, why, and what the module takes.** KERNELS.md's section of each name
holds the lines. The later form is the one the history shows moved after the copy.

- `field_splat`. 3DEngine's code is the later, with a rim bit, the second plane and, since
  `96358279` of 2026-11-14, seven bits of the instance last, so a tie of two meshes' faces goes the
  same way in every run, and its build takes a mesh's rows against its cascade's corner, so the
  field far out reads as at the origin. BevyCSharp's code is 3DEngine's of before 2026-10-31, its
  build is in world floats, and its leaf cards, its reflectance in the gloss and, since `e19b2575`
  of 2026-11-17, the share a surface sends through in the gloss's lowest byte are its own. WGSL asks
  for `Atomic<uint>` and has no packed halves, so BevyCSharp's atomics and unpacked emission are the
  target's. The module takes 3DEngine's code, build and second plane, BevyCSharp's leaf cards,
  reflectance and share as data both engines fill, and the atomics and the emission's packing as the
  two targets' forms of one source.
- `field_resolve`. 3DEngine's double-sided distance with its plane in `rgba16f` is the mended form,
  since with BevyCSharp's half a cell taken off a sheet thinner than a cell reads a cell thick and a
  ray stops half a cell before its plane. 3DEngine resolves the build's region and clears the codes
  by a fill. BevyCSharp resolves the whole cascade, clears by a dispatch since WGSL has no fill, and
  since `e19b2575` writes the share sent through into an `r16f` image of its own. The module takes
  3DEngine's plane and region, the clearing dispatch in both targets, and the share's image as the
  design read's question 2 is answered again.
- `field_stamp`. The brick, the box's distance and the nearer surface's paint are one code. 3DEngine
  stamps the sheet's plane and glow, and BevyCSharp a shape's gloss and, since `e19b2575`, its share
  sent through. BevyCSharp reads the still images as textures, since WGSL reads and writes no image
  of halves or bytes in one kernel. The module takes one shape carrying emission, gloss and share,
  and the still images read as textures in both.
- `scenefield` and `bcs_field`. 3DEngine's table of 208 bytes with the waiting box, its corners
  against the origin and `cascadeAt` reading the table in place are the later, where BevyCSharp's
  table is 176 bytes and `probesOf` copies it. 3DEngine splits its samplers for Metal's sixteen. The
  module takes `FieldTable` with the waiting box, the lookup in place, and the images behind
  `IFieldImages`, so each engine binds them its own way.
- `glow` and `bcs_glow`. One light of 80 bytes, at most 64, with the same marches. 3DEngine's is the
  later at its head, which reads a glow light's open faces from the field on the GPU (`ba117e9f` of
  2026-11-15, the design read's question 11), aims and widens a hit's march at the faces that emit
  toward it (`4f205366`, `da3d20cc`, `029f60de`) and keeps the 64 that send the most about the eye
  (`275f01d1`). BevyCSharp clips a square edge by edge with no array, since a local array spills to
  slow memory, and reads a far light as one point for every caller. The module takes 3DEngine's with
  BevyCSharp's clip, and `GlowLights`'s choice on the CPU with it.
- `lightthrough` and the crossing of `bcs_field`. 3DEngine steps out of a solid by `max(0.5 * cell,
  -d)` and BevyCSharp by half a cell always. BevyCSharp's leaf cards are absorbed cell by cell, and
  3DEngine's `metPastLeaves` has no like in BevyCSharp. The module takes 3DEngine's step and march
  and BevyCSharp's absorption.
- `gi_trace`. Unchanged in 3DEngine since the document and changed by 5 lines in BevyCSharp.
  3DEngine's carried rays through sixteen places, its move through a narrow space and its sky past
  the cascade above are the later, and its mesh hits through ray queries are its own. BevyCSharp's
  leaf layers, up to 5 shaded at the crown's depth, and its light scattered before the interval are
  its own. The module takes 3DEngine's trace with BevyCSharp's leaves and scattered light, the sky
  behind `ISky` (question 6) and the hits behind `IProbeRays`, the field's march the default of both
  and the ray queries 3DEngine's (question 10).
- `gi_merge`. Unchanged in both. 3DEngine's mean of the four rays' cells is the mended read of a
  reach, BevyCSharp's nearest texel reading too dark past a corner, and 3DEngine reads the frame
  before's reach where BevyCSharp reads this frame's. 3DEngine's agreement march and a carried
  probe's sixteen-frame cycle are the later. `mergedSky` follows the sky's share, which BevyCSharp
  dropped on purpose at `e0014da4`. The module takes 3DEngine's read on this frame's reach (question
  7), the march and the cycle, and keeps the sky's share 3DEngine's until the atrium's figures say
  which part of the dark atrium the kernels carry.
- `gi_ambient`. BevyCSharp mended its sum over a probe's rays at `997d407e` of 2026-11-14, so the
  two agree there. 3DEngine's patch normal from the trace's kept mean is the later and the cheaper,
  and BevyCSharp's scattered light in the early light is its own. The module takes both, and lays
  the reach out where the trace's layout settles.
- The probes' readers, `gi`, `cascades` and `probelight` against `bcs_gi`. 3DEngine's fade, its hold
  of the far cascade, its faces moved toward the surface and its one glow march are the later, and
  BevyCSharp's blended reach read of 2026-11-10 is the later of that read. The hits' surface
  responses differ, 3DEngine's GGX at an `f0` of 0.04 against BevyCSharp's split-sum at `0.16 *
  reflectance^2`, and neither is the mended one. The module takes the later of each and holds both
  responses as a constant each engine sets (question 12) until a room against its reference chooses.
- `gi_screen`. 3DEngine's cascades a probe stands in, its places turned each frame, its faint lights
  and, since `6309f746` of 2026-11-16, a ray handed to the field past a stretch behind the depth are
  the later, and its tree holds more work on the file uncommitted. BevyCSharp's normals that pass
  the sky over and its faint lights grouped by way are its own, and its glow bytes in four images
  are WGSL's form of 3DEngine's buffer. The depth is each engine's, half size of the shadow casters
  in 3DEngine and Bevy's reversed prepass in BevyCSharp. The module takes 3DEngine's with
  BevyCSharp's normals, the depth behind `IScreenDepth`.
- `gi_screen_filter`. 3DEngine's is the later throughout, and BevyCSharp's history holds the blended
  light, so its blend feeds back. The module takes 3DEngine's, BevyCSharp giving the history images
  it reads.
- `gi_screen_glow`. One code line for line. The texel's size, 2 pixels up to 1080 rows in 3DEngine
  and half the picture in BevyCSharp, and where the glow bytes stand are each engine's to give.
- `gi_reference`. Each engine's own, bound to its ray scene and materials, BevyCSharp's changed by
  86 lines since for its textures' means and blended faces. The module keeps the references the two
  trace.
- BevyCSharp's five of no like by name. `gi_sky` writes the sky BevyCSharp's `ISky` gives and stays
  its own. `gi_light` keeps its take-back of Bevy's terms, which `4981d5b5` of 2026-11-18 changed,
  and gives the module its probes' reads and `reflectedAt`, beside 3DEngine's `roughreflect`.
  `field_shadows`'s march moves as a function beside 3DEngine's `sunReaching`, where its use stays.
  `field_slice` and `gi_frame` are views and stay.

**What stays an engine's own.** KERNELS.md's list holds at the heads. An engine keeps its bindings,
a file of its own a kernel that binds its resources and calls the module's generic body, its view,
its frame's origin, its depth, the producers behind the interface (3DEngine's environment cube and
shadow maps, BevyCSharp's `gi_sky`), 3DEngine's ray scene, Bevy's terms in BevyCSharp's light pass,
its views, its `gi_reference`, and its tests and rooms.

**The interface.** KERNELS.md's ten things written out, cut as the design read's question 12 cuts
them. The engine gives the scene's drawn instances, the frame (its number, the origin in doubles,
the world's step, the eye, the switches and a scale of the light's units), the lights, the sky, the
sun's reach, a ray scene where it has one, the screen's view, and its services and hooks in the
frame's order, and it sets one choice, the hit's surface response. The module owns the field's
formats and bits, the table, `ProbeRun` and `ProbePlan`, the kept images' flags, the glow lights and
the screen's plan, and hands back the probes' images and `ProbePlan` for the engine's light pass. Of
the design read's twelve, the module takes 1, 3, 4, 5, 6, 10 and 12 as written. 11 is in 3DEngine's
head and moves with `glow`. 7 and 8 change what a frame reads and land each as a commit of its own
after the move. 9 is BevyCSharp's item 141. 2 is asked again, since BevyCSharp gave the share sent
through an image of its own after the design read.

**The order of the move.** One commit a part here with its tests, each part holding both engines'
forms where they differ behind a constant each engine sets, so each engine's tests and rooms read as
before through the move, and each form dropped after it is a commit of its own with its failing test
or its pair of pictures. What the module takes above is where each part ends. Each part is followed
by each engine's commit that moves the pointer and binds it, the first of them item 4.

1. The Slang built here. `build/fetch-slang.sh` as the engines' at their pinned 2026.18.2, the
   workflow fetching it, and a test that compiles every file of the module to SPIR-V and to WGSL
   with no GPU, skipped with its reason where slangc is absent (N 3.2).
2. The field's plan and the triangles' pool, in C#, both engines' `SceneFieldTests` the oracle.
3. `GlowLights`'s choice and layout, in C#.
4. The readers' module with `field_splat`, `field_resolve` and `field_stamp`, the box of 2 in both
   engines holding them. After it BevyCSharp takes 3DEngine's instance bits and its build against
   the cascade's corner, each a commit with its test.
5. `glow` and `lightthrough`, which the probes import.
6. `gi_trace`, `gi_merge` and `gi_ambient` with the probes' readers, held by the nine bounce rooms
   of both against one set of references kept here. Then BevyCSharp's item 141 with the closed
   room's test failing first in both engines, then the design read's 7 and 8.
7. `gi_screen`, `gi_screen_filter` and `gi_screen_glow`, once 3DEngine's screen work is committed.
8. The light on the picture's shared functions with `roughreflect` and `reflectedAt`, and the
   contact shadow's march.

The rays traced by the adapter, BevyCSharp's item 144 and 3DEngine's item 271, come after the move,
behind `IProbeRays`.

**Two surfaces of unlike color in one field cell.** Both engines keep one paint, one face's way and
one gloss a cell, the triangle's with the least code, and a hit blends the eight cells about it that
face within 60 degrees of its normal, falling back to `surfaceAlbedo`'s blend where none does
(`surfaceFacing`, 3DEngine's `3DEngine/Shaders/gi.slang:232-261` and BevyCSharp's
`native/bevy_csharp/src/render/bcs_field.slang:257-286`). Two unlike surfaces that face apart, as a
red wall meeting a white one, part by that test across cells, as BevyCSharp's comment at `:242-247`
says. Within one cell one surface alone keeps its paint. Of two faces back to back, as a sheet
painted unlike on its two sides, the code's side bits give the cell to the face it lies behind, so
one side's hits may find no cell facing them and read the other side's paint through the fallback.
Two faces of one mesh tied on every bit take the last thread's paint in both engines, since
3DEngine's instance bits part two meshes alone. The read writes the test first, a sheet half a cell
thick painted red on one face and white on the other and lit from either side, and a floor checkered
red and white by the cell, in both engines against the reference. Its numbers choose between one
paint a cell, a second paint beside 3DEngine's second plane, and a facing read that reaches a cell
further before the fallback.

**How many of the finest world probes stand in air or in solids.** Each trace writes a probe's place
into `held`, its move in its spacing with w 1 where it then stands outside every mesh (3DEngine's
`3DEngine/Shaders/gi_trace.slang:185`, `359-360`, BevyCSharp's
`BevyCSharp/GlobalIllumination/Kernels/gi_trace.slang:166`), and a probe inside a mesh sends
nothing. The count is a read back of the first cascade's places in both engines over the same rooms,
the closed room of item 141, the atrium of 3DEngine's Decision 118, the nine bounce rooms and
Sponza, giving for each room the probes in air, those moved off a surface and those inside. It says
how much of the finest cascade's work goes where nothing is read, and how many of the probes item
141's near wall reads stand inside its walls. It runs on the GPU, so it is taken with the engines'
coders through N 7.9's lock as the read's first measurement.

Part 2b, the plan's second forms, in the commit carrying this line. `FieldForms` is a public record
of four members, `Ahead`, `StampsArrivals`, `WholeBuilds` and `RepaintWaits`, each a way the two
engines' plans part, its defaults 3DEngine's and every member set BevyCSharp's. It fits the two
engines alone and is named here as Decision 7 asks, and its rows in KERNELS.md's table of the forms
held twice close by the field's kernels, when it goes or narrows. The instance and the shape carry a
reflectance and a share sent through, and the plan gains `Lead`, `DrewTwice` and the way the eye
looks. 10 of BevyCSharp's 16 tests of its plan hold with every member set, each member failing one
of them when cleared, and six stay in BevyCSharp, five on its GPU and one of its bridge's layout.
The budget's lift after a cut, which BevyCSharp's plan lacks, comes to it with the module, none of
its tests reaching it.

The binding, in the commit carrying this line. `FieldForms` takes its four members as required
properties and the plan's constructor takes the forms with no default, after the triangles, so a
plan is made only with each form named; the README's section Binding it in your engine says an
engine new to the module names every form false, and why, and when `RepaintWaits` or `WholeBuilds`
is set. Its C# block is the host's in BindingTests.cs, run with no engine and compared with the
README line for line by a test of its own until the shared checks are taken.

Part 3, the glow lights' choice and layout, in the commit carrying this line, with its sheet.
`GlowForms` is a public record of three required members, `FaintThousandth`, `NearestFirst` and
`LightsAlone`, each a way the two engines' glow lights part, cleared 3DEngine's and set
BevyCSharp's, named here as Decision 7 asks, its rows in KERNELS.md's table closing by the glow
kernel. The module owns the glow block alone, its head and its lights, and the place of the block in
an engine's own lights is the engine's. `FrameOrigin` in doubles takes the place of 3DEngine's
origin and its world's step, zero for an engine that has none.

Four ways no form holds come to BevyCSharp with the module where it binds the glow lights, and its
rooms hold them. A copy recorded twice is one light. A flat emitter emits from its two broad faces
at twice its share, where BevyCSharp's emitted from all six faces of its thickened box at a share of
1, a flat quad's surface so about 2 percent less and a disc's a quarter of pi. A box of no size
takes no share where BevyCSharp's took 0.05. `Write` returns the lights found, the places taken
being in `GlowPlaces.Count`. A form is added for any of them that BevyCSharp's rooms read apart.
Nine of 3DEngine's CPU tests of the glow lights come with it, twelve cases, and one under
BevyCSharp's forms; the rest draw, or read the engine's shaders, and stay.
