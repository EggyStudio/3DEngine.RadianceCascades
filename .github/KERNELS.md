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
