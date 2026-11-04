# Heaven and Void review

Native Unity captures from the isolated `Build/ReviewCache/Temp/DecisionHoverReview` checkout. The live Unity project was not launched or switched for this review.

- [Heaven motion preview](Heaven.gif): revised white cloud banks, golden fences, authored formations and widening siege lasers.
- [Void motion preview](Void.gif): four forms and three different disappearing-boss barrages.
- [Model gallery](Models.jpg), [gameplay contact sheet](Gameplay.jpg), [motion storyboard](Motion.jpg). Individual front and angled model renders are in `Models/`.
- [Build result](Build.txt): Windows executable at `Build/HeavenVoid/Shootemup.exe`, zero errors and warnings.

The GIFs sample gameplay at five frames per second; they are visual review artifacts, not frame-rate measurements or player approval. Full 1280-by-720 source frames remain locally in the ignored `Frames/` folder. `Tools/render_heaven_void_review.py` recreates the sheets and GIFs.

## Validation

`Runtime-Initial.txt` preserves the first batch, including the failed Heaven seed. All Void checks passed in that batch: three Easy / four Normal and Hard phases, damage clamping at phase boundaries, damage and homing rejection while hidden, fifteen-second absences, difficulty-independent interlude timing, the existing projectile budget and final destruction. No projectile objects were created during any of the three fights. Sampled live counts were 241 Normal, 623 Hard and 196 Easy; these are observations, not theoretical maxima.

`Runtime.txt` records the targeted repair check: all 399 Heaven compositions pass across seven seeds, budgets 2–20 and three difficulties. The cloud meshes were also revised after visual inspection found they looked like stone. Only the revised Heaven captures are included in the current gameplay sheet.

`Ports.txt` records the final targeted check that every Void form's charge and release flashes follow its changing anatomical ports, reusing four prepared cues. The eyes, blade tips, jaw lips and final star points use the same positions for anticipation and projectile release.

Hard Void's full main-thread frames, measured without screenshot I/O, had a median of 4.88 ms, p99 of 8.66 ms and maximum of 12.09 ms. These CPU observations include the frame's physics work; they do not establish GPU performance on other machines.

The review entry points are `VerifyHeavenVoid.Run`, `RunFixes`, `RunPorts` and `Build`. Review-only player settings isolate these runs from Miguel's saved player state.

## Sound candidates

[Charge audition](charge-audition.html) and [ignition audition](ignite-audition.html) retain the original and best generated takes, repeat-fire previews and provenance. Both bounded iterations exhausted their generation budgets; model critique is not listening approval. `AudioProvenance.json` records that these candidates are unapproved and not imported.

Automatic approval review initially blocked importing the generated candidates without Miguel's listening approval. Miguel then explicitly chose to keep the approved laser sounds for now. Runtime uses those existing samples with a deeper siege hum; the generated takes remain review-only. Sound Lab's published bank and saved selections are unchanged. The local sound-generator server was left available for auditioning. No Heaven or Void music track was supplied; these districts stop the previous district's song.
