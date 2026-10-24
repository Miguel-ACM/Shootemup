# Enemy shell ranges and boss scores

`DistrictEnemy.shellTravelDistance` selects shell travel in world units. Set it in the enemy's `Configure()` or on the spawned instance. `Fan`, `FanFrom` and boss `Radial` apply it through `Fire`. Individual scored volleys can override a returned shell with `SetTravelDistance(distance)`. Bullet speed changes the flight time, not the selected distance. Pooled shells reset their settings at launch.

Heavy Bomber uses 5.2 units, Avalanche Bomber 8.4, Plains Heavy Wing 5.4, Snow Siege Craft 7.2, Plains boss 4.6, Snow boss 6.6 and City heavy 5.8. Snow alternates its crown rows between 6.6 and 4.6 units.

Fragmentation emits all 12 headings at 30-degree spacing. Mines likewise emit their complete 10-shot ring. A charged projectile waits if the 280-round budget cannot fit the whole ring; it never emits a partial angular sector. Normal lifetime and explicit clearing still bound/remove the parent.

| Boss | Repeating score | Second phase |
|---|---|---|
| Snow | Four shell crowns, two staggered suspended combs, two opposing needle fans; rests between phrases | Seven shells per crown instead of five, denser combs/fans, faster score |
| City duo | Heavy owns beats 0/2/4 and 6/7; fast owns 1/3/5 and 8/9; 10/11 rest | First death gives no drops. Survivor heals half missing health, resets its score, speeds up and fills missing accents |
| Thunder | Two charged side lasers alternate with paired aimed needle releases | Seven attached beams sweep together, reversing each cycle; eight nine-shot barrages share one committed axis, followed by a cooling rest |
| Tunnel | Physical bounces alone trigger inward-facing mixed-size combs; alternating small-shot offsets | Faster travel and denser combs; pauses to deploy two sturdy destroyable saws with identical bounce-only emission |

City partners share a tempo until one dies; separate half-health thresholds do not desynchronize the duo. No post-Plains boss adds an independent continuous pressure timer over its score. Tunnel saws remain thruster-free and never emit shots from damage or timer callbacks.

Run `VerifyBossPatterns.Run` in an isolated Unity project copy for selected ranges at 30/60/144 FPS, complete and saturated rings, pattern counts/rests, charged emitters, alternating sweeps, duo healing, saw bounce behavior, and both-phase gameplay captures. Output: `ArtSource/CombatReview/BossPatterns/`.
