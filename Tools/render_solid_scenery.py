"""CPU views of exported Unity meshes/UVs; these are not gameplay screenshots."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1] / 'ArtSource/SolidSceneryReview'


def render(meshes, size, scale, center, angles=(0, 0), atlas_path=None):
    width, height = size
    color = np.zeros((height, width, 3), dtype=np.uint8)
    color[:] = (12, 19, 26)
    depth = np.full((height, width), np.inf)
    atlas = np.asarray(Image.open(atlas_path or ROOT / 'Atlas.png').convert('RGB'))
    a, b = np.deg2rad(angles)
    rx = np.array([[1, 0, 0], [0, np.cos(a), -np.sin(a)], [0, np.sin(a), np.cos(a)]])
    ry = np.array([[np.cos(b), 0, np.sin(b)], [0, 1, 0], [-np.sin(b), 0, np.cos(b)]])
    light = np.array([-.25, .45, -.86]); light /= np.linalg.norm(light)
    for mesh in meshes:
        v = np.array([[p['x'], p['y'], p['z']] for p in mesh['vertices']]) @ (rx @ ry).T
        uv = np.array([[p['x'], p['y']] for p in mesh['uv']])
        p = np.column_stack((width / 2 + (v[:, 0] - center[0]) * scale, height / 2 - (v[:, 1] - center[1]) * scale))
        for indices in np.array(mesh['triangles']).reshape(-1, 3):
            xyz = v[indices]; q = p[indices]
            normal = np.cross(xyz[1] - xyz[0], xyz[2] - xyz[0])
            norm = np.linalg.norm(normal)
            if norm < 1e-9 or normal[2] >= 0: continue
            normal /= norm
            x0, y0 = np.maximum(np.floor(q.min(axis=0)).astype(int), 0)
            x1, y1 = np.minimum(np.ceil(q.max(axis=0)).astype(int), (width-1, height-1))
            if x1 < x0 or y1 < y0: continue
            xx, yy = np.meshgrid(np.arange(x0, x1+1)+.5, np.arange(y0, y1+1)+.5)
            den = (q[1, 1]-q[2, 1])*(q[0, 0]-q[2, 0])+(q[2, 0]-q[1, 0])*(q[0, 1]-q[2, 1])
            if abs(den) < 1e-9: continue
            u = ((q[1, 1]-q[2, 1])*(xx-q[2, 0])+(q[2, 0]-q[1, 0])*(yy-q[2, 1]))/den
            w = ((q[2, 1]-q[0, 1])*(xx-q[2, 0])+(q[0, 0]-q[2, 0])*(yy-q[2, 1]))/den
            t = 1-u-w
            z = u*xyz[0, 2]+w*xyz[1, 2]+t*xyz[2, 2]
            region = depth[y0:y1+1, x0:x1+1]
            visible = (u >= 0) & (w >= 0) & (t >= 0) & (z < region)
            tex = u[..., None]*uv[indices[0]]+w[..., None]*uv[indices[1]]+t[..., None]*uv[indices[2]]
            tx = np.clip((tex[..., 0]*(atlas.shape[1]-1)).astype(int), 0, atlas.shape[1]-1)
            ty = np.clip(((1-tex[..., 1])*(atlas.shape[0]-1)).astype(int), 0, atlas.shape[0]-1)
            rgb = np.clip(atlas[ty, tx] * (.5+.5*max(0, normal@light)), 0, 255).astype(np.uint8)
            color[y0:y1+1, x0:x1+1][visible] = rgb[visible]
            region[visible] = z[visible]
    return Image.fromarray(color)


def main():
    mine = json.loads((ROOT/'Mine.json').read_text())
    sheet = Image.new('RGB', (1080, 400), '#0c131a')
    draw = ImageDraw.Draw(sheet)
    for i, angles in enumerate(((0, 0), (20, 30), (-15, -35))):
        sheet.paste(render([mine], (360, 350), 150, (0, -.1), angles), (360*i, 25))
        draw.text((360*i+14, 12), ['Front', 'Raised view', 'Opposite view'][i], fill='#e2c58e')
    draw.text((14, 380), 'Exported Unity mesh and atlas - CPU lighting preview', fill='#9eb0b8')
    sheet.save(ROOT/'Mine-Views.png')

    sheet = Image.new('RGB', (972, 620), '#0c131a')
    draw = ImageDraw.Draw(sheet)
    for i, budget in enumerate((6, 10, 16)):
        data = json.loads((ROOT/f'Tunnel-{budget}.json').read_text())
        view = render(data['meshes'], (324, 576), 32, (0, -.6))
        ink = ImageDraw.Draw(view)
        for shot in data['rounds']:
            x, y = 162+shot['x']*32, 288-(shot['y']+.6)*32
            ink.ellipse((x-2, y-2, x+2, y+2), fill='#e6ac75')
        sheet.paste(view, (324*i, 24))
        draw.text((324*i+10, 7), f'Passage {budget} - native positions', fill='#e2c58e')
    draw.text((10, 605), 'CPU mesh preview; dots mark projectile positions, not projectile artwork', fill='#9eb0b8')
    sheet.save(ROOT/'Passages.png')
    frames = []
    for path in sorted(ROOT.glob('Motion-*.json')):
        data = json.loads(path.read_text())
        view = render(data['meshes'], (324, 576), 32, (0, -.6))
        ink = ImageDraw.Draw(view)
        for shot in data['rounds']:
            x, y = 162+shot['x']*32, 288-(shot['y']+.6)*32
            ink.ellipse((x-2, y-2, x+2, y+2), fill='#e6ac75')
        ink.text((8, 560), 'Native positions / CPU mesh preview', fill='#e2c58e')
        frames.append(view)
    if frames:
        frames[0].save(ROOT/'Passage-Motion.gif', save_all=True, append_images=frames[1:], duration=250, loop=0)
        strip = Image.new('RGB', (324*6, 600), '#0c131a')
        draw = ImageDraw.Draw(strip)
        for column, index in enumerate((0, 4, 8, 12, 16, 20)):
            if index < len(frames):
                strip.paste(frames[index], (column*324, 24))
                draw.text((column*324+12, 6), f'{index*.25:.1f}s - recorded formation', fill='#e2c58e')
        strip.save(ROOT/'Passage-Sequence.png')
    print('Rendered mine views, passage geometry and recorded formation motion.')


if __name__ == '__main__':
    main()
