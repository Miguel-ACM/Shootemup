"""Import only explicitly chosen local item takes after Finish selection; never changes picks."""
import argparse
import datetime
import hashlib
import json
from pathlib import Path
import re
import shutil
import uuid

ROOT=Path(__file__).resolve().parents[1]
SOURCE='local-sa3-item-effects-v1'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def prepare(root):
    public=root/'SoundLab/public'
    manifest=json.loads((public/'manifest.json').read_text(encoding='utf-8'))
    selections=json.loads((root/'SoundLab/selections.json').read_text(encoding='utf-8'))
    if selections.get('status')!='ready': raise ValueError('Finish selection before importing audio.')
    if selections.get('bankVersion')!=manifest.get('bankVersion'): raise ValueError('Sound bank version mismatch.')
    targets=[]
    for cue in manifest['cues']:
        if cue.get('source')!=SOURCE: continue
        name=cue.get('unityCue','')
        if not re.fullmatch(r'item[A-Za-z0-9]+',name) or name!=cue['id']: raise ValueError('Invalid item cue destination.')
        pick=selections.get('picks',{}).get(cue['id'],{})
        if pick.get('choice') in (None,'pending','reject','original'): continue
        choice=next((choice for choice in cue['choices'] if choice['id']==pick['choice']),None)
        if choice is None: raise ValueError('Unknown chosen take: '+cue['id'])
        source=(public/choice['file']).resolve()
        if not source.is_relative_to(public.resolve()) or source.suffix.lower()!='.wav': raise ValueError('Invalid audio source path.')
        if sha(source)!=choice['sha256']: raise ValueError('Chosen take hash mismatch: '+cue['id'])
        gain=pick.get('gain',1)
        if isinstance(gain,bool) or not isinstance(gain,(int,float)) or not 0<=gain<=1.5: raise ValueError('Invalid approved effect level.')
        targets.append(dict(cue=name,source=source,gain=gain,sha256=choice['sha256'],ref=choice.get('ref')))
    return targets

def meta(path):
    metadata=Path(str(path)+'.meta')
    if not metadata.exists(): metadata.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')

def apply(root,targets):
    resources=root/'Assets/Resources'
    gains=resources/'ItemSoundGains.json'
    previous=json.loads(gains.read_text(encoding='utf-8')) if gains.exists() else {'cues':[]}
    levels={entry['cue']:entry for entry in previous['cues']}
    # All sources and choices were checked before creating any Unity files.
    stamp=datetime.datetime.now().strftime('%Y%m%d-%H%M%S-%f')
    archive=root/'ArtSource/ItemEffectsAudioReview/Applied'/stamp
    archive.mkdir(parents=True,exist_ok=False)
    if gains.exists(): shutil.copyfile(gains,archive/'ItemSoundGains.previous.json')
    output=resources/'Sounds/ItemApproved'
    output.mkdir(parents=True,exist_ok=True);meta(output)
    imported=[]
    for target in targets:
        destination=output/(target['cue']+'.wav')
        if destination.exists() and sha(destination)!=target['sha256']:
            shutil.copyfile(destination,archive/(target['cue']+'.previous.wav'))
        if not destination.exists() or sha(destination)!=target['sha256']:
            staged=destination.with_suffix('.wav.pending');shutil.copyfile(target['source'],staged);staged.replace(destination)
        meta(destination)
        levels[target['cue']]={'cue':target['cue'],'gain':target['gain']}
        imported.append({key:str(value) if isinstance(value,Path) else value for key,value in target.items()})
    staged=gains.with_suffix('.json.pending')
    staged.write_text(json.dumps({'cues':[levels[name] for name in sorted(levels)]},indent=2)+'\n',encoding='utf-8')
    staged.replace(gains);meta(gains)
    (archive/'approval.json').write_text(json.dumps({'selections_sha256':sha(root/'SoundLab/selections.json'),'imported':imported},indent=2)+'\n',encoding='utf-8')
    return imported

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply',action='store_true',help='Apply ready choices; otherwise show the exact import plan.')
    args=parser.parse_args()
    targets=prepare(ROOT)
    if args.apply and targets: apply(ROOT,targets)
    print(json.dumps({'mode':'applied' if args.apply else 'plan','cues':[{'cue':target['cue'],'gain':target['gain'],'sha256':target['sha256']} for target in targets]},indent=2))

if __name__=='__main__': main()
