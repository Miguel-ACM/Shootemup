"""Native pixels for the hangar pressure doors and hull service portraits."""
from pathlib import Path
from PIL import Image, ImageDraw
out=Path(__file__).resolve().parents[1]/'Assets/Resources/DockArt'
ink='#09161e'; deep='#142a33'; mid='#2d4650'; steel='#536c72'; edge='#849993'; pale='#c6d4c3'; gold='#e2af62'
# Workshop floors are built in game from the hangar lane's own plate colours.

# Price token. The three strokes on its face echo the modeled money pickup.
coin = Image.new('RGBA', (16,16))
mint = ImageDraw.Draw(coin)
mint.polygon([(4,0),(10,0),(13,2),(15,5),(15,10),(12,14),(9,15),(4,15),(1,12),(0,9),(0,5),(2,2)], fill='#422c24')
mint.polygon([(5,1),(10,1),(13,3),(14,5),(14,10),(11,13),(9,14),(5,14),(2,11)], fill='#a46331')
mint.polygon([(4,1),(9,1),(12,4),(12,9),(9,12),(4,12),(1,9),(1,5)], fill='#e6a547')
mint.line([(1,7),(1,5),(4,2),(8,1),(9,1)], fill='#fff1a3')
mint.line([(4,12),(8,12),(12,8)], fill='#f5ce70')
mint.line([(4,4),(6,3),(9,3)], fill='#f8ca68')
mint.line([(8,4),(5,4),(4,5),(4,7),(8,7),(8,9),(7,10),(4,10)], fill='#805023')
mint.line([(5,5),(7,5)], fill='#ffe798')
mint.line([(5,8),(7,8)], fill='#feda84')
for x,y in ((13,7),(13,10),(11,12)):
    mint.point((x,y), fill='#df9d45')
coin.save(out/'DockCoin.png')
# Alternating steel shutter slats, fixed highlights and worn brass latch.
im=Image.new('RGBA',(128,128),deep);d=ImageDraw.Draw(im)
for y in range(0,128,16):
 d.rectangle((0,y,127,y+12),fill=mid)
 d.line((0,y,127,y),fill=steel)
 d.line((0,y+13,127,y+13),fill=ink,width=3)
 for x in (8,116):
  d.rectangle((x,y+4,x+3,y+7),fill=ink);d.point((x,y+4),fill=edge)
for x in (0,121):
 d.rectangle((x,0,x+6,127),fill=ink)
 d.line((x+1,0,x+1,127),fill=steel)
d.rectangle((54,53,74,75),fill=ink,outline=gold)
d.rectangle((58,59,70,69),fill=gold)
im.save(out/'PressureDoor.png')
# Four individually outlined hulls, light from upper left. All edges stay on native pixels.
for kind in ('GR07','Lancer','Ember','Warden'):
 im=Image.new('RGBA',(64,80));d=ImageDraw.Draw(im)
 def poly(points,fill,outline=ink):d.polygon(points,fill=fill,outline=outline)
 def line(points,fill,width=1):d.line(points,fill=fill,width=width)
 def box(rect,fill,outline=None):d.rectangle(rect,fill=fill,outline=outline)
 # Paint the small offset cast shadow before the hull.
 if kind=='GR07':
  poly([(32,8),(39,28),(48,33),(55,56),(44,61),(39,53),(32,65),(25,53),(20,61),(9,56),(16,33),(25,28)],'#192d36')
  poly([(31,5),(37,25),(46,30),(53,53),(43,58),(38,49),(31,62),(24,49),(19,58),(7,53),(14,30),(24,25)],'#586a7c')
  poly([(31,8),(34,31),(31,50),(26,31)],'#a9bdc0')
  poly([(15,31),(23,29),(19,49),(11,51)],'#879bab')
  poly([(39,29),(45,32),(49,51),(42,48)],'#334e70')
  poly([(31,20),(36,33),(34,40),(28,40),(25,33)],'#0d2436')
  poly([(30,23),(33,30),(31,35),(27,32)],'#76ccda')
  for x in (17,41):
   box((x,43,x+4,58),'#263f54',ink);box((x,55,x+4,58),gold);box((x+1,58,x+3,63),'#b86537')
  for x in (21,38):box((x,28,x+3,39),steel,ink);box((x+1,26,x+2,31),pale)
  line([(31,6),(24,26),(15,31)],pale)
 elif kind=='Lancer':
  poly([(32,2),(39,25),(44,42),(51,63),(39,65),(34,57),(29,70),(23,63),(12,65),(20,43),(25,22)],'#3d426f')
  poly([(31,3),(35,29),(33,52),(28,65),(26,42)],'#9894bf')
  poly([(24,29),(19,48),(17,60),(23,59),(28,39)],'#5c61a0')
  poly([(37,29),(42,47),(47,61),(40,58),(34,39)],'#242e55')
  poly([(31,21),(35,33),(32,41),(28,40),(27,32)],'#e56d1c')
  line([(29,29),(31,25),(33,32),(31,36)],'#ffd090',2)
  for x in (19,39):
   box((x,42,x+4,64),'#202e43',ink);box((x+1,46,x+2,55),'#82a4b8');box((x+1,62,x+3,65),gold)
  line([(31,4),(26,24),(23,34)],'#ded6e1')
  for y in (44,48,52):line([(28,y),(34,y)],'#4d5478')
 elif kind=='Ember':
  poly([(31,8),(38,28),(49,31),(58,56),(47,54),(39,47),(32,61),(25,48),(14,55),(5,56),(14,31),(24,27)],'#8b3c43')
  poly([(31,10),(35,30),(31,54),(26,30)],'#d29479')
  poly([(15,33),(23,31),(19,45),(10,50)],'#bd6256')
  poly([(40,32),(48,35),(54,50),(42,43)],'#562f40')
  poly([(31,23),(36,34),(33,42),(28,42),(25,34)],'#30232b')
  poly([(30,26),(33,31),(30,37),(27,33)],'#f4d77c')
  for x in (16,42):
   box((x,43,x+4,58),'#4a2b36',ink);box((x+1,54,x+3,58),'#ffcf81');box((x+1,59,x+3,64),'#b55335')
  for x in (22,37):box((x,28,x+3,38),'#ba987e',ink)
  line([(31,11),(24,28),(15,33)],'#edb09a')
 else:
  poly([(32,14),(41,24),(48,19),(53,9),(58,24),(57,53),(47,64),(38,54),(32,61),(26,54),(16,64),(7,54),(6,25),(11,9),(17,20),(22,24)],'#4a6658')
  poly([(11,13),(14,25),(12,49),(18,57),(23,46),(22,27)],'#8d9f7d')
  poly([(50,14),(48,29),(51,49),(45,56),(39,45),(41,27)],'#314d49')
  poly([(31,17),(37,27),(36,44),(31,54),(26,44),(25,27)],'#9da889')
  poly([(30,26),(35,33),(33,39),(29,39),(26,33)],'#122f38')
  line([(29,30),(31,27),(33,32),(30,36)],'#91d2d1',2)
  for x in (9,48):
   for y in (31,38,45):box((x,y,x+5,y+3),'#254047',ink);line([(x+1,y),(x+4,y)],'#a9b896')
  for x in (19,41):box((x,50,x+4,61),'#263d3c',ink);box((x+1,58,x+3,62),gold)
 im.save(out/('Portrait'+kind+'.png'))

# Input housings. The current action binding is printed by the game.
for name in ('KeyPrompt','PadPrompt'):
    im=Image.new('RGBA',(48,32))
    d=ImageDraw.Draw(im)
    if name=='KeyPrompt':
        d.rounded_rectangle((6,3,41,29),radius=3,fill=ink,outline=edge)
        d.rounded_rectangle((7,2,40,24),radius=3,fill='#dfebd7',outline=steel)
        d.line((10,4,37,4),fill='#ffffff')
    else:
        d.polygon([(2,16),(7,9),(23,5),(39,3),(45,6),(47,14),(41,23),(17,28),(5,27)],fill=ink,outline=edge)
        d.polygon([(4,16),(9,10),(25,6),(39,5),(44,8),(44,14),(39,20),(15,25),(5,24)],fill='#dedfbd')
        d.line([(9,12),(24,8),(38,6)],fill='#ffffff',width=2)
    im.save(out/(name+'.png'))

# A round face button; the input legend is drawn by the game.
im=Image.new('RGBA',(48,32))
d=ImageDraw.Draw(im)
d.ellipse((9,2,38,31), fill=ink, outline=edge)
d.ellipse((10,1,37,27), fill='#c8dbda', outline=steel)
d.arc((12,3,35,25), 195, 295, fill='#f6fcf0', width=2)
im.save(out/'FacePrompt.png')
