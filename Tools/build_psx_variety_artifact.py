"""Build the PSX variety round of the Sound Picks artifact (db collection `picks4`).

  & $taskSfxPython Tools/build_psx_variety_artifact.py

Reads ArtSource/PSXVarietyRound/generation.json (Tools/psx_variety_round.py) and writes
ArtSource/AudioReview/PSXVariety/: index.html, cues.json, options/ (each kept take, level-matched
to the sound in game, for apply_psx_variety_picks.py) and audio/<cue>.wav, one file per cue holding
its options and the sound in game back to back, so the page stays inside the artifact's file cap.
Cue labels, groups and rapid-fire intervals come from the previous round's page. The "Game"
reference is the cue's current SfxMix override when there is one, else the previous reference.
"""
import json
from pathlib import Path
import re
import shutil

import numpy as np
import soundfile as sf

ROOT = Path(__file__).resolve().parents[1]
GEN = ROOT / 'ArtSource/PSXVarietyRound/generation.json'
PREVIOUS = ROOT / 'ArtSource/AudioReview/PSXLoRA'
OUT = ROOT / 'ArtSource/AudioReview/PSXVariety'
MIX = ROOT / 'Assets/Resources/SfxMix.asset'
SR = 44100
GAP = .06
MOST = 8  # options shown per cue, best-ranked designs first


def load(path):
    x, sr = sf.read(str(path), dtype='float32', always_2d=True)
    x = x.mean(axis=1)
    if sr != SR:
        from math import gcd
        from scipy.signal import resample_poly
        g = gcd(sr, SR)
        x = resample_poly(x, SR // g, sr // g).astype(np.float32)
    return x


def rms_active(x):
    a = np.abs(x)
    keep = a > a.max() * 10 ** (-30 / 20)
    return float(np.sqrt(np.mean(x[keep] ** 2))) if keep.any() else 0.0


def loudest_window(x):
    """The page's level measure: RMS of the loudest 30 ms."""
    win = int(SR * .03)
    if len(x) <= win:
        return float(np.sqrt(np.mean(x ** 2))) or 1e-4
    c = np.convolve(x ** 2, np.ones(win) / win, 'valid')
    return float(np.sqrt(c.max())) or 1e-4


def overrides():
    """cue -> clip file for SfxMix entries that replace the clip."""
    guids = {}
    for meta in (ROOT / 'Assets/Resources/Sounds').rglob('*.wav.meta'):
        m = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8'), re.M)
        if m:
            guids[m.group(1)] = meta.with_suffix('')
    found = {}
    for cue, guid in re.findall(r'- cue: (\S+)\n    clip: \{fileID: 8300000, guid: (\w+)', MIX.read_text(encoding='utf-8')):
        if guid in guids:
            found[cue] = guids[guid]
    return found


def main():
    gen = json.loads(GEN.read_text(encoding='utf-8'))
    previous = json.loads((PREVIOUS / 'cues.json').read_text(encoding='utf-8'))
    mix = overrides()
    if OUT.exists():
        shutil.rmtree(OUT)
    (OUT / 'audio').mkdir(parents=True)
    (OUT / 'options').mkdir()
    cues = []
    for prev in previous:
        cid = prev['id']
        entry = gen['cues'].get(cid)
        if not entry or not entry.get('chosen'):
            print('no takes yet:', cid)
            continue
        ref_path = mix.get(cid) or (PREVIOUS / 'audio' / prev['current'] if prev['current'] else None)
        ref = load(ref_path) if ref_path else None
        target = rms_active(ref) if ref is not None else None
        parts, options, cursor = [], {}, 0.0

        def add(x):
            nonlocal cursor
            seg = dict(start=round(cursor, 5), dur=round(len(x) / SR, 5), level=round(loudest_window(x), 6))
            parts.extend([x, np.zeros(int(SR * GAP), np.float32)])
            cursor += (len(x) + int(SR * GAP)) / SR
            return seg

        for n, c in enumerate(entry['chosen'][:MOST], 1):
            x = load(c['wav'])
            if target:
                x = x * (target / max(rms_active(x), 1e-6))
            x = x * min(1.0, .89 / (np.abs(x).max() + 1e-9))
            sf.write(str(OUT / 'options' / f'{cid}_{n}.wav'), x, SR, subtype='PCM_16')
            text = entry['variants'][str(c['variant'])]['text'].split(': ', 1)[-1].split(', low-bitrate')[0]
            options[str(n)] = dict(add(x), strength=c['strength'], text=text)
        current = add(ref) if ref is not None else None
        sf.write(str(OUT / 'audio' / f'{cid}.wav'), np.concatenate(parts), SR, subtype='PCM_16')
        cues.append(dict(id=cid, label=prev['label'], group=prev['group'], repeat=prev['repeat'], file=f'{cid}.wav',
                         current=current, options=options))
    (OUT / 'cues.json').write_text(json.dumps(cues, indent=1), encoding='utf-8')
    html = (Path(__file__).resolve().parent / 'psx_variety_template.html').read_text(encoding='utf-8')
    (OUT / 'index.html').write_text(html.replace('/*CUES*/[]', json.dumps(cues, separators=(',', ':'))), encoding='utf-8')
    size = sum(f.stat().st_size for f in (OUT / 'audio').iterdir()) / 1e6
    print(f'{len(cues)} cues, {sum(len(c["options"]) for c in cues)} options, {size:.1f} MB of audio -> {OUT}')


if __name__ == '__main__':
    main()
