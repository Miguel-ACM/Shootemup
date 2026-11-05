# Boss visual pass — 5 November 2026

Miguel rejected the smooth Heaven/Void models as inconsistent with the game's low-poly craft. The replacement geometry uses flat normals, mitered ridges, beveled armor, polygonal gold wheels and inset eyes. The four newer mechanical bosses gain layered armor and independently moving surfaces. Every major boss has articulated machinery or its own existing/custom animation; the black hole remains entirely a shader effect, with a precessing accretion plane.

Heaven's wheels slow during a committed attack, preserving the selected firing eyes. Its iris follows the pilot. Void's horns, scythes, jaws and fracture arms articulate around a steady central target. Its emitters and attached lasers follow the moving anatomy. Teleports display both purple endpoints, dissolve the hull, then restore it before attacks/contact resume. The existing fifteen-second interludes, phase counts, health, gates and rewards remain intact.

The spell break now has a white ignition, broad pressure front, delayed rings, tapered rays and stronger shake. Cleared rounds become white light. It uses one shared mesh/material and one transparent pass, alongside the existing bounded particles.

Heaven scenery uses cut-stone bastions, broken golden palisades, faceted cloud islands and a seamless, script-drawn cloud relief texture. Projectile pixels and district palettes are unchanged.

## Pattern references and composition

The [ZUN interview](https://www.onemillionpower.com/talking-with-zun-from-the-touhou-series-a-conversation-hell/) connects character identity to the attacks and discusses curved and changing-speed bullets. The visual review also considered [Yukari's straight/curved pattern screenshot](https://www.takamagahara.info/2008/1112) and [UFO stage-six pattern screenshots](https://starlotusship8.hatenablog.com/entry/2020/12/05/232555).

The resulting scores use repeated motifs and shared timing: Heaven's eye-led vaults, reflected arches and laser aisles; Void's butterfly seams, folding pearl fans, opposed scythes, jaw crowns and four-port brocade. Each card runs on its own, with a rest and teleport between Void cards. Difficulty continues to thin the same geometry and extend rests.

## Verification

- The isolated Unity batch checked all major boss animations, Heaven's three phases, and all four Void forms and both cards per form. Eye colliders, teleport damage/target/contact gates, white phase breaks and fifteen active-second interludes passed.
- Heaven peaks were 552 / 820 / 1100 live rounds. Void peaks were 921 / 707 / 878 / 880. The last phase initially exhausted its ricochet reserve; a targeted recheck created **zero** rounds after reallocating reserves. Total pool remains 7400, with 3200 standard rounds and 800 giant orbs; live cap remains 3600.
- Full isolated Windows build succeeded with zero errors. The build report records one warning; no C# compiler warnings or shader errors were logged. This is not a full rendered-frame performance measurement.
- Exact Unity meshes were exported and rendered in background Blender for inspection. These images use review materials, not the gameplay shaders. Pattern diagrams plot actual simulated bullet coordinates and omit separate beam ribbons. Neither artifact replaces Miguel's visual/play approval.
- No desktop controls, visible Unity window or live editor session was used. A no-op render pipeline was confined to the headless simulation; production Graphics/Quality settings were restored in the review checkout before building.

The initial batch found MaterialPropertyBlock creation in field initializers; these allocations now happen in Start/Build/Create. Successful cases were retained while affected mechanisms, Void, and then only the final pool case were rechecked. `ArtSource/BossPass/Runtime.txt`, retained progress reports and `Build.txt` record these results.

## Reproduction

`Tools/refit_boss_models.py` runs in background Blender and bakes Hull81–84 from their preserved original Terra sources. `Tools/draw_heaven_clouds.py` regenerates the relief PNG. `VerifyBossPass.Run` requires batch mode and the isolated `Build/ReviewCache/Temp/DecisionHoverReview` project; `RunFixes`, `RunVoid`, and `RunPool` target outstanding cases. `VerifyBossPass.Build` builds only after the runtime report passes and exports the final faceted meshes. `Tools/render_boss_pass.py -- --final-only` renders only the final exported geometry. `Tools/review_boss_patterns.py` assembles local review sheets.

`Tools/prepare_boss_review.ps1` keeps unrelated in-progress changes out of the isolated review copy; it never reverts them in the main workspace.
