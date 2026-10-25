"""Package actual Unity run-economy, encounter and simultaneous-shell verification."""
from pathlib import Path
import re
import shutil
import subprocess
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/DesertReviewProject/ArtSource/RunChoicesReview"
DEST = ROOT / "ArtSource/RunChoicesReview"
DEST.mkdir(parents=True, exist_ok=True)
for name in ("Baseline.txt", "Verification.txt"):
    report = (SOURCE / name).read_text()
    if not report.startswith("PASS"):
        raise RuntimeError(report)
    shutil.copy2(SOURCE / name, DEST / name)

gallery = ("coinShop-AfterTwoPurchases", "multiplierShop-AfterTwoPurchases", "Encounter-Choosing", "Challenge-Active", "Challenge-Reward", "Shell-Barrage")
sheet = Image.new("RGB", (640 * 3, 505 * 2), "#17212b")
draw = ImageDraw.Draw(sheet)
for index, name in enumerate(gallery):
    path = SOURCE / f"{name}.png"
    shutil.copy2(path, DEST / path.name)
    with Image.open(path) as im:
        im = im.convert("RGB").resize((640, 480))
    x, y = index % 3 * 640, index // 3 * 505
    sheet.paste(im, (x, y + 25))
    draw.text((x + 12, y + 7), name.replace("-", " / "), fill="#e5e9e6")
sheet.save(DEST / "GameplayReview.jpg", quality=95)
for name in ("Shop-SoldOut", "Encounter-Fork", "City-Duo", "Explosion-Stress"):
    shutil.copy2(SOURCE / f"{name}.png", DEST / f"{name}.png")

manifest = []
last = None
for label in ("Explosions", "Choice"):
    frames = sorted((SOURCE / "Frames").glob(f"{label}-*.png"))
    if not frames:
        raise RuntimeError(f"Missing actual motion frames: {label}")
    for frame in frames:
        manifest += [f"file '{frame.as_posix()}'", "duration 0.12"]
        last = frame
for name in gallery:
    path = SOURCE / f"{name}.png"
    manifest += [f"file '{path.as_posix()}'", "duration 1.0"]
    last = path
manifest += [f"file '{last.as_posix()}'"]
sequence = SOURCE / "Sequence.txt"
sequence.write_text("\n".join(manifest) + "\n")
subprocess.run([
    shutil.which("ffmpeg") or r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
    "-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(sequence),
    "-vf", "fps=30", "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart",
    str(DEST / "RunChoicesMotion.mp4")
], check=True)
measurements = []
for name in ("Baseline.txt", "Verification.txt"):
    text = (DEST / name).read_text()
    measurements.append(re.search(r"detonation CPU: ([\d.]+) ms; new bullet objects: (\d+); ignition particle systems: (\d+)", text).groups())
before, after = measurements
(DEST / "README.md").write_text(f"""# Exploding shells, coins, shops and encounter choices

Real Unity 6000.6.2f1 gameplay captures and integration checks in an isolated project. The active game editor was left alone.

## Simultaneous shell release

Sixteen shells release their full 192-child pattern in one frame, using the original sprites, exact pixel colliders and 280-bullet limit.

| Measured section | Before | After |
| --- | ---: | ---: |
| First simultaneous detonation CPU | {before[0]} ms | {after[0]} ms |
| New child bullet objects in combat | {before[1]} | {after[1]} |
| Ignition particle systems | {before[2]} | {after[2]} |

This is a specific editor CPU workload, not an overall FPS claim. Child bullets are prewarmed before combat, with pool capacity reserved for them. Ignition shares a bounded particle renderer. Mass bullet erasure uses shared particles rather than hundreds of individual shockwave renderers; other shockwaves also respect the existing fireball budget. A real moving shell barrage is captured in the first part of the movie.

## Economy and decisions

- Coin pickups add one to the wallet, independently of score and multiplier. The wallet persists across districts and resets on a new run.
- Coin shop prices are 12 / 22 / 36 / 55 by rarity; a low-health kit costs 10. Multiplier shops retain multiplier prices and never spend coins or score.
- Both paid shops allow multiple purchases and remain open even when sold out until the player holds the bottom SKIP zone. Free rewards still grant one choice.
- After the third and sixth combat phrases, the familiar steering panels offer two distinct options from SHOP, MULT SHOP, ITEM, HEALTH and CHALLENGE.
- HEALTH gives one existing health pickup. CHALLENGE raises the next three wave budgets by three (up to 20), adds 20% enemy health and 15% movement / bullet speed, then grants a free item choice. Its third wave must clear before the reward. Later enemies return to normal difficulty; challenge modifiers never alter global run stats.
- City boss base health is 1000 fast / 1400 heavy, down from 1500 / 2100. The existing survivor healing and aggression remain.

`Verification.txt` covers real coin collection, independent currencies, multiple purchases, sold-out exit, steering, all five encounter types, actual challenge enemies, exactly three completed waves, the reward pickup, restored normal combat, City health scaling, wallet travel and new-run reset. `GameplayReview.jpg` and `RunChoicesMotion.mp4` show the new HUD, shops, choices and shell motion.

Reproduce with `VerifyRunChoices.Baseline` before the changes, then `VerifyRunChoices.Run` in a separate project. Package using `Tools/render_run_choices_review.py`.
""")
print(f"Shell detonation CPU {before[0]} -> {after[0]} ms; new bullet objects {before[1]} -> {after[1]}")
