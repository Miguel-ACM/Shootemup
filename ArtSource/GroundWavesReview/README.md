# Plains and Snow ground formations

Plains adds the Meadow Tank: an olive tracked chassis with separate tread pads, an engine deck, headlights and an independently rotating turret. It follows the west-bank service road through the seeded valley, with pairs of aimed needles on coordinated accents. Its 27 base HP keeps the opening stock gun effective.

Snow adds the Frost Auger: a squat white siege crawler with a split ice plow, two visibly rotating helical propulsion screws and an articulated twin mortar rack. Its paired shells split into smaller rounds using Snow's existing projectile behavior. It leaves darkened snow furrows and throws powder behind its screws.

Each district has six new ground motifs in the normal wave catalogue. Ground convoys retain staggered trains and shared firing accents; successive motifs vary pace and route. A wave includes at most one ground convoy so Plains tanks cannot be scheduled on top of one another on the single road. Air formations still compose alongside them.

The tank chassis follows the road's terrain coordinates, keeping it clear of the river and the vegetation clearances already authored around the road. Snow crawlers follow their own curved routes across the open central snowfield. Their surface height comes from the actual snow mesh, including drift crests. Track marks store terrain-local corners so scrolling and player parallax move them with the scenery.

Ground hulls render in the scenery layer, with their bases intersecting the terrain; their hit regions stay in the combat plane. Weapons rotate separately from the driving chassis. Both types use the established fleet atlas, approved projectile pixels and combat sound workflow.

## Verification

`VerifyGroundWaves.Run` checks 114 seeded normal compositions, all twelve new motifs and mirrored previews, then renders dedicated convoys and mixed encounters in both districts. Runtime checks cover surface contact against actual mesh raycasts, dry Plains routes, attached marks, absence of thrusters, real firing, and actual stock BaseWeapon time to kill against moving ground targets.

The captures show the actual game filter and HUD. `GroundWavesMotion.mp4` is sampled playback using recorded timestamps. Rebuild with `Tools/render_ground_waves_review.py` after successful verification in the isolated `Temp/DesertReviewProject` copy.
