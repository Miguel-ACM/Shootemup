# Desert district

After Thunder or Tunnel, choose **Desert** or loop straight to Plains. Clearing Desert starts the next Plains loop with the existing loadout and score. Desert is also available in Debug Run and Wave Watcher.

The scenery uses asymmetric sculpted sand ridges with broad windward slopes, short slipfaces and restrained facet lighting, paired small silos and compact gray outposts with roof vents, doors, antennas and pipe manifolds. Irregular crests taper into open sand. The seamless layouts vary with the run seed; the previous repeated dark crescents have been replaced.

Dust storms build into rolling sand banks, with faster wind streamers and flying grains, then fade into clear intervals. All dust renders over the scenery and behind ships and projectiles. Its materials and meshes are released with the district.

Building and silo faces share the dunes' sun direction. Their shadows are baked onto the sand from box and cylinder silhouettes, with a softened edge; close-fitting concrete foundations replace the oversized dark pads. The bake is performed when the district is prepared, not during combat.

Three new enemies have their own modeled hulls:

- **Sand Emitter:** a buried eight-port machine. Disturbed sand precedes its emergence; it releases evenly spaced rings and burrows again. Concealed machines do not attract homing shots.
- **Sand Striker:** a narrow interceptor that locks one aim direction and sends six shots 75 milliseconds apart.
- **Sand Worm:** a small articulated machine with four following body sections. Alternating body ports release coordinated fans. Its body can be shot as well as its head.

The **White Worm** boss has seven independently destructible body reactors and one larger head reactor. Their exposed radii are now about 22% smaller, with matching smaller hit regions and muted brick-red paint. All eight must be destroyed. Dead reactors stay dark on the moving wreck and stop firing. At four remaining reactors, the boss accelerates its attack score. Its route accounts for the visible playfield rather than the full unmasked camera output.

All geometry is authored in the runtime mesh factories, following the project's existing asset pipeline. No external model downloads are required. Existing projectile art and music are reused; Sound Lab selections are unchanged.

## Verification and visual review

`Assets/Editor/VerifyDesert.cs` compiles and runs in an isolated Unity project. It checks standard and extended route loops, terrain wrapping, 57 seeded wave compositions, all sixteen playable motifs, concealed targeting, real wave firing, clear weather and full dust gusts, projectile collisions against moving cores, head-first destruction, repeated hits on dead parts, full boss completion and cleanup.

`Verification.txt` and `Geometry.txt` contain the results. `Scenery.png`, `DustStorm.png`, `Boss.png`, `DamagedBoss.png` and `RouteChoice.png` are captures of the actual filtered game and HUD. `MotionReview.jpg` shows successive formation and boss poses. `DesertMotion.mp4` is sampled playback, not a frame-rate benchmark; wave timing comes from the capture timestamps and boss samples use the nominal quarter-second interval.

Rebuild the review with Unity's `-executeMethod VerifyDesert.Run` in a disposable project, then run `Tools/render_desert_review.py` using the project's bundled Python runtime.
