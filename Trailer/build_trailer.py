import json,subprocess,concurrent.futures,hashlib
from pathlib import Path
root=Path(__file__).resolve().parent
review=root/'Review';parts=review/'v5_parts';parts.mkdir(exist_ok=True)
clips=json.loads((root/'edit.json').read_text())
beat=json.loads((root/'music_beats.json').read_text())
cursor=0;total_bars=0;timeline=[]
anchors=beat['bar_anchors'];music_start=beat['music_start']
for i,clip in enumerate(clips):
 total_bars+=clip['bars']
 accent=anchors[total_bars]-music_start
 end=round(accent*60)/60
 if i==len(clips)-1:end=90.0
 timeline.append(dict(clip,index=i+1,start=cursor,end=end,duration=end-cursor,accent_time=accent));cursor=end
fade_start=round((anchors[62]-music_start)*60)/60
fade_end=round((anchors[63]-music_start)*60)/60
(root/'timeline.json').write_text(json.dumps(dict(music='Assets/Resources/Music/fields.mp3',music_start=music_start,bpm=beat['bpm'],duration=90,clips=timeline),indent=2))

def render(c):
 i=c['index'];vf=f"setpts=(PTS-STARTPTS)/{c.get('speed',1)},fps=60,setsar=1"
 if i==1:vf+=',fade=t=in:st=0:d=0.12'
 if i==len(clips):vf+=f",fade=t=out:st={fade_start-c['start']:.6f}:d={fade_end-fade_start:.6f}"
 dest=parts/f'{i:02}.mp4'
 signature=hashlib.sha256(json.dumps(c,sort_keys=True).encode()).hexdigest();stamp=dest.with_suffix('.sha256')
 if dest.exists() and stamp.exists() and stamp.read_text()==signature:return
 subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-ss',str(c['source_start']),'-i',f"C:/Users/Miguel/Downloads/{c['source']}",'-t',str(c['duration']),'-an','-vf',vf,'-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-threads','4',str(dest)],check=True)
 stamp.write_text(signature);print(f"Rendered {i}/{len(clips)}: {c['label']}",flush=True)
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as ex:list(ex.map(render,timeline))
concat=review/'concat_v5.txt'
concat.write_text('\n'.join("file '"+str((parts/f'{i:02}.mp4').resolve()).replace('\\','/')+"'" for i in range(1,len(clips)+1)))
subprocess.run(['ffmpeg','-v','error','-y','-f','concat','-safe','0','-i',str(concat),'-ss',str(music_start),'-i',str(root.parent/'Assets/Resources/Music/fields.mp3'),'-map','0:v','-map','1:a','-af',f'afade=t=in:st=0:d=0.01,afade=t=out:st={anchors[61]-music_start:.6f}:d={anchors[63]-anchors[61]:.6f}','-t','90','-c:v','copy','-c:a','aac','-b:a','320k','-movflags','+faststart',str(root/'Shootemup_Gameplay_Trailer_90s_v5.mp4')],check=True)
print('COMPLETE',flush=True)
