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

8. **A second merge, by line of sight and agreement, and a ladder the merges climb.** The module's
   merge lets a near probe's rays run past their interval and blends the farther cascade's probes by
   place. A host's own passes merge another way and read a room with a door nearer the traced
   reference on its walls, so that way is written into the module as a second merge a host selects,
   with the first unchanged. A near probe takes the farther cascade's probes about its own place,
   leaves out each one it has no clear line to through the distance field, leaves out each whose hit
   along the way disagrees with the occluder the near probe's own way finds, weighs the rest by
   place, and falls back to its own ray where none is left. The kernel is written once beside the
   first, with the same inputs and outputs, a setting naming which merge runs, and the pass graph
   the same for both. The source to read is the host's own merge shader, read in place and written
   anew to this module's form, its path given by the review. The module's own tests hold for both
   merges, each cascade's sum against a closed room's known light, no light through a wall of one
   cell, and a probe behind a lintel. The CPU reference's table in the README gains a row for each
   merge. The merges are kept as a ladder and not as a pair. A merge is one kernel behind one shape
   with a name, a host selects it by that name, and adding one touches no pass and no other merge. A
   ladder program in the module runs every merge over a set of scenes on the CPU reference with no
   graphics device, at least six, among them a room with a door, a closed box with one lamp, a lit
   room beside a dark one through a wall of one cell, a lintel, an open box under a sky and a long
   hall, and one more scene held out that no merge is tuned on. For each merge and scene it reads
   the parts' ratios to the traced answer and writes a table, the mean and the worst of the parts'
   distance from one as a logarithm, the light through the thin wall, and the count of rays and
   probe reads as the cost. A merge stands above another where it is nearer in the mean and no worse
   in the worst part, the leak and the cost, and the table names the one on top, which is the
   default. The table is kept in the README and a file of results with the date and the commit, so
   each new merge is read against every earlier one, and a merge that every other beats on every
   scene is deleted after the hosts have confirmed the order in their own target scenes on a device.
   This goes in one commit with the two merges as the ladder's first rungs. No host is named in the
   module's files.

9. **The ladder holds the bounce.** The ladder lights a hit with a fixed light and reads one gather,
    where a host lights each hit from its own probes frame over frame, so a merge that reads a
    little high or low at one gather is carried round the loop and grows or dies with every bounce.
    At a host's configuration the ladder reads a room's walls near 1.07 where the host's device
    reads 2.05 with the same merge, and the two part at the hit's light. The ladder gains that loop.
    Each merge is run to a steady state, a hit taking its light from the probes of the round before,
    with no history beyond that one round, and the traced answer it is read against follows light
    through as many bounces until it changes by under a hundredth. For each merge and scene the
    table gains the ratio after one gather beside the ratio at the steady state, the count of rounds
    to reach it, and the gain a round, the factor one bounce multiplies an error by. The standing is
    taken at the steady state. The first reading says whether the vestibule's walls then read high
    on the CPU with the merge at the ray's end, and which merge holds the loop nearest one. Where
    the CPU still reads near one, the Reply names the next place the device parts from it, and a
    host's session is asked for one probe's light after each of its first frames to set beside the
    CPU's rounds.

10. **Rough reflections read from the cascades.** The next large step after the probe passes is
    reflection, and its rough half is tried first here, whole each frame with no history (the
    lighting's rule stays). A rough surface reflects a cone about the mirror direction, and a cone
    of light by direction is the thing a cascade stores, so a kernel reads it and traces nothing.
    The kernel takes a point, a normal, a view direction and a roughness and gives the reflected
    light, choosing by the cone's width the cascade whose rays are as wide and reading it across the
    cone at the point's probes, with the same sight weighting the gather uses so a wall hides what
    lies behind it. It is written once beside the merges and behind a shape of its own, so a second
    way of reading the cone can stand next to the first and be compared as the merges are. The
    reading is on the CPU against a traced reference that samples the same cone with many rays, on
    the ladder's scenes plus one with a small bright light and one with a colored wall beside a gray
    floor, at roughness 0.1, 0.2, 0.3, 0.5, 0.7 and 1.0. It gives the mean and worst difference by
    roughness, the leak through the thin wall, the cost in reads a point, and above all the
    roughness below which the cascades' directions are too few and the reading parts from the
    reference by more than 0.10, since a traced ray takes over below that mark in a later item. A
    sheet shows a row of spheres or a floor strip by roughness, the kernel beside the reference with
    a difference panel. The Reply says the mark, what sets it (rays a probe, probe spacing, or the
    far cascades' sky share) and what a host needs to hand the kernel. No host is named and nothing
    is taken from one.

11. **A merge that blends about its own probe, and a ladder scene that ranks as a host's device
    does.** A host read three ways of lighting a ray past its interval in a real scene, a sunlit
    floor under a roof's edge beside shaded walls, with the bounce at zero and settled. The merge at
    the ray's end read 0.739 and 1.382 of the reference on a wall where the host's own merge reads
    0.524 and 1.064, and the wall's upper parts carried the surplus. The host's merge differs in
    where and how it reads the far probes, and with its march test off it still holds those upper
    parts, so the place and the read carry it and not the test. The ladder ranked these the other
    way round on its own scenes, so it does not yet hold the case that decides. Two things follow.
    First a merge behind the shape, in the form the host asked for. `farLightAbout` takes the near
    probe's place and blends the far probes about that place and not about the ray's end, and reads
    each far probe along the way from it toward the point of the near ray nearest that far probe's
    reach. The two go together, the blend about the probe without that read fell to 0.271 and 0.615.
    The sight test stays a separate choice on top. Second a ladder scene that holds the case, a
    floor lit by the sun through an opening with the shadow line of an edge across it, walls in
    shade lit by that floor alone, and a low part and a high part of the wall read apart, at the
    host's configuration and the module's, with the bounce at zero and settled. The pass mark for
    the scene is that it ranks the merge at the ray's end above the reference on the wall's high
    part as the device did. The Reply gives the ladder's standing with the fourth merge and the new
    scene, says whether the merge at the ray's end stays the default, and names what a host hands
    the new merge that it does not hand today.

## Verdicts

Verdict 1 is settled at 22:56 on 2026-11-21, the build run on `2f0dc8e` green on the three systems,
the Windows job 2 minutes with the CPU kernels built at full optimization.

1. **The Windows job runs the tests past its 30 minutes.** In run 44943704829 on `d446283`, read at
    21:57 on 2026-11-21, `test (windows-latest)` built in 18 seconds, began the tests and was
    stopped at its time limit 30 minutes after its start with no test's result printed, while Linux
    and macOS passed in 5 and 4 minutes. The job's time has grown over the day's pushes, 16 minutes
    on `41f0e09`, 28 on `0f0ff99`, past 30 here, so the tests added since cost Windows far more than
    the others. Run the suite on Windows by its log (the trx of an earlier green run names each
    test's time) or time the newest tests one by one, name which take minutes there and why (the
    fine traces of the closed room and the vestibule's kind, a kernel's compile, threads), and mend
    the cost, a coarser trace where the bound allows or the heavy cases behind one shared fixture. A
    larger limit is no mend. Settled when a run passes on the three systems with the Windows job's
    minutes stated.

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
