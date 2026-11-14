# Review

Reviewed up to `040353c`, item 7, the interface written out, the ten things an engine gives a kernel
as declarations with each field's name in each engine and both forms shown where they differ, a
section of .github/KERNELS.md, pushed at 02:29 on 2026-11-14; it keeps the form, one sentence of it
naming the engines' items by number, to be mended. Before it `13d21db`, item 6, what holds each
kernel today, 262 tests of the two engines tabled a pair with their devices and gaps, the rooms both
draw alike and the order of the move, a section of .github/KERNELS.md, pushed at 02:15 on
2026-11-14; it keeps the form. Before it `7806020`, item 5, the two engines' radiance cascades read
side by side in .github/KERNELS.md, each kernel paired with its like, where each pair differs, what
stays an engine's own and the interface of ten things an engine gives a kernel, a document alone
that starts no run, pushed at 00:10 on 2026-11-14; it keeps the form. Before it `8cc387d`, item 2,
pushed at 12:31 on 2026-10-31 with `8811a8e`, item 1, after the owner's first commit, its run green
at 12:35 on the three systems in about three minutes, the first of this repository. Both commits
keep the form, the three-mark subject, one sentence, the owner's identity as author and committer,
no trailer (N 7.2 and N 7.6 kept at the first commit); the library references no engine (N 2.1). The
documents are the engines' with the engine's name replaced and the engine-only lines dropped,
COMMITS.md the game's with N 7.1's pointer rule in place of the legacy branch line, .gitignore with
the dump folder, .claude and .ref; AGENTS.md's devlog row carries no link until DEVLOG.md exists and
its table of areas gains the rows item 2 adds. The skeleton sets ImportDirectoryBuildTargets false,
since a submodule under modules/ would inherit an engine's Directory.Build.targets, makes a missing
summary an error, loads the assembly by name in its one test, and build/version.sh prints 0.1.0; the
strict build 0 warnings and 0 errors, the test passing. After it `ff4bfe3`, the norm and the
review's head alone, pushed at 14:51 on 2026-11-03, with no run since the build passes over the
review's files; it keeps the form. Before it `cbc0d97`, the owner's first commit alone.

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

## Replies
