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
change daily until then. By the direction of 2026-11-25 the cascades come before everything else in
both engines, and this module's next item is the gather's shared step the bridge waits on, proposed
by the session and written here; item 10 follows the probe passes. On 2026-11-27 the order after
19's first change is 20, 21, 22, 23 and 24, then the lintel's carry-over and item 10.

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

12. **What carries the high wall's surplus in the arcade court.** Every merge on the ladder reads
    the court's high wall at 2.37 to 2.73 of the trace at a host's configuration, so the surplus
    lies outside the hand-off, in a scene made to hold a host's case. This item finds its source on
    the CPU by taking one thing out at a time, as the host's readings did. Read the high wall with
    the far cascades' light set to zero (the near interval alone), with the sky share set to zero,
    with each cascade's rays a probe doubled, with the probe spacing halved, and with the probes
    that stand inside or behind the roof left out, and give the wall's reading for each beside the
    trace. Follow one probe at the high wall through its texels as the parity replay does and name
    the texels that carry the surplus and where their rays end. The Reply names the source in one
    reading or says it is suspected, says whether the same source is in the module's configuration
    (the high wall there), and proposes the mend with its cost. No mend is made in this item unless
    it is a fault in a kernel, which is mended with a test.

19. **The finest probes from the view, kept; the coarse levels as fixed world grids.** The direction
    of 2026-11-26 at 16:3x (Decision 11), after item 18's fallback for a point no probe sees and
    before the lintel's carry-over and the single trace. Two changes to the split store. A probe,
    once placed from the view, is kept until memory presses rather than freed the frame after it is
    last asked for, with the store's memory read and a rule for what goes first when it fills, so a
    turn of the eye finds the probes it left behind still standing; and the levels above the finest
    are plain world-space grids that never move with the eye, the finest level alone placed from the
    visible surfaces, so a fixed point's level is set by where it stands and never changes as the
    eye comes near, in place of the level by distance from the eye. Read on the corridor walk
    against today's 0.360 and 0.609 worst step and on the whole ladder, the store's memory beside;
    the walk's bound moves toward 2% with it. One or two commits, the hand's change said if any.
    Ruled on the second change at 16:4x, the level by distance dropped outright. Every surface point
    asks for and gathers from cascade-0 probes at the world's 0.25 spacing placed from its surface
    and kept, and the levels above are further cascades on fixed world cells of 0.25 times two to
    the level; far surfaces take as many probes as near ones, their rays a frame fewer a probe and
    filled over frames by the keeping, with the probe count, the rays a probe and the memory read on
    the corridor and the ladder. On 2026-11-27 at 19:01 both changes are recorded at 10c530a with
    item 20, one commit because the keeping's age bins and the fixed grids' keys share the clear and
    the entry layout, 241 tests green, pushed, its run read next tick. The hand changes. SplitStore
    loses levels, reach and age and defaults to 6 cascades; clearSplitTable takes the frame; an
    entry is 8 uints; splitLevelLight becomes splitFinestLight; splitLevelOf and splitLevelAt go.
    Last eye 7,665, 1,740, 474, 114, 32 and 8 probes by cascade, 6,129 asked, 4.1 rays a probe; 78.6
    MB at 8,192 slots and 157.3 MB at the default. Review, the key bounds the store at 63.5 units
    from the eye at 0.25 and the corridor at the engine's configuration asks 36 and 20 probes of its
    coarsest pools, which hold 32 and 8, so the two coarsest pools are read for fullness at the
    engine's reach under item 21 or a sixth change to this item, the pool sizes set from the reach
    and the spacing rather than fixed. The sixth change, the pools, is in a99aae0 with item 21. The
    first cascade holds the slots and each cascade above the square of the root of the slots over
    two to its level plus two, 8,192, 2,304, 625, 196, 64 and 25 against the corridor's asks of
    1,740, 474, 114, 36 and 20; memory 130.7 MB at 8,192 slots and 216.3 MB at the default, the walk
    leaving no probe without an entry. Two scenes still ask past their pools, the court for 410
    finest probes and the lintel for 2,212 second- and 83 third-cascade probes, the lintel 0.339
    against the quarter pools' 0.319, carried into the lintel's item.

23. **Fresh probes take more rays.** A pixel whose finest probe was placed within its first frames
    sends four rays rather than one until the probe's count reaches the others', so a surface coming
    into view fills in within those frames rather than popping in; read as the frames to settle
    after a turn of the eye on the corridor, with the rays a frame beside. One commit.

24. **A probe drops its history when its fresh deposits disagree.** The direction's rule for history
    (the engine's Decision 123) in world space. A probe whose frame's deposit differs from its kept
    mean in a direction by more than the noise restarts that direction from the frame, so a lamp
    switched or a sun moved shows in the next frame and nothing lags; the threshold read on a ladder
    scene with a lamp switching and the sun stepping, the frames after the change counted, not
    guessed. One commit. The lintel's carry-over, which the published method does not solve, is read
    after these with the per-direction agreement as its lever.

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

Decisions 1 to 5 stand in REVIEW-ARCHIVE.md under Decisions; the last five follow.

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

9. **The radiance cascades before everything else, until they work.** The direction of 2026-11-25,
    given after the machine's restart. This module's next item is the gather's shared step the
    bridge's item 157 waits on, proposed by the session in a few lines and written here, with what
    each engine hands it and the test that holds it; item 10 follows the engine's probe passes. The
    engine runs 313, 304, 316 and 311, the bridge 162, 150 and 157; the other modules and the game
    stay idle until the cascades work.

10. **The module adopts split radiance cascades.** The direction of 2026-11-26, on the published
    sparse method and its demonstration. After item 17's corridor reading the module replaces its
    dense grid about the eye with sparse persisted probes near the visible surfaces and rays split
    by hit distance from the surface points, item 18, measured on the corridor's number and the
    engine's door before the engines rebind; the dense grid's remaining misses are not chased
    further, and the screen cascades stay as they are on top of whichever world probes exist.

11. **The finest probes from the view and kept, the coarse levels fixed in the world.** The
    direction of 2026-11-26, item 19. Probes placed from the view are cached in world space and kept
    until memory presses, and the levels above the finest are fixed world grids rather than levels
    by distance from the eye, so nothing a fixed point reads changes with the eye's place; the
    finest level alone follows the view onto the surfaces.

## Replies

Replies of the items done up to 18 stand in REVIEW-ARCHIVE.md under Replies archived.

Items 19 and 20, the finest probes from the view kept and the cascades above on fixed world grids
that feed the merge alone, in the commit carrying this line, one commit for the two since the
keeping's count of ages and the fixed grids' keys share the clear and the entry's layout. Every seen
point asks for and gathers from the finest cascade's probes at the store's spacing, 0.25 by default,
on a grid fixed in the world, and each cascade above stands on a fixed grid of twice the spacing of
the one below and is never gathered from. The level by distance from the eye goes, and with it the
overlap's blend and its second trace. A probe is kept until its pool presses. `clearSplitTable`
counts each probe in its cascade's bin of how many frames ago it was last asked for, a bin a frame
for the first 16 and one for each doubling past them, and `keepSplitProbes` keeps the bins from the
newest while they leave the pool room for twice the probes taken in it the frame before and for a
sixteenth of its entries, at most half, so those asked for longest ago go first and every probe
asked for the frame before stays. A probe's key holds its cascade and nine bits of its cell along
each axis and its entry keeps the whole cell, so a probe whose key would read back about the eye as
another cell is freed. The pool's test holds 200 points' probes through 200 frames no point asks for
them, and as eleven groups more ask in turn the groups standing are the latest, each whole, and a
fixed point's gathered light reads the same within a millionth from three eyes along a line of
points (`dotnet test RadianceCascades.Tests --filter FullyQualifiedName~SplitStoreTests`).

The hand's change. `SplitStore` keeps the slots, the spacing and the cascades and loses the levels,
the reach and the age, its default 6 cascades. `clearSplitTable` takes the `SplitFrame`. An entry is
8 uints, `SplitEntryUints`, and the counts 32, four a cascade for up to 8, the ages' counts of both
parities after them, `SplitAgeBins` a cascade, all of which a binding sizing its buffers by
`SplitStoreShape.Uints` and `Lights` takes without change. `SplitStoreShape.Bytes` gives the memory,
157.3 MB at the default of 16,384 slots and 6 cascades, where 5 levels of 4 cascades took 104.9 MB,
and 78.6 MB at the walk's 8,192 (`dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~SplitStoreTests`). `splitLevelLight` becomes `splitFinestLight`, and
`splitLevelOf` and `splitLevelAt` go.

The whole walk steps 0.076 at worst and 0.005 at the 99th part at an engine's configuration and
0.007 and 0.004 at the module's, where it stepped 0.609 and 0.249 and 0.360 and 0.220, the eye still
between readings 0.011 and 0.006 and 0.007 and 0.005, and the short walk 0.031 and 0.018 and 0.027
and 0.013 (`RC_LADDER=.github/LADDER.md dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~On_The_Split_Probes`). At the last eye the store stands 7,665, 1,740, 474, 114,
32 and 8 probes by cascade at an engine's configuration, 6,129 of the finest asked for at its last
frame and the others kept from the eyes before, those asked for taking 4.1 rays a frame each at the
mean, and 3,009, 756, 222, 48, 16 and 8 at the module's, all asked for. By a reading not committed,
the engine's worst step falls at the wall's points half a unit high from 4 to 6 along the corridor
as the eye crosses -3.25, and with the field held still about the corridor's middle the walk steps
0.015 and 0.005, and with the seen faces held as well 0.010 and 0.005, so what is left past the
rays' noise is the engine's field snapping about the eye. The wall's point at the lamp's edge reads
1.050 and 1.089 of its trace fed by every face and 1.011 and 1.020 fed by its own rays, where its
own level read 1.197 and 1.127 and 0.983 and 0.983 (`dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~At_The_Lamps_Edge`).

The split ladder reads 0.127 from the trace on the mean and 0.430 at worst at the module's
configuration and 0.125 and 0.421 at an engine's, where it read 0.107 and 0.459 and 0.108 and 0.459,
the leak 0 and the held out room 0.018 and 0.062 and 0.027 and 0.087. By scene at the module's, the
court falls from 0.109 to 0.052 and the room with a door from 0.020 to 0.012, the lintel rises from
0.316 to 0.341 and the lamp's closed box from 0.397 to 0.418, and the vestibule rises from 0.047 to
0.132 and the long hall from 0.040 to 0.083, every part of either above its trace
(`RC_LADDER=.github/LADDER.md dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~The_Ladder`). A scene's points stand about one to a finest probe, 1.0 to 1.1 rays
a probe a frame, so they ask for nearly as many probes of the second cascade as of the finest, and
the pools, each a quarter of the one below, fill in the court, its finest and second, and under the
lintel, its second and third. By a reading not committed, a store of 32,768 slots that holds every
probe asked for reads the court 0.052, the vestibule 0.132 and the long hall 0.083 as before and the
lintel 0.368, its second cascade standing 4,516 probes beside the finest's 4,532, so the rise is not
the pools'. Under the levels by distance a far point's rays went to its own level's probes, and on
the fixed grids every point's rays share the coarse probes, so the likeliest reading is the lintel's
carry-over over a wider span, a probe above that a vestibule's or a hall's probe sees holding the
far parts of the rays of the court's or the opening's points. The lintel's item after 21 to 24 is
where it is mended.

Three things for the reviewing session's word. The key's nine bits a cell along each axis bound the
store at 254 of the finest spacings from the eye, 63.5 at the default 0.25, where the levels by
distance held 254 of the coarsest level's, 1,016, and a point past it asks for nothing and gathers
nothing, so a binding with a far view needs a coarser spacing or a wider key, a third uint a slot of
the table. The pools are each a quarter of the one below, as the probes of a surface the eye's
pixels cover are, and by a reading not committed the corridor at an engine's configuration asks 36
and 20 probes of its two coarsest pools, which hold 32 and 8, so some probes below them take no
light from past their own interval. The default store takes 157.3 MB, each cascade's ways and sums
the same share as the finest's.

Item 23, fresh probes take more rays. A seen point whose probe of the finest cascade was taken
within the last `SplitFreshFrames`, 16, sends `SplitFreshRays`, 4, rays a frame rather than one,
each further ray of a frame taking the step of its sequence a quarter of their range past the last,
which lays the four on a lattice over the square. The rule reads a probe's age from the frame taken
its entry keeps rather than from its count, since the counts sit a way and not a probe, and 16
frames at 4 rays give a point 64 rays before it falls to one. Turned at the corridor's middle from
the faces toward -x to those toward +x, the 6,016 points newly seen come within a tenth of their
light 160 frames on at the 95th part by the 11th frame at the module's configuration and the 12th at
an engine's, where at one ray a point they are still past it 24 frames on, 34 and 51 read under a
fifth of it at the first frame against 357 and 538, and their mean distance at frames 1, 4 and 16 is
0.243, 0.109 and 0.024 against 0.651, 0.224 and 0.061 at the module's and 0.306, 0.128 and 0.028
against 0.778, 0.277 and 0.078 at an engine's, the 6,332 points seen sending 25,091 rays at the
first frame and 6,332 at the 24th (`dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~A_Surface_Turned_Into_View`). By a reading not committed on the walk's 60 points
past the turn, 16 frames bring the 99th part within a tenth by the 9th frame at the module's and the
11th at an engine's against the 11th and the 25th at 8, for 502,768 rays over 32 frames against
352,696, and 202,624 at one ray a point.

The ladder reads the split store 0.124 from its trace on the mean and 0.407 at worst at the module's
configuration and 0.125 and 0.415 at an engine's, against 0.121 and 0.402 at both, the dark room
taking none of the lit room's light and the lintel's room reading 0.334 against 0.340, and the moves
lie in the bright scenes, the closed box with one lamp 0.383 and 0.393 on the mean against 0.371 and
0.373, the wall under a lamp 0.108 and 0.105 against 0.093 and 0.091, and the court 0.058 and 0.058
against 0.048 and 0.057 (`RC_LADDER=.github/LADDER.md dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~The_Ladder`). By a reading not committed they come of the count of rays and not
of the rule. At the module's one ray a point over 112 frames, the rays 16 fresh frames give over 64,
reads the closed box 0.404, the court 0.053 and the wall 0.102, and over 256 frames 0.390, 0.056 and
0.104, against 0.394, 0.058 and 0.109 with 16 fresh frames over 256, so at 64 frames of one ray the
store read those scenes nearer their trace than it reads them at more rays, and at 4, 8 and 24 fresh
frames over 64 the wall reads 0.100, 0.105 and 0.116. The walk steps 0.008 and 0.004 at an engine's
and 0.006 and 0.004 at the module's, with the eye still 0.007 and 0.005 at an engine's and 0.005 and
0.004 at the module's, each finest probe asked for at the last eye taking 4.5 rays a frame at an
engine's, and at the lamp's edge the probes fed by every face read 1.042 and 1.091 of the trace and
by their own rays alone 0.962 and 0.981, against 1.044 and 1.096 and 0.971 and 0.983
(`RC_LADDER=.github/LADDER.md dotnet test RadianceCascades.Tests --filter
'FullyQualifiedName~On_The_Split_Probes|FullyQualifiedName~At_The_Lamps_Edge'`). The specks are the
engine's normals, as the reviewing session read, and no kernel reading a pixel's eight probes is
added.
