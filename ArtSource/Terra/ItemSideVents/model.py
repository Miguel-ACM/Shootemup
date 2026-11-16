"""Paired orange side exhausts on a steel double-ended manifold."""
from pathlib import Path
import sys

source = Path(__file__).resolve()
sys.path.insert(0, str(source.parents[1]))
import item_kit as k

out = Path(sys.argv[sys.argv.index("--") + 1]) if "--" in sys.argv else source.parent
out.mkdir(parents=True, exist_ok=True)
k.begin([(.13, .17, .22), (.36, .47, .55), (.94, .38, .12), (1, .70, .24), (.71, .81, .84)])
k.box((.40, .44, .32), tile=1, bevel=.053)
k.box((.21, .30, .06), loc=(0, 0, .168), tile=0, bevel=.013)
for stripe in (-1, 0, 1):
    k.box((.15, .026, .026), loc=(0, stripe * .08, .203), tile=4, bevel=.006)
for side in (-1, 1):
    k.cylinder(.12, .145, .16, loc=(side * .26, 0, .009), rot=(0, side * 90, 0), tile=4)
    k.cylinder(.133, .181, .15, loc=(side * .351, 0, .009), rot=(0, side * 90, 0), tile=2)
    k.cylinder(.126, .126, .012, loc=(side * .429, 0, .009), rot=(0, side * 90, 0), tile=0)
    k.torus(.144, .030, loc=(side * .432, 0, .009), rot=(0, side * 90, 0), tile=3, ring=12)
k.finish(str(out), 1.16)
