# Plains riverbank review

Real Unity 6000.6.2f1 gameplay renders from the isolated review project, with the normal PSX filter and HUD. The active game editor was left alone.

The river now has periodic erosion scallops shared by the water and terrain meshes, sculpted grassy lips, sloped gravel shelves, dark wet margins, embedded pebbles and uneven clumps of modeled reeds. Fine gravel grain stays attached to the ground. These details share the existing section meshes and material; there are no added scenery objects, colliders or per-frame shore updates.

`RiverbankComparison.jpg` compares the same five terrain stations before and after. `RiverbankMotion.mp4` shows the new banks moving through the full 96-unit landscape wrap. Full-size individual captures preserve the original gameplay aspect ratio.

`Verification.txt` records shader compilation, bounded finite meshes, water/shore seams across four sections and three seeds, and scrolling loop checks. The road path and tank surface sampling are unchanged.

Reproduce in a separate Unity project using `VerifyRiverbanks.Baseline` before the change, then `VerifyRiverbanks.Run` with the new scenery, and package with `Tools/render_riverbank_review.py`.
