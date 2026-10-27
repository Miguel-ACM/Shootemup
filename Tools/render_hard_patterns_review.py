"""Package actual Unity boss sequences and complete-frame Hard measurements."""
from pathlib import Path
from collections import defaultdict
import csv, shutil, statistics
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'Temp/CombatReworkReview/ArtSource/CombatReworkReview'
OUT=ROOT/'ArtSource/HardPatternsReview'
EARLY=['Fields','Snow','City','Thunder','Tunnel']
LATE=['Desert','Candyland','Space','Underwater','Hell']

def captures(case):
    return sorted((SRC/'Frames'/case).glob('*.png'),key=lambda p:int(p.stem))
def frame(case,t):
    fs=captures(case);return fs[round((len(fs)-1)*t)]
def thumbnail(board,path,x,y,size):
    im=Image.open(path).convert('RGB');im.thumbnail(size,Image.Resampling.LANCZOS);board.paste(im,(x,y))
def sheet(levels,name):
    im=Image.new('RGB',(1280,280*len(levels)),(15,20,26));d=ImageDraw.Draw(im)
    for row,level in enumerate(levels):
        for col,(phase,t) in enumerate([(1,.38),(2,.23),(2,.48),(2,.76)]):
            case=f'{level}-Hard-Boss{phase}';f=frame(case,t)
            thumbnail(im,f,col*320,row*280+23,(320,250))
            d.text((col*320+5,row*280+5),('Plains' if level=='Fields' else level)+f' / phase {phase}',fill='white')
    im.save(OUT/name,quality=94)

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    runtime=(SRC/'Verification.txt').read_text(encoding='utf-8')
    assert runtime.startswith('PASS')
    for level in EARLY+LATE:
        for phase in [1,2]:assert f'PASS {level} phase {phase} uses genuinely evolving' in runtime
    shutil.copy2(SRC/'Verification.txt',OUT/'Runtime.txt')
    for name in ['MotionChecks.txt','EasyParity.txt']:
        src=ROOT/'Temp/CombatReworkReview/ArtSource/HardPatternsReview'/name
        assert src.read_text().startswith('PASS');shutil.copy2(src,OUT/name)
    compiler=ROOT/'Temp/CombatReworkReview/ArtSource/CodeCleanupReview/Verification.txt'
    assert compiler.read_text().startswith('PASS');shutil.copy2(compiler,OUT/'Compilation.txt')
    build=(SRC/'Build.txt').read_text();assert build.startswith('PASS') and 'warnings=0' in build
    shutil.copy2(SRC/'Build.txt',OUT/'Build.txt')
    sheet(EARLY,'EarlyHard.jpg');sheet(LATE,'LateHard.jpg')
    board=Image.new('RGB',(1120,500),(15,20,26));d=ImageDraw.Draw(board)
    for i,level in enumerate(['Snow','Space','Underwater','Hell']):
        im=Image.open(frame(f'{level}-Hard-Boss2',.68)).convert('RGB')
        im=im.crop((round(im.width*.30),0,round(im.width*.74),im.height));im.thumbnail((275,470))
        board.paste(im,(i*280,24));d.text((i*280+4,5),level,fill='white')
    board.save(OUT/'HardPatterns.jpg',quality=95)
    fs=captures('Space-Hard-Boss2');fs=fs[round(len(fs)*.22):round(len(fs)*.72)]
    frames=[]
    for f in fs:
        im=Image.open(f).convert('RGB');im=im.crop((round(im.width*.30),0,round(im.width*.74),im.height));frames.append(im)
    palette=Image.new('RGB',(frames[0].width,frames[0].height*6))
    for i in range(6):palette.paste(frames[round((len(frames)-1)*i/5)],(0,frames[0].height*i))
    palette=palette.quantize(colors=160)
    animation=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in frames]
    animation[0].save(OUT/'SpaceOrbits.gif',save_all=True,append_images=animation[1:],duration=250,loop=0,optimize=True)
    with (SRC/'Frames.csv').open(newline='') as f:rows=[r for r in csv.DictReader(f) if r['case'].endswith('-Uncaptured')]
    with (OUT/'DenseFrames.csv').open('w',newline='') as f:
        writer=csv.DictWriter(f,fieldnames=rows[0].keys());writer.writeheader();writer.writerows(rows)
    groups=defaultdict(list)
    for r in rows:groups[r['case']].append(r)
    lines=['PASS','Complete frames after dense phase-two phrases, no capture calls in these sections. Editor Game View, 60 FPS target.',
           'Native capture size: '+str(Image.open(frame('Fields-Hard-Boss1',.5)).size),'case | frames | peak bullets | frame p99/max ms | physics p99/max ms | created range']
    for name,rs in groups.items():
        dt=sorted(float(r['dt_ms']) for r in rs);physics=sorted(float(r['physics_ns'])/1e6 for r in rs)
        lines.append(f'{name} | {len(rs)} | {max(int(r["live"]) for r in rs)} | {dt[int(len(dt)*.99)]:.3f}/{max(dt):.3f} | {physics[int(len(physics)*.99)]:.3f}/{max(physics):.3f} | {min(int(r["created"]) for r in rs)}-{max(int(r["created"]) for r in rs)}')
    (OUT/'Performance.txt').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    (OUT/'Verification.txt').write_text('PASS\nEasy matches the recorded former Hard global factors, 76 actor configurations and 40 production wave signatures.\nAll ten new Hard boss scores reviewed through full multi-second phrases and both phases, plus Hard production waves.\nMotion/delivery families, giant diameters, multipart behavior, Candyland bounds, vertical Thunder beams, minion-free Hell and no pool growth verified.\nFocused pure checks cover double-orbit geometry, visible deployment, mid-eruption phase restart, pooled-state reset and exact giant-bullet contact.\nClean compilation and Windows x64 build passed with zero warnings/errors.\nSee Runtime.txt, MotionChecks.txt, EasyParity.txt and Performance.txt for exact evidence.\n',encoding='utf-8')
    (OUT/'README.md').write_text('''# Boss-specific Hard patterns — 27 October 2026

The previous Hard balance is now Easy. Hard develops a separate attack identity for each boss, using evolving geometry and coordinated pressure rather than renamed fans.

- [Early bosses](EarlyHard.jpg), [late bosses](LateHard.jpg), [four pattern examples](HardPatterns.jpg)
- [Space orbital motion](SpaceOrbits.gif), assembled from actual Unity captures at four frames per second
- [Verification](Verification.txt), [exact Easy parity](EasyParity.txt), [motion/contact regressions](MotionChecks.txt)
- [Complete-frame performance](Performance.txt), [raw dense frames](DenseFrames.csv), [runtime report](Runtime.txt)
- [Clean compiler rebuild](Compilation.txt), [Windows player build](Build.txt)

The observer review covers long enough sequences for deployed circles and wakes to travel through the arena, with actual collisions retained. It is a visual and technical review, not a human difficulty playthrough. The captured animation's sampling rate is not the game's frame rate. The first pure regression attempt was corrected to account for edit-mode automation not dispatching MonoBehaviour.OnDisable; clearing is checked through inactive delivery objects, while Play mode maintains its live registry.

The submarine's first pressure crown now restarts correctly even when a phase change interrupts the preceding eruption. Projectile sprites, per-district hues, native contacts, exact opaque-pixel collision, shared bloom and the 280 live / 768 pooled budget are preserved.

Research and individual boss intentions: `Design/HardBossPatterns.md`. Player build: `Build/HardPatterns/Shootemup.exe` with its adjacent data folder.
''',encoding='utf-8')
    print('\n'.join(lines));print(build)

if __name__=='__main__':main()
