# Shootemup Sound Lab

Run `./SoundLab/Start-SoundLab.ps1` in PowerShell from the game project, then open
the printed localhost address. Alternatively run `python SoundLab/server.py`.
The server binds only to `127.0.0.1`; there are no accounts or network services.

The page has 36 effects with five sample-based alternatives each. Current sounds
are included as a level-matched reference. Cues without existing audio explicitly
show **Currently silent**; **Keep current** preserves that silence.

Play once, compare A through E, or repeat at 1–20 shots per second for 3, 8, 20
or 60 seconds. Escape stops everything. Keys 1–5 audition A–E; arrow keys navigate.
Playback stops when the page is hidden. The combat preview uses your picks and
falls back to current behavior for unpicked or rejected effects. Rapid-repeat
playback reserves headroom for overlapping tails; it is an audition aid, not an
exact recording of Unity gameplay.

Selections, effect level and notes automatically save to `SoundLab/selections.json`.
The previous save is retained in `selections.previous.json`. Browser storage is
an additional fallback, and **Export picks** downloads a JSON backup.
**Finish selection** marks the project file `status: ready`. Tell Codex
“my sound picks are ready”; the next pass can read this file and apply only the
approved choices. `reject` requests more alternatives. `pending` retains notes
without choosing an effect. Unpicked effects keep current behavior.

The server does not modify Unity assets or scripts. There is no automatic game
import while you audition. The rejected procedural bank is not used here.

To rebuild the candidates, run `python SoundLab/build_candidates.py` with ffmpeg
on PATH and the local Frontlines source project available. Recipes, output
hashes and preview levels are recorded in `public/manifest.json`. Source cut
locations/hashes are in `source-provenance.json`; the original license record is
preserved in `SOURCE-LICENSES.txt`. This round uses the user's existing game
sounds and generated Frontlines material. No new synthesis service is called.

To stop the local app, stop the Python process whose ID is in `server.pid`, after
confirming its command line is this folder's `server.py`.
