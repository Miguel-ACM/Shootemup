# Longer, varied Tunnel passages

5 November 2026. Supersedes the three straight channels in `SolidSceneryReview`.

- Six authored layouts: narrowing throats, zigzag, chicane, alternating side buttresses, chambers and a long switchback. Each run selects three using separate seed bits.
- Passages stay anchored to the backdrop at 10.2 world units/second. Their measured travel through the arena lasts 7.55–11.28 seconds, up from approximately 5.7 seconds. The later layouts last longer.
- Sloping collision faces match the actual mesh. Silver rails and brass marks outline the walls. Each complete layout uses one warmed mesh and renderer; only nearby slabs participate in collision queries.
- Four enemy phrases follow the changing corridor centre with small authored lateral offsets. Their firing scores stay shared across difficulties. Choices and following encounters wait until the passage clears.
- A wall collision costs one health segment through the normal hazard-damage path. The usual invulnerability, hit effects, shield and no-hit tracking apply. Recovery returns the ship to open space instead of trapping it in the wall or beyond the bottom edge.

## Verification

`Runtime.txt` records the first isolated headless batch: all six layouts on Easy, Normal and Hard, zero enemy/wall penetrations, longer durations, warmed hostile rounds, exact diagonal sweeps, and 86,564 recovery samples without a trapped ship. Its two contact assertions failed because Wave Watcher disables the player controller.

`Fixes.txt` records the targeted correction to that test setup: enable the real controller, verify one-segment contact damage, invulnerability, and an actual scrolling wall catching a stationary pilot. It also compares complete headless frames during a 3,000-round burst with and without walls. The initial batch was not repeated. `Build.txt` records the Windows build.

`Layouts.png` shows complete exported Unity meshes. `Switchback-Sequence.png` and `Switchback.gif` show recorded native positions and meshes. These use CPU lighting and dots for bullet positions; they are not gameplay screenshots or a GPU performance review. The actual game backdrop, bloom and final compositing are excluded.

Use Unity 6000.6.2f1 with `-batchmode -nographics` and the isolated `Build/ReviewCache/Temp/DecisionHoverReview` checkout. Entry points: `VerifyTunnelVariety.Run`, `VerifyTunnelVariety.Fixes`, `VerifyTunnelVariety.Build`. Offline previews use `Tools/render_tunnel_variety.py` with NumPy and Pillow. Raw frame exports are temporary and can be regenerated; compact visual evidence is committed.
