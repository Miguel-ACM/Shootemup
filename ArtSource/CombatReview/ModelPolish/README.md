# Enemy model review

These sheets render the actual runtime meshes and fleet material in an isolated Unity preview scene. They do not substitute for the moving combat review.

- `FleetFront.png` and `FleetAngle.png`: all 27 runtime enemy/boss hulls, including the separate Snow avalanche bomber (17) and Thunder arc lancer (18).
- `FleetGameplayScale.png`: runtime model scales at approximately 28.2 pixels per world unit, matching 480 internal lines over the 17-unit arena. Empty space is intentional; small enemies are shown at their actual relative size.
- `SnowClose.png` and `ThunderClose.png`: enlarged district groups for checking surfaces, apertures, and geometry.
- `HullIds.txt`: the row order of each sheet.
- `PolygonBreakupVerification.txt`: actual Unity checks for four legacy hulls and four district hulls, including boss lifetime, transformed geometry, exhaust exclusion, and generated-mesh cleanup. Moving breakup clips are in the adjacent `Polish` review folder.

The redesign replaces repeated slat towers with recessed propulsion housings, uses shaped armor shoulders and faceted glass, and gives every active hull an explicit builder. Snow's siege sled has a broad three-gun deck, the mortar has a single oversized central barrel, and the avalanche bomber carries two rounded pressure vessels. Thunder uses an open lens crescent, a three-needle capacitor fighter, a gull-wing skimmer, an asymmetric lance, a slim boss with six shoulder emitters and a central lance, and a transverse transformer with a rectangular emitter. The tunnel saw has exposed mechanical slots and alternating cutter teeth, with no engine sockets.

The shared geometry retains separate animated exhaust. The scout, maintenance tug, attack drone, and courier use a single engine; their exhaust sockets and the friendly support drone were updated with the models. Beam effects attach to the corresponding lens or rail sockets.

The `Polish/ModelAttackSiegeGunship` and `Polish/ModelAttackStormReactor` captures check the redesigned hulls against their actual district scenery and filter. The siege shells leave all three visible gun mouths; the Reactor beam begins inside its rectangular recess. The final Snow moving capture also shows the wider siege deck banking in a three-ship echelon with clear gaps between ships.

To regenerate while Unity is in Edit mode and compilation is complete, create `Temp/render-fleet.request`. `RenderFleetReview` writes these sheets and reports completion in `Temp/render-fleet.result` without starting a run.
