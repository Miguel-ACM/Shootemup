"""Focused checks for candidate integrity and the project selection contract.

Run: python -m unittest discover -s SoundLab -p test_sound_lab.py
Uses isolated temporary selection files, never the user's actual choices.
"""
import array
import hashlib
import json
from pathlib import Path
import tempfile
import threading
import unittest
import urllib.error
import urllib.request
import wave

import server


class SelectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp=tempfile.TemporaryDirectory()
        server.SELECTIONS=Path(cls.temp.name)/'choices.json'
        cls.http=server.ThreadingHTTPServer(('127.0.0.1',0),server.Handler)
        cls.url=f'http://127.0.0.1:{cls.http.server_port}'
        cls.thread=threading.Thread(target=cls.http.serve_forever,daemon=True);cls.thread.start()
        cls.manifest=json.loads((server.PUBLIC/'manifest.json').read_text(encoding='utf-8'))
    @classmethod
    def tearDownClass(cls):
        cls.http.shutdown();cls.http.server_close();cls.thread.join();cls.temp.cleanup()
    def request(self,path,value=None,headers=None):
        request=urllib.request.Request(self.url+path,data=json.dumps(value).encode() if value is not None else None,
            method='PUT' if value is not None else 'GET',headers={'Content-Type':'application/json',**(headers or {})})
        with urllib.request.urlopen(request,timeout=5) as response:return response.status,json.loads(response.read())
    def test_candidates_match_manifest(self):
        self.assertEqual(len(self.manifest['cues']),36)
        count=0
        for cue in self.manifest['cues']:
            self.assertEqual([c['id'] for c in cue['choices']],list('abcde'))
            self.assertEqual(len({c['sha256'] for c in cue['choices']}),5)
            for choice in cue['choices']:
                path=server.PUBLIC/choice['file']
                self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(),choice['sha256'])
                with wave.open(str(path),'rb') as f:
                    self.assertEqual((f.getnchannels(),f.getsampwidth(),f.getframerate()),(1,2,22050))
                    samples=array.array('h',f.readframes(f.getnframes()))
                self.assertEqual((samples[0],samples[-1]),(0,0))
                self.assertLess(max(map(abs,samples)),23500)
                count+=1
        self.assertEqual(count,180)
    def test_round_trip_and_backup(self):
        payload=server.empty();payload['picks']={'shot1':{'choice':'c','gain':.85,'note':'Bite, less tail. "Steel" ✓'}}
        self.assertEqual(self.request('/api/selections',payload)[0],200)
        self.assertEqual(self.request('/api/selections')[1],payload)
        ready={**payload,'status':'ready'}
        self.request('/api/selections',ready)
        self.assertEqual(json.loads(server.SELECTIONS.read_text(encoding='utf-8')),ready)
        self.assertEqual(json.loads(server.SELECTIONS.with_name('choices.previous.json').read_text(encoding='utf-8')),payload)
    def test_reject_and_silent_original_are_explicit(self):
        payload=server.empty();payload['picks']={
            'enemySimple':{'choice':'original','gain':1,'note':''},
            'shotRail':{'choice':'reject','gain':1,'note':'Try less metallic'},
            'shot1':{'choice':'pending','gain':.9,'note':'Not decided'}}
        self.assertEqual(self.request('/api/selections',payload)[1]['selections'],payload)
        self.assertIsNone(next(c for c in self.manifest['cues'] if c['id']=='enemySimple')['original'])
    def test_invalid_write_does_not_replace_choices(self):
        good=server.empty();self.request('/api/selections',good)
        for pick in ({'choice':'z','gain':1},{'choice':'a','gain':float('inf')},{'choice':'a','gain':-1}):
            bad=server.empty();bad['picks']={'shot1':pick}
            with self.assertRaises(urllib.error.HTTPError) as error:self.request('/api/selections',bad)
            self.assertEqual(error.exception.code,400)
            self.assertEqual(self.request('/api/selections')[1],good)
    def test_foreign_origin_cannot_save(self):
        with self.assertRaises(urllib.error.HTTPError) as error:
            self.request('/api/selections',server.empty(),{'Origin':'https://unrelated.example'})
        self.assertEqual(error.exception.code,403)


if __name__=='__main__':unittest.main()
