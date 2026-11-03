import { Transport, prepareKick, seekTarget } from './transport.js';
import { barGrid, markerLimits, sameSpan, sectionKeys, validSpan } from './markers.mjs';

const $ = id => document.getElementById(id);
const parts = { intro: ['Intro','↗'], body: ['Flight','≋'], peak: ['Boss','▲'], fast: ['Boss (fast)','▲▲'], outro: ['Outro','↘'] };
const fmt = s => Number.isFinite(s) ? Math.floor(s/60) + ':' + String(Math.floor(s%60)).padStart(2,'0') : '—';
const set = (node, value) => { if (node.textContent !== value) node.textContent = value; };
const volume = () => Number($('volume').value) / 100 * .65;
let tracks = [], track, map, engine, context, controller, loadingAudio = false, message = '', chordKey = '';
let fastLoad, kickLoad, cueRequest = 0, selection, handoff;
let pendingSeek = null;
const padNodes = {};
const drafts = new Map(), saving = new Set();
let editSection = 'peak';
const dirtySections = sheet => sectionKeys.filter(k => !sameSpan(sheet.sections[k],sheet.savedSections[k]));
const stamp = t => {
    const ticks = Math.round(t*100);
    return Math.floor(ticks/6000) + ':' + ((ticks%6000)/100).toFixed(2).padStart(5,'0');
};
const queued = (title, cut) => title + ' queued · ' + cut.label + ' at ' + stamp(cut.offset);

async function get(url, signal) {
    const res = await fetch(url, { signal });
    if (!res.ok) throw new Error('Could not load ' + url);
    return res;
}

async function decode(url, signal) {
    const data = await (await get(url,signal)).arrayBuffer();
    return context.decodeAudioData(data);
}

async function openTransport(id, sheet, signal) {
    if (sheet.adaptive && !kickLoad) {
        kickLoad = Promise.all([context.audioWorklet.addModule('/boss-kick.js'), decode('/boss-kick.wav')])
            .then(([,buffer]) => prepareKick(buffer.getChannelData(0),context.sampleRate))
            .catch(err => { kickLoad = null; throw err; });
    }
    const [audio,kick,fast] = await Promise.all([decode('/audio/' + id,signal),sheet.adaptive ? kickLoad : null,
        sheet.adaptive ? decode('/fast/' + id + '?full=1',signal).catch(() => null) : null]);
    if (signal.aborted) return null;
    const transport = new Transport(context,audio,sheet,volume(),kick);
    transport.fastBuffer = fast;
    return transport;
}

function library() {
    for (const district of [true,false]) {
        const group = document.createElement('div');
        group.className = 'track-group';
        group.textContent = district ? 'DISTRICTS' : 'OTHER RECORDINGS';
        $('library').append(group);
        const options = document.createElement('optgroup');
        options.label = group.textContent;
        $('track-picker').append(options);
        for (const item of tracks.filter(t => t.district === district)) {
            const button = document.createElement('button');
            const index = String(tracks.indexOf(item) + 1).padStart(2, '0');
            button.className = 'track';
            button.dataset.id = item.id;
            const number = document.createElement('span');
            number.className = 'index';
            number.textContent = index;
            button.append(number, item.title);
            button.onclick = () => select(item);
            $('library').append(button);
            options.append(new Option(item.title, item.id));
        }
    }
    $('count').textContent = tracks.length;
    $('track-picker').onchange = e => select(tracks.find(t => t.id === e.target.value));
}

function showTrack(item) {
    track = item;
    pendingSeek = null;
    chordKey = '';
    $('title').textContent = item.title;
    $('track-no').textContent = (item.district ? 'DISTRICT' : 'RECORDING') + ' / ' + String(tracks.indexOf(item)+1).padStart(2,'0');
    $('track-picker').value = item.id;
    for (const b of document.querySelectorAll('.track')) b.setAttribute('aria-current', b.dataset.id === item.id);
    populateLoopEditor();
}

function finishHandoff() {
    if (!handoff || context.currentTime < handoff.at) return;
    // Its last source owns the outgoing tail and disconnects itself when the fade completes.
    engine.retire();
    controller?.abort();
    controller = new AbortController();
    selection = null;
    engine = handoff.engine;
    map = handoff.map;
    fastLoad = null;
    showTrack(handoff.item);
    handoff = null;
    message = '';
}

function cancelSelection() {
    finishHandoff();
    if (selection || handoff) message = '';
    selection?.abort();
    selection = null;
    if (!handoff) return;
    handoff.engine.close();
    handoff = null;
    if (engine.current) engine.plan();
}

async function select(item) {
    cancelSelection();
    message = '';
    if (track === item && map) return;
    const switching = !!engine?.playing;
    const requestedAt = context?.currentTime ?? 0;
    const keepPlaying = switching || loadingAudio;
    if (switching) selection = new AbortController();
    else {
        controller?.abort();
        controller = new AbortController();
    }
    const signal = (switching ? selection : controller).signal;
    cueRequest++;
    if (!switching) {
        engine?.close();
        engine = map = fastLoad = null;
        loadingAudio = false;
        showTrack(item);
    }
    message = switching ? 'Opening ' + item.title + '…' : 'Loading cue map…';
    try {
        const value = await (await get('/cues/' + item.id, signal)).json();
        if (signal.aborted) return;
        value.savedSections = { ...value.sections };
        value.grid = barGrid(value);
        for (const [key,edit] of Object.entries(drafts.get(item.id) ?? {})) {
            if (!validSpan(value,value.grid,edit.span) || sameSpan(value.sections[key],edit.span)) continue;
            value.savedSections[key] = edit.previous;
            value.sections[key] = edit.span;
        }
        if (switching) {
            const incoming = await openTransport(item.id,value,signal);
            if (signal.aborted) return;
            engine.tick();
            const cut = engine.current ? engine.plan(null,incoming,requestedAt) : { at:context.currentTime+.045, fade:.1 };
            incoming.start(cut.at,cut.fade);
            handoff = { ...cut, engine:incoming, map:value, item };
            message = cut.offset == null ? 'Starting ' + item.title + '…' : queued(item.title,cut);
            return;
        }
        map = value;
        populateLoopEditor();
        message = '';
        $('bpm').textContent = map.bpm ? Number(map.bpm.toFixed(1)) : '—';
        if (keepPlaying) await play();
    } catch (error) {
        if (!signal.aborted) message = error.message + ' · select a track to retry.';
    }
}

async function play() {
    if (!map || loadingAudio) return;
    const signal = controller.signal;
    try {
        context ??= new AudioContext();
        if (engine?.playing) {
            await context.suspend();
            if (!signal.aborted) engine.playing = false;
            return;
        }
        await context.resume();
        if (signal.aborted) return;
        if (engine && !engine.ended && !pendingSeek) { engine.playing = true; return; }
        if (!engine) {
            loadingAudio = true;
            message = 'Opening recording…';
            const loaded = await openTransport(track.id,map,signal);
            if (signal.aborted) return;
            engine = loaded;
        }
        if (pendingSeek) {
            engine.seek(pendingSeek);
            pendingSeek = null;
        } else engine.start();
        message = '';
    } catch (error) {
        if (!signal.aborted) message = 'Playback failed · ' + error.message;
    } finally {
        if (!signal.aborted) loadingAudio = false;
    }
}
$('play').onclick = play;
$('volume').oninput = () => { if (engine) engine.master.gain.setTargetAtTime(volume(), context.currentTime, .015); };
$('preview').onclick = () => engine?.previewChange();

function seekTo(time) {
    cancelSelection();
    if (!map) return;
    ++cueRequest;
    message = '';
    const target = seekTarget(map,time,pendingSeek?.part ?? engine?.state().part);
    if (engine?.playing) engine.seek(target);
    else {
        engine?.disposeDecks();
        pendingSeek = target;
    }
    draw();
}
const wave = $('wave');
wave.onclick = e => {
    if (!map) return;
    const rect = wave.getBoundingClientRect();
    seekTo((e.clientX - rect.left) / rect.width * map.duration);
};
wave.onkeydown = e => {
    if (!map) return;
    const pos = pendingSeek?.pos ?? engine?.state().pos ?? map.sections.intro.start;
    const targets = { ArrowLeft:pos-1, ArrowRight:pos+1, Home:0, End:map.duration };
    if (!(e.key in targets)) return;
    e.preventDefault();
    seekTo(targets[e.key]);
};
for (const [part,[label,icon]] of Object.entries(parts)) {
    const b = document.createElement('button');
    b.className = 'pad';
    b.dataset.part = part;
    b.innerHTML = '<span class="pad-icon" aria-hidden="true"></span><b></b><small></small>';
    b.firstChild.textContent = icon;
    b.children[1].textContent = label;
    b.onclick = async () => {
        cancelSelection();
        const chosen = track, ticket = ++cueRequest;
        const requestedAt = context?.currentTime ?? 0;
        message = '';
        if (!engine?.playing) await play();
        if (chosen !== track || !engine || ticket !== cueRequest) return;
        if (part === 'fast' && !engine.fastBuffer) {
            const signal = controller.signal;
            message = 'Preparing pitch-preserved fast mix…';
            try {
                fastLoad ??= get('/fast/' + track.id + '?full=1', signal).then(r => r.arrayBuffer()).then(data => context.decodeAudioData(data));
                const buffer = await fastLoad;
                if (signal.aborted) return;
                engine.fastBuffer = buffer;
                if (ticket !== cueRequest) return;
                message = '';
            } catch {
                if (!signal.aborted) {
                    fastLoad = null;
                    if (ticket === cueRequest) message = 'Fast mix unavailable. Check FFmpeg, then try again.';
                }
                return;
            }
        }
        engine.request(part,requestedAt);
    };
    padNodes[part] = b;
    $('pads').append(b);
}

function populateLoopEditor() {
    $('loop-editor').hidden = !map?.adaptive;
    for (const side of ['start','end']) {
        $('handle-' + side).hidden = !map?.adaptive;
        const select = $('loop-' + side);
        select.replaceChildren();
        if (map?.adaptive) {
            for (const [i,time] of map.grid.entries()) select.add(new Option(stamp(time), i));
        }
    }
    if (map?.adaptive) refreshLoopEditor();
}

function refreshLoopEditor(note) {
    const span = map.sections[editSection], bounds = map.grid, label = parts[editSection][0];
    for (const side of ['start','end']) {
        const t = span[side], select = $('loop-' + side), handle = $('handle-' + side);
        const [lo,hi] = markerLimits(map,bounds,span,side);
        const index = bounds.indexOf(t);
        select.value = index;
        select.disabled = handle.disabled = saving.has(track.id);
        $(side + '-back').disabled = select.disabled || index <= lo;
        $(side + '-forward').disabled = select.disabled || index >= hi;
        for (const opt of select.options) opt.disabled = opt.index < lo || opt.index > hi;
        handle.style.left = 'clamp(15px,' + 100*t/map.duration + '%,calc(100% - 15px))';
        handle.setAttribute('aria-valuemin',bounds[lo]);
        handle.setAttribute('aria-valuemax',bounds[hi]);
        handle.setAttribute('aria-valuenow',t);
        handle.setAttribute('aria-valuetext',stamp(t));
        handle.setAttribute('aria-label',label + ' ' + side);
        handle.title = label + ' ' + side + ' ' + stamp(t);
    }
    const dirty = dirtySections(map).length > 0;
    const saved = map.editedSections?.includes(editSection) || (editSection === 'peak' && map.peakEdited);
    $('save-loop').disabled = !dirty || saving.has(track.id);
    set($('loop-note'),note ?? (saving.has(track.id) ? 'Saving…' : dirty ? 'Unsaved' : saved ? 'Saved for game' : 'Analyzed'));
}

$('edit-section').onchange = e => {
    editSection = e.target.value;
    refreshLoopEditor();
};

function editBoundary(side, index) {
    if (!map?.adaptive) return;
    const bounds = map.grid, span = map.sections[editSection];
    const [lo,hi] = markerLimits(map,bounds,span,side);
    const t = bounds[Math.max(lo,Math.min(hi,index))];
    if (span[side] === t) return;
    map.sections[editSection] = { ...span, [side]:t };
    drafts.set(track.id,Object.fromEntries(dirtySections(map).map(k =>
        [k,{span:map.sections[k],previous:map.savedSections[k]}])));
    refreshLoopEditor();
}

for (const side of ['start','end']) {
    const select = $('loop-' + side), handle = $('handle-' + side);
    let grabX, grabTime;
    function stepBar(delta) {
        const grid = map.grid, i = Number(select.value), last = grid.length-1;
        // At a file edge, step about one bar even if a downbeat is only milliseconds away.
        if (i === 0 && delta > 0 && grid[1] < (grid[2]-grid[1])/2) return 2;
        if (i === last && delta < 0 && grid[last]-grid[last-1] < (grid[last-1]-grid[last-2])/2) return last-2;
        return i + delta;
    }
    for (const [direction,delta] of [['back',-1],['forward',1]]) {
        $(side + '-' + direction).onclick = () => {
            cancelSelection();
            editBoundary(side,stepBar(delta));
            engine?.updateSections();
        };
    }
    select.onchange = () => {
        cancelSelection();
        editBoundary(side,Number(select.value));
        engine?.updateSections();
    };
    handle.onpointerdown = e => {
        if (e.button !== 0) return;
        cancelSelection();
        grabX = e.clientX;
        grabTime = map.sections[editSection][side];
        handle.setPointerCapture(e.pointerId);
    };
    handle.onpointermove = e => {
        if (!handle.hasPointerCapture(e.pointerId) || !map?.adaptive) return;
        const box = $('wave').getBoundingClientRect();
        const t = grabTime + (e.clientX-grabX)/box.width*map.duration;
        const bounds = map.grid;
        const i = Math.max(0,lastAt(bounds,t));
        editBoundary(side, i+1 < bounds.length && bounds[i+1]-t < t-bounds[i] ? i+1 : i);
    };
    handle.onlostpointercapture = () => engine?.updateSections();
    handle.onkeydown = e => {
        const indices = {ArrowLeft:stepBar(-1),ArrowDown:stepBar(-1),ArrowRight:stepBar(1),ArrowUp:stepBar(1),Home:0,End:map.grid.length-1};
        if (!(e.key in indices)) return;
        e.preventDefault();
        cancelSelection();
        editBoundary(side,indices[e.key]);
        engine?.updateSections();
    };
}
$('hear-loop').onclick = async () => {
    cancelSelection();
    const chosen = map;
    const ticket = ++cueRequest;
    message = '';
    if (!engine?.playing) await play();
    if (map === chosen && ticket === cueRequest && engine?.playing) engine.auditionEnding(editSection);
};
$('save-loop').onclick = async () => {
    const sheet = map, id = sheet.id;
    const edits = dirtySections(sheet).map(section => ({section,...sheet.sections[section],previous:sheet.savedSections[section]}));
    if (saving.has(id)) return;
    saving.add(id);
    refreshLoopEditor();
    let error = '';
    try {
        const res = await fetch('/cues/' + id, { method:'PUT', headers:{'Content-Type':'application/json'},
            body:JSON.stringify({ edits }) });
        if (!res.ok) throw new Error((await res.text()) || 'Save failed. Try again.');
        for (const target of new Set([sheet,map?.id === id ? map : sheet])) {
            const edited = new Set(target.editedSections);
            for (const {section,start,end} of edits) {
                target.savedSections[section] = {start,end};
                edited.add(section);
            }
            target.editedSections = [...edited];
        }
        drafts.delete(id);
    } catch (err) { error = err.message; }
    finally {
        saving.delete(id);
        if (map?.id === id) refreshLoopEditor(error || undefined);
    }
};

// Last timestamp at or before the playhead.
function lastAt(list, time, value = x => x) {
    let lo = 0, hi = list.length;
    while (lo < hi) {
        const mid = (lo+hi) >>> 1;
        if (value(list[mid]) <= time) lo = mid+1; else hi = mid;
    }
    return lo-1;
}
function harmonicStrip(pos, idx) {
    const bounds = map.boundaries;
    const start = bounds[idx] ?? 0, end = bounds[idx+1] ?? map.duration;
    const active = lastAt(map.chords, pos, chord => chord.time);
    const key = track.id + '/' + idx + '/' + active;
    if (chordKey === key) return;
    chordKey = key;
    const strip = $('chords');
    strip.replaceChildren();
    const first = Math.max(0, lastAt(map.chords, start, c => c.time));
    for (let i = first; i < map.chords.length && map.chords[i].time < end; i++) {
        const chord = map.chords[i], cell = document.createElement('span');
        cell.className = 'chord' + (i === active ? ' on' : '');
        cell.textContent = chord.name;
        cell.title = fmt(chord.time);
        strip.append(cell);
    }
    if (!strip.childElementCount) set(strip, '—');
}

function waveform(s) {
    const canvas = $('wave'), w = canvas.clientWidth, h = canvas.clientHeight;
    if (!w || !h) return;
    const dpr = devicePixelRatio || 1;
    if (canvas.width !== Math.round(w*dpr) || canvas.height !== Math.round(h*dpr)) {
        canvas.width = Math.round(w*dpr); canvas.height = Math.round(h*dpr);
    }
    const g = canvas.getContext('2d');
    g.setTransform(dpr,0,0,dpr,0,0); g.clearRect(0,0,w,h);
    if (!map) return;
    const x = t => w*t/map.duration;
    const rect = map.sections[s.part === 'fast' ? 'peak' : s.part];
    const boss = s.part === 'peak' || s.part === 'fast';
    g.fillStyle = boss ? '#34271e' : '#1a2820';
    if (rect) g.fillRect(x(rect.start), 10, x(rect.end-rect.start), h-27);
    g.fillStyle = '#33413c';
    for (const boundary of map.boundaries) g.fillRect(Math.round(x(boundary)), 10, 1, h-27);
    const mid = (h-7)/2;
    for (let i=0; i<w; i+=3) {
        const amp = map.waveform[Math.min(map.waveform.length-1,Math.floor(i/w*map.waveform.length))] || 0;
        const size = Math.max(1, amp * (h-37)*.48);
        g.fillStyle = i < x(s.pos) ? '#c3f4c3' : rect && i >= x(rect.start) && i < x(rect.end) ? boss ? '#ebb486' : '#88bc95' : '#52655e';
        g.fillRect(i, mid-size, 2, size*2);
    }
    if (map.adaptive) {
        g.fillStyle = '#6b7972';
        for (const bar of map.downbeats) g.fillRect(x(bar),h-20,1,4);
        const { start, end } = map.sections[editSection];
        g.fillStyle = '#e6a473';
        g.fillRect(x(start),10,Math.max(1,x(end-start)),2);
        for (const t of [start,end]) g.fillRect(Math.min(w-1,x(t)),10,1,h-27);
    }
    g.fillStyle = '#e4f9d7';
    const head = Math.max(1,Math.min(w-2,x(s.pos)));
    g.fillRect(head,6,1,h-20);
    g.beginPath(); g.moveTo(head-4,4);g.lineTo(head+5,4);g.lineTo(head,10);g.fill();
    g.fillStyle='#98aaa1';g.font='9px Consolas, monospace';
    for (let i=0; i<=4; i++) {
        g.textAlign = i===0 ? 'left' : i===4 ? 'right' : 'center';
        g.fillText(fmt(i*map.duration/4), i*w/4, h-2);
    }
}

function draw() {
    finishHandoff();
    const s = pendingSeek ?? engine?.state() ?? { part:'intro',pos:map?.sections.intro.start ?? 0,rate:1,bass:0 };
    const moving = !!engine?.playing;
    $('play').disabled = !map || loadingAudio;
    $('hear-loop').disabled = !map?.adaptive || loadingAudio;
    const label = moving ? 'Ⅱ  Pause' : engine?.ended && !pendingSeek ? '↺  Replay' : '▶  Play';
    set($('play'), label);
    set($('clock'), fmt(s.pos) + ' / ' + fmt(map?.duration));
    wave.setAttribute('aria-valuemax', map?.duration ?? 0);
    wave.setAttribute('aria-valuenow', Math.floor(s.pos));
    wave.setAttribute('aria-valuetext', fmt(s.pos));
    set($('bpm'), map?.bpm ? String(Number(map.bpm.toFixed(1))) : '—');
    for (const [part,b] of Object.entries(padNodes)) {
        const range = map?.sections[part === 'fast' ? 'peak' : part];
        b.disabled = !map || loadingAudio || map.adaptive === false;
        b.setAttribute('aria-pressed', String(s.part === part));
        b.classList.toggle('queued', s.next === part);
        set(b.lastChild, range && map.adaptive !== false ? fmt(range.start) + ' – ' + fmt(range.end) : '—');
    }
    set($('status'), message || (s.next ? queued(parts[s.next][0],s) :
        pendingSeek ? 'Cued at ' + fmt(s.pos) + '.' : engine?.ended ? 'Recording complete.' : map?.adaptive === false ? 'Short cue · plays straight through.' :
        !engine ? 'Start on Intro. Choose a cue to queue a change.' : !moving ? 'Paused.' :
        s.part === 'fast' ? 'Fast boss loop · original pitch.' : s.part === 'peak' ? 'Boss loop · original tempo.' :
        s.part === 'body' ? 'Flight section looping.' : s.part === 'outro' ? 'Playing out.' :
        s.part === 'recording' ? 'Playing recording.' : 'Flight follows the intro.'));
    $('preview').hidden = !s.next || !moving || s.remaining < 2*60/(map?.bpm || 120) + .1;
    set($('rate'), s.rate.toFixed(2) + '×');
    set($('bass'), (s.bass > .05 ? '+' + s.bass.toFixed(1) : '0') + ' dB');
    if (map) {
        const idx = lastAt(map.boundaries,s.pos);
        const change = map.chords[lastAt(map.chords,s.pos,c => c.time)+1];
        set($('boundary'), change ? change.name + ' · ' + fmt(change.time) : '—');
        harmonicStrip(s.pos,idx);
        const i = lastAt(map.beats,s.pos), bar = lastAt(map.downbeats,s.pos);
        const first = bar < 0 ? 0 : lastAt(map.beats,map.downbeats[bar]+.025);
        const meter = map.beatsPerBar || 4;
        const beat = i < 0 ? -1 : (i - Math.max(first,0)) % meter;
        const pulses = $('pulses');
        if (pulses.children.length !== meter) pulses.replaceChildren(...Array.from({length:meter}, () => document.createElement('i')));
        for (let n=0;n<meter;n++) pulses.children[n].classList.toggle('lit', !!engine && n === beat);
        set($('beat'), 'BEAT ' + (beat < 0 || !engine ? '—' : beat+1));
    } else {
        set($('chords'),'—'); set($('boundary'),'—'); set($('beat'),'BEAT —');
    }
    waveform(s);
}
let last = -100;
function frame(now) { if (now-last > 32) { draw(); last = now; } requestAnimationFrame(frame); }
requestAnimationFrame(frame);
try {
    tracks = await (await get('/tracks')).json();
    library();
    await select(tracks[0]);
} catch (err) { message = err.message; }
