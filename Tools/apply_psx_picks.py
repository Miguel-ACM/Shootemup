"""Apply Miguel's PSX LoRA picks (Sound Picks artifact, db `picks3`) to the game.

    python Tools/apply_psx_picks.py [--dry-run]

Each picked take is copied to Assets/Resources/Sounds/Selected/<cue>_psx.wav and assigned
as that cue's SfxMix override, the same route the SFX Preview window's "Use" button takes,
so it wins over the synth bank, ItemApproved and legacy clips. Existing entries keep their
gain, mute and order; only `clip` and `choice` change. "current" picks are left alone.
"""
import argparse
import json
from pathlib import Path
import re
import shutil
import uuid

ROOT = Path(__file__).resolve().parents[1]
REVIEW = ROOT / 'ArtSource/AudioReview/PSXLoRA'
SELECTED = ROOT / 'Assets/Resources/Sounds/Selected'
ASSET = ROOT / 'Assets/Resources/SfxMix.asset'
AUDIO_FILE_ID = 8300000
META = ('fileFormatVersion: 2\nguid: {guid}\nAudioImporter:\n  externalObjects: {{}}\n  serializedVersion: 7\n'
        '  defaultSettings:\n    serializedVersion: 2\n    loadType: 0\n    sampleRateSetting: 0\n'
        '    sampleRateOverride: 44100\n    compressionFormat: 1\n    quality: 1\n    conversionMode: 0\n'
        '    preloadAudioData: 1\n  platformSettingOverrides: {{}}\n  forceToMono: 1\n  normalize: 0\n'
        '  loadInBackground: 0\n  ambisonic: 0\n  3D: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')


def guid_of(path):
    meta = Path(str(path) + '.meta')
    if meta.exists():
        return re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8'), re.M).group(1)
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'shootemup/psx-picks/' + path.name).hex
    meta.write_text(META.format(guid=guid), encoding='utf-8', newline='\n')
    return guid


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--dry-run', action='store_true')
    args = parser.parse_args()
    picks = json.loads((REVIEW / 'picks.json').read_text(encoding='utf-8'))
    cues = {c['id']: c for c in json.loads((REVIEW / 'cues.json').read_text(encoding='utf-8'))}
    text = ASSET.read_text(encoding='utf-8')
    newline = '\r\n' if '\r\n' in text else '\n'
    text = text.replace('\r\n', '\n')
    head, entries = text.split('  entries:\n', 1)
    blocks = re.findall(r'  - cue: .*?(?=\n  - cue: |\Z)', entries.rstrip('\n'), re.S)
    by_cue = {re.match(r'  - cue: (\S*)', b).group(1): i for i, b in enumerate(blocks)}
    applied = []
    for cue, choice in picks.items():
        if choice == 'current':
            continue
        take = cues[cue]['options'][choice]
        target = SELECTED / f'{cue}_psx.wav'
        if not args.dry_run:
            SELECTED.mkdir(parents=True, exist_ok=True)
            shutil.copy2(REVIEW / 'audio' / take['file'], target)
        guid = guid_of(target) if not args.dry_run else '?' * 32
        clip = f'{{fileID: {AUDIO_FILE_ID}, guid: {guid}, type: 3}}'
        tag = f'psx{choice}'
        if cue in by_cue:
            b = blocks[by_cue[cue]]
            b = re.sub(r'(\n    clip: ).*', lambda m: m.group(1) + clip, b, count=1)
            b = re.sub(r'(\n    choice:).*', lambda m: m.group(1) + ' ' + tag, b, count=1)
            blocks[by_cue[cue]] = b
        else:
            blocks.append(f'  - cue: {cue}\n    clip: {clip}\n    gain: 1\n    muted: 0\n    choice: {tag}')
            by_cue[cue] = len(blocks) - 1
        applied.append(f'{cue} <- {take["file"]}')
    out = head + '  entries:\n' + '\n'.join(blocks) + '\n'
    if not args.dry_run:
        ASSET.write_text(out.replace('\n', newline), encoding='utf-8', newline='')
    print('\n'.join(applied))
    print(f'{len(applied)} cues {"would be" if args.dry_run else ""} assigned')


if __name__ == '__main__':
    main()
