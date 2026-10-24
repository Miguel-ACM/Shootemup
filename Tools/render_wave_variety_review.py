"""Package the isolated Unity wave review's actual five-fps captures."""
from pathlib import Path
import shutil
import subprocess

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/FeedbackUnityProject/ArtSource/WaveVarietyReview"
DEST = ROOT / "ArtSource/WaveVarietyReview"
LEVELS = ("Fields", "Snow", "City", "Thunder", "Tunnel")


def main():
    report = (SOURCE / "Verification.txt").read_text(encoding="utf-8-sig")
    if not report.startswith("PASS\n"):
        raise RuntimeError("The Unity review must pass before packaging captures")
    DEST.mkdir(parents=True, exist_ok=True)
    (DEST / "Verification.txt").write_text(report, encoding="utf-8")
    sheet = Image.new("RGB", (1100, 1260), "#101819")
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 18)
    for column, level in enumerate(LEVELS):
        frames = sorted((SOURCE / f"{level}Wave8").glob("????.jpg"))
        draw.text((column * 220 + 10, 8), "PLAINS" if level == "Fields" else level.upper(), font=font, fill="white")
        for row, frame in enumerate((12, 22, 32)):
            picture = Image.open(frames[min(frame, len(frames) - 1)])
            w, h = picture.size
            half = h * 405 / 720 / 2
            picture = picture.crop((round(w / 2 - half), 0, round(w / 2 + half), h))
            picture = picture.resize((216, 384), Image.Resampling.LANCZOS)
            x, y = column * 220 + 2, row * 410 + 30
            sheet.paste(picture, (x, y))
            draw.text((x + 5, y + 386), f"{frame / 5:.1f}s", font=font, fill="#aab9b9")
    sheet.save(DEST / "Formations.jpg", quality=94)
    shutil.copyfile(SOURCE / "FieldsWave8/0022.jpg", DEST / "WaveWatcher.jpg")
    for name in ("FieldsWave8", "ThunderWave15"):
        subprocess.run([
            "C:/Program Files/ffmpeg/bin/ffmpeg.exe", "-hide_banner", "-loglevel", "error", "-y",
            "-framerate", "5", "-i", str(SOURCE / name / "%04d.jpg"),
            "-vf", "scale=1280:-2", "-c:v", "libx264", "-crf", "21", "-pix_fmt", "yuv420p",
            "-movflags", "+faststart", str(DEST / f"{name}.mp4"),
        ], check=True)


if __name__ == "__main__":
    main()
