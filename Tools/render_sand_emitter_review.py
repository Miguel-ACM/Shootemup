"""Package terrain-bound emitter captures from actual Unity gameplay."""
from pathlib import Path
import shutil
import subprocess
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/DesertReviewProject/ArtSource/SandEmitterReview"
DEST = ROOT / "ArtSource/SandEmitterReview"
DEST.mkdir(parents=True, exist_ok=True)
names = ("Warning", "Emerging", "Exposed", "Burrowing")
for name in (*[n + ".png" for n in names], "Verification.txt"):
    shutil.copy2(SOURCE / name, DEST / name)

sheet = Image.new("RGB", (810, 1492), "#231c14")
draw = ImageDraw.Draw(sheet)
for i, name in enumerate(names):
    with Image.open(SOURCE / (name + ".png")) as image:
        w, h = image.size
        half = h * 405 / 720 / 2
        arena = image.crop((int(w / 2 - half), 0, int(w / 2 + half), h)).resize((405, 720))
    x, y = i % 2 * 405, i // 2 * 746
    sheet.paste(arena, (x, y + 26))
    draw.text((x + 10, y + 7), name, fill="#e5cf9e")
sheet.save(DEST / "EmergenceReview.jpg", quality=94)

started = (SOURCE / "Warning.png").stat().st_mtime
manifest = []
last = None
for budget in (2, 10, 20):
    frames = sorted(p for p in (SOURCE / "Frames").glob(f"Wave{budget}-*.png") if p.stat().st_mtime >= started)
    for i, path in enumerate(frames):
        stamp = int(path.stem.split("-")[1]) / 100
        duration = int(frames[i + 1].stem.split("-")[1]) / 100 - stamp if i+1 < len(frames) else .12
        manifest += [f"file '{path.as_posix()}'", f"duration {duration:.4f}"]
        last = path
manifest += [f"file '{last.as_posix()}'"]
sequence = SOURCE / "Sequence.txt"
sequence.write_text("\n".join(manifest) + "\n")
subprocess.run([
    shutil.which("ffmpeg") or r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
    "-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(sequence),
    "-vf", "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30",
    "-c:v", "libx264", "-crf", "20", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(DEST / "GroundEncounter.mp4")
], check=True)
print(DEST)
