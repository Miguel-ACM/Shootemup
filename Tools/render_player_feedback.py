"""Package real Unity feedback captures as motion clips and review contact sheets."""
from pathlib import Path
import argparse
import shutil
import subprocess
from PIL import Image, ImageDraw

parser=argparse.ArgumentParser()
parser.add_argument('source',type=Path)
parser.add_argument('destination',type=Path)
args=parser.parse_args()
args.destination.mkdir(parents=True,exist_ok=True)
for image_path in args.source.glob('*'):
    if image_path.is_file():
        shutil.copy2(image_path,args.destination/image_path.name)
for directory in args.source.iterdir():
    if not directory.is_dir():
        continue
    frames=sorted(directory.glob('????.jpg'))
    if not frames:
        continue
    count=min(8,len(frames))
    sheet=Image.new('RGB',(4*320,2*430),(9,13,18))
    draw=ImageDraw.Draw(sheet)
    for slot in range(count):
        index=round((len(frames)-1)*slot/max(1,count-1))
        image=Image.open(frames[index]).convert('RGB')
        w,h=image.size
        # Include the real arena and its immediate HUD margins at readable scale.
        half=min(w/2,h*.43)
        image=image.crop((round(w/2-half),0,round(w/2+half),h))
        image.thumbnail((320,400))
        x=(slot%4)*320+(320-image.width)//2
        y=(slot//4)*430+24
        sheet.paste(image,(x,y))
        draw.text(((slot%4)*320+8,(slot//4)*430+6),f'{directory.name}  {index/10:.1f}s',fill=(220,226,229))
    sheet.save(args.destination/(directory.name+'.jpg'),quality=91)
    subprocess.run(['C:/Program Files/ffmpeg/bin/ffmpeg.exe','-y','-loglevel','error','-framerate','10','-i',str(directory/'%04d.jpg'),'-vf','pad=ceil(iw/2)*2:ceil(ih/2)*2','-c:v','libx264','-crf','21','-pix_fmt','yuv420p','-movflags','+faststart',str(args.destination/(directory.name+'.mp4'))],check=True)
print(f'Packaged captures in {args.destination.resolve()}')
