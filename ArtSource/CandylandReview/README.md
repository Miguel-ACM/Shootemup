# Candyland

After Thunder or Tunnel, choose Desert, Candyland, or the next Plains loop. Both fourth districts loop back to harder Plains with the current score and equipment. Candyland also appears in Debug Run and Wave Watcher. The three-way junction leaves the bottom quarter neutral; hold a route above it to travel. The existing two-way choices keep their original controls.

Candyland's pink cloud sky has layered drifting billows and 25 falling, tumbling sweets at two depths. The new modeled props are spiral lollipops, six-square chocolate bars with folded wrappers, and wrapped mint candies. They have no collision and stay behind combat. Confections use their own trim atlas; projectile artwork is unchanged.

- Bonbon Skirmisher: small wrapped hull, twisted side fins and a mint visor. Fires one aimed round per accent.
- Wafer Dart: chocolate-topped wafer keel and narrow swept wings. Fires two separated needles.
- Mint Spinner: six radial peppermint vanes around a dark central emitter. Fires a three-round fan.

Eighteen authored motifs use nine Candyland routes, with ribbon rushes, zipper passes, curls, braids and fountains. Waves compose three or four formations. Short travel times and more ships provide pressure while each enemy fires fewer rounds. The miniboss slot is a fast swarm composition at budget 16.

The Candy Regent has 880 base health before normal run scaling, compared with the Plains boss's 1,440. Its continuous looping dashes accelerate at half health. Expanding mint hatches warn of each release of six minions, rising to eight in phase two; at most twelve summoned minions can coexist. They follow mirrored descending curves, can be destroyed, and disappear when their boss dies. The Regent itself fires two or three needles with deliberate rests.

New models, palette textures and cloud rendering are authored in code following the game's existing asset pipeline. Existing City music and combat sounds are reused, preserving Sound Lab selections.

## Review

`Assets/Editor/VerifyCandyland.cs` validates the route loops and three-way choice, 57 seeded wave compositions with swept model spacing, all eighteen motifs, live waves, boss speed and lower health, destructible minions, stock projectile hits, and cleanup. It runs in the isolated `Temp/DesertReviewProject` copy of the project.

`Scenery.png`, both boss phase captures, `RouteChoice.png` and `MotionReview.jpg` show the actual filtered game. `CandylandMotion.mp4` is sampled playback, not a frame-rate benchmark: wave durations use recorded timestamps, and boss samples use nominal quarter-second intervals. Rebuild it with `Tools/render_candyland_review.py` after a successful `VerifyCandyland.Run`.
