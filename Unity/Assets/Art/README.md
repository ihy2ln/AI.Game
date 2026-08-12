# Aesthetics, camera & map layout

Art direction reference for the 2.5D anime × HD-pixel look (Brown Dust 2
inspired), and the camera/map plan for growing past the current 2×2 diorama
sandbox into a larger forest-farm map (roadmap item 3 in the
[project README](../../../README.md)).

## Current implementation (Home / diorama mode)

The live Farm scene already implements a tight, code-generated diorama look —
see [`Scripts/Farm/FarmIso.cs`](../Scripts/Farm/FarmIso.cs) and
[`Scripts/Farm/FarmVisuals.cs`](../Scripts/Farm/FarmVisuals.cs):

- Orthographic camera, pitch 33°, yaw 45°, tight `orthographicSize 2.55`
  (`FarmIso.ApplyAestheticCamera`) — a "stage" framing, dark plate underfoot,
  soft key spotlight, dusk-purple background.
- Point-filtered procedural pixel materials, no imported sprites — everything
  built from code (`FarmPixelArt`).
- Chibi billboard player with a procedural anime face texture.

This is the **Home** camera mode: close, character/plot-scale, used for the
farm itself. It matches the day-farm and night-farmhouse reference moods
below (same pitch/framing family, different lighting).

## New reference moods (this pass)

Source images in [`Refs/Forest/`](Refs/Forest/):

| File | Mood | Camera read |
|---|---|---|
| `forest-path-exploration.png` | Winding forest path, stream, moss-covered ruins as waypoints, canopy light shafts | Steeper, wider pitch than Home — reads as a region/exploration view, not stage-diorama |
| `farm-plot-day.png` | Tidy crop rows, barn, garden beds, warm rustic daylight | Same close pitch family as current Home camera |
| `farmhouse-night.png` | Lit farmhouse windows, hanging lantern, character at door, deep indigo night | Same close pitch as Home, night palette instead of dusk |

## Camera plan: Home vs Overworld

The forest reference is a materially different framing from Home — wide
enough to read a winding path and landmarks, not a 2×2 stage. Rather than
stretching one camera to do both jobs, the plan is two presets sharing the
same fixed yaw (world orientation never changes, only pitch/zoom):

| | Home (current) | Overworld (new, for the larger map) |
|---|---|---|
| Pitch | 33° | ~58–62° (steeper, more top-down) |
| Yaw | 45° | 45° (unchanged) |
| Ortho size | 2.55 (tight) | ~9–11 (wide) |
| Use | Farm plot, farmhouse, any character-scale scene | Forest path traversal, landmark/ruin spotting |

`FarmIso.ApplyOverworldCamera(Camera cam, Vector3 lookAt)` has been added
alongside the existing `ApplyAestheticCamera` as a non-breaking preset —
nothing currently calls it; it's ready for whoever wires up the larger map
(roadmap item 3) to switch modes at a zone boundary instead of re-deriving
the numbers from scratch.

## Map layout direction

For the larger map: one continuous grid (not separate scenes) — forest path
leads to the farm gate, farm leads to the farmhouse as the day/night anchor.
Ruins/stone pillars sit at path junctions as waypoints, matching
`forest-path-exploration.png`. The stream is a natural boundary/guide line
before the farm, not a hard wall. Farmhouse is where night visually
"belongs" — reinforces it as the home-base/rest point.

## Palette carry-over

Keep the existing dusk-purple/warm-spotlight language (`FarmVisuals.
BuildAtmosphere`) for Home, and extend rather than replace it:

- Forest (day): deep teal-green base, warm gold light-shaft accent.
- Farm (day): current warm soil/crop palette already matches the reference.
- Farmhouse (night): current dusk fog color shifts toward deeper
  indigo/navy, with the existing warm spotlight repurposed as lantern glow.
