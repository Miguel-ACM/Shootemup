// Cue coordinates are seconds in the original recording. The fast audition is already time-stretched.
const bassDb = 4.5, shelfHz = 145, lead = .045, maxWait = 10, outroWait = 4;
const clamp = (n, lo, hi) => Math.max(lo, Math.min(hi, n));
const position = (d, at) => d.offset + Math.max(0, at - d.at)*d.speed;
const clockAt = (d, offset) => d.at + (offset - d.offset)/d.speed;
const boss = part => part === 'peak' || part === 'fast';
export function seekTarget(map, time, preferred = 'intro') {
    const pos = clamp(time, 0, map.duration);
    // Flight and Boss often overlap. Keep the active mix when the click falls inside it.
    const selected = [preferred, 'intro', 'body', 'outro', 'peak'].find(key => {
        const range = map.sections[key === 'fast' ? 'peak' : key];
        return range && range.start <= pos && pos < range.end;
    }) ?? 'recording';
    return { part:selected, pos, rate:selected === 'fast' ? 1.06 : 1, bass:boss(selected) ? bassDb : 0 };
}
function bassAt(d, at) {
    const t = d.ramp ? clamp((at - d.at)/d.ramp, 0, 1) : 1;
    return d.from + (d.to - d.from)*t;
}

export function prepareKick(pcm, rate) {
    const duck = new Float32Array(pcm.length);
    const attack = Math.exp(-1/(.002*rate)), release = Math.exp(-1/(.055*rate));
    const depth = 1 - 10**(-1/20);
    let envelope = 0;
    for (let i = 0; i < pcm.length; i++) {
        const peak = Math.abs(pcm[i]);
        envelope = peak + (envelope-peak)*(peak > envelope ? attack : release);
        const tail = Math.min(1,(pcm.length-1-i)/(.04*rate));
        duck[i] = depth * Math.min(1,envelope/.2) * tail;
    }
    return {pcm,duck};
}

function openingChord(chords, { start, end }) {
    if (!chords?.length) return null;
    let i = 0;
    while (i+1 < chords.length && chords[i+1].time <= start) i++;
    for (; i < chords.length && chords[i].time < end; i++) {
        const name = chords[i].name;
        if (name && name !== 'N') return name;
    }
    return null;
}

function transitionPoint(map, start, end, earliest, deadline, target) {
    const limit = Math.min(end,Math.max(earliest,deadline));
    const eligible = t => t > start && t >= earliest && t <= limit;
    const chord = target && map.chords?.find(c => eligible(c.time) && c.name === target);
    if (chord) return { offset:chord.time, label:chord.name };
    const phrase = map.boundaries?.find(eligible);
    if (phrase !== undefined) return { offset:phrase, label:'progression end' };
    if (end <= limit) return { offset:end, label:'section end' };
    const bar = map.downbeats?.findLast(eligible);
    if (bar !== undefined) return { offset:bar, label:'bar line' };
    const beat = map.beats?.findLast(eligible);
    return { offset:beat ?? limit, label:beat === undefined ? 'time limit' : 'beat' };
}

export class Transport {
    constructor(ctx, buffer, map, volume, kickSample) {
        Object.assign(this, { ctx, buffer, map, kickSample, playing:false, ended:false });
        this.voices = new Set();
        this.master = ctx.createGain();
        this.master.gain.value = volume;
        this.master.connect(ctx.destination);
        this.backing = ctx.createGain();
        this.backing.connect(this.master);
    }
    range(part) {
        return this.map.adaptive === false || part === 'recording' ? { start:0, end:this.map.duration } :
            this.map.sections[part === 'fast' ? 'peak' : part];
    }
    deck(part, at, offset, from, fadeIn, end = this.range(part).end) {
        const source = this.ctx.createBufferSource(), shelf = this.ctx.createBiquadFilter(), gain = this.ctx.createGain();
        const speed = part === 'fast' ? 1.06 : 1;
        const start = offset/speed;
        source.buffer = part === 'fast' ? this.fastBuffer : this.buffer;
        shelf.type = 'lowshelf';
        shelf.frequency.value = shelfHz;
        source.connect(shelf).connect(gain).connect(this.backing);
        const to = boss(part) ? bassDb : 0;
        const ramp = from === to ? 0 : Math.min((to > from ? 1 : 8)*60/this.map.bpm, Math.max(0,end-offset)/speed);
        shelf.gain.setValueAtTime(from, at);
        if (ramp) shelf.gain.linearRampToValueAtTime(to, at+ramp);
        gain.gain.setValueAtTime(0, at);
        gain.gain.linearRampToValueAtTime(1, at+fadeIn);
        const deck = { part, at, offset, end, speed, from, to, ramp, source, shelf, gain,
            availableUntil:at + source.buffer.duration - start };
        if (boss(part) && this.kickSample && this.map.beats.length) {
            const hits = this.map.beats.filter(t => t >= offset)
                .map(t => Math.round(clockAt(deck,t)*this.ctx.sampleRate));
            deck.kick = new AudioWorkletNode(this.ctx,'boss-kick',{numberOfInputs:0, numberOfOutputs:2, outputChannelCount:[1,1],
                processorOptions:{hits,...this.kickSample}});
            deck.kick.connect(this.master,0);
            deck.kick.connect(this.backing.gain,1);
            deck.kickLevel = deck.kick.parameters.get('level');
        }
        this.voices.add(deck);
        source.onended = () => { this.disconnect(deck); this.tick(); };
        source.start(at, start);
        return deck;
    }
    disconnect(d) {
        if (d.kick) { d.kick.disconnect(); d.kick.port.postMessage('stop'); d.kick.port.close(); }
        d.source.disconnect(); d.shelf.disconnect(); d.gain.disconnect();
        this.voices.delete(d);
        if (this.retired && !this.voices.size) this.master.disconnect();
    }
    discard(d) {
        d.source.onended = null;
        d.source.stop();
        this.disconnect(d);
    }
    disposeDecks() {
        for (const d of this.voices) this.discard(d);
        this.current = this.next = this.cut = this.requested = this.afterCut = null;
        this.editPending = false;
    }
    close() { this.disposeDecks(); this.master.disconnect(); this.playing = false; }
    retire() { this.retired = true; if (!this.voices.size) this.master.disconnect(); }
    start(at = this.ctx.currentTime + lead, fadeIn = .04) {
        this.disposeDecks();
        this.ended = false;
        this.playing = true;
        this.current = this.deck('intro', at, this.range('intro').start, 0, fadeIn);
        this.plan();
    }
    seek({part, pos, bass}) {
        const now = this.ctx.currentTime, at = now + .012;
        for (const d of this.voices) {
            if (d.at >= now) this.discard(d);
            else {
                const gain = d.gain.gain, level = gain.value;
                gain.cancelScheduledValues(now);
                gain.setValueAtTime(level,now);
                gain.linearRampToValueAtTime(0,at);
                if (d.kickLevel) {
                    const level = d.kickLevel.value;
                    d.kickLevel.cancelScheduledValues(now);
                    d.kickLevel.setValueAtTime(level,now);
                    d.kickLevel.linearRampToValueAtTime(0,at);
                }
                d.source.stop(at);
            }
        }
        this.current = this.next = this.cut = this.requested = this.afterCut = null;
        this.editPending = false;
        this.ended = pos >= this.map.duration;
        this.playing = !this.ended;
        this.cutOffset = pos;
        if (this.ended) return;
        this.current = this.deck(part, at, pos, bass, .012);
        this.plan();
    }
    plan(request = null, incoming = null, requestedAt = this.ctx.currentTime, point = null) {
        const d = this.current;
        if (this.next) this.discard(this.next);
        this.next = null;
        if (this.cut != null) d.gain.gain.cancelScheduledValues(this.fadeAt - .000001);
        const end = Math.min(d.end, this.map.duration);
        if (!point && (request || incoming)) {
            const dest = incoming ?? this;
            const target = openingChord(dest.map.chords, dest.range(incoming ? 'intro' : request));
            const wait = request === 'outro' ? outroWait : maxWait;
            const deadline = d.offset + (requestedAt + wait - d.at)*d.speed;
            point = transitionPoint(this.map,d.offset,end,position(d,this.ctx.currentTime+lead),deadline,target);
        }
        point ??= {offset:end};
        this.requestedAt = requestedAt;
        this.cutLabel = point.label;
        this.cutOffset = point.offset;
        this.cut = clockAt(d, this.cutOffset);
        // Only the incoming deck owns the downbeat at the cut, including on a boss loop.
        if (d.kickLevel) {
            d.kickLevel.cancelScheduledValues(this.ctx.currentTime);
            // Beat onsets are rounded to sample frames. Close just before that frame
            // so a fractional clock time cannot leak the outgoing kick's first sample.
            d.kickLevel.setValueAtTime(1,Math.max(d.at,this.cut-.003));
            d.kickLevel.linearRampToValueAtTime(0,Math.max(d.at,this.cut-1/this.ctx.sampleRate));
        }
        this.requested = request;
        const part = incoming ? null : request ?? (this.map.adaptive === false || d.part === 'outro' || d.part === 'recording' ? null : d.part === 'intro' ? 'body' : d.part);
        const fade = clamp(2*60/this.map.bpm, .48, 1);
        // An outgoing deck plays beyond the cut while the incoming downbeat lands exactly on it.
        // At the physical end of a recording, use its final tail for the fade instead.
        const tail = Math.max(0, Math.min(fade, d.availableUntil - this.cut));
        const overlap = (part || incoming) && tail >= .04 ? tail : 0;
        this.fadeAt = overlap ? this.cut : Math.max(d.at, this.cut-fade);
        const stop = this.cut + overlap;
        d.gain.gain.setValueAtTime(1, this.fadeAt);
        d.gain.gain.linearRampToValueAtTime(0, stop);
        d.source.stop(stop);
        if (part) this.next = this.deck(part, this.cut, this.range(part).start, bassAt(d,this.cut), overlap || fade);
        return { at:this.cut, fade:overlap || fade, label:this.cutLabel, offset:this.cutOffset };
    }
    tick() {
        if (!this.current || this.ctx.currentTime < this.cut) return;
        this.current = this.next;
        this.next = this.cut = this.requested = null;
        if (this.current) {
            this.plan();
            if (this.editPending) { this.editPending = false; this.updateSections(); }
        }
        else { this.ended = true; this.playing = false; }
    }
    request(part, requestedAt = this.ctx.currentTime) {
        this.tick();
        if (this.map.adaptive === false || !this.current) return;
        if (part !== 'outro' && this.next && this.cut - this.ctx.currentTime < lead) {
            this.afterCut = {part, requestedAt};
            return;
        }
        this.afterCut = null;
        this.plan(part === this.current.part ? null : part,null,requestedAt);
    }
    updateSections() {
        this.tick();
        const d = this.current;
        if (!d) return;
        if (this.cut - this.ctx.currentTime < lead) { this.editPending = true; return; }
        const { start, end } = this.range(d.part);
        const pos = position(d, this.ctx.currentTime + lead);
        if (pos >= start && pos < end) {
            d.end = end;
            this.plan(this.requested,null,this.requestedAt);
        } else this.plan(this.requested ?? d.part,null,this.requested ? this.requestedAt : this.ctx.currentTime);
    }
    auditionEnding(selected) {
        const part = selected === 'peak' && this.current?.part === 'fast' ? 'fast' : selected;
        const { start, end } = this.range(part);
        this.disposeDecks();
        this.playing = true;
        this.ended = false;
        const offset = Math.max(start, end - 2*60/this.map.bpm);
        this.current = this.deck(part,this.ctx.currentTime+lead,offset,boss(part) ? bassDb : 0,.04);
        this.plan();
    }
    previewChange() {
        this.tick();
        if (!this.requested) return;
        const offset = this.cutOffset - 2*60/this.map.bpm;
        if (offset <= position(this.current,this.ctx.currentTime + lead)) return;
        const { part, end } = this.current;
        const oldClock = clockAt(this.current,offset), newClock = this.ctx.currentTime+lead;
        const bass = bassAt(this.current,oldClock);
        const request = this.requested;
        const point = {offset:this.cutOffset, label:this.cutLabel};
        const requestedAt = this.requestedAt + (newClock-oldClock);
        this.disposeDecks();
        this.current = this.deck(part,newClock,offset,bass,.04,end);
        this.plan(request,null,requestedAt,point);
    }
    state() {
        this.tick();
        if (this.afterCut && this.current && this.cut - this.ctx.currentTime >= lead) {
            const {part,requestedAt} = this.afterCut;
            this.afterCut = null;
            this.request(part,requestedAt);
        }
        const d = this.current;
        if (!d) return { part:this.ended && this.map.adaptive !== false ? 'outro' : 'intro',
            pos:this.ended ? this.cutOffset : this.range('intro').start, rate:1, bass:0 };
        return { part:d.part, pos:position(d,this.ctx.currentTime), rate:d.speed,
            bass:bassAt(d,this.ctx.currentTime), next:this.requested, label:this.cutLabel, offset:this.cutOffset,
            remaining:this.cut-this.ctx.currentTime };
    }
}
