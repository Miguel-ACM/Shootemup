# Scrolling Tunnel passages and underwater mines

Implemented 5 November 2026. Miguel chose walls anchored to the tunnel, scrolling downward to form passages.

## Tunnel

Ordinary waves 4, 6 and 9 (budgets 6, 10 and 16) introduce centered, left and right passages. Seed parity swaps the two offset passages. Each opening is 4.7 world units wide; its side structures travel at the backdrop's 10.2 units/second. Three authored enemy trains thread through the opening. Adjacent waves wait for the passage phrase, and pending encounter hexagons wait until the walls leave.

The pilot slides against the walls. Moving walls resolve an overlapping ship back into open space, including at the bottom edge. Swept segment checks stop fast, piercing and returning player rounds, ordinary hostile rounds, electricity, static arcs, companion lasers and continuous player/hostile beams. No dense bullet fixtures were added to the native physics broad phase. Existing pixel contact against ships is unchanged.

Walls clear on decisions, district transitions, defeat and Wave Watcher resets. Wall meshes warm behind the cloud curtain.

## Underwater

Depth mines have a faceted body, eight contact horns, a tether and an amber fuse. Three formations join the seeded ordinary-wave composition. Mines drift without firing; damage starts a 0.48-second flashing fuse, stops movement, and releases one 360-degree burst of existing circular rounds. The burst uses alternating speeds and a small speed ramp. Difficulty controls density through the existing shared score. Clearing an unshot mine produces no burst.

## Evidence and limits

- `Runtime.txt`: the three passage waves and mine detonation on Easy, Normal and Hard; zero enemy/wall penetrations; all six player projectile deliveries and hostile shots blocked; actual hostile laser damage blocked with an unshielded control; no new hostile round allocation during the tested waves/bursts.
- `Extras.txt`: 225,000 moving-wall recovery samples, no trapped positions; decision cleanup; mines present in ordinary waves for three seeds. A separate wall-entry window measured 3.70 ms maximum main-thread time including subsequent physics frames, without graphics rendering.
- `Mines.txt`: final weak-shot checks at loop 12, including fuse position and one complete burst on each difficulty.
- `Build.txt`: isolated Windows player build result.

`Mine-Views.png`, `Passages.png`, `Passage-Sequence.png` and `Passage-Motion.gif` render actual exported Unity meshes, atlas UVs and recorded native formation positions using CPU lighting. The previews show geometry and movement, **not the game's renderer, backdrop, bloom or final compositing**. Orange dots indicate projectile positions, not replacement artwork. This is not a human gameplay approval or a GPU performance measurement.

## Reproduction

Use the isolated `Build/ReviewCache/Temp/DecisionHoverReview` project with Unity 6000.6.2f1, `-batchmode -nographics`. Run `VerifySolidScenery.Run` for the main batch; `Extras` for targeted geometry/model checks; `Mines` for the final trigger checks; `Build` for the Windows player. These methods refuse the live project and graphical mode. Run only failed or revised cases after the batch.

The review exports local mesh JSON and atlas data. `Tools/render_solid_scenery.py` renders them with Python, NumPy and Pillow. Raw frame exports remain local; the compact visual evidence is committed.
