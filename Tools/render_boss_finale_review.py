"""Package the batched boss review and its focused final-district continuation."""
from pathlib import Path
import shutil
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/DesertReviewProject/ArtSource/BossFinaleReview"
DEST = ROOT / "ArtSource/BossFinaleReview"
DEST.mkdir(parents=True, exist_ok=True)
baseline = (SOURCE / "Baseline.txt").read_text()
batch = (SOURCE / "Verification.txt").read_text()
loops = (SOURCE / "LoopsVerification.txt").read_text()
if not baseline.startswith("PASS") or not loops.startswith("PASS"):
    raise RuntimeError("Missing passing reproduction or focused continuation")
required = [f"{name}: path choice opens only after collecting the item" for name in ("City-FastFirst", "City-HeavyFirst", "Fields", "Snow", "Thunder", "Tunnel")]
required += [f"{name}: the final-district loop waits for the item and resumes cleanly" for name in ("Desert", "Candyland")]
for check in required:
    if "PASS " + check not in batch + loops:
        raise RuntimeError(f"Missing required integration result: {check}")
shutil.copy2(SOURCE / "Baseline.txt", DEST / "Before-CityLock.txt")
(DEST / "InitialBatch.txt").write_text("\n".join(line.rstrip() for line in batch.splitlines()) + "\n")
shutil.copy2(SOURCE / "LoopsVerification.txt", DEST / "FinalDistrictChecks.txt")
passed = list(dict.fromkeys(line for line in (batch + "\n" + loops).splitlines() if line.startswith("PASS ")))
(DEST / "Verification.txt").write_text("PASS combined batched review and focused final-district checks\n" + "\n".join(passed) + "\n")

names = ("City-FastFirst-Freeze", "City-HeavyFirst-Freeze", "Desert-Freeze", "City-FastFirst-Item", "City-FastFirst-Route", "Candyland-Item")
sheet = Image.new("RGB", (405 * 3, 746 * 2), "#18232c")
draw = ImageDraw.Draw(sheet)
for index, name in enumerate(names):
    path = SOURCE / f"{name}.png"
    shutil.copy2(path, DEST / path.name)
    with Image.open(path) as im:
        w, h = im.size
        half = h * 405 / 720 / 2
        arena = im.crop((int(w/2-half), 0, int(w/2+half), h)).resize((405, 720))
    x, y = index % 3 * 405, index // 3 * 746
    sheet.paste(arena, (x, y + 26))
    draw.text((x + 12, y + 7), name.replace("-", " / "), fill="#e5e9e6")
sheet.save(DEST / "BossFinaleReview.jpg", quality=95)
for name in ("Before-CityLock", "Snow-Freeze", "Thunder-Freeze", "Tunnel-Freeze", "Candyland-Freeze"):
    shutil.copy2(SOURCE / f"{name}.png", DEST / f"{name}.png")
(DEST / "README.md").write_text("""# Boss finales and City encounter completion

Actual Unity 6000.6.2f1 captures from an isolated project. The active game was left alone.

## City progression fix

The fast ship was registered with the encounter controller, but its spawned heavy partner was not. A fast-first kill order therefore left the surviving heavy ship unable to complete the district. Both ships now share the encounter owner. Both kill orders were checked through survivor healing, final destruction, mandatory item pickup and path selection.

## Finale

The intact boss hull remains visible through five pre-explosion accents and two short authored simulation holds. A final half-second hold freezes the arena before shattering. Music stops exactly on the breakup; prepared polygon wreckage is released together with a larger explosion and shockwaves. Whole-body wreckage includes the Desert worm's articulated sections and excludes extinguished engine flames. Existing explosion samples are reused.

After the main blast opens, one random item falls at the wreck's position, clamped into collectible screen bounds. It has no SKIP action. The real item pickup callback unlocks the next path, or the existing final-district loop to Plains. Full weapon/active slots still use the established replacement controls. The first City ship and miniature saws do not trigger victory rewards.

## Verification scope

`Before-CityLock.txt` records the reproduced lock. `InitialBatch.txt` preserves the complete first review, including passing checks for both City orders, Plains, Snow, Thunder and Tunnel, and the Desert finale/item gate. That batch stopped at the Desert return because the old test harness loaded L1 directly and left the title's parked preload blocking speculative route-music loading. The harness now uses the real cloud-transition entry. Only Desert and Candyland were rerun; both passed their reward-gated returns (`FinalDistrictChecks.txt`). `Verification.txt` combines the completed checks and asserts every district's required outcome.

The checks also cover pause/resume during the final hold, cancelling active Bullet Time, intact hull visibility, prepared wreckage timing, exact one-item drops, no bottom-edge bypass, occupied-slot replacements, and music cut timing. The six-panel gallery shows the held models and the item/route ordering; it is not an audio listening review.

Reproduce a full batch with `VerifyBossFinale.Run`; use `VerifyBossFinale.RunRemaining` for the focused final-district continuation. Package with `Tools/render_boss_finale_review.py`.
""")
print(DEST)
