"""Bake a seamless slope and foam map for the sewer channel."""
from pathlib import Path

import numpy as np
from PIL import Image


def main():
    rng = np.random.default_rng(1307)
    size = 256
    y, x = np.mgrid[:size, :size] * (2 * np.pi / size)

    def waves(bands, horizontal, vertical):
        field = np.zeros_like(x)
        for _ in range(bands):
            kx = rng.integers(1, horizontal + 1) * rng.choice([-1, 1])
            ky = rng.integers(1, vertical + 1)
            field += np.sin(kx * x + ky * y + rng.uniform(0, 2 * np.pi))
        return field / np.sqrt(bands)

    bend = waves(9, 3, 3)
    pools = waves(12, 4, 3)
    surface = .40 * waves(15, 5, 11)
    surface += .52 * np.sin(8 * y + 2 * x + 2.4 * bend)
    surface += .23 * np.sin(14 * y - 3 * x + 1.8 * pools)
    across = np.roll(surface, -1, axis=1) - np.roll(surface, 1, axis=1)
    along = np.roll(surface, -1, axis=0) - np.roll(surface, 1, axis=0)

    # Thin, broken filaments follow elongated shear lines in the current.
    whorl = 7 * x + 1.6 * np.sin(y + bend * .7) + pools
    filament = np.exp(-np.square(np.sin(whorl) * 5)) * np.clip(pools + .1, 0, 1)
    channels = np.stack((.5 + across * .9, .5 + along * .62, filament), axis=-1)
    pixels = np.rint(np.clip(channels, 0, 1) * 255).astype(np.uint8)
    target = Path(__file__).resolve().parents[1] / "Assets/Resources/SewerCurrent.png"
    Image.fromarray(pixels).save(target)


if __name__ == "__main__":
    main()
