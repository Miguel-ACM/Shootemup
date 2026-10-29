"""Second picking round: four PSX-processed takes per cue, built from the styles Miguel liked.

Run: python Tools/synth_sfx_psx.py                 render the four takes of every cue for the picker
     python Tools/synth_sfx_psx.py --apply picks.json
                                                  render four game variants of each chosen take

Every take goes through psx_spu: SPU-RAM sample rate, 4-bit ADPCM and an SDK reverb preset.
  1  the round-one pick (or the original clip Miguel kept), stored at 22 kHz, Room
  2  the physical design (layered when physical was already the pick), 22 kHz, Studio Small
  3  layered and physical blended into one patch, stored at 11 kHz, Room
  4  the round-one pick again, re-pitched like a PS1 sample (lower for weight, higher for blips),
     Studio Medium
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np

import psx_spu as P
import synth_sfx as S
import synth_sfx_options as O

ROOT = S.ROOT
OUT = ROOT / 'ArtSource/AudioReview/SynthPSX'
ROUND_ONE = ROOT / 'ArtSource/AudioReview/SynthOptions/picks.json'
SMALL = {'enemyHit', 'bossHit', 'coin', 'mana', 'itemGet', 'uiConfirm', 'uiSkip', 'ready', 'popcorn', 'shot1'}


def raw(cue, style, v):
    S.rng = np.random.default_rng(int.from_bytes(hashlib.sha256(f'{cue}:{style}:{v}:psx-v1'.encode()).digest()[:8], 'little'))
    x = O.design(cue, style)(v)
    return x / max(np.max(np.abs(x)), 1e-9)


def original(cue):
    source = S.CUES[cue][1]
    return S.read_wav(ROOT / 'Assets/Resources' / (source + '.wav')) if isinstance(source, str) else None


def base(cue, pick, v):
    if pick == 'current' and original(cue) is not None:
        x = original(cue)
        return x / np.max(np.abs(x))
    return raw(cue, pick if pick in O.STYLES else 'layered', v)


def take(cue, number, pick, v):
    small = cue in SMALL
    x = chain(cue, number, pick, v, small)
    if small:
        # Repeating blips keep a short room: cap the tail so rapid fire doesn't smear.
        keep = np.nonzero(np.abs(x) > np.max(np.abs(x)) * .1)[0][-1] + S.n_of(.12)
        x = x[:keep].copy()
        fade = min(len(x), S.n_of(.06))
        x[-fade:] *= np.linspace(1, 0, fade) ** 2
    x = S.finish(x, psx=False)
    return x * min(O.level_target(cue) / S.loudness(x), .98 / np.max(np.abs(x)))


def chain(cue, number, pick, v, small):
    wet = .45 if small else 1.0
    if number == 1:
        x = P.reverb(P.store_and_play(base(cue, pick, v), 22050), 'room', .22 * wet)
    elif number == 2:
        style = 'layered' if pick == 'physical' else 'physical'
        x = P.reverb(P.store_and_play(raw(cue, style, v), 22050), 'studioSmall', .28 * wet)
    elif number == 3:
        a, b = raw(cue, 'layered', v), raw(cue, 'physical', v)
        n = max(len(a), len(b))
        x = np.pad(a, (0, n - len(a))) * .6 + np.pad(b, (0, n - len(b))) * .6
        x = P.reverb(P.store_and_play(x, 11025), 'room', .2 * wet)
    else:
        x = P.store_and_play(base(cue, pick, (v + 2) % 4), 22050, 1.12 if small else .8)
        x = P.reverb(x, 'studioMedium', .3 * wet)
    return x


def round_one():
    picks = json.loads(ROUND_ONE.read_text(encoding='utf-8'))
    return {cue: picks.get(cue, 'layered') for cue in S.CUES}


def render(only=None):
    audio = OUT / 'audio'
    audio.mkdir(parents=True, exist_ok=True)
    picks = round_one()
    rows = []
    previous = json.loads((OUT / 'cues.json').read_text(encoding='utf-8')) if only else []
    for cue, (_, source, label, group) in S.CUES.items():
        if only and cue not in only:
            rows.append(next(r for r in previous if r['id'] == cue))
            continue
        files = []
        for number in range(1, 5):
            name = f'{cue}_{number}.wav'
            S.write_wav(audio / name, take(cue, number, picks[cue], 0))
            files.append(name)
        game = f'game_{cue}.wav'
        live = S.OUT / f'{cue}_01.wav'
        S.write_wav(audio / game, S.read_wav(live) if live.exists() else original(cue))
        rows.append(dict(id=cue, label=label, group=group, game=game, takes=files, repeat=S.REPEAT.get(cue)))
        print(cue)
    (OUT / 'cues.json').write_text(json.dumps(rows, indent=1), encoding='utf-8')


def apply(choices):
    """choices: {cue: 1-4 | 'game'}. A number replaces that cue's game variants; 'game' keeps them."""
    picks = round_one()
    for cue, number in choices.items():
        if number == 'game':
            continue
        for old in S.OUT.glob(f'{cue}_[0-9][0-9].wav*'):
            old.unlink()
        for v in range(S.VARIANTS):
            path = S.OUT / f'{cue}_{v + 1:02}.wav'
            S.write_wav(path, take(cue, int(number), picks[cue], v))
            S.meta(path)
        print(cue, number)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--apply', help='JSON file of {cue: take number or "game"}')
    parser.add_argument('--only', default='', help='comma-separated cues to rerender for the picker')
    args = parser.parse_args()
    if args.apply:
        apply(json.loads(Path(args.apply).read_text(encoding='utf-8')))
    else:
        render(set(filter(None, args.only.split(','))))
