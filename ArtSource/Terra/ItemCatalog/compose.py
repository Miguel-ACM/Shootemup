"""Lay out the Unity studio captures and verify the complete image set."""

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


GROUPS = (
    ("weapon", "Weapons"),
    ("passiveWeapon", "Secondary fire"),
    ("active", "Actives"),
    ("partner", "Companions"),
    ("passive", "Passives"),
    ("pickup", "Pickups"),
)
RARITY = {
    "Common": "#abb9c5", "Rare": "#70aaff", "Epic": "#d09cf2",
    "Legendary": "#efc27c", "Pact": "#f08b8c", "Special": "#91d2b5", "Filler": "#9ea9b0",
}
BACKGROUND = "#111820"
INK = "#ecf0ed"
MUTED = "#a7b4bb"
FRAMING = (768, 768)


def font(size, bold=False):
    name = "segoeuib.ttf" if bold else "segoeui.ttf"
    try:
        return ImageFont.truetype(str(Path("C:/Windows/Fonts") / name), size)
    except OSError:
        return ImageFont.load_default(size=size)


def heading(width, height, title, line):
    page = Image.new("RGB", (width, height), BACKGROUND)
    draw = ImageDraw.Draw(page)
    draw.text((32, 19), title, font=font(37, True), fill=INK)
    draw.text((34, 70), line, font=font(16), fill=MUTED)
    return page


def backdrop(tile):
    image = Image.new("RGB", (96, 96))
    pixels = image.load()
    for y in range(96):
        for x in range(96):
            light = max(0, 1 - ((x - 41) / 79) ** 2 - ((y - 32) / 90) ** 2)
            pixels[x, y] = tuple(round(base + light * value) for base, value in ((23, 25), (31, 20), (41, 15)))
    return image.resize((tile, tile), Image.Resampling.BICUBIC).convert("RGBA")


def portrait(page, background, path, x, y):
    card = background.copy()
    with Image.open(path) as source:
        card.alpha_composite(source.convert("RGBA").resize(card.size, Image.Resampling.LANCZOS))
    page.paste(card.convert("RGB"), (x, y))


def label(draw, row, x, y, tile):
    size = 21
    while size > 13 and draw.textlength(row["name"], font=font(size, True)) > tile - 8:
        size -= 1
    draw.text((x, y), row["name"], font=font(size, True), fill=INK)
    draw.text((x, y + 27), row["rarity"].upper(), font=font(12), fill=RARITY[row["rarity"]])


def sheet(folder, rows, title, line, name, columns, tile, suffix="", model_folder=None):
    margin, gutter, top, caption = 32, 13, 105, 57
    row_count = (len(rows) + columns - 1) // columns
    width = margin * 2 + columns * tile + (columns - 1) * gutter
    height = top + row_count * (tile + caption + gutter) + 15
    page = heading(width, height, title, line)
    draw = ImageDraw.Draw(page)
    background = backdrop(tile)
    for i, row in enumerate(rows):
        x = margin + (i % columns) * (tile + gutter)
        y = top + (i // columns) * (tile + caption + gutter)
        portrait(page, background, (model_folder or folder) / f"{row['key']}{suffix}.png", x, y)
        draw.line((x, y + tile, x + 29, y + tile), fill=RARITY[row["rarity"]], width=3)
        label(draw, row, x, y + tile + 7, tile)
    page.save(folder / name, optimize=True)


def comparison(folder, previous, items):
    keys = ("BaseWeaponItem", "NeedleCannonItem", "MirrorBurst", "Caffeine",
            "RelaySwitch", "SignalFlare", "Telekinesis", "Tripod")
    lookup = {item["key"]: item for item in items}
    tile, gap, top, row_step = 204, 24, 108, 283
    cell = 2 * tile + 7
    page = heading(64 + cell * 4 + gap * 3, top + row_step * 2 + 16,
                   "Item model comparison", "Previous catalog on the left · Terra studio render on the right")
    draw = ImageDraw.Draw(page)
    background = backdrop(tile)
    for index, key in enumerate(keys):
        x = 32 + (index % 4) * (cell + gap)
        y = top + (index // 4) * row_step
        portrait(page, background, previous / f"{key}.png", x, y)
        portrait(page, background, folder / f"{key}.png", x + tile + 7, y)
        label(draw, lookup[key], x, y + tile + 9, cell)
    page.save(folder / "Before-and-after.png", optimize=True)


def audited_items(folder):
    items = []
    for line in (folder / "items.tsv").read_text(encoding="utf-8-sig").splitlines():
        key, name, kind, rarity, status, *_ = line.split("\t")
        if status != "ok":
            raise ValueError(f"Missing render for {key}")
        items.append(dict(key=key, name=name, kind=kind, rarity=rarity))
    reports = {}
    for file in ("mesh-audit.tsv", "partial-mesh-audit.tsv"):
        path = folder / file
        if path.exists():
            for line in path.read_text(encoding="utf-8-sig").splitlines():
                reports[line.split("\t")[0]] = line
    if len(items) != len(reports) or any(not reports[item["key"]].endswith("\tok") for item in items):
        raise ValueError("Unity triangle/normal checks did not pass for the entire catalog")
    (folder / "mesh-audit.tsv").write_text("\n".join(reports[item["key"]] for item in items) + "\n", encoding="utf-8")
    for item in items:
        key = item["key"]
        paths = [folder / f"{key}{suffix}.png" for suffix in ("", "-Front", "-Rear")]
        paths += [folder / "Normals" / f"{key}{suffix}.png" for suffix in ("", "-Rear")]
        for path in paths:
            with Image.open(path) as source:
                if source.mode != "RGBA" or source.size != FRAMING:
                    raise ValueError(f"Invalid image format: {path}")
                bounds = source.getchannel("A").getbbox()
                if bounds is None or min(bounds[0], bounds[1], 768 - bounds[2], 768 - bounds[3]) < 40:
                    raise ValueError(f"Clipped or invisible model: {path}")
                if source.getpixel((0, 0))[3] != 0:
                    raise ValueError(f"Capture did not preserve the transparent background: {path}")
    return items


def compose(folder, previous):
    items = audited_items(folder)
    ordered = []
    for kind, heading_text in GROUPS:
        group = sorted((row for row in items if row["kind"] == kind), key=lambda row: row["name"].casefold())
        ordered.extend(group)
        for start in range(0, len(group), 20):
            page_number = start // 20 + 1
            page = group[start:start + 20]
            key = heading_text.lower().replace(" ", "-")
            line = f"{len(page)} models · Terra studio"
            sheet(folder, page, heading_text, line, f"{key}-{page_number}.png", 4, 290)
            sheet(folder, page, heading_text + " · Rear", line, f"{key}-{page_number}-Rear.png", 4, 290, "-Rear")
    if len(ordered) != len(items):
        raise ValueError("One or more items are absent from the sheets.")
    count = len(ordered)
    sheet(folder, ordered, "Shootemup item catalog", f"{count} revised low-poly models",
          "All-items.png", 10, 230)
    sheet(folder, ordered, "Rear views", f"{count} revised low-poly models",
          "All-items-Rear.png", 10, 230, "-Rear")
    sheet(folder, ordered, "Surface normals", f"{count} models · Front three-quarter diagnostic",
          "Normals-Front.png", 10, 155, model_folder=folder / "Normals")
    sheet(folder, ordered, "Surface normals", f"{count} models · Rear three-quarter diagnostic",
          "Normals-Rear.png", 10, 155, "-Rear", folder / "Normals")
    comparison(folder, previous, items)
    print(f"Verified {count} meshes and {count * 5} images.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("render_folder", type=Path)
    parser.add_argument("previous_folder", type=Path)
    args = parser.parse_args()
    compose(args.render_folder, args.previous_folder)
