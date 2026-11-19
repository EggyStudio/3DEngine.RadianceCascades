# Review

Reviewed up to `040353c`, item 7, the interface written out, the ten things an engine gives a kernel
as declarations with each field's name in each engine and both forms shown where they differ, a
section of .github/KERNELS.md, pushed at 02:29 on 2026-11-14; it keeps the form, and `21761ff` and
`ad7841e` after it, pushed at 02:30 and at 02:31, take three sentences that named an item by number
out of the document. Before it `13d21db`, item 6, what holds each kernel today, 262 tests of the two
engines tabled a pair with their devices and gaps, the rooms both draw alike and the order of the
move, a section of .github/KERNELS.md, pushed at 02:15 on 2026-11-14; it keeps the form. Before it
`7806020`, item 5, the two engines' radiance cascades read side by side in .github/KERNELS.md, each
kernel paired with its like, where each pair differs, what stays an engine's own and the interface
of ten things an engine gives a kernel, a document alone that starts no run, pushed at 00:10 on
2026-11-14; it keeps the form. Before it `8cc387d`, item 2, pushed at 12:31 on 2026-10-31 with
`8811a8e`, item 1, after the owner's first commit, its run green at 12:35 on the three systems in
about three minutes, the first of this repository. Both commits keep the form, the three-mark
subject, one sentence, the owner's identity as author and committer, no trailer (N 7.2 and N 7.6
kept at the first commit); the library references no engine (N 2.1). The documents are the engines'
with the engine's name replaced and the engine-only lines dropped, COMMITS.md the game's with N
7.1's pointer rule in place of the legacy branch line, .gitignore with the dump folder, .claude and
.ref; AGENTS.md's devlog row carries no link until DEVLOG.md exists and its table of areas gains the
rows item 2 adds. The skeleton sets ImportDirectoryBuildTargets false, since a submodule under
modules/ would inherit an engine's Directory.Build.targets, makes a missing summary an error, loads
the assembly by name in its one test, and build/version.sh prints 0.1.0; the strict build 0 warnings
and 0 errors, the test passing. After it `ff4bfe3`, the norm and the review's head alone, pushed at
14:51 on 2026-11-03, with no run since the build passes over the review's files; it keeps the form.
Before it `cbc0d97`, the owner's first commit alone.

## Now

The order the owner set on 2026-10-31 for the six modules, this one holding the cascades' CPU side,
their layout, the probes' placement, the field's build and the merge's rules, and the Slang shaders.
The documents first, then the rest as the other projects have it, then the engines taking the
module. Items 1 and 2 are the game's session's, being free while the game waits on the engine's
package; item 3 is the session's that owns the code today; item 4 is each engine's session's. Among
the six this module comes last, after the GI items of 3DEngine's Decision 75, since the cascades
change daily until then.

1. **The documents, one commit.** AGENTS.md on the game's shape (one paragraph saying what the
    module is and that no engine is referenced, N 2.1; the documents table with STYLE, COMMITS,
    REVIEW, NORM and DEVLOG; building and testing; Where things are with a row a folder; the
    conventions, the commit form among them), README.md (what the module is, how an engine takes it
    as a submodule, `dotnet build` and `dotnet test`), `.github/STYLE.md`, `COMMITS.md` and
    `CODE_OF_CONDUCT.md` as the engines' with the engine's name replaced, `.gitignore` of the
    engines' shape with `/.github/dump/` in it, `.gitattributes`. The game's AGENTS.md and the
    engines' documents are the models; nothing is written that the module does not have yet. This
    commit is the first test of the commit form here, N 7.2 and N 7.6, read by the reviewing session
    before it is pushed. Done at `8811a8e`.

2. **The skeleton, one commit after item 1.** `RadianceCascades.slnx`,
    `RadianceCascades/RadianceCascades.csproj` as a class library on net10.0 with `Nullable` and
    `ImplicitUsings` in `Directory.Build.props`, `RadianceCascades.Tests/` on xunit with one test
    that the library loads, `build/version.txt` at `0.1` and `build/version.sh` as the engines', and
    `.github/workflows/build.yml` on a push, `test (os)` on ubuntu-24.04, windows-latest and
    macos-15 with `dotnet build RadianceCascades.slnx -warnaserror` and `dotnet test
    RadianceCascades.Tests --no-build`, a time limit on every job with its reason, `permissions`
    read and the engines' concurrency group. Green on the three systems before item 3. Done at
    `8cc387d`.

3. **The code's move.** The source is 3DEngine's Rendering/Lighting and Shaders and the bridge's
render graph, each engine keeping its own resource binding, Slang compiling to SPIR-V for 3DEngine
and to WGSL for the bridge. One commit a part, the tests coming with the code, the strict build and
the suite green, a Shared line in the engines' REVIEW.md when a part lands. Items written by the
reviewing session when item 2 is green and the owning session is free.

4. **The engines take the module.** Each engine adds this repository as a git submodule under
`modules/` and references its project, its workflows checking submodules out, its own folder for
this code becoming the binding of the module's interfaces (N 2.1) and the rest deleted; a change
used by an engine is committed here first and pushed before the engine's commit that moves the
pointer. One commit an engine, after item 3's first part.

5. **The two copies read, before the move.** Written on 2026-11-13, ahead of item 3, by the session
    that is free while the game waits on the engine's package, as items 1 and 2 were. One document,
    `.github/KERNELS.md`, of the radiance cascades as each engine has them today, from reading
    alone, no engine's file changed and nothing built or run. 3DEngine's are the Slang under
    3DEngine/Shaders, cascades, gi, gi_trace, gi_merge, gi_ambient, gi_probes, gi_screen,
    gi_screen_filter, gi_screen_glow, glow, probelight, scenefield, the field's splat, stamp,
    resolve and count, lightthrough and roughreflect, with their C# under
    3DEngine/GlobalIllumination and Rendering/Lighting; the bridge's are the Slang under
    BevyCSharp/GlobalIllumination/Kernels with the C# beside them and bcs_pass. The document lists
    each engine's kernels and passes in the order a frame runs them; pairs each kernel with its like
    in the other engine or says it has none; says for each pair where the two differ, in the
    algorithm, in a constant, in what is read and written, with file and line in both, and which
    reads to the reader as the later or the mended form where that can be told from the code; says
    what is one engine's own and stays there, its bindings, its view's block, its field's lookups;
    and ends with the interface the module would name, the fewest things an engine must give a
    kernel. It also places the three findings that wait for the kernels written once, a hit past a
    cascade's band taking the next cascade's probes where none sees the point, a tied cell of the
    field's color pass taking whichever thread ran last, and the field's build in world floats, each
    at the kernel it belongs to. The engines' sessions read it at their items 265 and 138 and
    correct it in their Replies. One commit of the document here, read by the reviewing session
    before it is pushed.

6. **What holds each kernel, before the move.** Written on 2026-11-14, after item 5, by the same
    session, from reading alone. A section added to `.github/KERNELS.md`, the tests and reference
    rooms each engine has today for each kernel the document pairs, so the move of item 3 knows what
    must read alike before and after. For each pair, the tests in 3DEngine.Tests and in
    BevyCSharp.Tests that would fail if that kernel were wrong, with file and test name, what each
    holds, a level, a count of pixels, a reference picture or a path traced room, and on which
    device it runs; where one engine holds a thing the other does not, said as a gap with the test
    that would close it; and the rooms both engines can draw alike, so the two engines' pictures of
    one room can be compared, with what differs in how each builds it. It ends with the order the
    kernels could move in, the least bound first. One commit of the document, read by the reviewing
    session before it is pushed.

7. **The interface written out.** Written on 2026-11-14, after item 6, by the same session, from
    reading alone. A section added to `.github/KERNELS.md` that takes the ten things an engine gives
    a kernel and writes each as the declaration it would be, a Slang interface or struct with its
    fields and the functions a kernel calls on it, each field and function marked with what it is
    called in each engine today, with file and line, and with which engine lacks it. Where the two
    engines hold the same thing in two forms, both are shown and the difference said, with no choice
    made between them; where one engine's form holds what the other's cannot, that is said. It ends
    with what each engine would have to change in its own bindings to give that interface, a list a
    file. The engines' sessions choose between the forms at their items 265 and 138. One commit of
    the document, read by the reviewing session before it is pushed.

## Verdicts

None yet.

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

6. **The module is worked by a session of its own.** Decided at 16:43 on 2026-11-18 for the seven
    modules at once. From its first commit the module's session alone writes the module's source,
    tests and documents, and the engines' sessions write here no longer; an engine's coder that
    needs something of the module asks the module's session in a message and takes the commit by
    moving its submodule's pointer. Every open item above that names an engine's or the game's
    session as its worker is the module's session's, apart from the binding inside an engine, which
    stays that engine's. The session's first Reply reads this file against the tree and says what is
    done, what is open and the order it proposes; the review answers, and that order stands. The
    rules are NORM.md's as before, among them no engine referenced (N 2.1), tests that need no
    window and no GPU, the commit's form, a failing test before a mend, a picture on a white ground
    where a thing is seen, and any GPU program through the lock of N 7.9 with a waiting file. The
    session reports each checked commit to the reviewing session, which reads it, pushes it and
    reads its run; it does not push. Here the session owns the kernels' Slang and the interface the
    engines bind through, and the move of 3DEngine's item 265 and BevyCSharp's item 138 is done with
    both engines' coders, each engine's coder keeping its bindings, its tests and its rooms; the
    read of where the two copies differ is written three ways, this module's Reply holding the
    interface.

7. **The module is agnostic enough that a stranger binds it in an engine of their own from its
    README alone (2026-11-18 17:28).** It asks a README section, Binding it in your engine, with
    every interface the module declares, the conventions it stands on and the smallest binding that
    works, a small host in the tests that implements those interfaces with no engine and that the
    section's steps are run against, a test that fails where the library references an engine, a
    window or a graphics API, the form in the Tools module's ReferenceTests.cs, and no engine's name
    in a public member or its summary. The two engines appear in the README as examples. A public
    shape that fits the two engines alone is named in a Reply before it changes. The Slang kernels
    are the part an engine in any language can take, so the README says how to compile them without
    the C# library.

8. **The package id and the assembly are 3DEngine.RadianceCascades and the namespace stays
    RadianceCascades (2026-11-18 17:28).** The prefix gives a unique name. A package is published
    when the time is right and on the owner's word, for a reader who takes the module alone. The
    engines keep taking the module as a git submodule, compiled with them and carried inside each
    engine's one package. Folders and project file names stay.

## Replies

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
