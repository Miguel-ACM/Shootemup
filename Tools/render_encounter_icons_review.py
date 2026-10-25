"""Package native editor-only UI previews; no gameplay recording or test run."""
from pathlib import Path
import shutil
import subprocess
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/DesertReviewProject/ArtSource/EncounterIconsReview"
DEST = ROOT / "ArtSource/EncounterIconsReview"
DEST.mkdir(parents=True, exist_ok=True)
report = (SOURCE / "Verification.txt").read_text()
if not report.startswith("PASS"):
    raise RuntimeError(report)
shutil.copy2(SOURCE / "Verification.txt", DEST / "Verification.txt")
pairs = ("Shop-MultShop", "Item-Health", "Challenge-Shop")
sheet = Image.new("RGB", (405 * 3, 746 * 2), "#17232e")
draw = ImageDraw.Draw(sheet)
manifest = []
for col, name in enumerate(pairs):
    for row, phase in enumerate(("Open", "Exit")):
        path = SOURCE / f"{name}-{phase}.png"
        shutil.copy2(path, DEST / path.name)
        with Image.open(path) as im:
            w,h = im.size
            half = h * 405 / 720 / 2
            arena = im.crop((int(w/2-half),0,int(w/2+half),h)).resize((405,720),Image.Resampling.NEAREST)
        x,y = col*405,row*746
        sheet.paste(arena,(x,y+26))
        draw.text((x+12,y+7),f"{name} / {phase}",fill="#e1e8df")
    frames = sorted((SOURCE / "Frames").glob(f"{name}-*.png"))
    if len(frames) != 19:
        raise RuntimeError(f"Missing UI animation poses: {name}")
    manifest += [f"file '{frames[0].as_posix()}'", "duration 0.8"]
    for frame in frames[:-1]:
        manifest += [f"file '{frame.as_posix()}'", "duration 0.025"]
    manifest += [f"file '{frames[-1].as_posix()}'", "duration 0.35"]
sheet.save(DEST / "ChoicePreview.jpg", quality=95)
manifest += [f"file '{frames[-1].as_posix()}'"]
sequence = SOURCE / "Sequence.txt"
sequence.write_text("\n".join(manifest)+"\n")
subprocess.run([
    shutil.which("ffmpeg") or r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
    "-y","-hide_banner","-loglevel","error","-f","concat","-safe","0","-i",str(sequence),
    "-vf","fps=60","-c:v","libx264","-crf","18","-pix_fmt","yuv420p","-movflags","+faststart",
    str(DEST / "ChoiceExitPreview.mp4")
],check=True)
(DEST / "README.md").write_text("""# Encounter sprites and choice exit

Five original 32 x 32 transparent pixel sprites drawn directly with Python/Pillow: gold coin shop, blue multiplier shop, green item crate, medkit and challenge crest. There are no encounter text labels in the game UI. Sprites use point sampling, no mipmaps and uncompressed textures, displayed at 96 UI units (64 screen pixels at the existing 480px gameplay height).

The chosen icon gives a small confirmation pop and rises as the panels fade. Other panels fade sooner and slide outward. The selected encounter opens only after the 0.45-second exit completes. The same exit works for district path selection while retaining district names. Pause stops the animation; UI textures are cached and there is no added combat update loop.

`IconSheet.png` shows the source art enlarged with nearest-neighbour sampling. `ChoicePreview.jpg` and `ChoiceExitPreview.mp4` were rendered by the actual Unity UI component in edit mode, over an existing clean gameplay capture. These are editor UI previews, not a new gameplay recording. No Play mode or gameplay test run was started for this change.

`Verification.txt` covers icon imports, text replacement, native UI animation poses and the fully hidden endpoint. Draw with `Tools/draw_encounter_icons.py`, render with `ReviewEncounterIcons.Run` in an isolated Unity project, and package with `Tools/render_encounter_icons_review.py`.
""")
print(DEST)
