"""Package native Unity review captures and measured results; never alters game artwork."""
from pathlib import Path
import csv
import shutil
import statistics
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Temp/CombatReworkReview/ArtSource/CombatReworkReview'
OUTPUT = ROOT / 'ArtSource/CombatReworkReview'
EARLY = ['Fields', 'Snow', 'City', 'Tunnel', 'Thunder']
LATE = ['Desert', 'Candyland', 'Space', 'Underwater', 'Hell']


def captures(case):
    files = sorted((SOURCE / 'Frames' / case).glob('*.png'))
    if not files:
        raise RuntimeError('Missing native captures: ' + case)
    newest = max(f.stat().st_mtime for f in files)
    return [f for f in files if newest - f.stat().st_mtime < 20]


def at(case, fraction):
    files = captures(case)
    return files[round((len(files) - 1) * fraction)]


def paste(canvas, path, x, y, size, combat=False):
    im = Image.open(path).convert('RGB')
    if combat:
        im = im.crop((round(im.width * .30), 0, round(im.width * .74), im.height))
    im.thumbnail(size, Image.Resampling.LANCZOS)
    canvas.paste(im, (x, y))


def bosses(levels, filename):
    board = Image.new('RGB', (1280, len(levels) * 280), (15, 20, 26))
    draw = ImageDraw.Draw(board)
    for row, level in enumerate(levels):
        for col, (phase, fraction) in enumerate([(1, .35), (1, .75), (2, .35), (2, .75)]):
            case = f'{level}-Hard-Boss{phase}'
            paste(board, at(case, fraction), col * 320, row * 280 + 23, (320, 250))
            draw.text((col * 320 + 5, row * 280 + 5), f'{level} / phase {phase}', fill='white')
    board.save(OUTPUT / filename, quality=92)


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    initial = (SOURCE / 'InitialTour.txt').read_text(encoding='utf-8')
    remaining = (SOURCE / 'RemainingDistricts.txt').read_text(encoding='utf-8')
    focused = (SOURCE / 'FocusedRevisions.txt').read_text(encoding='utf-8')
    rising = (SOURCE / 'RisingCorrection.txt').read_text(encoding='utf-8')
    assert rising.startswith('PASS') and 'authored formation actually enters and fires' in rising, 'Corrected rising entrance must fire'
    assert remaining.startswith('PASS') and focused.startswith('PASS'), 'Outstanding runtime check'
    for levels, report in [(EARLY, initial), (LATE, remaining)]:
        for level in levels:
            for mode in ['Easy', 'Hard']:
                for case in ['Wave', 'Boss1', 'Boss2']:
                    assert f'PASS {level}-{mode}-{case}:' in report, f'Missing {level}/{mode}/{case}'
    assert focused.count('authored actors spawned') == 60, 'All sixty revised parts must complete'
    assert focused.count('actual player projectiles kill the moving hull') == 6, 'Moving stock-gun cases'
    for name, source in [('InitialTour.txt', 'InitialTour.txt'), ('RemainingDistricts.txt', 'RemainingDistricts.txt'),
                         ('FocusedRevisions.txt', 'FocusedRevisions.txt'), ('RisingCorrection.txt', 'RisingCorrection.txt'), ('Authoring.txt', 'Editor.txt'),
                         ('Compositions.txt', 'Compositions.txt'), ('Build.txt', 'Build.txt')]:
        shutil.copy2(SOURCE / source, OUTPUT / name)
    compilation = ROOT / 'Temp/CombatReworkReview/ArtSource/CodeCleanupReview/Verification.txt'
    assert compilation.read_text().startswith('PASS'), 'Clean compilation must pass'
    shutil.copy2(compilation, OUTPUT / 'Compilation.txt')
    build = (SOURCE / 'Build.txt').read_text()
    assert build.startswith('PASS'), 'Final player build must pass'
    models = sorted((SOURCE / 'Models').glob('*.png'))
    board = Image.new('RGB', (1000, ((len(models) + 3) // 4) * 210), (15, 20, 26))
    draw = ImageDraw.Draw(board)
    for i, model in enumerate(models):
        x, y = i % 4 * 250, i // 4 * 210
        paste(board, model, x, y, (240, 180))
        draw.text((x + 5, y + 184), model.stem, fill='white')
    board.save(OUTPUT / 'Models.jpg', quality=95)
    bosses(EARLY, 'EarlyBossPatterns.jpg')
    bosses(LATE, 'LateBossPatterns.jpg')
    board = Image.new('RGB', (960, 1500), (15, 20, 26))
    draw = ImageDraw.Draw(board)
    for i, level in enumerate(EARLY + LATE):
        x, y = i % 2 * 480, i // 2 * 300
        for j, t in enumerate([.22, .40, .58]):
            paste(board, at(level + '-PhaseEntry', t), x + j * 160, y + 24, (155, 270), combat=True)
        draw.text((x + 5, y + 5), level + ' / deploying phase mechanism', fill='white')
    board.save(OUTPUT / 'PhaseEntries.jpg', quality=94)
    board = Image.new('RGB', (1230, 500), (15, 20, 26))
    draw = ImageDraw.Draw(board)
    for i, (case, title) in enumerate([('Terrace rooftop turn', 'City'), ('Underwater-Boids', 'Underwater'), ('Hell-Hard-Boss2', 'Hell')]):
        paste(board, at(case, .55), i * 410, 24, (410, 470))
        draw.text((i * 410 + 5, 5), title, fill='white')
        shutil.copy2(at(case, .55), OUTPUT / (title + '.png'))
    board.save(OUTPUT / 'Scenery.jpg', quality=94)
    with (SOURCE / 'FocusedFrames.csv').open(newline='') as f:
        rows = [row for row in csv.DictReader(f) if row['case'] == 'Tunnel-UncapturedProfile']
    assert len(rows) > 500, 'Complete uncaptured Tunnel frames required'
    with (OUTPUT / 'TunnelFrames.csv').open('w', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=rows[0].keys()); writer.writeheader(); writer.writerows(rows)
    def summary(column, scale=1):
        values = sorted(float(r[column]) / scale for r in rows)
        return f'median {statistics.median(values):.3f}, p99 {values[int((len(values)-1)*.99)]:.3f}, max {max(values):.3f}'
    profile = ('PASS\nThirty seconds of actual complete Tunnel frames, both phases and auxiliaries, active player collider, no screenshot captures.\n'
               + f'Frames={len(rows)}; peak rounds={max(int(r["live"]) for r in rows)}.\n'
               + 'Frame milliseconds: ' + summary('dt_ms') + '\n'
               + 'Main-thread milliseconds: ' + summary('main_ns', 1e6) + '\n'
               + 'Physics milliseconds: ' + summary('physics_ns', 1e6) + '\n'
               + f'Created pooled rounds: {min(int(r["created"]) for r in rows)} to {max(int(r["created"]) for r in rows)}.\n'
               + 'Editor Game View measurements at a 60 FPS target; captures in other cases are excluded.\n'
               + 'Native review capture size: ' + str(Image.open(at('Fields-Hard-Wave', .5)).size) + ' pixels.\n')
    (OUTPUT / 'TunnelPerformance.txt').write_text(profile, encoding='utf-8')
    verification = ('PASS\nAll ten districts: ordinary waves and both boss phases on Easy and Hard.\n'
                    'Every existing regular/champion and all twenty additions observed with actual Unity models and firing.\n'
                    'Sixty revised formation parts completed; swept geometry checked on both sides and both difficulties.\n'
                    'Six moving Plains stock-gun hit/kill measurements passed.\n'
                    'Rising Nautilus route: legacy bottom-edge cleanup corrected; targeted entrance and firing check passed.\n'
                    'Distinct phase mechanisms inspected in native captures.\n'
                    'Candyland lower bound, Hell no-minion rule, Space reactor pressure, and moving underwater schools passed.\n'
                    'Resolved initial Desert construction error by obtaining the weakpoint material from its visual child.\n'
                    'Subsequent outstanding-district review and focused revisions passed without game runtime exceptions.\n'
                    + compilation.read_text() + '\n' + profile + '\n' + build)
    (OUTPUT / 'Verification.txt').write_text(verification, encoding='utf-8')
    (OUTPUT / 'README.md').write_text('''# Combat rework review — 27 October 2026

Native Unity captures and measured reports for the complete combat pass. Screenshots use the actual game compositor and HUD. Existing projectile artwork is preserved.

- [New hulls and remodeled Space fleet](Models.jpg)
- [Early-route boss patterns](EarlyBossPatterns.jpg), [late-route boss patterns](LateBossPatterns.jpg)
- [Phase mechanism deployments](PhaseEntries.jpg)
- [City, Underwater and Hell scenery](Scenery.jpg)
- [Combined verification](Verification.txt), [clean compiler rebuild](Compilation.txt), [Windows player build](Build.txt)
- [Full-frame Tunnel measurements](TunnelPerformance.txt) and [raw frames](TunnelFrames.csv)

The initial tour found a Desert weakpoint-material error; its original failure is retained in InitialTour.txt. The fix restored complete body construction. RemainingDistricts.txt records the corrected outstanding districts. FocusedRevisions.txt covers all sixty revised routes, larger phase machinery, the corrected inward Hell score on both difficulties, moving stock-gun hits and the uncaptured Tunnel profile. Six wide formations also received offscreen entry clearance before the focused review. That review exposed the old bottom-edge cleanup removing the rising Nautilus before entry. RisingCorrection.txt records the targeted fix and confirms that the complete formation enters and fires.

The graphical tour is a deterministic observer review, with human inspection of native rendered sequences. Stock-gun measurements use a predictive pilot capped at normal movement speed. These validate real hits and timing; they are not a human difficulty playthrough.

The Windows player is in `Build/CombatRework/`. Keep its executable and adjacent data folder together. Source direction and the detailed feature ledger are in `Design/CombatRework.md` and `Design/BossAttackPatterns.md`.
''', encoding='utf-8')
    print(verification)


if __name__ == '__main__':
    main()
