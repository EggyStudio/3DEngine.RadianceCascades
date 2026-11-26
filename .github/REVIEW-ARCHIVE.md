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

The sweep, in the commit carrying this line, with Tools at its head `7cb9e97`, the README check
given the library again and the sixth fact, no tracked text naming an engine. `FieldForms` and
`GlowForms` are `FieldPlanOptions` and `GlowOptions`, sealed records of settings each at a default,
every summary saying what the setting does and its remarks what it trades, and the constructor and
`GlowLights.Write` take them last, null for the defaults. `Ahead` and the constant `Lead` are the
one setting `Lead`, 0 for none. The way the eye looks stays a parameter of `Update` and
`OriginAround`, since a lead needs a way to lie along. `FaintThousandth` is `Faint`, a share, with
`ReachFloor` beside it, and `StampsArrivals` is `StampArrivals`. `DrewTwice` is `DrawnTwiceInPlace`.

The diagnostics stay as they are, since `Waiting` sums with LINQ and `Unsettled` builds an array, so
one value gathering them would allocate each frame an engine reads it where a single property read
does not. KERNELS.md leaves the repository, and the README's binding section holds the settings'
table, each with its default, what it does and what it trades. The sheet is made again with its
title line and gray, from the settings.

The color a hit reads from the field, in the commit carrying this line, with the reading of the
cells about a box and a thin sheet. The finding of two unlike surfaces in one field cell is mended
in the module's field. Its found case is a floor over another of another color a tenth of a unit
below, both facing up, whose cells between lie nearer the lower and face the hit's way as the
upper's do. Read as both copies read it, the eight cells about the hit facing within 60 degrees of
its normal, the upper floor's white read (0.77, 0.90, 0.77), the green blended in by a quarter,
which the test failing first said. `paintFacing` counts a cell's paint only where the surface it
holds, its distance back from its middle along its face's way, passes within `PaintReach`, 0.3 of a
cell, of the hit, past the way's eight bits' error across half a cell and short of a second surface
four tenths of a cell off, and falls back to the cells that face the hit's way and then to the plain
blend where none holds a surface through it. The red wall on a white floor and a slab half a cell
thick, red above and white below, read their own colors under both reads. A second surface nearer
than 0.3 of a cell to the first still blends in, which no test here reads.

The paint's reach, in the commit carrying this line. No measurement chose 0.3 of a cell; it lies
between two bounds. Above it the found case's second surface lies 0.4 of a cell off the hit. Below
it a face's way read back from its eight bits leans up to 14.15 degrees from it over 65,536 ways,
the largest near the octahedron's folded corners
(`FieldCodeTests.A_Faces_Way_Read_Back_From_Its_Eight_Bits_Lies_Within_Fourteen_And_A_Half_Degrees_Of_It`),
so across the cells about a hit, up to 1.41 cells along the surface, a surface held through it may
pass up to 0.35 of a cell off at the coarsest ways. My first reading took four degrees for every
way, which is the lean of the sheet's own way alone; the sheet is drawn again saying so. Such a cell
is left out and the cells facing the hit's way lend the color, so no hit of one surface reads
another, but a second surface 0.3 to 0.35 of a cell off may still blend in at those ways. A floor a
twentieth of a unit over another, a fifth of a cell, blends a quarter of the lower in, 0.74 of the
white with 0.26 of the green as the cells' weights give, which
`FieldPaintTests.A_Floor_Over_Another_Of_Unlike_Color_A_Twentieth_Below_Blends_A_Quarter_Of_It_In`
states, so a change to the read shows there.

The splat and the stamp, the splat at `881fa74` and the stamp in the commit carrying this line. What
an engine's binding takes from the library in place of its own shaders' code, kernel by kernel, each
kernel named by what it computes and the engine keeping its bindings, its threads, its atomics and
its dispatch:

1. The probes' directions, every pass's octahedral map, its share, the rays a texel sends and a
   texel's marks, from `rc_octahedron`.
2. The splat's distance pass, the body each lane runs over a triangle's band of cells, from
   `splatCodes` over an `ISplatCells` the engine keeps by its atomic minimum; the code itself, the
   nearest point with its open edges, the way's eight bits and the passing over of a sliver, from
   `rc_field_code`.
3. The splat's paint pass, the cells that kept a triangle's code painted its color blended at the
   nearest point, from `splatPaint` over an `IPaintCells`, with `lightThroughPaint`, `cornerColor`,
   `barycentric` and `glossBits`.
4. The resolve's distance, a code and the face behind it made the distance a reader marches, the
   thin wall held, from `resolvedDistance`; the plane a double-sided face keeps, from `sheetOf`; the
   pinch between two blocks, from `pinched` over an `IResolvedCells` the engine reads its resolved
   cells through.
5. The stamp's cell, the least of a still cell and the boxes of its brick with the nearer surface's
   paint, face and gloss, from `stampCell` over an `IStampShapes`, each shape laid out as
   `FieldShape.WriteStamp` writes it, 32 floats.
6. The readers' color at a hit, from `paintFacing` and `paintAt` over an `IFieldCells`, the mend of
   two unlike surfaces in one cell with it, and the face's gloss from `glossOf`.

Not yet the library's: the splat's second plane, kept where a second double-sided face of another
way passes a cell, the lending of emitters thinner than a cell, the paint again of a mesh whose
colors changed in place, the clearing of a cascade's codes, and every reader of the field's distance
and of the probes. The kernels' own files, each a binding over these, stay each engine's.

The field's distance at a point and the march, in the commit carrying this line, with the march's
sheet. `rc_field_read` holds `IFieldVolume`, the cascades as a reader reads them, the finest holding
a point, a cascade's corner and cell, its field between cells and a cell as it is, and its second
plane; `fieldDistanceIn`, the cells' blend held half a cell below a double-sided face's plane;
`fieldDistanceSheetIn` and `sheetDistance`, the probes' read of a cell's planes held `SheetHalf`
either side; and `traceField` and `traceFieldSheets`, 64 steps at most of the distance read or a
quarter of a cell at least, met within a tenth of a cell, each step handed to an `IMarchSteps`,
`NoSteps` where none is kept. The tests, written first, measure the blend about a box of two 0.286
of a cell off the box's own distance at worst over 400 points, held to 0.3; rays stopping 0.043 of a
cell short of a face and 0.144 past it at worst, within the march's tenth and its quarter-cell step;
and a ray from either side of a sheet stopping on its own side within 0.35 of a cell of the plane.
The README's binding section names every Slang interface an engine implements. Not yet the
library's: the field's normal, the table of cascades an engine's `cascadeOf` reads, and the march
held within a cascade.

Handoff, for a fresh session. Where the work stands: the library holds the field's plan
(`SceneFieldPlan`, `FieldPlanOptions`), the triangles' layout (`FieldTriangles`), the glow lights
(`GlowLights`, `GlowOptions`), and in Slang the probes' octahedron (`rc_octahedron`), the cell code,
resolve and pinch (`rc_field_code`), the hit's color read (`rc_field_paint`), the splat and its
paint (`rc_field_splat`), the stamp (`rc_field_stamp`) and the field's distance and march
(`rc_field_read`), each run on the CPU by its tests through `CpuKernel` (`slangc -target
shader-sharedlib`). Tools is pinned at `1e1ec63` through `modules/Tools/scripts/pin.py`. Next in
order: the probes' kernels, trace, merge and gather with their readers, a hit taking the split sum
from a 32 by 32 Slang table the tests hold to Materials' `SurfaceResponse.SplitSumIntegrated`; then
the light through surfaces, the screen probes, the light on the picture and the contact shadow's
march. Left behind each part's Reply: the field's normal, the cascades' table, the march held within
a cascade, the splat's second plane, the lending of thin emitters, the repaint and the clearing of
codes. Open: AGENTS.md's documents rows for DEVLOG.md and CHEATSHEET.md wait for the owner. Read
first: AGENTS.md, this file's head, Decisions 6 to 8 and these Replies, the README's binding
section, NORM.md's N 4.8 to 4.11 and N 7.9, and the tests' `CpuKernel.cs`. Every build and test goes
through N 7.9's lock (`flock -s /run/user/1000/gpu.lock` once `/run/user/1000/gpu.waiting/` holds no
living process's file), restored outside it and built with `--no-restore
-p:UseSharedCompilation=false` and `MSBUILDDISABLENODEREUSE=1`, and each commit is read by
`commitcheck.py . --staged --message=<file>` first.

The probes' trace, the merge between cascades and the gather, on the CPU, in the commit carrying
this line. `rc_probes` holds `ProbeCascade`, `traceProbeRay`, which crosses a surface that lets
light through and carries share to the power of the depth crossed, a cell whose surface lets none
through ending the crossing, the reach map (`reachAlong`, 16 by 16 ways) and the gather
(`lightTaken`, `reflectedLight`); a cell carries its surface's share of light through as a value of
its own (`throughAt`), not in the paint's alpha. `rc_cascade_merge` holds the agreed rule,
`mergedLight`: a near ray past its reach takes the light of the eight far probes about its end E at
their trilinear weights, a probe inside a mesh or not seeing E (`farProbeSees`, slack an eighth of
the far spacing) weighing nothing, and marches on itself where none counts. The four tests, each
failing first: the open box's floor reads 0.2540 against the dome's 0.2511 (+1.1%), and 0.3119
(+24%) with every far probe at its trilinear weight; the lidded box reads 0 (0.0580 without the
rule); the box lidded by a sheet reads 0.70% of the open one, under 1% with little room; the dark
room behind a wall passing 0.707 straight on reads 0.675 of its floor with the wall away. The
self-march is taken by 0 of 256 texels in all four; forced on every texel, the open box reads 0.2470
with 128 texels marching 4.9 steps on the mean and 11 at most. Open, for your word: the far probes'
rays begin at the probes, the far interval from 0, not 4 units out, since probes about E whose rays
begin 4 out leave the near ray's next 4 units past E untraced, and the open box then reads 0.900,
3.6 times the dome's. The phase's picture follows that word.

The cost of the far rays begun at the probe and the phase's picture, in the commit carrying this
line. Counted by the tests' kernel, a far probe's light ray takes 7.58 steps on the mean and 58 at
most begun at the probe against 3.90 and 47 begun 4 units out in the open box, 7.49 and 61 against
3.71 and 50 in the lidded box, 7.48 and 58 against 3.74 and 47 in the box lidded by a sheet, 8.23
and 68 against 4.52 and 70 in the rooms parted by the wall and 7.25 and 68 against 3.87 and 54 with
the wall away, about 1.9 times the steps on the mean and alike at most, over 46080 rays in the boxes
and 38400 in the rooms. Begun 4 out, the floors read 0.900 in the open box, 0.696 lidded, 0.772
lidded by a sheet and 0.376 in the dark room with the wall and without it alike. The self-march
stays, and no texel of the four tests takes it, nor any at the picture's 15 probes. A scene that
would is one where a near ray ends in a space narrower than the far probes' spacing, a duct, a
closet or the gap between two walls, so the eight far probes about its end all stand in the walls or
beyond them. The picture, `2026-11-19-item-3-open-box-rules.webp`, reads the floor across its middle
under the trilinear weights alone, the agreed rule and that rule with the far rays begun 4 out,
beside the dome's share there. The agreed rule reads 0.2540 at the middle against the dome's 0.2511,
and 17% over at worst near the walls, the trilinear weights alone up to 48% over, and the far rays
begun 4 out up to 3.6 times.

The probes' passes, in the commit carrying this line. `rc_probe_passes` holds what an engine's
kernel calls once a thread with its index, past the last index doing nothing: `traceTexel` writes a
texel's ray to `IProbeRays`, `reachTexel` a reach texel to `IReachTexels`, `settleTexel` gives the
coarsest cascade's texels their own light or the sky, `mergeTexel` merges a texel with the merged
light of the cascade above through `IFarProbes` into `IMergedLight`, and `gatherAt` takes the finest
cascade's probes about a surface point into `ISurfacePoints` by the merge's own weights. Every
cascade's rays begin at its probe, the engine setting each one's end in its `ProbeCascade`, farther
each cascade up and the coarsest past the field's edge, each end beyond its spacing times the root
of three so its reach map covers the seeing test. Dispatched a thread a texel, the passes read as
the one-thread trace in all five fields with no difference, and three cascades ending at 2, 4 and 32
read the open box's floor 0.2317 against the dome's 0.2511 (7.7% under) and the lidded box's 0. For
your word: in a chain longer than two, a texel no probe above sees marches on to the coarsest
cascade's end, an extension of the agreed rule past a pair. `SlangInterfaceTests` holds every public
Slang interface to a row in the README's table, which failed first on the four new ones. `CpuKernel`
compiles a kernel once a run and runs one kernel's dispatches one at a time. For the engines' change
list: a cell carries its surface's light-through as a value of its own (`throughAt`), not in the
paint's alpha, which the README's binding section states.

The three cascades' loss, read by switching each link of the chain against the finest cascade's
texels each marched on itself to 32, which read the open box's floor 0.2470 against the dome's
0.2511. The finest-to-middle link by the rule, the middle's texels marched on themselves, reads
0.2504, 1.4% over the reference; the middle-to-far link alone reads 0.2116, 14.3% under; both links
by the rule 0.2317, 6.2% under. The middle cascade ending at 4 hands its rays to the far probes some
6 units out, 2 under the box's rim, where the far probes about that point stand up to 2 units off
the ray and their rays meet the rim, so the opening's edge is smeared across the ways about it and
light moves from the steep texels, which weigh most on the floor, to shallow ones toward the walls.
It is not the lookup between cascades (16 texels at each), the grids' edges (no texel marched on
itself) or the slack (a quarter of it reads the same). The same hand-off lets 4.28% of the open
box's light through the sheet lid in three cascades, which the committed test does not read. Ending
the middle cascade at 2, twice its spacing, so its rays reach the far cascade 4 out as in the pair,
reads the open box 0.2548 in three cascades (1.5% over), the lidded box 0 and the sheet-lidded 0.81%
of the open box's, and leaves the pair at 1.1% over, so a test at 5% holds. Reading each far probe
toward where its own ray stopped, or toward a point two far spacings along the ray, mended the open
box and let 4.4 to 9.3% through the sheet lid. A hand-off that lands within a far spacing of an edge
still smears it, so short ends lessen the loss in other scenes and do not end it. For your word: the
mend, the three-cascade test's middle end at 2 held to 5% with the sheet-lidded box beside it, and
the note on ends in `rc_probe_passes` that engines set their ends by.

Decision, from the review, recorded as the probes' readers enter: a hit takes the split sum with its
cell's own reflectance, its shares from the integrated table, a 32 by 32 Slang constant array the
tests hold to Materials' `SurfaceResponse.SplitSumIntegrated`, with a floor of 0.045. It is not yet
built; a hit's light is read through `IProbeField.surfaceLightAt` until it is.

The mend, agreed, in the commit carrying this line: the three-cascade test's middle cascade ends at
2, twice its spacing, held to 5 percent, reading the open box's floor 0.2548 against the dome's
0.2511, and reads the box lidded by a sheet beside the lidded one, 0.81 percent of the open box's
where reaching 4 let through 4.28; the note on ends in `rc_probe_passes` says why, and the README
states the ends' rule as a convention a binding follows, a cascade's end no farther than twice its
spacing and past its spacing times the root of three, with its limit, that an edge within a far
spacing past a hand-off is still smeared.

The passes' four additions a binding asked for, in the commit carrying this line, each held by a
test in `ProbeMergeTests.Bindings.cs` run failing first. `IProbePlaces.placeOf`, `GridPlaces` its
default, gives each probe's place, read by the trace, the reach map, the merge's tests of a probe
inside a mesh and of seeing and the gather, the trilinear weights staying on the grid: probes
standing on the open box's floor read it 0, and moved a quarter unit up along the field's normal by
the test's rule read 0.2540 against the dome's 0.2511. `IProbeField.hitLight(place, way, cascade,
cell)` takes the place of `surfaceLightAt`, a hit's light as the binding shades it, read in the
march and the march-on alike, with `fieldNormalIn` in `rc_field_read` for the normal a split sum
needs: a probe 3 units from a sunlit wall read it 0 by emission alone and 0.1574 against 0.1575
through either. `ISky.skyAlong(way)`, `UniformSky` its default, gives the light of every cascade's
ray that leaves the field and of the march-on: the open box under a sky varying with the way read
0.2540 by a uniform sky of its scale and 0.4779 against the sky's integral over the opening, 0.4789.
`farTexelsLight` gives a near texel the far texels it covers, weighed by their solid angles, where
the far cascade keeps a whole multiple of the near's texels a side, and the one its middle way falls
in at any other count: 8 near to 16 far under the varying sky read 10.5% off the texels' own rays at
worst by the middle texel alone and 1.9% by the covered ones. Shape changes: `mergedLight` takes the
places and the near texel in place of its way, `lastCascadeLight` the way and the sky, `traceTexel`,
`reachTexel` and `gatherAt` the cascade's places, `mergeTexel` both cascades' places and the sky,
and `settleTexel` the sky; the README has rows for `IProbePlaces` and `ISky` and a convention on far
texel counts.

A finding from the hit's test, for your word: at a near reach of 4 the merge read the sunlit wall
37% under (0.0989), the hand-off landing 0.4 units before the wall and the far probes' rays passing
its edge, the smear the ends' rule names, here before a lit wall and not at a rim. The march alone
(reach 8) and the march-on (reach 2) read it within 0.1%, so the hook holds and the loss is the
hand-off's; the test holds the hook at those reaches and does not hold a hand-off before a wall.

Not built, as the review asked, three options a binding may set or keep outside the passes, drafted
in the code's terms.

More than one ray a texel. `ProbeCascade` would gain `rays`, the rays each texel sends, with
`RaysAt(texels)` as the count a binding takes where it sets none: four from the middles of a texel's
quarters at 8 a side, one from its middle at 16. `traceTexel`'s index would run over a cascade's
texels times its rays, and `IProbeRays` would keep each ray at its texel's index times `rays` plus
the ray's own. `settleTexel` and `mergeTexel` would read each of a texel's rays, merge each from its
own end with the level above, and keep their mean as the texel's merged light, so `IMergedLight` and
the gather keep a texel's index. `mergedLight` would take the ray's way in place of the texel's
middle way and keep `farTexelsLight` over the texel's covered far texels.

A cap on a march's steps. `traceProbeRay` would take a `steps` count at most, 0 for none, carried in
`ProbeCascade` so each level sets its own. A ray that takes its last step without meeting a surface
would end as `RayPast` at the distance it reached, not as `RayLeft`. A finer level would then hand
it to the level above about the point where it stopped, as at its interval's end. The coarsest
level, with none above it, would take no light for it, since a ray out of steps mostly grazes a
surface inside the field, and the sky would light a closed room through it. The self-march would
stop the same way and take, where capped, the merged light of the level above about its stop, or
none from the coarsest. A `RayCapped` ending beside the three would let a binding count its capped
rays.

A mesh met by the device's own ray tracing. An interface `IProbeHits` would trace a ray from a place
along a way between a start and an end and return the `ProbeRay` it ends as, with `FieldHits` as the
default that marches the field (`traceProbeRay` over an `IProbeField`). It would also answer
`reachAlong`'s question, how far a ray goes before it meets any surface. `traceTexel`, `reachTexel`
and `mergedLight`'s march-on would take it in place of the field's march. The hit's light would stay
`hitLight`'s, read at the place and cell the tracer reports. Light through a surface would take the
tracer's own crossing of the mesh, an any-hit pass that multiplies the share to the depth between
entry and exit. The merge's and the gather's test of a probe inside a mesh would still read the
field's distance, or the tracer's own answer where a binding has no field.

The sunlit wall past a hand-off, in the commit carrying this line, by the rule the review gave: a
near ray past its end where the field's distance reads a surface near marches on itself
(`marchToOpenSpace` in `rc_probes`, one march with `traceProbeRay`) until it meets the surface or
stands in open space, and hands off there, uncapped, at every link through `mergedLight`. The field
reads no farther than its band, 4 cells, so open space is where the distance reads the lesser of a
far spacing and the band (`openAt`, with `FieldBand` in `rc_field_read`, which a test holds to
`SceneFieldPlan.Band`), and an edge between the band and a far spacing past a hand-off is still
smeared. The test, written first at a near reach of 4 with the wall 0.4 past the hand-off, read
0.0989 against 0.1575 (37% under) and reads 0.1574, held to 10%. Texels marching on, of 256, with
their mean and most steps: in two cascades the wall 34 (13%), 4.2 and 8, and each of the open,
lidded and sheet-lidded boxes 76 (30%), 2.9 and 5; in three cascades (ends 2, 2 and 32) the wall 12
(5%), 3.0 and 3, reading 0.1588, and each box 88 (34%), 7.6 and 20. The boxes read as before,
0.2540, 0 and 0.0018 in two cascades and 0.2548, 0 and 0.0021 in three, and the fallback march stays
at no texel. The rooms parted by a wall that lets light through move from 0.675 of their reading
with the wall away to 0.663, within the test's 15%. A middle cascade reaching 4, outside the ends'
rule, reads worse with the rule, the open box 12.4% under where it was 7.7 and the sheet lid letting
through 6.2% where it was 4.3, since the march carries the hand-off nearer the rim; the README's
ends convention carries these figures.

A crossed surface's own light and the step cap, in the commit carrying this line, each held by a
test run failing first. Where a ray crosses a surface that lets light through it takes the surface's
light through `hitLight` at the entry, times the share carried to it, times what the surface keeps,
and carries on its share times what the surface passes, through several such surfaces in turn, in
the march and the march-on alike. A thick surface passes its share to the power of the depth crossed
and keeps the rest, and a double-sided sheet thinner than a cell, whose depth the field cannot read,
passes its share once, as a unit of depth would. A probe under two sunlit sheets each passing half
read 0.7716 through the march and 0.7670 through the march-on against 0.4694, each sheet passing its
light whole and keeping none of its own, and reads 0.4640 and 0.4648, held to 3%. The rooms parted
by a wall that lets light through read as before, 0.663 of their reading with the wall away, since
that test lights no surface; a lit wall adds its light times what it keeps. `ProbeCascade.stepCap`,
0 for none, caps a cascade's own rays and, from the cascade above, its march to open space and its
march-on. A capped ray ends as `RayCapped` and is handed off where it stopped, a capped march-on
takes the far probes about its stop or nothing where none counts, and at the coarsest cascade a
capped ray takes no light past its stop. With its near rays capped the open box reads 0.2540 at 16
steps (30 of 256 capped) and at 8 (66), 0.2657 at 4 (5.8% over, 198) and 0.2309 at 2 (8.0% under,
238), held to 10%, and the lidded box with every cascade capped at 4 or 2 reads 0. Shape changes:
`ProbeCascade` gains `stepCap`, `ProbeRay.light` holds the crossings' light for every ending, to
which `lastCascadeLight` adds the sky, `traceProbeRay` has an overload taking a cap and
`marchToOpenSpace` takes one; the README states both conventions.

Places in the module's Slang that rest on exact floats or a buffer's bounds, noted for a binding's
finding on macOS's device and not changed. `cellCode` quantizes a distance to 1024ths of a cell and
keeps the least code, so two faces at one distance, common on meshes laid on the grid, are told
apart by their low bits, which a device rounding a distance one ulp otherwise, as fast math and
fused multiply-adds may, can turn. `floor(cells)` and `d < MarchHit * cell` in `rc_field_read`
choose a cell and a hit by exact comparisons. `cellsOf` holds a point half a cell from the far side,
so a blend there reads a cell one past the last at no weight, which a sampler clamps and an engine
reading a buffer by hand must clamp too, since no weight times what lies past a buffer's end may be
NaN.

The macOS fault, a binding's field that held every cascade's table by value, so each read copied
the table into a thread's own memory. No shape of `IFieldVolume` keeps an array out of a binding's
struct, since a Slang interface names a struct's functions and never its fields, and it asks for a
cascade's corner and cell by its index and never for a table. A shape taking the engine's buffers
in place of its struct would choose the engine's storage for it and still leave it free to copy. So
it is a note and a test: the README's binding section says the struct holds no table by value, and
each test kernel is compiled to SPIR-V assembly, in which no variable of Function or Private
storage holds an array, a check a field holding two or eight cascades' corners by value is seen to
fail. The one array the module kept, `pinched`'s three axes, is made from the index instead.

The lintel, held: no rule lands. Against the fine trace, the truth a test holds, the -x face of the
probe at (0.5, 1.5, 0.5) under the lintel reads 0.1686 against 0.1202, and the room's deep end, x
0.5 and 1.5 over z, 1.47 and 1.41 times the trace at 8, 8, 16 and 16 texels a side kept as 2 by 2
means. Doubling the texels every cascade moves that to 1.37 and 1.35 at 8 to 64 and 1.33 and 1.28 at
16 to 128, so the texels are not the mend: about 30 percent stays, the hand-off's parallax at the
opening's edges and the floor outside. The parallax read alone reads 0.2347. A march of the near ray
on to the far probes' claimed hits, a probe whose own hit disagrees weighing nothing, reads x 0.5 at
0.85 to 1.20 times the trace but x 1.5 at 1.55 to 1.84 and the sheet lid in three cascades 1.57 to
2.71 percent, so it stays out. Each probe's own texels marched on read about half the trace at the
deep end, one ray a texel missing the strip under the lintel. In the open box the probe at (2, 2, 2)
reads its up face within 4 percent at every count, and the floor as `gatherAt` reads it 26 to 30
percent over, its probes 2 units up seeing 1.248 times its share, which `gatherAt`'s comment and the
README state, correcting nothing. Steps in the lintel room rise 6 and 23 times at the doubled counts
and texels kept 8.5 and 34 times at 8 probes a side, 1.6 and 6.3 times where probes fall eightfold a
cascade. The light-through reach stops at the first surface on purpose, since a probe across one
lends light it has not dimmed, and the binding keeps the module's.

The lintel's limit, stated and held. The committed rule stands: the README states that a hand-off
within a far spacing of an opening's edge reads the room behind it about 1.4 times the fine trace at
its deep end, and a test holds the lintel room's probe at 1.402 of the trace and its deep end at
1.472 and 1.405, bounded at 1.45, 1.52 and 1.46 and below at 0.75, in 16 seconds. The bounded
attempt, a near ray whose far probes disagree on their hits along its way marching on itself to the
far cascade's own end and handing off there, brings the deep end to 0.99 and 0.96 of the trace and
62 of 64 probes within a quarter, at 1.15 times the steps, with the open box unchanged and the
two-cascade lids dark. It stops at its condition: the three-cascade sheet lid lets 1.57 percent
through at every share tried, against its bound of 1, as the agree march did, read as, though not
traced to the texel, through the field's slit where the sheet meets the walls' tops, and a
four-cascade open box, not a test, reads 19 percent over. The patch is kept outside the tree.

The slit is the field's and mended; the split rule is held. Under both rules the same 138 far rays
passed the three-cascade sheet lid, each crossing its height within a cell inside the walls' faces,
cells that keep the wall's code and so lack the sheet's plane. `seamSheet` in `rc_field_read`, the
pinch's kind of rule, gives such a cell the plane a neighbor keeps where it passes within half a
cell of its middle, and a field's build runs it after the pinch. A test that failed first with 4 of
50 rays through holds it, and every lid in every chain reads 0.00 percent under both rules. Four
cascades at the ends rule, each over its truth, committed against the split rule at 0.25: the open
box's row a quarter over the floor 1.042 against 0.953, its worst probe 6.5 against 7.1 percent off,
and its floor's middle by `gatherAt` 1.040 against 0.920; the lintel's deep end 1.472 and 1.405
against 0.990 and 0.957, with 38 of 64 probes within a quarter against 62; the binding's open box
alike under both; the steps 1.15 times. By the condition the open box reads worse under the split
rule, 8.0 percent under against 4.0 over, so it does not land, and its patch applies over the mend.

The split rule, landed at a share of 0.25 (`HitSpread`). Where the far probes about a hand-off that
count disagree on where a surface lies along the ray, the ray takes none of them and marches across
the far interval to the far cascade's own end. The lintel room's test holds its probe at 0.878 of
the fine trace, its deep end at 0.990 and 0.957, and 62 of 64 probes within a quarter, bounded at
0.75 to 1.25 and at least 60, in 31 seconds. A four-cascade open box at the ends rule is a test of
its own, its floor row 0.929 to 1.010 of its share and its middle by `gatherAt` 0.920, bounded
within a tenth. Every lid reads nothing and is held at nothing. A middle cascade reaching 4 reads
the open floor 0.3 percent under, as reaching 2 does, where it read 12.4 under. The binding's box
reads the same within 0.0002, and the steps rise about 1.15 times. Bindings follow two shapes the
README states: `mergedLight` takes `marchEnd` after the far cascade, which keeps its own end, and
`reachAlong` gives `ReachOpen` where it meets nothing, which a reach image keeps at or past it.

One place of shading, at BevyCSharp's finding. A driver inlines a binding's `hitLight` at every
call, and that binding's shading, with its sun's and lamps' shadows, compiled a trace in 46 seconds
and a merge in 2.6 minutes at 3.75 GB, with a call at each of the three ways the march meets a
surface and a march at each of the merge's three legs. The march takes a surface's light at one
place a step, and `mergedLight` marches its legs in one loop at one call of `marchToOpenSpace`,
clearance 0 for the legs that run to their end, so a trace holds the shading once where it held
three copies and a merge once where it held nine, which a test counts by a `hitLight`'s atomics in
the SPIR-V. No signature changed, and the README tells a binding it shades in `hitLight` itself. Its
loop of marches, endless with each return inside it, fell off the merge's end in WGSL, which naga
refuses, and BevyCSharp's merge was never made. The loop is bounded at three marches with a return
after it, and a test reads every kernel's WGSL for a function returning a value that may end with no
return.

The lintel's sampling, from 3DEngine's six cases built on the CPU from its numbers. Only its lintel
is the module's: the face toward the opening read 0.047 under the passes and 0.0473 here against
0.077 traced, where the module's test lit what rays meet by a bounce and read the far level at 8.
The ways a few degrees under the horizon toward the opening meet the ground outside along their
texels' middle ways where a third of their solid angle sees the sky past the ground's end, and read
nothing. Every cascade traced at twice the texels a side it is read at, each texel read the mean of
the 2 by 2 it holds, reads the face 0.94 of the trace at four times the rays and steps, which a test
holds within a fifth with no library code changed. Traced at 16 and read at 8, as many rays as
before, it reads 0.70; sub-ways past a ray's end alone read 0.84 at 1.7 times the steps and put the
varying sky and the sunlit wall 4 percent under; a sub-way varied by probe reads 0.65. The open
box's floor reads 1.205 here from probes 2 m up, the field's finest cascade holding y 4 to 20, its
merge reading their face 1.013 of a trace there. The slab and the canopy read within 2 percent of
their traces, the canopy at 2.5 m 10 percent under, and the bounce rooms and the board need the
renderer. The share is a value a binding passes, `TracedMeans.perRead`, 1 or 2 traced texels along a
side of each read texel, which wraps a binding's reads of its traced texels and weighs each by its
solid angle, with `readCascade` giving the cascade as read; the test kernel reads every level
through it. Each traced texel weighs by its solid angle in the texel read, as `farTexelsLight`
weighs, so the face reads 0.83 of the trace at 2, 0.57 at 1, and 0.64 traced at 16 and read at 8,
where the even means of a243f3d read 0.94, 0.61 and 0.70, counting the texels by the octahedron's
corner toward the opening, which see the sky and span the least solid angle, as whole ones.

13. **The pixel's gather written once, its level by the rim rule.** The gather's shared step of the
    kernel move, proposed by the session on 2026-11-25 and taken under Decision 9. Each engine picks
    a pixel's cascade by its own rule today, one the finest around the point with no rim, the other
    the finest holding the point with a fade at its edge, and the rim rule lives in the test
    kernel's gatherLevel alone. The module gains in rc_probe_passes.slang gatheredLevel, the finest
    level whose probes stand about the point or within half its spacing past its edge probes, else
    the coarsest, and gatherSurface, that level with gatherAt's weights, trilinear, a probe inside a
    surface or not seeing the point weighing nothing, renormalized; no fade until a ladder reading
    shows one does better. A host hands one struct, IGatherLevels, the level count and each level's
    ProbeCascade, a level's merged light and reach at a probe and texel, and a probe's place by its
    own move rule, beside its IProbeField for the sight check and an ISurfacePoints over its pixels,
    the place from depth, the normal, keep. Tests, points inside the finest probes, a quarter unit
    past their edge, 0.6 of a spacing past, and outside every level reading levels 0, 0, 1 and the
    coarsest; the test kernel's gatherLevel replaced by the library's with the rim as an argument
    for the court's knockouts, the court's high wall under 1.3 and the ladder holding on the
    library's code. The engines then drop their level choice and probe weighting and each implements
    IGatherLevels over its images, 3DEngine as its item 313 and the bridge as its 157's first half,
    so 157's two tables differ by the merge alone, and item 10's cone takes the same level choice.
    One or two commits, before item 10. Done at `e042398` on 2026-11-25, pushed at 16:36, its run
    green. rc_probe_passes.slang gains GatherRim at 0.5, gatheredLevel and gatherSurface with its
    sky form; IGatherLevels and IGatherSkyLevels stand in rc_cascade_merge.slang beside IFarProbes,
    IConeLevels folded into IGatherLevels since no engine bound it; GatherLevelTests reads the four
    points at 0, 0, 1 and 2 with the rim and 0, 1, 1 and 2 without, the test kernel's gather goes
    through gatherSurface, the court's high wall 1.240 as before, 210 tests. A host hands its
    IProbeField, an ISurfacePoints over its pixels and one IGatherLevels over its cascade images.
    The bridge's field_stamp.slang still calls stampCell with four arguments and takes the cell
    width as the third on its pointer move past 774d11d; the engine's call passes it already.

14. **A ray that meets a surface before its interval reads dark in the merge.** From the engine's
    item 304 at its mark on 2026-11-25, under Decision 9. The engine's own trace marches each ray
    from its probe and marks one that meets a surface before its interval begins, and its merge
    reads that ray as dark; the module traces from the interval's start and its merge takes the far
    texel's light from past that surface, so light crosses a wall that stands between a probe and
    its interval. At the engine's door with the screen probes off the module's merge reads six parts
    off the engine's at zero bounce +0.325, -0.092, -0.626, +0.129, -0.185 and +0.108 and settled
    +0.462, +0.009, -0.677, +0.231, -0.001 and +0.113, the open box at Low 1.289 over its 1.25, the
    solid slab's floor 0.160 against 0.063 and the half canopy 0.323 against 0.403, the lintel and
    the still glow passing, the time under the engine's own. Untested by trial there. The module's
    trace marks a ray blocked between its probe and its interval, and the merge reads it dark, or
    says in one reading why that is not the cause and what is; a test holds a slab's floor under a
    solid slab and an open box against their references, and the ladder holds. Then the engine reads
    304 again at its mark. One commit, before item 10. Done at `d89a34b`, pushed at 18:58 on
    2026-11-25, its run green. The mark is not the cause, the slab's floor reading 1.562 and 1.693
    alike with it and without, so it is not kept. Two causes instead. The finest two levels at 8
    texels a side double a grazing sky in the gather and the merge's far texel, the floor reading
    1.693 at 8 and 0.915 at 16; and past a far cascade ending at the field's edge the open box's
    corner probes lent the wall, so a probe weighs nothing where its own hit lies short of the ray's
    end, the floor reading 1.160. The ladder by reach and sight at the engine's configuration 0.088
    mean and 0.367 worst from 0.117 and 0.388, the module's 0.092 and 0.454 from 0.095 and 0.487,
    the door moved 0.014 to 0.026; ProbeMergeTests.Floors.cs holds the slab with the finest two at
    16 and the open box. No binding changed; a host keeps its finest two cascades at 16 texels a
    side, four times their merged-light memory, or keeps the excess.

15. **The surface gather's light over the trace, and the points no probe sees.** From the engine's
    item 313 on 2026-11-25, under Decision 9, after item 14. The gather at the engine's depth
    points, in its sky form over the engine's merged images, reads 1.5 to 1.85 of the traced light
    once settled on the door's wall where the module's own note gives 1.26 on an open box floor, and
    at zero bounce 1.02 of the wall against the engine's faces at 0.63; and it gives 0 where no
    probe passes the sight check at 0.125 spacing, a dither of black points on the wall. One reading
    of where the surplus comes from, the merge, the weights or the levels, with a ladder scene that
    holds a wall under a lamp settled, and a rule for a point no probe sees, the nearest seeing
    probe or the coarser level, with its test. One commit. Done at `39d0a8a`, pushed at 20:18 on
    2026-11-25, its run green. The surplus is where the probes stand. On the new ladder scene, a
    wall under a lamp at the engine's configuration, the top middle reads 1.507 after one gather and
    1.432 settled, its probe keeping 1.140 of its own place's trace while that place takes 1.834 of
    what the wall point takes; the loop gains 0.486 a round against the trace's 0.489, so the
    engine's growth to settled is its own face bounce, outside the module. On a flat wall every
    point has a seeing probe; the few black corner points are probes whose texel ways miss the small
    lamp. New rule, a point no probe of its level sees takes the level above's light and no further,
    a 0.6 slot from 0 to 1.51 to 1.55 of its trace, while walking to the coarsest lent the sky into
    a 0.2 slot. Ten ladder scenes, by reach and sight the engine's configuration 0.095 mean and the
    module's 0.103, the worst parts unchanged; the default stays the merge at the ray's end. Nothing
    an engine binds changed; 213 tests.

16. **The engine's door at the mark, read again on items 14 and 15.** From the engine's 304 on
    2026-11-26 with the pin at 39d0a8a and the finest levels at 16 texels a side. The six parts over
    the reference at zero 0.85, 0.41, 3.34, 0.33, 0.14 and 5.35 against the engine's own 0.73, 0.85,
    3.88, 0.29, 0.38 and 4.38, settled 1.62, 1.18, 4.19, 0.97, 1.18 and 6.71 against 1.30, 1.47,
    4.82, 0.78, 1.17 and 5.83; the open box at Low 1.373 against 1.25, the solid slab's floor 0.126
    against 0.063, the half canopy 0.310 against 0.403, and the lintel's sky 0.1079 against at most
    0.0882 where it passed at 774d11d, the same four misses at 8 texels a side, so the lintel's miss
    came with items 14 or 15 and the texel count moved the slab by 0.007 and not from 1.69 to 0.92
    as the module's slab did. The module's slab and the engine's differ, so this item builds the
    engine's slab, lintel, open box and half canopy as ladder scenes at the engine's configuration
    from the figures it hands, finds what its mend of 14 did not reach and what 15 broke at the
    lintel, and mends it with a test; then the engine reads 304 again. One or two commits. Done at
    `676c5f5`, pushed at 05:42 on 2026-11-26, its run green; the door's mark is read once on the
    split design (Decision 10). The lintel broke on 14's rule leaving out far probes whose hit lay
    short of the ray's end; the rule is gone, and a ray whose far cascade ends at the field's edge
    marches on to twice where it ended and then takes far probes as an agreeing cascade does, a
    probe under a slab holding it (+x 1.199 of its trace where 1.73). On the engine's scenes the
    lintel's sky reads 0.0705 against 0.0802 and passes, the solid slab 0.141 where 0.126, the box
    at Low 1.373 against 1.25 as before 14 (1.289 at 774d11d), so that surplus is not the merge's
    and stays open; the slab's remainder is its ways leaving the 19.2 field before they meet the
    floor, 0.555 against 0.280 traced, the engine's field size and not the merge. The ladder's means
    as before, 214 tests. The engine reads 304 again on it.

17. **A fixed point's light holds still as the field moves.** The direction of 2026-11-26 through
    the engine's item 317. On the ladder, a corridor of fixed walls under a sun and a lamp with the
    field and its cascades stepped along it in small steps over several cells of every level, so
    every edge crosses a set of fixed surface points; each point's gathered light over the walk
    against its own mean, the number the worst swing and the 99th part, under 2% as the first bound,
    at both configurations and with each merge. The scene joins the ladder and a test holds it.
    Where it swings, the edge and level are named and the placement or the merge mended with the
    test; the engine's figures from 317 come here as they are read. One commit, before 16's second
    commit if 16 is not done. Read against the sparse method on 2026-11-26. Ours is a dense grid a
    level about the eye with probes in air and inside walls, each tracing from where it stands, the
    host moving one off a surface and the gather skipping one inside a mesh; theirs keeps only the
    probes nearest the visible surface points, persisted, with rays started at the surface points
    and split by hit distance into each cascade's probe and direction. Ours lacks the sparse set,
    the ray splitting, the time-jittered low-discrepancy directions, the irradiance texture a probe
    and the temporal blend. Under movement each probe keeps its world place, the spacing being eight
    cells and the snap eight, and a level's region moves a whole spacing at a time. The likeliest
    mover of a fixed point is the gather's hard switch of level at a level's edge, the point's light
    jumping from level n's gather to n+1's in one frame where the levels read differently near
    walls; second, the fade bands of the field cascade a ray reads following the eye, so a hit can
    move by a coarse cell. First to take, a blend across each level's edge band in the gather as the
    sparse method's overlap does, once the corridor shows that swing. The corridor read on
    2026-11-26 at the engine's configuration by reach and sight, the eye walked in quarter steps,
    the swing 3.59 worst and 2.47 at the 99th part against the 2% bound. Three causes, the levels
    disagreeing at the switch, level 2 reading a point at a median 4.07 times level 1 since none of
    its probes stands in the corridor's air of 2.5 width and level 1 reading 0.60 to 2.97 times
    level 0 near the lamp; the rim, a point half a spacing past a level's edge probes off by up to
    0.64; and a tenth of the samples dark, no probe of the point's level or the one above seeing it.
    Inside a level the light is steady, 5% at the 99th part, and with the levels held still and the
    field alone moving the swing is 0.33 and 0.136, the cascade a ray reads changing. A blend across
    each level's edge band was tried, about the snapped probes and about the eye, and neither brings
    the swing near the bound, since a blend cannot close what two levels read apart. Ruled, no band
    mend; the commit holds the scene on the ladder, the reading and a test of the step jump at
    today's figure as the baseline that item 18 has to bring under 2%, which then becomes the test's
    bound; the eye as a member of a host's hand comes with 18's interface. Done at `edc5520`, pushed
    at 06:39 on 2026-11-26, its run green. The corridor is on the ladder in
    ProbeMergeTests.Corridor.cs and LADDER.md reads it at both configurations by every merge; at the
    engine's by reach and sight the swing 3.594 worst and 2.466 at the 99th part, the step 3.558 and
    1.299, with the levels held still 0.631 and 0.246 and with the field held still 3.633 and 2.437,
    so the levels carry it; the test holds the step under 3.6 and 1.31 as the baseline until item 18
    moves it to 2%. The ladder runs 10 m 45 s, past the shared lock's ten-minute bound.

The gather toward the surface, `gatherTraced`, for a surface whose probes stand off it. A point
sends a ray along each way of the finest cascade's octahedron that faces its normal, from a cell off
the surface, and hands each on through `mergedLight` as a ray that ended where it began, weighed by
cosine and solid angle. Built from 3DEngine's numbers, each failing first through `gatherAt`: the
open box's floor under probes 2 m up reads 1.031 of its fine trace, where `gatherAt` reads 1.234,
the binding's passes 1.346 and its main 1.142; the floor under the solid slab reads 0.933 of the
trace of the slab as the field holds it, where `gatherAt` reads 1.129, and 1.39 of the whole slab's,
since the binding's coarser field cascade ends about 6 m short of the point and the rays leaving
there take the sky. No weighing of the probes about the point serves, since neither a probe's reach
nor its hits say what lies between the surface and the probes' height. It costs 128 rays and about
1,000 to 1,600 steps a point at 16 texels a side, which the README states, and a binding calls it
where it chooses; `gatherAt` stands unchanged.

The sky's share beside the light, at 3DEngine's second merge pass, which merged every texel again
under a sky of 1 with every surface unlit and cost it 1.4 of 6.46 ms. The merge keeps the share in
the same pass: a ray that takes the sky brings the share it carries there (`lastCascadeSkyShare`),
and a hand-off the far probes' shares weighed as their light is. Each pass and `mergedLight` has a
form over `IFarSkyProbes` with `IMergedSkyLight` or `ISurfaceSkyPoints`, and the form over
`IFarProbes` reads through `NoSkyShares`, so no binding of `IFarProbes` changes and the march holds
a binding's shading once in either. The open box's floor takes a share of 0.24696 against the
0.25111 of the sky its black walls leave open, 1.7 percent under, the same under the uniform and the
varying sky, and every light figure the suite prints is that of e660713. A binding that dims its sky
for a cascade dims the share it keeps by its own rule, which the README states. The share takes its
weight from the sky beside its light (`ISkyShare.shareAlong`), since 3DEngine lends a finer
cascade's leaving rays no sky until its field holds every mesh in reach and cannot dim a share the
merge has mixed; a sky read through `ISky` alone lends its share whole (`WholeShare`). The open
box's floor with its finer cascade's sky gated off takes a share of 0.06367, its light over the
sky's to 1e-5.

The merge's cost, counted. Two bindings timed one cascade's merge alone, a pass, under an exclusive
hold, 131,072 rays a dispatch: 3DEngine's finest 2.24 ms on Sponza and BevyCSharp's 6.72, against
their own merges' 0.36 and 0.37 ms a frame for every cascade, with 62 to 89 percent of a finer
cascade's rays past their end and a cascade that merges nothing from above 0.034 to 0.039. On the
CPU a ray past its end takes 1.4 to 7.6 steps, about what a traced ray takes, but reads the field 14
to 40 times and the reach maps 14 to 23, most of them the eight far probes' tests of standing inside
a mesh and of seeing the hand-off, twice where the split was tested, and shades a surface on 0.07 to
0.57 of its legs. A probe inside a mesh keeps `ReachInside` along every way of its reach map, which
`farProbeSees` passes over, and the far probes about a hand-off are tested once for the split and
the light alike: the field's reads a ray fall by a half to two thirds and the reach's by a quarter,
and every figure the tests print stands as it was. What the marches buy, measured by leaving them
out or ending them sooner: the lintel's row 11 to 41 percent over where it reads within 9, the four
cascades' worst 16 percent off where it reads 7, the traced floors 29 over and 43 under where they
read 3.6 and 7.8, so they stay. Neither binding's own merge shades a surface its march meets, and
the shading the CPU cannot time is the next to place.

The merge's hits. At 16af1c8 3DEngine timed three cascades' merge at 4.89 ms on Sponza and 9.23 to
13.05 on the Bistro as bound, 4.00 and 4.08 with a hit lit by its probes alone, 0.54 and 0.57 with a
hit taking nothing, and 0.55 and 0.56 with a hit one read of a texture at its cell, against its own
whole merge's 0.36 and 0.25, so the shading at a hit is 89 to 94 percent of the merge's cost. On the
CPU a merge taking nothing at a hit reads the lintel's deep end 0.507 of its trace against 0.819, 4
of 64 probes within a quarter against 62, a sunlit wall a little past the near rays' reach 44
percent under, the sunlit sheets 34 percent under and a wall past every far probe 0, and one taking
in its place the far probes' light about the hit reads the open box's floor 1.49 times its share and
the lintel's deep end 1.73 times its trace, so the hits stay shaded. An engine that holds the light
of its field's cells (`IHeldLight`) passes the merge `HeldHitLight`, which reads at a hit the light
held for its cell, and runs `holdCellLight` each frame over a share of the cells, each within a cell
and a tenth of a surface shaded once by its own `hitLight`. On the CPU the sunlit wall so reads 0.6
percent under the shaded merge and the sheets 0.04 percent over. Shading every cell within a cell of
a surface once took 3DEngine 1.91 ms on Sponza and 3.07 on the Bistro, so held over eight frames a
frame pays 0.24 and 0.38 beside the merge's 0.55. 3DEngine's own cheaper hit, its probes' kept light
read half a spacing off the surface and its sun map at the hit with no lamps, took the whole merge
of five cascades to 1.03 and 1.07 ms and passes its sunlit passage test. The comment of the lintel's
deep-end test held figures from before the merge's reads changed, and gives the ones it prints. The
choice: each binding passes the merge its own cheap hit, the sun's reach read at the surface and the
probes' light half a spacing off it toward the ray, which the README states, and the held light
stays offered beside it, held whole each frame, its share above 1 only on the owner's word.

The crowns' share a step. A crossing passes over its depth the share of the cell each step within
began in, to the power of that step's depth, where it passed the share of the cell it was met at for
its whole depth, so a crown's thin edge no longer stands for its core. On the CPU a crown 6 units
deep, held as BevyCSharp holds leaf cards that run together, one region half a cell inside
throughout, passing 0.9 a unit within a unit of its faces and 0.3 within, reads the sky a probe 2
units before it takes through it 0.992 of the trace of each way's own depth, where it read 1.232
times it. The march takes no step and no read more, since each step within read its cell's share for
a solid met within, and the floor behind the wall that lets light through reads 0.04 percent under
what it did, the sheets as they did. The foliage trace's cost beyond BevyCSharp's own is not the
module's march: its hitLight marches the crossing again, up to 64 half-cell steps, to light where
the leaves stop the ray, which a crossing lit by the march where it has stopped half the light would
spare, at a second place of the shading or a stop deferred a step.

The closed rooms' 5 percent over, held. On the CPU the passes add no light to a closed room on one
bounce: the open box with a lid, every face sending a light of 1, reads 1.0000 at all 24 points
through four cascades, which a test holds. Under broad light, a wall, half the floor or the lid
alone sending it, the points read 0.99 to 1.12 of the light traced against the room's faces on the
mean, and each texel marched on to the coarsest level's end alone 0.97 to 1.15, single points up to
a quarter either way in both, the sampling of 16 by 16 texels and no rule of the merge's.
BevyCSharp's rooms take a bounce at each hit, its probes' faces read there, and its smooth room's
second bounce read 11.1 percent of its light under its own code and 12.1 under the passes, so the 5
percent is asked of it split, its views 2, 4 and 7 read under the cheap hit it binds with the
probes' light at the hit and without it.

A crossing's light where it has stopped half the light it carried in. The march takes no light where
it meets a surface that lets light through, and takes it at the step within where the share the
crossing has passed falls to half or under, or, where the crossing passes more, at its last point
within, the light taken a step later where the crossing leaves the surface, meets a solid or ends
with the march, the solid's step taken again without its count. The shading stays at its one call.
On the CPU the crown lit from before it by a sun whose share its depth passes, each cell sending
that share, reads a probe 2 units before it 0.10949 against 0.11402 traced, 0.960 of it, where taken
where the ray met it it read 0.18061, 1.584 of it, which a test holds run failing first. Its trace
takes 15.8 steps a ray on the mean and 64 at most before and after, and the crown under the sky, the
sunlit wall through the march and the march-on and reaching 4, the sunlit sheets and the floor
behind the wall that lets light through read every figure and step count as they did. `traceTexel`
takes an `IMarchSteps` where a binding counts its trace's steps, which the tests' kernel does.

The held light's two misfits, from 3DEngine's binding. A cell keeping a sheet thinner than a cell
holds a light a side, 0 along its plane's normal and 1 against it (`heldSide`), shaded where a march
toward the plane from either side stops, and the read takes the side the ray meets: on the CPU a
sheet at 2.1, four tenths up its cell, lit from above alone reads a probe under it 0.00000 against
0.00000 shaded and over it 0.27710 against 0.27710, where one light a cell read under it 0.27710,
which a test holds run failing first. A cell holds its own light apart from the probes' light it
reflects (`IHeldShading`, `WholeShading` for an engine whose hit weighs no probe's share), and the
read weighs the probes' light by the share the engine gives at the hit (`IHeldLight.probeShare`):
the sunlit wall whose hits take a quarter of a probes' light of 0.2 reads 0.17364 held against
0.17461 shaded, where held whole it read 0.22521, 29 percent over, held by a second test run failing
first. The holding pass shades a sheet's cells twice, 6,272 of a lone sheet's 9,860 held cells and
2,144 of the box's 27,856 under a sheet for a lid, 64 and 8 percent more shading; the engines'
counts of their sheet cells give their cost. The sunlit wall and sheets held read as they did, the
sheets 0.46498 where they read 0.46501. The GPU lock is taken through Tools' gate, `modules/Tools`
pinned at c389096.

The lintel's mixed texels, kept as the setting. Each cascade takes its own `TracedMeans.perRead`, so
a binding traces its finest two at 2 and reads the face under the lintel 0.825 of its trace at 2.21
times the trace's steps, as all four at 2 read it at 4.0 times, where the coarser two at 2 read
0.637 at 2.8 times and one ray a texel 0.637, which the README's table gives as the stated limit and
the test holds at 0.8 or more. The fault is the merge's march past a finer cascade's ray's end,
which meets the ground outside along the texel's middle way short of the ground's end. A texel whose
ray ends unlike one of its four or eight neighbors', traced from its quarters, read 0.689 and 0.710
at 1.8 and 2.05 times the steps, 11 to 14 percent of the texels mixed, since the finer cascades'
rays end in open space and agree with their neighbors; its quarters handed on with no march read
1.068 and broke six of the merge's tests, the four cascades' open box reading up to 1.25 times its
share and the wall past the near rays' reach 22 percent under. A test on how the merged ray ended
would take three dispatches a cascade, a second mechanism for one case, and is not built.

The held light's sides for thin solids, from 3DEngine's binding at 389f951, whose thin room's wall
is a box 0.1 thick in cells of 0.15, kept as a solid and not a sheet, so its cell held the face its
middle lies on, the lamp's. A solid's cell holds side 1 too, the far face found within 1.2 cells of
its middle against the field's normal, shaded where a march from beyond it stops, or side 0's light
again where none lies there, and the read takes the side by the field's normal at the cell's middle.
On the CPU a wall 0.1 thick lit from beyond it reads a probe before its dark face 0.00000 against
0.00000 shaded and before its lit face 0.27366 against 0.27616, where the one side read the dark
face 0.27366, run failing first; 2,944 cells about it hold their sides apart, and the holding pass
reads the field up to 8 times more for each other held cell. Every other held figure stands.

The held side's cost. On 41f0e09 3DEngine's thin room and its view pass, its holding pass taking
2.25 ms on Sponza and 3.34 on the Bistro, where it took 2.06 and 2.80, a second side shaded apart
for 10,183 of 153,245 held cells and 15,063 of 110,018, and its merge 0.77 and 0.98 ms, where it
took 0.56 and 0.63, from the field's normal `heldSide` read at the cell's middle in six reads.
`heldSide` reads a solid's side as the field's distance growing along the ray's way across half a
cell either side of the middle, two reads, and every held figure stands.

The closed rooms' 5 percent over, placed. BevyCSharp's views 2, 4 and 7 at 6e16181 read 1.050, 1.047
and 1.086 of their traces bound, and with no probe light at the hit, its sun, lamps, glow and
emission alone, 0.570, 0.382 and 0.550, so the bounce at a hit carries 43 to 62 percent of each
view's light, and the 5 percent is 8 to 13 percent of that bounce. On one bounce the passes add
none: the closed box lit by its -x wall alone reads, summed over 49 points for each of five facings,
0.978 to 1.010 of the light traced against its faces, and its texels each marched on alone 0.975 to
1.011, the merge adding 1.4 percent at most to its texels' own rays, which a test holds within 3
percent beside the box every face of which sends the same light. The 5 percent so lies in the bounce
a binding's hit reads of its probes at the surface, which is the binding's, with the read half a
spacing off the surface toward the ray the README asks of a cheap hit. The held cells read the views
3.568, 1.069 and 1.027 at 6e16181, view 2's thin walls mended since.

Verdict 1, the Windows job's time. The tests build each kernel for the CPU through slangc, which
hands it to the system's C++ compiler at its own level, given by Slang's rules as -Os to gcc and
clang and as no level at all to Visual Studio's cl, so cl built every kernel unoptimized and each
test that runs one ran several times slower on Windows than on Linux and macOS, the day's new tests
running the passes the longest. The probe merge tests run one after another in one class, and on a
desktop at -Os the heaviest were the lintel's two (25.7 and 23.6 seconds), the four scenes' floors a
thread a texel (21.8), the closed room lit by one wall (16.4) and the floor under probes two units
over it (16.1), of a suite of 3 minutes 22 seconds. `CpuKernel` gives slangc -O2, which gives cl /O2
and the others -O2: the closed room, the lintel lit by the sky alone and the four scenes' floors
take 170 seconds at -O0, 67 at -Os and 30 at -O2, the suite 2 minutes 2 seconds, and every line the
tests print reads the same at -O2 as at -Os. The Windows job's minutes are those of this commit's
run.

The vestibule behind a doorway onto a sunlit court, which 3DEngine's door view of Sponza's vault
read at 1.70 of its traced light under the passes and 1.19 under its own code. No light is counted
twice along a ray that marches on: the merge takes a leg's light once, the march's or its far
probes', never both. 3DEngine's view, screen probes off, reads 1.562 of its 256-path trace under the
passes and 1.483 with c0's merge reading every probe above as inside, so that each c0 texel marches
on alone to the coarsest cascade's end: the chain adds 5 percent, and the rest is the light each
texel's own rays bring from its probe's place and their hits. On the CPU a room behind a wall 1.2
units thick, its doorway onto a lit court and its far probes in the wall, reads 1.268 of a fine
trace through four cascades and 1.216 with its texels each marched on alone, and a wider vault 0.723
and 0.761. The coarsest cascade's 0.60 of the trace alone, 0.01 under 3DEngine's own code, is the
court's light a ray reaches past the third cascade's end, which the march alone brings as well,
since every cascade's rays begin at its probe. A held shading that reads the probes' reach maps for
visibility in place of a march to each probe reads the view at 2.07 and 2.16 in 3DEngine's trial, so
in this vault what a hit sees of the probes it reads moves the figure far more than the chain does.
Nothing in the library changed.

The listing of the held cells, which BevyCSharp asked the module to hold for every binding. A thread
a cell over the whole field leaves most threads with no cell to shade, 25,600 cells held of about a
million on Sponza in BevyCSharp, and a workgroup whose one thread shades takes as long as a full
one, so its hold took 2.72 ms on Sponza and 2.95 on the Bistro over every cell, and 1.60 and 1.71
with the cells listed first, the listing 0.048 and 0.064 more. `listHeldCell` lists into the
binding's `IHeldList`, which the binding counts atomically and sets to 0 before the listing, the
cells `holdCellLight` holds, chosen by the same test (`heldCellAt`) apart from the shading
(`holdHeldCell`), and `holdListedCell` holds them a thread a listed cell. On the CPU the floor under
the solid slab with a wall 0.1 thick, in two field cascades, lists 50,177 cells of 524,288, those
the hold over every cell holds, and every held float is the same.

The shading inside the march's step loop. `marchProbeRay` called `hitLight` within the loop its
steps take, so a driver inlined a binding's whole shading in that loop and every step paid for it:
one binding that left the loop at a stop and shaded once after it took its trace from 4.02 to 1.75
ms in one scene and from 5.50 to 2.02 in another, every reading the same, with a second call site.
The march's steps take an inner loop that reads the field alone and leaves where a step takes the
light of a surface, a crossing that has stopped half its light, one leaving a surface or meeting a
solid within it before its light is taken, a stop, or the march's end with a crossing's light still
to take, and an outer loop shades there at the one call and goes on. The merge marches through the
same function, so its marches take the same form. The test of one call site holds, beside the one
copy of the shading in the SPIR-V of a trace and of both merges, that the innermost loop about it
holds a loop of its own, the steps', where before the change it held none. All 175 tests pass and
every line the tests print reads the same as before. The README names as a binding's choice the rays
of each cascade above the finest beginning at the finer one's end, which took that binding's trace
from 4.02 to 3.27 ms and from 5.50 to 5.14 alone, beside the floor of a box open to the sky that it
read 3.6 times its light.

The held list's count each frame. BevyCSharp recorded its count every frame under a moving field,
1,267 frames on Sponza and 1,280 on the Bistro with the finest field cascade moving 20 times, and
found no gap and no frame's count more than 5 percent from the one before, the largest at a move of
the field, its 0 a buffer write queued before the frame's commands and its hold dispatched over
every cell. `IHeldList`'s comment asks for the 0 among the frame's commands before the listing, a
clear of the buffer or a dispatch of one thread, since a 0 from the host that reaches the GPU after
the listing's submit leaves the count going on from the frame before, and for the hold dispatched
after the listing among the same commands, over every cell or by a dispatch the GPU sizes from the
count's buffer, and not over a count read back to the host, which waits on the listing or is a frame
old.

The grazing way handed off over a floor. 3DEngine's door view read its floor's probe 142 percent
over at the first bounce in ways handed to the cascade above at 2 m, one of which takes 0.0526 where
its trace meets a step's face 4.79 out with 0.0172, the far probes about the ray's end standing a
spacing over the floor and seeing over the step. On the CPU a probe 0.5 over a floor, its far probes
2 apart with their lower row inside the floor and a step's face 2.76 past the ray's end, reads that
way 1.000 against 0.333 traced, 3.0 times it, and every way marched alone reads its trace. Weighing
a far probe by how near its ray runs to the near ray's changes nothing there, every one that counts
standing as far off it. Marching on where the far probes that count stand on the whole more than a
quarter or a half of the far spacing off the ray's line took 62 and 39 percent more steps past the
rays' ends, and only on the side the ray heads away from, at a half, 14 percent, reading the way
0.333 and moving no other test's figures or steps. On the branch `offside-march`, in 3DEngine, that
rule left the floor's probe as it was, the door view 1.556 to 1.536 and 1.603 to 1.592 with the
screen probes, the merge 0.76 and 0.78 ms against 0.76, and every other reading the same, so the
merge keeps the over-read and the case is a test of it, the way reading over 2.5 times its trace.

Handoff, for a fresh session, in place of those above. Where the work stands: the library holds the
field's plan, the triangles' layout and the glow lights in C#, and in Slang the probes' octahedron,
the cell code, resolve and pinch, the hit's color read, the splat, the stamp, the field's distance
and march (`rc_field_read`), the probes' trace through surfaces that let light through, their reach
and their gather (`rc_probes`), the merge with the seeing rule and the self-march
(`rc_cascade_merge`) and the per-thread passes an engine's kernels call (`rc_probe_passes`), each
run on the CPU by its tests through `CpuKernel`. The rules agreed for the merge: a far probe inside
a mesh or not seeing the near ray's end weighs nothing, every cascade's rays begin at its probe, a
texel no probe above sees marches on to the coarsest cascade's end, and a cascade's end lies within
twice its spacing, which the README states. A near ray past its end beside a surface marches on to
it or to open space, the lesser of a far spacing and the field's band, before it hands off. A
surface a ray crosses adds its own light weighed by what it keeps, and a binding may cap a cascade's
steps (`ProbeCascade.stepCap`). A binding gives the passes its probes' places (`IProbePlaces`), a
hit's light (`IProbeField.hitLight`) and the sky along a way (`ISky`), and a near texel takes the
far texels it covers (`farTexelsLight`). A binding's struct holds no array, which the tests check on
the module's own kernels by their SPIR-V. A hand-off near an opening's edge read the room behind it
about 1.4 times the fine trace, and the split rule, a ray marching on where its far probes disagree
(`HitSpread`), takes it within a quarter at about 1.15 times the steps, reading a four-cascade open
box's floor 8 percent under, its figures in its Reply. A field's build runs `seamSheet` after the
pinch. The march calls `hitLight` at one place, outside the loop its steps take, and the merge
marches at one place, which a test holds by the SPIR-V. A cascade is traced at `TracedMeans.perRead`
texels a side of each it is read at, 2 at four times the rays, which the lintel's sampling asks, or
1. A surface whose probes stand off it gathers along its own rays (`gatherTraced`). The merge keeps
the sky's share beside the light in one pass (`IFarSkyProbes`, `IMergedSkyLight`). A probe inside a
mesh keeps `ReachInside` in its reach map, and the far probes about a hand-off are tested once. The
shading at a merge's hits is most of its cost, and each binding passes the merge a cheap hit of its
own, the sun's reach read at the surface and the probes' light half a spacing off it, or the held
light of the field's cells (`HeldHitLight`, `holdCellLight`) held whole each frame. A binding may
list the cells to hold first (`listHeldCell`, `IHeldList`) and hold them a thread a listed cell
(`holdListedCell`), the count set to 0 among the frame's commands before the listing and the hold
dispatched over every cell or sized on the GPU from the count's buffer. A crossing passes each
step's own share and takes its light where it has stopped half the light it carried in. A cascade's
traced texels a side of each read one are a binding's setting for each cascade, the finest two at 2
reading the lintel's face 0.825 at 2.2 times the trace's steps. The closed rooms' 5 percent over
lies in the bounce a binding's hit reads of its probes, the passes adding none on one bounce. A
vestibule beside a sunlit court reads over its trace in the light its texels' own rays bring from
their probes' places and their hits, the chain adding 5 percent. A grazing way handed off over a
floor takes what the far probes over it see past a step it meets beyond its end, 3.0 times its
trace, a test of the over-read, since marching on where those probes stand off the ray mended it on
the CPU and not in 3DEngine's door view. The tests build their kernels for the CPU at -O2, at which
Visual Studio's cl optimizes them as gcc and clang do. Next, as the bindings land the passes: the
engines' further findings as they take the passes, then the split sum at a hit, the screen probes,
the light on the picture and the contact shadow's march. Left behind each part's Reply: the field's
normal, the cascades' table, the march held within a cascade, the splat's second plane, the lending
of thin emitters, the repaint and the clearing of codes. Open: AGENTS.md's documents rows for
DEVLOG.md and CHEATSHEET.md wait for the owner. Read first: AGENTS.md, this file's head and
Decisions, the Replies from the field's distance on, the README's binding section, NORM.md's N 4.8
to 4.11 and N 7.9, `CpuKernel.cs` and the head of `rc_probe_passes.slang`. Every build and test goes
through N 7.9's lock by `python3 -I modules/Tools/scripts/gpu.py shared -- <part> -- <part>`, built
with `--no-restore -p:UseSharedCompilation=false`, its output written to a file of which the summary
line and the failing lines alone are read, and each commit is read by `commitcheck.py . --staged
--message=<file>` first. At a binding's request, in the commit carrying this line:
`build/fetch-slang.sh` reads an archive's sha256 with `shasum -a 256`, on macOS, Linux and Git's
bash on Windows alike, and Python's hashlib where a system has none, in place of `sha256sum`, which
N 6.6's check refuses. This repository's own tests do not run that check on its scripts; the
binding's suite found it.

Item 8, in the commit carrying this line, after `af7d199`. A merge says whether it reads the
per-probe stage (`IMerge.readsStage`), and a binding runs `stageProbe` over a cascade's probes
before the cascade's merge, keeps one uint a probe (`IStageKeep`) and hands the values to
`mergeTexel` beside the merge (`IStageValues`), `NoStageValues` where the merge reads none, so a
merge that reads none pays nothing. `FarSightLines` keeps a bit for each of the eight far probes
about a probe, clamped onto the far grid, set where the probe sees it along a line marched through
the field past surfaces that let light through (`seesAcross`, `opaqueAlong`), half a cell kept clear
at either end. The eight lengths wait for a merge that asks for them. The third rung,
`MergeByMarchedSight`, is gi_merge.slang's merge to the letter but in three places where the module
has no like. The far texel's mark, whole, half or inside, has none, so a far probe inside a mesh is
passed over by its reach map. The march on begins at the near ray's end, the interval's end for a
ray that reached it. A texel traces one ray where 3DEngine traces four. The ladder's cost counts the
stage's steps. The theories pin each merge's own bound. The first commit's port fix stands, the
second merge leaving out far probes outside the field, whose reach maps are open along every way.
Its lintel weakness, far probes above the opening read toward the ground under the lintel, stays
pinned.  The third rung loses light by its rule and not by the port. Where the march on meets a
surface short of the far level's end and no far probe's own hit lies within its interval to agree,
as at a graze over a floor, the light from past the ray's end is none, so the uniform closed room
reads 0.84 to 0.96. Where every coarser probe about a probe stands behind the room's faces none is
seen, so the lintel's deep end reads 0.15 of its trace and its face under the sky alone none of it.
The ladder ranks it last at both configurations, 0.426 and 0.349 on the mean distance.  The ladder's
configuration axis takes 3DEngine's configuration as its session gave it, probes every 8 cells of a
field of five cascades of 64 cells from 0.125, 8 a side, spacing 1 to 16, the finest two traced at
16 texels a side and read at 8 for four rays a texel, every ray to four spacings but the last two's
to the field's diagonal of 221.7, caps of 64 but the last's, probes moved 0.1 clear by at most 0.45
of their spacing, and each point gathered from the finest level about it. The door is Sponza's
vestibule and not a built room, so the ladder's stand-in is a vestibule off a sunlit court. At that
configuration the CPU does not read the walls high. `MergeAtRayEnd` reads the door's walls 1.075 and
the vestibule's 1.04 to 1.07, where the device path at `42ed4e13` reads up to 2.05. So the cause is
not sampling at that configuration, and the ladder does not predict the device. The two part before
the gather, since the device's probe (3,4,2) of cascade 1 reads 2.1 times main's on its +z face, at
what the CPU does not hold, the light a hit takes, which on the CPU is a fixed light and on the
device the probes' own light frame over frame for 240 frames, beside the sun and Sponza's meshes.
The engine's own merge reads the vestibule's walls 0.555 on the CPU where main reads 1.07 on the
device, so the device's walls take most of their light through that bounce, and the ladder predicts
the device only where it holds the bounce. No merge stands above every other at both configurations,
`MergeAtRayEnd` reading 14.4 a texel at the engine's against marched sight's 12.7, so the default
stays `MergeAtRayEnd`.  Item 8's two commits stand as above, every merge behind `IMerge`, the stage
and the ladder at two configurations, `.github/LADDER.md` written by `RC_LADDER=.github/LADDER.md
dotnet test RadianceCascades.Tests --filter FullyQualifiedName~The_Ladder` in about five and a half
minutes. Open: the ladder holds one bounce, so it does not say whether the device's 2.05 lies in the
bounce; AGENTS.md's documents rows for DEVLOG.md and CHEATSHEET.md wait for the owner, and
LADDER.md's row is with the owner. Read first: the README's ladder section,
`rc_marched_sight_merge.slang`, `stageProbe` in `rc_probe_passes.slang`, `ProbeMergeTests.Ladder.cs`
and the test kernel's pass 10.

Item 9, in the commit carrying this line, after `e310325`. The ladder runs every merge to a steady
state of the bounce. A round runs the whole chain with every hit taking its surface's own light and
what the surface reflects of the light the probes gave it the round before, a half where it is not
black and the lintel room's 0.352, gathered at a cell of a grid of 64 a side over the scene's
surfaces, and nothing is kept past that one round. The test kernel reads that light a cell back
along the ray from the hit (`bounceAt`), so a march that stops at a sheet's plane inside a wall one
cell thick reads the side it came from, which a first run without it leaked through the thin wall at
0.05 of the lit room. The traced loop follows the same rounds over the same cells, those the points'
rays meet a surface in and those the cells' own rays meet in turn, 2,392 to 6,372 a scene, so the
two loops part only where a cell's light comes from, the probes or the trace. A closed room whose
faces send 1 and reflect a half settles at pi times 2 less a sixty-fourth in seven rounds at a gain
of 0.5000 traced, and the merge at the ray's end follows it at 1.0000 round for round, a test of the
default suite. Each run of a kernel on the CPU enters a library of its own, a copy of the compiled
file, so the ladder runs its 48 loops side by side in about five minutes.

With the loop in, the vestibule's walls at 3DEngine's configuration read 1.042 of their trace after
one gather and 1.033 at the steady state with `MergeAtRayEnd`, in seven rounds at a gain of 0.443,
where the traced loop's is 0.447. No merge carries an error round the loop on the CPU, since no
merge's gain lies more than 0.03 above the traced loop's in any scene. The merge that loses light at
one gather does not hold the loop down either. By marched sight the vestibule's walls read 0.555
after one gather and 0.600 at the steady state, since what it loses is the light from past its rays'
ends, the court's through the opening, in every round alike, and its gain lies up to 0.085 below the
trace's. The merge at the ray's end holds the loop nearest one at the engine's configuration, 0.043
on the mean distance, and by reach sight at the module's, 0.083 against 0.098 with a worse worst
part. No merge stands above every other at both, so the default stays `MergeAtRayEnd`.

The CPU reads near one with the bounce in, so the 2.05 lies in what a device hit reflects of the
probes, where the CPU's reflects its albedo over pi of the module's gather. For the device's walls
to read twice the CPU's from the same first frame, its gain a frame must be near 0.72 against the
CPU's 0.44. A device hit (`glossyHit` in 3DEngine's `probelight.slang`) reflects its diffuse share
past the gloss's of a six-faced cube of each probe's light read at the normal and moved toward the
faces' light at the surfaces (`towardSurface`), beside the gloss's share of the same cube read about
the mirror way, both times `lights.counts.z` and `kept`. The gloss's share takes no part of the
paint's color, so a smooth wall at a graze reflects nearly all it takes, and the gain a frame can
lie well above the paint's albedo. The same gain would raise main's merge, which reads 0.600 on the
CPU at the steady state, and main's 1.07 on the device would then be a merge that loses light
carried up by a hit that reflects too much. The 3DEngine session is asked, through the reviewer's
queue, for frames 1 to 10 from a cold start at `42ed4e13` and at `df4d0559`, the +z face of probe
(3,4,2) of cascade 1 in `bounced` and the left wall, with `lights.counts.z`, `kept` and the gloss's
share at the wall's hits, to set beside the vestibule's rounds in LADDER.md.

Item 9 stands in one commit, the bounce in the test kernel and the tests, the ladder's tables after
one gather and at the steady state with the rounds and the gain a round, the vestibule round by
round, and the CPU kernel's copies. Open: the device's frames from the 3DEngine session, and
AGENTS.md's documents rows for DEVLOG.md and CHEATSHEET.md wait for the owner, LADDER.md's row with
the owner. Read first: `ProbeMergeTests.Bounce.cs`, `bounceAt` in the test kernel, the README's
ladder section, and the last two tables of LADDER.md.

Item 10, in the commit carrying this line, after `c395762`. `rc_cone_read` reads the cone a rough
surface reflects from the merged levels and traces nothing, whole each frame with no history, behind
`IConeRead`, whose first read is `ConeAtProbes`. It weighs each texel of a level by the lobe of a
GGX surface of alpha the roughness squared at the texel's middle way times its solid angle, at the
eight probes about the point by their trilinear weight and the gather's sight, so the thin wall's
leak is 0 at the module's configuration. The lobe is never narrower than half a texel's mean width,
the square root of pi over the texels a side, and the level read is the finest from the point's own
up that keeps texels as narrow as the cone, or else the first that keeps the most. Test pass 11 runs
it over the ladder's eight scenes, a low room lit by a small bright light and a red wall beside a
gray floor, at 0.1, 0.2, 0.3, 0.5, 0.7 and 1.0, against the same lobe traced with 4,096 rays a cone,
the trace's own noise at most 0.017 of a scene's light. A difference is taken over a scene's traced
light at roughness 1.0, so a scene whose narrow cones see little is read against the light it holds.

The mark, on the median scene, is 0.5 at the module's configuration, 0.3 reading 0.103, and 0.7 at
3DEngine's, 0.5 reading 0.118. Rays a probe set it at the module's: 16 texels a side floor the lobe
at alpha 0.111, roughness 0.333, and the widening alone parts by 0.081 at 0.3, 0.295 at 0.2 and
0.438 at 0.1. Probe spacing sets it at 3DEngine's. Its finest two levels keep 8 texels, flooring the
lobe at roughness 0.471, so a narrower cone reads the first level that keeps 16, whose probes stand
4 apart against the point's 1. There it reads under the trace by 0.42 to 0.47 of the scenes' light
on the mean and leaks 0.021 of the lit room through the thin wall. The far cascades' sky share does
not set it, since the open box and the red wall under a sky mark with the closed rooms. On the mean
over every part, no roughness reads within 0.10 at either configuration. The lintel room's points by
the opening part most, by 4.8 at the module's and 7.9 at 3DEngine's at 0.1, falling to 0.11 and 0.14
at 1.0, and the lintel room and the roof window read within 0.10 only at 1.0. The small bright
light's floor parts by 1.0 to 3.2 at the module's at every roughness and the closed box with one
lamp by 0.12 to 0.88 at both, since a texel takes its light from one ray at the module's finest
level and from four at 3DEngine's, so a small source fills a whole texel or misses it. A cone reads
167 to 323 texels at the module's and 88 to 171 at 3DEngine's. On the sheet's spheres before the red
wall, the module's reading parts from the trace by 0.039 at 0.3 and 3DEngine's by 0.030 at 0.5.

For each level from the one a point gathers from up, a host hands the kernel the level's cascade at
the texels it keeps, its merged light at those texels, its reach map and its probes' places. Its
merge writes all of these from the coarsest level down. Per point, it hands the place, the normal,
the way toward the eye, the roughness and the level its gather reads, and a buffer for the light. It
scales that light by its own reflectance, as a split sum scales its filtered light.

Item 10 stands in one commit, which holds the read and its shapes, test pass 11, the closed room's
fact of every cone reading 1, the cones' section of LADDER.md with each scene's mark, the README's
section of rough reflections and the sheet. Open: the parity dump from the 3DEngine session, whose
replay goes first, and AGENTS.md's documents rows for DEVLOG.md and CHEATSHEET.md, which wait for
the owner, as LADDER.md's row does. Read first: `rc_cone_read.slang`, `ProbeMergeTests.Cone.cs`,
pass 11 in the test kernel, and the cones' section of LADDER.md.

The parity dump, in the commit carrying this line, after `11ece81`. 3DEngine logged probe (3, 4, 2)
of cascade 1 in the door view on the two frames after the cold start's clear, at `42ed4e13`, whose
module is `84c4225`. Since then the library's passes have changed only by moving the merge into its
own shape. `ParityReplayTests` runs each texel's trace and merge again on the CPU through
`parity_replay_cpu.slang`, every call answered from the log. On both frames, all 256 texels of the
trace and of the merge make the device's calls in its order, their arguments at most 7.6e-6 from the
device's, and keep its light to the bit. The library's passes run alike on the device and the CPU,
so what parts lies in what the binding answers. The held cells do not carry the old bounce. On frame
1, the merge's 16 hits past the rays' ends read 0 but for two directly lit cells (0.41 each), and on
frame 2 the held cells refill and read 0.005 to 0.03. Cascade 1 traces in halves, so on frame 1 the
probe's merge reads its rays kept from before the clear. They end where frame 2's fresh rays end on
every texel, and frame 2's light is 0.58 of theirs on the median texel (0.32 and 0.73 the
quartiles), so the clear leaves the old light in every ray of the parity not traced that frame. This
probe's merged light on frame 1 is 0.95 of frame 2's, the old rays' extra light offset by the empty
held cells, so it does not by itself give the door view's 1.482 against main's 0.834. The run that
would part the two is frame 1 at `42ed4e13` with the kept rays cleared with the gather's images, or
with every probe traced on the first frame. Open, in place of item 10's list, are that run in the
3DEngine session and AGENTS.md's documents rows for DEVLOG.md and CHEATSHEET.md, which wait for the
owner, as LADDER.md's row does. Read first: `ParityReplayTests.cs` and `parity_replay_cpu.slang`.

The clean frame 1 from the 3DEngine session, at `42ed4e13` with the probe traced whole on the first
frame, replays alike, all 256 texels traced and merged as the device did them, arguments at most
7.6e-6 apart and the light kept to the bit, on that frame and the next. Its rays carry no light on
frame 1, and the probe's light comes from texel (1, 3) alone, whose rays march on past their ends
and meet the floor through the held cells. The 3DEngine session reads the split at frame 1 as
`df4d0559`'s carried rays, whose spot follows the frame's number, and the settled split as
repeating, 2.034 to 2.045 at `42ed4e13` against 1.072 to 1.075 at `df4d0559`. The CPU's loop settles
the vestibule's walls at 1.033 of their trace with `MergeAtRayEnd`, near `df4d0559`'s and far from
`42ed4e13`'s. A dump of a settled frame replays through `ParityReplayTests` as it stands.

Item 11, in the commit carrying this line, after `151689d`. `MergeAboutProbe`, in
`rc_reach_sight_merge`, is the fourth merge. Its `farLightAbout(field, probes, places, far, place,
way, texels, out skyShares, out weights)` takes the shape the host asked for, blending the far
probes about the near probe's own place, each at its trilinear weight where it stands within the
field and sees that place by its reach map, and reading each toward the point of the near ray
nearest that probe's hit along the way. No march past the ray's end leaves a probe out, and the ray
marches on itself to the march's end where none counts. The new scene, `ArcadeCourt` in
`ProbeMergeTests.Court.cs`, is a court 8 high open to a black sky, its floor lit in a strip 3 wide
down its middle with the shadow lines of the walls' tops along it, an arcade under a roof at 3 along
each long wall with a beam across its front, and the long walls read low within the arcades and high
above them. It meets the mark at an engine's configuration, the merge at the ray's end reading the
high wall 2.367 of the trace after one gather and 1.922 settled, against the low wall's 1.163 and
1.082, which a test holds. It does not part the merges as the device did, since every merge reads
the high wall 2.37 to 2.73 of the trace after one gather there (`MergeAboutProbe` 2.727,
`MergeByMarchedSight` 2.573), so on the CPU the surplus lies outside how a merge hands a ray off. At
the module's configuration the merge at the ray's end reads the high wall 1.212 after one gather and
0.883 settled, and the blend about the probe parts the two walls most, 0.575 low and 1.264 high
after one gather. Over the eight scenes of the standing, the mean and the worst part read 0.095 and
0.487 for `MergeByReachSight`, 0.099 and 0.329 for `MergeAtRayEnd`, 0.130 and 0.720 for
`MergeAboutProbe` and 0.501 and 1.769 for `MergeByMarchedSight` at the module's configuration, and
0.072 and 0.653, 0.107 and 0.724, 0.227 and 0.750, and 0.283 and 0.768 in the order of the standing
at an engine's, the merge at the ray's end first. The merge at the ray's end stands above
`MergeAboutProbe` at both and above `MergeByReachSight` at an engine's, and no merge stands above
every other at both, so it stays the default. `MergeAboutProbe` takes 0.0 steps a texel past its
ray's end on the mean and reads 7.9 and 16.2 far texels a texel, against the merge at the ray's
end's 6.7 and 14.3. A host hands it nothing it does not hand today, the field, the far probes'
light, sky shares and reach (`IFarSkyProbes`), their places and the sky, as for `MergeAtRayEnd`, and
names `MergeAboutProbe merge;` before `mergeTexel` and `gatherTraced`. Where the host's merge reads
a far probe's reach as the mean of four texels, the module reads the one texel the way toward the
place falls in (`texelOfWay`).

Handoff, for a fresh session, beside the one above that begins with where the work stands, whose
account of the library holds. The ladder runs four merges over nine scenes at two configurations,
eight in the standing and the window held out, and writes `.github/LADDER.md` and the README's table
where `RC_LADDER` names the file. The court between arcades holds the device's case of a high wall
read over its trace by the merge at the ray's end, but at an engine's configuration every merge
reads it over by about the same, so the CPU does not yet rank the merges as the device did, and the
merge at the ray's end stays the default. `ParityReplayTests` replays a binding's dump of a frame
through the passes. Open are AGENTS.md's documents rows for DEVLOG.md and CHEATSHEET.md, which wait
for the owner, as LADDER.md's row does. Read first `ProbeMergeTests.Court.cs`,
`ProbeMergeTests.Ladder.cs` and `rc_reach_sight_merge.slang`.

Item 12, in the commit carrying this line, after `6aa63e9`. The source is the level the gather
reads, named in one reading. At an engine's configuration the court's wall points lie at z = -3.75,
a quarter of a unit past the finest level's last probes at -3.5, so the gather takes them to the
level spaced 2 (`gatherLevel` in the test binding, the finest level whose probes stand about a point
on every side). Of that level's trilinear weight over the high wall, the probes 0.75 off the wall
above the roof hold 0.572 and count, and the row at y = 3 in and under the roof (0.084) and the
column behind the wall (0.344) are left out already, by the field and by their sight of the point,
so leaving out the probes the item names reads 2.367 as it stands, and so do the probes left at
their grid places. Probe (-1, 5, -3) of that level, followed through its 256 texels, keeps the light
traced from its place along every way, 5.065 toward the court against 5.065, where the wall's point
behind it sees 1.238 along the same ways. The texels that carry it look down past the roof's edge,
from (0.09, -0.83, 0.55) to (0.53, -0.74, 0.42) and their mirrors, and their rays end on the sunlit
strip 5.3 to 9.5 units out, where the same ways from the wall's point meet the roof's top 1.6 to 2.9
out. One thing at a time, after one gather, the high wall reads 2.367 of its trace as it stands,
1.240 gathered from the finest level, 1.729 along each point's own rays, 1.801 with the level spaced
2 the last and its light past its end none, 2.027 with four times the rays a probe (32 texels a
side, as an octahedron holds a square count), 1.432 with the spacing halved, and 2.367 with the sky
share zero, since the court's sky is black and the share of 0.093 carries no light. The module's
configuration gathers from the finest level alone, its edge probes a quarter of a unit off the wall,
and reads the high wall 1.212, the same source at a third of the offset. Along each point's own rays
it reads 1.535 there and 1.729 at an engine's, since `gatherTraced` hands each ray on at open space,
half a unit out, to the probes about it, which see past the roof's edge as the gathered probes do.
The mend proposed is the binding's rule and no kernel's, to gather a point from the finest level
whose probes stand about it or within half its spacing past its edge probes, the level's own cells,
which `GatherRim` sets in the test binding. It adds no read and no step. The court's high wall reads
1.240 after one gather and 1.177 settled, in place of 2.367 and 1.922. Over the ladder's scenes at
an engine's configuration with the merge at the ray's end, settled, the mean distance goes from
0.072 to 0.074 and the worst part from 0.653 to 0.446. The vestibule's walls go from 1.033 to 0.995
and the held-out room's walls from 1.166 to 1.026, and the lamp's box falls from 0.907 to 0.640 on
its floor and from 0.919 to 0.672 on its walls, which is the mend's cost and which this item does
not read further. By the ladder's rule the mend does not stand above the gather as it stands, being
farther on the mean.
`A_Court_Wall_Past_The_Finest_Probes_Takes_The_Light_The_Probes_A_Level_Above_See_Three_Quarters_Of_A_Unit_Off_It`
holds the probe and the two readings of the high wall, and `dotnet test RadianceCascades.Tests
--filter FullyQualifiedName~Court` with `RC_COURT` set measures the rest in some ten minutes. The
setup comment of the test kernel now names the fourth merge at 127.

Handoff, for a fresh session, beside the two above. The court's surplus on the CPU is the gather's
level, the wall's points read by probes a level above that stand 0.75 off the wall. The rule that
would mend it, `GatherRim`, is in the test binding and off in the ladder, waiting on the owner's
choice against the lamp's box. Open are AGENTS.md's documents rows for DEVLOG.md and CHEATSHEET.md,
which wait for the owner, as LADDER.md's row does. Read first `ProbeMergeTests.Court.cs` and
`gatherLevel` in `probe_passes_cpu.slang`.

Item 12's rim, in the commit carrying this line, after `1e53334`. An engine's configuration in the
tests now gathers each point from the finest level whose probes stand about it or within half its
spacing past its edge probes (`GatherRim`), and the README says what a binding does at a level's rim
and why. A host's binding should hold by a level every point within half its spacing past its edge
probes, the level's own cells, its edge probes taking it, and hand to the level above only a point
past that rim, since a wall's points a quarter of a unit past the finest level's last probes
otherwise go to the level above, whose probes stand a spacing off the wall and see past what hides
it. With it the ladder at an engine's configuration reads, on the mean and in the worst part, 0.074
and 0.446 for `MergeAtRayEnd`, 0.117 and 0.388 for `MergeByReachSight`, 0.229 and 0.655 for
`MergeAboutProbe` and 0.316 and 0.810 for `MergeByMarchedSight`, and the merge at the ray's end
stands above `MergeAboutProbe` alone there and stays the default. The lamp's box moves to 0.640
since its floor's and walls' points, past the finest level's probes as the court's are, now go to
that level's probes a quarter of a unit off, which trace the lamp nearly as the points do (0.373 at
a wall probe's place against 0.343 beside it), where the level above's probes 0.75 off stood nearer
the lamp and traced more of it (0.484 against 0.387). Probes of both levels keep only part of the
small lamp where it lies past their rays' end, a finest wall probe 7.016 of the 10.482 its ways
trace, one of the level above 6.535 of 13.117 and a finest probe over the floor 3.445 of 14.905, so
the over-read 0.75 off had hidden a loss at the hand-off, which the rim shows. With the rim the
court parts the merges, the merge at the ray's end nearest and the three that blend the far probes
about the near probe's own place over the trace on the high wall, the other way round from the
device, each part's light after one gather and settled as `RC_LADDER` writes it.

| Merge | Low wall | High wall |
|---|---|---|
| `MergeAtRayEnd` | 1.175 and 1.137 | 1.240 and 1.177 |
| `MergeByReachSight` | 1.324 and 1.259 | 1.668 and 1.474 |
| `MergeByMarchedSight` | 0.871 and 0.871 | 1.681 and 1.454 |
| `MergeAboutProbe` | 0.899 and 0.899 | 1.780 and 1.484 |

The lamp's box's figures come from `dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~Court` with `RC_COURT` set, which reads the court's knockouts of item 12 with
each point held by the probes about it, as they were read.

Handoff, for a fresh session, beside the ones above. The rim is on at an engine's configuration in
the tests, the court parts the merges with the merge at the ray's end nearest, and the lamp's box
shows the merge's loss of a small bright light past a ray's end. Open are AGENTS.md's documents rows
for DEVLOG.md and CHEATSHEET.md, which wait for the owner, as LADDER.md's row does.

The owner confirmed AGENTS.md's documents rows on 2026-11-23, and the commit carrying this line
links DEVLOG.md and CHEATSHEET.md there with the commands that write them and adds a row for
LADDER.md with its command.

The moving thin wall, in the commit carrying this line. `stampCell` takes the cascade's cell width
and stamps a box thinner than a cell along an axis of its own a cell across there, held in its own
space by the length of each axis's row, so a turned or scaled box holds a cell of the world.
`StampTests` stamps a wall of 0.1 at eight places across a cell at each cascade's cell of an
engine's field. At its own thickness a march passes it at 0, 3, 5, 5 and 7 of the 8 at cells of
0.125 to 2. Held, every place holds a cell's middle and the row reads the distance to a wall a cell
thick. A thin moving mesh's faces then stand out by half a cell less half its thickness, 0.0125 at
0.125 and 0.075 at 0.25, so near them a ray along that axis meets it up to that much early, a probe
that near its face stands inside it, and a point on its face lies that far inside the field's wall.
The engine's call must hand the cell, `stampCell(shapes, p, cellWidth, reach, still)`, its
`originAndCell.w`, when it moves its pointer. Handoff, for a fresh session. The stamp holds thin
boxes, and the engine's call takes the new argument when it moves to this commit. Read first:
`rc_field_stamp.slang`, `StampTests.cs` and the README's paragraph on `stampCell`.

Item 13, in the commit carrying this line. `rc_probe_passes.slang` gains `GatherRim` (0.5),
`gatheredLevel(levels, p, rim)` and `gatherSurface(field, levels, surfaces, index)` in two forms,
the second keeping the share of the sky. `IGatherLevels` and `IGatherSkyLevels` sit in
`rc_cascade_merge.slang` beside `IFarProbes`, with `GatherLevelProbes` and `GatherSkyLevelProbes`
reading one level of them as a cascade's probes, so the cone read takes the same struct:
`IConeLevels` and `ConeLevelProbes` are gone for them, no engine having bound them.
`GatherLevelTests` reads the four points at levels 0, 0, 1 and 2 at the library's rim and 0, 1, 1
and 2 with none. The test kernel's gather calls `gatherSurface` at an engine's configuration and the
library's `gatheredLevel` for its knockouts, and the court and the ladder read as before, the high
wall 1.240 and LADDER.md the same but its date. An engine hands `gatherSurface` its field
(`IProbeField`), its pixels (`ISurfacePoints`, the place from depth and the normal, keeping the
light) and one struct over its cascade images (`IGatherLevels`). That struct gives the level count,
each level's `ProbeCascade` at the texels it keeps, a probe texel's merged light and reach, and a
probe's place by its own move rule, in place of `finestAround` and `probesAround` in one engine's
`gi_screen.slang` and the finest-cascade choice in the other's `gi_light.slang`. Handoff, for a
fresh session. The gather's level and weights are the library's; item 10 follows the engines' probe
passes. Read first: `gatherSurface` in `rc_probe_passes.slang`, `IGatherLevels` in
`rc_cascade_merge.slang` and `GatherLevelTests.cs`.

Item 14, in the commit carrying this line. The mark is not the cause. The engine's branch at
997d10e7 hands the module every interval from 0, so each ray is traced from its probe and none
begins past a surface. Built with each level's rays past the first begun at its spacing, as the
engine's main begins them, and those meeting a surface between the probe and that start kept as met
with no light, the floor under the solid slab read 1.562 of the sky traced there at the ray's end
and 1.693 by reach sight, alike with the mark, without it and with every ray begun at its probe, and
1.056 against 1.059 with every surface sending half the sky's light, so the mark is not kept. Two
causes are, and the floors test prints both (`dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~A_Floor_Under_A_Solid_Slab_And`). The slab's floor reads 1.693 by reach sight
with the finest two levels read at 8 texels a side, the 2 by 2 means of the 16 they trace, as an
engine binds them, and 0.915 read at the 16. The floor takes the sky under the slab's edge at a
graze, and a mean of four rays read at its texel's middle way weighs the low two at the middle's
cosine, about twice their own, in the gather and again where the level below takes the coarser
texel. The open box's floor read 0.612 by reach sight, where the far cascade ends at the field's
edge and the ray takes every far probe that sees its probe with no march, a probe in the box's
corner lending the wall beside it along a way the ray leaves by through the top. Such a probe
weighs nothing where its own hit lies short of where the ray ended (`sightedFarLight`), and the
floor reads 1.160. The test holds the slab within 0.15 of the trace in the logarithm with the
finest two read at 16, and the open box within 0.2 as bound. By reach sight the ladder reads at the
engine's configuration 0.088 on the mean where 0.117 and 0.367 worst where 0.388, the vestibule
0.022 where 0.129, the open box 0.016 where 0.069 and the door 0.026 where 0.014, and at the
module's 0.092 where 0.095 and 0.454 worst where 0.487, the lintel's
(`RC_LADDER=.github/LADDER.md dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~The_Ladder`). A merge taking the march's own surface wherever it meets one, in
place of the far probes that agree on it, read the slab 0.856 but the module's lintel's deepest
0.547 where 0.615 and its closed box 0.261 where 0.096, so it is not kept. The engine's slab reads
near its reference with its finest two levels kept at the 16 texels they trace, four times their
merged light's memory, and keeps the excess at 8. Handoff, for a fresh session. Past a far cascade
that ends at the field's edge, the merge by reach sight leaves out each far probe whose own hit
lies short of the ray's end, and nothing an engine binds changes. Read first: `sightedFarLight` and
`reachSightLight` in `rc_reach_sight_merge.slang` and `ProbeMergeTests.Floors.cs`.

Item 15, in the commit carrying this line. The surplus is where the probes stand, not the merge, the
kept texels or the levels. On a wall under a lamp, a ladder scene now, at the engine's
configuration by reach sight, the top middle reads 1.507 of its trace after one gather and 1.432 at
the steady state. The finest level's probe half a unit before it keeps 1.140 of the light traced
along its texels' ways from its place, and those ways take 1.834 of what they take from the wall's
point behind it, which sees the lamp lower (`dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~A_Wall_Under_A_Lamp`). Read while working, the part read higher still with the
finest two levels kept at the 16 texels they trace, lower at the module's configuration, and
nearer its trace along the point's own rays (`gatherTraced`). The module's loop gains 0.486 a round
where the trace gains 0.489, so the surplus does not grow from one gather to the steady state. The
engine's growth from 1.02 at zero bounce to 1.5 to 1.85 settled is the light its hits take from its
faces (`bouncedPlaced` and `probesHold` in its `lights.slang`), which grow alike, and lies outside
the module. On a flat wall a model of the sight check over the lamp wall's points, its probes half
a unit or a tenth off, found every point seen by some probe, and the few the module reads black at
the wall's top corners at the ray's end are seen by their probes, whose texels' middle ways miss
the small lamp and read nothing traced from their places too. A point no probe of its level sees,
each inside a mesh or not seeing it by its reach, now takes the level above's light and goes no
further (`gatherSurface` and `gatheredSums` in `rc_probe_passes.slang`). The floor of a slot 0.6
wide, whose finest probes about it all stand inside the blocks and the ground, read nothing and
reads 1.513 to 1.546 of its trace. In a slot 0.2 wide, narrower than the level above's cells, the
rule walked to the coarsest lent two of the four points the whole sky from a probe over the
blocks, so it stops at one level, and those two stay dark while the other two read 1.260
(`dotnet test RadianceCascades.Tests --filter FullyQualifiedName~A_Point_No_Probe`). By reach
sight the ladder over ten scenes reads 0.095 on the mean and 0.367 worst at the engine's
configuration where 0.088 and 0.367 over nine, and 0.103 and 0.454 at the module's where 0.092 and
0.454, the new scene alone moving them, and at the module's configuration the merge at the ray's
end now stands above it, the default staying `MergeAtRayEnd`
(`RC_LADDER=.github/LADDER.md dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~The_Ladder`). Handoff, for a fresh session. The gather falls to the level above
for a point none of its own level's probes sees, and nothing an engine binds changes. Read first
`gatherSurface` and `gatheredSums` in `rc_probe_passes.slang` and `ProbeMergeTests.Lamp.cs`.

Item 16, in the commit carrying this line. The lintel's miss came with item 14's rule, which left
out each far probe whose own hit lay short of where the ray ended where the far cascade is the
coarsest, its end at the field's edge. That rule is gone. Such a ray marches on to twice where it
ended, where far's interval would end were the ends doubling to the last, and takes the far probes
by the rule a far cascade that ends within the march takes them (`reachSightLight`). Under a slab 2
over a floor in a field 19.2 across, the engine's slab's, a probe 0.6 up read its face toward +x
1.73 times the sky traced along its texels' ways and reads 1.199 with the march, and the ways of
its face toward -z that meet a surface in the field take 0.013 where the trace gives none (`dotnet
test RadianceCascades.Tests --filter FullyQualifiedName~A_Probe_Under_A_Slab`). Along the ways that
leave the field before they meet the floor that face takes 0.555 against 0.280 traced, the sky past
the field's edge where the floor lies beyond it, which no merge can know, so what 14's mend did not
reach of the engine's slab is its field's size. The module's floors under the solid slab and in the
open box read 0.915 and 1.160 as they did (`dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~A_Floor_Under_A_Solid_Slab`). Read by the engine's session with this file in
place of 39d0a8a's, the lintel's sky reads 0.0705 against the reference's 0.0802 where it read
0.1079, and its test passes. The solid slab's floor reads 0.141 of the open floor's trace where it
read 0.126, the half canopy 0.301 as pinned, and the open box 0.904, 0.943 and 1.373 at High,
Medium and Low as pinned. The box at Low stays over its 1.25, and 774d11d's file read it 1.289, so
its surplus is not this merge's and stays open. The ladder by reach sight reads 0.095 on the mean
and 0.367 worst at the engine's configuration as it did, its steps a texel 2.1 where 1.2, and 0.103
and 0.467 at the module's where 0.103 and 0.454, the module's lintel's deepest settled at 0.627
where 0.635 (`RC_LADDER=.github/LADDER.md dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~The_Ladder`). Handoff, for a fresh session. Where the far cascade's end lies at
the field's edge, the merge by reach sight marches the ray on to twice where it ended and takes the
far probes as a cascade ending within the march does, and nothing an engine binds changes. The
engine's slab needs a field reaching past its floor, and its open box at Low a reading of the
engine's own gather. Read first `reachSightLight` in `rc_reach_sight_merge.slang` and
`ProbeMergeTests.Floors.cs`.

Item 17, in the commit carrying this line. The corridor joins the ladder in
`ProbeMergeTests.Corridor.cs`, a corridor along x 2.5 wide and 3.5 high inside under a sun through a
gap in its roof and a lamp in the roof beside it, its points on the floor and on the wall at -z from
-6 to 6. The eye walks it in quarter steps, from -8 to 8 at the engine's configuration with each
field cascade and its level snapped to the level's spacing about the eye, and from -4 to 4 at the
module's with its levels snapped about the eye in the field's one cascade, which holds still, each
point gathered by `gatherSurface` within `GatherRim`. The ladder writes its section, The corridor
walked, at both configurations by each merge. By reach sight at the engine's configuration the
swing reads 3.594 worst and 2.466 at the 99th part and the step 3.558 and 1.299, the other merges'
swings 3.496 to 4.870 worst and 2.297 to 2.934 at the 99th part, and the module's 3.141 to 3.637 and
2.635 to 3.299. With the levels held still and the field alone moving over -8 to 8 the swing reads
0.631 and 0.246, where the walk kept out of the tree read 0.33 and 0.136 over -4 to 4, and with the
field held still 3.633 and 2.437, so the levels carry the swing (`RC_LADDER=.github/LADDER.md dotnet
test RadianceCascades.Tests --filter FullyQualifiedName~The_Ladder`). The three causes named in the
item were read from that walk's figures by level, which no committed command reads. A test holds the
step at the engine's configuration by reach sight under 3.6 at worst and 1.31 at the 99th part, the
dense levels' reading, in place of the 2% bound item 18 is to meet, and takes 44 seconds (`dotnet
test RadianceCascades.Tests --filter FullyQualifiedName~A_Corridors_Fixed_Point`). The ladder with
the corridor runs 10 minutes 45 seconds, past the GPU gate's shared bound of ten, so it ran under
`--minutes 30`, and the suite comes to about 9 minutes against that bound. No library code changes.
Handoff, for a fresh session. The corridor's walk is `CorridorWalk` and `CorridorSwings`, its
layouts `SnappedEngineField` and `CorridorModuleLevels`, and item 18's sparse probes are read on it
beside the dense grid as one more layout, the test's bound going to 2% once they meet it. Read first
`ProbeMergeTests.Corridor.cs` and the corridor's section of the ladder's test in
`ProbeMergeTests.Ladder.cs`.

## Decisions

1. **The repository's birth.** The owner made this repository on 2026-10-31 at 12:05 among six,
Modeling, Vegetation, Levels, Landscape, Tools beside it, after the ideas recorded as 3DEngine's
Decision 78 and BevyCSharp's Decision 71, to hold the cascades' CPU side, their layout, the probes'
placement, the field's build and the merge's rules, and the Slang shaders as one library both
engines use rather than one implementation an engine. The owner set the order the same day, the
documents first, then the rest as the other projects have it, then the engines taking the module,
and said the modules may be tried at once beside the engine's global illumination work (3DEngine
Decision 79).

2. **A git submodule, not a package.** The owner chose on 2026-10-31 that the engines take the
module as a git submodule built as a project of their own, since a package's upload, wait, test and
pack is slower to iterate than a commit. A change is committed here first; the reviewing session
pushes this repository before the engine's commit that moves its pointer; the workflows check
submodules out. The game keeps using the engine through the engine's package.

3. **The module knows no engine.** N 2.1. The module references no engine, package or bridge; what
it needs of a renderer, a world, a scheduler or a file system it takes through interfaces it
declares, which each engine implements where it binds the module. A type of an engine's in this
repository is a fault.

4. **The repository's terms are the owner's.** The repository was made by the owner with its terms
   file in place, which stands as the owner set it and is spoken of nowhere else.

5. **Who writes here.** The game's session writes items 1 and 2 for the six modules, being free
while the game waits on the engine's package; the session that owns the code today moves it (item
3), BevyCSharp's for Vegetation and Levels, 3DEngine's for Modeling and the cascades, the reviewing
session's scripts going into Tools; each engine's session binds the module (item 4). Every session
reads REVIEW.md's head, its items and the Replies before a batch, as N 7.7 says, and the reviewing
session pushes.

## Verdicts settled

2. **The build on `5dec8d3` is cancelled on Linux at the job's 30 minutes.** Read at 09:59 on
    2026-11-26, run of `5dec8d3`, job 139773518212. Windows and macOS pass in six minutes; the Linux
    test job ran past its limit of 30 minutes and was cancelled, the corridor walk on the split
    probes with its 256 warm frames and four at each quarter step being the new cost, the suite at 9
    to 10 minutes on the desk. `3d9cbfc`, pushed at 09:59, meets the same limit unless mended. The
    mend, its own commit ahead of the ladder's split rows, makes the walk's test fit the runner,
    fewer warm frames or points under a setting the ladder's full read does not use, with the full
    walk kept for the ladder's own run, and raises the Linux job's limit to 60 minutes beside it,
    saying the job's time after. Mended at `4bbcd0d`, pushed at 10:25 after the run of `3d9cbfc`
    went green within the 30 minutes. Without the ladder's variable the walk warms 64 frames, reads
    4 still and walks the first 2 units at the engine's configuration, 35 s on the desk where 2 m 10
    s, its bounds 0.37 and 0.24 for the step and 0.105 and 0.065 still at the engine's, 0.38, 0.20,
    0.055 and 0.04 at the module's; with the variable set the whole walk runs under the earlier
    bounds, the field built once for both. The Linux job's limit 60 minutes with the job times in a
    comment. Settled at 10:5x on 2026-11-26, run 45791850009 green with the Linux job at 21.2
    minutes, Windows 7.3 and macOS 6.1.
