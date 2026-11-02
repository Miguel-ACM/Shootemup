"""Draw the five drydock workstations at their native pixel scale."""
from pathlib import Path
from PIL import Image, ImageDraw
import math

out = Path(__file__).resolve().parents[1] / 'Assets/Resources/DockArt'
out.mkdir(exist_ok=True)
ink, deep, iron, edge, silver = '#0c1923','#1b303a','#37505a','#67808a','#adbdba'
brass, ochre, ivory = '#f1b45d','#a06c3d','#fff0b2'
for station, glow in zip(('Fleet','Armory','Simulator','Recorder','Flight'),('#82d9c7',brass,'#be9de4','#8fc7ec','#ffab7f')):
    image=Image.new('RGBA',(80,62))
    d=ImageDraw.Draw(image)
    def line(points,color,width=1): d.line(points,fill=color,width=width)
    def rect(box,color): d.rectangle(box,fill=color)
    def poly(points,color,outline=None): d.polygon(points,fill=color,outline=outline)
    def chassis(x,y,w,h,face):
        poly([(x+4,y),(x+w-4,y),(x+w,y+4),(x+w,y+h-3),(x+w-4,y+h+1),(x+3,y+h+1),(x,y+h-3),(x,y+4)],face,ink)
        line([(x+1,y+5),(x+5,y+1),(x+w-5,y+1)],edge)
        line([(x+4,y+h),(x+w-4,y+h),(x+w-1,y+h-3)],deep,2)
    def bolt(x,y):
        rect((x,y,x+2,y+2),ink); d.point((x,y),silver)
    def reel(x,y,r):
        d.ellipse((x-r,y-r+2,x+r,y+r+2),fill=ink)
        d.ellipse((x-r,y-r,x+r,y+r),fill=edge,outline=silver)
        for a in range(0,360,72):
            dx,dy=round(math.cos(math.radians(a))*r*.55),round(math.sin(math.radians(a))*r*.55)
            d.ellipse((x+dx-2,y+dy-2,x+dx+2,y+dy+2),fill=deep)
        rect((x-1,y-1,x+1,y+1),glow)
    chassis(1,2,76,58,iron)
    chassis(4,4,70,48,deep)
    for x in range(9,72,9):
        line([(x,6),(x-3,11)],iron,2)
    for x,y in ((5,5),(71,5),(5,54),(71,54)): bolt(x,y)
    rect((12,46,67,55),ink)
    line([(17,57),(60,57)],edge)
    line([(10,3),(36,3)],glow)
    if station=='Fleet':
        for x in (12,59):
            chassis(x,9,9,34,iron)
            for y in (15,24,33):
                line([(x+1,y+3),(x+6,y-2)],ochre,3)
                line([(x+1,y+2),(x+5,y-2)],brass)
        for x in (26,49):
            line([(x,14),(x-3,18),(x-3,37),(x,40)],edge)
            line([(x+1,14),(x+1,39)],iron)
        line([(19,10),(58,10)],silver,2)
        rect((30,8,39,13),ochre);rect((31,8,38,10),brass)
    elif station=='Armory':
        for x in (10,33,54):
            chassis(x,9,15,31,'#495655')
            rect((x+2,14,x+13,36),ink)
        # Long finned missile, brass ammunition drums and a rack of cold steel.
        poly([(15,33),(15,18),(18,12),(20,18),(20,33),(23,37),(13,37)],'#a39e83',ink)
        rect((16,21,19,24),'#cf8151');line([(16,17),(16,30)],ivory)
        for y in (17,26,34):
            rect((36,y-2,46,y+2),ochre);rect((37,y-2,44,y-1),brass);rect((45,y-2,47,y+1),silver)
        for x in (57,62,66):
            line([(x,34),(x+2,16)],silver,2);line([(x,35),(x-1,38)],ochre,2)
        rect((10,40,69,43),edge);rect((11,40,67,40),silver)
    elif station=='Simulator':
        for dx in (-1,1):
            x=40+dx*20
            poly([(x,15),(x+dx*7,18),(x+dx*11,35),(x+dx*5,39),(x,31)],iron,ink)
            line([(x+dx*3,19),(x+dx*8,32)],glow,2)
        poly([(29,9),(49,9),(56,17),(56,34),(49,42),(28,42),(21,35),(21,18)],'#746c87',ink)
        poly([(30,11),(47,11),(53,17),(51,29),(27,29),(24,21)],'#29384e',silver)
        poly([(30,13),(43,13),(33,25),(27,25)],'#54617c')
        line([(29,22),(37,18),(41,20),(47,16)],glow)
        rect((27,32,51,37),ink)
        for x in range(30,49,4): rect((x,34,x+1,35),glow)
    elif station=='Recorder':
        chassis(9,10,60,32,'#6d706a')
        rect((13,13,65,32),deep)
        line([(16,23),(24,36),(50,36),(60,25)],ink,3)
        line([(16,22),(24,35),(50,35),(60,24)],ochre)
        reel(26,21,9);reel(52,21,9)
        rect((25,36,46,38),deep)
        for x in (51,56,61):rect((x,36,x+2,37),glow)
    else:
        chassis(10,10,43,26,iron);rect((14,13,49,31),'#123c40')
        for x in range(17,48,6):line([(x,15),(x,29)],'#1b5250')
        line([(17,27),(25,22),(32,24),(43,16)],'#8fe3bd',2)
        chassis(54,13,13,24,iron)
        for y in (18,23,28):rect((57,y,64,y+1),glow)
        chassis(15,36,49,7,edge)
        for x in range(19,53,5):rect((x,38,x+2,39),deep)
        rect((55,38,60,40),brass)
    image.save(out/(station+'.png'))
# Pilot's wings struck onto the final flight receipt.
image=Image.new('RGBA',(96,48))
d=ImageDraw.Draw(image)
for side in (-1,1):
    for tier,(reach,y) in enumerate(((43,11),(36,17),(29,23))):
        poly([(48+side*12,y+3),(48+side*reach,y-4),(48+side*(reach-5),y+3),
              (48+side*17,y+13)], ochre,ink)
        line([(48+side*16,y+3),(48+side*(reach-2),y-3)],ivory)
        line([(48+side*17,y+6),(48+side*(reach-9),y+1)],brass)
poly([(34,8),(48,3),(62,8),(61,25),(56,32),(48,38),(40,32),(35,25)],ochre,ink)
poly([(36,9),(48,5),(60,9),(59,24),(54,31),(48,35),(41,30),(37,24)],brass,ivory)
poly([(39,11),(48,8),(57,11),(56,23),(48,31),(40,24)],deep,ink)
poly([(48,10),(51,19),(55,23),(50,22),(48,27),(46,22),(41,23),(46,17)],ivory)
image.save(out/'Wings.png')
# Squadron stamps, hand cut from a seven-column brass punch.
punches = {
'Fields':('...#...','..###..','#.###.#','#######','..###..','.##.##.','..#.#..'),
'Snow':('...#...','.#####.','#######','##.#.##','#######','.#####.','...#...'),
'City':('.#.....','###..#.','###.###','.#..###','....###','...##.##','....#.#'),
'Thunder':('....##.','...##..','..##...','.#####.','...##..','..##...','..#....'),
'Desert':('....##.','...####','...#..#','...###.','.###...','###....','.###...'),
'Tunnel':('..##...','#.####.','###..##','##...##','##..###','.####.#','...##..'),
'Candyland':('.......','##...##','#######','.#####.','#######','##...##','.......'),
'Space':('..###..','.##.##.','.##.###','###.###','###.##.','.##.##.','..###..'),
'Underwater':('...#...','..###..','..#.#..','.#####.','.#####.','###.###','..#.#..'),
'Hell':('#.....#','##...##','#.#.#.#','.#####.','##.#.##','.#...#.','##...##')}
for name,rows in punches.items():
    image=Image.new('RGBA',(9,9));d=ImageDraw.Draw(image)
    for y,row in enumerate(rows):
        for x,ch in enumerate(row):
            if ch=='#':d.point((x+1,y+1),fill='white')
    image.save(out/('Stamp'+name+'.png'))
