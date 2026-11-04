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
    if folder.name.startswith('Heaven-') and not folder.name.startswith('Heaven-Revised-'):continue
    if folder.name.startswith(('Void-Normal-','Void-Easy-','Void-Hard-')):continue
    frames=sorted(folder.glob('*.jpg'))
    if frames:files.append(frames[min(len(frames)-1,int(len(frames)*.58))])
sheet(files,'Gameplay.jpg',4,(240,450),True)

def animation(name,clips):
    output=[]
    for clip,start,end in clips:
        frames=sorted((root/'Frames'/clip).glob('*.jpg'))[start:end]
        for p in frames:
            image=Image.open(p).convert('RGB');w,h=image.size;span=round(h*405/720);left=(w-span)//2
            image=image.crop((left,0,left+span,h)).resize((270,480))
            output.append(image.quantize(colors=96))
    if output:output[0].save(root/name,save_all=True,append_images=output[1:],duration=200,loop=0,optimize=True)
animation('Heaven.gif',[('Heaven-Revised-Clouds-Wave',15,40),('Heaven-Revised-Lasers',10,35)])
animation('Void.gif',[(f'Void-Cues-{phase}',5,35) for phase in range(1,5)]+[(f'Void-Interlude-{phase}',20,35) for phase in range(1,4)])

# Keep a short motion storyboard for inspection alongside the native captures.
motion=[]
for clip in ['Heaven-Revised-Clouds-Wave','Heaven-Revised-Lasers','Void-Cues-4','Void-Interlude-3']:
    frames=sorted((root/'Frames'/clip).glob('*.jpg'))
    motion.extend(frames[i] for i in [10,20,30,40] if i<len(frames))
sheet(motion,'Motion.jpg',4,(240,450),True)
