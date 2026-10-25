import json,subprocess,concurrent.futures
from pathlib import Path
root=Path(__file__).resolve().parent; review=root/'Review'; parts=review/'parts';parts.mkdir(exist_ok=True)
clips=json.loads((root/'edit.json').read_text());clips[5]=[3,264,'City crossfire'];clips[6]=[2,402,'Thunder formations'];clips[17]=[2,710,'Submarine attack'];clips[18]=[3,142,'Plains boss'];clips[23]=[2,909,'Hell crab'];(root/'edit.json').write_text(json.dumps(clips,indent=2))
beat=json.loads((root/'music_beats.json').read_text());p=beat['period'];offset=beat['offset'];cursor=0;timeline=[]
for i,(n,start,label) in enumerate(clips):
 end=round((offset+p*sum([8 if j<18 else 4 for j in range(i+1)]))*60)/60
 duration=end-cursor;timeline.append(dict(index=i+1,source=f'semup{n}.mp4',source_start=start,label=label,start=cursor,end=end,duration=duration));cursor=end
(root/'timeline.json').write_text(json.dumps(dict(music='Assets/Resources/Music/fields.mp3',bpm=beat['bpm'],duration=90,clips=timeline),indent=2))
def render(c):
 i=c['index']; vf='fps=60,setsar=1'
 if i==1:vf+=',fade=t=in:st=0:d=0.25'
 if i==24:vf+=f",fade=t=out:st={c['duration']-1.5:.6f}:d=1.5"
 dest=parts/f'{i:02}.mp4'
 subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-ss',str(c['source_start']),'-i',f"C:/Users/Miguel/Downloads/{c['source']}",'-t',str(c['duration']),'-an','-vf',vf,'-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-threads','4',str(dest)],check=True)
 print(f"Rendered {i}/24: {c['label']}",flush=True)
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as ex:list(ex.map(render,timeline))
concat=review/'concat.txt';concat.write_text('\n'.join("file '"+str((parts/f'{i:02}.mp4').resolve()).replace('\\','/')+"'" for i in range(1,25)))
subprocess.run(['ffmpeg','-v','error','-y','-f','concat','-safe','0','-i',str(concat),'-i',str(root.parent/'Assets/Resources/Music/fields.mp3'),'-map','0:v','-map','1:a','-vf',f'tpad=stop_mode=clone:stop_duration={90-cursor+0.1:.5f}','-af','afade=t=in:st=0:d=0.08,afade=t=out:st=87.8:d=2.2','-t','90','-c:v','libx264','-preset','fast','-crf','18','-threads','6','-c:a','aac','-b:a','320k','-movflags','+faststart',str(root/'Shootemup_Gameplay_Trailer_90s.mp4')],check=True)
print('COMPLETE',flush=True)

