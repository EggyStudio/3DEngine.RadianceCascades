# Kernels

The radiance cascades as 3DEngine and BevyCSharp each have them, read from the code alone ahead of
the move of item 3 of [REVIEW.md](REVIEW.md), kernel by kernel in the order a frame runs them, each
paired with its like in the other engine. 3DEngine is read at `3768c988` and BevyCSharp at
`36840712`, both of 2026-11-13, and every line number is at those commits. Nothing of either engine
was built or run for this document, so each difference it states comes from the code and not from a
measurement.

Paths are relative to each repository's root, shortened as the table says. A reference stands in its
engine's column or after its engine's name, so `GlobalIllumination/` is read in the repository the
column names, and a line alone, as `:424`, is in the file of the same engine named last.

| Written | 3DEngine | BevyCSharp |
|---|---|---|
| `Shaders/` | `3DEngine/Shaders/` | |
| `GlobalIllumination/` | `3DEngine/GlobalIllumination/` | `BevyCSharp/GlobalIllumination/` |
| `Rendering/` | `3DEngine/Rendering/` | |
| `Kernels/` | | `BevyCSharp/GlobalIllumination/Kernels/` |
| `render/` | | `native/bevy_csharp/src/render/` |

The two copies have one source. BevyCSharp's commits that brought each part over say so in their
bodies, the far cascade at `0f0be8f0` and the screen probes at `f0ae14ec` on 2026-10-24 and the
carried rays at `98dad7f7` on 2026-11-04, and much of the code is the same line for line. Where the
two differ, the history usually tells which moved after the copy, and the document says so with the
commit and its date.

## The frame

### 3DEngine

The render graph runs the field's node, `scene_field`, before the window's depth, its ambient
occlusion and `global_illumination`, and the model pass in `hdr_scene` reads all of them
(`Rendering/Renderer.cs:143-193`, `GlobalIllumination/GlobalIlluminationPlugin.cs:71-76`). The field
is drawn by `Rendering/Lighting/SceneField.cs:222` and the probes by
`GlobalIllumination/GlobalIlluminationRenderer.cs:134`, each on its own setting
(`SceneField.cs:228`, `Rendering/Lighting/Illumination.cs:110-114`).

1. The field's info, the cascades' corners and cells against the frame's origin, written every frame
   (`SceneField.cs:312`).
2. `field_counted` (`Shaders/field_counted.slang:33`), a thread a vertex, for a mesh whose vertices
   a compute shader wrote, while one waits (`SceneField.cs:321`).
3. A cascade's cells shifted by copies, with no kernel, where a build moves a cascade by whole cells
   (`SceneField.cs:341`, `Rendering/Lighting/SceneFieldStages.Shift.cs:26`).
4. `field_splat` (`Shaders/field_splat.slang:251`), a workgroup a triangle, as the distance pass,
   the paint pass and, where a build holds an emitter thinner than a cell, the lending pass
   (`Rendering/Lighting/SceneFieldStages.cs:432`, `438`, `442`).
5. `field_resolve` (`Shaders/field_resolve.slang:189`), a thread a cell over the build's region or
   its whole cascade, on a build's last part (`SceneFieldStages.cs:482`).
6. `field_splat` again as the repaint, for a mesh whose colors changed in place
   (`SceneField.cs:350`, `SceneFieldStages.cs:540`).
7. `field_stamp` (`Shaders/field_stamp.slang:83`), a workgroup a brick of cells a moved mesh's
   shapes reach (`SceneFieldStages.cs:575`).
8. At High, the ray scene of the meshes, which the trace may query
   (`GlobalIllumination/GlobalIllumination.Rays.cs:12-29`).
9. `gi_trace` (`Shaders/gi_trace.slang:324`), a thread a ray, a dispatch a cascade
   (`GlobalIllumination/GlobalIlluminationStages.cs:386`). The far cascade traces a quarter of its
   probes a frame while it lies still, and a still cascade from the second up traces half
   (`GlobalIlluminationStages.cs:46-47`, `376-382`). It reads the frame before's gathered faces and
   the frame before's sun shadow map (`GlobalIlluminationRenderer.cs:479-490`).
10. `gi_merge` (`Shaders/gi_merge.slang:228`), a thread a probe's texel, from the last cascade down
    (`GlobalIlluminationStages.cs:411-448`), reading the frame before's reach (`:424`).
11. `gi_ambient` (`Shaders/gi_ambient.slang:182`), a thread a probe's face
    (`GlobalIlluminationStages.cs:474`), writing the faces, the faces at the surface, the reach, the
    skies and the ways, which the model pass and the next frame's trace read.
12. The screen probes, at Medium and High, or at Low while glow lights are lit
    (`GlobalIlluminationRenderer.cs:284-308`,
    `GlobalIllumination/GlobalIlluminationStages.ScreenProbes.cs:21`). `gi_screen`
    (`Shaders/gi_screen.slang:362`) runs a thread a probe over the half-size window depth
    (`ScreenProbes.cs:74`), `gi_screen_filter` (`Shaders/gi_screen_filter.slang:233`) runs twice,
    keeping and then blending (`:84-104`), and `gi_screen_glow` (`Shaders/gi_screen_glow.slang:274`)
    runs where glow lights are lit (`:135`). The images the next frame reads are copied after
    (`:148-159`).
13. The model pass (`Shaders/modelpass.slang:561`), forward, which takes the probes', the screen
    probes' and the glow lights' light into its own lighting, with the sun's contact shadow that the
    ambient occlusion pass traced through the field (`Shaders/ao.slang:133-156`).
14. The lit picture and its depth kept for the next frame's reflections
    (`GlobalIlluminationRenderer.cs:466-476`).

The views `gi_probes`, `field_view` and `gi_view` draw after the scene when asked, and
`gi_reference` runs outside the frame on `gi.reference`
(`GlobalIllumination/GlobalIlluminationCommands.cs:108`).

### BevyCSharp

The main world's tick queues the field at 2000 and the probes at 2100
(`GlobalIllumination/FieldWork.cs:11-21`), and the render world runs the queued dispatches in that
order before the cameras (`render/compute.rs:162-166`, `411-473`), a dispatch whose pipeline is
still compiling dropped for the frame (`:427-431`). The work on a camera runs at its frame points
(`render/views.rs:199-208`).

1. `field_resolve` as a clear (`Kernels/field_resolve.slang:65`), where the build before left its
   codes (`GlobalIllumination/SceneField.cs:535`).
2. `field_splat` (`Kernels/field_splat.slang:224`), a workgroup a triangle, as the distance pass,
   the paint pass and the lending pass (`SceneField.cs:546`, `552`, `560`).
3. `field_resolve` over the whole cascade, then again as a clear (`SceneField.cs:566`, `568`).
4. `field_stamp` (`Kernels/field_stamp.slang:56`), a workgroup a brick (`SceneField.cs:634`).
5. `gi_trace` (`Kernels/gi_trace.slang:191`), a thread a ray, a dispatch a cascade
   (`GlobalIllumination/WorldProbes.cs:471`), with no parts or halves, and none of this or the next
   two while the probes stand still. It reads the frame before's gathered faces and reach and the
   frame before's sky map.
6. `gi_merge` (`Kernels/gi_merge.slang:72`), from the last cascade down (`WorldProbes.cs:474-484`).
7. `gi_ambient` (`Kernels/gi_ambient.slang:142`), a thread a probe's face (`WorldProbes.cs:495`).
8. On each camera after its prepass, `gi_sky` (`Kernels/gi_sky.slang:27`) over a 64 by 64 octahedron
   (`WorldProbes.cs:304`), which the next frame's trace reads, and the screen probes, `gi_screen`
   (`Kernels/gi_screen.slang:402`) over the full depth, `gi_screen_filter`
   (`Kernels/gi_screen_filter.slang:146`) once, and `gi_screen_glow`
   (`Kernels/gi_screen_glow.slang:271`) at half size
   (`GlobalIllumination/WorldProbes.Screen.cs:226-228`). Which of the sky's and the screen's
   dispatches runs first was not found.
9. On each camera after its opaque pass, two full-screen draws added to the picture, `field_shadows`
   (`Kernels/field_shadows.slang:136`) and `gi_light` (`Kernels/gi_light.slang:614`)
   (`SceneField.cs:315-316`), which take back what Bevy's lighting gave and add the field's and the
   probes' in its place.
10. Leaves and grass drawn forward, which read the probes through WGSL joined into their shader
    (`render/forward_probes.rs:67`, `render/leaves.rs:322`, `363`).

`field_slice` draws on the field's window when asked (`GlobalIllumination/SceneField.Window.cs:39`),
and `gi_frame` and `gi_reference` run on a camera over several frames on `gi.reference`
(`GlobalIllumination/GiReference.cs:401`, `438`).

## The kernels paired

| 3DEngine | BevyCSharp | Its work |
|---|---|---|
| `field_counted` | none | A mesh's corners from vertices on the GPU |
| `field_splat` | `field_splat` | The field's codes, colors and emission from triangles |
| `field_resolve` | `field_resolve` | The codes turned into distances |
| `field_stamp` | `field_stamp` | Moved meshes stamped as shapes |
| `scenefield` | `bcs_field` | The field as its readers see it |
| `gi_trace` | `gi_trace` | A cascade's rays |
| `gi_merge` | `gi_merge` | The cascades merged from the last down |
| `gi_ambient` | `gi_ambient` | A probe's light gathered into the faces of a cube |
| `gi`, `cascades`, `probelight` | `bcs_gi` | The octahedron, the march, the probes' reads and a hit's light |
| `lightthrough` | the crossing functions of `bcs_field` | Rays through a surface that lets light pass |
| `roughreflect` | `reflectedAt` in `gi_light` | A rough surface's reflection from the probes |
| the environment cube read by `gi_trace` | `gi_sky` | The light of a ray that meets nothing |
| `gi_screen` | `gi_screen` | The screen probes' rays |
| `gi_screen_filter` | `gi_screen_filter` | The screen probes kept over frames and blended |
| `gi_screen_glow` | `gi_screen_glow` | The glow lights' light on the screen |
| `glow` | `bcs_glow` | The glow lights in closed form and their marches |
| the model pass's terms in `modelpass` and `lights` | `gi_light`, `forward_probes.rs` | The light put on the picture |
| `sunReaching` in `ao` | `field_shadows` | The sun's contact shadow through the field |
| `gi_reference` | `gi_reference` | The path-traced reference |
| `field_view`, `gi_probes`, `gi_view` | `field_slice`, `gi_frame` | Views, each engine's own |

The CPU side pairs as well. `SceneFieldPlan` (`Rendering/Lighting/SceneFieldPlan.cs` against
`GlobalIllumination/SceneFieldPlan.cs`), the triangles' pool
(`Rendering/Lighting/SceneField.Pool.cs` against `render/scene_field.rs`) and `GlowLights`
(`Rendering/Lighting/GlowLights.cs` against `GlobalIllumination/GlowLights.cs`) are read with the
kernels they feed.

## The field

The plan's constants are equal in both. The field is 64 cells a side at a first cell of 0.25, with a
band of 4 cells, at most 9 cascades, bricks of 4 cells, a snap of 8 cells, 8 frames to settle, 1,024
frames for a moving mesh to settle, at most 256 shapes and a budget of 32,768 triangles
(`Rendering/Lighting/SceneFieldPlan.cs:52-84` against `GlobalIllumination/SceneFieldPlan.cs:45-78`).
A triangle's corners are 48 bytes in both, a `uint4` a corner of the place's float bits and three
sRGB bytes with an open edge's bit at 24 (`Rendering/Lighting/SceneField.Pool.cs:73-86` against
`render/scene_field.rs:14-18`, `282-286`).

### The plan

| | 3DEngine | BevyCSharp |
|---|---|---|
| A cascade's place | centered on the snap nearest the eye, the eye 28 to 36 cells from each side (`SceneFieldPlan.cs:367-372`) | its middle 0.35 of its width ahead of the eye, snapped down (`SceneFieldPlan.cs:81`, `337-349`) |
| The far cascade's cell | its reach over 0.85 of 64 cells (`:357`) | the same, written as `0.5 + Lead` of 64 cells (`:314`) |
| Moving a cascade | its cells shifted, the slab brought in rebuilt (`SceneFieldPlan.Shifts.cs`) | rebuilt whole |
| A build | of a region (`SceneFieldPlan.Regions.cs`), its budget raised over frames (`SceneFieldPlan.Budget.cs`) | of a whole cascade |
| A mesh known by | its vertex array (`:669`) | its number and generation (`:613`) |
| A moved mesh's shapes | a box a part or a joint (`SceneFieldPlan.cs:599-626`, `788`) | a box a mesh (`SceneFieldPlan.cs:549-572`) |
| A mesh that came without moving | built, not stamped (`:201-206`, `493-495`) | stamped |
| Colors changed in place | repainted (`SceneFieldStages.cs:505-540`) | rebuilt as a new generation (`render/scene_field.rs:10-12`) |

3DEngine's form is the later. It dropped the lead at `3b5264d7` on 2026-11-01, since a lead makes a
turn rebuild the whole cascade (`SceneFieldPlan.cs:25-31`), and the shifts, the regions and the
budget came the same day (`2d051ada`, `2cb39bcb`, `6fd05fd4`). The pool differs in its keeping
alone. 3DEngine's is C# keyed by the vertex array, grown to twice what it holds and halved where
memory runs short (`SceneField.Pool.cs:21-51`, `103-114`), and BevyCSharp's is Rust keyed by number
and generation, uploaded whole when it changes (`render/scene_field.rs:556-619`). BevyCSharp
compares an open edge's corners by their bits (`render/field_triangles.rs:111`, `128-140`) and
3DEngine by their values (`SceneField.Pool.cs:136-153`), so -0 and +0 are one corner in 3DEngine and
two in BevyCSharp.

### `field_counted`

3DEngine's alone (`Shaders/field_counted.slang:33-44`, recorded at `SceneField.cs:319-326`). It
writes a mesh whose vertices only the GPU holds into the pool, with no edge marked open. BevyCSharp
has no such path, since `corners_of` returns none for a mesh whose data lies in the render world
alone (`render/field_triangles.rs:82-123`).

### `field_splat`

`Shaders/field_splat.slang:251` against `Kernels/field_splat.slang:224`, both 64 threads, a
workgroup a triangle, the instance found by a binary search (`:259-273` against `:232-247`).

| | 3DEngine | BevyCSharp |
|---|---|---|
| The code's bits | 1024ths of a cell above bit 11, a rim bit, sided, front, the way in eight bits (`:386`) | 1024ths above bit 10, sided, front, the way (`:362`) |
| Front on a double-sided face | the side of its plane the cell lies on, with the rim bit where the way to the cell leaves the normal (`:384-385`) | always set (`:361`) |
| A second plane | kept by an atomic minimum where a double-sided face of another way lies within 0.75 of a cell (`:403-413`) | none |
| The passes | distance, paint and second plane, lending, repaint (`:109-113`) | distance, paint, lending (`:96-100`) |
| Emission | two pairs of halves packed by SPIR-V's own instruction (`:244-249`, `433`) | a `float4`, since that instruction is SPIR-V's alone (`:26-27`, `391`) |
| Gloss | roughness and metallic in bytes of the instance's triangles' word (`:38-42`, `431`), 96 bytes an instance (`Rendering/Lighting/SceneField.cs:620`) | a `float4` of roughness, metallic and reflectance (`:49-50`, `387`), 112 bytes an instance (`GlobalIllumination/SceneField.cs:442`) |
| Leaf cards | none | each double-sided see-through triangle's area added into the cell of its middle (`:72-75`, `278-285`) |
| Its cells | clamped to the region's first and last cells (`:313-314`, `326-327`) | clamped to the cascade (`:293-294`, `306-307`) |
| Atomics | `InterlockedMin` on a `RWStructuredBuffer<uint>` (`:390`) | `Atomic<uint>`, which WGSL asks for (`:25-27`, `366`) |

The nearest point with open edges (`:127-159` against `:105-137`), the slivers passed over
(`:300-302` against `:271-273`), the tests of a cell behind a face and the thin emitter's lending
(`:320-369` against `:300-349`) are the same. 3DEngine's code word is the later. BevyCSharp's is
3DEngine's before `b16f26b5` of 2026-10-31, which brought the sheet's plane, and `fdde73b2` the same
day brought the second plane. BevyCSharp's leaf cards (`01951f29`, 2026-11-08) and its reflectance
in gloss (`7bd61bcc`, 2026-11-07) are its own and later than anything 3DEngine has of them.

### `field_resolve`

`Shaders/field_resolve.slang:189` against `Kernels/field_resolve.slang:65`, both a thread a cell, 4
by 4 by 4.

| | 3DEngine | BevyCSharp |
|---|---|---|
| Its cells | the build's region (`:194-195`) | the whole cascade |
| The codes cleared | by `vkCmdFillBuffer` (`Rendering/Lighting/SceneFieldStages.cs:381-394`) | by a second dispatch that clears alone, since a dispatch there has no call that fills a buffer (`:7-9`, `74-90`) |
| A double-sided face's distance | kept positive, with the plane in `yzw` (`:131-132`, `151-163`) | half a cell taken off (`:108-109`) |
| A wall thinner than a cell | `(d + beyond) / 2` less half a cell, or half the last counted cascade's cell in the far cascade (`:133-134`, `f05ea96f`) | less half a cell (`:110-111`) |
| The field | `rgba16f`, distance and plane (`SceneFieldStages.cs:95-104`) | `r16f`, distance alone (`GlobalIllumination/SceneField.cs:333-339`) |
| Beside it | the second plane, each cascade's codes kept for a repaint, the still glow (`:198`, `211-212`, `239-240`) | the light let through remapped, an odd byte for a leaf card and an even one for the rest (`:164-171`) |

The thin test and the pinched cells are the same (`:124-125`, `168-187` against `:106-107`,
`120-139`). 3DEngine's plane is the later and mended form. With BevyCSharp's half cell a sheet
thinner than a cell reads as a cell thick, and a ray traced through it stops half a cell before its
plane.

### `field_stamp`

`Shaders/field_stamp.slang:83` against `Kernels/field_stamp.slang:56`, a workgroup a brick, with the
same brick code, the same box's distance and the same rule that the nearer surface paints the cell
(`:113-131` against `:84-98`). Both shapes are 112 bytes, 3DEngine's carrying emission (`:27-29`)
and BevyCSharp's gloss (`:29-30`).

| | 3DEngine | BevyCSharp |
|---|---|---|
| The sheet's plane | kept and cleared (`:104`, `121`, `133`) | none |
| Glow | stamped (`:107`, `126`, `135`, `2cb39bcb`) | none |
| A stamped face's gloss | none, read as roughness 0.5 and no metal (`:130`, `Shaders/scenefield.slang:473-487`) | the shape's (`:97`) |
| The still images | read and written as storage (`:43-72`) | read as textures, since WGSL does not read and write an image of halves or bytes in one kernel (`:11-12`, `40-47`) |
| The info | a constant buffer (`:50`) | `info[0]` of a structured buffer (`:51`, `65`) |

3DEngine stamps glow from 2026-11-01 and BevyCSharp stamps gloss, so each holds a part the other
lacks.

### `scenefield` and `bcs_field`

The modules a reader of the field imports, `Shaders/scenefield.slang` against
`render/bcs_field.slang`. Both hold 9 cascades, a band of 8 cells (`FieldBand`) and 6 cells kept
clear about the eye (`EyeClear`), and the band's weight is the same code (`:138-146` against
`:72-80`).

| | 3DEngine | BevyCSharp |
|---|---|---|
| The info | `SceneFieldInfo`, 208 bytes, with the cells a shift brought in still waiting (`:73-88`, `Rendering/Lighting/GpuSceneField.cs:69`) | `FieldInfo`, 176 bytes (`:24-34`, `GlobalIllumination/SceneField.cs:439`) |
| A cascade's corner | against the frame's origin (`:68-69`) | in world numbers (`:18-19`) |
| The distance read | less half a cell near a sheet, `fieldDistanceIn` (`:225-233`), and the probes' rays through `fieldDistanceSheetIn` with the second plane (`:44-64`, `245-273`) | the sample as it is (`:127-134`) |
| The cascade of a point | the waiting cells passed over (`:190-205`) | every cell (`:108-122`) |
| The probes' cascade | `cascadeAt`, which reads the far cascade in place of the last (`:96-108`) | `probesOf`, which copies the info and writes the last cascade over (`:48-57`) |
| Gloss bits | bits 8 to 23 and a painted flag at 24 (`:478-487`) | bits 8 to 31 with reflectance (`:195-206`) |
| The volume | `Sampler3D` or `SplitVolume`, the second for Metal's sixteen samplers (`:10-40`) | a `Texture3D` and a `SamplerState` |

3DEngine's `cascadeAt` is the later form. Its comment says a copied info goes to the thread's memory
(`:100-103`), and BevyCSharp's own WGSL copy of the lookup reads the buffer in place for the same
reason (`render/forward_probes.rs:91-104`), while its Slang still copies.

## The world's probes

Both engines trace, merge and gather alike in shape. The trace is a thread a ray with 64 threads, so
a workgroup is a part of one probe, the merge a thread a probe's texel from the last cascade down,
and the gather a thread a probe's face. The constants below are the same in both.

| Constant | Value | 3DEngine | BevyCSharp |
|---|---|---|---|
| Probes' spacing | 8 cells | `Rendering/Lighting/Illumination.cs:15` | `GlobalIllumination/WorldProbes.cs:31` |
| Cascades | 5 at most, the far one among them | `Illumination.cs:18` | `render/bcs_gi.slang:114` |
| Rays of a texel | 4 at 8 texels a side or fewer, else 1 (`RaysAt`) | `Shaders/gi.slang:17-60` | `render/bcs_gi.slang:36-78` |
| A texel's mark | 0 a surface, 0.5 early, 0.25 sky, 1 nothing, a sixteenth a ray that met nothing | `gi.slang:79-91` | `bcs_gi.slang:86-98` |
| The march | 64 steps, a hit within a tenth of a cell, steps of a quarter cell at least | `Shaders/scenefield.slang:357-373` | `bcs_gi.slang:137-153` |
| `Clearance`, `MostMoved` | 0.1, 0.45 | `gi.slang:463-464` | `bcs_gi.slang:290-291` |
| `ReachTexels`, `WaysTexels` | 16, 10 | `gi.slang:457`, `Shaders/roughreflect.slang:10` | `bcs_gi.slang:278`, `284` |
| `Lean`, `CascadeBand` | 0.5, 8 | `Shaders/cascades.slang:15`, `21` | `bcs_gi.slang:398`, `403` |
| `Falls`, `Rises`, `OwnShare` | 0.8, 4, 0.25 | `gi.slang:483-509` | `bcs_gi.slang:310-335` |
| A probe's facing | a floor of 0.02, a smoothstep of 0.15 either way, 0.2 added | `gi.slang:432-444` | `bcs_gi.slang:228-237` |
| A probe seen | 0.3 of the spacing off, 0.125 of slack | `gi.slang:520-530` | `bcs_gi.slang:368-376` |
| An unseen probe's weight | `lerp(0.05, 1, sees)` | `Shaders/probelight.slang:225` | `bcs_gi.slang:481` |
| The intervals | the first from 0, the rest from the spacing to 4 spacings, the far from twice the cascade before's spacing, the last to the field's edge | `GlobalIllumination/GlobalIlluminationRenderer.cs:202-208` | `WorldProbes.cs:401-408` |

| | 3DEngine | BevyCSharp |
|---|---|---|
| Texels a side by quality | Low 8, 8, 8, Medium 8, 8, 16, High 8, 8, 16, 16 (`Illumination.cs:26-37`) | Low 8, 8, Medium 8, 8, 16, 16, High 8, 16, 16, 16 (`WorldProbes.cs:156-167`) |
| The cascade before the far one | a top cascade, its rays to the field's edge, merged from nothing above (`GlobalIlluminationRenderer.cs:191-207`, `GlobalIlluminationStages.cs:362`, `368`, `439-447`) | an interval of 4 spacings, the far cascade merged into it (`WorldProbes.cs:407`, `462`, `477-479`) |
| Fewer probes a frame | the far cascade in four parts, a still cascade's probes by halves (`GlobalIlluminationStages.cs:46-47`, `57`, `377-382`) | all, or none while still (`WorldProbes.cs:471`, `484`, `495`) |

3DEngine's top cascade before the far one (`f05ea96f`, 2026-10-25) and its High of 8 texels at the
second cascade (`d9f3f108`, 2026-11-01) are the later forms.

### `gi_trace`

`Shaders/gi_trace.slang:324` against `Kernels/gi_trace.slang:191`. The probe's placement off a
surface takes four steps along the normal, at most `MostMoved` (`:194-232` against `:164-189`), and
the kept share is summed over the same 27 probes (`:244-282` against `:231-275`).

| | 3DEngine | BevyCSharp |
|---|---|---|
| A probe in a narrow space | moved through the far face from the second cascade up (`:210-230`) | none |
| Outside | `d >= 0` (`:231`) | `d > 0` or a leaf card (`:186-188`, `5d575d5e`) |
| A carried ray | where none of the cascade above is seen or `around.w < 0.25` (`:374-401`), through the sixteen `TracePlaces` for a moving probe (`:404`, `459-461`) | where less than a quarter of the cascade above is seen (`:292-317`) |
| Mesh hits | the ray scene's query at High (`:475-486`, `547-604`) | none |
| A miss of the last cascade | the environment cube at its first level times its strength, or the ambient color where there is none (`:505`, `512`, `687`, `GlobalIlluminationRenderer.cs:527-536`) | the ambient light added to `gi_sky`'s octahedron (`:52-60`), the sun dimmed by the camera's air (`:214`) |
| Sky past the cascade above | `skyPastAbove` (`:304-322`, `510`, `685`) | none |
| The part before the interval through leaves | ends where the cascade below's interval ends (`:546`, `740066d6`) | ends at the interval's start (`:397`) |
| Surfaces shaded through leaves | two, the first leaf and the stop (`:656-678`) | each layer, at most 5, at the crown's mean depth (`:377-413`) |
| Light scattered before the interval | none | kept in `kept`'s color (`:92-98`, `397-401`, `a5f277d5`) |
| The sky's mark past leaves | where `through >= 0.5` (`:693`) | wherever the first hit is unset (`:423`) |
| Reach | each ray of a texel of four (`:722-727`, `6a190757`), laid out by the gather | written here, a ray a texel of 16 by 16 (`:453-455`, `e074525f`) |
| Written | radiance, `rg16f` distances, the hits' mean normals, the held and partial images (`:762-765`) | radiance, `r16f` distances, `kept`, the held and partial buffers (`:27-30`, `104-109`) |

BevyCSharp took the carried rays as 3DEngine's on 2026-11-04 (`98dad7f7`), and 3DEngine's moving
carry, its mesh hits and its sky past the cascade above came after. BevyCSharp's leaf layers and its
early scattered light are its own.

### `gi_merge`

`Shaders/gi_merge.slang:228` against `Kernels/gi_merge.slang:72`. The test of which of the eight
probes above are seen past leaves (`:253-266` against `:100-113`), the read toward the point the
probe above met (`:340-348` against `:152-160`), the folded read and the weight over the factor
squared are the same, and so are the alphas, 1, 0.5 for early and 0 inside a mesh (`:296-301`,
`360-378` against `:127-131`, `169-175`).

| | 3DEngine | BevyCSharp |
|---|---|---|
| The reach read | the four rays' cells at the texel's middle averaged (`:330-339`, `6a190757`) | the nearest texel (`:150-151`) |
| Whose reach | the frame before's, as the gather wrote it (`GlobalIlluminationStages.cs:424-425`) | this frame's, as the trace wrote it (`WorldProbes.cs:467-468`, `481`) |
| An agreement march | dark where none of the probes above agrees and the march met a surface (`:314-317`, `349-351`, `381-391`) | none |
| The sky merged | `mergedSky` carried down (`:63-70`, `393-397`) | none |
| A carried probe's cycle | sixteen frames, eased means (`:157-221`, `ed469e90`) | none |

3DEngine's reach read is the mended form. Its comment says the nearest cell past a corner reads far
too dark, which is BevyCSharp's read.

### `gi_ambient`

`Shaders/gi_ambient.slang:182` against `Kernels/gi_ambient.slang:142`. The faces sum to pi (`:254`,
`256` against `:178`, `233`), and the alpha, the walled-in test and the ways of 8 by 8 directions
with a border and a lobe of cos to the eighth (`:289-334` against `:239-290`) are the same.

| | 3DEngine | BevyCSharp |
|---|---|---|
| The probe's sums in `follow` | over `texels * texels * RaysAt(texels)` rays (`:123-129`) | over `texels * texels` rays (`:98`) |
| The patch's normal for the faces at the surface | the trace's kept mean normal (`:240-246`, `c3f31f93`) | `fieldNormal` a texel and a face (`:221-227`, `46ae797e`) |
| Reach | laid out here, from the rays' reach at 8 texels a side and from the distances otherwise (`:262-287`) | the trace's |
| Written beside | `skies` (`:202`, `219`, `258`) | none |
| Early light | the face times the kept share (`:216-218`, `317`) | the kept share and `kept`'s scattered light (`:166-168`, `228-229`, `264-265`) |

BevyCSharp's `follow` reads a fault 3DEngine mended at `d9f3f108` on 2026-11-01. A cascade of 8
texels a side sends four rays a texel, 256 a probe, in four workgroups, and BevyCSharp sums the
first workgroup's alone, the first sixteen texels, a band of the sphere, and divides by every
texel's share. 3DEngine's patch normal is the later and the cheaper.

### The probes' reads, `gi`, `cascades` and `probelight` against `bcs_gi`

| | 3DEngine | BevyCSharp |
|---|---|---|
| A cascade faded into the next | only where the next is built and is not the far cascade (`Shaders/cascades.slang:27-31`) | wherever the next is built, the far one among them (`render/bcs_gi.slang:498`) |
| A point's hold of the far cascade | `farHold`, weighed about the eye (`cascades.slang:59-60`, `64-78`, `9c6c8d0e`) | `bandHold` alone (`bcs_gi.slang:419-427`) |
| A probe's reach read in `probeSees` | the nearest of 16 by 16 (`Shaders/gi.slang:526-527`) | the four nearest blended (`bcs_gi.slang:341-355`, `368-376`, `07143fdf`) |
| Faces moved toward the surface | by how far ahead and how near the probe stands (`Shaders/probelight.slang:310-324`, `cd6cae8f`), for hits alone (`:218-219`) | by how far ahead (`bcs_gi.slang:478-479`, `540-542`), for every reader |
| The blend in `bouncedIn` | a seen-only sum it leans toward, and with the sky, a hidden probe's light less its sky (`probelight.slang:182-240`) | the 0.05-weighted blend (`bcs_gi.slang:446-486`), the sky's share gone at `e0014da4` |
| A hit's direct light | a GGX lobe a light (`gi.slang:391-428`), widened by the texel (`gi.slang:364-368`) | the light reaching the hit times its diffuse share and its split-sum gloss (`bcs_gi.slang:704-712`) |
| A hit's mirror lobe | the cube's faces about the mirror way (`probelight.slang:38-83`, `326-371`) | the ways of 8 by 8 (`bcs_gi.slang:575-594`, `8b16bd71`) |
| `f0` | 0.04 | `0.16 * reflectance^2` (`bcs_gi.slang:705`) |
| Glow lights marched at a hit | once (`probelight.slang:516`, `94097b62`) | twice (`bcs_gi.slang:711`) |
| The sun at a hit | the frame before's shadow map or the field (`gi.slang:174-197`, `317-352`) | the field (`bcs_gi.slang:204-220`) |

3DEngine reads as the later in the fade, the far cascade's hold, the faces moved toward the surface
and the glow light's one march. BevyCSharp reads as the later in the blended reach read, from
2026-11-10. Neither mirror lobe reads as the mended one, and the sky's share parts on purpose.
BevyCSharp dropped the sky's share at `e0014da4` on 2026-11-07, and its light pass says why
(`Kernels/gi_light.slang:748-753`), so `skies`, `mergedSky`, `skyAt` and `bouncedAndSkyAt`
(`probelight.slang:131-159`, `242-282`) are 3DEngine's own. BevyCSharp's leaf port reads plain
`cubes` with no faces at the surface (`render/forward_probes.rs:233-255`), unlike the `bcs_gi` it
copies.

### `lightthrough` and the crossing of `bcs_field`

`Shaders/lightthrough.slang` against `render/bcs_field.slang:294-459`, with `seesPastLeaves` at
`render/bcs_gi.slang:252-274`. 3DEngine steps out of a solid by `max(0.5 * cell, -d)`
(`lightthrough.slang:45`) and BevyCSharp by half a cell always (`bcs_field.slang:372`). BevyCSharp's
leaf cards are absorbed cell by cell with the depth `stopped` (`bcs_field.slang:300-323`,
`405-459`), and 3DEngine's merge march `metPastLeaves` has no like in BevyCSharp.

### `roughreflect` and `reflectedAt`

`Shaders/roughreflect.slang:16-99` against `Kernels/gi_light.slang:205-275` with
`render/bcs_gi.slang:385-393`. The probes' ways read about the mirror way are the same code.
3DEngine reads its reach filtered by the sampler inside a probe's block with a slack of 0.125
(`roughreflect.slang:16-27`) and the finest cascade alone (`:29-41`), and BevyCSharp blends by hand
with a slack of half the spacing and fades into the next cascade (`gi_light.slang:259-275`). Its
leaf port reads the finest alone (`render/forward_probes.rs:296-318`). 3DEngine returns the light
and its share apart (`:98`), BevyCSharp the light times its share (`gi_light.slang:252-253`), and
the two callers come to the same blend (`Shaders/modelpass.slang:684` against `gi_light.slang:768`).

### The sky

`gi_sky` (`Kernels/gi_sky.slang:27-59`) writes, on each camera after its prepass, the environment's
light at a roughness of 0.25 times the exposure over a 64 by 64 octahedron, with the sun's
transmittance through the camera's air and the environment's light from above in `gi_air`, which the
next frame's trace reads (`WorldProbes.cs:104`, `304`, `352`). 3DEngine has no pass for it. Its rays
read the environment cube at its first level times its strength, and the ambient color only where
there is no environment (`GlobalIlluminationRenderer.cs:527-536`, `Shaders/gi_trace.slang:505`).
3DEngine's light is in the scene's linear units and BevyCSharp's in the picture's after the exposure
(`render/gi_lights.rs:4-10`).

## The screen probes

BevyCSharp's screen probes came over at `f0ae14ec` on 2026-10-24. 3DEngine turned its rays' places
each frame and kept a mean over 16 frames at `2ad96a80` on 2026-10-30, let a probe stand in every
cascade at `b83b1a85` on 2026-11-03 and stopped marching to faint lights that send nothing at
`cfcfda02` on 2026-11-10, and BevyCSharp's filter is 3DEngine's before `2ad96a80`. The tiles are the
same, 16, 12 and 8 pixels at Low, Medium and High at 1080 rows and grown with the height
(`Rendering/Lighting/Illumination.cs:91-100` against
`GlobalIllumination/WorldProbes.Screen.cs:100-109`), and so is `JitterAt` (`Illumination.cs:60-65`
against `WorldProbes.Screen.cs:129-134`).

### `gi_screen`

`Shaders/gi_screen.slang:362` against `Kernels/gi_screen.slang:402`, 8 by 8, a thread a probe, 4 by
4 texels of a hemisphere, a ray from half a cell off the surface (`:491` against `:575`), the depth
marched in 12 steps with 5 halvings (`Shaders/screenmarch.slang:160-230` against a copy at
`:187-252`).

| | 3DEngine | BevyCSharp |
|---|---|---|
| The depth | half size, of the meshes that cast shadows, 1 for none (`:375`, the depth from `Rendering/PostProcess/AmbientOcclusion.cs:25-29`) | Bevy's prepass at full size, reversed, 0 for none (`:106-112`, `418-422`) |
| A probe's normal | toward the nearer texel (`:151-160`) | the sky's texels passed over (`:142-156`) |
| A ray's length | twice the spacing, doubled a cascade past the second (`:504`, `GlobalIlluminationRenderer.cs:216`) | twice the spacing (`:576`, `WorldProbes.Screen.cs:236`) |
| Cascades a probe stands in | each before the far one, five bound (`:222-230`, `482`, `43-61`) | the first two (`:10-11`, `558-570`, `49-53`) |
| A ray's place in its texel | turned through 16 places a frame (`:522-523`, `Shaders/gi.slang:358`) | fixed by the tile (`:596`) |
| Leaves | 5 layers, two surfaces shaded (`:562-615`) | 6, each shaded (`:623-673`) |
| A dark ray marched past its reach | where the probes hold nothing either (`:638-647`) | always (`:639-651`) |
| Glow lights' bytes | a buffer of 20 uints a probe (`Shaders/glow.slang:14-17`) | four `rgba32ui` images and one `rgba32f` (`:61-68`) |
| Faint glow lights | each marched to its middle, those that send nothing passed over (`:449-452`) | grouped by way into four, the brightest of each marched (`:483-543`) |
| Glow alone | at Low (`:464-468`) | where the program turned the probes off (`WorldProbes.Screen.cs:145`, `183-184`) |

3DEngine's cascades, places and faint lights are the later forms. BevyCSharp's normals that pass the
sky over and its grouping of faint lights are its own.

### `gi_screen_filter`

`Shaders/gi_screen_filter.slang:233`, two dispatches, against `Kernels/gi_screen_filter.slang:146`,
one. The 5 by 5 weights are the same (`:141-169` against `:82-104`).

| | 3DEngine | BevyCSharp |
|---|---|---|
| The order | kept over frames first, a mean of up to 16 (`:82`, `286-287`), then blended 5 by 5 (`:260-266`) | blended 5 by 5 first (`:163`), then a fifth of this frame to four fifths of the frame before (`:34`, `168-170`) |
| The frames kept | cut by the error against the spread (`:282-285`) | none |
| What the history holds | the kept light before the blend (`GlobalIlluminationStages.ScreenProbes.cs:150`) | the blended light (`WorldProbes.Screen.cs:207`), so the blend feeds back |
| A still view | the frame before not reprojected (`:187-198`, `574565cb`) | always reprojected (`:69-74`, `115-124`) |
| The 16 frames' stand-in | eased between rounds (`:61-68`, `88-96`, `289-324`, `ed469e90`) | none |

3DEngine's is the later form throughout.

### `gi_screen_glow`

`Shaders/gi_screen_glow.slang:274` against `Kernels/gi_screen_glow.slang:271`. The constants and the
work are the same line for line, `GlowRound` 16, `GlowStill` 1/32, `GlowMoving` 1/8,
`GlowDeviations` 1.5, `GlowMargin` 0.03, `GlowLetGo` 1/64 and `GlowSameSurface` 0.5 (`:80-98`
against `:79-93`), with `probeLike`, the 4 by 4 fallback, the dominant light, the new place and the
rules to drop and to start afresh. 3DEngine's texel is 2 pixels up to 1080 rows and grows past them
(`Rendering/Lighting/GpuScreenProbes.cs:56`), and BevyCSharp's is half the picture at any size
(`WorldProbes.Screen.cs:213-216`). 3DEngine reads the glow bytes from its buffer (`:190-200`) and
BevyCSharp from its images (`:142-147`, `189`). The texel's place less its offset in the frame
before came to both on 2026-11-12 (`fdf68d62` and `a64815c7`), so neither is the later.

### `glow` and `bcs_glow`

`Shaders/glow.slang:278-596` against `render/bcs_glow.slang:236-521` hold the same light, the same
80 bytes a light, at most 64 (`:12` against `:19`), and the same marches. 3DEngine clips a square's
light into an array of 5 in a loop (`:77-103`), and BevyCSharp edge by edge with no array, since a
local array spills to slow memory (`:7-11`, `79-121`). BevyCSharp reads a light past 8 half
diagonals as one point for every caller (`:147-192`), and 3DEngine reads far faces as points and a
light under a thirty-second of the largest as one point at a probe's hit alone
(`glow.slang:163-244`, `Shaders/gi.slang:665-697`, `b7f176d9`, `afd5640f`). `glowLightsAt` groups
faint lights by the quarter of the tangent plane in 3DEngine (`gi.slang:623-760`) and by the nearest
leading way in BevyCSharp (`bcs_glow.slang:540-645`). 3DEngine keeps its loops rolled, which keeps
the model pass's compile short (`glow.slang:2-5`), and BevyCSharp unrolls them.

`GlowLights` keeps 64 lights sorted nearest the eye in both, with the same places
(`Rendering/Lighting/GlowLights.cs:471-528` against `GlobalIllumination/GlowLights.cs:236-293`), the
same rule that a light is carried where its longest side is at most two spacings of the first
cascade's cells (`:196-203` against `GlobalIllumination/SceneField.cs:581-589`), and touching boxes
joined (`:359-414` against `GlobalIllumination/GlowLights.cs:143-192`). 3DEngine's reach takes a
faint level of 1/300 with a floor of half a spacing (`:72`, `80`, `249`, `47a5972b`) where
BevyCSharp's takes 0.001 with none (`:50`, `84`), and 3DEngine alone splits a mesh into parts
(`:111-175`), masks a light by its faces (`:178-193`, `336-353`), writes a head with the count and
the reach's box (`:54-64`, `240-261`) and places the lights against the frame's origin (`:269-283`).

## The light on the picture

3DEngine puts the indirect light in its own forward lighting, each term weighed by the probes' hold
in place of the environment's and the ambient lights' (`Shaders/modelpass.slang:705`, `744`, `757`).
BevyCSharp draws over a picture Bevy lit, so `gi_light` works out again what Bevy's ambient term,
its environment and its light probes gave a surface and takes it back before it adds its own
(`Kernels/gi_light.slang:186-203`, `732-738`, `762-764`, `772`). That take-back is BevyCSharp's own.

| | 3DEngine | BevyCSharp |
|---|---|---|
| The screen probes read to | the last counted cascade (`Shaders/lights.slang:475-479`, `01dd3ced`) | the second (`gi_light.slang:666`) |
| The world's probes taken | where the screen probes match under a quarter of their weight (`modelpass.slang:616-617`, `e86ca5c3`) | where none matches (`gi_light.slang:670-673`) |
| A pixel's world probes | `bouncedAndSkyAt`, a hidden probe less its sky (`lights.slang:494-503`) | `bouncedIn` with the faces at the surface (`render/bcs_gi.slang:446-486`) |
| Rough reflection applies | by the probes' hold above a roughness of 0.25 to 0.5 (`modelpass.slang:677`, `4105ee5e`) | by the hold at every roughness (`gi_light.slang:767-769`) |
| The light past the last cascade | the environment facing up and each ambient light (`modelpass.slang:705`, `744`) | one sum, the ambient light at 0.45 and the environment from above (`gi_light.slang:690`, `render/gi_lights.rs:22`, `117`) |
| A directional light's share past it | one read of its shadow map along the normal (`lights.slang:237-242`, `fbb0bf89`) | a march along the normal, each of up to 10 suns through the camera's air (`gi_light.slang:113-160`, `694-706`, `074645e3`) |
| The glow texel the pass reads | `lighting.screenGlow.x` pixels | 2 pixels |
| `f0` | 0.04 with a table of specular shares (`modelpass.slang:581`, `659`) | `0.16 * reflectance^2` with Bevy's table (`gi_light.slang:759-763`) |
| Units | the scene's linear light, a white surface's return (`lights.slang:483-490`) | the picture's, after the exposure (`render/gi_lights.rs:4-10`) |

The screen probes' reach and the world probes' fallback are later in 3DEngine, and BevyCSharp's
march of a sun's share is later than 3DEngine's one read, which BevyCSharp's leaves still make
(`render/forward_probes.rs:343-367`).

### The sun's contact shadow, `sunReaching` and `field_shadows`

`Shaders/ao.slang:133-156` against `Kernels/field_shadows.slang:48-70`, the same march, from a cell
and a half off in 40 steps of at least half a cell to 32 cells' reach
(`Rendering/PostProcess/AmbientOcclusion.cs:90`, `160` against
`GlobalIllumination/SceneField.cs:46`, `285`), with the same three modes. 3DEngine traces it at half
size in the ambient occlusion pass for the frame's one shadowed sun and multiplies the light by it
(`ao.slang:181-183`, `modelpass.slang:309-317`, `767`). BevyCSharp traces it for every pixel and
every directional light through the camera's air, works out Bevy's directional light again and takes
the shadowed part away (`field_shadows.slang:84-134`, `142-180`), and it takes its point as the
pass's world position (`:156`) where `gi_light` takes an offset from the eye. Neither reads as the
later.

### `gi_reference`

`Shaders/gi_reference.slang:575` against `Kernels/gi_reference.slang:652`. Both end a path by
Russian roulette after the third bounce at a share of 0.05 to 0.95 (`:548-553` against `:633-639`),
take at most 32 faces, draw the cosine and the GGX's visible normals and return a white surface's
light with no pi. 3DEngine scatters by gloss only where asked (`:128-133`, `489`, `528`), with
Lambert and its GGX at an `f0` of 0.04 (`:207-256`), carries textures, vertex colors, normal maps
and light through (`:461-468`), and places its lamps and camera against the ray scene's origin
(`Rendering/Lighting/GpuRayScene.cs:55-56`, `293`). BevyCSharp scatters by gloss at every face
(`7bd61bcc`, 2026-11-07) with Burley and Bevy's GGX (`:414-438`), reads the material's color with
its cut-outs and transmission (`:22-37`) and dims the sun through the air (`:654-661`).

### The views

3DEngine's `field_view` sphere traces a cascade over the window (`Shaders/field_view.slang:43-87`),
`gi_probes` draws the probes as cubes and `gi_view` the screen probes' parts. BevyCSharp's
`field_slice` paints a slice of a cascade into an image its window shows
(`Kernels/field_slice.slang:21-60`) and `gi_frame` copies the picture for `gi.compare`
(`Kernels/gi_frame.slang:10`). Each is its engine's own tool.

## What stays one engine's own

- **Binding.** 3DEngine binds by number in Vulkan descriptor sets with push constants
  (`GlobalIllumination/GlobalIlluminationStages.cs:72-112`, `510-552`), the info and the lights in
  constant buffers, and its model pass binds images without samplers and reads them through two
  shared samplers, since Metal allows sixteen samplers to a stage
  (`Rendering/Models/ModelRenderer.Lighting.cs:179-209`, `Shaders/scenefield.slang:10-13`).
  BevyCSharp sets each resource by its name from C# (`GlobalIllumination/WorldProbes.cs:464-470`),
  its kernels' numbers as `uniform` globals (`Kernels/gi_trace.slang:113-121`), the info and the
  lights in structured buffers, `bcs_pass`'s inputs in space 101 moved to group 1 after the compile
  (`render/bcs_pass.slang:8-10`), and its layout read from the WGSL.
- **What WGSL asks.** BevyCSharp's atomics are `Atomic<uint>`, its emission is not packed in halves,
  its still images are read as textures and its codes are cleared by a dispatch, each said at the
  kernel above.
- **The view.** 3DEngine's world kernels read no camera but the info's eye, its screen kernels
  `ScreenView` (`Shaders/screenmarch.slang:49`) built in doubles
  (`GlobalIlluminationRenderer.cs:380-458`), and its model pass its push and lighting block
  (`Shaders/modelpass.slang:56-64`, `Shaders/lightset.slang:73-119`). BevyCSharp's kernels read
  `bcs_pass`'s view, the view before, the depth and the lights (`render/bcs_pass.slang:39-61`), and
  `gi_light` the G-buffer.
- **The frame's origin.** 3DEngine's readers take every place against the frame's origin at the eye,
  the field's corners, the lights, the glow lights and the model pass's instances
  (`Rendering/Lighting/SceneField.cs:590-600`, `Shaders/modelpass.slang:44-45`, `414`,
  `Shaders/lightset.slang:124-127`), while its field's build stays in world numbers
  (`SceneField.cs:303-304`). BevyCSharp has no origin, and its light pass takes a point as an offset
  from the eye and adds the eye for the field's and the probes' reads
  (`Kernels/gi_light.slang:639-640`).
- **The field's lookups.** 3DEngine's waiting cells, sheets and second plane, and BevyCSharp's leaf
  cards and odd bytes, are each read by the engine's own readers.
- **Bevy's terms.** `gi_light`'s and `field_shadows`'s take-back of Bevy's ambient, environment,
  light probes and directional light, and the WGSL joined into the leaves, are BevyCSharp's
  bridge's.
- **3DEngine's ray scene.** The mesh hits of the trace and the reference at High, through ray
  queries.

## The three findings

**A hit past a cascade's band taking the next cascade's probes where none sees the point.** A
probe's ray that meets a surface reads the indirect light there from the finest cascade that holds
the point and from that cascade alone (`Shaders/probelight.slang:328-334` against
`render/bcs_gi.slang:603-610`). Past cascade c's band the point is read from the next cascade alone,
and a probe of it counts only where it stands in front of the surface and sees the point
(`probelight.slang:361` against `bcs_gi.slang:536`). Where none does, the read is 0
(`probelight.slang:369-370` against `bcs_gi.slang:631-632`) and the hit sends on its direct and glow
light alone (`probelight.slang:517` against `bcs_gi.slang:713`), with no blend of the hidden probes
to fall back on. The pixels' seen-only read has the same seam from the other side. It fades c into
c+1 across the band, and where either cascade's seen sum is nothing it drops both, c's light with
them, for the blend that weighs hidden probes by 0.05 (`Shaders/lights.slang:541-565`, the drop at
`:560-561`, against `Kernels/gi_light.slang:510-531`, the drop at `:526-527`). It belongs to the
read of a hit's light, 3DEngine's `bouncedSeenAtBoth` and BevyCSharp's `bouncedSeenAt`, which
`gi_trace` calls at each hit, and to the pixels' `bouncedSeenPlaced` and `bouncedSeen`.

**A tied cell of the field's color pass taking whichever thread ran last.** The distance pass keeps
each cell's least code by an atomic minimum (`Shaders/field_splat.slang:390` against
`Kernels/field_splat.slang:366`), and the paint pass lets every triangle whose code equals the
cell's write the cell's color, gloss and emission with plain stores (`:414`, `430-433` against
`:378`, `386-391`, and the repaint at `field_splat.slang:426-427`). Two triangles tie where every
bit of the code is equal, the same 1024th of a cell, the same bits for the sides and the same way's
byte, as coplanar neighbors or two meshes' coplanar faces do, and the last to store wins, which the
GPU's order decides. Parts of one build paint in a fixed order
(`Rendering/Lighting/SceneFieldStages.cs:346-349`), so the race lies inside one dispatch. It belongs
to `field_splat`'s paint pass.

**The field's build in world floats.** Both engines put a triangle's corners in the world by its
instance's matrix, translation and all (`Rendering/Lighting/SceneField.cs:627-629`,
`Shaders/field_splat.slang:290-292` against `render/scene_field.rs:461-462`, `506`,
`Kernels/field_splat.slang:261-263`), and take each cell's middle as the cascade's corner plus its
cells in world numbers (`field_splat.slang:375-378` against `Kernels/field_splat.slang:355-358`).
3DEngine leaves the build in world numbers on purpose (`SceneField.cs:303-304`, `a0af50a3`) and
places the readers' info and the stamps against the frame's origin, and BevyCSharp has no origin.
The float's step reaches the code's 1024th of a first cell of 0.25, 0.000244 units, at 2,048 units
out, so past that the finest cascade's distances are finer than the places they are taken from. It
belongs to `field_splat` and the plan that hands it a cascade's corner.

## The interface

The fewest things an engine gives a kernel, from what both supply.

1. **The field's images.** The distances, the cascades along z, with the colors in `rgba8`, the
   emission, the faces in `r32ui`, and 3DEngine's second plane where it keeps one. 3DEngine's
   `GpuSceneField` (`Rendering/Lighting/GpuSceneField.cs:60-154`), BevyCSharp's `SceneField`
   (`GlobalIllumination/SceneField.cs:127-150`). The kernels take them through an interface as
   3DEngine's `IVolume` (`Shaders/scenefield.slang:14`), so one engine splits its samplers and the
   other binds a texture and a sampler.
2. **The field's cascades.** The counts, each cascade's corner and cell, and the eye, against one
   origin, with the waiting box where cascades shift. 3DEngine's `SceneFieldInfo`
   (`Shaders/scenefield.slang:73-88`), BevyCSharp's `FieldInfo` (`render/bcs_field.slang:24-34`).
   The module's lookup of a probes' cascade reads the table in place, as `cascadeAt` does.
3. **The plan's facts for the probes.** Each cascade's built corner, which cascade the probes take,
   whether there is a far one, the frame's builds with their changed boxes, the first cell, and
   whether the field is whole (`GlobalIlluminationRenderer.cs:233-253` against
   `WorldProbes.cs:431-449`).
4. **The origin.** One point every place is taken against, the frame's origin in 3DEngine
   (`Shaders/lightset.slang:119`) and zero in BevyCSharp.
5. **The lights.** The sun's way and color, the sky's light, up to 16 lamps and the glow lights, 64
   of 80 bytes, alike in both (`Shaders/gi.slang:113-145` against `render/bcs_gi.slang:117-131`).
6. **The sky.** The light along a way for a ray that meets nothing, the environment cube in 3DEngine
   and `gi_sky`'s octahedron in BevyCSharp, as a function of a way.
7. **The sun's reach.** Whether the sun reaches a point, as 3DEngine's `ISunShadow`
   (`Shaders/gi.slang:149-174`), the frame before's shadow map or the field in 3DEngine and the
   field in BevyCSharp.
8. **The screen's view.** This frame's and the frame before's view and projection with their
   inverses, the eye, the picture's size, the depth with its size and the jitter (3DEngine's
   `ScreenView`, `Shaders/screenmarch.slang:49`, against `bcs_pass`'s `PassView` and
   `PassPreviousView`, `render/bcs_pass.slang:39-80`).
9. **The frame before.** The images the next frame reads, kept by copies in 3DEngine and as a
   camera's history images in BevyCSharp, with a frame's count, its parity and whether the history
   holds.
10. **The order.** The field's build, then the trace, the merge and the gather before any camera,
    then on each view after its depth the screen probes, then the light put on the picture, then the
    lit picture kept for the next frame's reflections.

## What holds each kernel

The tests each engine has for the kernels this document pairs, so the move of item 3 knows what must
read alike before and after it. Each engine's tests are read as committed, 3DEngine's at `d102cd72`
of 2026-11-14 and BevyCSharp's at `36840712`. 3DEngine's lie in `3DEngine.Tests/Rendering/`.
BevyCSharp's lie in `BevyCSharp.Tests/GlobalIllumination/`, apart from `TreeCanopyTests.cs` in
`BevyCSharp.Tests/Vegetation/` and the Rust tests in `field_triangles.rs` and `scene_field.rs` under
`render/`. A test is named by its file, the line of its method and the method's name.

### The devices

| Device | What the test needs | Where it runs |
|---|---|---|
| CPU | the runtime alone, a plain `[Fact]` | everywhere, CI among them |
| Vulkan | `slangc` and any Vulkan device, `NeedsVulkanFact` (3DEngine's `Needs.cs:30`) | CI on lavapipe on Linux and Windows, where `E3D_REQUIRE_VULKAN=1` fails a missing device rather than skipping it, and through MoltenVK on macOS (3DEngine's `.github/workflows/build.yml:125-134`) |
| GPU rays | a Vulkan device with ray queries that does not draw on the CPU, `NeedsRayQuery` (3DEngine's `Needs.cs:73-91`) | a desktop GPU alone, skipped in every CI job, the bounds measured on an RTX 4070 |
| Bevy | a render bridge and `slangc`, `Needs.Shaders()` (BevyCSharp's `Needs.cs:27`, `62-66`) | a local GPU alone, since CI builds the headless bridge (BevyCSharp's `build.yml:173`) and the test skips there |
| Bevy rays | the same with a wgpu adapter offering ray queries, `Needs.RayQueries()` (BevyCSharp's `Needs.cs:112-113`) | a local GPU alone |
| Rust | `cargo test` of the bridge (BevyCSharp's `build/test.py:122-124`) | Linux CI, on the CPU |

3DEngine's comparisons with a path-traced reference run on no CI machine, and BevyCSharp's tests of
global illumination on the GPU run on none either, its CPU and Rust tests alone running in CI. A
kernel moved into the module is held in CI by 3DEngine's Vulkan tests on lavapipe and by the CPU
tests of both, and against a reference by local runs on a GPU with ray queries in each engine.

### The field's plan

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `SceneFieldTests.cs:34` `A_Cascade_Lies_Around_The_Eye_And_Moves_Only_Past_Its_Grid` | Each cascade's corner about the eye and which cascades rebuild as the eye crosses its grid, exact corners | CPU |
| 3DEngine | `SceneFieldTests.cs:52` `A_Mesh_Drawn_The_Same_For_Eight_Frames_Is_Built_And_Stands_In_For_Nothing_Until_Then` | A mesh built after eight still frames and stamped as nothing before | CPU |
| 3DEngine | `SceneFieldTests.cs:76` `A_Still_Mesh_Waiting_For_Its_Cascades_Is_Stamped_Only_Where_It_Moved_Before_It_Settled` | A still mesh waiting for its cascades stamped only where it moved | CPU |
| 3DEngine | `SceneFieldTests.cs:108` `A_Still_Mesh_Replaced_In_Place_Stays_In_The_Field_Until_The_One_Replacing_It_Settles` | A mesh replaced in place kept until its replacement settles | CPU |
| 3DEngine | `SceneFieldTests.cs:135` `A_Mesh_Kept_For_A_Replacement_That_Never_Settles_Leaves_After_Twice_The_Frames_To_Settle` | A kept mesh leaves after twice the settling frames, exactly | CPU |
| 3DEngine | `SceneFieldTests.Replaced.cs:10` `A_Mesh_That_Replaced_One_In_Place_Is_Not_Stamped_While_A_Cascade_Not_Yet_Built_Holds_The_One_It_Replaced` | No stamp while a cascade not yet built holds the replaced mesh | CPU |
| 3DEngine | `SceneFieldTests.cs:155` `The_Budget_Builds_The_Finest_Dirty_Cascades_First` | The finest dirty cascades built first, 0, 1, 2 | CPU |
| 3DEngine | `SceneFieldTests.cs:171` `A_Moving_Mesh_Is_Stamped_Into_The_Bricks_Around_It_And_They_Are_Put_Back_When_It_Leaves` | A moving mesh stamped into 125 bricks and the bricks given back | CPU |
| 3DEngine | `SceneFieldTests.cs:194` `Each_Brick_Is_Given_The_Shapes_That_Come_Within_It_Alone` | Each brick's own shapes, the ranges summing to the list | CPU |
| 3DEngine | `SceneFieldTests.cs:225` `A_Scene_Drawn_In_The_Same_Order_Is_Read_By_Place_And_Planned_As_One_Drawn_In_Another_Order_Each_Frame` | A scene read by place matches one read whole over about 1,038 frames | CPU |
| 3DEngine | `SceneFieldTests.cs:260` `Figures_Past_What_A_Frame_Stamps_Are_Boxes_Beyond_The_Nearest_And_None_Is_Left_Out` | Figures past a frame's shapes become boxes, none left out | CPU |
| 3DEngine | `SceneFieldTests.cs:501` `A_Moving_Mesh_Is_Stamped_In_Its_Color_Times_Its_Vertices_Mean_Color` | A stamped shape's color, exactly | CPU |
| 3DEngine | `SceneFieldTests.Figures.cs:10` `A_Figure_Walking_Among_Still_Meshes_Is_Read_By_Place_And_Builds_No_Cascade_Again_Where_It_Stops` | A walking figure read by place builds no cascade again | CPU |
| 3DEngine | `SceneFieldTests.Figures.cs:37` `A_Copy_That_Never_Moved_Settles_After_The_Short_Count_Beside_One_That_Walks` | A copy that never moved settles after the short count | CPU |
| 3DEngine | `SceneFieldTests.Budget.cs:47` `Five_Hundred_Meshes_Arriving_In_One_Frame_Are_Built_Over_Several_Frames_Under_The_Triangle_Budget_The_Nearest_First` | 500 meshes built in parts under 65,536 triangles a frame, nearest first | CPU |
| 3DEngine | `SceneFieldTests.Budget.cs:94` `A_Build_In_Progress_Takes_In_A_Mesh_That_Settles_And_Starts_Again_For_A_Splatted_One_That_Leaves` | A build in progress takes in a settled mesh and restarts for a leaving one | CPU |
| 3DEngine | `SceneFieldTests.Budget.cs:146` `A_Finer_Cascade_The_Eye_Moves_Past_Is_Built_Before_A_Coarser_Build_In_Progress_Which_Starts_Again_After` | A finer cascade the eye passes is built before a coarser one in progress | CPU |
| 3DEngine | `SceneFieldTests.Budget.cs:224` `The_Triangle_Budget_Is_Raised_Where_The_Frame_Has_Room_And_Falls_Back_Where_It_Has_None` | The budget raised where the frame has room and back where it has none | CPU |
| 3DEngine | `SceneFieldTests.Budget.cs:255` `A_Cut_Past_The_Finer_Cascades_Boxes_Builds_Them_At_Once_And_A_Walk_Keeps_The_Budget` | A cut builds the finer cascades at once and a walk keeps the budget | CPU |
| 3DEngine | `SceneFieldTests.Cut.cs:12` `After_A_Cut_The_Lift_Holds_While_The_Meshes_The_New_View_Brings_Arrive_And_Ends_Once_Frames_Bring_None` | The lift after a cut holds and ends | CPU |
| 3DEngine | `SceneFieldTests.Regions.cs:13` `A_Mesh_That_Settles_In_A_Built_Cascade_Builds_The_Cells_Within_Its_Band_From_The_Meshes_Near_Them_Alone` | A settling mesh builds the cells within its band, the changed box exactly | CPU |
| 3DEngine | `SceneFieldTests.Shifts.cs:14` `A_Move_Of_The_Eye_By_Whole_Cells_Shifts_Its_Cascade_And_Builds_The_Slab_It_Brings_In_Alone` | A whole-cell move shifts the cascade and builds the slab it brings in | CPU |
| 3DEngine | `SceneFieldTests.Shifts.cs:39` `The_Field_Is_Not_Whole_While_The_Last_Cascades_Shift_Holds_Its_Slab_Empty` | The field is not whole while the last cascade's slab is empty | CPU |
| 3DEngine | `SceneFieldTests.Whole.cs:14` `The_Field_Is_Whole_Once_Every_Mesh_That_Came_Is_Built_In_And_Stays_So_While_A_Move_Of_The_Eye_Builds_It_Again` | Whole, built and frames built through arrivals and a move | CPU |
| 3DEngine | `SceneFieldTests.Far.cs:13` `A_Far_Cascade_Reaches_Its_Distance_Ahead_In_Cells_Of_Its_Own_And_Is_Left_Out_Of_The_Count` | The far cascade's cells and its place about the eye | CPU |
| 3DEngine | `SceneFieldTests.Far.cs:46` `The_Far_Cascade_Is_Built_After_The_Counted_Ones_Under_The_Budget_With_The_Meshes_Past_Them` | The far cascade built after the counted ones under the budget | CPU |
| 3DEngine | `SceneFieldTests.Far.cs:137` `The_Last_Cascade_Of_Probes_Stands_In_The_Far_Cascade_With_As_Many_Directions_As_The_One_Before` | The last cascade of probes stands in the far cascade | CPU |
| 3DEngine | `GlobalIlluminationTests.Ahead.cs:12` `The_Field_Held_Ahead_Lies_Around_The_Point_Half_Its_Finest_Cascade_Before_The_Eye` | The field held ahead lies about the point half its finest cascade before the eye | Vulkan |
| BevyCSharp | `SceneFieldTests.cs:44` `ACascadeLiesAroundTheEyeAndMovesOnlyPastItsGrid` | Each cascade's corner about the eye, BevyCSharp's own snap | CPU |
| BevyCSharp | `SceneFieldTests.cs:64` `AMeshDrawnTheSameForEightFramesIsBuiltAndStampedUntilThen` | A mesh built after eight still frames and stamped until then, the opposite of 3DEngine's | CPU |
| BevyCSharp | `SceneFieldTests.cs:90` `AStillMeshReplacedInPlaceStaysInTheFieldUntilTheOneReplacingItSettles` | A mesh replaced in place kept until its replacement settles, by generation | CPU |
| BevyCSharp | `SceneFieldTests.cs:115` `TheBudgetBuildsTheFinestDirtyCascadesFirst` | The finest dirty cascades built first | CPU |
| BevyCSharp | `SceneFieldTests.cs:134` `AMeshDrawnTwiceInOnePlaceIsToldAndTheFramesWithoutItsCopyAreReadByPlace` | A mesh drawn twice in one place told, the frames without its copy read by place | CPU |
| BevyCSharp | `SceneFieldTests.cs:152` `AMovingMeshIsStampedIntoTheBricksAroundItAndTheyArePutBackWhenItLeaves` | A moving mesh stamped into the bricks about it, the same numbers as 3DEngine's | CPU |
| BevyCSharp | `SceneFieldTests.Budget.cs:52` `FiveHundredMeshesArrivingInOneFrameAreBuiltOverSeveralFramesUnderTheTriangleBudgetTheNearestFirst` | 500 meshes built in parts, nearest first | CPU |
| BevyCSharp | `SceneFieldTests.Budget.cs:89` `ABuildInProgressTakesInAMeshThatSettlesAndStartsAgainForASplattedOneThatLeaves` | A build in progress takes in a settled mesh and restarts | CPU |
| BevyCSharp | `SceneFieldTests.Budget.cs:144` `AFinerCascadeTheEyeMovesPastIsBuiltBeforeACoarserBuildInProgressWhichStartsAgainAfter` | A finer cascade the eye passes is built first | CPU |
| BevyCSharp | `GlobalIlluminationTests.cs:49` `AFarCascadesCellsReachItsDistanceAheadOfTheEye` | The far cascade's cells and the probes' cascade | CPU |

The two engines part on purpose in two places, each stated by a test. BevyCSharp stamps a mesh until
it settles where 3DEngine stamps nothing (3DEngine's `SceneFieldTests.cs:52` against BevyCSharp's
`SceneFieldTests.cs:64`), and each cascade's corner differs by BevyCSharp's lead (3DEngine's
`SceneFieldTests.cs:34` against BevyCSharp's `SceneFieldTests.cs:44`). The module keeps one form of
each, and the other engine's test changes with it.

- **BevyCSharp has the code and no test** for the bricks' own shapes (3DEngine's
  `SceneFieldTests.cs:194`), the read by place and the walking figure (`:225`,
  `SceneFieldTests.Figures.cs:10`, `:37`), the cap on shapes (`:260`), a stamped shape's color
  (`:501`), the far cascade built under the budget (`SceneFieldTests.Far.cs:46`) and the field held
  ahead (`GlobalIlluminationTests.Ahead.cs:12`). Each is a port of the 3DEngine test named.
- **3DEngine has no test** of a mesh drawn twice in one place (BevyCSharp's
  `SceneFieldTests.cs:134`) and no code for it either.
- **3DEngine's own**, held by their own tests, are the regions, the shifts, the lift after a cut and
  the pool's capacity.

### The triangles' pool

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `SceneFieldTests.cs:212` `An_Edge_Is_Open_Where_No_Other_Triangle_Shares_Its_Ends_Whatever_Vertices_Hold_Them` | An edge open where no other triangle shares its ends | CPU |
| 3DEngine | `SceneFieldTests.cs:276` `A_Moving_Mesh_That_Does_Not_Bend_Is_Stamped_As_The_Parts_Its_Triangles_Are_Cut_Into` | A moving mesh that does not bend stamped as its parts | CPU |
| 3DEngine | `SceneFieldTests.Budget.cs:81` `The_Pool_Takes_Twice_Its_Build_Where_The_Device_Has_Room_And_Less_Where_It_Is_Short` | The pool's capacity, exactly, by the device's room | CPU |
| BevyCSharp | `field_triangles.rs:157` `an_edge_is_open_where_no_other_triangle_shares_its_ends` | An edge open where no other triangle shares its ends, the same quad as 3DEngine's | Rust |
| BevyCSharp | `field_triangles.rs:165` `a_mesh_weighs_the_triangles_the_pool_reads` | The triangles a mesh hands the pool | Rust |
| BevyCSharp | `field_triangles.rs:179` `a_mesh_emits_from_the_area_of_its_triangles` | A mesh's emitting area, a cube within 1e-4 and a sphere within 2% | Rust |
| BevyCSharp | `scene_field.rs:650` `a_draw_lies_where_the_managed_side_reads_it` | A draw's layout where the managed side reads it | Rust |
| BevyCSharp | `SceneFieldTests.cs:25` `ADrawLiesWhereTheBridgeWritesIt` | The same layout from the managed side | CPU |

The area a mesh emits from (`field_triangles.rs:179`) has no 3DEngine test, its share held only for
boxes (`GlowLightsTests.cs:68`, `:90`).

### `field_splat` and `field_resolve`

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `SceneFieldTests.cs:361` `The_Field_Built_On_The_GPU_Holds_The_Distances_To_Its_Meshes` | A box's distances read back, worst under 0.01, and its paint | Vulkan |
| 3DEngine | `SceneFieldTests.cs:514` `A_Sphere_Holds_Its_Distances_On_Every_Side_Its_Poles_Too` | A sphere's distances on every side, the poles too, worst under 0.03 | Vulkan |
| 3DEngine | `SceneFieldTests.cs:626` `No_Cell_Away_From_Every_Mesh_Reads_As_Near_A_Surface` | No cell away from every mesh read as near a surface over three cascades | Vulkan |
| 3DEngine | `SceneFieldTests.cs:691` `A_Closed_Wall_Thinner_Than_A_Cell_Stops_A_Ray_And_An_Open_Plane_Puts_No_Wedge_Below_Its_Rim` | A closed wall thinner than a cell stops a ray, an open plane puts no wedge under its rim | Vulkan |
| 3DEngine | `SceneFieldTests.Sheets.cs:14` `A_Double_Sided_Sheet_Thinner_Than_A_Cell_Is_Met_At_Its_Plane_Wherever_It_Lies_Between_The_Cells` | A double-sided sheet met at its plane within a tenth of a cell, the reader done again on the CPU | Vulkan |
| 3DEngine | `SceneFieldTests.Sheets.cs:80` `Two_Double_Sided_Sheets_Crossing_Where_Four_Cells_Meet_Are_Both_Met_In_Those_Cells` | Two crossing sheets both met, the first and second planes | Vulkan |
| 3DEngine | `SceneFieldTests.Sheets.cs:138` `A_Closed_Room_Of_Double_Sided_Sheets_Stays_Dark_Inside` | A closed room of double-sided sheets stays dark inside | Vulkan |
| 3DEngine | `SceneFieldTests.Far.cs:79` `A_Sheet_Under_The_Far_Cascades_Wide_Cells_Rises_No_Slab_In_Them` | A sheet under the far cascade's wide cells rises no slab | Vulkan |
| 3DEngine | `SceneFieldTests.cs:320` `A_Skinned_Mesh_Is_Held_As_The_Boxes_Of_Its_Joints_Where_Its_Pose_Puts_Them` | A skinned mesh held as its joints' boxes | Vulkan |
| 3DEngine | `SceneFieldTests.Regions.cs:40` `The_Cells_Built_Again_Where_A_Mesh_Was_Replaced_In_Place_Hold_What_A_Whole_Build_Of_The_Scene_Holds` | A region built again holds what a whole build holds, distances bit for bit | Vulkan |
| 3DEngine | `SceneFieldTests.Shifts.cs:73` `A_Shifted_Cascade_Holds_The_Field_A_Build_Of_The_Whole_Cascade_At_Its_New_Corner_Holds` | A shifted cascade holds what a whole build at its new corner holds, worst under 0.01 | Vulkan |
| 3DEngine | `SceneFieldTests.Budgets.cs:13` `A_Budget_Changed_Through_SetSceneField_Keeps_The_Field_And_A_Changed_Cell_Makes_It_Again` | A budget changed keeps the field, a changed cell makes it again | Vulkan |
| 3DEngine | `SceneFieldTests.Budget.cs:175` `On_The_Device_Five_Hundred_Meshes_Arriving_At_Once_Are_Built_With_No_Frame_Near_The_Field_Time_Of_All_At_Once` | 500 meshes on the device, no budgeted frame near the time of all at once | Vulkan |
| 3DEngine | `SceneFieldTests.March.cs:12` `A_March_Read_Back_Stops_At_A_Wall_In_The_Cascade_And_Cell_That_Hold_It_And_Leaves_The_Field_Above_It` | A march over the read-back field stops at the wall in its cascade, 1.85 within an eighth | Vulkan |
| 3DEngine | `SceneFieldTests.Materials.cs:11` `A_Mesh_Whose_Material_Bounces_No_Light_Casts_Its_Shadow_And_Is_Not_Held_In_The_Field` | Meshes whose material sends no indirect light, or that cast no shadow, left out of the field | Vulkan |
| 3DEngine | `SceneFieldTests.cs:438` `The_Field_Paints_A_Mesh_In_Its_Vertices_Colors_As_The_Model_Pass_Draws_Them` | A mesh painted in its vertices' colors within 0.02 | Vulkan |
| 3DEngine | `SceneFieldTests.cs:562` `An_Emissive_Sheet_Thinner_Than_A_Cell_On_A_Wall_Emits_Its_Whole_Light_Where_It_Lies` | An emissive sheet thinner than a cell emits its whole light, over 1.8 of 2 | Vulkan |
| BevyCSharp | `SceneFieldTests.cs:177` `TheFieldBuiltOnTheGpuHoldsTheDistancesToABox` | A box's distances read back, the same bounds as 3DEngine's, and the field's state line | Bevy |
| BevyCSharp | `SceneFieldTests.cs:222` `ATunnelOneCellWideIsClearInItsMiddle` | A tunnel one cell wide clear in its middle | Bevy |
| BevyCSharp | `SceneFieldTests.cs:320` `TheBudgetKeepsAFramesBuildsToTheCascadesAsked` | A frame's builds kept to the cascades asked | Bevy |
| BevyCSharp | `SceneFieldTests.Budget.cs:178` `OnTheGpuFiveHundredMeshesArrivingAtOnceAreBuiltInPartsWithNoSplatNearTheOneOfAllAtOnce` | 500 meshes built in parts hold the distances of one whole build, worst under 1e-3 | Bevy |
| BevyCSharp | `GlobalIlluminationTests.cs:73` `TheFieldPaintsEachCellItsNearestSurfacesColor` | Each cell painted its nearest surface's color, by bytes | Bevy |
| BevyCSharp | `GlobalIlluminationTests.Gloss.cs:23` `TheFieldKeepsEachCellsNearestSurfacesGlossBesideItsFace` | Each cell's gloss beside its face, within 0.01 | Bevy |
| BevyCSharp | `TreeCanopyTests.cs:43` `AForestFloorTakesTheSkyLightItsCanopyLetsThrough` | Leaf cards painted to let the sky's light through | Bevy |

- **BevyCSharp has the code and no test** for a sphere's poles (3DEngine's
  `SceneFieldTests.cs:514`), the signs over a varied yard (`:626`), a closed wall thinner than a
  cell (`:691`), the vertices' colors (`:438`), a thin emitter's whole light (`:562`), the far
  cascade's sheet (`SceneFieldTests.Far.cs:79`) and meshes left out of the field
  (`SceneFieldTests.Materials.cs:11`). Its double-sided faces, read as a zero crossing half a cell
  off, have no test, which a port of `SceneFieldTests.Sheets.cs:14` and `:138` to zero crossings
  closes.
- **3DEngine has the code and no test** for a build in parts holding the distances of one whole
  build on the device (BevyCSharp's `SceneFieldTests.Budget.cs:178`, worst under 1e-3, where
  3DEngine's `SceneFieldTests.Budget.cs:175` reads the time alone), a tunnel one cell wide
  (BevyCSharp's `SceneFieldTests.cs:222`), the field's gloss read back (BevyCSharp's
  `GlobalIlluminationTests.Gloss.cs:23`) and the cascades a frame builds on the device (BevyCSharp's
  `SceneFieldTests.cs:320`).
- **The shared test** is the box of 2 read back, with the same bounds in both (3DEngine's
  `SceneFieldTests.cs:361` against BevyCSharp's `SceneFieldTests.cs:177`).

### `field_stamp`

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `GlobalIlluminationTests.cs:255` `A_Red_Wall_That_Moves_Tints_The_Side_Of_A_White_Block_Facing_It_As_A_Still_One_Does` | A moving red wall tints a white block as a still one does, read through the light | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Moving.cs:15` `An_Emissive_Bar_Too_Long_To_Be_A_Glow_Light_Lights_A_Wall_As_It_Moves_As_It_Does_Held_Still` | A moving emissive bar lights as a still one within 0.6 to 1.8 | Vulkan |
| BevyCSharp | `SceneFieldTests.cs:258` `AMovingBoxIsStampedWhereItStandsAndTheCellsItLeftAreEmptyAgain` | A moving box stamped where it stands, read back, the cells it left empty again | Bevy |

3DEngine reads a stamped shape through the light alone, so BevyCSharp's read-back of a moving box's
distances (BevyCSharp's `SceneFieldTests.cs:258`) is the test it lacks. BevyCSharp has no test of a
moving wall's color through the light (3DEngine's `GlobalIlluminationTests.cs:255`), and a port of
the moving emitter (`GlobalIlluminationTests.Moving.cs:15`) fails there by design, since
BevyCSharp's stamp writes no glow.

### `field_counted`

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `GlobalIlluminationTests.Counted.cs:46` `A_Red_Wall_A_Compute_Shader_Wrote_Tints_The_Side_Of_A_White_Block_Facing_It` | A wall a compute shader wrote tints a white block | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Counted.cs:104` `A_Wall_A_Compute_Shader_Wrote_Bounces_The_Color_Of_Its_Color_Stream_Beside_A_Stream_Of_Its_Own` | The same with a color stream | Vulkan |

3DEngine's own, as the kernel is.

### The readers' modules, `scenefield` and `bcs_field`

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `SceneFieldTests.cs:731` `Particles_Bounce_Off_A_Wall_Behind_The_Camera_Through_The_Field_And_Pass_It_Without` | Particles bounce off a wall behind the camera through the field | Vulkan |

No test in either engine reads the readers alone. 3DEngine's sheet and march tests
(`SceneFieldTests.Sheets.cs:14`, `:80`, `SceneFieldTests.March.cs:12`) do the readers' work again on
the CPU over a read-back field, so a fault in the shader's own reader passes them, and the readers
are held through the pictures that use them. A test that samples a built field through the module's
reader in a compute dispatch and reads the result back holds them directly, and the move needs it
before the readers go.

### `gi_trace`, `gi_merge` and `gi_ambient`

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `GlobalIlluminationTests.cs:28` `Each_Quality_Traces_More_Directions_Than_The_One_Below_In_The_Cascades_The_Field_Allows` | Texels a side, tiles and cascades at each quality | CPU |
| 3DEngine | `GlobalIlluminationTests.cs:106` `Under_A_Uniform_Sky_Every_Face_Of_A_Probe_Reads_The_Same_Light_At_Each_Quality` | Every face of a probe under a uniform sky reads pi within 2%, Low and High | Vulkan |
| 3DEngine | `GlobalIlluminationTests.cs:226` `A_Red_Wall_Tints_The_Side_Of_A_White_Block_Facing_It` | A red wall tints a white block, red gain over 20 | Vulkan |
| 3DEngine | `GlobalIlluminationTests.cs:286` `An_Emissive_Panel_Lights_Its_Room_With_No_Light_In_It` | An emissive panel lights the floor of a closed room | Vulkan |
| 3DEngine | `GlobalIlluminationTests.cs:355` `A_Probe_In_The_Plane_Of_A_Ceiling_Stands_Below_It_And_Holds_Light` | A probe in a ceiling's plane moved into the room | Vulkan |
| 3DEngine | `GlobalIlluminationTests.cs:580` `A_Lamp_That_Casts_Shadows_Sends_No_Indirect_Light_Into_A_Room_Its_Walls_Close_It_Out_Of` | A lamp beside a closed room lends it no light | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Walled.cs:11` `A_Probe_Moved_Into_The_Stone_At_A_Lamps_Corner_Is_Walled_In_And_Holds_Nothing` | A probe pushed into stone at a lamp's corner walled in | Vulkan |
| 3DEngine | `GlobalIlluminationTests.WallFoot.cs:13` `A_Lamp_Outside_A_Closed_Room_Of_Thin_Walls_Lights_None_Of_Its_Floor_Along_A_Walls_Foot` | The floor at a wall's foot no brighter than beside it | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Board.cs:14` `A_Lit_Board_Outside_A_Closed_Room_Of_Thin_Walls_Lights_Nothing_Inside` | A room of thin walls dark within 15% of its measure | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Hidden.cs:13` `The_Foot_Of_A_Wall_Takes_None_Of_The_Light_The_Hidden_Probes_Under_Its_Floor_See` | Probes under the floor lend a wall's foot nothing | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Court.cs:13` `The_Floor_Of_A_Court_Narrower_Than_The_Probes_Apart_Takes_The_Sky_Through_Its_Opening` | A court's floor takes the sky past roofed probes, over 0.26 of the open | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Carried.cs:15` `A_Floor_Under_A_Solid_Slab_In_Four_Cascades_Takes_The_Sky_Past_Its_Sides_And_Its_Underside_Reflects_Little` | Carried rays light a floor and leave the underside dark | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Cycle.cs:15` `A_Carried_Probes_Cycle_Is_Kept_Whole_Only_Where_Its_Rays_Move` | The carried cycle's memory at High with ray queries | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Cycle.cs:44` `A_Carried_Probes_Share_Of_The_Sky_Is_Kept_Through_Its_Cycle_As_Its_Light_Is` | The merged sky still over 32 frames | GPU rays |
| 3DEngine | `GlobalIlluminationTests.Far.cs:13` `A_Ray_That_Leaves_The_Counted_Cascades_Meets_A_Wall_In_The_Far_Cascade_Before_The_Sky` | The far cascade dims a face toward a far black wall | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Far.cs:49` `The_Far_Cascades_Probes_Trace_A_Part_Of_Them_A_Frame_And_All_On_A_Frame_It_Moved` | The far cascade traced in parts and whole when it moves | Vulkan |
| 3DEngine | `GlobalIlluminationTests.FarMove.cs:12` `A_Surface_In_The_Far_Cascades_Band_Keeps_Its_Light_As_The_Cascade_Moves` | A block's underside holds across the far cascade's move | Vulkan |
| 3DEngine | `CascadeEdgeTests.cs:281` `The_Last_Cascades_Rays_Meet_The_Walls_Of_A_Closed_Room_Wider_Than_Four_Times_Its_Spacing` | No texel of the last cascade marked sky in a closed room | Vulkan |
| 3DEngine | `CascadeEdgeTests.Corridor.cs:13` `A_Closed_Corridors_Far_End_Takes_None_Of_The_Sky_Its_Probes_Outside_It_Saw` | A closed corridor's far end dark against an open one | Vulkan |
| 3DEngine | `CascadeEdgeTests.Shift.cs:12` `A_Closed_Corridors_Light_Holds_While_The_Slabs_A_Walk_Brings_Into_The_Field_Are_Built` | A corridor's light holds while a walk's slabs are built | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Gloss.cs:16` `A_Probe_Facing_A_Sunlit_Wall_Reads_Its_Gloss_Within_Its_Measured_Difference_From_The_Reference` | A face toward a glossy wall against the reference, first bounce | GPU rays |
| 3DEngine | `GlobalIlluminationTests.Gloss.cs:62` `A_Closed_Room_Of_Smooth_White_Walls_Under_A_Lamp_Holds_Its_Light_Steady` | A closed room of smooth white walls under a lamp holds its light steady | GPU rays |
| 3DEngine | `GlobalIlluminationTests.Reach.cs:13` `A_Probes_Rays_In_A_Closed_Room_Reach_Where_The_References_Paths_First_Meet_Its_Walls` | Hits within 25% of the reference's first hit | GPU rays |
| 3DEngine | `GlobalIlluminationTests.LampMarch.cs:13` `A_Lamp_Just_Outside_A_Thin_Wall_Is_Hidden_From_Most_Of_The_Surfaces_A_Probe_Inside_Meets` | A lamp just outside a thin wall hidden from the surfaces a probe inside meets | GPU rays |
| 3DEngine | `GlobalIlluminationTests.Lintel.cs:13` `A_Probe_Whose_Ray_Meets_A_Lintel_Past_Its_Interval_Takes_No_Sky_The_Probes_Above_See_Under_It` | A face under a lintel 0.8 to 1.1 of the reference | GPU rays |
| 3DEngine | `GlobalIlluminationTests.Lintel.cs:61` `A_Probe_Under_The_Lintel_Holds_Its_Face_Still_While_The_View_Is_Held` | The spread of twelve readings under a lintel | GPU rays |
| 3DEngine | `GlobalIlluminationTests.Passage.cs:13` `A_Probe_Deep_In_A_Narrow_Walled_Passage_Reads_The_Sunlit_Court_Through_Its_Opening_As_The_Reference_Does` | A face in a passage 0.75 to 1.25 of the reference | GPU rays |
| 3DEngine | `GlobalIlluminationTests.Shadowed.cs:13` `A_Probe_Over_A_Walls_Long_Shadow_In_Coarse_Cells_Reads_The_Shaded_Floor_As_The_Reference_Does` | A face in a wall's long shadow 0.75 to 1.25 of the reference | GPU rays |
| 3DEngine | `GlobalIlluminationTests.Threshold.cs:14` `The_Wall_Above_A_Threshold_On_A_Thin_Floor_Reads_A_Lamp_Under_The_Floor_Within_Its_Measured_Leak` | A thin floor's leak within 15% of its measure | GPU rays |
| 3DEngine | `GlobalIlluminationTests.Frames.cs:13` `The_Indirect_Light_Goes_Within_Two_Frames_Of_The_Light_That_Went_Out` | The indirect light goes within two frames of the light that went out | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Frames.cs:70` `A_Lamp_Put_Out_In_A_Room_Apart_Leaves_A_Corridor_Lit_By_Indirect_Light_Alone_As_It_Was` | A corridor holds when a far room's lamp goes out | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Frames.cs:134` `A_Lamp_Carried_Away_Takes_Its_Indirect_Light_With_It` | The light left where a carried lamp stood halves within four frames | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Frames.cs:190` `Indirect_Light_Follows_A_Lamp_Brought_Into_The_Room_Within_Two_Frames` | A lamp brought in lights within two frames | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Dimming.cs:12` `A_Lamp_Dimming_Slowly_Dims_The_View_Evenly_From_One_Frame_To_The_Next` | A dimming light falls without a step three times the median | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Gain.cs:27` `A_Floor_Lit_By_A_Panel_Takes_More_Of_Its_Walls_Light_As_The_Bounce_Gain_Rises` | Each bounce's gain adds light | Vulkan |
| 3DEngine | `BounceViewTests.cs:26` `Each_View_Draws_Its_Own_Picture_And_Each_Part_Left_Out_Changes_The_Frame` | Each view and each part left out changes the frame | Vulkan |
| 3DEngine | `GlobalIlluminationPluginTests.cs:66` `A_Program_Built_Without_The_Plugin_Draws_As_With_Global_Illumination_Off_And_Holds_None_Of_It` | The plugin's frame differs from global illumination off, its absence equals it | Vulkan |
| BevyCSharp | `GlobalIlluminationTests.cs:21` `EachQualityTracesItsCascadesAndDirections` | Texels a side at each quality, 256 rays a probe | CPU |
| BevyCSharp | `GlobalIlluminationTests.cs:114` `AProbeBeforeASunlitRedWallTakesItsRedOnTheFaceTowardIt` | A probe's face toward a red wall reads red | Bevy |
| BevyCSharp | `GlobalIlluminationTests.cs:140` `AProbeInsideASolidBlockHoldsNothing` | A probe inside a solid holds nothing | Bevy |
| BevyCSharp | `GlobalIlluminationTests.cs:168` `AFarCascadeHoldsAWallPastTheCountedOnes` | The far cascade dims a face toward a far black wall, 3DEngine's bounds | Bevy |
| BevyCSharp | `GlobalIlluminationTests.cs:196` `AnEmissivePanelLightsTheProbesOfAClosedRoom` | An emissive panel lights a probe's face | Bevy |
| BevyCSharp | `GlobalIlluminationTests.cs:212` `AStripThinnerThanACellLendsItsLightToTheCellsBesideIt` | A strip the field emits lights a probe's face | Bevy |
| BevyCSharp | `GlobalIlluminationTests.cs:322` `ARedWallTintsAWhiteBlocksSideTowardIt` | A red wall tints a white block's side toward it in the picture | Bevy |
| BevyCSharp | `GlobalIlluminationTests.ProbeReading.cs:27` `AProbeInAClosedRoomHoldsItsReferencesLightAtEachBounceItIsHeldAt` | A probe against the reference at 0, 2 and all bounces within 10% | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Bounces.cs:30` `AClosedRoomReadsItsLightPastTheDirectAsItsReferenceThroughEveryBounce` | Closed rooms of 4, 8 and 16, each face within 15% past the direct | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Bounces.cs:63` `AClosedRoomOfSmoothWhiteWallsReadsItsLightPastTheDirectAsItsReference` | Smooth white rooms do not run away | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Bounces.cs:96` `AClosedCornellBoxOnTheWorldsProbesReadsItsLightPastTheDirectAsItsReference` | A closed Cornell box within 6% | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Gloss.cs:75` `ASmoothRoomsProbeReflectsItsSecondBounceAlongEachRaysMirrorWay` | The second bounce off gloss within 12% | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Corridor.cs:27` `ACorridorNarrowerThanTheProbesAboveReadsItsLightPastTheDirectAsItsReference` | Each face of a corridor within 30%, carried rays | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Air.cs:21` `AWallInShadeTakesTheFloorsLightAsItsReferenceDoesUnderALowSun` | A wall in shade takes the floor's light under a low sun through the air, within 30% | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Reach.cs:20` `AFarWallInShadeTakesTheYardsLightAtMedium` | A yard's far wall within 30% at Medium | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Reach.cs:40` `AFarWallInShadeTakesTheYardsLightAtLow` | The same at Low | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Seam.cs:26` `AnAisleReadsAlikeWithACascadesSideOnTheWallBehindTheEye` | Faces read from two eyes within 0.04 | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.ThinWalls.cs:28` `ALampOutsideAThinWalledRoomLightsNothingUnderItsWalls` | A room of thin walls dark, each face under 0.005 | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.ThinWalls.cs:48` `ALampOutsideAThinWalledRoomIsHiddenFromTheHitsBesideItsWalls` | The same with the screen probes, under 0.04 | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.ThinWalls.cs:90` `ALitBoardOutsideAThinWalledRoomLightsNothingInside` | The same with the board, under 0.005 | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.ThinWalls.cs:116` `AProbeOnTheMiddleOfAWallThinnerThanACellHoldsNoLight` | A probe on the middle of a wall thinner than a cell holds no light | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Foot.cs:33` `AWallsFootOverAThinFloorReadsEvenWithALampUnderTheFloor` | A wall's foot over a thin floor even, warmth within 0.05 | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Off.cs:17` `AnAppWithGlobalIlluminationOffMakesNoneOfItsPrograms` | Off makes no program and traces nothing | Bevy |
| BevyCSharp | `GlobalIlluminationTests.Off.cs:49` `GlobalIlluminationTurnedOffTracesNoMoreAndLetsItsFieldGo` | Tracing stops and the plan goes | Bevy rays |

- **BevyCSharp has no test of light that changes over frames**, 3DEngine's
  `GlobalIlluminationTests.Frames.cs:13`, `:70`, `:134`, `:190` and
  `GlobalIlluminationTests.Dimming.cs:12`. The fault in BevyCSharp's `follow` lies in that gap. The
  probe's own light it sums feeds the kept share alone, which reads 1 while the light holds still,
  and every BevyCSharp test reads a light held still. A port of `Frames.cs:13` at Low closes it.
- **BevyCSharp has the code and no test** for every face reading pi under a uniform sky (3DEngine's
  `GlobalIlluminationTests.cs:106`), which its `gi_ambient` states, the walled-in quarter
  (3DEngine's `GlobalIlluminationTests.Walled.cs:11`) and a probe moved off a ceiling's plane
  (3DEngine's `GlobalIlluminationTests.cs:355`).
- **3DEngine has the code and no test** for a probe held against the reference at a chosen number of
  bounces (BevyCSharp's `GlobalIlluminationTests.ProbeReading.cs:27`), where 3DEngine holds the
  first bounce alone, and for a probe on the middle of a wall thinner than a cell (BevyCSharp's
  `GlobalIlluminationTests.ThinWalls.cs:116`). BevyCSharp's sun through the camera's air
  (`GlobalIlluminationTests.Air.cs:21`) has no 3DEngine test.
- **The texels a side** at High differ, 8 in 3DEngine's second cascade and 16 in BevyCSharp's, each
  stated by a test (3DEngine's `GlobalIlluminationTests.cs:28` against BevyCSharp's
  `GlobalIlluminationTests.cs:21`).

### The probes' readers and the cascades' bands

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `GlobalIlluminationTests.cs:324` `A_Ceiling_In_A_Plane_Of_Probes_Takes_The_Light_Of_The_Probes_Below_It` | A ceiling takes red from the probes | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Far.cs:115` `The_Screen_Probes_Stand_In_The_Counted_Cascades_And_A_Floor_Past_Them_Takes_The_Far_Probes` | Near probes lit and far probes dark past the field | Vulkan |
| 3DEngine | `CascadeEdgeTests.cs:31` `The_Floors_Light_Fades_Across_Each_Cascades_End_Rather_Than_Stepping` | No step between rows across a cascade's band under an ambient sky, under 6% | Vulkan |
| 3DEngine | `CascadeEdgeTests.cs:45` `The_Floors_Reflection_Of_The_Sky_Fades_Across_The_Fields_End_Rather_Than_Stepping` | The same under an environment map | Vulkan |
| 3DEngine | `CascadeEdgeTests.cs:106` `A_Closed_Rooms_Walls_Stay_Dark_At_Every_Heading_Of_An_Eye_Inside` | A closed room dark at twelve headings | Vulkan |
| 3DEngine | `CascadeEdgeTests.cs:167` `A_Face_Before_The_Eye_Stays_Dark_Where_The_Eye_Stands_Nearest_The_Last_Cascades_Side` | A face before the eye stays dark where the eye stands nearest the last cascade's side | Vulkan |
| 3DEngine | `CascadeEdgeTests.cs:230` `A_Closed_Rooms_Floor_Stays_Hidden_From_An_Ambient_Light_Before_The_Last_Cascade_Is_Built` | The first three frames under twice the settled light | Vulkan |
| 3DEngine | `CascadeEdgeTests.Distant.cs:14` `A_Floor_And_A_Wall_Along_It_Take_Their_Light_Across_The_Fields_Edge_With_No_Step` | Floor and wall even past the field, steps under 1% and 1.5% | Vulkan |
| BevyCSharp | `GlobalIlluminationTests.cs:361` `AnOpenFloorReadsAsBevysAmbientLightGivesIt` | An open floor reads Bevy's ambient within 4 levels | Bevy |
| BevyCSharp | `GlobalIlluminationTests.cs:390` `AnOpenFloorReadsAsAnEnvironmentMapLightsIt` | The same under an environment map within 6 | Bevy |
| BevyCSharp | `GlobalIlluminationTests.cs:420` `AClosedRoomUnderAnEnvironmentMapIsDarkInside` | A closed room under an environment map under a quarter of off | Bevy |
| BevyCSharp | `GlobalIlluminationTests.Ambient.cs:24` `AnOpenFloorUnderTheSkyReadsItsReferenceNearTheEyeAndAtAGrazingAngle` | An open floor near and grazing within 5% of the reference | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Probes.cs:24` `TheLightBevysLightProbesGaveIsTakenBackAsTheyGaveIt` | The light of Bevy's light probes taken back as they gave it, within 30% | Bevy rays |

BevyCSharp has the bands' fade and no test of a step across a band, which 3DEngine's
`CascadeEdgeTests.cs:31`, `:45`, `:106`, `:167`, `:230` and `CascadeEdgeTests.Distant.cs:14` hold.
BevyCSharp's take-back of Bevy's ambient light and light probes (BevyCSharp's
`GlobalIlluminationTests.cs:361`, `:390`, `:420` and `GlobalIlluminationTests.Probes.cs:24`) is its
own.

### Light through a surface

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `GlobalIlluminationTests.Foliage.cs:16` `A_Floor_Under_A_Slab_That_Lets_Light_Through_Takes_The_Sky_Light_Its_Share_Lets_Through` | A slab letting all, half or none of the light through, Low and High | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Foliage.cs:72` `A_Floor_Under_A_Slab_Letting_All_Light_Through_Reflects_The_Sky_As_In_The_Open` | The sky reflected under a slab letting all through | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Foliage.cs:106` `A_Floor_Under_Leaves_Past_The_Screen_Probes_Reach_Takes_The_Sky_Light_They_Let_Through` | Light past the screen probes' reach under leaves | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Foliage.cs:156` `A_Floor_Under_A_Canopy_Of_Leaf_Blocks_Takes_The_Sky_Light_Their_Faces_Let_Through` | A canopy of leaf blocks within measured bands | Vulkan |
| BevyCSharp | `GlobalIlluminationTests.LightThrough.cs:27` `AFloorUnderASlabThatLetsLightThroughTakesTheSkyLightItsShareLetsThrough` | A slab letting all, half or none through, Low and High | Bevy |
| BevyCSharp | `GlobalIlluminationTests.UniformSky.cs:22` `AGrassPatchUnderAUniformSkyReadsItsReference` | Grass within 15% of the reference | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.UniformSky.cs:56` `ALoneAspensLeavesUnderAUniformSkyReadTheirReference` | An aspen within 25% | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.UniformSky.cs:78` `ALoneAspenLitFromBehindLetsThroughTheSunItsReferenceDoes` | A backlit aspen within 10% | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.UniformSky.cs:100` `AForestFloorUnderAUniformSkyReadsItsReference` | A forest floor within 30% | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.CrownScatter.cs:32` `ProbesAboutAFrontLitCrownHoldTheLightItsLeavesScatter` | Probes in a crown 0.75 to 1.25 of the reference | Bevy rays |

BevyCSharp has no test of the sky reflected under a slab that lets all light through (3DEngine's
`GlobalIlluminationTests.Foliage.cs:72`) or of light past the screen probes' reach under leaves
(3DEngine's `GlobalIlluminationTests.Foliage.cs:106`). 3DEngine reads its foliage against measured
bands, where BevyCSharp reads grass, aspens, a forest floor and a crown against a reference traced
in the run (`GlobalIlluminationTests.UniformSky.cs`, `GlobalIlluminationTests.CrownScatter.cs`).

### Rough reflection

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `RoughReflectionTests.cs:88` `A_Rough_Metal_Wall_Reflects_The_Light_The_Probes_Hold_By_Its_Specular_Share` | The probes hold a metal box's light within 12.1%, the wall's specular share within 0.5% | GPU rays |
| 3DEngine | `GlobalIlluminationTests.Feet.cs:13` `The_Foot_Of_A_Rough_Wall_Reflects_None_Of_The_Sky_The_Probes_Under_Its_Floor_See` | A rough wall's foot darker than above | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Frames.cs:484` `A_Room_Sealed_From_The_Sky_Reflects_None_Of_It_Where_An_Open_Floor_Reflects_It` | An open floor takes the sky's blue, a sealed room none | Vulkan |
| 3DEngine | `GlobalIlluminationTests.cs:506` `A_Polished_Floor_Reflects_A_Red_Block_On_It_Through_The_Window_Where_Global_Illumination_Is_On` | A mirror floor reflects red | Vulkan |
| 3DEngine | `GlobalIlluminationTests.cs:537` `A_Reflection_Of_A_Surface_The_Frame_Before_Hid_Shows_The_Surface_And_Not_What_Hid_It` | A reflection of a surface the frame before hid shows that surface | Vulkan |
| 3DEngine | `GlobalIlluminationTests.cs:650` `A_Mirror_Shows_A_Block_Its_Lamp_Is_Shut_Away_From_In_The_Dark` | A mirror shows a block its lamp is shut away from in the dark | Vulkan |
| 3DEngine | `GlobalIlluminationTests.cs:729` `A_Mirror_Reflects_A_Block_Behind_The_Camera_Through_The_Field` | A mirror reflects a block behind the camera through the field | Vulkan |
| BevyCSharp | `GlobalIlluminationTests.cs:456` `AMirrorFloorReflectsARedWallStandingOnIt` | A mirror floor reflects red | Bevy |
| BevyCSharp | `GlobalIlluminationTests.cs:494` `AClosedRoomReflectsNoSky` | A closed room reflects no sky, under a quarter of off | Bevy |
| BevyCSharp | `GlobalIlluminationTests.ThinWalls.cs:72` `ALampOutsideAThinWalledRoomOfGlossyFacesIsReflectedNowhereInside` | A glossy room of thin walls dark | Bevy rays |

BevyCSharp has no test of the specular share to half a percent (3DEngine's
`RoughReflectionTests.cs:88`), its own bounds being loose (BevyCSharp's
`GlobalIlluminationTests.cs:494`).

### The screen probes

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `GlobalIlluminationTests.cs:133` `A_Floor_Seen_At_A_Slant_Takes_The_Light_Of_The_Screens_Probes_Along_Its_Slope` | No steep fall over four rows from the screen probes | Vulkan |
| 3DEngine | `GlobalIlluminationTests.cs:168` `The_Screens_Probes_Hold_Light_On_A_Floor_Past_The_Fields_First_Cascade` | Floor probes past the first cascade hold light | Vulkan |
| 3DEngine | `GlobalIlluminationTests.cs:458` `A_Room_Drawn_Into_A_Render_Texture_Takes_Global_Illumination_As_The_Window_Does` | A window and a texture alike with and without the screen probes | Vulkan |
| 3DEngine | `GlobalIlluminationTests.ScreenProbes.cs:13` `Low_Leaves_The_Screens_Probes_Out_And_Their_Debug_Toggle_Leaves_Them_Out_At_Any_Quality` | No screen probes at Low, the cascades alone light | Vulkan |
| 3DEngine | `GlobalIlluminationTests.ScreenProbes.cs:168` `A_Screen_Probe_Fed_A_Value_That_Is_Not_A_Number_Leaves_The_Light_It_Blends_Into_Finite` | A NaN fed three frames leaves no black | Vulkan |
| 3DEngine | `GlobalIlluminationTests.ScreenProbes.cs:225` `The_Back_Of_A_Two_Sided_Wall_Takes_The_Screens_Probes_As_Its_Front_Does` | A wall's back face changes as its front | Vulkan |
| 3DEngine | `GlobalIlluminationTests.History.cs:13` `Leaving_The_Screen_Probes_Frames_Before_Out_Of_A_Still_View_Keeps_Its_Light` | History off within 8% of kept | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Frames.cs:234` `Global_Illumination_Holds_Still_As_The_Camera_Slides` | A sliding camera adds under 0.15 levels a frame | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Frames.cs:311` `In_A_Still_View_Each_Screen_Probe_Takes_The_Frame_Befores_Light_From_Where_It_Stood` | Every held probe's alpha 1.9375 | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Frames.cs:352` `The_Light_That_Bounces_Settles_By_The_Fifth_Frame_After_A_Walk_Or_A_Turn` | A walk and a turn change under 0.12 levels by the fifth frame | Vulkan |
| BevyCSharp | `GlobalIlluminationTests.ScreenProbes.cs:19` `AFrameWithTheScreensProbesLeftOutDrawsTheCascadesLightAlone` | Probes unread equal probes left out, pixel for pixel | Bevy |
| BevyCSharp | `GlobalIlluminationTests.ScreenProbes.cs:165` `AScreenProbeFedAValueThatIsNotANumberLeavesTheLightItBlendsIntoFinite` | A NaN leaves no black | Bevy |
| BevyCSharp | `GlobalIlluminationTests.ProbeReading.cs:103` `AScreenProbeInAClosedRoomSumsItsReferencesLightAlongItsWays` | A screen probe's sum within 10% of the reference | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.ScreenTile.cs:16` `TheScreenProbesTileHoldsUpTo1080RowsAndGrowsWithTheHeightPastThem` | The tile from the picture's height | CPU |
| BevyCSharp | `GlobalIlluminationTests.ScreenTile.cs:42` `APicturesScreenProbesStandEveryTileItsHeightGives` | The tile drawn at 8 or 11 | Bevy |
| BevyCSharp | `BounceRoomsTests.cs:178` `WithTheScreenProbesAViewReadsTheSameWithItsCameraMovedByAFewFloatSteps` | A camera moved by a few float steps changes the picture under 0.001 | Bevy rays |

- **BevyCSharp has the code and no test** for the filter's frame before (3DEngine's
  `GlobalIlluminationTests.Frames.cs:234`, `:311`, `:352` and
  `GlobalIlluminationTests.History.cs:13`), a probe past the first cascade (3DEngine's
  `GlobalIlluminationTests.cs:168`), a floor's slope (`:133`) and the back of a two-sided wall
  (3DEngine's `GlobalIlluminationTests.ScreenProbes.cs:225`).
- **3DEngine has the code and no test** for probes left unread against probes left out, pixel for
  pixel (BevyCSharp's `GlobalIlluminationTests.ScreenProbes.cs:19`), and a camera moved by a few
  steps of the float (BevyCSharp's `BounceRoomsTests.cs:178`).

### The glow lights

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `GlowLightsTests.cs:28` `An_Emitter_No_Longer_Than_Twice_The_First_Cascades_Spacing_Is_Carried` | Which emitters are carried | CPU |
| 3DEngine | `GlowLightsTests.cs:41` `The_Lights_Written_Are_The_Nearest_The_Eye_With_Their_Boxes_And_Reach` | The 64 nearest written with their reach and box | CPU |
| 3DEngine | `GlowLightsTests.cs:68` `Touching_Emitters_That_Give_Off_The_Same_Light_Are_One_Light` | A gathered light's share | CPU |
| 3DEngine | `GlowLightsTests.cs:90` `A_Mesh_Of_Many_Blocks_Is_Cut_Into_Its_Parts_With_The_Faces_They_Show` | A mesh split into its parts | CPU |
| 3DEngine | `GlowLightsTests.cs:143` `An_Emissive_Rods_Light_On_The_Floor_Falls_With_No_Step` | A rod's light within 5% of a traced table | Vulkan |
| 3DEngine | `GlowLightsTests.cs:165` `An_Emissive_Block_Lights_The_Floor_Around_It_As_A_Path_Traced_Reference_Does` | An emitter's floor rings within 6% of a traced table at each quality | Vulkan |
| 3DEngine | `GlowLightsTests.cs:185` `An_Emissive_Block_That_Is_Part_Of_A_Mesh_Of_Many_Lights_The_Floor_As_A_Block_Alone_Does` | A mesh of two blocks read from 40 blocks as from 5 | Vulkan |
| 3DEngine | `GlowLightsTests.cs:268` `A_Row_Of_Lamps_Lights_The_Floor_Under_A_Block_Floating_Below_It_As_Beside_It` | The floor under a floating block | Vulkan |
| 3DEngine | `GlowLightsTests.cs:343` `A_Wall_Sees_Four_Joined_Lamps_Past_A_Pillar_As_A_Path_Traced_Reference_Does` | A pillar's shares within 0.05 of the reference's | Vulkan |
| 3DEngine | `GlowLightsTests.cs:420` `A_Room_Lit_By_Six_Small_Emitters_Reads_As_A_Path_Traced_Reference_Does` | One and six emitters within 10% of a live reference | GPU rays |
| 3DEngine | `GlowLightsTests.cs:483` `A_Bulb_Hung_High_Lights_The_Floor_Under_It_As_Its_Lower_Face_Does` | A bulb within 20% of its closed form | Vulkan |
| 3DEngine | `GlowPillarTests.cs:25` `A_Lamps_Light_Past_A_Pillars_Edge_Rises_On_The_Wall_Behind_Without_A_Step` | No steep step beside a pillar | Vulkan |
| 3DEngine | `GlowPenumbraTests.cs:30` `A_Turn_Of_A_Few_Pixels_Moves_The_Penumbra_With_The_World` | No strays in a penumbra and its settling | Vulkan |
| 3DEngine | `GlowPenumbraTests.cs:89` `With_No_Screen_Probe_The_Floor_Behind_The_Wall_Stays_In_Its_Shadow` | The model pass's own march with the screen probes off | Vulkan |
| 3DEngine | `GlowFarFromOriginTests.cs:145` `A_Glow_Lights_Light_On_A_Wall_Far_From_The_Origin_Is_As_Smooth_As_At_The_Origin` | A lantern pocket far from the origin reads as near it | Vulkan |
| 3DEngine | `GlowStillTests.cs:74` `A_Still_View_Holds_Its_Glow_Lights_Light_From_Frame_To_Frame` | The blend held still over 30 frames | Vulkan |
| 3DEngine | `GlobalIlluminationTests.ScreenProbes.cs:51` `At_Low_A_Glow_Light_Is_Read_From_Probes_Standing_For_It_Alone` | Probes for the glow lights alone, marched against read within 6 | Vulkan |
| 3DEngine | `GlobalIlluminationTests.ScreenProbes.cs:105` `Far_From_The_Worlds_Origin_Glow_Lights_Leave_No_More_Of_The_Land_Black_Than_Near_It` | A village's torches far and near | Vulkan |
| 3DEngine | `GlobalIlluminationTests.Walled.cs:67` `Two_Blocks_Meeting_Along_An_Edge_Close_It_To_A_Glow_Lights_Light` | A glowstone walled in lends nothing past the wall | Vulkan |
| 3DEngine | `GlobalIlluminationTests.cs:199` `An_Emissive_Strip_Thinner_Than_A_Cell_Lights_Its_Room` | A strip carried as a glow light lights the floor | Vulkan |
| BevyCSharp | `GlobalIlluminationTests.cs:227` `TouchingGlowingBlocksAreOneGlowLight` | A gathered light's share and a long block dropped | CPU |
| BevyCSharp | `GlobalIlluminationTests.cs:254` `AGlowingBlockLightsTheFloorBesideIt` | Glow lights on and off | Bevy |
| BevyCSharp | `GlobalIlluminationTests.ScreenProbes.cs:87` `WithTheScreensProbesLeftOutAGlowLightIsReadFromProbesStandingForItAlone` | Probes for the glow lights alone, 3DEngine's twin | Bevy |
| BevyCSharp | `GlobalIlluminationTests.ScreenProbes.cs:236` `FarFromTheWorldsOriginGlowLightsLeaveNoMoreOfTheLandBlackThanNearIt` | A village's torches far and near | Bevy |
| BevyCSharp | `GlobalIlluminationTests.ScreenProbes.cs:305` `AStillViewOfAWallLitByAGlowLightHoldsItsLightFromFrameToFrame` | A still glow room changes under 0.1 levels | Bevy |
| BevyCSharp | `GlobalIlluminationTests.Edges.cs:21` `TwoBlocksMeetingAlongAnEdgeCloseItToAGlowLightsLight` | A glowstone walled in lends nothing past the wall | Bevy |
| BevyCSharp | `GlobalIlluminationTests.Emitters.cs:20` `ARoomLitBySixEmittersReadsAsItsReference` | Six emitters, each face within 10% | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Emitters.cs:46` `ARoomLitByOneEmitterKeepsItsReading` | A room lit by one emitter keeps its reading, within 14% | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Emitters.cs:71` `ARoomLitByABallReadsAsItsReference` | A ball emitter within 10% | Bevy rays |

BevyCSharp has no unit test of `GlowLights.Write` (3DEngine's `GlowLightsTests.cs:41`), whose faint
level differs, 1/300 in 3DEngine and 0.001 in BevyCSharp, and none of the traced tables of a rod,
the floor's rings, a pillar and a bulb (`:143`, `:165`, `:343`, `:483`), the penumbra's tests or the
lantern pocket far from the origin (`GlowFarFromOriginTests.cs:145`). 3DEngine has no test of a
ball's share by its area (BevyCSharp's `GlobalIlluminationTests.Emitters.cs:71`).

### The light on the picture

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `BounceRoomsTests.Each.cs:53` `Each_Room_Reads_Within_Its_Measured_Difference_From_Its_Reference` | The nine bounce rooms at three qualities against their stored references, bounds 0.02 to 0.34 | GPU rays |
| 3DEngine | `BounceRoomsTests.cs:36` `The_Grazing_Floor_At_Low_Keeps_Its_Difference_With_A_Far_Cascade_Set` | The grazing floor at Low with a far cascade, under 0.06 | GPU rays |
| 3DEngine | `BounceReferenceTests.cs:180` `A_Lone_Slab_Under_A_Lamp_Reads_In_The_Frame_As_In_The_Reference` | A lone slab under a lamp against a reference traced in the run, within 1% | GPU rays |
| 3DEngine | `BounceReferenceTests.cs:218` `A_Slab_Colored_In_Its_Vertices_Reads_In_The_Frame_As_In_The_Reference` | The same with colors in the vertices | GPU rays |
| 3DEngine | `BounceReferenceTests.cs:253` `A_Slab_Whose_Colors_Are_Written_In_Place_Bounces_Them_Onto_A_Wall_As_The_Reference_Does` | The same with colors written in place | GPU rays |
| 3DEngine | `BounceReferenceTests.Textures.cs:11` `A_Slab_Of_Two_Colors_Reads_Each_In_The_Reference_As_In_The_Frame` | A two-color texture within 3% a pixel | GPU rays |
| 3DEngine | `BounceReferenceTests.Textures.cs:35` `A_Slab_Bent_By_Its_Normal_Map_Reads_In_The_Reference_As_In_The_Frame` | A normal map under the sun within 2% | GPU rays |
| 3DEngine | `ReferenceFrameTests.GlobalIllumination.cs:11` `A_Cornell_Box_Lit_By_Light_That_Bounces_Matches_Its_Reference` | The Cornell box at High against a stored picture, 5% of pixels may differ | Vulkan |
| 3DEngine | `ReferenceFrameTests.GlobalIllumination.cs:49` `A_Room_Of_A_Game_Lit_By_The_Sun_Through_Its_Windows_Matches_Its_Reference` | A library lit by the sun and an environment | Vulkan |
| 3DEngine | `ReferenceFrameTests.GlobalIllumination.cs:90` `Glossy_Surfaces_Reflecting_Through_The_Field_Match_Their_Reference` | A glossy hall at High | Vulkan |
| 3DEngine | `ReferenceFrameTests.GlobalIllumination.cs:137` `A_Room_Of_Wick_Lit_By_Its_Lamp_And_A_Wick_Matches_Its_Reference` | A house at Low | Vulkan |
| 3DEngine | `LightTermsTests.cs:27` `A_Sunlit_Pixels_Light_Is_The_Sum_Of_Its_Terms_The_Suns_The_Largest` | The light's terms sum to the drawn light within 5% | Vulkan |
| 3DEngine | `BounceRoomsTests.cs:102` `The_Thin_Rooms_Corner_Takes_No_Light_From_The_Lamp_Outside` | The thin room's corner takes no light from the lamp outside | Vulkan |
| BevyCSharp | `BounceRoomsTests.cs:91` `EachRoomLiesWithinItsMeasuredDistanceFromItsReference` | The nine bounce rooms at three qualities against their stored references, the screen probes unread, bounds 0.02 to 0.41 | Bevy rays |
| BevyCSharp | `BounceRoomsTests.cs:151` `WithTheScreenProbesEachRoomLiesWithin3DEnginesBound` | The same with the screen probes, bounds 0.03 to 0.42 | Bevy rays |
| BevyCSharp | `BounceRoomsTests.cs:197` `ALitRoomsFloorLiesWithinTheBoundOfItsReference` | The Cornell floor within 16% of a reference traced in the run | Bevy rays |
| BevyCSharp | `GlobalIlluminationTests.Forward.cs:21` `ASurfaceDrawnForwardKeepsTheLightBevyGaveIt` | A surface drawn forward keeps the light Bevy gave it, within 5% | Bevy |
| BevyCSharp | `GlobalIlluminationTests.Forward.cs:140` `ALeafInShadeTakesTheProbesLightAsTheSurfaceBesideIt` | A leaf against a card within 12% | Bevy |
| BevyCSharp | `GlobalIlluminationTests.Forward.cs:161` `ALeafGivesBackTheLightBevyLetsThroughItFromAllAround` | A leaf gives back the light Bevy lets through it, under 0.35 of off | Bevy |

3DEngine's stored pictures (`ReferenceFrameTests.GlobalIllumination.cs`) run on lavapipe and allow
2% of the pixels, 5% for the Cornell box, more than 24 levels apart. The distances to the
path-traced references in both engines run on a GPU with ray queries alone.

### The contact shadow

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `ModelShadowTests.cs:59` `With_Contact_Shadows_Off_A_Shadow_The_Map_Holds_Keeps_Its_Hard_Edge_With_The_Field_On` | The field's contact shadow beside the map's hard edge and past its reach | Vulkan |
| 3DEngine | `ReferenceFrameTests.SceneField.cs:43` `A_Scene_Lit_Through_Its_Distance_Field_Matches_Its_Reference` | A scene lit through its field against a stored picture | Vulkan |
| BevyCSharp | `SceneFieldShadowTests.cs:89` `TheFieldsShadowIsAsDeepAsTheShadowMapsOwnWhereBothDarken` | The field's shadow as deep as the map's, within 4 levels | Bevy |
| BevyCSharp | `SceneFieldShadowTests.cs:108` `PastTheShadowMapTheFieldLeavesWhatTheMapHoldsAsItWas` | Past the map the field leaves what the map holds | Bevy |
| BevyCSharp | `SceneFieldShadowTests.cs:130` `ASunUnderTheHorizonHasNoLightTakenBackThroughTheField` | A sun under the horizon takes no light back | Bevy |
| BevyCSharp | `SceneFieldShadowTests.cs:205` `AProgramsOwnDrawOnTheCameraStaysBesideTheFieldsShadows` | A program's own draw stays beside the field's shadows | Bevy |

3DEngine has no test of the field's shadow as deep as the map's in levels (BevyCSharp's
`SceneFieldShadowTests.cs:89`) or of a sun under the horizon (`:130`). BevyCSharp has no test that
the field darkens the ground past the map's reach (3DEngine's `ModelShadowTests.cs:59`) and no
stored picture of a scene lit through its field (`ReferenceFrameTests.SceneField.cs:43`).

### The reference

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `BounceReferenceTests.cs:28` `Inside_A_Closed_Emissive_Box_The_Reference_Shows_Its_Light_Over_One_Less_Its_Color` | An emissive box reads 1/(1-rho) within 2% | GPU rays |
| 3DEngine | `BounceReferenceTests.cs:40` `In_The_Emissive_Box_No_Bounce_Shows_The_Walls_Own_Light_And_One_Bounce_Adds_Their_Color` | 0 bounces read 1 and one bounce 1+rho | GPU rays |
| 3DEngine | `BounceReferenceTests.cs:52` `A_Probe_Inside_The_Emissive_Box_Has_Its_Light_Over_One_Less_Its_Color_Arriving_From_Every_Way` | A probe in the emissive box within 3% | GPU rays |
| 3DEngine | `BounceReferenceTests.cs:74` `A_Screen_Probe_Inside_The_Emissive_Box_Reads_Each_Ray_As_The_Reference_Does` | A screen probe's rays against the reference | GPU rays |
| 3DEngine | `BounceReferenceTests.cs:113` `A_Probe_Beside_A_Block_Carried_As_A_Glow_Light_Reads_None_Of_Its_Emission_In_The_Reference` | A glow block left out of the probes' reference and kept in the picture's | GPU rays |
| 3DEngine | `BounceReferenceTests.cs:316` `A_Floor_Under_A_Slab_Letting_Half_The_Light_Through_Reads_In_The_Reference_The_Sky_Its_Depth_Keeps` | A slab letting half through keeps 0.336 of the sky | GPU rays |
| 3DEngine | `BounceReferenceTests.Gloss.cs:15` `A_Smooth_Metal_Plane_Under_The_Sky_Reads_The_Sky_Times_Its_Fresnel` | A metal plane under the sky 0.87 to 1.0 | GPU rays |
| 3DEngine | `BounceReferenceTests.Gloss.cs:29` `A_Rough_Dielectric_At_A_Grazing_View_Reads_Fresnels_Share_Of_The_Sky_Over_Its_Diffuse` | A rough dielectric at a grazing view 0.59 to 0.72 | GPU rays |
| 3DEngine | `BounceReferenceTests.Gloss.cs:43` `A_Metal_Wall_Under_The_Sun_Mirrors_It_Onto_The_Floor_Beside_It_Where_Every_Face_Is_Glossy` | A metal wall mirrors the sun onto the floor | GPU rays |
| 3DEngine | `RoughReflectionTests.cs:71` `In_A_Box_Of_Metal_The_Reference_Reflects_The_Boxs_Light_Where_Its_Specular_Share_Is_Traced` | A metal box with and without specular | GPU rays |
| BevyCSharp | `GiReferenceTests.cs:160` `AReferenceTracedAfterAnotherHoldsItsOwnPathsAlone` | A second reference holds its own paths | Bevy rays |
| BevyCSharp | `GiReferenceTests.cs:205` `TheReferencesSunIsDimmedThroughTheAirAsThePictures` | The reference's sun dimmed through the air as the picture's, within 8% | Bevy rays |
| BevyCSharp | `GiReferenceTests.cs:266` `ACutOutCardLetsTheReferencesPathsThroughWhereItsAlphaIsUnderTheCutoff` | Cut-out cards | Bevy rays |
| BevyCSharp | `GiReferenceTests.cs:355` `AReferenceMeetsTheLevelOfAMeshItsDistanceFromTheEyeGives` | The reference meets a mesh where its distance says | Bevy rays |
| BevyCSharp | `GiReferenceTests.cs:416` `AGrassPatchReadsInTheReferenceTheLightThePictureGivesIt` | Grass 0.85 to 1.15 | Bevy rays |
| BevyCSharp | `GiReferenceTests.cs:540` `ACutOutCardsNormalMapTurnsItsLightInTheReferenceAsInThePicture` | A normal map 0.9 to 1.1 | Bevy rays |
| BevyCSharp | `GiReferenceTests.cs:616` `ASmoothMetalPlaneReflectsTheSkyInTheReference` | A metal plane under the sky 0.85 to 1.05 | Bevy rays |
| BevyCSharp | `GiReferenceTests.cs:633` `ARoughDielectricAtAGrazingViewReadsFresnelsShareOfTheSkyOverItsDiffuse` | A rough dielectric at a grazing view 0.59 to 0.72 | Bevy rays |
| BevyCSharp | `GiReferenceTests.cs:701` `TheReferenceBindsTheMasksTheDevicesTexturesAStageLeaveRoomFor` | The reference binds the masks | CPU |

The smooth metal plane and the rough dielectric are twins, the same plane 4 by 4 read at 128 by 128,
the dielectric's bound the same in both, 0.59 to 0.72, and the metal's 0.87 to 1.0 in 3DEngine
against 0.85 to 1.05 in BevyCSharp. Each tests its own tracer and neither tests one against the
other.

### The views

| Engine | Test | What it holds | Device |
|---|---|---|---|
| 3DEngine | `ReferenceFrameTests.SceneField.cs:55` `A_Cascade_Of_The_Distance_Field_Matches_Its_Reference` | A cascade of the field against a stored picture | Vulkan |
| BevyCSharp | `SceneFieldShadowTests.cs:22` `TheWindowsSliceAcrossABoxIsRedInsideBlackAtItsFacesAndGrayBeyond` | A slice across a box red inside, black at its faces, gray beyond | Bevy |
| BevyCSharp | `GiReferenceTests.cs:31` `ThePictureIsReadWithGlobalIlluminationOff` | The picture read before and after with global illumination off | Bevy |

Each engine's view is its own tool, held by its own test.

## Rooms both engines draw alike

The nine bounce rooms are built alike in both, in 3DEngine's `BounceRoomsTests.cs:611-748` and its
example `3DEngine.Examples/Shaders/ShadersBounceRooms.cs:27-170`, and in BevyCSharp's
`BounceRoomsTests.cs:301-475`, which says it took them from 3DEngine (BevyCSharp's
`BounceRoomsTests.cs:20-24`). They hold the same geometry, colors, roughness of 0.5, lights' places,
ranges and shadows and the same sun with no sky, and both read them at 160 by 90 averaged to 80 by
45 against stored references of 4,096 paths a pixel by the same distance. The two engines' stored
references, each traced by its own tracer, agree. BevyCSharp's mean light over 3DEngine's is 0.971
to 1.026 a view, and the sum of their difference over 3DEngine's light is 0.001 to 0.049 a view, the
most in the corridor. They are the pair the two engines' pictures are compared by, and no test
compares them. What differs in how each lights them:

- **Units.** 3DEngine draws at an exposure of 1, a light's strength being the light a white surface
  facing it returns. BevyCSharp draws in Bevy's units at an exposure of `1 / (1.2 * 2^9.7)`, the
  sun's lux and the lamps' lumens worked from the same strengths (BevyCSharp's
  `BounceRoomsTests.cs:447-470`).
- **The diffuse term.** 3DEngine's model pass and reference take Lambert's, and Bevy's lighting and
  BevyCSharp's reference take Burley's.
- **The field.** 3DEngine centers it on each room's middle (`SetSceneFieldCenter`), and BevyCSharp
  lays it about the camera.
- **The direct light.** 3DEngine's model pass draws it, and in BevyCSharp Bevy's lighting draws it
  under `gi_light`'s take-back.
- **The screen probes.** 3DEngine's distance test reads them. BevyCSharp's `BounceRoomsTests.cs:91`
  leaves them unread and its `BounceRoomsTests.cs:151` reads them.
- **The bounds.** BevyCSharp's test named for 3DEngine's bound (`BounceRoomsTests.cs:151`) carries
  numbers of its own, 0.42 against 3DEngine's 0.164 in view 3 at High, 0.17 against 0.106 in view 4
  and 0.09 against 0.036 in view 7.

Room 1 is the Cornell box in both, and 3DEngine also draws it as its example and its stored picture
`cornell_box.png`. BevyCSharp's closed Cornell box (`GlobalIlluminationTests.Bounces.cs:239`) has no
3DEngine twin.

Smaller rooms built alike in the two engines' tests, each a pair whose readings can be set side by
side:

| Room | 3DEngine | BevyCSharp |
|---|---|---|
| a red wall, a white block and a floor, a lamp at 0.5, 2.6, 0.8 | `GlobalIlluminationTests.cs:233-238` | `GlobalIlluminationTests.ScreenProbes.cs:179-192` |
| a closed room 5.6 wide with a strip 2 by 0.06 by 0.06 | `GlobalIlluminationTests.cs:206-211` | `GlobalIlluminationTests.ScreenProbes.cs:104-117` |
| a room of thin walls in cells of 0.5, a lamp outside and a board | `GlobalIlluminationTests.WallFoot.cs:44-49`, `GlobalIlluminationTests.Board.cs:42-48` | `GlobalIlluminationTests.ThinWalls.cs:280-308` |
| a stone room with a stepped wall and a glowstone at 15, 5, -14 | `GlobalIlluminationTests.Walled.cs:94-105` | `GlobalIlluminationTests.Edges.cs:41-53` |
| a black wall 200 wide and 40 ahead | `GlobalIlluminationTests.Far.cs:24-25` | `GlobalIlluminationTests.cs:179` |
| ground and a slab 20 by 1 by 20 at 2.5 or 5.5 | `GlobalIlluminationTests.Foliage.cs:41-42` | `GlobalIlluminationTests.LightThrough.cs:36-37` |
| a desert village of 56 torches near the origin and 10,000 out | `GlobalIlluminationTests.ScreenProbes.cs:141-143` | `GlobalIlluminationTests.ScreenProbes.cs:259-273` |
| a mirror floor and a red block or wall | `GlobalIlluminationTests.cs:516-520` | `GlobalIlluminationTests.cs:467-476` |
| a closed room under an environment map | `GlobalIlluminationTests.Frames.cs:508-511`, `CascadeEdgeTests.cs:139-146` | `GlobalIlluminationTests.cs:431-440`, `:505-520` |
| a box of 2 for the field | `SceneFieldTests.cs:366-375` | `SceneFieldTests.cs:180-185` |
| a plane 4 by 4 under the sky for the reference | `BounceReferenceTests.Gloss.cs:15`, `:29` | `GiReferenceTests.cs:616`, `:633` |

The panel room (4 by 3 in 3DEngine, 6 by 3 in BevyCSharp) and the six emitters (6 wide and gray 170
in 3DEngine, 8 wide, linear 0.787 and roughness 0.9 in BevyCSharp) share a purpose and not a form.
Sponza comes from one source in both, as different packs lit in different ways, and no test loads it
in either. The only reference the two engines hold byte for byte is tonemapping's, eight pictures
alike in both (SHARED.md).

## The order of the move

The kernels move least bound first, each with the tests named above in both engines.

1. **The field's plan and the triangles' pool.** C# and Rust on the CPU, tested on the CPU in both
   and in CI. The module settles the two forms first, the stamp before a mesh settles and the lead,
   3DEngine's being the later.
2. **The glow lights' choice and layout** (`GlowLights`). On the CPU, with 3DEngine's tests and a
   port of `GlowLightsTests.cs:41` to BevyCSharp once the faint level is one.
3. **`field_splat`, `field_resolve` and `field_stamp` with the readers' module**, which every later
   kernel imports. Their WGSL differences, the atomics, the emission left unpacked, the dispatch
   that clears and the still images read as textures, become the module's two bindings. The box of 2
   is the test both engines share, and the readers' own test is written before them.
4. **`gi_trace`, `gi_merge` and `gi_ambient` with the probes' readers**, `gi`, `cascades` and
   `probelight` against `bcs_gi`. They are held by the nine bounce rooms in both against one set of
   references the module keeps, and by a port of `Frames.cs:13` to BevyCSharp, which the fault in
   its `follow` fails.
5. **`lightthrough` and `roughreflect`**, small modules the probes import.
6. **`gi_screen`, `gi_screen_filter` and `gi_screen_glow` with `glow`.** They are bound to each
   engine's depth and view through the screen's view the interface names.
7. **The light on the picture** moves least. Its shared functions move, `distantAround`,
   `reflectedAt` and the glow lights' read, and the model pass's terms and `gi_light`'s take-back
   stay their engine's.
8. **The contact shadow's march** moves as a function, and where it is traced and how its result is
   applied stay each engine's.
9. **`gi_reference`** stays with each engine, bound to its own ray scene, materials and diffuse
   term, and the module keeps the references the two trace.

## The interface written out

The ten things of the interface above, each written as the declaration the module would hold, a
Slang struct or interface and, for the order, a C# interface. 3DEngine is read at `d102cd72` as
committed and BevyCSharp at `36840712`. Each field and function carries what it is called in each
engine today, as `3DEngine: name file:line | BevyCSharp: name file:line`, with `lacks` where an
engine has none. A file is named alone, as `scenefield.slang`, where its name is its engine's own.
Where the two engines hold one thing in two forms both are shown and the difference is said, and no
choice is made between them. The choice is made when the kernels move.

### 1. The field's images

```slang
// A cascade's cell q is texel (q.x, q.y, q.z + c * size), its uvw (cell.xy / size,
// (cell.z + c * size) / (size * count)), the cell held within 0.5 and size - 0.5.
// 3DEngine: scenefield.slang:229-230 | BevyCSharp: bcs_field.slang:131-132
public interface IFieldImages
{
    // The distance between texels, x the distance and yzw the first sheet's plane where one is kept.
    // 3DEngine: IVolume.sampleAt scenefield.slang:16, over Sampler3D :22, SplitVolume :38, SheetVolume :61
    // BevyCSharp: Texture3D<float>.SampleLevel with field_sampler, bcs_field.slang:133, x alone
    float4 distanceBlend(float3 uvw);
    // 3DEngine: IVolume.loadAt scenefield.slang:17, :23, :39, :62 | BevyCSharp: lacks
    float4 distanceTexel(int3 texel);
    // 3DEngine: ISheetVolume.secondAt scenefield.slang:46 over fieldSecond, gi_trace.slang:113-114 | BevyCSharp: lacks
    float3 secondPlane(int3 texel);
    // 3DEngine: albedo.sampleAt gi.slang:210, :270, :288 | BevyCSharp: albedo.SampleLevel bcs_field.slang:221, :238
    float4 colorBlend(float3 uvw);
    // 3DEngine: albedo.loadAt gi.slang:250 | BevyCSharp: albedo.Load bcs_field.slang:278, :349, :439
    float4 colorTexel(int3 texel);
    // 3DEngine: glow.sampleAt gi.slang:271, :291 | BevyCSharp: glow.SampleLevel bcs_field.slang:239
    float4 glowBlend(float3 uvw);
    // 3DEngine: glow.loadAt gi.slang:303 | BevyCSharp: glow.Load bcs_glow.slang:672
    float4 glowTexel(int3 texel);
    // 3DEngine: fieldFaces.Load gi.slang:248 | BevyCSharp: faces.Load bcs_field.slang:276
    uint faceTexel(int3 texel);
    // 3DEngine: true by its form | BevyCSharp: lacks, false by its form
    bool hasSheets();
};

// The bits the images hold, which the module owns.
// 3DEngine: faceBits scenefield.slang:456, faceWay :464 | BevyCSharp: faceBits bcs_field.slang:175, faceWay :184, alike
uint faceBits(float3 n);
float3 faceWay(uint bits);
// 3DEngine: lightThroughOf scenefield.slang:426 | BevyCSharp: lightThroughOf bcs_field.slang:328, alike
float lightThroughOf(float alpha);
```

| Image | 3DEngine | BevyCSharp |
|---|---|---|
| distance | `R16G16B16A16Sfloat`, distance and plane (`SceneFieldStages.cs:97`) | `R16Float`, distance alone (`SceneField.cs:333`) |
| colors | `R8G8B8A8Unorm` (`:98`) | `Rgba8` (`:335`) |
| emission | `R16G16B16A16Sfloat` (`:99`) | `Rgba16Float` (`:339`) |
| faces | `R32Uint` (`:101`) | `R32UInt` (`:337`) |
| second plane | `R16G16B16A16Sfloat` (`:104`) | lacks |
| sampler | linear, clamped to the edge (`SceneFieldStages.cs:156-157`) | `SamplerSettings.ClampedTable`, linear and clamped (`BevyCSharp/Rendering/SamplerSettings.cs:70`) |
| handed from C# | `GpuSceneField.View`, `AlbedoView`, `GlowView`, `FacesView`, `SecondView` (`GpuSceneField.cs:60`, `89`, `92`, `99`, `154`) | `SceneField.Image`, `Albedo`, `Glow`, `Faces` (`SceneField.cs:127`, `136`, `142`, `150`), set by name (`WorldProbes.cs:464-465`) |

3DEngine holds both forms of a binding. Its probe kernels bind a combined image and
sampler, `Sampler3D` (`gi_trace.slang:37-47`), and its model pass binds the images apart from one
shared sampler, `SplitVolume` (`lightset.slang:247-259`, `281-282`), since Metal allows sixteen
samplers to a stage. BevyCSharp binds a `Texture3D` and a `SamplerState` everywhere
(`gi_trace.slang:35-41` among its kernels), which is the split form, WGSL having no combined one.

Where one form holds what the other cannot:

- **Sheets.** 3DEngine's distance image carries the first sheet's plane and its second image the
  second plane, and its readers take half a cell off near a sheet (`scenefield.slang:231-232`) and
  trace through sheets (`fieldDistanceSheetIn` `:245`, `traceFieldSheets` `:376`). BevyCSharp's one
  channel holds no plane and cannot hold sheets.
- **Gloss bits.** 3DEngine holds roughness in bits 8 to 15 and metallic in 16 to 23 with a painted
  flag in bit 24, an unpainted cell reading 0.5 and no metal (`glossBits` `scenefield.slang:478`,
  `faceGloss` `:484`). BevyCSharp holds reflectance in bits 24 to 31 and no flag
  (`bcs_field.slang:195`, `:203`). Each layout holds a thing the other drops, the flag or the
  reflectance.
- **Leaf cards.** BevyCSharp marks a card by an odd alpha byte (`lightThroughCardsPaint`
  `bcs_field.slang:311`, `lightThroughCards` `:320`) and rounds the rest to an even byte (`:294`).
  3DEngine's paint is blended (`field_resolve.slang:234`, `field_splat.slang:419`) and keeps no byte
  even, so a shared `lightThroughCards` reads 3DEngine's odd bytes as cards.

### 2. The field's cascades

```slang
// xyz the cascade's corner, w the width of its cell, 0 for a cascade not built.
// 3DEngine: FieldCascade.originAndCell scenefield.slang:66-71 | BevyCSharp: FieldCascade.origin_and_cell bcs_field.slang:16-21
public struct FieldCascade { float4 originAndCell; };

// 3DEngine: SceneFieldInfo scenefield.slang:73-88, 208 bytes | BevyCSharp: FieldInfo bcs_field.slang:24-34, 176 bytes
public struct FieldTable
{
    // x the cascades held, y the cells a side, z the cells of exact distance, w the far cascade or 0.
    // 3DEngine: counts scenefield.slang:78 | BevyCSharp: counts bcs_field.slang:29
    float4 counts;
    // 3DEngine: cascades scenefield.slang:79 | BevyCSharp: cascades bcs_field.slang:30
    FieldCascade cascades[9];
    // xyz the eye the cascades lie about. 3DEngine: eye scenefield.slang:82 | BevyCSharp: eye bcs_field.slang:33
    float4 eye;
    // The cells a shift brought in that hold nothing yet, w of the first one more than the cascade.
    // 3DEngine: waitingFirst, waitingLast scenefield.slang:86-87 | BevyCSharp: lacks
    float4 waitingFirst;
    float4 waitingLast;
};

// 3DEngine: fieldOn scenefield.slang:91 | BevyCSharp: fieldOn bcs_field.slang:37
bool fieldOn(FieldTable t);
// The probes' cascade c, the far cascade in place of the last where there is one.
// 3DEngine: cascadeAt scenefield.slang:104, reading the table in place | BevyCSharp: probesOf bcs_field.slang:48, copying it
float4 probeCascade(FieldTable t, int c, int probes);
// 3DEngine: cascadesOf scenefield.slang:112 | BevyCSharp: probesOf's counts.x bcs_field.slang:54
int probeCascades(FieldTable t, int probes);
// 3DEngine: fieldCascade scenefield.slang:190, passing over the waiting cells | BevyCSharp: fieldCascade bcs_field.slang:108
int fieldCascade(FieldTable t, float3 p, float margin, int probes);
// 3DEngine: fieldWaits scenefield.slang:179 | BevyCSharp: lacks
bool fieldWaits(FieldTable t, float3 cell, float margin);
// 3DEngine: bandHold scenefield.slang:138, fieldHold :155, fieldCoarsestBuilt :167, fieldCell :432
// BevyCSharp: bandHold bcs_field.slang:72, fieldHold :98, fieldCoarsestBuilt :85, fieldCell :161, none taking probes
float bandHold(FieldTable t, int c, float3 cell, float band, float margin, int probes);
float fieldHold(FieldTable t, float3 p);
int fieldCoarsestBuilt(FieldTable t, int upTo, int probes);
float fieldCell(FieldTable t, float3 p);
// 3DEngine: fieldDistanceIn scenefield.slang:225, fieldDistance :295, fieldNormal :310, traceField :357
// BevyCSharp: fieldDistanceIn bcs_field.slang:127, fieldDistance :138, fieldNormal :146, traceField bcs_gi.slang:137
float fieldDistanceIn<I : IFieldImages>(I images, FieldTable t, int c, float3 p);
float fieldDistance<I : IFieldImages>(I images, FieldTable t, float3 p);
float3 fieldNormal<I : IFieldImages>(I images, FieldTable t, float3 p);
float traceField<I : IFieldImages>(I images, FieldTable t, float3 from, float3 way, float near, float far);
// 3DEngine: FieldBand :119, EyeClear :128 | BevyCSharp: FieldBand bcs_field.slang:61, EyeClear :64
static const float FieldBand = 8;
static const float EyeClear = 6;
```

BevyCSharp's 176 bytes are 3DEngine's first 176, so the two layouts agree as far as BevyCSharp's
goes, and the waiting box follows in 3DEngine's alone. 3DEngine binds the table as a constant buffer
(`gi_trace.slang:40-41`), written by `vkCmdUpdateBuffer` (`SceneFieldStages.cs:268-277`) from
`SceneField.Info` (`SceneField.cs:590-610`). BevyCSharp binds it as a structured buffer read at
`info[0]` (`gi_trace.slang:37`, `212`), written by `InfoValues` (`SceneField.cs:445-456`). Its WGSL
copy for the leaves reads the buffer in place for the reason 3DEngine's `cascadeAt` gives
(`forward_probes.rs:91-104`).

### 3. A cascade's run and the plan's facts

```slang
// What a dispatch of the trace, the merge or the gather is told of its cascade.
// 3DEngine: TracePush gi_trace.slang:126-149 (IlluminationTrace GlobalIlluminationStages.cs:24-34), MergePush gi_merge.slang:137-155, AmbientPush gi_ambient.slang:99-111
// BevyCSharp: uniform globals gi_trace.slang:113-121, gi_merge.slang:40-45, gi_ambient.slang:75-78, set WorldProbes.cs:462-494
public struct ProbeRun
{
    uint cascade;        // 3DEngine: cascade.x in all three | BevyCSharp: cascade.x in all three
    uint probes;         // 3DEngine: cascade.y | BevyCSharp: cascade.y
    uint texels;         // 3DEngine: cascade.z | BevyCSharp: cascade.z
    uint last;           // 3DEngine: trace cascade.w, the last or the last counted, GlobalIlluminationStages.cs:362 | BevyCSharp: trace cascade.w, the last alone, WorldProbes.cs:462
    uint texelsAbove;    // 3DEngine: trace above.x :368, merge cascade.w | BevyCSharp: merge cascade.w, the trace lacks
    float start;         // 3DEngine: trace interval.x | BevyCSharp: trace interval.x
    float end;           // 3DEngine: trace interval.y, gather interval.x | BevyCSharp: the same
    float spacing;       // 3DEngine: trace interval.z, merge spacing.x, the gather lacks | BevyCSharp: trace interval.z, merge spacing.x, gather interval.y
    uint cascades;       // 3DEngine: trace interval.w, merge counts.x, the gather lacks | BevyCSharp: the same and gather interval.z
    float edge;          // 3DEngine: trace follow.y, the last counted cascade's end | BevyCSharp: trace follow.y, the last cascade's end
    float aboveEnd;      // 3DEngine: merge spacing.w | BevyCSharp: merge spacing.w
    float belowEnd;      // 3DEngine: trace follow.w, read gi_trace.slang:546 | BevyCSharp: lacks
    uint lying;          // 3DEngine: trace follow.x, gather cascade.w bit 1 | BevyCSharp: trace follow.x, gather cascade.w + 2
    uint parity;         // 3DEngine: trace follow.z bit 0, gather cascade.w bit 0 | BevyCSharp: trace follow.z, gather cascade.w bit 0
    uint fieldWhole;     // 3DEngine: trace follow.z bit 1, GlobalIlluminationStages.cs:365 | BevyCSharp: trace follow.w, WorldProbes.cs:447-449
    uint firstProbe;     // the far cascade in four parts. 3DEngine: trace above.y :376-383 | BevyCSharp: lacks
    uint half;           // 3DEngine: trace above.w, Halved :46 | BevyCSharp: lacks
    uint traceFlags;     // 1 moving carried rays, 2 every ray meets meshes. 3DEngine: trace above.z | BevyCSharp: lacks
    uint moved;          // 3DEngine: merge counts.y, three signed bytes and bits 24 and 25, :436-438 | BevyCSharp: lacks
    uint carriedSince;   // 3DEngine: merge counts.z | BevyCSharp: lacks
    uint carriedPhase;   // 3DEngine: merge counts.w | BevyCSharp: lacks
    float carriedShare;  // 3DEngine: gather interval.y | BevyCSharp: lacks
    float mergeOff;      // 3DEngine: merge spacing.y | BevyCSharp: lacks
    float alone;         // 3DEngine: merge spacing.z | BevyCSharp: lacks
};

// What the light passes are told of the probes.
// 3DEngine: Lighting.indirect (on, spacing, probes, cascades) lightset.slang:76, LightingUboPrepare.cs:95
// BevyCSharp: gi_light's probes (probes, spacing, cascades, 0) gi_light.slang:62, WorldProbes.cs:509, and forward_probes.rs probe_counts (probes, spacing, cascades, on) :49-51
public struct ProbePlan { float on; float spacing; float probes; float cascades; };
```

3DEngine traces its last counted cascade to the field's edge and merges it from nothing above
(`GlobalIlluminationRenderer.cs:206-207`, `GlobalIlluminationStages.cs:444`), and BevyCSharp has no
such cascade, its intervals ending at the edge for the last cascade alone
(`WorldProbes.cs:401-408`). `fieldWhole` sits in `follow.z` bit 1 in 3DEngine and in `follow.w` in
BevyCSharp, whose `follow.w` 3DEngine gives to `belowEnd`. 3DEngine's gather binds no field, and
BevyCSharp's reads the field for its normals (`gi_ambient.slang:46-48`). Neither engine tells a
kernel its quality, both choosing the texels a cascade on the CPU and the trace's variant by light
through (3DEngine's `GlobalIlluminationStages.cs:314-316`, BevyCSharp's `WorldProbes.cs:453-457`).
The frame's number reaches the kernels as its parity alone, the number itself riding in the lights
(item 5).

### 4. The origin

```slang
// The place every position the module is handed is taken against. w unused.
// 3DEngine: RenderOrigin RenderOrigin.cs:36 (Near :51, a step of 16 :48, AsVector3 :55), on the GPU Lighting.origin lightset.slang:119 and ModelPush.origin modelpass.slang:57-64
// BevyCSharp: lacks, every position in world numbers, the origin 0
public struct FrameOrigin { float4 origin; };
// 3DEngine: world modelpass.slang:333 | BevyCSharp: the place itself
float3 worldOf(FrameOrigin o, float3 place);
// 3DEngine: placeOf lightset.slang:126 | BevyCSharp: the world point itself
float3 placeOf(FrameOrigin o, float3 world);
```

The trace, the merge and the gather read no origin in either engine. They work in whatever frame the
table, the lights and the frame before's corners are handed in, places against the origin in
3DEngine (`SceneField.cs:596-600`, `GlobalIlluminationRenderer.cs:496-509`) and world numbers in
BevyCSharp (`SceneField.cs:451-454`, `WorldProbes.cs:433-436`). The module needs the origin itself
only where a kernel reads something laid in world numbers, as BevyCSharp's light pass reads Bevy's
air through the world point (`gi_light.slang:623-626`), or as a hash of a point is taken.

### 5. The lights

```slang
// 3DEngine: GiLights.lights[16] gi.slang:126 | BevyCSharp: GiLights.lights[16] bcs_gi.slang:128, MOST_LIGHTS gi_lights.rs:15
static const int MostLamps = 16;
// 3DEngine: MostGlowLights glow.slang:12, GlowLights.cs:49 | BevyCSharp: MostGlowLights bcs_glow.slang:19, GlowLights.cs:38
static const int MostGlowLights = 64;

// 64 bytes, laid out alike in both.
public struct GiLamp
{
    float4 positionAndKind;    // w 1 point, 2 spot. 3DEngine: gi.slang:98, against the origin GlobalIlluminationRenderer.cs:500-501 | BevyCSharp: bcs_gi.slang:104, world numbers gi_lights.rs:131, 152
    float4 directionAndRange;  // 3DEngine: gi.slang:100 | BevyCSharp: bcs_gi.slang:106
    float4 colorAndShadow;     // w 1 where it casts shadows. 3DEngine: gi.slang:102 | BevyCSharp: bcs_gi.slang:108
    float4 cone;               // 3DEngine: x cos inner, y cos outer, gi.slang:104 | BevyCSharp: x scale, y offset, bcs_gi.slang:110
};

// 80 bytes, laid out and read alike in both.
public struct GlowLight
{
    float4 middleAndReach;     // 3DEngine: glow.slang:32 | BevyCSharp: bcs_glow.slang:35
    float4 halfX;              // 3DEngine: glow.slang:34 | BevyCSharp: bcs_glow.slang:37
    float4 halfY;              // 3DEngine: glow.slang:35 | BevyCSharp: bcs_glow.slang:38
    float4 halfZ;              // 3DEngine: glow.slang:36 | BevyCSharp: bcs_glow.slang:39
    float4 glowAndShare;       // w 3DEngine: the share and twice the faces, GlowLights.cs:258 | BevyCSharp: the share alone, the faces 0 for all, GlowLights.cs:92
};
// 3DEngine: glowShare glow.slang:44, glowFaces :53 | BevyCSharp: glowShare bcs_glow.slang:47, glowFaces :56
float glowShare(GlowLight l);
uint glowFaces(GlowLight l);

public interface ILights
{
    bool hasSun();                              // 3DEngine: sunDirection.w gi.slang:116 | BevyCSharp: sunDirection.w bcs_gi.slang:120
    float3 towardSun();                         // 3DEngine: sunDirection.xyz gi.slang:116 | BevyCSharp: sunDirection.xyz bcs_gi.slang:120
    float3 sunLight();                          // 3DEngine: sunColor.rgb gi.slang:118 | BevyCSharp: sunColor.rgb bcs_gi.slang:122, above the air
    bool sunCastsShadows();                     // 3DEngine: lacks in the probes, sunColor.w left 0 | BevyCSharp: sunColor.w bcs_gi.slang:121, read by gi_reference.slang:476 alone
    int lampCount();                            // 3DEngine: counts.x gi.slang:122 | BevyCSharp: counts.x bcs_gi.slang:125
    GiLamp lamp(int i);                         // 3DEngine: lights[i] gi.slang:126 | BevyCSharp: lights[i] bcs_gi.slang:128
    float spotShare(GiLamp l, float3 toward);   // 3DEngine: smoothstep(cone.y, cone.x) inline gi.slang:347-348 | BevyCSharp: spotShare bcs_gi.slang:177-183, the square of a clamped line
    int glowCount();                            // 3DEngine: glowCounts.x gi.slang:132 | BevyCSharp: counts.w bcs_gi.slang:126
    GlowLight glow(int i);                      // 3DEngine: glows[i] gi.slang:135, in the lights' buffer | BevyCSharp: glow_lights[i] gi_trace.slang:48, a buffer of its own
    bool glowsMayReach(float3 p);               // 3DEngine: the reach's box glowCounts.yzw to glowHigh.xyz gi.slang:130-134, read :651 | BevyCSharp: lacks
};
```

The two engines give the sun and the lamps in one unit, the light a white surface facing one returns
(3DEngine's `probelight.slang:550-552`, BevyCSharp's `gi_lights.rs:4-10`), and differ in the
exposure, which BevyCSharp multiplies into every light (`gi_lights.rs:95-98`) and 3DEngine applies
after. 3DEngine holds the lights, the frame before's corners, the glow lights with their head of 32
bytes and the sun's map's cascades in one constant buffer of 6,608 bytes
(`GpuIllumination.cs:77-86`, `GlowLights.cs:55-64`), and BevyCSharp holds the lights and corners in
a structured buffer of 1,168 bytes and the glow lights in a structured buffer of their own
(`WorldProbes.cs:40`, `350-351`). The head's box of the glow lights' reach and the faces in
`glowAndShare.w` are 3DEngine's alone. The two cones are two forms of a soft edge, a smoothstep and
a squared ramp, and neither is written in the other's terms, so each engine keeps its own
`spotShare`. `counts.w` means a toggle of the lamps at hits in 3DEngine (`gi.slang:123-124`, read
`:684`) and the glow lights' count in BevyCSharp, a clash the shared layout settles. The frame's
number (`counts.y`), the gain on the frame before's light (`counts.z`) and the frame before's
corners (`before[5]`, 3DEngine's `gi.slang:129` and BevyCSharp's `bcs_gi.slang:131`) share the
buffer today and belong with the run and the frame before (items 3 and 9).

### 6. The sky

```slang
public interface ISky
{
    // The light along a way of a ray that meets nothing.
    // 3DEngine: inline gi_trace.slang:505, 512, 687 | BevyCSharp: skyAlong gi_trace.slang:56-60
    float3 along(float3 way);
    // The share of the sun's light the air lets through to the probes.
    // 3DEngine: lacks, 1, the air baked into the sun's color on the CPU, DaySky.Air.cs:97-106 | BevyCSharp: air[0].rgb gi_trace.slang:46, applied :214
    float3 sunThrough();
};
```

3DEngine's `along` is the environment cube at its first level times `lights.sky.rgb` where the
program has an environment, and `lights.sky.rgb` alone, the ambient lights' sum, where it has none
(`gi_trace.slang:75-77`, `GlobalIlluminationRenderer.cs:527-536`), one or the other. BevyCSharp's is
the ambient light plus a 64 by 64 octahedron of the environment blurred to a roughness of 0.25 and
times the exposure, which `gi_sky` writes on the camera after its prepass and the trace reads the
frame after (`gi_sky.slang:22`, `57`, `WorldProbes.cs:104`, `294-304`, `352`). BevyCSharp's sum
holds 3DEngine's choice, an ambient of 0 where a map is, and 3DEngine's form cannot hold an ambient
light and a map at once. 3DEngine's cube holds the sky sharp, and BevyCSharp's octahedron holds it
blurred and a frame late.

### 7. The sun's reach

```slang
public interface ISunReach
{
    // The share of the sun reaching p on a face facing n, 0 to 1, or -1 where this source does not
    // hold p and the field is traced.
    // 3DEngine: ISunShadow.reached(lights, p, n, cell) gi.slang:149-152 | BevyCSharp: lacks
    float reached(float3 p, float3 n, float cell);
};
// 3DEngine: FieldSun gi.slang:155-161 | BevyCSharp: its one path, unnamed
public struct FieldSunOnly : ISunReach { float reached(float3 p, float3 n, float cell) { return -1.0; } };
// 3DEngine: directLight gi.slang:317-352, directReflected :391-408 | BevyCSharp: directLight bcs_gi.slang:204-221
float3 directLight<S : ISunReach, L : ILights, I : IFieldImages>(S sun, L lights, I images, FieldTable t, float3 p, float3 n);
```

3DEngine reads the frame before's sun shadow map at the probes' hits through `SunShadowMap`
(`gi.slang:174-197`), its cascades in the lights' buffer, a soft share of a 3 by 3 compare, and
traces the field where the map does not hold the point (`gi.slang:330`). It binds the map only where
the frame before's shadow was the probes' sun (`GlobalIlluminationRenderer.cs:482-490`, `555`). The
screen probes pass `FieldSun` (`gi_screen.slang:613`, `645`). BevyCSharp traces the field alone at
every hit (`bcs_gi.slang:204-221`). Its shadow maps are bound to the camera's passes alone
(`bcs_pass.slang:200`), and whether the bridge can bind them to the probes' dispatch, which runs
before the cameras, was not found. The map's share passes through gaps narrower than a cell, which
the field's yes or no cannot.

### 8. The screen's view

```slang
public struct GiScreenView
{
    float4x4 clipFromWorld;      // 3DEngine: viewProjection screenmarch.slang:52, unjittered, against the origin | BevyCSharp: passView.clip_from_world bcs_pass.slang:41, jittered, and unjittered_clip_from_world :42
    float4x4 worldFromClip;      // 3DEngine: inverseViewProjection screenmarch.slang:53 | BevyCSharp: passView.world_from_clip bcs_pass.slang:43
    float4x4 eyeFromClip;        // 3DEngine: inverseFromEye screenmarch.slang:89, in doubles GlobalIlluminationRenderer.cs:411-419 | BevyCSharp: lacks as a matrix, passEyeOffset bcs_pass.slang:504-509
    float3 eye;                  // 3DEngine: eye.xyz screenmarch.slang:55, against the origin | BevyCSharp: passView.world_position bcs_pass.slang:48
    float4x4 lastClipFromWorld;  // 3DEngine: lastViewProjection screenmarch.slang:66, against this frame's origin | BevyCSharp: passPreviousView.clip_from_world bcs_pass.slang:58
    float3 lastEye;              // 3DEngine: last.xyz screenmarch.slang:70 | BevyCSharp: lacks, eyeBefore gi_screen_filter.slang:69-74 from passPreviousView
    float lastState;             // 0 none, 1 readable, 2 still. 3DEngine: last.w screenmarch.slang:70 | BevyCSharp: screen.y WorldProbes.Screen.cs:234, 0 or 1, lacks 2
    float2 pictureSize;          // 3DEngine: sizes.zw screenmarch.slang:64 | BevyCSharp: passPictureSize bcs_pass.slang:402-412
    float2 depthSize;            // 3DEngine: sizes.xy, half the window | BevyCSharp: lacks, the picture's size
    float tile;                  // 3DEngine: grid.x screenmarch.slang:59 | BevyCSharp: screen.x WorldProbes.Screen.cs:245
    uint2 probesAcross;          // 3DEngine: grid.yz | BevyCSharp: lacks as a value, the image's size gi_screen.slang:414-416
    uint switches;               // 1 the frame before left out, 2 the neighbors left out. 3DEngine: grid.w | BevyCSharp: lacks
    float firstInterval;         // 3DEngine: trace.x | BevyCSharp: screen.y in gi_screen, WorldProbes.Screen.cs:236
    float3 worldProbes;          // the probes a side, their spacing, their cascades. 3DEngine: trace.yzw | BevyCSharp: probes.xyz WorldProbes.Screen.cs:246
    float2 jitter;               // 3DEngine: jitter.xy screenmarch.slang:77 | BevyCSharp: jitter.xy WorldProbes.Screen.cs:237
    uint cyclePhase;             // 3DEngine: jitter.z, the frame modulo 16 | BevyCSharp: lacks
    uint settling;               // 3DEngine: jitter.w | BevyCSharp: lacks
    uint2 glowKept;              // 3DEngine: glows.yz | BevyCSharp: places.xy WorldProbes.Screen.cs:250
    uint cascadesRebuilt;        // 3DEngine: glows.w | BevyCSharp: places.z
    float3 changedMin;           // 3DEngine: changedMin screenmarch.slang:84, against the origin | BevyCSharp: changed_min WorldProbes.Screen.cs:255, world numbers
    float3 changedMax;           // 3DEngine: changedMax screenmarch.slang:85 | BevyCSharp: changed_max
};

// The depth the screen probes stand on and march through.
// 3DEngine: IPicture screenmarch.slang:7-23 | BevyCSharp: passLoadDepth bcs_pass.slang:428
public interface IScreenDepth
{
    float load(float2 uv);       // 3DEngine: depth.SampleLevel gi_screen.slang:372, half size, forward | BevyCSharp: passLoadDepth by the pixel, full size, reversed
    bool empty(float d);         // 3DEngine: d >= 1 gi_screen.slang:375 | BevyCSharp: d <= 0 gi_screen.slang:427
    float2 ndcOf(float2 uv);     // 3DEngine: uv * 2 - 1 gi_screen.slang:145 | BevyCSharp: (u * 2 - 1, 1 - v * 2) bcs_pass.slang:506
    float2 uvOf(float2 ndc);     // 3DEngine: ndc * 0.5 + 0.5 gi_screen_filter.slang:197 | BevyCSharp: (x * 0.5 + 0.5, 0.5 - y * 0.5) gi_screen_filter.slang:119
};
// 3DEngine: fromEyeAt gi_screen.slang:143-147 | BevyCSharp: passEyeOffset bcs_pass.slang:504-509
float3 fromEye(GiScreenView v, float2 uv, float d);
// 3DEngine: mul(lastViewProjection, eye + p) gi_screen_filter.slang:194 | BevyCSharp: passPreviousClipAt bcs_pass.slang:554
float4 lastClipAt(GiScreenView v, float3 fromEye);
```

3DEngine fills one block of 400 bytes on the CPU, its places and matrices against the origin in
doubles (`GlobalIlluminationRenderer.cs:380-458`, `GpuScreenProbes.cs:114`), and draws the depth
unjittered for it (`AmbientOcclusion.cs:205-207`). BevyCSharp reads Bevy's view block
(`bcs_pass.slang:39-62`), its matrices jittered as Bevy's prepass is, with loose uniforms beside it,
and keeps its precision by working on offsets from the eye. BevyCSharp alone carries the frame
before's inverse, view and projection, and 3DEngine alone the frame before's eye, the still state,
the depth's own size, the switches, the cycle and the settling flag. 3DEngine traces the screen
probes for the window and for every render target, each target with its own frame before
(`GlobalIlluminationRenderer.cs:286-307`, `330-367`), and BevyCSharp for one camera
(`WorldProbes.cs:293`). `JitterAt` and `TileAt` are the same code in both (3DEngine's
`Illumination.cs:60-65`, `91-99`, BevyCSharp's `WorldProbes.Screen.cs:100-109`, `129-134`) and move
into the module as they are.

### 9. The frame before

```slang
public struct GiHistoryFlags
{
    uint frame;            // 3DEngine: _frame GlobalIlluminationRenderer.cs:85, in the lights :553 | BevyCSharp: _frame WorldProbes.cs:124, in the lights :419
    uint parity;           // 3DEngine: GlobalIlluminationStages.cs:365, 385, 472 | BevyCSharp: WorldProbes.cs:443, 463, 490
    uint lying;            // a bit a cascade. 3DEngine: GlobalIlluminationRenderer.cs:247-250 | BevyCSharp: WorldProbes.cs:433-435
    uint shift;            // 3DEngine: MergePush counts.y GlobalIlluminationStages.cs:436-438 | BevyCSharp: lacks, 0 WorldProbes.cs:478
    uint cycleSince;       // 3DEngine: GlobalIlluminationStages.cs:447, GlobalIllumination.Changes.cs:37-41 | BevyCSharp: lacks
    uint cyclePhase;       // 3DEngine: the same | BevyCSharp: lacks
    int farPart;           // 3DEngine: GlobalIlluminationRenderer.cs:257 | BevyCSharp: lacks
    float heldBounce;      // the gain on the frame before's light. 3DEngine: lights :553 | BevyCSharp: lights WorldProbes.cs:419
};

public interface IGiFrameBefore
{
    float4 bounced(int3 t);        // the gathered faces. 3DEngine: CubesView binding 6 gi_trace.slang:61-64 | BevyCSharp: bounced gi_trace.slang:67-69
    float4 bouncedAt(int3 t);      // 3DEngine: CubesAtView binding 15 | BevyCSharp: bounced_at
    float reachBefore(int3 t);     // 3DEngine: ReachView, the gather's of the frame before, gi_merge.slang:72-75 | BevyCSharp: lacks, the trace writes reach and the merge reads this frame's
    float skyAbove(int3 t);        // 3DEngine: MergedSky of the cascade above, gi_trace.slang:121-124 | BevyCSharp: lacks
    float4 waysBefore(int3 t);     // 3DEngine: lacks in the trace | BevyCSharp: ways gi_trace.slang:73-75
    float4 held(int3 t);           // the probe's own light by parity. 3DEngine: an rgba16f image gi_trace.slang:94-98 | BevyCSharp: a structured buffer gi_trace.slang:100-104
    float4 partial(int3 t);        // 3DEngine: an rgba32f image | BevyCSharp: a structured buffer gi_trace.slang:106-109
    float4 cycleBefore(int3 t);    // 3DEngine: the Cycle*Before images, merge bindings 10, 11, 14, 16 | BevyCSharp: lacks
    float sunVisible(float3 p);    // 3DEngine: the frame before's map, item 7 | BevyCSharp: lacks
    float4 skyBefore(float2 oct);  // 3DEngine: lacks, this frame's cube | BevyCSharp: sky_map and air from gi_sky, gi_trace.slang:43-52
    float4 cornerBefore(uint c);   // 3DEngine: the lights' before GlobalIlluminationRenderer.cs:245-251 | BevyCSharp: the lights' before WorldProbes.cs:436
    float4 screenLight(int2 i);    // 3DEngine: History, the kept light before the blend | BevyCSharp: gi_screen_blended_previous, after it, WorldProbes.Screen.cs:207
    float4 screenSurface(int2 i);  // 3DEngine: LastGeometry | BevyCSharp: gi_screen_surfaces_previous WorldProbes.Screen.cs:206
    float4 screenCycle(int2 i);    // 3DEngine: Cycle.Last images | BevyCSharp: lacks
    float4 glowBefore(int2 i);     // 3DEngine: the glow's Last images GlobalIlluminationStages.ScreenProbes.cs:126-131 | BevyCSharp: gi_glow_*_previous WorldProbes.Screen.cs:213-215
    float4 picture(float2 uv, float lod); // 3DEngine: the reflection history, half size with mips, ModelRenderer.Lighting.cs:230 | BevyCSharp: gi_picture_previous, full size, one level, WorldProbes.cs:307-308
    float pictureDepth(float2 uv); // 3DEngine: the reflection history's depth ModelRenderer.Lighting.cs:233 | BevyCSharp: lacks
};
```

The two engines keep the frame before in two ways. Both read some images before this frame writes
them over in place, the faces, the faces at the surface, the held light and the partial sums. Beyond
that 3DEngine copies with `vkCmdCopyImage`, the world's cycle
(`GlobalIlluminationStages.cs:392-409`), the screen probes' kept light, surfaces and cycle and the
glow's images (`GlobalIlluminationStages.ScreenProbes.cs:150-159`) and the lit picture with its
depth (`GlobalIlluminationRenderer.cs:466-476`). BevyCSharp gives a camera's image two textures that
trade places each frame, read as `name_previous` (`render/images.rs:106-108`, `175-183`), and has no
copy call and no history for images in three dimensions, so a world image it needs from the frame
before is a pair it swaps by parity. The merge reads the frame before's reach in 3DEngine and this
frame's in BevyCSharp, a difference of meaning the module settles.

### 10. The order

```csharp
// What the module asks of an engine.
public interface IGiEngine
{
    // 3DEngine: GlobalIlluminationRenderer._frame :85 | BevyCSharp: WorldProbes._frame :124
    long Frame { get; }
    // The views that take screen probes.
    // 3DEngine: the window GlobalIlluminationRenderer.cs:286 and each render target TargetsNode.cs:39-43 | BevyCSharp: one camera WorldProbes.cs:293
    IReadOnlyList<GiView> Views { get; }
    // 3DEngine: records at once, ProbeRun GlobalIlluminationStages.cs:510, run.Dispatch :370, :448, :474 | BevyCSharp: queues, Shaders.Dispatch BevyCSharp/Rendering/Shaders.Views.cs:620, or a ViewDispatch WorldProbes.Screen.cs:226-229
    void Dispatch(GiStep step, GiView? view, GiKernel kernel, IGiBindings bindings, uint x, uint y, uint z);
    // 3DEngine: vkCmdCopyImage GlobalIlluminationStages.ScreenProbes.cs:139-159 | BevyCSharp: a history image render/images.rs:106-108, lacks for three dimensions
    void KeepForNextFrame(GiImage image);
}

// What an engine calls on the module, in this order.
public interface IGiFrameHooks
{
    // 3DEngine: GlobalIlluminationRenderer.Draw :134-275 | BevyCSharp: WorldProbes.Tick :247-313, Trace :391-445
    void PrepareFrame(GiFrameState state);
    // 3DEngine: the scene_field node Renderer.cs:149-150, SceneField.cs:754-758 | BevyCSharp: AreasWork 2000 FieldWork.cs:11-14, SceneField.Tick :197
    void BuildField(IGiEngine engine);
    // 3DEngine: the global_illumination node GlobalIlluminationPlugin.cs:71-73 | BevyCSharp: AreasWork 2100 FieldWork.cs:15-18, WorldProbes.cs:471, 484, 495
    void WorldProbes(IGiEngine engine);
    // After the view's depth. 3DEngine: GlobalIlluminationRenderer.cs:286-307, a target TargetsNode.cs:42 | BevyCSharp: FramePoint.AfterPrepass WorldProbes.Screen.cs:226-229, render/views.rs:201-204
    void ScreenProbes(IGiEngine engine, GiView view);
    // 3DEngine: _sunBefore GlobalIlluminationRenderer.cs:227, the shadows node after it Renderer.cs:169-170 | BevyCSharp: lacks
    void SunShadowDrawn(GiShadow shadow);
    // 3DEngine: the hdr_scene node Renderer.cs:182-183, ModelRenderer.Lighting.cs:223-233 | BevyCSharp: gi_light at FramePoint.AfterOpaque SceneField.cs:316, render/views.rs:255-259
    void LightView(GiView view);
    // 3DEngine: KeepFrame HdrNodes.cs:47, GlobalIlluminationRenderer.cs:466 | BevyCSharp: CopyAt BeforeTonemapping WorldProbes.cs:307-308, render/views.rs:235-238
    void KeepPicture(GiView view, GiImage lit);
    // 3DEngine: the copies at the recordings' end GlobalIlluminationStages.ScreenProbes.cs:139-159 | BevyCSharp: lacks, its images trading at the prepare
    void EndFrame(IGiEngine engine);
}
```

3DEngine records Vulkan commands at once inside its render graph's nodes, from the render world.
BevyCSharp's C# runs in the main world at the frame's first stage and queues work, the global
dispatches run before the cameras (`render/compute.rs:161-166`, `411-472`) and a camera's at its
frame points (`render/views.rs:170-308`), so a hook declares work for a step and records nothing on
the spot. BevyCSharp's trace reads the sky and the air the camera wrote the frame before, and
3DEngine's reads the sun's map of the frame before. Render targets with screen probes of their own
and an end-of-frame step are 3DEngine's alone. Whether BevyCSharp's render world runs a frame behind
its main world's queued work was not found.

### What each engine changes to give it

3DEngine, a file at a time:

- `3DEngine/Shaders/scenefield.slang`. `IVolume`, `ISheetVolume`, `SheetVolume` and `SplitVolume`
  (`:14-64`) become one struct implementing `IFieldImages`. `SceneFieldInfo` (`:73-88`) becomes
  `FieldTable`, and `cascadeAt` and `cascadesOf` (`:104`, `:112`) the module's `probeCascade` and
  `probeCascades`. `lightThroughPaint` (`:419-422`) keeps an even byte if the cards' mark is taken,
  and `glossBits` and `faceGloss` (`:478-487`) change if BevyCSharp's gloss is taken.
- `3DEngine/Shaders/field_splat.slang` (`:419`, `:431`), `field_stamp.slang` (`:122`) and
  `field_resolve.slang` (`:234`). They follow any change to the paint's byte or the gloss.
- `3DEngine/Shaders/gi.slang`. `GiLight` (`:95-105`) becomes `GiLamp`, and `GiLights` (`:113-145`)
  is wrapped as an `ILights` over the constant buffer. `ISunShadow` (`:149-152`) becomes
  `ISunReach`, and `SunShadowMap` (`:174-197`) reads its cascades from its own fields.
  `directLight`, `directReflected`, the hidden lamps and the glow lights (`:311-743`) move to the
  module, and `counts.w`'s toggle moves where the shared layout puts it. The surface reads
  (`:201-307`) take the images' struct.
- `3DEngine/Shaders/glow.slang`. `GlowLight` and its closed forms (`:12-57` and after) move to the
  module as they are.
- `3DEngine/Shaders/gi_trace.slang`. The bindings (`:37-47`, `:107-114`) build the images' struct,
  the lights' binding (`:49-50`) is wrapped, and a sky struct over the cube (`:76-77`) gives `along`
  in place of the reads at `:505`, `:512` and `:687`. `TracePush` (`:126-149`) maps onto `ProbeRun`,
  and `gatheredAt` (`:169-181`) and the reads of `before` (`:177`) take the module's lookups.
- `3DEngine/Shaders/gi_merge.slang`. The bindings (`:52-56`, `:78-79`) build the images' struct, and
  `MergePush` (`:137-155`) maps onto `ProbeRun`.
- `3DEngine/Shaders/gi_ambient.slang`. `AmbientPush` (`:99-111`) takes the spacing and the cascades
  in its unused `interval.zw`, and the field's images where the shared gather reads normals.
- `3DEngine/Shaders/probelight.slang` (`:443-568`), `cascades.slang` (`:27-86`) and
  `lightthrough.slang` (`:12`, `:65`, `:93`). They take `FieldTable`, the images' struct and a
  generic `ILights`.
- `3DEngine/Shaders/screenmarch.slang`. `ScreenView` (`:49-90`) becomes `GiScreenView`, and
  `IPicture` (`:7-23`) the module's `IScreenDepth`.
- `3DEngine/Shaders/gi_screen.slang`, `gi_screen_filter.slang` and `gi_screen_glow.slang`. They
  import the module, read the frame before through `IGiFrameBefore` and use the wrappers for the
  lights (`gi_screen.slang:33`, `613`, `645`, `gi_screen_glow.slang:24`).
- `3DEngine/Shaders/lightset.slang`. The model pass's images (`:247-259`, `:281-285`) build the
  split form, and `indirect` (`:76`) is read as or reordered to `ProbePlan`.
- `3DEngine/Shaders/lights.slang` (`:727-738`). The model pass's reads of the lights and the sky use
  the wrappers.
- `3DEngine/GlobalIllumination/GlobalIlluminationStages.cs`. `IlluminationTrace` (`:24-34`) and the
  merge's and gather's spans (`:445-447`, `:472-473`) follow `ProbeRun`'s order, and the frame
  before's bindings (`:345-355`, `:392-409`, `:424-433`) move to the module's slots.
- `3DEngine/GlobalIllumination/GlobalIlluminationStages.ScreenProbes.cs` (`:21-165`). The copies sit
  behind `KeepForNextFrame` and `EndFrame`.
- `3DEngine/GlobalIllumination/GlobalIlluminationRenderer.cs`. The screen's view (`:380-421`) is
  written in `GiScreenView`'s order, and the lights (`:515-585`) where the shared layout moves a
  field.
- `3DEngine/GlobalIllumination/GlobalIllumination.Changes.cs` (`:17-44`). It fills `GiHistoryFlags`.
- `3DEngine/Rendering/Lighting/GpuScreenProbes.cs` (`:104-114`). The view's buffer takes the
  module's size.
- `3DEngine/Rendering/Lighting/Illumination.cs` (`:60-99`). `JitterAt` and `TileAt` move to the
  module.
- `3DEngine/Rendering/Lighting/IIlluminationPasses.cs` (`:10-32`). It implements `IGiFrameHooks`,
  `KeepFrame` and `DrawTarget` being there, the field's and the world's hooks added.
- `3DEngine/Rendering/Lighting/SceneField.cs` (`:754-758`). The field's node calls the module's
  `BuildField`. The table's bytes (`GpuSceneField.cs:69`, `SceneField.cs:590-610`) stay as they are
  unless the origin joins the table.
- `3DEngine/GlobalIllumination/GlobalIlluminationPlugin.cs` (`:67-77`),
  `3DEngine/Rendering/PostProcess/HdrNodes.cs` (`:43-48`) and
  `3DEngine/Rendering/Immediate/TargetsNode.cs` (`:39-43`). The graph's wiring stays and calls the
  hooks.

BevyCSharp, a file at a time:

- `native/bevy_csharp/src/render/bcs_field.slang`. An `IFieldImages` struct over the `Texture3D`
  images and the sampler, its texel distance and second plane reading 0 and `hasSheets` false.
  `FieldInfo` (`:24-34`) gains the waiting box, `fieldCascade` (`:108`) a branch for it, and
  `probesOf` (`:48`) a lookup in place or stands as the module's second form. The free functions
  (`:127-459`) take the images' struct.
- `native/bevy_csharp/src/render/bcs_gi.slang`. `GiLight` (`:100-111`) becomes `GiLamp`, `GiLights`
  (`:117-132`) is wrapped as an `ILights` over the buffer and the glow lights' buffer, and
  `spotShare` (`:177-183`) stays its own. The lamps', the direct and the hits' light (`:187-221`,
  `:644-731`) move to the module with a sun parameter filled by `FieldSunOnly`. The march and the
  probes' lookups (`:137`, `:157`, `:252`, `:434`, `:494`, `:607`) take the images' struct.
- `native/bevy_csharp/src/render/bcs_glow.slang`. The glow lights' reads (`:19-60`, `:540-541`,
  `:647`) take `ILights` and the images' struct, `glowsMayReach` answering true unless a reach's box
  is added.
- `native/bevy_csharp/src/render/bcs_pass.slang` or a file of BevyCSharp's own beside it.
  `IScreenDepth` over `passDepth`, reversed, 0 for none, full size and turned in y, `eyeFromClip`
  through `passEyeOffset`, and `lastEye` from `passPreviousView`.
- `native/bevy_csharp/src/render/forward_probes.rs` (`:68-72`, `:77`, `:340`). The WGSL copy of the
  table follows the new layout, and the sky's index where it moves.
- `native/bevy_csharp/src/render/gi_lights.rs` (`:24-54`, `:147-157`, `:175-182`). It changes only
  if the cone is made one.
- `BevyCSharp/GlobalIllumination/Kernels/gi_trace.slang`. The bindings (`:35-48`) build the images'
  struct and the wrapped lights, a sky struct over `sky_map`, `lights.sky` and `air[0]` gives
  `along` (`:56-60`) and `sunThrough` (`:214`), and the uniforms (`:113-121`) become `ProbeRun`,
  `fieldWhole` moving from `follow.w` and the fields BevyCSharp lacks set to 0. The lookups
  (`:152-159`, `:215`, `:298`) take the module's.
- `BevyCSharp/GlobalIllumination/Kernels/gi_merge.slang` (`:24-45`, `:90`), `gi_ambient.slang`
  (`:46-48`, `:75-78`, `:196`) and `gi_light.slang` (`:40-42`, `:57-58`, `:62`, `:66-68`, `:410`).
  The same as the trace, `ProbePlan` in place of `probes` in the light pass.
- `BevyCSharp/GlobalIllumination/Kernels/gi_screen.slang` (`:73-101`), `gi_screen_filter.slang`
  (`:31`) and `gi_screen_glow.slang` (`:58-71`). The loose uniforms become one `GiScreenView`, which
  the bridge's reflection fills field by field by name (`render/reflect.rs:13`, `657-665`).
- `BevyCSharp/GlobalIllumination/SceneField.cs`. `InfoBytes` (`:439`) grows from 176 to 208, and
  `InfoValues` (`:445-456`) writes a waiting box of none.
- `BevyCSharp/GlobalIllumination/WorldProbes.cs`. The values (`:462-470`, `:477-483`, `:490-494`,
  `:509`) are set in `ProbeRun`'s and `ProbePlan`'s order, the frame before's merged sky and cycle
  are made as pairs swapped by parity (`:318-365`), and the layouts (`:40`, `:350-351`, `:415-440`)
  follow any move of the shared lights. Whether the bridge sets a uniform of a struct's type as it
  sets a `float4` was not read.
- `BevyCSharp/GlobalIllumination/WorldProbes.Screen.cs` (`:204-255`). It fills the view's still
  state, the switches, the cycle, the settling flag and the depth's size, adds the history images
  the module's filter asks for, and resets `_screenSince` on a resize.
- `BevyCSharp/GlobalIllumination/GlowLights.cs` (`:69-95`). It writes a reach's box where `ILights`
  asks for one.
- `BevyCSharp/GlobalIllumination/FieldWork.cs` (`:11-22`) and `SceneField.cs` (`:314-317`). They
  call the hooks at the areas' work 2000 and 2100 and in the draw after the opaque pass.
- The frame before's sun map. Nothing keeps Bevy's directional maps for the global queue, which runs
  before the cameras, so a map behind `ISunReach` takes work in `render/views.rs` and
  `render/images.rs`, or the module leaves the map to the engine that has one.
