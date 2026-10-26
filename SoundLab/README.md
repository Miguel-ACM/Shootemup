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

## Local generation and automatic iteration

The installed generator is **`C:\Users\Miguel\Tools\local-sounds`**, a separate
local repository. It runs **Stable Audio 3 Small-SFX** from downloaded weights.
Frontlines' local generated takes are in `C:\Users\Miguel\Tools\frontlines-sfx`.
The hosted-service provenance in this game's existing source manifests describes
older samples; it is not evidence that the current generator runs in the cloud.
Unity plays exported audio files and does not run a generation model.

The automatic iteration feature is implemented in the tool's `scripts/iterate.py`
(tool commit `66556f8`). From the Shootemup repository, for example:

```powershell
$sfxTool = Join-Path $env:USERPROFILE 'Tools/local-sounds'
& "$sfxTool/.venv/Scripts/python.exe" "$sfxTool/scripts/iterate.py" `
  "one compact energy shot, dry, short tail" --duration 0.3 `
  --require "Is there exactly one shot?" --require "Is the tail short?" `
  --require "Is the audio free of music and speech?" `
  --out ArtSource/AudioReview/Iterations --baseline
```

Add `--dry-run` to inspect the brief and budget without starting a server or
loading models. `--no-start` requires an already running generator. It uses the
tool's existing local server at `127.0.0.1:8191`, separate from Sound Lab. If that
server predates the feature, restart it through its normal launcher before use.
Do not interrupt Miguel's live game or stop other programs to free GPU memory.

- Keep a fixed description and concrete requirements throughout the run. A JSON
  `--brief` can supply `description` and `requirements` instead of text/`--require`.
  Iteration prompts explicitly include `TrackType: SFX`; the ordinary generator's
  prompt path is unchanged.
- The local **MiDashengLM 0.6B** critic first describes the audio without seeing
  the target, then checks each requirement and proposes regeneration, shortening,
  softening or an audio-to-audio variation. It runs on **CPU**, while the generator,
  CLAP and Audiobox checkers use the GPU. Its weights are installed on this machine.
- The default is four initial takes and at most two repair rounds; `--rounds`
  allows 0–3 and `--n` allows 1–8. Repairs create new requests. Originals and the
  best earlier take remain available. Compare candidates against the fixed brief,
  never the evolving generation prompt. Stop when the critic supports every
  requirement or the round budget is exhausted.
- Each new output folder contains `session.json`, WAVs, sidecars, spectrograms
  and five-second repeat-fire previews (`--repeat-hz`, default 10). `--mix` adds a
  preview over a supplied local combat/music file. The session records seeds,
  request references, critiques, actions and failures; it is not a resume file.
- `--baseline` shares the initial batch and gives the original prompt the same
  actual generation count as iteration. It exports both winners and a level-matched
  `compare.html` with opaque A/B labels. Keep `comparison-key.json` hidden until
  Miguel chooses. DSP repairs and critique add work beyond this generation budget.

The small critic gave an incorrect caption and unsupported affirmative answers
in a synthetic-tone check. Bare YES/NO responses without supporting detail count
as **uncertain**; an exhausted budget is not approval. `human_approved` remains
false. Judge real improvements by listening to isolated, repeated and mixed cues.
Preserve `SoundLab/selections.json`, its backup and approved sounds. Generated
files are review candidates: this command does not publish them to Sound Lab,
replace Unity assets or change runtime sound routing.

Implementation details and API documentation are in the tool's own `README.md`
and `SKILL.md`. If the optional critic is absent on another machine, its pinned
download is `.venv/Scripts/python.exe scripts/download_models.py
--accept-licenses --only captioner` from the tool directory (Apache-2.0, ~2.8 GB).

## Item effects round

The original bank and saved choices stay intact. New item cues use fixed briefs in
`ArtSource/ItemEffectsAudioReview/cues.json`. Each cue has two initial takes and one
bounded repair round (at most four generations); the local CPU critic is advisory.
The source requests, earlier best take, repeated-fire previews and a preview over
five seconds of Miguel's Plains music are retained in the review folder.

From the game repository, using the local generator's installed interpreter:

```powershell
$taskSfxPython = "$env:USERPROFILE/Tools/local-sounds/.venv/Scripts/python.exe"
& $taskSfxPython Tools/iterate_item_sfx.py --dry-run
& $taskSfxPython Tools/iterate_item_sfx.py --start
& $taskSfxPython Tools/iterate_item_sfx.py --publish
```

The normal GPU guard remains active. `--start-anyway` is only for explicit permission
to run alongside other GPU programs; never stop Miguel's game to free memory.
`--only itemChargeBlast` targets one cue; `--retry-failed` explicitly retries failed
requests. Existing published choice letters are immutable, so saved picks cannot
quietly change meaning. Publishing adds audition copies; it does not import Unity
sounds. The continuous flame cue's repeat button plays one loop, rather than
stacking many copies.

After Miguel finishes the selection, inspect the exact approved import plan:

```powershell
& $taskSfxPython SoundLab/apply_item_picks.py
& $taskSfxPython SoundLab/apply_item_picks.py --apply
```

Only explicitly chosen local item takes are copied to `Sounds/ItemApproved`; their
saved levels are respected. Pending, rejected and Keep current choices stay as they
are. Hashes are verified before writing, previous imported takes are backed up and
existing Unity GUIDs are preserved. This importer never changes the selection files,
original item names, original bank takes or older Unity audio. Absent approvals,
new runtime cue hooks retain their previous sound or silence.
