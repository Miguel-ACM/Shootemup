"""Make true-time review clips from Unity captures, preserving short HUD dropouts."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import csv
import statistics
import subprocess

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/CombatReview/Polish'
OUT.mkdir(parents=True,exist_ok=True)
FONT=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',16)

def render(source,label):
    if not (source/'timing.csv').exists():return
    rows=list(csv.DictReader((source/'timing.csv').open()))
    frames=[source/(f"{int(row['frame']):04}.jpg") for row in rows]
    times=[float(row['seconds']) for row in rows]
    if len(frames)<2 or not all(p.exists() for p in frames):return
    intervals=[b-a for a,b in zip(times,times[1:])]
    manifest=source/'video.ffconcat'
    lines=['ffconcat version 1.0']
    for path,duration in zip(frames,intervals+[statistics.median(intervals)]):
        lines += ["file '"+path.as_posix()+"'",f'duration {max(.001,duration):.6f}']
    lines += ["file '"+frames[-1].as_posix()+"'"]
    manifest.write_text('\n'.join(lines)+'\n')
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-safe','0','-i',str(manifest),
        '-vf','crop=min(iw\\,trunc(ih*1.1/2)*2):ih:(iw-ow)/2:0,scale=-2:720',
        '-fps_mode','vfr','-c:v','libx264','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(OUT/(label+'.mp4'))],check=True)
    if label=='HudStartup':
        desired=[0,.35,.55,.82,1.18,1.65,2.2,3.4]
    else:desired=[times[-1]*f for f in [.02,.20,.39,.53,.72,.93]]
    columns=4 if label=='HudStartup' else 3
    canvas=Image.new('RGB',(columns*396,2*388+40),(10,15,20));draw=ImageDraw.Draw(canvas)
    draw.text((12,10),label+' / actual Unity capture',font=FONT,fill=(220,230,235))
    for j,t in enumerate(desired):
        index=min(range(len(times)),key=lambda i:abs(times[i]-t))
        image=Image.open(frames[index]);w,h=image.size;span=min(w,h*1.1)
        image=image.crop(((w-span)/2,0,(w+span)/2,h)).resize((396,360))
        x=(j%columns)*396;y=40+(j//columns)*388
        draw.text((x+8,y+4),f'{times[index]:.2f}s',font=FONT,fill=(153,201,213));canvas.paste(image,(x,y+28))
    canvas.save(OUT/(label+'.jpg'),quality=91)
    print(label,len(frames),'frames',f'{times[-1]:.2f}s')

render(ROOT/'Temp/HudStartupReview','HudStartup')
render(ROOT/'Temp/TitleHudStartupReview','TitleHudStartup')
for folder in sorted((ROOT/'Temp/PolishReview').glob('*')):
    if folder.is_dir():render(folder,folder.name)
