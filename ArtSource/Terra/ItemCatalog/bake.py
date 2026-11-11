"""Bake the complete Terra item review set into the isolated Unity project."""

import argparse
from pathlib import Path

import actives
import hardware
import passives


def bake(manifest, destination):
    expected = [line.split("\t")[0] for line in manifest.read_text(encoding="utf-8-sig").splitlines()]
    models = {}
    for group in (actives.BUILDERS, hardware.BUILDERS, passives.BUILDERS):
        duplicates = models.keys() & group.keys()
        if duplicates:
            raise ValueError(f"Duplicate model builders: {sorted(duplicates)}")
        models.update(group)
    if models.keys() != set(expected):
        raise ValueError(f"Missing {set(expected) - models.keys()}, unexpected {models.keys() - set(expected)}")
    destination.mkdir(parents=True, exist_ok=True)
    for key in expected:
        try:
            model = models[key]()
            (destination / f"Item{key}.txt").write_text(model.bake(), encoding="ascii")
        except ValueError as exc:
            raise ValueError(f"{key}: {exc}") from exc
    print(f"Baked {len(expected)} hand-modeled items.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    bake(args.manifest, args.destination)
