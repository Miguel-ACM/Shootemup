// Cue coordinates are seconds in the original recording. The fast audition is already time-stretched.
const bassDb = 4.5, shelfHz = 145, lead = .045;
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

export class Transport {
    constructor(ctx, buffer, map, volume, kickPCM) {
        Object.assign(this, { ctx, buffer, map, kickPCM, playing:false, ended:false });
        this.voices = new Set();
        this.master = ctx.createGain();
        this.master.gain.value = volume;
        this.master.connect(ctx.destination);
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
        source.connect(shelf).connect(gain).connect(this.master);
        const to = boss(part) ? bassDb : 0;
        const ramp = from === to ? 0 : Math.min((to > from ? 1 : 8)*60/this.map.bpm, Math.max(0,end-offset)/speed);
        shelf.gain.setValueAtTime(from, at);
        if (ramp) shelf.gain.linearRampToValueAtTime(to, at+ramp);
        gain.gain.setValueAtTime(0, at);
        gain.gain.linearRampToValueAtTime(1, at+fadeIn);
        const deck = { part, at, offset, end, speed, from, to, ramp, source, shelf, gain,
            availableUntil:at + source.buffer.duration - start };
        if (boss(part) && this.kickPCM && this.map.beats.length) {
            const hits = this.map.beats.filter(t => t >= offset)
                .map(t => Math.round(clockAt(deck,t)*this.ctx.sampleRate));
            deck.kick = new AudioWorkletNode(this.ctx,'boss-kick',{numberOfInputs:0, outputChannelCount:[1],
                processorOptions:{hits,pcm:this.kickPCM}});
            deck.kick.connect(this.master);
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
    plan(request = null, exit = false) {
        const d = this.current;
        if (this.next) this.discard(this.next);
        this.next = null;
        if (this.cut != null) d.gain.gain.cancelScheduledValues(this.fadeAt - .000001);
        const end = Math.min(d.end, this.map.duration);
        const pos = position(d, this.ctx.currentTime + lead);
        this.cutOffset = request || exit ? this.map.boundaries.find(b => b > pos && b <= end) ?? end : end;
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
        const part = exit ? null : request ?? (this.map.adaptive === false || d.part === 'outro' || d.part === 'recording' ? null : d.part === 'intro' ? 'body' : d.part);
        const fade = clamp(2*60/this.map.bpm, .48, 1);
        // An outgoing deck plays beyond the cut while the incoming downbeat lands exactly on it.
        // At the physical end of a recording, use its final tail for the fade instead.
        const tail = Math.max(0, Math.min(fade, d.availableUntil - this.cut));
        const overlap = (part || exit) && tail >= .04 ? tail : 0;
        this.fadeAt = overlap ? this.cut : Math.max(d.at, this.cut-fade);
        const stop = this.cut + overlap;
        d.gain.gain.setValueAtTime(1, this.fadeAt);
        d.gain.gain.linearRampToValueAtTime(0, stop);
        d.source.stop(stop);
        if (part) this.next = this.deck(part, this.cut, this.range(part).start, bassAt(d,this.cut), overlap || fade);
        return { at:this.cut, fade:overlap || fade };
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
    request(part) {
        this.tick();
        if (this.map.adaptive === false || !this.current) return;
        if (this.cut - this.ctx.currentTime < lead) {
            this.afterCut = part;
            return;
        }
        this.plan(part === this.current.part ? null : part);
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
            this.plan(this.requested);
        } else this.plan(this.requested ?? d.part); // Finish the outgoing progression before the jump.
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
        const bass = bassAt(this.current,clockAt(this.current,offset));
        const request = this.requested;
        this.disposeDecks();
        this.current = this.deck(part,this.ctx.currentTime+lead,offset,bass,.04,end);
        this.plan(request);
    }
    state() {
        this.tick();
        if (this.afterCut && this.current && this.cut - this.ctx.currentTime >= lead) {
            const part = this.afterCut;
            this.afterCut = null;
            this.request(part);
        }
        const d = this.current;
        if (!d) return { part:this.ended && this.map.adaptive !== false ? 'outro' : 'intro',
            pos:this.ended ? this.cutOffset : this.range('intro').start, rate:1, bass:0 };
        return { part:d.part, pos:position(d,this.ctx.currentTime), rate:d.speed,
            bass:bassAt(d,this.ctx.currentTime), next:this.requested, remaining:this.cut-this.ctx.currentTime };
    }
}
