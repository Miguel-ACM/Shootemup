"""Draw the remaining district stamps on the same 48-pixel grid as Mushroom."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1] / "Assets/Resources/DockArt"


def stamp(name, inks, draw):
    image = Image.new("RGBA", (48, 48))
    draw(ImageDraw.Draw(image), *inks.split())
    image.save(ROOT / f"Stamp{name}.png")


def plains(d, edge, leaf, grain):
    for x, y in [(14, 13), (24, 8), (34, 15)]:
        d.line((x-3, 40, x, y), edge, 3)
        for i in range(3):
            h = y + i*6
            d.polygon([(x, h+10), (x-6,h+4), (x-6,h), (x,h+4)], leaf)
            d.polygon([(x,h+7), (x+5,h+2), (x+6,h-2), (x,h+1)], grain)


def snow(d, edge, ice, light):
    for dx, dy in [(0, -17), (15, -8), (15, 8), (0, 17), (-15, 8), (-15, -8)]:
        for width, ink in [(5, edge), (3, ice)]:
            d.line((24,24,24+dx,24+dy), ink, width)
            for sign in [-1,1]:
                d.line((24+dx*.58,24+dy*.58,24+dx*.78-sign*dy*.25,24+dy*.78+sign*dx*.25),ink,width)
    d.polygon([(24,17),(30,21),(30,27),(24,31),(18,27),(18,21)],light)


def city(d, edge, roof, lit):
    for x, y, w in [(6,21,10),(29,16,12),(16,10,13)]:
        d.rectangle((x,y,x+w,38),edge)
        d.polygon([(x,y),(x+4,y-4),(x+w,y-4),(x+w,y)],roof)
        for row in range(y+3,36,6):
            for col in range(x+2,x+w-1,4):
                d.rectangle((col,row,col+1,row+2),lit)


def thunder(d, edge, gold, light):
    points=[(23,7),(39,7),(29,20),(37,20),(13,42),(20,28),(10,28)]
    d.polygon([(x+1,y+2) for x,y in points],edge)
    d.polygon(points,gold)
    d.polygon([(23,7),(29,7),(18,24),(12,25)],light)


def tunnel(d, edge, stone, lit):
    d.polygon([(5,36),(5,21),(14,10),(31,9),(41,20),(42,36)],edge)
    d.line([(7,32),(8,21),(15,13),(29,12),(38,21),(39,32)],stone,5)
    for a,b in [((13,15),(16,18)),((22,11),(22,16)),((33,15),(30,19))]:
        d.line([a,b],lit,2)
    for x in [8,34]: d.line((x,39,22+(x-21)*.25,25),lit,2)


def desert(d, edge, sand, sun):
    d.ellipse((27,8,38,19),sun)
    d.polygon([(4,35),(14,19),(21,15),(37,35)],edge)
    d.polygon([(4,35),(14,19),(21,15),(16,26)],sand)
    d.polygon([(17,38),(30,23),(35,21),(44,38)],sand)
    d.polygon([(30,31),(35,21),(44,38)],edge)


def candy(d, edge, pink, white):
    d.line((20,26,32,41),edge,5)
    d.line((20,26,32,40),white,2)
    d.ellipse((6,6,34,34),edge)
    d.ellipse((8,7,32,31),pink)
    d.arc((10,9,30,29),174,460,white,3)
    d.arc((15,14,26,25),-75,225,white,3)
    d.rectangle((19,17,22,20),white)


def space(d, edge, planet, lit):
    d.ellipse((12,9,36,34),edge)
    d.ellipse((13,9,33,31),planet)
    d.arc((15,11,30,25),190,280,lit,2)
    d.line([(13,20),(5,28),(5,32),(12,33),(30,26),(41,18),(42,14),(36,14)],edge,4)
    d.line([(5,30),(11,31),(29,24),(40,17)],lit,3)


def underwater(d, edge, sea, pearl):
    d.polygon([(9,14),(17,16),(23,10),(29,14),(33,11),(41,19),(38,28),(28,36),(19,38),(8,28),(5,20)],edge)
    d.polygon([(8,20),(11,16),(17,19),(23,12),(29,17),(33,14),(38,20),(35,26),(23,35),(12,27)],sea)
    for x,y in [(13,21),(21,17),(30,19),(35,21)]: d.line((23,33,x,y),pearl,2)


def hell(d, edge, ember, hot):
    d.polygon([(6,34),(14,23),(18,11),(28,13),(34,24),(42,34),(39,38),(8,38)],edge)
    d.polygon([(16,16),(18,11),(28,13),(30,18),(26,17),(29,24),(34,32),(28,30),(22,19),(21,23)],ember)
    d.polygon([(19,12),(21,7),(23,10),(26,5),(27,13),(23,16)],hot)


def heaven(d, edge, gold, lit):
    pts=[(23,23),(5,12),(8,22),(15,27),(9,25),(14,32),(22,35)]
    for flip in [False,True]:
        d.polygon([(47-x if flip else x,y) for x,y in pts],edge)
        d.line([(47-x if flip else x,y) for x,y in [(8,16),(13,21),(20,25)]],lit,3)
    d.ellipse((14,7,33,15),outline=gold,width=3)
    d.polygon([(24,19),(29,27),(24,37),(19,27)],gold)


def void(d, edge, purple, light):
    d.polygon([(4,24),(15,16),(22,8),(25,14),(41,16),(36,23),(43,26),(30,32),(24,40),(21,33),(7,32),(12,27)],edge)
    d.polygon([(7,24),(19,17),(31,16),(39,18),(29,28),(18,31)],purple)
    d.polygon([(15,25),(21,20),(30,18),(27,24),(21,27)],light)
    d.polygon([(22,22),(26,19),(24,25),(21,26)],edge)


if __name__ == "__main__":
    for name, colors, motif in [
        ("Fields","#49693f #98a356 #efcf82",plains), ("Snow","#587990 #a8d4df #f0f8ee",snow),
        ("City","#41495b #8592a1 #f2cd87",city), ("Thunder","#956135 #edbb54 #fff4b8",thunder),
        ("Tunnel","#41464d #828e93 #d1b68b",tunnel), ("Desert","#ad6c50 #ecc487 #f8dc9a",desert),
        ("Candyland","#a65272 #e8a4b2 #fff0da",candy), ("Space","#554772 #9284bb #f2cb99",space),
        ("Underwater","#425d78 #79aaa9 #d4e9ca",underwater), ("Hell","#63505b #de7952 #ffd798",hell),
        ("Heaven","#9d9498 #dbb67e #fff1d5",heaven), ("Void","#403449 #9667a8 #e6bddd",void),
    ]:
        stamp(name, colors, motif)
