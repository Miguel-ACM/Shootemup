"""Arrange unaltered Unity captures for the Heaven / Void model and attack review."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1] / 'ArtSource/CelestialPolishReview'


def arena(path):
    with Image.open(path) as source:
        image = source.convert('RGB')
    width, height = image.size
    span = round(height * 405 / 720)
    left = (width - span) // 2
    return image.crop((left, 0, left + span, height))


def sheet(files, name, columns=4, tile=(320,350), gameplay=False):
    if not files:
        return
    canvas = Image.new('RGB', (columns * tile[0], ((len(files)+columns-1)//columns)*tile[1]), '#11141d')
    draw = ImageDraw.Draw(canvas)
    for index, path in enumerate(files):
        image = arena(path) if gameplay else Image.open(path).convert('RGB')
        image.thumbnail((tile[0]-12,tile[1]-30))
        x, y = index % columns * tile[0], index // columns * tile[1]
        canvas.paste(image, (x+(tile[0]-image.width)//2,y))
        label = path.parent.name if gameplay else path.stem
        draw.text((x+8,y+tile[1]-23),label,fill='#eee3cb')
    canvas.save(ROOT/name,quality=94)


def animation(name, clips, duration):
    frames = []
    for entry in clips:
        clip, start, stop = entry[:3]
        paths = sorted((ROOT/'Frames'/clip).glob('*.jpg'))[start:stop]
        if len(entry)>3:
            step=entry[3]*duration/1000
            paths=[paths[round(i*step)] for i in range(int(len(paths)/step)) if round(i*step)<len(paths)]
        for path in paths:
            frames.append(arena(path).resize((270,480),Image.Resampling.LANCZOS).quantize(colors=96))
    if frames:
        frames[0].save(ROOT/name,save_all=True,append_images=frames[1:],duration=duration,loop=0,optimize=True)


if __name__ == '__main__':
    sheet(sorted((ROOT/'Models').glob('*-0.png')),'Models.jpg')
    sheet(sorted((ROOT/'Models').glob('*-1.png')),'Models-Angled.jpg')
    gameplay=[]
    for folder in sorted((ROOT/'Frames').iterdir()):
        if folder.name.startswith(('Void-Easy','Void-Normal')) or folder.name=='Void-Hard-4':
            continue
        paths=sorted(folder.glob('*.jpg'))
        if paths:
            gameplay.append(paths[int(len(paths)*.57)])
    sheet(gameplay,'Gameplay.jpg',4,(270,510),True)
    animation('Heaven.gif', [('Heaven-Ruins-Wave',20,45),('Heaven-Gatekeeper',25,40),('Heaven-Angel',30,65)],200)
    animation('Void-Forms.gif',[(f'Void-Hard-{phase}',12,36) for phase in range(1,4)]+[('Void-Final-Art',15,45,5)],250)
    animation('Void-Alternate-Cards.gif',[(f'Void-Hard-{phase}',40,60) for phase in range(1,4)]+[('Void-Final-Art',50,75,5)],250)
    animation('Void-Interludes.gif',[(f'Void-Interlude-{phase}',20,45) for phase in range(1,4)],250)
    print('Prepared model sheets, gameplay sheet and four motion previews.')
