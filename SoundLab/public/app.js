const $ = s => document.querySelector(s);
let manifest, state, activeIndex = 0, generation = 0, audio, master, compressor;
let nodes = new Set(), timers = new Set(), cache = new Map(), saveTail = Promise.resolve();
let notesTimer, renderVersion = 0;
const STORAGE = 'shootemup-sound-lab-sample-round-1';
const escapeHTML = v => String(v).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const reviewed = p => p && !['pending'].includes(p.choice);
const cue = () => manifest.cues[activeIndex];
const currentPick = () => state.picks[cue().id];
const ensurePick = () => state.picks[cue().id] ||= {choice:'pending', gain:1, note:''};

function banner(message, error=false) {
  $('#banner').hidden = false; $('#banner').textContent = message; $('#banner').classList.toggle('error',error);
}
function progress() {
  const count = Object.values(state.picks).filter(reviewed).length;
  $('#progress-label').textContent = `${count} / ${manifest.cues.length}`;
  $('#progress-bar').style.width = `${100*count/manifest.cues.length}%`;
}
function nav() {
  const term = $('#search').value.toLowerCase().trim(); let group = '';
  $('#cue-nav').innerHTML = manifest.cues.map((c,i) => {
    if (term && !`${c.label} ${c.group}`.toLowerCase().includes(term)) return '';
    let heading = '';
    if (group!==c.group) { group=c.group; heading=`<div class="nav-group">${escapeHTML(group)}</div>`; }
    const pick=state.picks[c.id];
    const label=!reviewed(pick)?'':pick.choice==='reject'?'↻':pick.choice==='original'?'OLD':pick.choice.toUpperCase();
    return `${heading}<button class="nav-item ${i===activeIndex?'active':''}" data-cue="${i}" ${i===activeIndex?'aria-current="true"':''}><span class="index">${String(i+1).padStart(2,'0')}</span><span>${escapeHTML(c.label)}</span><span class="pick ${pick?.choice==='reject'?'rejected':''}">${label}</span></button>`;
  }).join('') || '<p class="muted">No matching effects.</p>';
  $('#cue-nav').querySelectorAll('[data-cue]').forEach(b => b.onclick=()=>navigate(Number(b.dataset.cue)));
  progress();
}
function render() {
  const c=cue(), p=currentPick(), renderId=++renderVersion;
  const chosen=!reviewed(p)?'NOT PICKED':p.choice==='reject'?'ANOTHER ROUND':p.choice==='original'?'KEEP CURRENT':`PICKED ${p.choice.toUpperCase()}`;
  $('#effect').innerHTML = `<div class="effect-head"><div><div class="eyebrow">${escapeHTML(c.group)} <span class="muted">/ ${String(activeIndex+1).padStart(2,'0')}</span></div><h2>${escapeHTML(c.label)}</h2></div><div class="head-actions"><span class="chip">${chosen}</span>${reviewed(p)?'<button class="quiet" id="clear-choice">Clear choice</button>':''}<button class="quiet" id="compare">▶ Compare A → E</button></div></div>
    <div class="baseline ${p?.choice==='original'?'chosen-baseline':''}"><div><strong>Current in-game sound</strong><p>${c.original?'The existing sound, level-matched for comparison.':'Currently silent. Keeping current adds no new sound.'}</p></div><div class="baseline-actions">${c.original?'<button id="original-play">▶ Play current</button><button id="original-repeat">↻ Repeat current</button>':''}<button id="keep-current">${p?.choice==='original'?'✓ Keeping current':'Keep current'}</button></div></div>
    <div class="choices">${c.choices.map(ch=>`<article class="choice ${p?.choice===ch.id?'chosen':''}" data-id="${ch.id}"><div class="choice-top"><span class="letter">${ch.id.toUpperCase()}</span><h3>${escapeHTML(ch.label)}</h3><span class="time">${Math.round(ch.seconds*1000)} ms</span></div><p>${escapeHTML(ch.description)}</p><canvas class="waveform" data-wave="${ch.id}" aria-label="${escapeHTML(ch.label)} waveform"></canvas><div class="audio-actions"><button data-play="${ch.id}">▶ Play once</button><button data-repeat="${ch.id}">↻ Repeat</button></div><button class="choose" data-choose="${ch.id}" aria-pressed="${p?.choice===ch.id}"><span class="radio"></span>${p?.choice===ch.id?'Selected':'Choose '+ch.id.toUpperCase()}</button></article>`).join('')}
    <article class="feedback-card ${p?.choice==='reject'?'rejected':''}"><h3>Not the right direction?</h3><p>Keep the original, or ask for another round. Your note will tell me what to change.</p><button id="reject">${p?.choice==='reject'?'✓ Another round requested':'None of these · try again ↻'}</button></article></div>
    <div class="notes"><label for="note">DIRECTION / NOTES<textarea id="note" maxlength="1000" placeholder="e.g. B has the right punch, but shorten the tail. Less metallic.">${escapeHTML(p?.note||'')}</textarea></label><div class="gain"><label for="cue-gain">SELECTED EFFECT LEVEL</label><input id="cue-gain" type="range" min="0" max="150" value="${Math.round((p?.gain??1)*100)}"><output id="cue-gain-label">${Math.round((p?.gain??1)*100)}%</output><p>Saved with this effect. Applies to all auditions here.</p></div></div>`;
  document.querySelectorAll('[data-play]').forEach(b=>b.onclick=()=>preview(b.dataset.play,false));
  document.querySelectorAll('[data-repeat]').forEach(b=>b.onclick=()=>preview(b.dataset.repeat,true));
  document.querySelectorAll('[data-choose]').forEach(b=>b.onclick=()=>choose(b.dataset.choose));
  $('#keep-current').onclick=()=>choose('original');
  if ($('#original-play')) $('#original-play').onclick=()=>preview('original',false);
  if ($('#original-repeat')) $('#original-repeat').onclick=()=>preview('original',true);
  if ($('#clear-choice')) $('#clear-choice').onclick=()=>choose('pending');
  $('#reject').onclick=()=>choose('reject'); $('#compare').onclick=compare;
  $('#note').oninput=()=>{ ensurePick().note=$('#note').value; state.status='draft'; clearTimeout(notesTimer); notesTimer=setTimeout(save,400); };
  $('#cue-gain').oninput=()=>{ensurePick().gain=Number($('#cue-gain').value)/100; $('#cue-gain-label').textContent=$('#cue-gain').value+'%'; state.status='draft'; clearTimeout(notesTimer); notesTimer=setTimeout(save,250);};
  $('#prev').disabled=activeIndex===0; $('#next').disabled=activeIndex===manifest.cues.length-1;
  nav();
  for (const ch of c.choices) buffer(ch.file).then(b=>{if(renderId===renderVersion)drawWave(ch.id,b);}).catch(()=>{});
}
function choose(id) { ensurePick().choice=id; state.status='draft'; save(); render(); }
function navigate(index) { if(index<0||index>=manifest.cues.length)return; stop(); clearTimeout(notesTimer); activeIndex=index; render(); save(); }

function save() {
  clearTimeout(notesTimer);
  const snapshot=JSON.stringify(state);
  try { localStorage.setItem(STORAGE,snapshot); } catch {}
  $('#save-status').textContent='Saving to project…';
  saveTail=saveTail.catch(()=>{}).then(async()=>{
    const response=await fetch('/api/selections',{method:'PUT',headers:{'Content-Type':'application/json'},body:snapshot});
    const result=await response.json(); if(!response.ok)throw new Error(result.error||'Save failed');
    $('#save-status').textContent='✓ Saved to project';
  }).catch(error=>{
    $('#save-status').textContent='⚠ Not saved to project';
    banner(`${error.message}. Keep this page open or use Export picks as a backup.`,true);
    throw error;
  });
  // Keep rejected network requests handled, while finish() can still await the failure.
  saveTail.catch(()=>{});
  return saveTail;
}
async function finish() {
  stop();state.status='ready';
  try {
    await save();
    const count=Object.values(state.picks).filter(reviewed).length;
    banner(`Saved ${count} of ${manifest.cues.length} reviewed effects to the project. Tell Codex “my sound picks are ready” and I can read and apply them. Unpicked effects keep their current behavior. “None of these” requests another round.`);
    $('#banner').scrollIntoView({behavior:'smooth',block:'center'});
  } catch { state.status='draft'; }
}
function exportPicks() {
  const data=new Blob([JSON.stringify(state,null,2)+'\n'],{type:'application/json'}), url=URL.createObjectURL(data);
  const a=document.createElement('a');a.href=url;a.download='shootemup-sound-picks.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);
}

function graph() {
  if(audio)return;
  audio=new AudioContext(); master=audio.createGain();master.gain.value=Number($('#volume').value)/100;
  compressor=audio.createDynamicsCompressor(); compressor.threshold.value=-5;compressor.knee.value=4;compressor.ratio.value=16;compressor.attack.value=.002;compressor.release.value=.12;
  compressor.connect(master);master.connect(audio.destination);
}
async function buffer(file) {
  graph();
  if(!cache.has(file))cache.set(file,fetch(file).then(r=>{if(!r.ok)throw new Error('Audio file unavailable');return r.arrayBuffer();}).then(data=>audio.decodeAudioData(data)).catch(e=>{cache.delete(file);throw e;}));
  return cache.get(file);
}
function drawWave(id,b) {
  const canvas=document.querySelector(`[data-wave="${id}"]`);if(!canvas)return;
  const width=Math.max(100,canvas.clientWidth),height=canvas.clientHeight||42,dpr=devicePixelRatio||1;
  canvas.width=width*dpr;canvas.height=height*dpr;const ctx=canvas.getContext('2d');ctx.scale(dpr,dpr);
  const data=b.getChannelData(0),bars=Math.floor(width/4),chunk=Math.max(1,Math.floor(data.length/bars));
  ctx.fillStyle=({a:'#8aa76c',b:'#6faaa5',c:'#b99966',d:'#9c88ba',e:'#b98d76'})[id];
  for(let i=0;i<bars;i++){let peak=0;for(let n=i*chunk;n<Math.min(data.length,(i+1)*chunk);n++)peak=Math.max(peak,Math.abs(data[n]));const h=Math.max(1,Math.min(height,peak*height*1.45));ctx.fillRect(i*4,(height-h)/2,2,h);}
}
function later(fn,ms) {const timer=setTimeout(()=>{timers.delete(timer);fn();},ms);timers.add(timer);return timer;}
function stop() {
  generation++; for(const timer of timers)clearTimeout(timer);timers.clear();
  for(const node of nodes){try{node.stop();}catch{}}nodes.clear();
  document.querySelectorAll('.playing').forEach(el=>el.classList.remove('playing'));
  $('#now-playing').hidden=true;
}
function markPlaying(text,id) {
  $('#playing-label').textContent=text;$('#now-playing').hidden=false;
  document.querySelectorAll('.playing').forEach(el=>el.classList.remove('playing'));
  document.querySelector(`.choice[data-id="${id}"]`)?.classList.add('playing');
}
function emit(b,at,gain=1) {
  const source=audio.createBufferSource(),level=audio.createGain();source.buffer=b;level.gain.value=gain;
  source.connect(level);level.connect(compressor);nodes.add(source);
  source.onended=()=>{nodes.delete(source);source.disconnect();level.disconnect();};source.start(at);return source;
}
async function preview(id,repeated) {
  stop(); const token=generation, c=cue(), ch=id==='original'?c.original:c.choices.find(v=>v.id===id);
  if(!ch){banner('This effect is currently silent. Choose an alternative to add a sound.');return;}
  try {
    graph();await audio.resume();const b=await buffer(ch.file);if(token!==generation)return;
    const gain=currentPick()?.gain??1, rate=Number($('#rate').value), duration=Number($('#duration').value);
    const count=repeated?Math.ceil(rate*duration):1;
    // Leave overlap headroom when intentionally stress-testing long tails at rapid rates.
    const overlap=repeated?Math.sqrt(Math.max(1,b.duration*rate*.45)):1;
    const start=audio.currentTime+.025;
    for(let n=0;n<count;n++)emit(b,start+(repeated?n/rate:0),gain/overlap);
    markPlaying(`${c.label} · ${id==='original'?'current':id.toUpperCase()}${repeated?` · ${rate}/sec · ${duration}s`:''}`,id);
    later(()=>{if(token===generation)stop();},(repeated?duration+b.duration:b.duration)*1000+80);
  } catch(error) {stop();banner(error.message,true);}
}
async function compare() {
  stop();const token=generation,c=cue();
  try {
    graph();await audio.resume();const buffers=await Promise.all(c.choices.map(ch=>buffer(ch.file)));if(token!==generation)return;
    let offset=.04;
    c.choices.forEach((ch,i)=>{
      emit(buffers[i],audio.currentTime+offset,currentPick()?.gain??1);
      later(()=>{if(token===generation)markPlaying(`${c.label} · ${ch.id.toUpperCase()} / ${ch.label}`,ch.id);},offset*1000);
      offset+=Math.max(.75,buffers[i].duration+.4);
    });
    later(()=>{if(token===generation)stop();},offset*1000);
  } catch(error){stop();banner(error.message,true);}
}
async function combat() {
  stop();const token=generation;graph();await audio.resume();
  const events=[];
  for(let n=0;n<150;n++){
    events.push({t:n*.1,id:['shot1','shotElectric','shotAK45','shotNeedle','shotRail','shotScatter','shotShell','shotDisc','shotMissile','shotFlame','shotBeam','shotCompanion'][Math.min(11,Math.floor(n/13))]});
    if(n%2===0)events.push({t:n*.1+.04,id:'hit1'});
    if(n%5===0)events.push({t:n*.1,id:['enemySimple','enemyPlasma','enemyMissile','enemyNeedle','beamFire'][Math.floor(n/5)%5]});
    if(n%9===0)events.push({t:n*.1+.1,id:n%36===0?'explosionHeavy':'enemyExplosion1'});
  }
  events.push({t:4,id:'hurt1'},{t:8,id:'blankbullet'},{t:14.5,id:'playerDead'});
  try {
    const resolved=await Promise.all(events.map(async e=>{
      const c=manifest.cues.find(v=>v.id===e.id), p=state.picks[e.id];
      const candidate=p&&['a','b','c','d','e'].includes(p.choice)?c.choices.find(v=>v.id===p.choice):c.original;
      return candidate?{...e,b:await buffer(candidate.file),gain:c.gameGain*(p?.gain??1)}:null;
    }));
    if(token!==generation)return;
    const start=audio.currentTime+.04; for(const event of resolved)if(event)emit(event.b,start+event.t,event.gain*2.4);
    markPlaying('Your palette · 15-second combat preview','mix');
    later(()=>{if(token===generation)stop();},16500);
  } catch(error){stop();banner(error.message,true);}
}

$('#rate').oninput=()=>$('#rate-label').textContent=$('#rate').value+' / sec';
$('#volume').oninput=()=>{const value=Number($('#volume').value)/100;$('#volume-label').textContent=$('#volume').value+'%';if(master)master.gain.setTargetAtTime(value,audio.currentTime,.02);};
$('#stop').onclick=stop;$('#stop-floating').onclick=stop;$('#mix').onclick=()=>combat().catch(e=>banner(e.message,true));
$('#finish').onclick=finish;$('#export').onclick=exportPicks;$('#search').oninput=nav;
$('#prev').onclick=()=>navigate(activeIndex-1);$('#next').onclick=()=>navigate(activeIndex+1);
document.addEventListener('keydown',e=>{
  if(e.key==='Escape'){stop();return;}
  if(['INPUT','TEXTAREA','SELECT'].includes(document.activeElement?.tagName))return;
  if(e.key==='ArrowRight')navigate(activeIndex+1);if(e.key==='ArrowLeft')navigate(activeIndex-1);
  if(/^[1-5]$/.test(e.key))preview(String.fromCharCode(96+Number(e.key)),false);
});
document.addEventListener('visibilitychange',()=>{if(document.hidden)stop();});
window.addEventListener('pagehide',stop);
window.addEventListener('beforeunload',e=>{if($('#save-status').textContent.includes('Saving')||$('#save-status').textContent.includes('Not saved')){e.preventDefault();e.returnValue='';}});
window.addEventListener('resize',()=>{if(manifest)for(const ch of cue().choices)buffer(ch.file).then(b=>drawWave(ch.id,b)).catch(()=>{});});

async function init() {
  try {
    const response=await fetch('manifest.json');if(!response.ok)throw new Error('Cannot load sound bank');manifest=await response.json();
    try{const r=await fetch('/api/selections');if(!r.ok)throw new Error('Server unavailable');state=await r.json();$('#save-status').textContent='✓ Connected to project';}
    catch{
      try{state=JSON.parse(localStorage.getItem(STORAGE));}catch{}
      state||={schemaVersion:1,bankVersion:manifest.bankVersion,status:'draft',picks:{}};
      $('#save-status').textContent='⚠ Offline · export picks';banner('Project connection unavailable. Picks will stay in this browser; use Export picks to keep a copy.',true);
    }
    if(state.bankVersion!==manifest.bankVersion)throw new Error('Saved picks belong to a different sound round. Export them before switching banks.');
    render();
    if(state.status==='ready')banner('Your last selection is saved and marked ready for Codex. Changing a choice starts a new draft.');
  } catch(error){banner(error.message,true);$('#effect').innerHTML='<p>Start the local server with the SoundLab launch script, then reopen its localhost address.</p>';}
}
init();
