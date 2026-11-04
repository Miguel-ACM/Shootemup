"""New hand-drawn record stamps only; preserve all existing dock art and metadata."""
from pathlib import Path
from PIL import Image
import re,uuid
root=Path(__file__).resolve().parents[1]/'Assets/Resources/DockArt'
punches={
    'Heaven':('#..#..#','##.#.##','.#####.','##.#.##','...#...','..###..','...#...'),
    'Void':('...#...','.#.#.#.','..###..','###.###','..###..','.#.#.#.','...#...')}
for name,rows in punches.items():
    art=Image.new('RGBA',(9,9))
    for y,row in enumerate(rows):
        for x,ch in enumerate(row):
            if ch=='#':art.putpixel((x+1,y+1),(255,255,255,255))
    path=root/('Stamp'+name+'.png');art.save(path)
    meta=Path(str(path)+'.meta')
    if not meta.exists():
        source=(root/'StampHell.png.meta').read_text()
        meta.write_text(re.sub(r'guid: [0-9a-f]+','guid: '+uuid.uuid4().hex,source,count=1))
