# Visual clarity and motion review

Hostile ordnance now has twelve distinct pixel silhouettes and palettes, with ink rims and stepped highlights matching the existing projectile style. Ordinary shots retain their authored artwork. Colour remains recognizable while guided, delayed and armed projectiles change state.

The paired helix rounds have short, tapered pink trails. Shell ignition particles start after the shell stops, move inward and gather closer to the shell as its fuse expires. Laser side volleys charge six outward-facing outlets at the actual launch positions; their projectiles use a separate lavender star sprite. Pooled rounds restore their enabled state, clear old trails and hide old outlet warnings.

Flight curves use rounded cubic B-splines with a finer distance lookup. The ten tightest hairpins were also widened in Plains, Snow, Thunder and Tunnel. Route checks measure speed consistency and turn rate at 30, 60 and 144 FPS. The interpolation comparison evaluates the previous and new interpolators against the same guide points.

`Verification.txt` records the sprite, trail, pooling, converging-particle, side-volley and motion checks. `WaveVerification.txt` records 855 seeded layouts, harder loops, normal director handoffs and fifteen complete live encounters using the revised paths. All reviews ran in the separate `Temp/FeedbackUnityProject` copy.

- `BulletStyles.png`: enlarged sprite reference, with the needle and missile proportions applied.
- `BulletGallery.jpg`: actual sprites rendered over the game environment and filter.
- `Helix.mp4`, `Shell.mp4`, `Laser.mp4`: actual moving effect captures.
- `Warnings.jpg`: successive close-ups of the shell and lateral laser warnings.
- `SmoothedFormations.jpg`: successive moments from real wave playback in all five districts.

Run `VerifyVisualClarity.Run` through Unity's `-executeMethod` in the disposable project, followed by `Tools/render_visual_clarity_review.py` to package passing captures. `VerifyVisualClarity.Bullets` repeats the projectile checks while retaining an existing passing full wave review.
