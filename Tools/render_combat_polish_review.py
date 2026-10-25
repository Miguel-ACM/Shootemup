"""Package the isolated Unity performance measurements and real gameplay captures."""
from pathlib import Path
import re
import shutil
import subprocess
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/DesertReviewProject/ArtSource/CombatPolishReview"
DEST = ROOT / "ArtSource/CombatPolishReview"
DEST.mkdir(parents=True, exist_ok=True)
for name in ("Baseline.txt", "Verification.txt", "DiscRotation.txt"):
    text = (SOURCE / name).read_text()
    if not text.startswith("PASS"):
        raise RuntimeError(f"Review did not pass: {name}\n{text}")
    shutil.copy2(SOURCE / name, DEST / name)

levels = ("Fields", "Snow", "City", "Thunder", "Tunnel", "Desert", "Candyland")
sheet = Image.new("RGB", (270 * 7, 505), "#17212b")
draw = ImageDraw.Draw(sheet)
for col, level in enumerate(levels):
    path = SOURCE / f"Palette-{level}.png"
    shutil.copy2(path, DEST / path.name)
    with Image.open(path) as im:
        w, h = im.size
        half = h * 405 / 720 / 2
        arena = im.crop((int(w/2-half), 0, int(w/2+half), h)).resize((270, 480))
    sheet.paste(arena, (col * 270, 25))
    draw.text((col * 270 + 10, 7), level, fill="#e1e8ed")
sheet.save(DEST / "DistrictPalettes.jpg", quality=95)
for name in ("Tanks-Road", "Snow-Boss"):
    shutil.copy2(SOURCE / f"{name}.png", DEST / f"{name}.png")

motion = Image.new("RGB", (270 * 6, 505 * 2), "#17212b")
draw = ImageDraw.Draw(motion)
manifest = []
last = None
for row, label in enumerate(("Tanks", "Snow")):
    frames = sorted((SOURCE / "Frames").glob(f"{label}-*.png"))
    if not frames:
        raise RuntimeError(f"No motion frames: {label}")
    for col in range(6):
        path = frames[min(len(frames)-1, round((col + .5) / 6 * len(frames)))]
        with Image.open(path) as im:
            w, h = im.size
            half = h * 405 / 720 / 2
            arena = im.crop((int(w/2-half), 0, int(w/2+half), h)).resize((270, 480))
        motion.paste(arena, (col * 270, row * 505 + 25))
        draw.text((col * 270 + 10, row * 505 + 7), f"{label} / frame {path.stem.split('-')[-1]}", fill="#e1e8ed")
    for path in frames:
        manifest += [f"file '{path.as_posix()}'", f"duration {'0.12' if label == 'Tanks' else '0.15'}"]
        last = path
motion.save(DEST / "MotionReview.jpg", quality=95)
manifest += [f"file '{last.as_posix()}'"]
sequence = SOURCE / "Sequence.txt"
sequence.write_text("\n".join(manifest) + "\n")
subprocess.run([
    shutil.which("ffmpeg") or r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
    "-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(sequence),
    "-vf", "fps=30", "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p",
    "-movflags", "+faststart", str(DEST / "CombatPolishMotion.mp4")
], check=True)
measurements = []
for name in ("Baseline.txt", "Verification.txt"):
    text = (DEST / name).read_text()
    measurements.append((float(re.search(r"median ([\d.]+) ms", text)[1]), float(re.search(r"p95 ([\d.]+) ms", text)[1]), int(re.search(r"bursts: (\d+)", text)[1])))
before, after = measurements
(DEST / "README.md").write_text(f"""# Combat performance, palettes and tank driving

Captured from Unity 6000.6.2f1 in the isolated review project. The active game editor was left alone.

## Matched CPU workload

280 mixed hostile rounds with their existing compound pixel colliders, moving through 24 enemy hulls. 220 synchronized physics steps, 30 warmup steps excluded.

| Measurement | Before | After |
| --- | ---: | ---: |
| Median physics + transform sync | {before[0]:.4f} ms | {after[0]:.4f} ms |
| p95 physics + transform sync | {before[1]:.4f} ms | {after[1]:.4f} ms |
| Objects used for four 280-round bursts | {before[2]} | {after[2]} |

The median measured CPU section fell by {100*(1-after[0]/before[0]):.1f}%. This is an editor physics benchmark, not a claim about total game FPS. Bullet counts, source sprites and exact pixel collision outlines are preserved. Filtering removes contacts against enemy hulls and other hostile ordnance; tests exercise real player-layer and companion-layer contacts and friendly rounds destroying plasma.

## Visual review

`DistrictPalettes.jpg` shows all twelve hostile sprite types in each district with separate cyan player rounds. `MotionReview.jpg` and `CombatPolishMotion.mp4` show the actual tanks following road bends and the Snow boss's shell patterns through both phases. Full frame captures keep the game's filter, HUD and aspect ratio.

Snow boss health is 2100 (previously 3200); run scaling and attack phases remain intact. Tank driving is downward relative to both the camera and scrolling terrain; chassis turn to the road tangent while turrets track the player.

See `Verification.txt` for contact, pixel-outline, palette reuse, heading, stock-gun damage and boss checks. `DiscRotation.txt` checks the existing disc spin after the orientation cache change. Reproduce with `VerifyCombatPolish.RunBaseline` on the preceding code, then `VerifyCombatPolish.Run` and `VerifyCombatPolish.CheckDiscRotation` on this code in a separate Unity project. Package with `Tools/render_combat_polish_review.py`.
""")
print(f"Median physics cost: {before[0]:.4f} -> {after[0]:.4f} ms; objects {before[2]} -> {after[2]}")
