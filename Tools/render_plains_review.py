"""Package the isolated Unity Plains review; preserve the actual capture timing."""
import csv
from pathlib import Path
import shutil
import subprocess

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/PlainsReviewProject/ArtSource/PlainsSceneryReview"
DEST = ROOT / "ArtSource/PlainsSceneryReview"
DEST.mkdir(parents=True, exist_ok=True)
for name in ("Geometry.txt", "Runtime.txt", "RuntimeWrap.png"):
    shutil.copy2(SOURCE / name, DEST / name)

seeds = (17381, 421, 9826)
sheet = Image.new("RGB", (384 * 3, 608 * 2), "#10191a")
draw = ImageDraw.Draw(sheet)
for row, kind in enumerate(("Bridge", "River")):
    for col, seed in enumerate(seeds):
        with Image.open(SOURCE / f"{kind}-{seed}.png") as image:
            sheet.paste(image, (col * 384, row * 608 + 32))
        draw.text((col * 384 + 12, row * 608 + 10), f"{kind} / seed {seed}", fill="#cbd3bc")
sheet.save(DEST / "SeedReview.jpg", quality=93)

frames = SOURCE / "Frames"
with (frames / "timing.csv").open() as handle:
    times = [float(row["seconds"]) for row in csv.DictReader(handle)]
manifest = []
for frame, timestamp in enumerate(times):
    duration = times[frame + 1] - timestamp if frame + 1 < len(times) else .07
    manifest += [f"file '{frame:03d}.png'", f"duration {duration:.4f}"]
manifest += [f"file '{len(times) - 1:03d}.png'"]
(frames / "sequence.txt").write_text("\n".join(manifest) + "\n")
subprocess.run([
    shutil.which("ffmpeg") or r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
    "-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0",
    "-i", str(frames / "sequence.txt"), "-vf", "fps=30", "-c:v", "libx264",
    "-crf", "19", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(DEST / "PlainsMotion.mp4")
], check=True)
shutil.copy2(frames / "020.png", DEST / "Gameplay.png")
shutil.copy2(SOURCE / "Bridge-9826.png", DEST / "Bridge.png")
filmstrip = Image.new("RGB", (640 * 3, 506 * 2), "#10191a")
draw = ImageDraw.Draw(filmstrip)
for index, frame in enumerate((0, 20, 40, 60, 80, 110)):
    x, y = (index % 3) * 640, (index // 3) * 506
    with Image.open(frames / f"{frame:03d}.png") as image:
        filmstrip.paste(image, (x, y + 26))
    draw.text((x + 12, y + 8), f"Gameplay / {times[frame]:.2f}s", fill="#cbd3bc")
filmstrip.save(DEST / "MotionReview.jpg", quality=90)
print(f"Saved seed sheet, gameplay stills and {times[-1]-times[0]:.2f}s of actual game motion to {DEST}")
