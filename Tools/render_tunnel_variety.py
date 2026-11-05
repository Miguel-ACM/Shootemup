"""Offline views of exported native passage models and recorded movement."""
from pathlib import Path
import json
from PIL import Image, ImageDraw
from render_solid_scenery import render

ROOT = Path(__file__).resolve().parents[1] / 'ArtSource/TunnelVarietyReview'
atlas = ROOT/'Atlas.png'
meshes = [json.loads((ROOT/f'Layout-{i}.json').read_text()) for i in range(1, 7)]
bottom = min(p['y'] for mesh in meshes for p in mesh['vertices'])
top = max(p['y'] for mesh in meshes for p in mesh['vertices'])
scale = 800/(top-bottom+4)
sheet = Image.new('RGB', (1080, 855), '#0c131a')
ink = ImageDraw.Draw(sheet)
for i, (mesh, name) in enumerate(zip(meshes, ('Throats', 'Zigzag', 'Chicane', 'Side buttresses', 'Chambers', 'Long switchback'))):
    sheet.paste(render([mesh], (180, 800), scale, (0, (bottom+top)/2), atlas_path=atlas), (180*i, 26))
    ink.text((180*i+10, 10), name, fill='#e2c58e')
ink.text((10, 838), 'Full passage meshes - CPU lighting; scenery enters from above and scrolls downward', fill='#9eb0b8')
sheet.save(ROOT/'Layouts.png')

frames=[]
for path in sorted(ROOT.glob('Motion-*.json')):
    data=json.loads(path.read_text())
    view=render(data['meshes'], (324,576), 32, (0,-.6), atlas_path=atlas)
    ink=ImageDraw.Draw(view)
    for p in data['rounds']:
        x,y=162+p['x']*32,288-(p['y']+.6)*32
        ink.ellipse((x-2,y-2,x+2,y+2), fill='#e6ac75')
    ink.text((8,560), 'Native movement / CPU mesh preview', fill='#e2c58e')
    frames.append(view)
if frames:
    frames[0].save(ROOT/'Switchback.gif', save_all=True, append_images=frames[1:], duration=500, loop=0)
    sheet=Image.new('RGB',(1944,600),'#0c131a');ink=ImageDraw.Draw(sheet)
    for column,index in enumerate((0,4,8,12,16,20)):
        sheet.paste(frames[index],(column*324,24));ink.text((column*324+10,6),f'{index*.5:.0f}s',fill='#e2c58e')
    sheet.save(ROOT/'Switchback-Sequence.png')
print('Rendered six layouts and recorded switchback motion.')
