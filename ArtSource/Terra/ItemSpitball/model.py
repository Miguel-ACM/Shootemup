"""A rubber squeeze bulb backing a wide green pellet nozzle."""
from pathlib import Path
import sys

source = Path(__file__).resolve()
sys.path.insert(0, str(source.parents[1]))
import item_kit as k

out = Path(sys.argv[sys.argv.index("--") + 1]) if "--" in sys.argv else source.parent
out.mkdir(parents=True, exist_ok=True)
k.begin([(.16, .20, .19), (.76, .30, .22), (.97, .49, .33), (.28, .55, .34), (.6, .82, .46), (.87, .94, .81)])
axis = (-90, 0, 0)
k.sphere(.21, loc=(0, -.204, 0), scale=(1.05, 1.04, .87), tile=1, u=12, v=7)
k.torus(.198, .023, loc=(0, -.187, 0), rot=axis, tile=2, ring=12)
k.cylinder(.134, .126, .16, loc=(0, .023, 0), rot=axis, tile=0)
k.cylinder(.124, .193, .22, loc=(0, .176, 0), rot=axis, tile=3, bevel=.009)
k.cylinder(.19, .19, .095, loc=(0, .315, 0), rot=axis, tile=4, bevel=.018)
k.cylinder(.154, .154, .013, loc=(0, .361, 0), rot=axis, tile=0)
k.sphere(.095, loc=(0, .325, .02), scale=(1, .75, 1), tile=5, u=10, v=6)
k.finish(str(out), 1.10)
