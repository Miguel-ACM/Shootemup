"""Prepare review-only Heaven SFX pages. Never import audio or change Sound Lab selections."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
REVIEW=ROOT/'ArtSource/HeavenVoidReview'
records=[]
for key,label,source,bank in [('charge','Heaven siege charge','AudioCharge','heavenSiegeCharge'),('ignite','Heaven lance ignition','AudioIgnite','heavenSiegeIgnite')]:
    session_path=sorted((REVIEW/source).glob('iteration-*/session.json'))[-1]
    session=json.loads(session_path.read_text())
    folder=REVIEW/'Candidates'/bank
    takes=sorted(folder.glob('*.wav'))
    rows=''.join('<p>'+p.stem+'</p><audio controls src="'+p.relative_to(REVIEW).as_posix()+'"></audio>' for p in takes)
    repeat=next(session_path.parent.glob('best_repeat.wav'))
    rows+='<h2>Repeated best take</h2><audio controls src="'+repeat.relative_to(REVIEW).as_posix()+'"></audio>'
    (REVIEW/(key+'-audition.html')).write_text('<!doctype html><meta charset="utf-8"><title>'+label+'</title><style>body{background:#151522;color:#ddd;font:18px system-ui;max-width:760px;margin:48px auto}audio{width:100%}</style><h1>'+label+'</h1><p>Review candidates only. No generated audio is imported into Unity. Automatic reviews are advisory and listening approval is pending.</p>'+rows,encoding='utf-8')
    records.append(dict(cue=key,ref=session['best']['ref'],seed=session['best']['seed'],status=session['status'],session=session_path.relative_to(ROOT).as_posix(),human_approved=False,runtime_imported=False,original_candidates=[p.relative_to(ROOT).as_posix() for p in takes]))
(REVIEW/'AudioProvenance.json').write_text(json.dumps(records,indent=2)+'\n')
print('Review pages prepared; no Unity or Sound Lab changes.')
