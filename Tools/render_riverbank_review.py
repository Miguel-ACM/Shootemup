"""Package before/after captures and scrolling review from the isolated Unity project."""
from pathlib import Path
import shutil
import subprocess
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/DesertReviewProject/ArtSource/RiverbankReview"
DEST = ROOT / "ArtSource/RiverbankReview"
DEST.mkdir(parents=True, exist_ok=True)
for name in ("Baseline.txt", "Verification.txt"):
    report = (SOURCE / name).read_text()
    if not report.startswith("PASS"):
        raise RuntimeError(report)
    shutil.copy2(SOURCE / name, DEST / name)

stations = ("0.0", "18.0", "39.0", "71.0", "95.9")
sheet = Image.new("RGB", (270 * 5, 505 * 2), "#182622")
draw = ImageDraw.Draw(sheet)
for row, label in enumerate(("Before", "After")):
    for col, station in enumerate(stations):
        path = SOURCE / f"{label}-{station}.png"
        shutil.copy2(path, DEST / path.name)
        with Image.open(path) as im:
            w, h = im.size
            half = h * 405 / 720 / 2
            arena = im.crop((int(w/2-half), 0, int(w/2+half), h)).resize((270, 480))
        sheet.paste(arena, (col * 270, row * 505 + 25))
        draw.text((col * 270 + 10, row * 505 + 7), f"{label} / station {station}", fill="#e6ece7")
sheet.save(DEST / "RiverbankComparison.jpg", quality=96)

frames = sorted((SOURCE / "Frames").glob("River-*.png"))
if not frames:
    raise RuntimeError("No scrolling review frames")
motion = Image.new("RGB", (270 * 6, 505), "#182622")
draw = ImageDraw.Draw(motion)
for col in range(6):
    frame = frames[min(len(frames)-1, round(col / 5 * (len(frames)-1)))]
    with Image.open(frame) as im:
        w, h = im.size
        half = h * 405 / 720 / 2
        arena = im.crop((int(w/2-half), 0, int(w/2+half), h)).resize((270, 480))
    motion.paste(arena, (col * 270, 25))
    draw.text((col * 270 + 10, 7), f"Scroll / frame {frame.stem.split('-')[-1]}", fill="#e6ece7")
motion.save(DEST / "ScrollingReview.jpg", quality=96)
manifest = []
for frame in frames:
    manifest += [f"file '{frame.as_posix()}'", "duration 0.12"]
manifest += [f"file '{frames[-1].as_posix()}'"]
sequence = SOURCE / "Sequence.txt"
sequence.write_text("\n".join(manifest) + "\n")
subprocess.run([
    shutil.which("ffmpeg") or r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
    "-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(sequence),
    "-vf", "fps=30", "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart",
    str(DEST / "RiverbankMotion.mp4")
], check=True)
(DEST / "README.md").write_text("""# Plains riverbank review

Real Unity 6000.6.2f1 gameplay renders from the isolated review project, with the normal PSX filter and HUD. The active game editor was left alone.

The river now has periodic erosion scallops shared by the water and terrain meshes, sculpted grassy lips, sloped gravel shelves, dark wet margins, embedded pebbles and uneven clumps of modeled reeds. Fine gravel grain stays attached to the ground. These details share the existing section meshes and material; there are no added scenery objects, colliders or per-frame shore updates.

`RiverbankComparison.jpg` compares the same five terrain stations before and after. `RiverbankMotion.mp4` shows the new banks moving through the full 96-unit landscape wrap. Full-size individual captures preserve the original gameplay aspect ratio.

`Verification.txt` records shader compilation, bounded finite meshes, water/shore seams across four sections and three seeds, and scrolling loop checks. The road path and tank surface sampling are unchanged.

Reproduce in a separate Unity project using `VerifyRiverbanks.Baseline` before the change, then `VerifyRiverbanks.Run` with the new scenery, and package with `Tools/render_riverbank_review.py`.
""")
print(DEST)
