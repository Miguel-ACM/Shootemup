# Full-frame tunnel boss profile

Captured in isolated Unity 6000.6.2f1 Play mode at 1280×720. Each comparison covers 22 seconds of the actual main saw and both auxiliary saws, natural wall bounces, both phases, the production player collider, sprite renderer, effects and glow. Weapons are disabled to keep the comparison reproducible; no manual screenshots are taken inside the measured window.

| Full encounter CPU metric | Before | Final |
| --- | ---: | ---: |
| Worst `Physics2D.Simulate` | 273.357 ms | 3.708 ms |
| 99th-percentile physics | 31.935 ms | 0.483 ms |
| Total physics over 22 s | 8322.2 ms | 308.3 ms |
| 99th-percentile main thread | 36.886 ms | 7.146 ms |

`Baseline-Stacks.txt` identifies `Physics2D.FindNewContacts` as the long stall: 271.841 ms in the worst retained frame. The burst's spawn loop was under 4 ms. Prewarming alone therefore did not address the reported freeze.

Each ordinary hostile round now registers one simple native contact shape. Only near-player/blocker bodies participate in physics. A narrow phase checks the same opaque-pixel rectangles against the player's convex hexagon or the companion's circle/box, preserving holes and native polygon skin. Destructible plasma retains its original native collision against player fire. Beam and mine rules are preserved. There are no extra projectile GameObjects and the 280-live-round cap remains unchanged.

`Baseline.csv` and `Optimized.csv` contain every recorded frame and native/script markers, whose time values are nanoseconds. Main-thread recorder samples finalize later than the in-frame physics/script samples, so correlate adjacent rows when comparing `dt_ms`. `Summary.json` reports aggregates over the full recordings. The raw binary captures remain in the isolated project's Temp directory; stack exports retain Unity's last 2000 frames.

The final recording contains one 287.7 ms wall-time gap with no burst or spawn in that frame; adjacent physics samples are under 0.2 ms. Its cause is not established by the retained stack window. It is included in the CSV and main-thread maximum, not removed from the results. These measurements establish the collision bottleneck's improvement; they are not a guaranteed whole-game FPS figure.

The combined contact review in `../CombatBalanceReview` verifies 4800 rotated/scaled cases against the original native fixtures, actual player hits, transparent holes, companion blocking, and distant destructible plasma.
