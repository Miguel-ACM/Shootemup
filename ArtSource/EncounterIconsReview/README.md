# Encounter sprites and choice exit

Five original 32 x 32 transparent pixel sprites drawn directly with Python/Pillow: gold coin shop, blue multiplier shop, green item crate, medkit and challenge crest. There are no encounter text labels in the game UI. Sprites use point sampling, no mipmaps and uncompressed textures, displayed at 96 UI units (64 screen pixels at the existing 480px gameplay height).

The chosen icon gives a small confirmation pop and rises as the panels fade. Other panels fade sooner and slide outward. The selected encounter opens only after the 0.45-second exit completes. The same exit works for district path selection while retaining district names. Pause stops the animation; UI textures are cached and there is no added combat update loop.

`IconSheet.png` shows the source art enlarged with nearest-neighbour sampling. `ChoicePreview.jpg` and `ChoiceExitPreview.mp4` were rendered by the actual Unity UI component in edit mode, over an existing clean gameplay capture. These are editor UI previews, not a new gameplay recording. No Play mode or gameplay test run was started for this change.

`Verification.txt` covers icon imports, text replacement, native UI animation poses and the fully hidden endpoint. Draw with `Tools/draw_encounter_icons.py`, render with `ReviewEncounterIcons.Run` in an isolated Unity project, and package with `Tools/render_encounter_icons_review.py`.
