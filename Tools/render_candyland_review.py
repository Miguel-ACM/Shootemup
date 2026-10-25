"""Package actual filtered Candyland gameplay captures with recorded wave timing."""
from pathlib import Path
import shutil
import subprocess
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/DesertReviewProject/ArtSource/CandylandReview"
DEST = ROOT / "ArtSource/CandylandReview"
DEST.mkdir(parents=True, exist_ok=True)
for name in ("Geometry.txt", "Verification.txt", "Scenery.png", "BossPhase1.png", "BossPhase2.png", "RouteChoice.png"):
    shutil.copy2(SOURCE / name, DEST / name)
started = (SOURCE / "Scenery.png").stat().st_mtime
frames = SOURCE / "Frames"

def current(pattern):
    return sorted(p for p in frames.glob(pattern) if p.stat().st_mtime >= started)

chosen = [(SOURCE / "Scenery.png", "Pink cloud sky")]
for budget in (2, 10, 20):
    candidates = current(f"Wave{budget}-*.png")
    for fraction in (.25, .45):
        chosen.append((candidates[int(len(candidates) * fraction)], f"Wave {budget}"))
for phase in (0, 1):
    candidates = current(f"Boss{phase}-*.png")
    chosen.append((candidates[36], f"Regent / phase {phase+1}"))

sheet = Image.new("RGB", (405 * 3, 746 * 3), "#211421")
draw = ImageDraw.Draw(sheet)
for i, (path, label) in enumerate(chosen):
    with Image.open(path) as image:
        w, h = image.size
        half = h * 405 / 720 / 2
        arena = image.crop((int(w / 2 - half), 0, int(w / 2 + half), h)).resize((405, 720))
    x, y = i % 3 * 405, i // 3 * 746
    sheet.paste(arena, (x, y + 26))
    draw.text((x + 10, y + 7), label, fill="#ecd1dc")
sheet.save(DEST / "MotionReview.jpg", quality=94)

manifest = []
for budget in (2, 10, 20):
    candidates = current(f"Wave{budget}-*.png")
    for i, path in enumerate(candidates):
        stamp = int(path.stem.split("-")[1]) / 100
        duration = int(candidates[i+1].stem.split("-")[1]) / 100 - stamp if i+1 < len(candidates) else .25
        manifest += [f"file '{path.as_posix()}'", f"duration {duration:.4f}"]
for phase in (0, 1):
    for path in current(f"Boss{phase}-*.png"):
        manifest += [f"file '{path.as_posix()}'", "duration 0.25"]
manifest += [f"file '{(frames / 'Boss1-051.png').as_posix()}'"]
sequence = SOURCE / "Sequence.txt"
sequence.write_text("\n".join(manifest) + "\n")
subprocess.run([
    shutil.which("ffmpeg") or r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
    "-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(sequence),
    "-vf", "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30",
    "-c:v", "libx264", "-crf", "20", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(DEST / "CandylandMotion.mp4")
], check=True)
print(DEST)
