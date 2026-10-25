"""Package real Plains tank and Snow auger gameplay from the isolated Unity review."""
from pathlib import Path
import shutil
import subprocess
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/DesertReviewProject/ArtSource/GroundWavesReview"
DEST = ROOT / "ArtSource/GroundWavesReview"
DEST.mkdir(parents=True, exist_ok=True)
for name in ("Verification.txt", "Composition.txt"):
    shutil.copy2(SOURCE / name, DEST / name)
for level in ("Fields", "Snow"):
    for label in ("ConvoyA", "ConvoyB", "MixedEarly", "MixedLate"):
        name = f"{level}-{label}.png"
        shutil.copy2(SOURCE / name, DEST / name)

sheet = Image.new("RGB", (405 * 4, 746 * 2), "#151f23")
draw = ImageDraw.Draw(sheet)
manifest = []
last = None
started = (SOURCE / "Composition.txt").stat().st_mtime
for row, level in enumerate(("Fields", "Snow")):
    for col, label in enumerate(("ConvoyA", "ConvoyB", "MixedEarly", "MixedLate")):
        path = SOURCE / f"{level}-{label}.png"
        with Image.open(path) as im:
            w, h = im.size
            half = h * 405 / 720 / 2
            arena = im.crop((int(w/2-half), 0, int(w/2+half), h)).resize((405, 720))
        x, y = col * 405, row * 746
        sheet.paste(arena, (x, y + 26))
        draw.text((x + 10, y + 7), f"{level} / {label}", fill="#d9e0dc")
        frames = sorted(p for p in (SOURCE / "Frames").glob(f"{level}-{label}-*.png") if p.stat().st_mtime >= started)
        for i, frame in enumerate(frames):
            stamp = int(frame.stem.split("-")[-1]) / 100
            duration = int(frames[i+1].stem.split("-")[-1]) / 100 - stamp if i+1 < len(frames) else .16
            manifest += [f"file '{frame.as_posix()}'", f"duration {duration:.4f}"]
            last = frame
sheet.save(DEST / "MotionReview.jpg", quality=94)
manifest += [f"file '{last.as_posix()}'"]
sequence = SOURCE / "Sequence.txt"
sequence.write_text("\n".join(manifest) + "\n")
subprocess.run([
    shutil.which("ffmpeg") or r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
    "-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(sequence),
    "-vf", "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30",
    "-c:v", "libx264", "-crf", "20", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(DEST / "GroundWavesMotion.mp4")
], check=True)
print(DEST)
