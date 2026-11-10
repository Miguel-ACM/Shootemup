"""Build the PSX LoRA round of the Sound Picks artifact (db collection `picks3`).

Run with the local-sounds venv after `psx_sfx_round.py --start` and `--publish`:
  & $taskSfxPython Tools/build_psx_picks_artifact.py

Writes ArtSource/AudioReview/PSXLoRA/ (index.html, audio/, cues.json). The synth-bank cues
(shot1, enemyHit, coin, ...) come first with the sound currently in game as reference; the
other Sound Lab cues follow with Sound Lab's "current sound". Takes already published to
Sound Lab are reused as they are; the synth-bank-only cues are picked with the round's own
rules (similarity limit, both strengths, no near-twins) and level-matched to the in-game sound.
Nothing is imported into Unity.
"""
import json
from pathlib import Path
import shutil
import sys

import numpy as np
import soundfile as sf

sys.path.insert(0, str(Path(__file__).resolve().parent))
from psx_sfx_round import PUBLIC, REVIEW, load_cli, logmel, mono, pick, rms_active  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'ArtSource/AudioReview/PSXLoRA'
GAME = ROOT / 'ArtSource/AudioReview/SynthPSX/audio'
SOURCE = 'local-sa3-psx-lora-v1'

# The synth-bank cues of the earlier Sound Picks rounds: id, label, group, rapid-fire interval.
BANK = [('shot1', 'Base gun', 'Shots', .075), ('enemyHit', 'Hit on enemy', 'Hits', .06),
        ('bossHit', 'Hit on boss', 'Hits', .08), ('hurt1', 'Player hurt', 'Hits', None),
        ('enemyExplosion1', 'Enemy explosion', 'Explosions', .22),
        ('explosionLarge', 'Boss phase change', 'Explosions', None), ('playerDead', 'Player death', 'Explosions', None),
        ('bossDeath', 'Boss death', 'Bosses', None), ('bossWarning', 'Boss warning', 'Bosses', None),
        ('coin', 'Coin', 'Pickups', .09), ('mana', 'Energy', 'Pickups', .15), ('itemGet', 'Item chosen', 'Pickups', None),
        ('uiConfirm', 'Purchase / confirm', 'UI', None), ('uiSkip', 'Skip', 'UI', None),
        ('blankbullet', 'Screen clear / shield', 'Items', None), ('heal', 'Heal', 'Items', None),
        ('timeStop', 'Time stop', 'Items', None), ('powerdown', 'Power down', 'Items', None),
        ('overdrive', 'Overdrive', 'Items', None), ('roll', 'Roll / dash', 'Items', None), ('ready', 'Ready', 'Items', None),
        ('shatter', 'Shatter', 'Items', None), ('capacitor', 'Capacitor', 'Items', None), ('fan', 'Fan', 'Items', None),
        ('frost', 'Frost', 'Items', None), ('decoy', 'Decoy', 'Items', None), ('popcorn', 'Popcorn', 'Items', .12),
        ('static', 'Static', 'Items', None)]
# Rapid-fire intervals for Sound Lab's repeating cues.
REPEAT = {'hit1': .06, 'hitElectric': .08, 'ricochetHit': .1, 'shellBurst': .25}


def write(path, x, sr):
    path.parent.mkdir(parents=True, exist_ok=True)
    sf.write(str(path), x, sr, subtype='PCM_16')


def main():
    plan = json.loads((REVIEW / 'cues.json').read_text(encoding='utf-8'))
    summary = json.loads((REVIEW / 'generation.json').read_text(encoding='utf-8'))
    manifest = json.loads((PUBLIC / 'manifest.json').read_text(encoding='utf-8'))
    lab = {c['id']: c for c in manifest['cues']}
    if OUT.exists():
        shutil.rmtree(OUT)
    (OUT / 'audio').mkdir(parents=True)
    cli = None
    cues = []

    def lab_takes(cid):
        return [ch for ch in lab[cid]['choices'] if ch.get('source') == SOURCE]

    def entry(cid, label, group, repeat, current, takes):
        cues.append(dict(id=cid, label=label, group=group, repeat=repeat, current=current,
                         options={str(i + 1): t for i, t in enumerate(takes)}))

    for cid, label, group, repeat in BANK:
        current = f'game_{cid}.wav'
        shutil.copy2(GAME / current, OUT / 'audio' / current)
        ref = rms_active(mono(GAME / current)[0])
        takes = []
        if cid in lab and lab_takes(cid):
            sources = [(PUBLIC / ch['file'], ch['loraStrength'], ch['maxTrainingSimilarity']) for ch in lab_takes(cid)]
        else:
            result = summary['cues'].get(cid)
            if not result:
                print('no takes yet:', cid)
                entry(cid, label, group, repeat, current, [])
                continue
            cli = cli or load_cli()
            paths = {}

            def path_of(t):
                if t['ref'] not in paths:
                    rec = cli.call('/api/requests/' + t['request'])
                    paths[t['ref']] = next(tk for tk in rec['takes'] if tk['ref'] == t['ref'])['paths']['wav']
                return paths[t['ref']]

            cache = {}
            lm_of = lambda t: cache.setdefault(t['ref'], logmel(*mono(path_of(t))))
            sources = [(Path(path_of(t)), t['strength'], t['maxTrainingSimilarity']) for t in pick(plan, result['takes'], lm_of)]
        for i, (src, strength, sim) in enumerate(sources):
            x, sr = sf.read(str(src), dtype='float32', always_2d=True)
            x = x.mean(axis=1)
            x = x * (ref / max(rms_active(x), 1e-6))
            x = x * min(1.0, .89 / (np.abs(x).max() + 1e-9))
            name = f'{cid}_psx{i + 1}.wav'
            write(OUT / 'audio' / name, x, sr)
            takes.append(dict(file=name, strength=strength, similarity=round(sim, 2)))
        entry(cid, label, group, repeat, current, takes)

    bank = {b[0] for b in BANK}
    for c in manifest['cues']:
        if c['id'] in bank:
            continue
        current = None  # cues that are silent in game have no reference
        if c.get('original'):
            current = f"lab_{c['id']}.wav"
            shutil.copy2(PUBLIC / c['original']['file'], OUT / 'audio' / current)
        takes = []
        for i, ch in enumerate(lab_takes(c['id'])):
            name = f"{c['id']}_psx{i + 1}.wav"
            x, sr = sf.read(str(PUBLIC / ch['file']), dtype='float32', always_2d=True)
            write(OUT / 'audio' / name, x.mean(axis=1), sr)
            takes.append(dict(file=name, strength=ch['loraStrength'], similarity=round(ch['maxTrainingSimilarity'], 2)))
        group = c['group'].split(' —')[0]
        entry(c['id'], c['label'].replace(' — ', ' - '), group, REPEAT.get(c['id']), current, takes)

    (OUT / 'cues.json').write_text(json.dumps(cues, indent=1), encoding='utf-8')
    html = (Path(__file__).resolve().parent / 'psx_picks_template.html').read_text(encoding='utf-8')
    (OUT / 'index.html').write_text(html.replace('/*CUES*/[]', json.dumps(cues, separators=(',', ':'))),
                                    encoding='utf-8')
    files = sum(1 for _ in (OUT / 'audio').iterdir())
    print(f'{len(cues)} cues, {sum(len(c["options"]) for c in cues)} takes, {files} audio files -> {OUT}')


if __name__ == '__main__':
    main()
