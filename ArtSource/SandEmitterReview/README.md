# Buried desert emitter

The emitter first chooses clear sand, then throws grit as its armored top rises through the terrain. A shallow displaced sand collar and close contact shadow join it to the surface. Its footprint stays the same width, with no flight banking. Retreat ejects more sand and disables its damage and homing target region.

The selected site belongs to an actual scrolling terrain section. Its local position stays fixed through terrain movement and player parallax. Placement excludes the projected silhouettes of all building solids: foundations, roofs, silos, pipes and service blocks, with clearance for the full sand collar. Other grounded emitters also reserve their own space.

Formation clocks and shared firing accents keep running after anchoring. The existing fourteen-shot rings are preserved. The machine's hull renders with the scenery; terrain depth hides it below the sand during emergence and retreat. Airborne enemies and bullets render above it.

`VerifySandEmitter.Run` checks candidate sites against raycasts into the actual building meshes, targetability, ballistic grains, real projectile hits, ground attachment through scrolling and parallax, and live composed waves. Captures use the game filter and HUD. The review project uses committed game assets with these emitter changes, excluding concurrent projectile migration work in the main workspace.

`Warning.png`, `Emerging.png`, `Exposed.png` and `Burrowing.png` show the lifecycle. `GroundEncounter.mp4` shows sampled moving combat using recorded frame timestamps. Rebuild with `Tools/render_sand_emitter_review.py` after a successful verification run in `Temp/DesertReviewProject`.
