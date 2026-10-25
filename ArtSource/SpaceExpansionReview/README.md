The campaign now uses four combat waves (2, 8, miniboss, 20), one encounter
decision and the boss. This removes six combat waves per district while retaining
the full difficulty ramp. A challenge can still cover the three remaining waves.

WaveHandoff samples the actual authored routes once when a wave starts. It aligns
the next visible entrance with the previous visible exit for roughly 0.35s of
overlap, rather than waiting through offscreen path tails. All outgoing units must
have launched, the existing live-enemy cap still applies, and decisions/bosses
wait for clear combat. A living miniboss is not overlapped by the next wave.

Challenge is now a three-wave no-hit objective at normal budgets and enemy stats.
The player's real damage event cancels it immediately. Three clean completions
open the existing item reward; ordinary wave difficulty is never boosted by it.
The player banks up to 28 degrees; its collider and hitbox marker remain independent.

Space follows either Desert or Candyland after the mandatory boss item. It then
returns to harder Plains after its own boss item, retaining the run's equipment,
score and wallet. Existing serialized district IDs are preserved; Space is ID 7.
The wave watcher includes all new roles, formations and both station phases.

Space assets are original procedural models: ivory/gold satellite bodies, gridded
blue solar cells, parabolic dishes, carrier hubs, rotating escort moonlets, an
open-truss station, breakable generators/turrets, a reactor and retracting shutters.
The background uses one distant shader surface for slowly drifting stars and
soft blue/lavender planets. There are no per-star objects or new particle systems.
The four satellite roles have no baked thrusters and use the existing pooled
projectile artwork. Space hostile fire shares a contrasting pink palette.

Both shield generators must break before the core accepts hits or homing targets.
Gun pods can be destroyed independently to reduce pressure. The boss uses an
attached charged laser down the arena center, side-wall ricochets and aimed rests
and answers. Core destruction uses the established intact-hull blasts, half-second
freeze, music cut and mandatory item gate. Existing music and laser effects are reused.

Editor.txt records 57 seeded wave compositions, all ten authored Space motifs,
route checks and native filtered model views. Runtime.txt records the complete
challenge/banking/escort/boss/item/loop batch. Its early visible-overlap measurement
is superseded by Pacing.txt: the focused follow-up passed with both cohorts on
screen together, eleven peak actors and 24.5 degrees of actual measured banking.
The boss was not replayed during those pacing follow-ups.

Native images and Orbital-Choreography.png were visually inspected, including the
actual charged laser, breakable-module states, HUD and scrolling planets.
Orbital-Passes.mp4 and Station-Attacks.mp4 use four captured frames per second for
visual review, not frame-rate measurement. Compilation.txt records a final clean
rebuild of all 108 assemblies with zero compiler warnings and errors.
