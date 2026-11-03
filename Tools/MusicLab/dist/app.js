import { Transport } from './transport.js';

const $ = id => document.getElementById(id);
const parts = { intro: ['Intro','↗'], body: ['Flight','≋'], peak: ['Boss','▲'], outro: ['Outro','↘'] };
const fmt = s => Number.isFinite(s) ? Math.floor(s/60) + ':' + String(Math.floor(s%60)).padStart(2,'0') : '—';
const set = (node, value) => { if (node.textContent !== value) node.textContent = value; };
const volume = () => Number($('volume').value) / 100 * .65;
let tracks = [], track, map, engine, context, controller, loadingAudio = false, message = '', chordKey = '';
const padNodes = {};

async function get(url, signal) {
    const res = await fetch(url, { signal });
    if (!res.ok) throw new Error('Could not load ' + url);
    return res;
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

async function select(item) {
    if (track === item && map) return;
    const keepPlaying = engine?.playing || loadingAudio;
    controller?.abort();
    controller = new AbortController();
    const signal = controller.signal;
    engine?.close();
    engine = map = null;
    loadingAudio = false;
    chordKey = '';
    track = item;
    message = 'Loading cue map…';
    $('title').textContent = item.title;
    $('track-no').textContent = (item.district ? 'DISTRICT' : 'RECORDING') + ' / ' + String(tracks.indexOf(item)+1).padStart(2,'0');
    $('track-picker').value = item.id;
    for (const b of document.querySelectorAll('.track')) b.setAttribute('aria-current', b.dataset.id === item.id);
    try {
        const value = await (await get('/cues/' + item.id, signal)).json();
        if (signal.aborted) return;
        map = value;
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
        if (engine && !engine.ended) { engine.playing = true; return; }
        if (!engine) {
            loadingAudio = true;
            message = 'Opening recording…';
            const data = await (await get('/audio/' + track.id, signal)).arrayBuffer();
            if (signal.aborted) return;
            const buffer = await context.decodeAudioData(data);
            if (signal.aborted) return;
            engine = new Transport(context, buffer, map, volume());
        }
        engine.start();
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
for (const [part,[label,icon]] of Object.entries(parts)) {
    const b = document.createElement('button');
    b.className = 'pad';
    b.dataset.part = part;
    b.innerHTML = '<span class="pad-icon" aria-hidden="true"></span><b></b><small></small>';
    b.firstChild.textContent = icon;
    b.children[1].textContent = label;
    b.onclick = async () => {
        const chosen = track;
        if (!engine?.playing) await play();
        if (chosen === track) engine?.request(part);
    };
    padNodes[part] = b;
    $('pads').append(b);
}

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
    const rect = map.sections[s.part];
    const boss = s.part === 'peak';
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
    const s = engine?.state() ?? { part:'intro',pos:0,rate:1,bass:0 };
    const moving = !!engine?.playing;
    $('play').disabled = !map || loadingAudio;
    const label = moving ? 'Ⅱ  Pause' : engine?.ended ? '↺  Replay' : '▶  Play';
    set($('play'), label);
    set($('clock'), fmt(s.pos) + ' / ' + fmt(map?.duration));
    if (!map) set($('bpm'), '—');
    for (const [part,b] of Object.entries(padNodes)) {
        const range = map?.sections[part];
        b.disabled = !map || loadingAudio || map.adaptive === false;
        b.setAttribute('aria-pressed', String(s.part === part));
        b.classList.toggle('queued', s.next === part);
        set(b.lastChild, range && map.adaptive !== false ? fmt(range.start) + ' – ' + fmt(range.end) : '—');
    }
    set($('status'), message || (s.next ? parts[s.next][0] + ' queued for the next progression ending.' :
        engine?.ended ? 'Recording complete.' : map?.adaptive === false ? 'Short cue · plays straight through.' :
        !engine ? 'Start on Intro. Choose a cue to queue a change.' : !moving ? 'Paused.' :
        s.part === 'peak' ? 'Boss section looping.' : s.part === 'body' ? 'Flight section looping.' : s.part === 'outro' ? 'Playing out.' : 'Flight follows the intro.'));
    $('preview').hidden = !s.next || !moving || s.remaining < 2*60/(map?.bpm || 120) + .1;
    set($('rate'), s.rate.toFixed(2) + '×');
    set($('bass'), (s.bass > .05 ? '+' + s.bass.toFixed(1) : '0') + ' dB');
    if (map) {
        const idx = lastAt(map.boundaries,s.pos);
        set($('boundary'), fmt(map.boundaries[idx+1]));
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
