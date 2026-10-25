# PSX-style combat sound bank

This Shootemup bank adapts the Frontlines `BuildSfx.py` layered rendering,
faded boundaries, controlled levels and decoded-output verification workflow.
The sounds here are original synthesis; no Frontlines recordings or downloads
are needed. Frontlines itself is not modified.

Run `python Tools/build_psx_sfx.py` from the repository to rebuild the 36 cues,
four variations per cue, generated C# catalog, listening bench and reports.
`--verify-only` checks the existing WAVs against deterministic synthesis and
rebuilds the listening reels. Python's standard library is sufficient.

Open `index.html` in a browser for single sounds, rapid repeated playback,
normal/dense battle mixes and a full-minute endurance reel cycling through
every player weapon. Enable **Loop previews** for longer listening sessions.
Single previews are at asset gain; repetition and battle reels use game gains.
The mixes are synthesized review arrangements, not recordings of gameplay.

## Sound design and integration

- Player: pulse cannon, electricity, AK45, companion, needle, rail, scatter,
  burst shell, returning disc, missile, flame projector, beam charge/fire.
- Enemy: simple, needle, plasma, missile, burst, disc, ricochet, helix,
  suspended, scatter, mine, expanding wave and beam charge/fire.
- Feedback: hull/electric hits, ricochet, ordinary/heavy explosions, shell
  detonation, player damage/death, blank and boss warning.
- Four seeded waveform variations; no immediate repeat of the same variation.
  Playback randomness is independent of Unity's gameplay random state.
- Mono 22,050 Hz / 16-bit PCM, rounded attacks, short room reflections, filtered
  noise, FM/inharmonic bodies, and zero-valued boundaries. This evokes sampled
  console SFX; it does not emulate the PS1 SPU or its ADPCM codec.
- PCM import preserves the authored peak (approximately -4.15 dBFS) and gain.
  Normalization is disabled; sounds preload and do not stream or decode in combat.
- 24 independent voices: player 4, enemy 6, hit 4, explosion 6, important cues 4.
  Cue gates coalesce fan pellets. Active sources are never retuned or stolen.
  Combat reserves gain headroom for damage/death and other important feedback.
- Explosions emit one sound per explosion event, rather than one per visual
  fireball. Heavy hulls use a longer, lower blast.
- Enemy beam charge sounds during warning; firing sounds when damage activates.
  Player beam firing also sounds only on activation. No bullet-travel loops.

## Verification

`verification.json` contains decoded PCM measurements and hashes for all 144
clips, 36 rapid-repeat reels and three mixed arrangements. It checks format,
duration, silence/clipping, DC bias, distinct variants and reproducibility.
The reels are checked before any limiting; WAV writing never normalizes them.

In Unity, use **Tools > Audio > Verify PSX SFX** outside Play mode.
`unity-verification.txt` records imported formats/levels, real cue routing,
same-frame suppression, independent active pitches, variation, gain/voice
budgets, invalid-input handling and preservation of gameplay randomness.
This tool checks source scheduling in Edit mode without changing scenes/saves.

These checks do not establish subjective sound quality or absence of listening
fatigue. The current agent environment cannot hear the audio; the playable
reels provide the remaining human listening check on speakers/headphones.
