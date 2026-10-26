"""Run the installed local iterator against fixed item briefs; publish audition copies only.

Use the local-sounds venv. No Unity audio is imported and no saved choices are changed.
--start uses the generator's normal GPU guard. --start-anyway requires Miguel's permission.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
import wave

ROOT=Path(__file__).resolve().parents[1]
REVIEW=ROOT/'ArtSource/ItemEffectsAudioReview'
TOOL=Path.home()/'Tools/local-sounds'
SOURCE='local-sa3-item-effects-v1'

def save(path,value):
    temporary=path.with_suffix(path.suffix+'.pending')
    temporary.write_text(json.dumps(value,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    temporary.replace(path)

def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def audition_description(take):
    notes=[]
    if any('tail is chopped' in warning for warning in take.get('warnings', [])):
        notes.append('The automatic check detected a possibly cut-off tail.')
    if any('out of phase' in warning for warning in take.get('warnings', [])):
        notes.append('The automatic check found a possible stereo phase issue.')
    if take.get('review', {}).get('warnings'):
        notes.append('The automatic listener was uncertain.')
    return ' '.join(notes) or 'Locally generated; your listening decides.'

def load_cli():
    sys.path.insert(0,str(TOOL))
    spec=importlib.util.spec_from_file_location('item_sfx_cli',TOOL/'scripts/sfx.py')
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    return module

def publish(plan,summary,cli):
    public=ROOT/'SoundLab/public'
    manifest=json.loads((public/'manifest.json').read_text(encoding='utf-8'))
    existing={cue['id']:cue for cue in manifest['cues']}
    base_fingerprint=hashlib.sha256(json.dumps([cue for cue in manifest['cues'] if cue.get('source')!=SOURCE],sort_keys=True).encode()).hexdigest()
    from sfx import iteration
    added=[]
    for cue in plan['cues']:
        result=summary['cues'].get(cue['id'],{})
        if result.get('returncode') != 0 or result.get('session') is None: continue
        session=json.loads(Path(result['session']).read_text(encoding='utf-8'))
        if not session.get('best'): continue
        # An existing letter must never silently refer to a different sound after a saved pick.
        if cue['id'] in existing:
            if existing[cue['id']].get('source')!=SOURCE: raise ValueError('Existing cue ID collision: '+cue['id'])
            continue
        selected=[]
        candidates=sorted([session['best']]+session['candidates'],key=lambda candidate: (
            not any('tail is chopped' in warning for warning in candidate.get('warnings', [])),
            iteration.key(candidate)),reverse=True)
        for candidate in candidates:
            if candidate.get('fails') or candidate.get('rejected') or candidate['ref'] in {c['ref'] for c in selected}: continue
            selected.append(candidate)
            if len(selected)==5: break
        folder=public/'audio'/cue['id'];folder.mkdir(parents=True,exist_ok=True)
        alternatives=[]
        for index,take in enumerate(selected):
            record=cli.call('/api/requests/'+take['ref'].split(':')[0])
            exported=cli.deliver(record,[{**take,'_extra':{'review':take['review'],'human_approved':False}}],folder,png=True,name='LocalTake')
            path=next(path for path in exported if path.suffix=='.wav')
            with wave.open(str(path),'rb') as audio:
                seconds=audio.getnframes()/audio.getframerate();sample_rate=audio.getframerate()
            alternatives.append(dict(id='abcde'[index],label='Local take '+str(index+1),
                description=audition_description(take),
                file=path.relative_to(public).as_posix(),seconds=seconds,sampleRate=sample_rate,sha256=digest(path),
                ref=take['ref'],review=take['review'],human_approved=False))
        if not alternatives: continue
        original=None
        if cue.get('original'):
            key=cue['original']
            path=ROOT/'Assets/Resources/Sounds'/((key[5:]+'.wav') if key.startswith('root:') else 'Items/'+key+'.wav')
            if path.is_file():
                target=folder/'original.wav';shutil.copyfile(path,target)
                with wave.open(str(target),'rb') as audio: seconds=audio.getnframes()/audio.getframerate()
                original=dict(file=target.relative_to(public).as_posix(),label='Current sound',seconds=seconds,sha256=digest(target))
        entry=dict(id=cue['id'],label=cue['label'],group='Item effects — local takes',family='energy',gameGain=.12,
            mode=cue['mode'],source=SOURCE,unityCue=cue['id'],original=original,choices=alternatives)
        manifest['cues'].append(entry);existing[cue['id']]=entry;added.append(cue['id'])
    if hashlib.sha256(json.dumps([cue for cue in manifest['cues'] if cue.get('source')!=SOURCE],sort_keys=True).encode()).hexdigest()!=base_fingerprint:
        raise ValueError('Existing sound bank changed')
    save(public/'manifest.json',manifest)
    return added

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dry-run',action='store_true')
    parser.add_argument('--start',action='store_true')
    parser.add_argument('--start-anyway',action='store_true')
    parser.add_argument('--publish',action='store_true')
    parser.add_argument('--only',nargs='+')
    parser.add_argument('--retry-failed',action='store_true')
    args=parser.parse_args()
    plan=json.loads((REVIEW/'cues.json').read_text(encoding='utf-8'))
    cues=[cue for cue in plan['cues'] if not args.only or cue['id'] in args.only]
    cue_indices={cue['id']:index for index,cue in enumerate(plan['cues'])}
    if args.only and set(args.only)-{cue['id'] for cue in cues}: parser.error('Unknown cue')
    if args.dry_run:
        print(json.dumps(dict(cues=len(cues),max_generations=len(cues)*plan['initialTakes']*(plan['repairRounds']+1),
            engine='Stable Audio 3 Small-SFX',listener='MiDashengLM 0.6B on CPU',human_approved=False,
            briefs=[{'id':cue['id'],**cue['brief']} for cue in cues]),indent=2))
        return
    cli=load_cli()
    cli.ensure_server(start=args.start or args.start_anyway,anyway=args.start_anyway)
    summary_path=REVIEW/'generation.json'
    summary=json.loads(summary_path.read_text(encoding='utf-8')) if summary_path.exists() else dict(schemaVersion=1,human_approved=False,cues={})
    if not args.publish:
        for cue in cues:
            previous=summary['cues'].get(cue['id'])
            if previous and not (args.retry_failed and previous.get('returncode')!=0): continue
            folder=REVIEW/'Iterations'/cue['id'];folder.mkdir(parents=True,exist_ok=True)
            brief=folder/'brief.json';save(brief,cue['brief'])
            duration=max(cue['duration'], .5) if previous and cue['mode']=='oneshot' else cue['duration']
            attempt=len(list(folder.glob('iteration-*')))
            command=[str(TOOL/'.venv/Scripts/python.exe'),str(ROOT/'Tools/run_sound_iteration.py'),'--brief',str(brief),
                '--out',str(folder),'--duration',str(duration),'--mode',cue['mode'],'--repeat-hz',str(cue['repeat_hz']),
                '--n',str(plan['initialTakes']),'--rounds',str(plan['repairRounds']),
                '--seed',str(260026+cue_indices[cue['id']]*100+attempt*500000),'--no-start']
            mix=REVIEW/'mix-bed.wav'
            if mix.is_file(): command.extend(['--mix',str(mix)])
            print('Creating '+cue['id'],flush=True)
            result=subprocess.run(command,cwd=ROOT)
            sessions=sorted(folder.glob('iteration-*/session.json'),key=lambda path:path.stat().st_mtime)
            summary['cues'][cue['id']]=dict(returncode=result.returncode,session=str(sessions[-1]) if sessions else None)
            save(summary_path,summary)
            if not cli.server_up(): break
    else:
        print(json.dumps(dict(added=publish(plan,summary,cli),human_approved=False),indent=2))

if __name__=='__main__': main()
