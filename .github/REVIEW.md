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
by the session and written here; item 10 follows the probe passes.

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

18. **Split radiance cascades, sparse probes with ray splitting.** The direction of 2026-11-26
    (Decision 10), after item 17's corridor reading, which is the baseline this design has to beat
    on the same number. The module's placement, store and trace path follow the published split
    method. Probes stand only where surfaces are seen, each visible point inserting its nearest base
    probe and each probe its nearest probe of the next cascade, persisted in a store keyed by
    position and level so a camera's move changes nothing a probe holds; levels by the base-two
    logarithm of the Chebyshev distance from the camera, each level's start shortened to 0.9 and
    blended over the overlap; rays start at the visible surface points and are split by hit distance
    into the cascades' intervals, deposited as radiance, transmittance and a count into the nearest
    probe and direction and averaged by count; branching 4 and length scaling 4, the base interval
    about 1.6 spacings, 32 directions at the base; directions from a low-discrepancy sequence by an
    equal-area mapping, jittered over time; the merge as before over the probes that exist with
    renormalized trilinear weights; the gather through the existing gatherSurface. Built on the CPU
    ladder first, every ladder scene and the corridor read under it beside the dense grid, then the
    door's mark of the engine's 304 read on it once. What a host hands changes, the visible points
    with depth and normal, the rays' hits and the store's memory, so the interface is written out as
    item 7 was before the engines rebind; the dense grid stays selectable until both engines read
    nearer on it. Several commits, each with its figures. Six commits as the session proposed on
    2026-11-26, taken. The host's interface written out as item 7 was, the visible points with depth
    and normal, the eye, the rays' hits and the store's memory, no behavior change; the sparse probe
    store on the CPU keyed by position and level and persisted, each visible point inserting its
    nearest base probe and each probe its nearest of the next cascade, levels by the base-two
    logarithm of the Chebyshev distance with the 0.9 start and the blend over the overlap in
    gatherSurface, a layout selector keeping the dense grid; rays from the surface points split by
    hit distance and deposited by count with transmittance, low-discrepancy equal-area directions
    jittered over time, 32 at the base, branching and length scaling 4, the base interval 1.6
    spacings; the merge over the probes that exist with renormalized trilinear weights; the ladder
    and the corridor read under it beside the dense grid, the test's bound moving to 2% when it
    holds; and the engine's door read once. First commit `b3862c6`, the interface, pushed at 07:11
    on 2026-11-26, its run awaited. rc_split.slang declares the hand that no kernel runs yet,
    IVisiblePoints with seenAt, IRayHits over a ray part and the field's march, IStoreUints with an
    atomic add and compare-exchange, IStoreLight, and the structs SplitFrame and SplitStore; a hand
    kernel over buffers compiles to SPIR-V, WGSL, HLSL and Metal with 32-bit atomics alone, the
    uints as atomic buffers since WGSL refuses an atomic add on a plain one. The Reply maps each
    declaration onto each engine's code; both lack the store and the hits, the engine lacks seenAt
    and hands frame modulo 16 alone, the bridge has no surface points over pixels. The ladder and
    the suite open no device and run without the lock; 218 tests. Second commit `1245c78`, the
    sparse store on the CPU, pushed at 07:39 on 2026-11-26, its run awaited. A key table of four
    times the slots to pools a cascade, a quarter fewer each cascade up, with free lists, four
    passes a frame, and the split gatherSurface with the level by the base-two logarithm of the
    Chebyshev distance, the 0.9 start and the blend over the overlap; ProbeLayout defaults to Dense,
    a test holding the store's shape to the Slang layout. The corridor's faces take 322, 108, 46 and
    28 probes a cascade against the dense grid's 2,048 at the finest, each within 0.866 of its
    spacing of what asked for it; over 16 eye steps 8,852 probes kept their entries, 368 were taken
    and none moved; a probe stands for its age and is freed the frame after; the gather lies on the
    levels' blend within 1e-6 and steps at most 0.050 between points 0.01 apart. Choices, a fresh
    entry's light reads as none from the frame it is taken, the gather keeps no sky share, a key
    keeps 9 bits an axis so a level reaches at most 128 spacings. 227 tests. Third commit `d2bd443`,
    the rays from the seen points split by interval and deposited by count, pushed at 08:04 on
    2026-11-26 after the run of `1245c78` went green, its run green. Directions by a low-discrepancy
    offset a point, one step a frame, equal-area on the normal's half, each height band within 0.004
    of its share over 4,096 frames and each finest way within 0.020; on the corridor one frame's
    20,997 rays from 18,985 points give 29,000 parts, none lacking a probe and the sums matching the
    whole-ray trace, 0.567 of the finest ways reached by frame 32. A point is held within 254
    coarsest spacings, 1,016 units at the defaults; past that it asks for nothing and the binding
    gives it the far light or raises the levels or the spacing. Choices for the merge, an unknown
    own way lets all light through, the average above renormalized over the known children and the
    standing corners, the coarsest cascade taking the sky, the gather skipping unknown merged ways.
    231 tests. Fourth commit `3c43c5e`, the merge over the probes that exist by renormalized
    trilinear weights, pushed at 08:24 on 2026-11-26 after the run of `d2bd443` went green, its run
    awaited. One choice changed on the closed room, where an unknown own way letting all light
    through leaked the sky, 4,442 of 6,528 finest ways more than 1% off after four frames; an
    unknown way merges to none and is left out below and in the gather, after which no known way is
    off and the 3,328 points gather 0.3 within 1e-6. The merge matches the CPU reading of the rule
    within 1e-6 over 105,024 ways of 504 probes, and under a sky of 1 with nothing to meet 18,985
    points gather 1. 234 tests. Fifth commit ruled, the corridor walk first as one more layout with
    rays through the field's hits, many seen faces and a fixed run of frames at each eye, the
    per-way average a running mean with the count capped a way where a tenth of a ray a frame is too
    noisy for the 2% bound; the ladder's split rows in the commit after. Fifth commit, first part,
    `5dec8d3`, the corridor walked on the split probes, pushed at 09:05 on 2026-11-26 after the run
    of `3c43c5e` went green, its run awaited. A cap of 256 rays a way with the count in the sums,
    256 warm frames and four at each quarter step; with the eye still the capped mean moves a point
    0.010 to 0.022 at the 99th part where a tenth of a ray moved it 0.156. The step at the engine's
    configuration 0.429 worst and 0.202 at the 99th part against the dense grid's 3.558 and 1.299,
    at the module's 0.319 and 0.164, the test holding that reading; the 2% bound does not hold yet.
    The cause is the level switch, two levels reading a point 0.665 to 1.948 of each other and the
    0.9 overlap narrower than a quarter step; more frames an eye did not help, a start of 0.5 gave
    0.213 and 0.102 at 1.8 times the ray time, keying probes by face nothing. Ruled, the 0.9 start
    stays as the published method has it and the bound stays 2% as the aim with the test at the
    reading; the next part reads why two levels read the same point up to twice apart, the coarser
    level's probes' places, their directions' coverage and the sky share each set against the trace
    at that point, and mends the cause rather than widening the blend; the ladder's split rows after
    that.

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

## Replies

Replies of the items done up to 17 stand in REVIEW-ARCHIVE.md under Replies archived.

Item 18, its first commit, in the commit carrying this line. The split cascades' hand is written out
in `rc_split.slang`, which no kernel runs yet, with a row of the README's table for each of its four
interfaces, `IVisiblePoints`, `IRayHits`, `IStoreUints` and `IStoreLight`, and a paragraph for its
two structs, `SplitFrame`, the eye and the frame's count, and `SplitStore`, the store's shape.
`split_hand_kernel.slang` among the tests' kernels gives every interface over buffers and compiles
to SPIR-V, WGSL, HLSL and Metal with no warning (`dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~SlangTests`, the Slang, README and source tests 67 passing in 32 seconds). The
uints take 32-bit atomics alone, held as `RWStructuredBuffer<Atomic<uint>>`, since a plain buffer's
uint taken by InterlockedAdd compiled to WGSL's atomicAdd on a plain u32, which WGSL does not allow,
and neither engine enables 64-bit or float atomics. No library behaviour changes. The ladder opens
no device, its kernels built by slangc for the CPU, so it and the suite run with no GPU lock from
this commit on (N 7.9).

Each engine's hand as it stands, read in place. For `IVisiblePoints`, 3DEngine's `WindowPoints`
(3DEngine/Shaders/gi_surface.slang:217) reads a pixel's place from the window's depth through the
unjittered inverse view projection (:228) and its normal from the depth's neighbors, a thread a
pixel at full size, and lacks only `seenAt`, which its gather's test for no depth gives.
BevyCSharp's one ISurfacePoints, `OneFace`
(BevyCSharp/GlobalIllumination/Kernels/gi_ambient.slang:213), is a probe's face, and its screen
probes read a tile's place from the prepass depth and its normal from the depth's neighbors
(gi_screen.slang:154), the prepass normals unread
(native/bevy_csharp/src/render/bcs_pass.slang:454), so it lacks the hand over its pixels. For
`SplitFrame`, 3DEngine's eye is `ScreenView.eye` (3DEngine/Shaders/screenmarch.slang:55) and its
frame only `_frame % 16` in `view.jitter.z`
(3DEngine/GlobalIllumination/GlobalIlluminationRenderer.cs:446), the whole count sitting in
`GiLights.counts.y`, which its gather does not bind. BevyCSharp's world passes take the eye as
`FieldInfo.eye` (native/bevy_csharp/src/render/bcs_field.slang:41) and the frame as
`GiLights.counts.y` (bcs_gi.slang:65). Each hands the whole count, since a turn of sixteen frames
repeats its ways.

For `IRayHits`, neither engine traces a probe's ray through the module's march. 3DEngine marches the
field and shades a hit in its own kernel (shadeProbeHitN, 3DEngine/Shaders/gi_trace.slang:523), and
has hardware rays (meshHit, 3DEngine/Shaders/meshrays.slang:43), so it gives the hits over either.
BevyCSharp marches its own `traceField` and shades by `shadeProbeHit`
(native/bevy_csharp/src/render/bcs_gi.slang:686), its hardware rays used only by its traced
reference, so it gives them over that march and that shading. `FieldHits` serves an engine whose
field shades a hit through IProbeField's hitLight, which neither engine's does, 3DEngine's
`UnreadField` and BevyCSharp's `NoField` giving none. For `IStoreUints` and `IStoreLight`, neither
engine has a store, its levels being 3D images a cascade
(3DEngine/GlobalIllumination/GlobalIlluminationStages.cs:195,
BevyCSharp/GlobalIllumination/WorldProbes.cs:318). 3DEngine takes a uint atomically by
InterlockedMin on a plain buffer (3DEngine/Shaders/field_splat.slang:195), which holds for its
SPIR-V alone (3DEngine/Graphics/Shaders/SlangCompiler.cs:39), and BevyCSharp through `Atomic<uint>`
(BevyCSharp/GlobalIllumination/Kernels/field_splat.slang:68), since it compiles to WGSL, which is
the form the hand takes for both.

What each engine changes to give the hand, a file each. In 3DEngine, gi_surface.slang gives `seenAt`
on WindowPoints, GlobalIlluminationRenderer.cs hands the eye and the whole frame count to the split
passes, gi_trace.slang gives `IRayHits` over its march or its hardware rays and shadeProbeHitN, and
GlobalIlluminationStages.cs makes the store's two buffers at the sizes the second commit gives and
dispatches the split passes beside the dense ones. In BevyCSharp, a struct over the prepass depth
and normals beside gi_screen.slang's reading gives `IVisiblePoints` a pixel, bcs_gi.slang gives
`IRayHits` over traceField and shadeProbeHit, bcs_field.slang or the lights' block hands the eye and
the frame's count, and WorldProbes.cs makes the two buffers and dispatches the split passes. Read
first for the second commit `rc_split.slang` and the README's paragraph beside the table.

Item 18, its second commit, in the commit carrying this line. `rc_split.slang` keeps the split
cascades' store on the CPU and lays it out in the engine's two buffers. The uints hold sixteen
counts, then a table four times the finest pool's slots from each probe's key to its entry, then a
pool for each cascade, the slots for the finest and a quarter as many for each above, each entry its
key and the frames it was taken and last asked for, then each cascade's free list. The light holds
each cascade's probes' ways in one span of 32 times the slots, their own light first and the merged
after. A key is the level, the cascade and the low nine bits of the cell along each axis, read back
against the eye's cell, so a level reaches at most 128 of its spacings. Each frame runs four passes,
`clearSplitTable`, then `keepSplitProbes`, which puts back each probe asked for within the store's
age and standing within its level's reach and frees the rest, then `askSeenProbes`, a seen point's
nearest probe of its level and of the next over the overlap, then `askCascadeProbes` once for each
cascade but the coarsest, the nearest probe above each probe asked for. A new `gatherSurface` over
the store picks a point's level by the base-two logarithm of its Chebyshev distance from the eye
over the finest reach, the next level starting at 0.9 of it and blended in over the overlap, and
weighs the eight probes about the point by their trilinear weights over those standing. A probe's
32 base ways, four times as many each cascade up, are the middles of a square's texels on each half
of the sphere by the concentric map lifted by equal area. `ProbeLayout`, Dense by default, and
`SplitStoreShape`, whose sizes a test holds to the Slang layout, are the C# side.

Read on the CPU (`dotnet test RadianceCascades.Tests --filter FullyQualifiedName~SplitStoreTests`,
six tests in 2.2 seconds, 227 in the suite). Over the corridor's inside faces from an eye in it, the
store holds 322, 108, 46 and 28 probes by cascade, against the 2,048 a dense grid of its four levels
stands at the finest, each within 0.866 of its spacing of what asked for it. Over 16 quarter steps
of the eye, 8,852 probes kept their entries and the frame they were taken, 368 were taken and none
moved. A probe unasked for stands for the store's age and is freed at the frame after, every pool
free again. Along a line out past every level's reach, each level's probes holding twice the light
of the one below, the gather lies on the levels' blend to within a millionth and steps at most 0.050
of itself between points a hundredth of a unit apart. Each base way holds four of the next
cascade's ways nearest it. Three things the third commit takes as given. A fresh entry's light is
read as none from the frame it is taken, the pool's third uint. The gather reads the merged light at
the 32 base ways by their cosines and keeps no share of the sky, and takes no field. A point whose
level's probes stand but not the next's takes its own level alone. Read first for the third commit
`askSplitProbe`, `splitLightAt` and `splitWay` in `rc_split.slang`.

Item 18, its third commit, in the commit carrying this line. Each seen point sends one ray a frame
(`traceSeenRays`), its way stepping along the plane's sequence of the plastic number from an offset
hashed from the point's index, one step a frame, laid on the half its normal faces by the
concentric map lifted by equal area, as the probes' ways are, and turned onto the normal. It starts
a quarter of its level's spacing off the surface and is traced through its level's cascades'
intervals one after another, the finest 1.6 spacings long and each above reaching four times as far
past the one below, so 0 to 1.6, 1.6 to 8, 8 to 33.6 and 33.6 to 136 spacings. Each part goes to
the probe of its cascade that the point's nearest probe of the level leads up to by the halving
`askCascadeProbes` asks by, so that probe stands, at the way it falls in (`splitWayOf`, the
concentric map taken back). A part adds its light, the share it lets through and a count of one to
five uints a way laid after the free lists, in 1024ths, so it takes atomics on uints alone. A part
that meets a surface lets none through, one that leaves the world adds the sky's light past what it
crossed, and the ray ends there. A point over the overlap of two levels traces the same way again by
the next level's intervals, two rays. `keepSplitLight`, a thread a way, takes the mean by count of
light and of share, wholly where the way held none and toward it by 1 − 0.9^count where it held
some, reads a probe taken this frame as holding none (w −1), keeps a way no ray reached, and empties
the sums.

What a binding does past a level's reach. Every level but the coarsest hands a point past its reach
to the next, so a scene exceeds the store only past the coarsest level, whose keys' nine bits a cell
along each axis hold 254 of its spacings from the eye before a cell reads back elsewhere. A point
past them is not held (`splitHolds`), asks for no probe, sends no ray and gathers 0, and the binding
gives that pixel its far light, the sky or a far field of its own, as it gives a pixel that sees the
sky. A binding whose scene runs farther raises `Levels`, up to 7, each doubling the hold, or the
spacing. At the defaults, a spacing of 0.25 and five levels, the hold is 1,016 units, and 4,064 at
seven.

Read on the CPU (`dotnet test RadianceCascades.Tests --filter FullyQualifiedName~SplitStoreTests`,
ten tests in 3.8 seconds, 231 in the suite). Over 4,096 frames a point's rays all fall in the half
its normal faces, each eighth of the half's height takes its share within 0.004 of it, their mean
lies 0.0005 from half the normal, and facing up each of the 16 finest ways of the half takes its
share within 0.020 of it. Each way of every cascade reads back as itself. In the corridor lit as the
merge's tests light it, one frame's 20,997 rays from 18,985 seen points add 29,000 parts to 8,189
ways, 20,997, 6,909, 855 and 239 reaching the cascades from the finest, every part finding its
probe and every sum's uint equal to what the same rays traced whole and split where they ended give.
Over two frames 10,777 ways took their rays' mean and 5,553 moved toward it, each within a millionth
of the rule, and over 32 frames the rays reached 0.567 of the finest cascade's ways, the rest facing
no seen point. A point at 250 of the coarsest spacings asks for its probe and one at 258 asks for
none and gathers 0. Three things the fourth commit takes as given. A way no ray reached yet holds
w −1 and no light. A part's interval is measured from the seen point that sent it, not from the
probe it goes to. The way of texel (x, y) of a cascade holds the four of texels (2x, 2y) to
(2x + 1, 2y + 1) of the same half above it. Read first for the fourth commit `keepSplitLight` and
`splitWayOf` in `rc_split.slang`.

Item 18, its fourth commit, in the commit carrying this line. `mergeSplitCascade`, a thread a way,
runs once for each cascade from the coarsest down after `keepSplitLight`. A way's merged light is
its own light and, by the share its own lets through, the light merged above it
(`splitAboveLight`). That light is the mean of the four ways of the cascade above that its texel
holds, at twice its texel along each side of the same half, at each of the eight probes of that
cascade about it that stand, weighed by their trilinear weights and taken over the corners standing
and the ways holding light. Above the coarsest it is the sky's along the way. The merged w is the
share let through to the sky. A probe's cell halved is its place in the next cascade's cells, so
each axis weighs one probe whole or two by halves, and the probe it leads up to is always among
them. The gather leaves out the base ways merged to none and takes each probe's weight over those
holding a way facing the point.

One of the four choices changed in the reading asked for. A way no ray reached was to let all
light through, and on the closed room that lets the sky in, which the room's test reads. The room is
8 by 4 by 6 and every wall sends 0.3. After four frames, letting such ways through puts 4,442 of the
finest cascade's 6,528 ways a hundredth off the walls' light, the brightest at the sky's 1.000. A
way whose rays all met a wall leaves the ways above it with no ray, and letting those through hands
the sky above the coarsest down the cascades. Such a way merges to none instead (w −1). The ways
below and the gather leave it out, and a way letting some through with none merged above it takes
none from above. With that, none of the 3,522 finest ways holding light lies a hundredth off, and
the room's 3,328 points gather the walls' light within a millionth.

Read on the CPU (`dotnet test RadianceCascades.Tests --filter FullyQualifiedName~SplitStoreTests`,
thirteen tests in 4.6 seconds, 234 in the suite). With each probe's own light set by a hash along
every way of the corridor's 504 probes, 21,121 of the 105,024 ways reached by no ray and 327 corners
above them not standing, the merge lies on the rule read in C# within a millionth. Under a sky of 1
with nothing to meet, the corridor's 18,985 points gather 1 within a millionth after four frames.
Three things the fifth commit takes as given. A frame runs `clearSplitTable`, `keepSplitProbes`,
`askSeenProbes`, `askCascadeProbes` from the finest up, `traceSeenRays`, `keepSplitLight`,
`mergeSplitCascade` from the coarsest down and `gatherSurface`, in that order. A point the store
does not hold, or whose probes hold no way facing it, gathers 0. A way's light settles over frames
by a tenth a ray, so a first frame holds the mean of one ray a point. Read first for the fifth
commit `mergeSplitCascade` and `splitAboveLight` in `rc_split.slang`, and `MergedBy` in
`SplitStoreTests.cs`.

Fifth commit's first part, the corridor walked on the split probes. `CorridorSplitWalk` in
`ProbeMergeTests.Split.cs` runs a frame's passes in `split_field_cpu.slang` over the corridor's
field, each ray traced through its march (`FieldHits`) and lit by the cell of the corridor's light
its hit lies in. The eye sees the corridor's inside faces within 16 units of it an eighth of a unit
apart, the store runs 256 frames at the first eye and 4 at each quarter step after, kept over the
walk at 4,096 slots, and the 125 fixed points are gathered after each eye's last frame and read by
`CorridorSwings` as the dense levels are.

The cap and the frames. A tenth a ray was too noisy for the bound. With every eye of the walk at the
corridor's middle at the module's configuration, it moved a point 0.385 of its mean at worst and
0.156 at the 99th part between readings a frame apart, and 0.466 and 0.268 four frames apart. A
way's light is the mean of its rays counted up to 256 (`SplitKeptRays`), past which each ray moves
it by a 256th, the count kept above the frame's rays in the sums' fifth uint, the frame's in its low
20 bits (`SplitCountBits`), and a probe taken this frame counts from none. Held still in the same
way, the capped mean moves a point 0.015 and 0.010 a frame apart and 0.036 and 0.022 four apart.
Those four readings came from a variant of the walk whose eyes all stood at the middle, not
committed. The test reads the still swing at the walk's first eye over 8 readings 4 frames apart,
0.082 and 0.068 at an engine's configuration and 0.061 and 0.051 at the module's, that eye standing
at the corridor's end so more of the points lie on the coarse levels, each way there the mean of a
few tens of rays short of the cap.

The walk. At an engine's configuration a point steps 0.429 of its mean at worst and 0.202 at the
99th part, against the dense levels' 3.558 and 1.299, and at the module's 0.319 and 0.164 (`dotnet
test RadianceCascades.Tests --filter FullyQualifiedName~On_The_Split_Probes`, 2.1 minutes, 238 tests
in the suite). The 2% bound does not hold, and the test holds the step at its reading, under 0.45
and 0.21 at an engine's and 0.33 and 0.17 at the module's. Every step above 0.12 at the module's
falls where the point's greatest distance from the eye crosses 2 or 4, a level's reach. The overlap,
the last tenth of a reach, is narrower than a quarter step below the fourth level, so the gather
steps from one level to the next whole. Where both stand about a point, the next level reads it
0.665 to 1.948 of its own from the 5th part to the 95th at an engine's and 0.713 to 1.467 at the
module's, since a coarser level's probe stands for the rays of a patch 2 or 4 units across about a
lamp 1.5 across and a gap 2 wide.

What was tried, each by a variant of the walk not committed. More frames at each eye leave the step
as it was, 16 and 64 giving 0.161 and 0.196 at the 99th part at the module's. An overlap over the
outer half of each level's reach (`SplitLevelStart` 0.5) brings it to 0.213 and 0.102 at an engine's
and 0.092 and 0.061 at the module's, at about 1.8 times the rays' time on the CPU since every point
over it traces twice. The commit keeps the ruled 0.9. A probe key carrying the face a point's normal
lies along, six faces with eight bits of a cell along each axis, left the levels as far apart, the
fourth reading the floor 1.18 to 1.29 of the third at the median, and was taken out.

Asked. Whether the overlap goes to 0.5, for half the step at an engine's configuration and under two
fifths of it at the module's at twice the rays over the outer half of each level. Whether item 18's
bound stays at 2%, which needs the levels to read nearer than a blend brings them, or the step's
reading becomes the bound. `splitLevelLight` is public so the test reads each level's light at a
point. The ladder's split rows come in the next commit. Read first for it `CorridorSplitWalk` in
`ProbeMergeTests.Split.cs` and `split_field_cpu.slang`.

Item 18, its sixth commit, in the commit carrying this line. The reading is at the wall's point at
the lamp's edge, 2.5 high, with the eye 7.5 from it along the corridor, where the walk read its two
levels most apart, the levels of spacing 2 and 4 meeting there. Every way facing the point held
rays at both levels, and the sky gave none of its traced light at an engine's configuration and
0.089 of it at the module's, whose field ends 8 from the corridor's middle. Each level's rays
started a quarter of that level's spacing off the surface, so the coarser's rays of a point over
the overlap started a unit off the wall where the finer's started half a unit off. At the module's
the finer read 1.329 of the trace and the coarser 0.577, the coarser 0.434 of the finer. Fed by the
point's own rays alone they read 1.034 and 0.849. With the sky out the coarser read 0.335 of the
finer, with the lamp out 1.437, and with the floor under the gap out 0.436. The finer's over-read
came from the faces feeding its probes, the wall's top faces starting half a unit off into the
roof's lamp cells, and the coarser's under-read in part from its own start and in part from its
probes, which spread 4 to 8 units about a lamp 1.5 across. These came from a variant of the reading
with each level's own start, not committed.

The mend. A seen point's rays start a quarter of the finest level's spacing off the surface for
each of the finest level's reaches the point lies from the eye (`splitRayOrigin`), so the start
grows with the distance as the spacings do and both levels' rays of a point over the overlap start
at one place. At the point the finer reads 1.016 of the trace at an engine's configuration and
0.988 at the module's, the coarser 0.515 and 0.611, and fed by its own rays alone the two read
0.983 and 0.988 at an engine's and 0.983 and 1.006 at the module's (`dotnet test
RadianceCascades.Tests --filter FullyQualifiedName~At_The_Lamps_Edge`). The test holds the own
rays' readings within a twentieth of the trace and of each other, the finer's from every face
within a twentieth, and the coarser's above half.

The walk. A point steps 0.429 at worst and 0.207 at the 99th part at an engine's configuration and
0.330 and 0.179 at the module's, and with the eye still 0.088 and 0.081 and 0.077 and 0.059, each a
little above the reading under each level's own start, since a point near a level's inner end
starts lower. The test holds them under 0.45, 0.22, 0.095 and 0.085 at an engine's and 0.35, 0.19,
0.085 and 0.065 at the module's. The levels read 0.706 to 2.195 of each other from the 5th part to
the 95th at an engine's and 0.743 to 1.570 at the module's (`dotnet test RadianceCascades.Tests
--filter FullyQualifiedName~On_The_Split_Probes`). The points they read most apart are the floor's
under the gap at an engine's, the coarser reading 2.479 of the finer, and the floor's near the
field's end at the module's, 1.748 (`RC_SPLIT_APART=1 dotnet test RadianceCascades.Tests --filter
FullyQualifiedName~At_The_Lamps_Edge`). At the floor's point under the gap the coarser's probe is
fed by the floor's and both walls' faces over 2.4 units, and the point's own rays alone read 0.897
and 1.054 of the trace, so what is left is the coarser probes' footprint.

Asked. Whether the levels take eight probes a reach, `Reach` 2 at a spacing of a quarter, at four
times the slots, 16,384. A variant of the walk not committed read the levels 0.727 to 1.173 of each
other from the 5th part to the 95th at an engine's configuration, the step 0.522 at worst and 0.175
at the 99th part, and the still swing 0.040 and 0.030. The ladder's split rows follow this commit.
