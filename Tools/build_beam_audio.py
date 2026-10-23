"""Render beam charge, ignition, sustain and release using Sound Lab's sample workflow.

Requires ffmpeg and the existing Frontlines source-audio folder. Does not change
Sound Lab selections or any previously selected game sound.
"""
import array
import hashlib
import json
import math
from pathlib import Path
import subprocess
import wave

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path.home() / 'Desktop/Frontlines/SourceAssets/Audio'
OUT = ROOT / 'Assets/Resources/Sounds/Beam'
REVIEW = ROOT / 'ArtSource/CombatReview/BeamAudio'
RATE = 22050
CUTS = {
    'arc': ('StableAudio/Combat/arcane.mp3', .035, .24),
    'electric': ('StableAudio/Combat/electric.mp3', 0, .085),
    'air': ('StableAudio/attack.mp3', .026, .225),
    'dark': ('StableAudio/Combat/dark.mp3', .015, .4),
}

def read(file, start, end):
    raw = subprocess.check_output(['ffmpeg', '-v', 'error', '-i', str(file), '-ss', str(start),
        '-t', str(end-start), '-ac', '1', '-ar', str(RATE), '-f', 'f32le', '-'])
    result = array.array('f'); result.frombytes(raw)
    return list(result)

def pitch(samples, rate):
    out = []
    for i in range(int((len(samples)-1)/rate)):
        p=i*rate; n=int(p); f=p-n
        out.append(samples[n]*(1-f)+samples[n+1]*f)
    return out

def add(target, source, offset, gain, circular=False):
    start=round(offset*RATE)
    for i,x in enumerate(source):
        n=start+i
        if circular: n%=len(target)
        if n<len(target): target[n]+=x*gain

def tone(samples, cutoff, target=.12, loop=False):
    a=1-math.exp(-2*math.pi*cutoff/RATE); v=w=0.; result=[]
    # Prime the low-pass from the end so a loop has the same filter history at its seam.
    if loop:
        for x in samples[-min(len(samples),2048):]:v+=a*(x-v);w+=a*(v-w)
    for x in samples:v+=a*(x-v);w+=a*(v-w);result.append(w)
    mean=sum(result)/len(result);result=[x-mean for x in result]
    if loop:
        seam=round(.014*RATE)
        for i in range(seam):
            f=.5-.5*math.cos(math.pi*i/seam)
            result[i]*=f;result[-1-i]*=f
    else:
        for i in range(len(result)):
            attack=min(1,i/(RATE*.004)); release=min(1,(len(result)-1-i)/(RATE*.035))
            result[i]*=(.5-.5*math.cos(math.pi*attack))*(.5-.5*math.cos(math.pi*release))
    rms=math.sqrt(sum(x*x for x in result)/len(result)); peak=max(map(abs,result))
    gain=min(target/max(rms,.0001),.65/max(peak,.0001))
    return [x*gain for x in result]

def save(path, samples):
    path.parent.mkdir(parents=True,exist_ok=True)
    pcm=array.array('h',(round(max(-1,min(1,x))*32767) for x in samples))
    with wave.open(str(path),'wb') as f:f.setparams((1,2,RATE,0,'NONE','not compressed'));f.writeframes(pcm.tobytes())
    return dict(seconds=round(len(samples)/RATE,3),peak=round(max(map(abs,samples)),4),
        rms=round(math.sqrt(sum(x*x for x in samples)/len(samples)),4),sha256=hashlib.sha256(path.read_bytes()).hexdigest())

def main():
    source={key:read(SOURCE/path,start,end) for key,(path,start,end) in CUTS.items()}
    charge=[0.] * round(RATE*.8)
    swell=pitch(source['arc'][::-1],.42)
    for i in range(len(swell)):swell[i]*=(i/max(1,len(swell)-1))**.65
    add(charge,swell,0,.9);add(charge,pitch(source['air'][::-1],.45),.29,.45)
    ignite=[0.] * round(RATE*.32)
    add(ignite,pitch(source['electric'],.66),0,.8);add(ignite,pitch(source['arc'],.85),.015,.8)
    add(ignite,pitch(source['air'],.7),.005,.4)
    sustain=[0.] * round(RATE*.72)
    for n in range(8):
        grain=pitch(source['arc'],.85+(n%3)*.055)
        grain=[x*math.sin(math.pi*i/max(1,len(grain)-1))**2 for i,x in enumerate(grain)]
        add(sustain,grain,n*.09,.42,True)
    for n in range(4):add(sustain,pitch(source['electric'],.52+n*.035),n*.18,.12,True)
    release=[0.] * round(RATE*.24)
    add(release,pitch(source['air'],.95),0,.5);add(release,pitch(source['dark'],1.5),0,.25)
    clips={'charge':tone(charge,3500,.10),'ignite':tone(ignite,5100,.14),
           'sustain':tone(sustain,3000,.09,True),'release':tone(release,2400,.08)}
    report={'workflow':'Sound Lab / Frontlines sample layers; new laser cues only','sampleRate':RATE,
        'sources':[{ 'id':key,'path':path,'start':start,'end':end,
          'sha256':hashlib.sha256((SOURCE/path).read_bytes()).hexdigest()} for key,(path,start,end) in CUTS.items()], 'clips':{}}
    for key,clip in clips.items():report['clips'][key]=save(OUT/(key+'.wav'),clip)
    # One minute: repeated drone lances, then seven-beam boss arrays. The runtime
    # coalesces ignition transients and reduces loop gain as more beams are active.
    mix=[0.] * (RATE*60)
    for n in range(22):
        start=n*2.5; count=1 if n<12 else 7; duration=.65 if count==1 else 3.6
        add(mix,clips['charge'],start,.20);add(mix,clips['ignite'],start+.8,.24)
        for beam in range(count):
            loop=clips['sustain']; length=round(RATE*duration)
            run=[loop[(i+beam*911)%len(loop)] * min(1,i/(RATE*.055),(length-1-i)/(RATE*.06)) for i in range(length)]
            add(mix,run,start+.8,.15/math.sqrt(count))
        add(mix,clips['release'],start+.8+duration,.14)
    report['repetitionPreview']=save(REVIEW/'BeamEndurance.wav',mix)
    REVIEW.mkdir(parents=True,exist_ok=True)
    (REVIEW/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
    (REVIEW/'SOURCE-LICENSES.txt').write_text((SOURCE/'LICENSES.txt').read_text(encoding='utf-8'),encoding='utf-8')
    print(json.dumps(report['clips'],indent=2));print('Preview peak:',report['repetitionPreview']['peak'])

if __name__=='__main__': main()
