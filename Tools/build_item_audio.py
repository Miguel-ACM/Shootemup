"""New item cues from the existing Sound Lab / Frontlines sample cuts.
Never modifies SoundLab selections or previously approved game sounds.
"""
import json
import sys
sys.dont_write_bytecode=True
from build_beam_audio import ROOT, SOURCE, RATE, CUTS, read, pitch, add, tone, save
import hashlib

OUT=ROOT/'Assets/Resources/Sounds/Items'
REVIEW=ROOT/'ArtSource/ItemFeedbackReview/Audio'

def main():
    src={k:read(SOURCE/p,a,b) for k,(p,a,b) in CUTS.items()}
    # (duration, layers: sample, pitch, offset, gain, reversed)
    recipes={
      'windup':(1.3,[('arc',.42,0,.6,True),('electric',.7,.48,.3,True),('arc',.85,.8,.7,True),('electric',1.4,1.05,.3,True)]),
      'ready':(.24,[('electric',1.5,0,.5,False),('arc',1.6,.08,.4,False)]),
      'cannon':(.4,[('dark',.7,0,.7,False),('electric',.65,0,.8,False),('air',.9,.02,.3,False)]),
      'popcorn':(.15,[('air',2.6,0,.8,False),('electric',1.8,.015,.25,False)]),
      'frost':(.25,[('arc',1.8,0,.4,True),('electric',2.2,.12,.3,False)]),
      'shatter':(.32,[('electric',2,0,.5,False),('air',1.8,.03,.6,False),('electric',1.5,.09,.3,False)]),
      'capacitor':(.3,[('electric',.8,0,.7,False),('arc',1.2,.02,.4,False)]),
      'overdrive':(.45,[('dark',.7,0,.4,True),('electric',.8,.23,.7,False)]),
      'powerdown':(.35,[('arc',.7,0,.5,False),('air',.8,.08,.4,False)]),
      'static':(.22,[('electric',1.1,0,.8,False),('electric',1.6,.06,.3,False)]),
      'roll':(.32,[('air',.8,0,.8,False)]),
      'fan':(.55,[('air',.5,0,.6,False),('air',.7,.2,.3,False)]),
      'decoy':(.4,[('arc',.7,0,.5,True),('electric',1.4,.22,.4,False)]),
      'heal':(.45,[('arc',1.3,0,.4,False),('arc',1.65,.15,.4,False)]),
      'timeStop':(.5,[('dark',.9,0,.5,True),('air',.65,.15,.6,True)]),
    }
    report={'workflow':'Sound Lab / Frontlines sample layering; technical levels only, not a listening approval','sources':[], 'clips':{}}
    for k,(p,a,b) in CUTS.items():report['sources'].append(dict(id=k,path=p,start=a,end=b,sha256=hashlib.sha256((SOURCE/p).read_bytes()).hexdigest()))
    reel=[0.]*(RATE*len(recipes)*2)
    for n,(name,(duration,layers)) in enumerate(recipes.items()):
        raw=[0.]*round(RATE*duration)
        for key,rate,offset,gain,reverse in layers:add(raw,pitch(src[key][::-1] if reverse else src[key],rate),offset,gain)
        clip=tone(raw,4000,.10);report['clips'][name]=save(OUT/(name+'.wav'),clip);add(reel,clip,n*2,.7)
    REVIEW.mkdir(parents=True,exist_ok=True)
    report['preview']=save(REVIEW/'ItemCues.wav',reel)
    (REVIEW/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
    (REVIEW/'SOURCE-LICENSES.txt').write_text((SOURCE/'LICENSES.txt').read_text(encoding='utf-8'),encoding='utf-8')
    print(json.dumps(report['clips'],indent=2))

if __name__=='__main__':main()
