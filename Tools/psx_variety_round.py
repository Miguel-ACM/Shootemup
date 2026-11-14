"""PSX LoRA variety round: six different sound designs per cue, one take kept per design.

  & $taskSfxPython Tools/psx_variety_round.py --dry-run
  & $taskSfxPython Tools/psx_variety_round.py --start [--only shot1 coin]

Every cue of ArtSource/PSXCandidateRound/cues.json gets the designs in Tools/psx_variety_prompts.py.
Each design is generated with the `psx` engine at LoRA strength 1.0 (and again at 0.75 when no 1.0
take passes), and its best eligible take is kept. A design whose kept take sounds like one already
kept for the cue (log-mel similarity >= .9) is dropped, so the options stay distinct sounds rather
than versions. Eligibility is the earlier round's: no generator failure, no near-copy of a training
clip, no near-silent raw output, no hard clipping. Results are resumable in
ArtSource/PSXVarietyRound/generation.json; tools/build_psx_picks_artifact.py builds the page.
"""
import argparse
import json
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from psx_sfx_round import REVIEW as FIRST, assess, eligible, load_cli, logmel, mono, similarity, training_set  # noqa: E402
from psx_variety_prompts import EXTRA, EXTRA_LOW, VARIANTS  # noqa: E402
from iterate_item_sfx import save  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'ArtSource/PSXVarietyRound'
SEEDS = 3
STRENGTHS = (1.0, .75)
EXTRA_STRENGTHS = (.6, .45)
LOW_STRENGTHS = (.35, .25)
DISTINCT = .9
# Broadband booms correlate highly on log-mel even when they sound different; these cues use a looser bar.
LOOSE = {'enemyBurst': .93, 'explosionLarge': .93, 'bossDeath': .93}


def prompt_of(cue, variant):
    event = cue['prompt'].split(',')[0]
    dry = ', no room sound' if 'no room sound' in cue['prompt'] else ''
    return f'{event}: {variant}, low-bitrate console sample, mono{dry}'


def designs(cid):
    """(text, strengths): the six designs, then the second pass's extras at lower LoRA strengths,
    which let the base model stray further from the training sounds when short cues collapse."""
    return ([(v, STRENGTHS) for v in VARIANTS[cid]] + [(v, EXTRA_STRENGTHS) for v in EXTRA.get(cid, [])]
            + [(v, LOW_STRENGTHS) for v in EXTRA_LOW.get(cid, [])])


def run_cue(cli, plan, cue, train, entry, seed_base):
    for v, (variant, strengths) in enumerate(designs(cue['id'])):
        slot = entry['variants'].setdefault(str(v), dict(text=prompt_of(cue, variant), takes=[]))
        if slot.get('done'):
            continue
        for k, strength in enumerate(strengths):
            if any(eligible(t, plan) for t in slot['takes']):
                break
            seeds = [seed_base + v * 100 + k * 10 + n for n in range(SEEDS)]
            body = dict(text=slot['text'], raw=True, engine='psx', lora_strength=strength, seeds=seeds,
                        duration=cue['duration'], mode=cue['mode'], check=True, name='PsxVar' + cue['id'][:24])
            res = cli.call('/api/generate', body)
            for take in res['takes']:
                slot['takes'].append(dict(assess(take, train), strength=strength, request=res['id'],
                                          wav=take['paths']['wav']))
        slot['done'] = True
        ok = [t for t in slot['takes'] if eligible(t, plan)]
        print(f"{cue['id']:18s} {v}: {len(ok)}/{len(slot['takes'])} ok  {variant}", flush=True)


def choose(plan, entry, distinct=DISTINCT):
    """One take per design, best rank first, dropping designs that came out like an earlier one."""
    best = []
    for v, slot in sorted(entry['variants'].items(), key=lambda kv: int(kv[0])):
        if 'done' not in slot:
            continue
        ok = sorted((t for t in slot['takes'] if eligible(t, plan)), key=lambda t: -(t['rank_score'] or 0))
        if ok:
            best.append((int(v), slot['text'], ok))
    chosen, cache = [], {}
    lm = lambda t: cache.setdefault(t['wav'], logmel(*mono(t['wav'])))
    for v, text, ok in best:
        take = next((t for t in ok if all(similarity(lm(t), lm(c['take'])) < distinct for c in chosen)), None)
        if take:
            chosen.append(dict(variant=v, text=text, take=take))
    return chosen


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--dry-run', action='store_true')
    parser.add_argument('--start', action='store_true')
    parser.add_argument('--only', nargs='+')
    parser.add_argument('--choose', action='store_true', help='recompute the kept takes without generating')
    args = parser.parse_args()
    plan = json.loads((FIRST / 'cues.json').read_text(encoding='utf-8'))
    cues = [c for c in plan['cues'] if not args.only or c['id'] in args.only]
    missing = [c['id'] for c in cues if c['id'] not in VARIANTS]
    if missing:
        parser.error('No designs for ' + ', '.join(missing))
    if args.dry_run:
        print(json.dumps({c['id']: [prompt_of(c, v) for v in VARIANTS[c['id']]] for c in cues}, indent=1))
        print(len(cues), 'cues,', sum(len(VARIANTS[c['id']]) for c in cues) * SEEDS, 'first-pass generations')
        return
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / 'generation.json'
    summary = json.loads(path.read_text(encoding='utf-8')) if path.exists() else dict(schemaVersion=1, cues={})
    if not args.choose:
        cli = load_cli()
        cli.ensure_server(start=args.start, anyway=False)
        train = training_set()
    index = {c['id']: i for i, c in enumerate(plan['cues'])}
    for cue in cues:
        entry = summary['cues'].setdefault(cue['id'], dict(variants={}))
        if not args.choose:
            run_cue(cli, plan, cue, train, entry, 910000 + index[cue['id']] * 1000)
        entry['chosen'] = [dict(variant=c['variant'], wav=c['take']['wav'], strength=c['take']['strength'],
                                similarity=c['take']['maxTrainingSimilarity']) for c in choose(plan, entry, LOOSE.get(cue['id'], DISTINCT))]
        print(f"{cue['id']:18s} -> {len(entry['chosen'])} distinct", flush=True)
        save(path, summary)


if __name__ == '__main__':
    main()
