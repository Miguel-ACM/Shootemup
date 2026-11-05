# Celestial pattern correction — 5 November 2026

Miguel confirmed that the two encounters were the Heaven and Void bosses. Their health, approved projectile artwork, district colors, phase breaks and rewards are preserved.

- Heaven releases upper and lower feather lobes, with six charged apparitions creating fire in empty air. Its three wheels tumble on different axes while turning around their own axles; phase speed factors are 1, 1.55 and 2.1. They continue moving during attacks, with beams attached to committed eyes and all 37 damage paths retained.
- Each Void disappearance has one source side for all fifteen active seconds. Two visible chevrons mark the sources. Top funeral fans, right-side scissor brocade and bottom scalloped waves have mirrored releases and continuous moving gaps. The four visible forms have stronger folds, faster phrase handoffs and coordinated replies.
- Space's first and final phase draw circular fronts at 2.6-unit intervals across the arena. Neighboring fronts turn oppositely; alternate circles carry dark stars. The core replies outward through the inward fronts. Tells clip to the combat rectangle, and births keep clearance from the player.

`Runtime.txt` is the combined passing review. `Runtime-Batch.txt` preserves the earlier reserve failures: Hard Heaven exceeded its old needle reserve, and Hard Void's final form exceeded its ricochet reserve. The targeted rerun retained unaffected cases and checked all revised Space phases, Hard Heaven and Hard Void's final form. All revised cases reuse prepared projectile objects. Global limits remain 3,600 live and 7,400 pooled; the 3,200 standard-round and 800 giant-orb reserves remain intact.

The recorded Hard peaks are 2,232 live rounds for Heaven's final phase, 1,400 for Void's final form, and 1,785 / 1,415 for Space's first / final phase. Native birth positions verify circular spawns beyond six units from Space's core in all four vertical screen bands, and exactly one source edge for every Void disappearance. The same score geometry runs on all difficulties. Wave Watcher's extra Easy third-phase previews are included; this does not add a campaign phase.

Earlier Hard captures in `ArtSource/BossPass/Patterns` averaged 5–9 rounds in Heaven's lower third; this pass averages 61–80. Void's corresponding averages rise from 60–90 to 102–151, depending on form. These are sampled occupancy comparisons, not a claim of playtested difficulty.

`Profile.txt` records complete CPU frames around the densest Heaven/Space phases without trajectory-recording overhead, including following physics steps. Rendering is disabled, so these are not full gameplay FPS measurements. `Build.txt` records the Windows build result. The review uses its own preferences and isolated project at `Build/ReviewCache/Temp/DecisionHoverReview`; no desktop controls or graphical Unity review were used.

`PatternGeometry.jpg` and the selected GIFs are diagrams of native Unity projectile positions, not screenshots. They omit beam footprints and hull art. Raw trajectories remain locally under ignored `Patterns/`. They establish geometry and motion, not Miguel's visual or difficulty approval.

Reproduce the command-line batch with `VerifyCelestialPatterns.Run`, the revised/failed cases with `RunFixes`, the capture-free dense-frame sample with `RunProfile`, and the Windows build with `Build`. `Tools/review_celestial_patterns.py` renders the position diagrams. The design reference is [ZUN's explanation of giving attacks a distinct identity](https://en.touhougarakuta.com/article/specialtaidan_zun_hiroyuki_5-en/).
