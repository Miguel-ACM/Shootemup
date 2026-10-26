"""Loopback-only audition UI; selections save to SoundLab/selections.json.

Run: python SoundLab/server.py --port 8765
No third-party packages. This server never edits game assets or scripts.
"""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
import json
import os
from pathlib import Path
import threading
from urllib.parse import urlparse

HERE=Path(__file__).resolve().parent
PUBLIC=HERE/'public'
SELECTIONS=HERE/'selections.json'
LOCK=threading.Lock()


def empty():
    return dict(schemaVersion=1,bankVersion='sample-round-1',status='draft',picks={})


def validate(value, manifest):
    if not isinstance(value,dict) or value.get('schemaVersion')!=1 or value.get('bankVersion')!=manifest['bankVersion']:
        raise ValueError('Selection format or bank version does not match this round.')
    if value.get('status') not in ('draft','ready') or not isinstance(value.get('picks'),dict):
        raise ValueError('Invalid selection status or picks.')
    by_id={c['id']:c for c in manifest['cues']}
    cleaned={}
    for cue_id,pick in value['picks'].items():
        if cue_id not in by_id or not isinstance(pick,dict): raise ValueError('Unknown effect.')
        if pick.get('choice') not in ('a','b','c','d','e','original','reject','pending'): raise ValueError('Unknown alternative.')
        if pick['choice'] in 'abcde' and pick['choice'] not in {choice['id'] for choice in by_id[cue_id]['choices']}:
            raise ValueError('That alternative is not available for this effect.')
        gain=pick.get('gain',1)
        if not isinstance(gain,(int,float)) or isinstance(gain,bool) or not 0<=gain<=1.5: raise ValueError('Invalid gain.')
        note=pick.get('note','')
        if not isinstance(note,str) or len(note)>1000: raise ValueError('Notes must be under 1,000 characters.')
        cleaned[cue_id]=dict(choice=pick['choice'],gain=gain,note=note)
    return dict(schemaVersion=1,bankVersion=manifest['bankVersion'],status=value['status'],picks=cleaned)


class Handler(SimpleHTTPRequestHandler):
    def __init__(self,*args,**kwargs): super().__init__(*args,directory=str(PUBLIC),**kwargs)
    def log_message(self,format,*args):
        if args and str(args[0]).startswith(('GET /api','PUT /api')): return
        super().log_message(format,*args)
    def end_headers(self):
        self.send_header('Cache-Control','no-store')
        self.send_header('X-Content-Type-Options','nosniff')
        self.send_header('Referrer-Policy','no-referrer')
        super().end_headers()
    def allowed(self):
        hosts={f'127.0.0.1:{self.server.server_port}',f'localhost:{self.server.server_port}'}
        if self.headers.get('Host') not in hosts: return False
        origin=self.headers.get('Origin')
        return origin is None or origin in {'http://'+h for h in hosts}
    def reply(self,status,value):
        data=json.dumps(value,ensure_ascii=False).encode()
        self.send_response(status);self.send_header('Content-Type','application/json; charset=utf-8')
        self.send_header('Content-Length',str(len(data)));self.end_headers();self.wfile.write(data)
    def do_GET(self):
        if not self.allowed(): return self.reply(403,dict(error='Local access only.'))
        path=urlparse(self.path).path
        if path=='/api/selections':
            with LOCK:
                value=json.loads(SELECTIONS.read_text(encoding='utf-8')) if SELECTIONS.exists() else empty()
            return self.reply(200,value)
        if path=='/api/health': return self.reply(200,dict(ok=True,app='shootemup-sound-lab',bankVersion='sample-round-1'))
        return super().do_GET()
    def do_PUT(self):
        if not self.allowed(): return self.reply(403,dict(error='Local access only.'))
        if urlparse(self.path).path!='/api/selections': return self.reply(404,dict(error='Unknown endpoint.'))
        try:
            length=int(self.headers.get('Content-Length','0'))
            if length<=0 or length>65536: raise ValueError('Invalid payload length.')
            if self.headers.get('Content-Type','').split(';')[0]!='application/json': raise ValueError('Expected JSON.')
            raw=json.loads(self.rfile.read(length))
            manifest=json.loads((PUBLIC/'manifest.json').read_text(encoding='utf-8'))
            value=validate(raw,manifest)
            with LOCK:
                data=json.dumps(value,indent=2,ensure_ascii=False)+'\n'
                temp=SELECTIONS.with_name(SELECTIONS.stem+'.pending.json')
                if SELECTIONS.exists():
                    SELECTIONS.with_name(SELECTIONS.stem+'.previous.json').write_bytes(SELECTIONS.read_bytes())
                with temp.open('w',encoding='utf-8') as f:
                    f.write(data);f.flush();os.fsync(f.fileno())
                os.replace(temp,SELECTIONS)
            return self.reply(200,dict(ok=True,selections=value))
        except (ValueError,KeyError,TypeError,json.JSONDecodeError) as error:
            return self.reply(400,dict(error=str(error)))
        except OSError:
            return self.reply(500,dict(error='Could not save to the project. Export your choices as a backup.'))


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--port',type=int,default=8765);args=parser.parse_args()
    server=ThreadingHTTPServer(('127.0.0.1',args.port),Handler)
    print(f'Sound Lab ready: http://127.0.0.1:{server.server_port}',flush=True)
    print(f'Picks save to: {SELECTIONS}',flush=True)
    try: server.serve_forever()
    except KeyboardInterrupt: pass
    finally: server.server_close()
