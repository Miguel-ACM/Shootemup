"""Side-by-side boss phase videos: current cards (left) against proposed cards (right).

Clips come from RecordBossPhaseProposals (Assets/Editor/Reviews) in the isolated review
checkout, already cropped to the play field. Each pair is labelled and written to
ArtSource/BossPhaseProposals/Videos/<clip>.mp4, with an index.html beside them.
Usage: python Tools/compose_boss_proposals.py [clip-filter]
"""
import html
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
CLIPS = ROOT / "Build/ReviewCache/Temp/DecisionHoverReview/ArtSource/BossPhaseProposals/Clips"
OUT = ROOT / "ArtSource/BossPhaseProposals"
VIDEOS = OUT / "Videos"
FFMPEG = r"C:\Program Files\ffmpeg\bin\ffmpeg.exe"
NAMES = {
    "FieldsBomber": "Plains bomber", "PlainsRedKite": "Plains Red Kite", "PlainsThreshingDrum": "Plains Threshing Drum",
    "SnowSiege": "Snow fortress", "SnowIcebreaker": "Snow Icebreaker", "CityDuo": "City duo", "CitySwitchyard": "City Switchyard",
    "ThunderLance": "Thunder", "TunnelSaw": "Tunnel saw", "DesertWorm": "Desert worm", "CandyRegent": "Candyland regent",
    "SpaceCitadel": "Space black hole", "AbyssSubmarine": "Underwater submarine", "HellCrab": "Hell crab",
    "SeraphJudicator": "Heaven seraph", "VoidSovereign": "Void sovereign",
}


def font(size):
    for name in ("consolab.ttf", "consola.ttf", "arialbd.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def title(clip):
    level, boss, phase = clip.rsplit("-", 2)
    return f"{NAMES.get(boss, boss)}  -  phase {phase}"


def header(clip, width, half):
    image = Image.new("RGB", (width, 64), (14, 16, 22))
    draw = ImageDraw.Draw(image)
    draw.text((width // 2, 6), title(clip), font=font(24), fill=(240, 236, 220), anchor="mt")
    draw.text((half // 2, 38), "CURRENT", font=font(20), fill=(170, 176, 190), anchor="mt")
    draw.text((half + half // 2, 38), "PROPOSED", font=font(20), fill=(255, 206, 110), anchor="mt")
    path = VIDEOS / f".{clip}-header.png"
    image.save(path)
    return path


def compose(clip):
    current, proposed = CLIPS / "current" / f"{clip}.mp4", CLIPS / "proposed" / f"{clip}.mp4"
    probe = subprocess.run([FFMPEG.replace("ffmpeg.exe", "ffprobe.exe"), "-v", "error", "-select_streams", "v:0",
                            "-show_entries", "stream=width", "-of", "csv=p=0", str(current)],
                           capture_output=True, text=True, check=True)
    half = int(probe.stdout.strip())
    banner = header(clip, half * 2, half)
    graph = "[0:v][1:v]hstack=2:shortest=1[pair];[2:v][pair]vstack=2:shortest=1,format=yuv420p[out]"
    target = VIDEOS / f"{clip}.mp4"
    subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-i", str(current), "-i", str(proposed),
                    "-loop", "1", "-framerate", "30", "-i", str(banner), "-filter_complex", graph, "-map", "[out]",
                    "-c:v", "libx264", "-crf", "22", "-preset", "medium", str(target)], check=True)
    banner.unlink()
    return target


def index(clips):
    ideas = OUT / "Ideas"
    rows = []
    for clip in clips:
        rows.append(f'<section><h2>{html.escape(title(clip))}</h2>'
                    f'<video src="Videos/{clip}.mp4" controls loop muted preload="metadata"></video></section>')
    notes = "".join(f'<li><a href="Ideas/{p.name}">{html.escape(p.stem)}</a></li>' for p in sorted(ideas.glob("*.md")))
    page = f"""<!doctype html><meta charset="utf-8"><title>Boss phase proposals</title>
<style>body{{background:#101218;color:#ece8da;font:15px system-ui;margin:24px}}section{{margin:0 0 32px}}
video{{width:min(100%,1080px)}}h2{{font-weight:600;margin:0 0 8px}}a{{color:#ffce6e}}</style>
<h1>Boss phase proposals</h1><p>Left: current cards. Right: proposed cards. Normal difficulty; phase 3 is Hard's, previewed.</p>
<p>Design notes per boss:</p><ul>{notes}</ul>{''.join(rows)}"""
    (OUT / "index.html").write_text(page, encoding="utf-8")


def main():
    VIDEOS.mkdir(parents=True, exist_ok=True)
    wanted = sys.argv[1] if len(sys.argv) > 1 else ""
    clips = [p.stem for p in sorted((CLIPS / "current").glob("*.mp4"))
             if (CLIPS / "proposed" / p.name).exists() and wanted.lower() in p.stem.lower()]
    order = {level: i for i, level in enumerate(["Fields", "Tunnel", "Snow", "Thunder", "City", "Desert", "Candyland",
                                                  "Space", "Underwater", "Hell", "Heaven", "Void"])}
    clips.sort(key=lambda c: (order.get(c.split("-")[0], 99), c))
    for clip in clips:
        print("composing", clip, flush=True)
        compose(clip)
    done = sorted((p.stem for p in VIDEOS.glob("*.mp4")), key=lambda c: (order.get(c.split("-")[0], 99), c))
    index(done)
    print(f"{len(clips)} videos written to {VIDEOS}")


if __name__ == "__main__":
    main()
