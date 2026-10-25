# Requested item expansion

All fifteen items are registered in normal reward/shop pools and discovered by Debug Run. Each item was implemented in its own commit. Models are built by `RequestedItemMeshes.cs` and use the existing painted fleet material; projectiles retain the established solid geometry.

| Item | Behavior / default tuning |
| --- | --- |
| Shrinker juice | Green bottle; hull, exhaust and ship hitbox become 70% size. Root, projectile size, weapon stats and equipment are unchanged. |
| EMP Pulse | Clears friendly and hostile bullets every 20 combat seconds. Pauses during decisions. |
| Quality glasses | Enemy projectile speed is multiplied by 0.9. |
| Bad glasses | The same 0.9 multiplier within three world units of the player, removed outside. Combines with Quality glasses to 0.81 locally. |
| Gasoline leak | Red rounds apply stacking burn; visible hull flames. Uses the existing capped burn damage system. |
| Poison | Green flask; green rounds refresh a five-second poison. Poison and burn coexist; combined rounds have a red nose and green tail. |
| Barrel roll | No energy cost; 2.6-unit side roll over 0.38 seconds with i-frames, 2.5-second cooldown. Uses held horizontal direction, otherwise the equipped slot's side. Preserves reserve energy. |
| Bouncing ball | Primary projectiles redirect once toward another nearby target after hitting. Legacy shots exclude the original target from their redirected round. |
| Focus time | Desk and book; Shift movement becomes 60% of its previous focused speed (30% of normal with the default focus setting). |
| Double trouble | Either fire button shoots both equipped weapons. Their slot identities and replacement controls remain intact. |
| Energy core | Regenerates three energy per combat second, up to capacity. Does not recharge during decisions. |
| Health packs | Active: 30 energy for 20 health, eight-second cooldown. Cannot spend energy at full health. |
| Laser drone | Small trailing companion with charge, continuous attached laser and release. Reuses the existing beam audio. |
| Bullet drone | Companion with twin normal projectiles. Both drone types share a staggered, progressively smaller formation, with independent world positions. The tail bends inward at the bottom boundary. |
| Bullet time | Active: 35 energy, 12-second cooldown. Stops the world for two seconds while player movement and weapon emission continue. Friendly rounds remain suspended until release. Pause preserves the remaining freeze duration. |

Verification used Unity 6000.6.2f1 in `Temp/RequestedItemsProject`, an isolated project copy that did not stop the existing Unity Play session. `VerifyRequestedItems.Run` compiled and exercised actual projectile collisions, status damage, keyboard movement and firing, active costs, i-frames, reserve preservation, companion damage and formation, frozen beams/enemies/drones, pause/resume and the full twenty-second EMP interval. See `Verification.txt` for the passing assertions.

The two model sheets, gameplay effects, HUD portraits, drone reversal sequence and bottom-corner frame were visually inspected. This is technical and visual verification; sound choices reuse the existing samples without changing saved Sound Lab selections or claiming a subjective listening review. No standalone player build was produced.
