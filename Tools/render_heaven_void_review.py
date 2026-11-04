"""Compose native Unity review captures without altering the shipping artwork."""
from pathlib import Path
from PIL import Image, ImageDraw
root=Path(__file__).resolve().parents[1]/'ArtSource/HeavenVoidReview'
def sheet(files,out,columns=4,tile=(300,330),crop=False):
    if not files:return
    canvas=Image.new('RGB',(columns*tile[0],((len(files)+columns-1)//columns)*tile[1]),'#13151d')
    draw=ImageDraw.Draw(canvas)
    for i,p in enumerate(files):
        img=Image.open(p).convert('RGB')
        if crop:
            w,h=img.size;span=int(h*405/720);x=(w-span)//2
            img=img.crop((x,0,x+span,h))
        img.thumbnail((tile[0]-12,tile[1]-35))
        x=(i%columns)*tile[0]+(tile[0]-img.width)//2;y=(i//columns)*tile[1]
        canvas.paste(img,(x,y));draw.text(((i%columns)*tile[0]+8,y+tile[1]-27),p.stem if not crop else p.parent.name,fill='#ece4d0')
    canvas.save(root/out)
sheet(sorted((root/'Models').glob('*-0.png')),'Models.jpg')
files=[]
for folder in sorted((root/'Frames').glob('*')):
    frames=sorted(folder.glob('*.jpg'))
    if frames:files.append(frames[min(len(frames)-1,int(len(frames)*.58))])
sheet(files,'Gameplay.jpg',4,(240,450),True)
