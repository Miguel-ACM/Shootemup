"""Preserve raw generator takes and request metadata beside each item iteration."""
import importlib.util
import json
from pathlib import Path
import shutil
import sys


ROOT = Path(__file__).resolve().parents[1]
TOOL = Path.home() / 'Tools/local-sounds'
sys.path.insert(0, str(TOOL))
spec = importlib.util.spec_from_file_location('local_sfx_cli', TOOL / 'scripts/sfx.py')
cli = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cli)
count = 0
for session in (ROOT / 'ArtSource/ItemEffectsAudioReview/Iterations').glob('*/iteration-*/session.json'):
    state = json.loads(session.read_text(encoding='utf-8'))
    for round_ in state.get('rounds', []):
        request_id = round_['request']
        record = cli.call('/api/requests/' + request_id)
        folder = session.parent / 'Originals' / request_id
        folder.mkdir(parents=True, exist_ok=True)
        (folder / 'request.json').write_text(json.dumps(record, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
        for take in record['takes']:
            raw = take.get('files', {}).get('raw')
            if not raw:
                raise ValueError('Missing raw take for ' + take['ref'])
            source = TOOL / 'output' / raw
            destination = folder / (str(take['seed']) + '_raw.wav')
            if not destination.exists():
                shutil.copyfile(source, destination)
            if destination.stat().st_size != source.stat().st_size:
                raise ValueError('Raw take copy incomplete: ' + take['ref'])
            count += 1
print(f'Preserved {count} raw takes with request metadata.')
