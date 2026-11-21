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
the module's own kernels by their SPIR-V. A hand-off near an opening's edge reads the room behind it
about 1.4 times the fine trace, which the README states and a test holds, and a rule marching on
where far probes disagree mends it but reads a four-cascade open box's floor 8 percent under, its
figures in its Reply. A field's build runs `seamSheet` after the pinch. Next in order: the engines'
further findings as they take the passes, then the split sum at a hit, the screen probes, the light
on the picture and the contact shadow's march. Left behind each part's Reply: the field's normal,
the cascades' table, the march held within a cascade, the splat's second plane, the lending of thin
emitters, the repaint and the clearing of codes. Open: AGENTS.md's documents rows for DEVLOG.md and
CHEATSHEET.md wait for the owner. Read first: AGENTS.md, this file's head and Decisions, the Replies
from the field's distance on, the README's binding section, NORM.md's N 4.8 to 4.11 and N 7.9,
`CpuKernel.cs` and the head of `rc_probe_passes.slang`. Every build and test goes through N 7.9's
lock, restored outside it, built with `--no-restore -p:UseSharedCompilation=false`, its output
written to a file of which the summary line and the failing lines alone are read, and each commit is
read by `commitcheck.py . --staged --message=<file>` first. At a binding's request, in the commit
carrying this line: `build/fetch-slang.sh` reads an archive's sha256 with `shasum -a 256`, on macOS,
Linux and Git's bash on Windows alike, and Python's hashlib where a system has none, in place of
`sha256sum`, which N 6.6's check refuses. This repository's own tests do not run that check on its
scripts; the binding's suite found it.
