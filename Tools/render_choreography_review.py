from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import subprocess,sys,shutil
root=Path(__file__).resolve().parents[1]
ffmpeg=shutil.which('ffmpeg')
if not ffmpeg:raise SystemExit('ffmpeg must be on PATH')
out=root/'ArtSource/CombatReview/Choreography'
out.mkdir(parents=True,exist_ok=True)
try:font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',18)
except OSError:font=ImageFont.load_default(size=18)
for name in ['Fields','Snow','Thunder','Tunnel','City']:
    source=root/'Temp/ChoreographyReview'/name
    if not (source/'timing.txt').exists():continue
    label='Plains' if name=='Fields' else name
    clip=out/(label+'.mp4')
    if clip.exists() and name not in sys.argv[1:]:continue
    frames=sorted(source.glob('*.jpg'))
    if not frames:continue
    subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-framerate','6','-i',str(source/'%04d.jpg'),'-vf','crop=trunc(iw*0.56/2)*2:ih:trunc(iw*0.22/2)*2:0,scale=-2:720','-c:v','libx264','-crf','22','-pix_fmt','yuv420p','-movflags','+faststart',str(clip)],check=True)
    starts=[float(line.split(':')[1]) for line in (source/'timing.txt').read_text().splitlines()]
    for group in range(2):
        canvas=Image.new('RGB',(1200,4*555+44),(12,18,25));draw=ImageDraw.Draw(canvas)
        draw.text((12,12),label+(' / full formations' if group==0 else ' / base-gun combat'),font=font,fill=(220,230,244))
        for row,start in enumerate(starts[group*4:group*4+4]):
            for col,delay in enumerate([.8,1.6,2.4,3.2]):
                index=min(len(frames)-1,round((start+delay)*6))
                frame=Image.open(frames[index]);w,h=frame.size
                # Inspect the real combat field at a consistent size, without side margins.
                half=h*405/720/2
                frame=frame.crop((round(w/2-half),0,round(w/2+half),h)).resize((300,533))
                canvas.paste(frame,(col*300,44+row*555+22))
                draw.text((col*300+8,44+row*555),f'Wave {group*4+row+1} / +{delay:.1f}s',font=font,fill=(157,209,235))
        destination=out/(label+('Formations.jpg' if group==0 else 'Combat.jpg'))
        pending=destination.with_name(destination.stem+'.pending.jpg')
        canvas.save(pending,quality=90)
        pending.replace(destination)
    print(f'{label}: {len(frames)} captured frames, video and both temporal contact sheets')
