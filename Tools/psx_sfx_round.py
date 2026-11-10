"""PSX LoRA candidate round for the Sound Lab cues; publishes audition copies only.

Use the local-sounds venv (same flow as Tools/iterate_item_sfx.py):
  & $taskSfxPython Tools/psx_sfx_round.py --dry-run
  & $taskSfxPython Tools/psx_sfx_round.py --start      # generate (normal GPU guard)
  & $taskSfxPython Tools/psx_sfx_round.py --publish    # append takes to Sound Lab

Takes come from the generator's opt-in `psx` engine (Small-SFX + the local PSX LoRA) at the
strengths in ArtSource/PSXCandidateRound/cues.json. A take is never published when its log-mel
similarity to any LoRA training clip is >= the plan's limit, when the model's raw output was
near-silent, or when the generator's checks failed it. Publishing appends new letters (f, g, ...)
after a cue's existing choices: existing letters, selections.json and its backup are untouched,
and a cue that already has takes from this round is skipped. No Unity audio is imported.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

import numpy as np
import soundfile as sf

ROOT = Path(__file__).resolve().parents[1]
REVIEW = ROOT / 'ArtSource/PSXCandidateRound'
PUBLIC = ROOT / 'SoundLab/public'
TOOL = Path.home() / 'Tools/local-sounds'
FINETUNE = TOOL / 'finetune/psx'
LETTERS = 'abcdefghi'

sys.path.insert(0, str(ROOT / 'Tools'))
from iterate_item_sfx import digest, load_cli, save  # noqa: E402  (the item round's helpers)


def resample(x, sr, to=44100):
    if sr == to:
        return x
    from scipy.signal import resample_poly
    from math import gcd
    g = gcd(sr, to)
    return resample_poly(x, to // g, sr // g).astype(np.float32)


def logmel(x, sr):
    import librosa
    x = resample(x, sr)
    m = librosa.feature.melspectrogram(y=x, sr=44100, n_fft=1024, hop_length=256, n_mels=64)
    return np.log(m + 1e-6)


def similarity(a, b):
    """The fine-tune's memorisation measure: correlation of time-aligned log-mel spectrograms."""
    t = min(a.shape[1], b.shape[1])
    if t < 4:
        return 0.0
    a, b = a[:, :t].ravel(), b[:, :t].ravel()
    a, b = a - a.mean(), b - b.mean()
    return float(a @ b / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-9))


def mono(path):
    x, sr = sf.read(str(path), dtype='float32', always_2d=True)
    return x.mean(axis=1), sr


def training_set():
    return {w.stem: logmel(*mono(w)) for w in sorted((FINETUNE / 'dataset').glob('*.wav'))}


def flat_top_fraction(path):
    x, _ = sf.read(str(path), dtype='float32', always_2d=True)
    a = np.abs(x).max(axis=1)
    m = a.max() + 1e-12
    hot = a >= .999 * m
    # samples that sit in runs of 3+ at the peak: hard clipping, not a single peak sample
    run = np.convolve(hot.astype(int), np.ones(3, int), 'same') >= 3
    return float(run.mean())


def assess(take, train):
    x, sr = mono(take['paths']['wav'])
    lm = logmel(x, sr)
    best = max(((similarity(lm, v), k) for k, v in train.items()), default=(0.0, None))
    raw_peak = take.get('signal', {}).get('peak_dbfs')
    return dict(ref=take['ref'], rank_score=take.get('rank_score'), rejected=bool(take.get('rejected')),
                fails=take.get('fails', []), warnings=take.get('warnings', []),
                maxTrainingSimilarity=round(best[0], 3), nearestTrainingClip=best[1],
                rawPeakDbfs=raw_peak, flatTopFraction=round(flat_top_fraction(take['paths']['wav']), 5),
                seconds=round(len(x) / sr, 3))


def eligible(a, plan):
    return (not a['rejected'] and not a['fails'] and a['maxTrainingSimilarity'] < plan['similarityLimit']
            and (a['rawPeakDbfs'] is None or a['rawPeakDbfs'] >= plan['minRawPeakDbfs'])
            and a['flatTopFraction'] < .001)


def generate(cli, plan, cue, summary, train, seed_base):
    entry = summary['cues'].setdefault(cue['id'], dict(requests=[], takes=[]))
    rounds = 1 + plan['maxExtraRounds']
    for r in range(rounds):
        if r > 0 and sum(eligible(t, plan) for t in entry['takes']) >= plan['publishPerCue']:
            break
        for k, strength in enumerate(plan['strengths']):
            seeds = [seed_base + r * 1000 + k * 100 + n for n in range(plan['takesPerStrength'])]
            body = dict(text=cue['prompt'], raw=True, engine=plan['engine'], lora_strength=strength, seeds=seeds,
                        duration=cue['duration'], mode=cue['mode'], check=True, name='Psx' + cue['id'][:30])
            res = cli.call('/api/generate', body)
            entry['requests'].append(dict(id=res['id'], strength=strength, seeds=seeds, prompt=res.get('prompt')))
            for take in res['takes']:
                entry['takes'].append(dict(assess(take, train), strength=strength, request=res['id']))
            print(f"{cue['id']:18s} round {r} strength {strength}: "
                  + ' '.join(f"{t['maxTrainingSimilarity']:.2f}{'' if eligible(t, plan) else '!'}" for t in entry['takes'][-len(seeds):]),
                  flush=True)


def pick(plan, takes, lm_of):
    """Up to publishPerCue eligible takes: best rank first, both strengths when both pass, no near-twins."""
    ok = sorted([t for t in takes if eligible(t, plan)], key=lambda t: -(t['rank_score'] or 0))
    chosen = []
    for want in plan['strengths']:  # the best take of each strength first
        first = next((t for t in ok if t['strength'] == want), None)
        if first:
            chosen.append(first)
    for t in ok:
        if len(chosen) >= plan['publishPerCue']:
            break
        if t in chosen or any(similarity(lm_of(t), lm_of(c)) >= .95 for c in chosen):
            continue
        chosen.append(t)
    return chosen[:plan['publishPerCue']]


def rms_active(x):
    a = np.abs(x)
    keep = a > a.max() * 10 ** (-30 / 20)
    return float(np.sqrt(np.mean(x[keep] ** 2))) if keep.any() else 0.0


def publish(cli, plan, summary):
    manifest = json.loads((PUBLIC / 'manifest.json').read_text(encoding='utf-8'))
    source = plan['source']
    strip = lambda m: [{**c, 'choices': [ch for ch in c['choices'] if ch.get('source') != source]} for c in m['cues']]
    before = hashlib.sha256(json.dumps(strip(manifest), sort_keys=True).encode()).hexdigest()
    cache = {}

    def lm_of(t):
        if t['ref'] not in cache:
            cache[t['ref']] = logmel(*mono(path_of(t)))
        return cache[t['ref']]

    def path_of(t):
        rec = cli.call('/api/requests/' + t['request'])
        return next(tk for tk in rec['takes'] if tk['ref'] == t['ref'])['paths']['wav']

    added = {}
    by_id = {c['id']: c for c in manifest['cues']}
    for cue in plan['cues']:
        result = summary['cues'].get(cue['id'])
        target = by_id.get(cue['id'])
        if not result or target is None:
            continue
        if any(ch.get('source') == source for ch in target['choices']):
            continue  # published letters never change meaning
        chosen = pick(plan, result['takes'], lm_of)
        if not chosen:
            continue
        # Level-match to the cue's existing alternatives so louder never just sounds better.
        refs = [ch for ch in target['choices'] if ch.get('source') != source]
        ref_rms = float(np.median([rms_active(mono(PUBLIC / ch['file'])[0]) for ch in refs])) if refs else None
        stage = REVIEW / 'delivered' / cue['id']
        folder = PUBLIC / 'audio' / cue['id']
        letters = LETTERS[len(target['choices']):]
        if len(letters) < len(chosen):
            raise ValueError('No free letters for ' + cue['id'])
        new = []
        for i, t in enumerate(chosen):
            rec = cli.call('/api/requests/' + t['request'])
            take = next(tk for tk in rec['takes'] if tk['ref'] == t['ref'])
            extra = {k: t[k] for k in ('strength', 'maxTrainingSimilarity', 'nearestTrainingClip', 'rawPeakDbfs',
                                       'flatTopFraction')}
            delivered = next(p for p in cli.deliver(rec, [{**take, '_extra': {**extra, 'human_approved': False}}],
                                                    stage, png=False, name='PsxTake') if p.suffix == '.wav')
            x, sr = sf.read(str(delivered), dtype='float32', always_2d=True)
            if ref_rms:
                x = x * (ref_rms / max(rms_active(x.mean(axis=1)), 1e-6))
            x = x * min(1.0, .89 / (np.abs(x).max() + 1e-9))
            letter = letters[i]
            out = folder / f'psx_{letter}.wav'
            if out.exists():
                raise FileExistsError(out)
            folder.mkdir(parents=True, exist_ok=True)
            sf.write(str(out), x, sr, subtype='PCM_24')
            note = f"PSX LoRA {t['strength']:g}. Closest training clip {t['maxTrainingSimilarity']:.2f}."
            if any('chopped' in w or 'cut off' in w for w in t['warnings']):
                note += ' The automatic check suspected a cut-off tail.'
            new.append(dict(id=letter, label=f"PSX take {i + 1} (LoRA {t['strength']:g})", description=note,
                            file=out.relative_to(PUBLIC).as_posix(), seconds=round(len(x) / sr, 3), sampleRate=sr,
                            sha256=digest(out), ref=t['ref'], source=source, loraStrength=t['strength'],
                            maxTrainingSimilarity=t['maxTrainingSimilarity'],
                            nearestTrainingClip=t['nearestTrainingClip'], flatTopFraction=t['flatTopFraction'],
                            rawPeakDbfs=t['rawPeakDbfs'], human_approved=False))
        target['choices'].extend(new)
        added[cue['id']] = [c['id'] for c in new]
    if hashlib.sha256(json.dumps(strip(manifest), sort_keys=True).encode()).hexdigest() != before:
        raise ValueError('Existing sound bank changed')
    save(PUBLIC / 'manifest.json', manifest)
    return added


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--dry-run', action='store_true')
    parser.add_argument('--start', action='store_true')
    parser.add_argument('--start-anyway', action='store_true')
    parser.add_argument('--publish', action='store_true')
    parser.add_argument('--only', nargs='+')
    args = parser.parse_args()
    plan = json.loads((REVIEW / 'cues.json').read_text(encoding='utf-8'))
    cues = [c for c in plan['cues'] if not args.only or c['id'] in args.only]
    if args.only and set(args.only) - {c['id'] for c in cues}:
        parser.error('Unknown cue')
    if args.dry_run:
        per = len(plan['strengths']) * plan['takesPerStrength']
        print(json.dumps(dict(cues=len(cues), first_round=len(cues) * per,
                              max_generations=len(cues) * per * (1 + plan['maxExtraRounds']), engine=plan['engine'],
                              strengths=plan['strengths'], human_approved=False,
                              prompts={c['id']: c['prompt'] for c in cues}), indent=2, ensure_ascii=False))
        return
    cli = load_cli()
    cli.ensure_server(start=args.start or args.start_anyway, anyway=args.start_anyway)
    summary_path = REVIEW / 'generation.json'
    summary = json.loads(summary_path.read_text(encoding='utf-8')) if summary_path.exists() else dict(
        schemaVersion=1, human_approved=False, cues={})
    if args.publish:
        print(json.dumps(dict(added=publish(cli, plan, summary), human_approved=False), indent=2))
        return
    train = training_set()
    index = {c['id']: i for i, c in enumerate(plan['cues'])}
    for cue in cues:
        if cue['id'] in summary['cues']:
            continue
        generate(cli, plan, cue, summary, train, 470000 + index[cue['id']] * 10000)
        save(summary_path, summary)


if __name__ == '__main__':
    main()
