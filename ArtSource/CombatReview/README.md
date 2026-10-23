# Combat visual review

The Unity batch writes the rendered fleet, district formation sequences, boss phases, equipment HUD and shops into this folder. Images are actual game renders, not mockups.

`Verification.txt` records the combined behavior checks. `FlowVerification.txt` records the separate real wave-controller pacing check. The player's health is raised in the verification fixture so damage does not interrupt coverage; these checks do not establish that difficulty is perfectly tuned.

`FieldsFormation0..5`, `TunnelFormation0..5`, `SnowFormation0..5`, `ThunderFormation0..5`, and `CityFormation0..5` follow a formation at 0.8-second intervals. The player starts firing after 2.2 seconds. `LiveWaveFlow0..7` follows the actual wave controller at three-second intervals with the base weapon firing and lateral player motion.

`FleetModels.png` identifies hulls by their factory ID:

| IDs | Hulls |
| --- | --- |
| 00–05 | Scout interceptor, patrol gunship, heavy bomber, wall turret, maintenance drone, armored carrier |
| 06–11 | Cold-weather fighter, siege gunship, shield transport, beam drone, relay drone, storm skimmer |
| 12–17 | Pursuit fighter, attack drone, command gunship, courier diver, avalanche bomber, arc lancer |
| 20–23 | Tunnel saw, snow citadel, thunder needle, fields fortress |
| 24–28 | Heavy wing, tunnel bastion, snow mortar, storm reactor, command ark |

To repeat the combined batch, refresh scripts in Unity and wait for compilation to finish, then create `Temp/verify-combat.request` containing `all`. Use `flow` for the focused pacing check. Write to a temporary sibling first and rename it to the request path to avoid a partial write. The editor watcher starts one play session, saves results, then exits Play mode. Never queue this during the player's own run.
