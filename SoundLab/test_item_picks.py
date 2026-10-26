import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import wave

spec=importlib.util.spec_from_file_location('item_import',Path(__file__).with_name('apply_item_picks.py'))
item_import=importlib.util.module_from_spec(spec);spec.loader.exec_module(item_import)

class ItemApprovalTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        public=self.root/'SoundLab/public';public.mkdir(parents=True)
        self.audio=public/'take.wav'
        with wave.open(str(self.audio),'wb') as audio:
            audio.setparams((1,2,48000,0,'NONE','not compressed'));audio.writeframes(b'\0\0'*100)
        self.manifest={'bankVersion':'sample-round-1','cues':[{'id':'itemSplit','unityCue':'itemSplit','source':item_import.SOURCE,
            'choices':[{'id':'a','file':'take.wav','sha256':item_import.sha(self.audio)}]}]}
        self.picks={'bankVersion':'sample-round-1','status':'ready','picks':{'itemSplit':{'choice':'a','gain':.7},'shot1':{'choice':'c','gain':1}}}
        self.save()
    def save(self):
        (self.root/'SoundLab/public/manifest.json').write_text(json.dumps(self.manifest),encoding='utf-8')
        (self.root/'SoundLab/selections.json').write_text(json.dumps(self.picks),encoding='utf-8')
    def test_draft_cannot_import(self):
        self.picks['status']='draft';self.save()
        with self.assertRaisesRegex(ValueError,'Finish selection'): item_import.prepare(self.root)
        self.assertFalse((self.root/'Assets').exists())
    def test_approved_take_and_level_only(self):
        picks=self.root/'SoundLab/selections.json';before=picks.read_bytes()
        targets=item_import.prepare(self.root)
        self.assertEqual([t['cue'] for t in targets],['itemSplit'])
        item_import.apply(self.root,targets)
        output=self.root/'Assets/Resources/Sounds/ItemApproved/itemSplit.wav'
        self.assertEqual(output.read_bytes(),self.audio.read_bytes())
        self.assertEqual(json.loads((self.root/'Assets/Resources/ItemSoundGains.json').read_text())['cues'],[{'cue':'itemSplit','gain':.7}])
        self.assertEqual(picks.read_bytes(),before)
        self.assertFalse((output.parent/'shot1.wav').exists())
    def test_reject_pending_original_leave_current_untouched(self):
        for choice in ['pending','reject','original']:
            self.picks['picks']['itemSplit']['choice']=choice;self.save()
            self.assertEqual(item_import.prepare(self.root),[])
        self.assertFalse((self.root/'Assets').exists())
    def test_hash_failure_prevents_any_import(self):
        self.audio.write_bytes(b'changed')
        with self.assertRaisesRegex(ValueError,'hash mismatch'): item_import.prepare(self.root)
        self.assertFalse((self.root/'Assets').exists())
    def test_path_cannot_escape_public(self):
        self.manifest['cues'][0]['choices'][0]['file']='../../private.wav';self.save()
        with self.assertRaisesRegex(ValueError,'source path'): item_import.prepare(self.root)
    def test_replacement_keeps_old_take_and_guid(self):
        targets=item_import.prepare(self.root);item_import.apply(self.root,targets)
        output=self.root/'Assets/Resources/Sounds/ItemApproved/itemSplit.wav'
        metadata=Path(str(output)+'.meta');guid=metadata.read_bytes();old=output.read_bytes()
        output.write_bytes(b'previous chosen take')
        item_import.apply(self.root,targets)
        self.assertEqual(metadata.read_bytes(),guid)
        archives=list((self.root/'ArtSource/ItemEffectsAudioReview/Applied').glob('*/itemSplit.previous.wav'))
        self.assertEqual(len(archives),1)
        self.assertEqual(archives[0].read_bytes(),b'previous chosen take')
        self.assertEqual(output.read_bytes(),old)

if __name__=='__main__': unittest.main()
