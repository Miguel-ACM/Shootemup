"""Plot native Unity trajectories; these are geometry diagrams, not game screenshots."""
from pathlib import Path
from collections import defaultdict
import csv
import math
import sys
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1] / 'ArtSource/CelestialPatternCorrection'
W, H, SCALE = 252, 444, 25


def draw(rows, title):
    image = Image.new('RGB', (W, H), '#101421')
    pen = ImageDraw.Draw(image)
    color = '#f3bd61' if 'Heaven' in title else '#d28bff' if 'Void' in title else '#f58ccc'
    def point(x, y):
        return W / 2 + float(x) * SCALE, 25 + (7.88 - float(y)) * SCALE
    for row in rows:
        x, y = point(row['x'], row['y'])
        if row['type'] == 'PulseLaser':
            continue  # The sprite is not the beam ribbon; do not invent its footprint.
        radius = max(1.2, min(11, float(row['width']) * SCALE * .35))
        if row['type'] == 'NeedleRound':
            angle = math.radians(float(row['angle']))
            dx, dy = math.sin(angle) * 3, math.cos(angle) * 3
            pen.line((x+dx, y+dy, x-dx, y-dy), fill=color, width=2)
        else:
            pen.ellipse((x-radius, y-radius, x+radius, y+radius), fill=color)
    if rows and 'Interlude' not in title:
        x, y = point(rows[0]['bossX'], rows[0]['bossY'])
        pen.ellipse((x-6, y-6, x+6, y+6), fill='#e7e8ee')
    pen.rectangle((0, 0, W, 23), fill='#232c40')
    pen.text((7, 6), title, fill='white')
    return image


tiles = []
targets = set(sys.argv[1:])
for path in sorted((ROOT / 'Patterns').glob('*.csv')):
    if targets and path.stem not in targets:
        continue
    if not path.name.startswith(('Normal-', 'Hard-')):
        continue
    frames = defaultdict(list)
    with path.open() as source:
        for row in csv.DictReader(source):
            frames[int(row['frame'])].append(row)
    for card in (0, 1):
        candidates = [rows for rows in frames.values() if int(rows[0]['card']) == card]
        if not candidates:
            continue
        candidates.sort(key=len)
        tiles.append(draw(candidates[int((len(candidates)-1)*.65)], path.stem+(' / '+str(card+1) if 'Interlude' not in path.stem else '')))
    animation = [draw(rows, path.stem).quantize(colors=64) for rows in frames.values()]
    if animation:
        animation[0].save(ROOT / (path.stem+'.gif'), save_all=True, append_images=animation[1:], duration=250, loop=0)
    if targets and frames:
        ordered = list(frames.values())
        sheet = Image.new('RGB', (W*4, H), '#101421')
        for i, fraction in enumerate((.15, .3, .45, .6)):
            rows = ordered[int((len(ordered)-1)*fraction)]
            elapsed = float(rows[0]['time']) - float(ordered[0][0]['time'])
            sheet.paste(draw(rows, path.stem+' / %.1fs'%elapsed), (i*W, 0))
        sheet.save(ROOT / ('Motion-'+path.stem+'.jpg'), quality=94)

if tiles:
    sheet = Image.new('RGB', (W*5, H*math.ceil(len(tiles)/5)), '#101421')
    for i, tile in enumerate(tiles):
        sheet.paste(tile, ((i%5)*W, (i//5)*H))
    sheet.save(ROOT / ('FocusGeometry.jpg' if targets else 'PatternGeometry.jpg'), quality=93)
print('Rendered', len(tiles), 'pattern panels from native positions.')
