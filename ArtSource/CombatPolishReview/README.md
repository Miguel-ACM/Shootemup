# Combat performance, palettes and tank driving

Captured from Unity 6000.6.2f1 in the isolated review project. The active game editor was left alone.

## Matched CPU workload

280 mixed hostile rounds with their existing compound pixel colliders, moving through 24 enemy hulls. 220 synchronized physics steps, 30 warmup steps excluded.

| Measurement | Before | After |
| --- | ---: | ---: |
| Median physics + transform sync | 26.5286 ms | 20.7660 ms |
| p95 physics + transform sync | 36.5831 ms | 25.2421 ms |
| Objects used for four 280-round bursts | 976 | 280 |

The median measured CPU section fell by 21.7%. This is an editor physics benchmark, not a claim about total game FPS. Bullet counts, source sprites and exact pixel collision outlines are preserved. Filtering removes contacts against enemy hulls and other hostile ordnance; tests exercise real player-layer and companion-layer contacts and friendly rounds destroying plasma.

## Visual review

`DistrictPalettes.jpg` shows all twelve hostile sprite types in each district with separate cyan player rounds. `MotionReview.jpg` and `CombatPolishMotion.mp4` show the actual tanks following road bends and the Snow boss's shell patterns through both phases. Full frame captures keep the game's filter, HUD and aspect ratio.

Snow boss health is 2100 (previously 3200); run scaling and attack phases remain intact. Tank driving is downward relative to both the camera and scrolling terrain; chassis turn to the road tangent while turrets track the player.

See `Verification.txt` for contact, pixel-outline, palette reuse, heading, stock-gun damage and boss checks. `DiscRotation.txt` checks the existing disc spin after the orientation cache change. Reproduce with `VerifyCombatPolish.RunBaseline` on the preceding code, then `VerifyCombatPolish.Run` and `VerifyCombatPolish.CheckDiscRotation` on this code in a separate Unity project. Package with `Tools/render_combat_polish_review.py`.
