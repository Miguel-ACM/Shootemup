"""Inspect the actual sampled bullet coordinates; diagrams, not gameplay captures."""
from pathlib import Path
import csv, math
from collections import defaultdict
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1]/'ArtSource/BossPass'

def plot(rows,title):
    scale=27;w=282;h=486
    image=Image.new('RGB',(w,h),'#101420');d=ImageDraw.Draw(image)
    def point(x,y):return (w/2+float(x)*scale,28+(7.87-float(y))*scale)
    color='#c18ff1' if title.startswith('Void') else '#72bee3'
    for row in rows:
        x,y=point(row['x'],row['y']);kind=row['type']
        if kind=='PulseLaser':continue # Sprite bounds do not describe the separate beam ribbon.
        angle=math.radians(float(row['angle']));width=float(row['width'])*scale;height=float(row['height'])*scale
        if 'Needle' in kind:
            length=max(3,min(8,max(width,height)*.42));half=max(1.2,min(2.4,min(width,height)*.2))
            def p(a,b):return (x+a*math.cos(angle)-b*math.sin(angle),y-a*math.sin(angle)-b*math.cos(angle))
            d.polygon([p(0,length),p(-half,-length*.6),p(0,-length*.25),p(half,-length*.6)],fill=color)
        else:
            radius=max(1.5,min(9,max(width,height)*.32));d.ellipse((x-radius,y-radius,x+radius,y+radius),fill=color,outline='#e7d8fa')
    if rows:
        x,y=point(rows[0]['bossX'],rows[0]['bossY']);d.polygon([(x,y-8),(x+8,y),(x,y+8),(x-8,y)],fill='#f8d985')
    d.rectangle((0,0,w,24),fill='#191f2d');d.text((8,7),title,fill='#ffffff')
    return image

tiles=[]
for file in sorted((ROOT/'Patterns').glob('*.csv')):
    frames=defaultdict(list)
    for row in csv.DictReader(file.open()):frames[int(row['frame'])].append(row)
    for card in (0,1):
        candidates=[(key,rows) for key,rows in frames.items() if int(rows[0]['card'])==card]
        if not candidates:continue
        # Middle-heavy frame, without choosing the maximum possible congestion.
        candidates.sort(key=lambda item:len(item[1]));key,rows=candidates[int((len(candidates)-1)*.66)]
        tiles.append(plot(rows,f'{file.stem} / card {card+1}'))
    animation=[plot(rows,file.stem).quantize(colors=96) for key,rows in sorted(frames.items())]
    if animation:animation[0].save(ROOT/(file.stem+'-paths.gif'),save_all=True,append_images=animation[1:],duration=500,loop=0)
if tiles:
    sheet=Image.new('RGB',(282*4,486*math.ceil(len(tiles)/4)),'#111722')
    for i,tile in enumerate(tiles):sheet.paste(tile,((i%4)*282,(i//4)*486))
    sheet.save(ROOT/'PatternGeometry.jpg',quality=95)

models=[]
for path in sorted((ROOT/'Renders').glob('*-0.png')):
    image=Image.open(path).convert('RGB');image.thumbnail((300,300));tile=Image.new('RGB',(310,330),'#1b2230');tile.paste(image,((310-image.width)//2,0));ImageDraw.Draw(tile).text((7,312),path.stem,fill='white');models.append(tile)
if models:
    sheet=Image.new('RGB',(310*4,330*math.ceil(len(models)/4)),'#1b2230')
    for i,tile in enumerate(models):sheet.paste(tile,((i%4)*310,(i//4)*330))
    sheet.save(ROOT/'Models.jpg',quality=95)

selection=['Heaven-Final','Void-Final-1','Void-Final-2','Void-Final-3','Void-Final-4','PlainsRedKiteBoss-0','PlainsThreshingDrumBoss-0','CitySwitchyardBoss-0','SnowIcebreakerBoss-0']
sheet=Image.new('RGB',(900,942),'#1b2230');draw=ImageDraw.Draw(sheet)
for i,name in enumerate(selection):
    path=ROOT/'Renders'/(name+'.png')
    if not path.exists():continue
    image=Image.open(path).convert('RGB');image.thumbnail((296,286));x=(i%3)*300;y=(i//3)*314
    sheet.paste(image,(x+(300-image.width)//2,y));draw.text((x+9,y+292),name.replace('-Final','').replace('Boss-0','').replace('Plains','').replace('City','').replace('Snow',''),fill='#f2e7d0')
sheet.save(ROOT/'BossVisuals.jpg',quality=96)
