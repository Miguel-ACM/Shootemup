# Candyland

After Thunder or Tunnel, choose Desert, Candyland, or the next Plains loop. Both fourth districts loop back to harder Plains with the current score and equipment. Candyland also appears in Debug Run and Wave Watcher. The three-way junction leaves the bottom quarter neutral; hold a route above it to travel. The existing two-way choices keep their original controls.

Candyland's sky layers sculpted cotton-candy puffs, peach highlights and lavender hollows. Thirty falling, tumbling sweets move at two depths, with larger pieces framing the sides. Props include rimmed spiral lollipops, six-square chocolate bars with folded foil, wrapped mint candies, cream-filled strawberry macarons and hooked striped candy canes. They have no collision and stay behind combat. Confections use their own trim atlas; projectile artwork is unchanged.

- Bonbon Skirmisher: small wrapped hull, twisted side fins and a mint visor. Fires one aimed round per accent.
- Wafer Dart: chocolate-topped wafer keel and narrow swept wings. Fires two separated needles.
- Mint Spinner: six radial peppermint vanes around a dark central emitter. Fires a three-round fan.

Eighteen authored motifs use nine Candyland routes, with ribbon rushes, zipper passes, curls, braids and fountains. Waves compose three or four formations. Short travel times and more ships provide pressure while each enemy fires fewer rounds. The miniboss slot is a fast swarm composition at budget 16.

The harder Candy Regent has 1,320 base health before normal run scaling, 50% above its first version but still below the Plains boss's 1,440. Twelve dash destinations produce curved cuts, double-backs, deeper dips and brief braking beats. Phase two speeds up both dashes and attack timing. It fires three- or five-needle accents followed by alternating seven- or nine-shot combs, with three rests in each eight-beat cycle. The base attack interval is 0.36 seconds, down from 0.90.

Expanding mint hatches warn of each mixed brood: eight minions every 4.1 seconds in phase one, ten every 3.25 seconds in phase two, capped at eighteen alive. Bonbons, wafer darts and peppermint enemies follow mirrored descending curves. They can be destroyed and all surviving types disappear with the boss.

New models, palette textures and cloud rendering are authored in code following the game's existing asset pipeline. Existing City music and combat sounds are reused, preserving Sound Lab selections.

## Review

`Assets/Editor/Checks/VerifyCandyland.cs` validates the route loops and three-way choice, 57 seeded wave compositions with swept model spacing, all eighteen motifs, live waves, boss speed, sharp turns and vertical coverage, increased bullet pressure, mixed destroyable broods, stock projectile hits, and cleanup. It runs in the isolated `Temp/DesertReviewProject` copy of the project.

This boss/scenery review uses the committed game assets plus these changes. Concurrent projectile migration work in the main workspace is excluded from the validation snapshot.

`Scenery.png`, both boss phase captures, `RouteChoice.png` and `MotionReview.jpg` show the actual filtered game. `CandylandMotion.mp4` is sampled playback, not a frame-rate benchmark: wave durations use recorded timestamps, and boss samples use nominal quarter-second intervals. Rebuild it with `Tools/render_candyland_review.py` after a successful `VerifyCandyland.Run`.
