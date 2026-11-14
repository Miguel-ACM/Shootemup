"""Apply Miguel's PSX variety picks (Sound Picks artifact, db `picks4`) to the game.

    python Tools/apply_psx_variety_picks.py [--dry-run]

Reads ArtSource/AudioReview/PSXVariety/picks.json ({cue: {choice, pitch}}, exported from the page).
Each pick is resampled by its pitch (semitones, the page's playback-rate change: pitch and length
move together) and written to Assets/Resources/Sounds/Selected/<cue>_psx.wav as that cue's SfxMix
override, keeping the entry's gain, mute and order. A "current" pick at pitch 0 changes nothing;
at another pitch, the sound in game is re-pitched the same way.
"""
import argparse
import json
from math import gcd
from pathlib import Path
import re

import numpy as np
import soundfile as sf

from apply_psx_picks import ASSET, AUDIO_FILE_ID, SELECTED, guid_of
from build_psx_variety_artifact import OUT as REVIEW, PREVIOUS, SR, load, overrides


def repitch(x, semitones):
    if not semitones:
        return x
    from scipy.signal import resample_poly
    ratio = 2 ** (semitones / 12)  # playback rate
    up, down = 1000, round(1000 * ratio)
    g = gcd(up, down)
    return resample_poly(x, up // g, down // g).astype(np.float32)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--dry-run', action='store_true')
    args = parser.parse_args()
    picks = json.loads((REVIEW / 'picks.json').read_text(encoding='utf-8'))
    previous = {c['id']: c for c in json.loads((PREVIOUS / 'cues.json').read_text(encoding='utf-8'))}
    mix = overrides()
    text = ASSET.read_text(encoding='utf-8')
    newline = '\r\n' if '\r\n' in text else '\n'
    text = text.replace('\r\n', '\n')
    head, entries = text.split('  entries:\n', 1)
    blocks = re.findall(r'  - cue: .*?(?=\n  - cue: |\Z)', entries.rstrip('\n'), re.S)
    by_cue = {re.match(r'  - cue: (\S*)', b).group(1): i for i, b in enumerate(blocks)}
    applied = []
    for cue, pick in sorted(picks.items()):
        choice, st = pick['choice'], int(pick.get('pitch') or 0)
        if choice == 'current':
            if not st:
                continue
            source = mix.get(cue) or (PREVIOUS / 'audio' / previous[cue]['current'])
        else:
            source = REVIEW / 'options' / f'{cue}_{choice}.wav'
        x = repitch(load(source), st)
        x = x * min(1.0, .89 / (np.abs(x).max() + 1e-9))
        target = SELECTED / f'{cue}_psx.wav'
        tag = f'psxv{choice}' + (f'{st:+d}st' if st else '')
        applied.append(f'{cue} <- {source.name} {st:+d} st')
        if args.dry_run:
            continue
        SELECTED.mkdir(parents=True, exist_ok=True)
        if target.resolve() == Path(source).resolve():
            x = x.copy()
        sf.write(str(target), x, SR, subtype='PCM_16')
        clip = f'{{fileID: {AUDIO_FILE_ID}, guid: {guid_of(target)}, type: 3}}'
        if cue in by_cue:
            b = blocks[by_cue[cue]]
            b = re.sub(r'(\n    clip: ).*', lambda m: m.group(1) + clip, b, count=1)
            b = re.sub(r'(\n    choice:).*', lambda m: m.group(1) + ' ' + tag, b, count=1)
            blocks[by_cue[cue]] = b
        else:
            blocks.append(f'  - cue: {cue}\n    clip: {clip}\n    gain: 1\n    muted: 0\n    choice: {tag}')
            by_cue[cue] = len(blocks) - 1
    if not args.dry_run:
        ASSET.write_text((head + '  entries:\n' + '\n'.join(blocks) + '\n').replace('\n', newline),
                         encoding='utf-8', newline='')
    print('\n'.join(applied))
    print(len(applied), 'cues', 'would be' if args.dry_run else '', 'assigned')


if __name__ == '__main__':
    main()
