"""Package sampled Unity desert playback; preserve the recorded wave timestamps."""
from pathlib import Path
import shutil
import subprocess
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/DesertReviewProject/ArtSource/DesertReview"
DEST = ROOT / "ArtSource/DesertReview"
DEST.mkdir(parents=True, exist_ok=True)
for name in ("Geometry.txt", "Verification.txt", "Scenery.png", "DustStorm.png", "Boss.png", "DamagedBoss.png", "RouteChoice.png"):
    shutil.copy2(SOURCE / name, DEST / name)

frames = SOURCE / "Frames"
started = (SOURCE / "Scenery.png").stat().st_mtime
def current(pattern):
    return sorted(path for path in frames.glob(pattern) if path.stat().st_mtime >= started)

chosen = []
for budget in (2, 10, 20):
    candidates = current(f"Wave{budget}-*.png")
    for fraction in (.3, .5):
        chosen.append((candidates[int(len(candidates) * fraction)], f"Wave {budget}"))
for frame in (24, 58, 100):
    chosen.append((frames / f"Boss-{frame:03d}.png", "White worm"))

sheet = Image.new("RGB", (405 * 3, 746 * 3), "#10171a")
draw = ImageDraw.Draw(sheet)
for i, (path, label) in enumerate(chosen):
    with Image.open(path) as image:
        w, h = image.size
        half = h * 405 / 720 / 2
        arena = image.crop((int(w / 2 - half), 0, int(w / 2 + half), h)).resize((405, 720))
    x, y = (i % 3) * 405, (i // 3) * 746
    sheet.paste(arena, (x, y + 26))
    draw.text((x + 10, y + 7), label, fill="#d7ddca")
sheet.save(DEST / "MotionReview.jpg", quality=94)

manifest = []
for budget in (2, 10, 20):
    candidates = current(f"Wave{budget}-*.png")
    for i, path in enumerate(candidates):
        stamp = int(path.stem.split("-")[1]) / 100
        duration = int(candidates[i + 1].stem.split("-")[1]) / 100 - stamp if i + 1 < len(candidates) else .4
        manifest += [f"file '{path.as_posix()}'", f"duration {duration:.4f}"]
# Boss frames are spaced by the probe's explicit quarter-second playback interval.
for path in current("Boss-*.png"):
    manifest += [f"file '{path.as_posix()}'", "duration 0.25"]
manifest += [f"file '{current('Boss-*.png')[-1].as_posix()}'"]
sequence = SOURCE / "Sequence.txt"
sequence.write_text("\n".join(manifest) + "\n")
subprocess.run([
    shutil.which("ffmpeg") or r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
    "-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(sequence),
    "-vf", "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30",
    "-c:v", "libx264", "-crf", "20", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(DEST / "DesertMotion.mp4")
], check=True)
print(DEST)
