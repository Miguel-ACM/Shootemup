# Combat polish review — 23 October 2026

These are captures of the running Unity game with its actual PSX output filter. They are review artifacts, not replacement artwork. The videos preserve the recorded frame timestamps; the contact sheets sample representative moments.

## Clips

- `TitleHudStartup`: real Title menu, New Game, moving cloud curtain and fluorescent instrument startup.
- `HudStartup`: a close replay of the shared startup clock, including five irregular dropouts and the equipment portrait alpha.
- `MovingLaser`, `LancerSweep`, `BossArray`: attached emitters, harmless charge, continuous active energy, seven boss beams and cooling.
- `ScoredLanceFirstLoop`, `ScoredLanceFastLoop`: actual repeat attacks at initial and capped loop tempo, including release audio fade and the next firing accent.
- `ModelAttack*`: newly shaped artillery and reactor hulls with their actual attack origins.
- `PlayerRounds*`: the unchanged base gun's softer blue pulse on all five districts.
- `Weapon*`: separate needle, shell, returning disc, flame and guided missile silhouettes on bright Snow and dark Thunder.
- `Breakup*`: actual district hull fragments, excluding animated exhaust, with cleanup.

The review pilot has inflated health to expose full patterns. Its long health display in these captures is test configuration, not the normal starting health.

## Reproduction

With Unity in Edit mode and current scripts imported, atomically place `polish` in `Temp/verify-combat.request`. The editor watcher runs the visual checks, all forty district compositions, then real stock-gun collision measurements, and returns to Edit mode. Use `visuals-title` for the full visual/audio review followed by the real Title startup, or `title-hud` for that startup alone. A `snow-choreography` request reviews only the eight Snow phrases after a localized route refinement. Render these clips with `Tools/render_polish_review.py`; district wave footage uses `Tools/render_choreography_review.py`.

`Verification.txt` records runtime checks. Choreography and time-to-kill results live in the parent review directory. The model fleet and seeded City architecture have their own adjacent review folders.

## Sound evidence

Four cues use the existing Sound Lab / Frontlines sample-layer workflow: charge, ignition, sustain and release. Provenance and a repeated one-minute mix are in `../BeamAudio`. Runtime checks inspect actual AudioSource output, overlapping loops and cancellation. These are technical audio checks; no subjective listening judgment is implied by a PASS report.
