# Boss warning fix

The seven generated ribbon transforms are now retained for animation. Each
complete warning enters from a side, aligns in the middle of the 405:720
playfield, and exits before the boss appears. The clip itself is explicitly
anchored at the center and the rows are vertically balanced.

`Verification.txt` records an isolated Unity editor check: triggering before
Start, retriggering, moving all seven rows, centering the clip and words, and
clearing the warning. `Warning.png` is a native camera-canvas render of the
actual UI during its centered hold. No Play mode or live game was interrupted.
The existing readability probe now also checks the clip and the seven words.
