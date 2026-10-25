"""Build five sample-based alternatives per SFX without touching Unity assets.

Python standard library + ffmpeg. Run from any directory.
"""
import array
import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys
import wave

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
PUBLIC = HERE / 'public'
RATE = 22050
FRONTLINES = Path.home() / 'Desktop/Frontlines/SourceAssets/Audio'
# Known isolated cuts from the Frontlines source manifest, not the rejected synth bank.
CUTS = {
    'gun': ('StableAudio/Combat/gunshot.mp3', .075, .35),
    'cannon': ('StableAudio/Combat/cannon.mp3', 0, .34),
    'electric': ('StableAudio/Combat/electric.mp3', 0, .085),
    'arcane': ('StableAudio/Combat/arcane.mp3', .035, .24),
    'dark': ('StableAudio/Combat/dark.mp3', .015, .4),
    'fire': ('StableAudio/Combat/fire.mp3', .17, .4),
    'metal': ('StableAudio/Combat/metal-hit.mp3', 0, .23),
    'arrow': ('StableAudio/Combat/arrow-hit.mp3', .012, .15),
    'boom': ('StableAudio/Combat/explosion.mp3', 0, .48),
    'holy': ('StableAudio/Combat/holy.mp3', 0, .23),
    'hit': ('StableAudio/hit.mp3', .205, .465),
    'impact': ('StableAudio/spell-impact.mp3', 0, .24),
    'building': ('StableAudio/building-hit.mp3', 0, .39),
    'swish': ('StableAudio/attack.mp3', .026, .225),
}
# id, label, group, family, pitch, length, game gain, original clip (None = currently silent)
CUES = [
    ('shot1','Pulse cannon','Player weapons','gun',1.05,.14,.10,'shot1'),
    ('shotElectric','Electricity','Player weapons','electric',1,.16,.10,'shot1'),
    ('shotAK45','AK45 / diagonal fire','Player weapons','gun',.88,.12,.10,'shot1'),
    ('shotCompanion','Companion gun','Player weapons','gun',1.3,.09,.065,'shot1'),
    ('shotNeedle','Needle cannon','Player weapons','gun',1.45,.09,.10,'shot1'),
    ('shotRail','Rail driver','Player weapons','rail',.8,.30,.13,'shot1'),
    ('shotScatter','Scatter cannon','Player weapons','gun',.78,.18,.12,'shot1'),
    ('shotShell','Burst launcher','Player weapons','rail',.7,.24,.12,'shot1'),
    ('shotDisc','Disc thrower','Player weapons','energy',.95,.23,.10,'shot1'),
    ('shotMissile','Swarm rack / missile','Player weapons','rocket',.95,.30,.10,'shot1'),
    ('shotFlame','Flame projector','Player weapons','rocket',1.35,.12,.075,'shot1'),
    ('shotBeam','Beam emitter · fire','Player weapons','energy',.85,.55,.12,None),
    ('enemySimple','Standard round','Enemy projectiles','gun',.9,.12,.045,None),
    ('enemyNeedle','Needle round','Enemy projectiles','gun',1.3,.085,.04,None),
    ('enemyPlasma','Plasma orb','Enemy projectiles','energy',.75,.26,.055,None),
    ('enemyMissile','Guided missile','Enemy projectiles','rocket',.8,.32,.055,None),
    ('enemyBurst','Burst shell · launch','Enemy projectiles','rail',.88,.20,.05,None),
    ('enemyDisc','Returning disc','Enemy projectiles','energy',1.1,.20,.045,None),
    ('enemyRicochet','Ricochet bolt','Enemy projectiles','metal',1.2,.14,.045,None),
    ('enemyHelix','Helix round','Enemy projectiles','electric',.9,.18,.045,None),
    ('enemySuspended','Suspended round','Enemy projectiles','energy',.65,.24,.05,None),
    ('enemyScatter','Scatter carrier','Enemy projectiles','rail',1.1,.16,.05,None),
    ('enemyMine','Proximity mine · deploy','Enemy projectiles','metal',.7,.20,.05,None),
    ('enemyWave','Expanding wave','Enemy projectiles','energy',.6,.36,.055,None),
    ('beamCharge','Beam · charge warning','Enemy projectiles','charge',1,.40,.05,None),
    ('beamFire','Enemy beam · fire','Enemy projectiles','energy',.7,.40,.065,None),
    ('hit1','Hull impact','Hits & feedback','metal',1.1,.075,.05,'hit1'),
    ('hitElectric','Electric impact','Hits & feedback','electric',1.2,.10,.045,None),
    ('ricochetHit','Wall / rebound ping','Hits & feedback','metal',1.5,.11,.045,None),
    ('enemyExplosion1','Small explosion','Explosions','blast',1.1,.42,.10,'enemyExplosion1'),
    ('explosionHeavy','Heavy hull explosion','Explosions','blast',.75,.85,.14,'enemyExplosion1'),
    ('shellBurst','Shell / mine detonation','Explosions','blast',1.35,.25,.08,None),
    ('hurt1','Player damage','Player & encounter','damage',.85,.26,.20,'hurt1'),
    ('playerDead','Player destroyed','Player & encounter','blast',.65,1.15,.35,'playerDead'),
    ('blankbullet','Blank / bullet clear','Player & encounter','energy',.65,.65,.15,'blankbullet'),
    ('bossWarning','Boss warning','Player & encounter','warning',1,.85,.18,'bosswarning'),
]

# Five distinct material recipes, not five random seeds or tiny pitch variations.
# Layer tuple: source, pitch, gain, delay, reverse. Cue tuning applies after this.
RECIPES = {
 'gun': [
  ('Dry crack','A compact sampled rifle transient.', [('gun',1,1,0,False),('hit',1.2,.20,0,False)]),
  ('Arcade metal','Old shot body with a short mechanical snap.', [('oldShot',1,.75,0,False),('metal',1.45,.55,0,False)]),
  ('Heavy punch','Cannon pressure and a darker gun crack.', [('cannon',1.1,.8,0,False),('gun',.85,.6,0,False)]),
  ('Energy snap','Electrical crack above a grounded gun body.', [('electric',.85,.8,0,False),('gun',1.2,.55,.006,False)]),
  ('Soft percussion','Muted impact and a small shell-like tick.', [('hit',1.3,1,0,False),('arrow',1.8,.5,0,False)]),
 ],
 'rail': [
  ('Cannon recoil','One deep report with a clean front edge.', [('cannon',.95,1,0,False),('gun',1.2,.4,0,False)]),
  ('Steel driver','Mechanical strike and dense cannon body.', [('metal',.7,.7,0,False),('cannon',1,.8,0,False)]),
  ('Industrial blast','Compact destruction and low pressure.', [('building',1,.6,0,False),('boom',1.35,.8,0,False)]),
  ('Charged rail','An electrical snap into a weighty shot.', [('electric',.7,.7,0,False),('cannon',1.1,.9,.012,False)]),
  ('Damped thump','Low, short recoil without a bright crack.', [('hit',.7,.8,0,False),('cannon',.8,.8,0,False)]),
 ],
 'electric': [
  ('Dry arc','A short isolated electrical discharge.', [('electric',1,1,0,False)]),
  ('Plasma contact','Electrical crack and compact energy impact.', [('electric',.75,.75,0,False),('impact',1.3,.65,0,False)]),
  ('Metal spark','A hard contact snap with a muted steel edge.', [('electric',1.3,.6,0,False),('metal',1.5,.7,0,False)]),
  ('Arcane flick','Airy energy release with a tight zap.', [('arcane',1.4,.8,0,False),('electric',.9,.4,.012,False)]),
  ('Low voltage','A darker grounded electrical punch.', [('electric',.55,1,0,False),('hit',1,.45,0,False)]),
 ],
 'energy': [
  ('Plasma pulse','Compact energy impact with physical weight.', [('impact',1,1,0,False),('hit',.9,.3,0,False)]),
  ('Air-cut energy','Air swish with an electrical leading edge.', [('swish',.85,.75,0,False),('electric',.8,.7,0,False)]),
  ('Dark pressure','Hollow air and a low energy body.', [('dark',.85,1,0,False),('impact',.7,.45,0,False)]),
  ('Bright discharge','A defined electric arc and airy release.', [('electric',1.05,.7,0,False),('arcane',1.1,.8,.015,False)]),
  ('Warm pulse','Soft energy body with a restrained round edge.', [('arcane',.7,.8,0,False),('hit',.7,.65,0,False)]),
 ],
 'rocket': [
  ('Dry ignition','Compact fire ignition and a small launch kick.', [('fire',1,1,0,False),('cannon',1.6,.3,0,False)]),
  ('Pressure jet','Rushing air and a grounded low launch.', [('swish',.65,.8,0,False),('dark',1,.7,0,False)]),
  ('Heavy exhaust','Dark, fiery launch with extra body.', [('fire',.7,.85,0,False),('boom',1.1,.5,0,False)]),
  ('Ion ignition','Electric ignition into a compact fiery tail.', [('electric',.85,.55,0,False),('fire',1.1,.8,.015,False)]),
  ('Soft jet','A damped air burst with a short muted contact.', [('swish',.9,1,0,False),('hit',.8,.35,0,False)]),
 ],
 'metal': [
  ('Steel contact','A short, dry metal impact.', [('metal',1.2,1,0,False)]),
  ('Armor thud','Blunt contact with a little steel bite.', [('hit',1,1,0,False),('metal',.8,.4,0,False)]),
  ('Heavy plate','Dense plate strike with a dark body.', [('building',1.3,.7,0,False),('metal',.7,.65,0,False)]),
  ('Hot spark','A tiny electrical crack over a sharp impact.', [('electric',1.2,.6,0,False),('arrow',1.3,.85,0,False)]),
  ('Muted tick','A small tactile hit that sits behind shooting.', [('arrow',1.1,.8,0,False),('hit',1.5,.65,0,False)]),
 ],
 'blast': [
  ('Compact explosion','A sampled blast with a short debris tail.', [('boom',1,1,0,False)]),
  ('Cannon rupture','A hard report opening into pressure.', [('cannon',.8,.8,0,False),('boom',1.2,.7,.02,False)]),
  ('Industrial wreck','A hull strike and layered destruction.', [('building',.85,.8,0,False),('boom',.8,.8,.025,False),('metal',.7,.25,.08,False)]),
  ('Fuel ignition','Fiery rupture with a clear initial crack.', [('fire',.6,.6,0,False),('boom',1,.85,0,False),('gun',.7,.3,0,False)]),
  ('Dark concussion','Low, rounded pressure with less sharp debris.', [('boom',.6,.85,0,False),('dark',.7,.4,0,False)]),
 ],
 'damage': [
  ('Armor strike','Firm physical hull contact.', [('metal',.75,.7,0,False),('hit',.8,.85,0,False)]),
  ('Shield overload','A short electrical failure and low impact.', [('electric',.6,.9,0,False),('impact',.8,.7,0,False)]),
  ('Heavy knock','A compact structural thump.', [('building',1.2,.9,0,False),('hit',.7,.6,0,False)]),
  ('Energy impact','A distinct energy impact against armor.', [('impact',.75,.9,0,False),('metal',1.3,.35,0,False)]),
  ('Damped damage','Muted contact for a less abrasive warning.', [('hit',.65,1,0,False),('dark',1.2,.35,0,False)]),
 ],
 'charge': [
  ('Air intake','A single rising reversed-air gesture.', [('swish',.7,1,0,True)]),
  ('Capacitor','A restrained reversed electrical swell.', [('arcane',.7,.8,0,True),('electric',.6,.45,.18,False)]),
  ('Heavy charge','Dark pressure drawing inward.', [('dark',.8,1,0,True),('hit',.7,.2,.25,False)]),
  ('Ion charge','An airy reversed energy gesture.', [('impact',.8,.8,0,True),('arcane',1.1,.5,0,True)]),
  ('Soft intake','A darker, quieter-feeling air movement.', [('swish',.55,.8,0,True),('dark',1,.5,0,True)]),
 ],
 'warning': [
  ('Metal warning','Two separated muted metal accents.', [('metal',.65,1,0,False),('metal',.65,.8,.34,False)]),
  ('Warm signal','Two rounded chimes, restrained highs.', [('holy',.7,1,0,False),('holy',.6,.75,.35,False)]),
  ('Low threat','Two dark pressure accents.', [('dark',.8,1,0,False),('impact',.6,.7,.38,False)]),
  ('Electrical alert','Paired electrical pulses with low body.', [('electric',.65,1,0,False),('electric',.55,.85,.32,False),('dark',1,.3,0,False)]),
  ('Mechanical alert','Damped contact followed by a heavier response.', [('hit',.65,.8,0,False),('metal',.55,.65,.32,False)]),
 ],
}


def decode(path, start=0, end=None):
    args = ['ffmpeg','-v','error','-i',str(path),'-ss',str(start)]
    if end is not None:
        args += ['-t',str(end-start)]
    raw = subprocess.check_output(args+['-ac','1','-ar',str(RATE),'-f','f32le','-'])
    data = array.array('f'); data.frombytes(raw)
    if sys.byteorder != 'little': data.byteswap()
    return list(data)


def trim(samples):
    peak = max(map(abs,samples), default=0)
    active = [i for i,x in enumerate(samples) if abs(x)>peak*.012]
    if not active: raise ValueError('Silent source')
    return samples[max(0,active[0]-20):min(len(samples),active[-1]+150)]


def resample(samples, pitch):
    out=[]
    for i in range(int((len(samples)-1)/pitch)):
        pos=i*pitch; j=int(pos); f=pos-j
        out.append(samples[j]*(1-f)+samples[j+1]*f)
    return out


def lowpass(samples, cutoff):
    a=1-math.exp(-2*math.pi*cutoff/RATE); v=w=0.; out=[]
    for x in samples:
        v+=a*(x-v); w+=a*(v-w); out.append(w)
    return out


def finish(samples, duration, cutoff):
    samples=lowpass(samples,cutoff)
    samples=samples[:round(duration*RATE)]
    if not samples: raise ValueError('Empty sound')
    mean=sum(samples)/len(samples)
    for i in range(len(samples)):
        attack=min(1,i/(RATE*.0015)); release=min(1,(len(samples)-1-i)/(RATE*.012))
        samples[i]=(samples[i]-mean)*(.5-.5*math.cos(math.pi*attack))*(.5-.5*math.cos(math.pi*release))
    peak=max(map(abs,samples))
    # Match short-term energy, with a hard transient ceiling; no clipping/brickwall limiter.
    active=[x for x in samples if abs(x)>peak*.04]
    rms=math.sqrt(sum(x*x for x in active)/len(active))
    gain=min(.16/rms,.70/peak)
    return [x*gain for x in samples]


def wav(path, samples):
    pcm=array.array('h',(round(max(-1,min(1,x))*32767) for x in samples))
    if sys.byteorder!='little': pcm.byteswap()
    path.parent.mkdir(parents=True,exist_ok=True)
    with wave.open(str(path),'wb') as f:
        f.setparams((1,2,RATE,0,'NONE','not compressed')); f.writeframes(pcm.tobytes())
    with wave.open(str(path),'rb') as f:
        decoded=array.array('h',f.readframes(f.getnframes()))
    if sys.byteorder!='little': decoded.byteswap()
    assert max(map(abs,decoded))<23500 and decoded[0]==decoded[-1]==0, path
    return dict(seconds=round(len(samples)/RATE,4),peak=round(max(map(abs,decoded))/32768,5),
                sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def build():
    PUBLIC.mkdir(parents=True,exist_ok=True)
    sources={key:trim(decode(FRONTLINES/file,start,end)) for key,(file,start,end) in CUTS.items()}
    sources['oldShot']=trim(decode(ROOT/'Assets/Resources/Sounds/shot1.wav'))
    manifest=dict(schemaVersion=1,bankVersion='sample-round-1',sampleRate=RATE,cues=[])
    records=[]
    for key,(file,start,end) in CUTS.items():
        records.append(dict(id=key,project='Frontlines',path=file,start=start,end=end,
                            sha256=hashlib.sha256((FRONTLINES/file).read_bytes()).hexdigest()))
    for cue_id,label,group,family,pitch,duration,game_gain,original in CUES:
        cue=dict(id=cue_id,label=label,group=group,family=family,gameGain=game_gain,
                 original=None,choices=[])
        if original:
            path=ROOT/'Assets/Resources/Sounds'/f'{original}.wav'
            audio=decode(path)
            cue['original']={'file':f'audio/{cue_id}/original.wav','label':'Current sound',
                             **wav(PUBLIC/f'audio/{cue_id}/original.wav',finish(audio,len(audio)/RATE,8500))}
        else:
            cue['originalLabel']='Currently silent'
        for n,(name,description,layers) in enumerate(RECIPES[family]):
            mixed=[0.] * round((duration+.02)*RATE)
            for source,tune,gain,delay,reverse in layers:
                material=sources[source]
                if reverse: material=material[::-1]
                material=resample(material,tune*pitch)
                offset=round(delay*RATE)
                for i in range(min(len(material),len(mixed)-offset)):
                    mixed[offset+i]+=material[i]*gain
            cutoff=[6500,4800,3800,5700,2600][n]
            audio=finish(mixed,duration,cutoff)
            # A tiny, soft reflection on selected material choices, no fake digital buzzing.
            choice_id=chr(97+n)
            file=f'audio/{cue_id}/{choice_id}.wav'
            metrics=wav(PUBLIC/file,audio)
            cue['choices'].append(dict(id=choice_id,label=name,description=description,file=file,
                                       recipe=layers,**metrics))
        assert len({c['sha256'] for c in cue['choices']})==5, cue_id
        manifest['cues'].append(cue)
    (PUBLIC/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    (HERE/'source-provenance.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
    (HERE/'SOURCE-LICENSES.txt').write_text((FRONTLINES/'LICENSES.txt').read_text(encoding='utf-8'),encoding='utf-8')
    print(f'Built {len(CUES)*5} alternatives for {len(CUES)} effects. Unity assets unchanged.')


if __name__=='__main__': build()
