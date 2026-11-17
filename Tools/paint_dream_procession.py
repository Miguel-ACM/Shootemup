"""Paint the four joined panels of Dream's scrolling canvas. Coordinates are brush pixels."""
from pathlib import Path
from math import cos, sin, pi
import random
import numpy as np
from PIL import Image, ImageDraw

W, H = 512, 3200
rng = random.Random(4180)
canvas = Image.new("RGB", (W, H), "#384553")
draw = ImageDraw.Draw(canvas)
ink, blue, violet = "#333444", "#516674", "#64566d"
rose, rust, ochre, sand = "#a47776", "#925e5d", "#ae906e", "#c0ab92"
jade, chalk = "#78958e", "#cebaa2"


class Brush:
    def __init__(self, x, y, scale=1, flip=1, tilt=0):
        self.x, self.y, self.s, self.flip, self.angle = x, y, scale, flip, tilt

    def xy(self, pts):
        c, s = cos(self.angle), sin(self.angle)
        return [(self.x+self.s*(x*self.flip*c-y*s), self.y+self.s*(x*self.flip*s+y*c)) for x, y in pts]

    def shape(self, color, *pts):
        draw.polygon(self.xy(pts), fill=color)

    def line(self, color, width, *pts):
        draw.line(self.xy(pts), fill=color, width=max(1, round(width*self.s)), joint="curve")

    def oval(self, x, y, a, b, color):
        self.shape(color, *((x+a*cos(k*pi/20), y+b*sin(k*pi/20)) for k in range(40)))

    def curve(self, color, width, a, b, c, d):
        points = []
        for k in range(37):
            t = k/36
            points.append(tuple((1-t)**3*a[j]+3*(1-t)**2*t*b[j]+3*(1-t)*t*t*c[j]+t**3*d[j] for j in (0,1)))
        self.line(color, width, *points)


def eye(b, x, y, s):
    pts = [(x+s*u, y+s*v) for u,v in [(-23,1),(-9,-9),(4,-10),(22,-3),(10,6),(-4,9)]]
    b.shape(ink,*pts)
    b.shape(sand, *((x+.81*(u-x), y+.68*(v-y)) for u,v in pts))
    b.oval(x+1,y-1,5*s,7*s,ink)


def sleeper(b):
    # Two readings of the same head. The sharp profile is continuous with the folded arm.
    b.shape(ink,(-91,-65),(-48,-119),(29,-136),(79,-102),(91,-44),(67,-5),(61,48),(15,84),(-48,63),(-99,5))
    b.shape(rose,(-86,-62),(-44,-114),(25,-129),(49,-102),(27,-72),(-6,-52),(-31,20),(-69,39),(-96,1))
    b.shape(ochre,(-31,20),(-5,-55),(34,-78),(17,-15),(43,3),(23,12),(33,31),(19,63),(-21,75),(-65,46))
    b.shape(jade,(31,-79),(50,-103),(72,-96),(86,-44),(58,-12),(56,44),(22,69),(31,28),(23,12),(48,3),(23,-16))
    b.shape(blue,(-86,-65),(-46,-113),(23,-130),(-7,-102),(-58,-80),(-76,-29))
    for i in range(6):
        b.curve(violet,2,(-83+i*4,-48+i*2),(-58,-110+i*3),(-4,-85+i*2),(17+i*3,-122+i))
    b.curve(ink,3,(-58,-40),(-44,-19),(-29,-27),(-18,-43))
    for i in range(5):
        b.line(ink,2,(-50+i*6,-31),(-53+i*6,-25))
    eye(b,55,-47,.77)
    b.line(ink,3,(16,-58),(4,-11),(27,1),(11,8))
    b.shape(rust,(-7,31),(10,23),(24,26),(11,38))
    b.line(ink,2,(-8,31),(12,30),(24,26))
    b.shape(violet,(-99,31),(-57,58),(16,84),(109,33),(82,94),(-19,124),(-118,75))
    b.curve(rose,4,(-103,58),(-49,97),(19,120),(83,68))


def lute(b):
    # Bent neck, soundboard, binding, bridge and six single painted strings.
    outline=[(-29,-23),(-4,-52),(20,-47),(43,-81),(76,-166),(93,-162),(68,-66),(42,-15),(58,13),(61,45),(42,77),(7,89),(-32,80),(-53,55),(-54,30)]
    b.shape(ink,*outline)
    b.shape(ochre,*[(x*.93,y*.96) for x,y in outline])
    b.shape(rust,(-49,39),(-25,-20),(-4,-46),(15,-43),(0,8),(-20,74),(-40,66))
    b.shape(sand,(0,8),(16,-43),(38,-56),(26,-9),(51,26),(40,61),(3,80),(-17,73))
    b.oval(15,-6,17,20,ink)
    b.oval(15,-6,13,15,blue)
    b.shape(ink,(-17,48),(11,52),(10,59),(-20,54))
    b.line(ink,12,(21,-19),(57,-87),(82,-160))
    for i in range(6):
        b.line(chalk if i%2 else ochre,1,(-14+i*3,53),(16+i*1.5,-13),(78+i*1.2,-158))
        b.line(ink,3,(77+i%2*10,-139-i*4),(72+i%2*24,-143-i*4))
    for k in range(8):
        b.line(sand,1,(44+3*k,-65-9*k),(55+3*k,-61-9*k))


def horse(b):
    b.shape(ink,(-117,106),(-61,28),(-75,-49),(-53,-99),(-40,-57),(-14,-91),(-19,-49),(10,-30),(47,-18),(83,15),(77,42),(46,51),(14,33),(-16,42),(19,105),(78,142),(7,145),(-54,104))
    b.shape(blue,(-110,103),(-56,28),(-68,-38),(-51,-82),(-45,-40),(-18,-74),(-26,-40),(-2,-13),(-29,36),(-39,83))
    b.shape(jade,(-65,-31),(-38,-45),(-10,-33),(10,-23),(43,-14),(76,17),(56,26),(10,11),(-30,35))
    b.shape(sand,(10,11),(52,19),(75,20),(72,37),(46,44),(15,27),(-18,39))
    b.shape(violet,(-29,36),(-16,42),(9,104),(65,138),(8,137),(-39,90))
    eye(b,-19,-18,.63)
    b.oval(61,22,6,3,ink)
    b.line(ink,3,(74,33),(52,35),(32,27))
    for k in range(8):
        b.curve(ochre,2,(-65,6+k*8),(-62,28+k*8),(-91,43+k*9),(-100-k*3,57+k*8))


def dove(b):
    b.shape(ink,(-78,-7),(-33,-18),(-70,-68),(-25,-51),(9,-13),(37,-22),(53,-19),(59,-11),(77,-10),(58,-3),(35,20),(9,35),(-40,20),(-83,28),(-55,5))
    b.shape(jade,(-73,-6),(-31,-13),(-62,-59),(-25,-47),(13,-9),(0,17),(-34,14),(-71,23),(-48,2))
    b.shape(sand,(-48,0),(10,-4),(38,-18),(51,-16),(56,-7),(69,-9),(52,-2),(32,16),(9,30))
    b.oval(45,-11,2,2,ink)
    for i in range(4):
        b.line(blue,2,(-56+i*6,-52+i*3),(-27+i*7,-22+i*2),(-6+i*3,-9+i*3))


def arcade(b):
    for k in range(5):
        x=-115+k*51
        b.shape(violet,(x,-34),(x+49,-46),(x+51,39),(x,53))
        b.shape(ink,(x+8,47),(x+8,3),(x+14,-11),(x+27,-18),(x+38,-14),(x+42,-6),(x+43,38))
        b.line(ochre,3,(x+5,1),(x+12,-15),(x+27,-22),(x+40,-17))
        b.line(rose,3,(x+1,-28),(x+49,-40))
    b.line(ink,6,(-119,-31),(139,-90))


def village(b):
    for k in range(9):
        x=-118+k*28; y=12*sin(k*.9); h=26+k%3*12
        b.shape(blue,(x,y),(x+24,y-5),(x+23,y-h),(x,y-h+5))
        b.shape(rose if k%3 else ochre,(x-3,y-h+7),(x+6,y-h-10),(x+15,y-h-14),(x+25,y-h))
        b.line(ink,3,(x+8,y-6),(x+8,y-17))
        b.line(ink,2,(x+17,y-10),(x+17,y-15))
    b.curve(jade,4,(-137,26),(-75,-3),(53,46),(153,-1))


def hand(b):
    # Wrist and four individually inked knuckles turning around the soundboard.
    b.shape(ink,(-62,24),(-30,-6),(-6,-16),(24,-30),(48,-29),(51,-24),(35,-17),(54,-23),(62,-20),(59,-14),(42,-5),(56,-10),(64,-6),(57,0),(33,10),(43,9),(47,13),(17,28),(-10,30),(-42,45))
    b.shape(ochre,(-57,24),(-28,-2),(-5,-12),(23,-26),(44,-26),(27,-15),(6,-4),(13,-3),(51,-19),(57,-19),(37,-7),(16,1),(24,3),(55,-6),(59,-5),(32,7),(12,12),(15,16),(37,12),(17,24),(-12,26),(-39,40))
    b.line(rust,2,(-34,16),(-19,7),(-3,5))
    b.line(rust,2,(-21,20),(-10,15),(1,14))


def silk(b):
    b.shape(ink,(-100,104),(-56,20),(-28,-34),(39,-55),(80,-20),(65,62),(13,119))
    b.shape(rose,(-92,100),(-51,20),(-23,-30),(36,-48),(67,-18),(54,59),(12,111))
    for k in range(5):
        x=-43+21*k
        b.shape(blue if k%2 else violet,(x,-1),(x+14,-22),(x+24,3),(x+9,26))
        b.shape(violet if k%2 else blue,(x-13,49),(x+2,26),(x+12,52),(x-3,77))
    b.curve(ochre,3,(-76,88),(-55,80),(22,109),(48,66))


def glasses(b):
    b.shape(ink,(-101,39),(63,14),(110,27),(-37,63))
    b.shape(ochre,(-90,39),(61,20),(90,27),(-36,56))
    b.shape(ink,(2,-54),(55,-48),(45,-20),(30,-9),(22,-8),(17,18),(32,24),(-5,28),(11,16),(14,-9),(3,-18))
    b.shape(blue,(6,-49),(49,-44),(41,-23),(21,-13),(9,-21))
    b.line(rose,3,(11,-31),(43,-33))
    b.oval(-34,11,22,16,rust)
    b.line(ink,3,(-31,1),(-26,-10),(-18,-14))
    b.shape(jade,(-26,-9),(-45,-13),(-55,-8),(-37,-4))


def bell_tower(b):
    b.shape(ink,(-25,48),(-22,-36),(-28,-40),(-4,-82),(24,-47),(18,-43),(24,43))
    b.shape(ochre,(-19,44),(-18,-36),(-2,-46),(1,42))
    b.shape(violet,(1,42),(-2,-46),(15,-42),(20,42))
    b.shape(rust,(-24,-42),(-4,-77),(19,-47))
    b.oval(-9,-23,4,8,ink)
    b.line(sand,2,(-16,4),(-4,2))


# One continuous landscape behind the figures. Wrap duplicate motifs at the canvas join.
for n in range(-1,17):
    y=n*200
    b=Brush(0,y)
    k=n%16
    b.shape(["#495264","#565168","#4c5866","#5b5466"][k%4],(0,-8),(154,35),(298,-9),(512,48),(512,208),(300,173),(82,213),(0,189))
    b.shape(["#495d66","#545369","#5b5866","#4b5767"][k%4],(0,86),(139,46),(248,73),(126,140),(0,174))
    b.shape(["#675764","#475763","#655967","#53606b"][k%4],(512,30),(322,95),(251,154),(392,122),(512,185))
    b.curve("#6c6973",2,(0,139),(143,103),(280,26),(512,81))
    for j in range(7):
        b.line("#73737a",1,(23+j*7,90-j*2),(45+j*7,75-j*2))

for n in range(4):
    top=n*800
    side=1 if n%2==0 else -1
    def at(x,y,s=1,t=0):
        return Brush(x if side==1 else W-x,top+y,s,side,t*side)
    village(at(348,90,1.18,.09))
    if n in (0,2):
        bell_tower(at(308,61,.81 if n==0 else .63,.085))
    else:
        glasses(at(98,116,.77,-.08))
    arcade(at(55,665,1.05,-.13))
    # Broad stepped planes carry the eye between the foreground figures.
    ground=at(130,460,1)
    ground.shape("#766270",(-155,69),(3,-83),(140,-110),(85,-44),(-20,0),(35,51),(-63,103))
    ground.shape("#42606a",(-144,81),(-13,11),(39,53),(155,11),(100,71),(-30,105))
    for j in range(6):
        ground.curve("#8e777d",2,(-125,61+j*5),(-55,29+j*6),(-19,1+j*7),(10,13+j*7))
    if n==0:
        silk(at(145,413,.94,.20)); sleeper(at(170,297,1.25,-.18)); hand(at(208,404,.72,.04)); dove(at(377,514,.93,-.17))
    elif n==1:
        horse(at(163,272,1.35,-.10)); silk(at(370,615,.95,.21)); lute(at(347,581,.91,.16)); hand(at(360,592,.54,-.31))
    elif n==2:
        silk(at(114,385,1.3,.29)); lute(at(136,325,1.31,-.18)); hand(at(141,355,.77,-.31)); sleeper(at(366,557,.87,.30))
    else:
        glasses(at(280,385,.99,-.18)); dove(at(180,295,1.69,-.2)); horse(at(347,553,.94,.17))
    # Orb with a thin offset contour, modest value so hostile moons stay luminous.
    orb=at(356,198)
    orb.oval(0,0,27,30,"#ad937d")
    orb.shape("#97817b",(-24,13),(-2,-4),(22,-16),(27,1),(17,23),(0,30),(-17,24))
    orb.curve("#b19c8c",1,(-35,6),(-42,-35),(0,-47),(19,-32))

# A dragged, broken brush leaves bristles running through the flat pigments.
noise=np.random.default_rng(310)
base=np.asarray(canvas).astype(np.float32)
def field(w,h):
    small=Image.fromarray(noise.integers(0,256,(h,w),dtype=np.uint8))
    return (np.asarray(small.resize((W,H),Image.Resampling.BICUBIC)).astype(np.float32)-127)/127
yy,xx=np.indices((H,W))
grain=field(41,670)*3.6+field(151,1240)*2.3+field(18,66)*3.8+noise.normal(0,1.8,(H,W))
grain+=1.3*np.sin(xx*2.1+np.sin(yy*.092))+1.1*np.cos(yy*2.6)
art=Image.fromarray(np.uint8(np.clip(base+grain[:,:,None],0,255)))
art.save(Path(__file__).resolve().parents[1]/"Assets/Resources/DreamProcession.png",optimize=True)
