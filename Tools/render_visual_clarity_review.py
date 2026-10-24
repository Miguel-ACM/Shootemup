"""Package the actual Unity projectile and smoothed-formation review captures."""
from pathlib import Path
import shutil
import subprocess

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/FeedbackUnityProject/ArtSource/VisualClarityReview"
WAVES = ROOT / "Temp/FeedbackUnityProject/ArtSource/WaveVarietyReview"
DEST = ROOT / "ArtSource/VisualClarityReview"
FONT = "C:/Windows/Fonts/consola.ttf"
STYLES = [
    ("SimpleRound", "SHOT"), ("NeedleRound", "NEEDLE"), ("HelixRound", "HELIX"), ("BurstShell", "BURST"),
    ("RicochetBolt", "BOUNCE"), ("SuspendedRound", "DELAY"), ("GuidedMissile", "GUIDED"), ("PlasmaOrb", "PLASMA"),
    ("ProximityMine", "MINE"), ("ReturningDisc", "RETURN"), ("ScatterCarrier", "SCATTER"), ("LaserSideRound", "LASER SIDE"),
]


def main():
    for name in ("Verification.txt", "WaveVerification.txt"):
        if not (SOURCE / name).read_text(encoding="utf-8-sig").startswith("PASS"):
            raise RuntimeError(f"Unity review must pass: {name}")
    DEST.mkdir(parents=True, exist_ok=True)
    for name in ("Verification.txt", "WaveVerification.txt", "BulletGallery.jpg"):
        shutil.copyfile(SOURCE / name, DEST / name)
    atlas = Image.new("RGB", (960, 660), "#263437")
    draw = ImageDraw.Draw(atlas)
    font = ImageFont.truetype(FONT, 20)
    for i, (kind, label) in enumerate(STYLES):
        picture = Image.open(SOURCE / "Sprites" / f"{kind}.png").convert("RGBA")
        xscale, yscale = (.55, 1.9) if kind == "NeedleRound" else (.7, 1.6) if kind == "GuidedMissile" else (1, 1)
        picture = picture.resize((round(96 * xscale), round(96 * yscale)), Image.Resampling.NEAREST)
        x, y = i % 4 * 240, i // 4 * 220
        atlas.paste(picture, (x + (240 - picture.width) // 2, y + (190 - picture.height) // 2), picture)
        draw.text((x + 18, y + 193), label, font=font, fill="#e1e9df")
    atlas.save(DEST / "BulletStyles.png")
    # A filmstrip makes the warning's radius and its timing inspectable without playback.
    strip = Image.new("RGB", (960, 680), "#101819")
    draw = ImageDraw.Draw(strip)
    for row, (name, positions) in enumerate((("Shell", (.70, .84, .97)), ("Laser", (.27, .38, .52)))):
        frames = sorted((SOURCE / name).glob("????.jpg"))
        for column, fraction in enumerate(positions):
            picture = Image.open(frames[min(len(frames) - 1, int(len(frames) * fraction))])
            w, h = picture.size
            if name == "Shell":
                half = h * .105
                picture = picture.crop((w / 2 - half, h * .386 - half, w / 2 + half, h * .386 + half))
            else:
                picture = picture.crop((w / 2 - h * .10, h * .25, w / 2 + h * .10, h * .65))
            picture.thumbnail((300, 300), Image.Resampling.LANCZOS)
            strip.paste(picture, (column * 320 + (320 - picture.width) // 2, row * 340 + 28))
        draw.text((12, row * 340 + 5), "CONVERGING SPARKS" if row == 0 else "SIDE OUTLETS / VOLLEY", font=font, fill="white")
    strip.save(DEST / "Warnings.jpg", quality=94)
    motion = Image.new("RGB", (1100, 1260), "#101819")
    draw = ImageDraw.Draw(motion)
    for col, level in enumerate(("Fields", "Snow", "City", "Thunder", "Tunnel")):
        draw.text((col * 220 + 5, 5), "PLAINS" if level == "Fields" else level.upper(), font=font, fill="white")
        frames = sorted((WAVES / f"{level}Wave8").glob("????.jpg"))
        for row, frame in enumerate((12, 22, 32)):
            picture = Image.open(frames[min(frame, len(frames) - 1)])
            w, h = picture.size
            half = h * 405 / 720 / 2
            picture = picture.crop((round(w / 2 - half), 0, round(w / 2 + half), h)).resize((216, 384), Image.Resampling.LANCZOS)
            motion.paste(picture, (col * 220 + 2, row * 410 + 30))
            draw.text((col * 220 + 5, row * 410 + 416), f"{frame / 5:.1f}s", font=font, fill="#aab9b9")
    motion.save(DEST / "SmoothedFormations.jpg", quality=94)
    for name, fps in (("Helix", 12), ("Shell", 15), ("Laser", 15)):
        subprocess.run([
            "C:/Program Files/ffmpeg/bin/ffmpeg.exe", "-hide_banner", "-loglevel", "error", "-y",
            "-framerate", str(fps), "-i", str(SOURCE / name / "%04d.jpg"),
            "-vf", "crop=ih*0.5625:ih:(iw-ih*0.5625)/2:0,scale=360:-2",
            "-c:v", "libx264", "-crf", "19", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(DEST / f"{name}.mp4"),
        ], check=True)


if __name__ == "__main__":
    main()
