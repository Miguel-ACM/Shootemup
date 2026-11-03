// All cue coordinates are seconds in the unmodified recording. The audio clock owns cuts.
export const mix = { rate: 1.06, bass: 4.5, hz: 145, releaseBeats: 8 };
const edge = .008;
const lead = .045;
const clamp = (n, lo, hi) => Math.max(lo, Math.min(hi, n));

function position(deck, at) {
    const dt = Math.max(0, at - deck.at);
    const t = Math.min(dt, deck.ramp);
    return deck.offset + dt * deck.to + (deck.from - deck.to) * (t - (deck.ramp ? t*t/(2*deck.ramp) : 0));
}

function clockAt(deck, offset) {
    const distance = Math.max(0, offset - deck.offset);
    const inRamp = deck.ramp * (deck.from + deck.to) / 2;
    if (distance >= inRamp) return deck.at + deck.ramp + (distance - inRamp) / deck.to;
    const slope = (deck.to - deck.from) / deck.ramp;
    return deck.at + 2 * distance / (deck.from + Math.sqrt(deck.from * deck.from + 2*slope*distance));
}

function fraction(deck, now) {
    return deck.ramp ? clamp((now - deck.at) / deck.ramp, 0, 1) : 1;
}

export class Transport {
    constructor(ctx, buffer, map, volume) {
        this.ctx = ctx;
        this.buffer = buffer;
        this.map = map;
        this.master = ctx.createGain();
        this.master.gain.value = volume;
        this.master.connect(ctx.destination);
        this.playing = false;
        this.ended = false;
    }
    range(part) {
        return this.map.adaptive === false ? { start: 0, end: this.map.duration } : this.map.sections[part];
    }
    deck(part, at, offset, from, bassFrom) {
        const source = this.ctx.createBufferSource();
        const shelf = this.ctx.createBiquadFilter();
        const gain = this.ctx.createGain();
        source.buffer = this.buffer;
        shelf.type = 'lowshelf';
        shelf.frequency.value = mix.hz;
        source.connect(shelf).connect(gain).connect(this.master);
        const to = part === 'peak' ? mix.rate : 1;
        const bassTo = part === 'peak' ? mix.bass : 0;
        const beats = part === 'peak' ? 1 : mix.releaseBeats;
        const ramp = (from === to && bassFrom === bassTo) ? 0 :
            Math.min(beats * 60/this.map.bpm, (this.range(part).end - offset) / Math.max(to, from));
        source.playbackRate.setValueAtTime(from, at);
        shelf.gain.setValueAtTime(bassFrom, at);
        if (ramp) {
            source.playbackRate.linearRampToValueAtTime(to, at + ramp);
            shelf.gain.linearRampToValueAtTime(bassTo, at + ramp);
        }
        gain.gain.setValueAtTime(0, at);
        gain.gain.linearRampToValueAtTime(1, at + edge);
        source.onended = () => { source.disconnect(); shelf.disconnect(); gain.disconnect(); this.tick(); };
        source.start(at, offset);
        return { part, at, offset, from, to, bassFrom, bassTo, ramp, source, gain };
    }
    start() {
        this.disposeDecks();
        this.ended = false;
        this.playing = true;
        this.current = this.deck('intro', this.ctx.currentTime + lead, 0, 1, 0);
        this.plan();
    }
    disposeDecks() {
        for (const deck of [this.current, this.next]) {
            if (!deck) continue;
            deck.source.onended = null;
            deck.source.stop();
            deck.source.disconnect();
            deck.gain.disconnect();
        }
        this.current = this.next = null;
        this.cut = null;
        this.requested = null;
    }
    close() { this.disposeDecks(); this.master.disconnect(); this.playing = false; }
    // Place the incoming start, outgoing fade and outgoing stop on one exact audio-clock time.
    plan(request = null) {
        const d = this.current;
        if (!d) return;
        if (this.next) {
            this.next.source.onended = null;
            this.next.source.stop();
            this.next.source.disconnect();
            this.next.gain.disconnect();
            this.next = null;
        }
        if (this.cut != null) d.gain.gain.cancelScheduledValues(this.cut - edge - .000001);
        const end = Math.min(this.range(d.part).end, this.buffer.duration);
        const pos = position(d, this.ctx.currentTime + lead);
        const boundary = request ? this.map.boundaries.find(b => b > pos && b <= end) ?? end : end;
        this.cut = clockAt(d, boundary);
        this.requested = request;
        this.cutOffset = boundary;
        const nextPart = request ?? (this.map.adaptive === false || d.part === 'outro' ? null : d.part === 'intro' ? 'body' : d.part);
        const t = fraction(d, this.cut);
        if (nextPart) this.next = this.deck(nextPart, this.cut, this.range(nextPart).start,
            d.from + t*(d.to-d.from), d.bassFrom + t*(d.bassTo-d.bassFrom));
        d.gain.gain.setValueAtTime(1, this.cut - edge);
        d.gain.gain.linearRampToValueAtTime(0, this.cut);
        d.source.stop(this.cut);
    }
    tick() {
        if (!this.current || this.ctx.currentTime < this.cut) return;
        this.current = this.next;
        this.next = null;
        this.cut = null;
        this.requested = null;
        if (this.current) this.plan();
        else { this.ended = true; this.playing = false; }
    }
    request(part) {
        this.tick();
        if (this.map.adaptive === false || !this.current) return;
        // If the next downbeat is already in the device's output buffer, let it land.
        if (this.cut - this.ctx.currentTime < lead) return;
        this.plan(part === this.current.part ? null : part);
    }
    previewChange() {
        this.tick();
        if (!this.requested) return;
        const offset = this.cutOffset - 2 * 60 / this.map.bpm;
        if (offset <= position(this.current, this.ctx.currentTime + lead)) return;
        const { part, from, to, bassFrom, bassTo } = this.current;
        const f = fraction(this.current, clockAt(this.current, offset));
        const request = this.requested;
        this.disposeDecks();
        this.current = this.deck(part, this.ctx.currentTime + lead, offset, from + (to-from)*f, bassFrom + (bassTo-bassFrom)*f);
        this.plan(request);
    }
    state() {
        this.tick();
        const d = this.current;
        if (!d) return { part: this.ended && this.map.adaptive !== false ? 'outro' : 'intro',
            pos: this.ended ? this.buffer.duration : 0, rate: 1, bass: 0 };
        const t = fraction(d, this.ctx.currentTime);
        return { part: d.part, pos: position(d, this.ctx.currentTime), rate: d.from + t*(d.to-d.from),
            bass: d.bassFrom + t*(d.bassTo-d.bassFrom), next: this.requested, remaining: this.cut - this.ctx.currentTime };
    }
}
