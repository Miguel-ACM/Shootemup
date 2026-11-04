# Heaven and Void polish

Open `index.html` for the native Unity model sheets and motion previews.

Every Heaven hull now has sculpted volume, a recognizable silhouette and pearl/gold materials. The angel has interwoven golden wheels and curved living eyes. Heaven's cloud banks carry raised ruins, broken arches, railings, banners and planted lances. The four Void bodies have separate crown, scythe, jaw and fracture-wing silhouettes with glossy obsidian contours.

Void uses thirteen approved projectile deliveries across eight attack scores. The complete batch checks upper-screen pressure on both attacks of every form, with the same authored geometry across difficulties. Normal and Hard retain four forms; Easy retains three. All disappearances last fifteen active seconds.

`Runtime-Batch.txt` retains the original batch result, including a ricochet-reserve failure. `Runtime-Hard.txt` is the passing targeted regression after reassigning unused reserve capacity. Global projectile limits were unchanged. `Runtime.txt` explains the combined result. `Final-Art.txt` covers the last eye correction. Timing measurements are from Wave Watcher and omit screenshot I/O during the measured window.

The initial review produced model renders and gameplay captures in an isolated checkout. After the updated desktop restriction was discovered, all further checks and the build used Unity command-line batch mode, with offscreen captures and no desktop controls. `VerifyHeavenVoid.RunPolishFixes` exercises the affected Hard fight; `RunPolishFinalArt` captures only the final visual correction. `BuildPolish` produces the Windows player after the checks pass.

`Tools/render_celestial_polish_review.py` assembles the captured frames. Raw frames stay local under ignored `Frames/`; the model renders, contact sheets, GIFs and reports are retained here. These are technical and visual review artifacts, not a claim of Miguel's artistic approval. Approved projectile artwork and laser sounds are unchanged.
