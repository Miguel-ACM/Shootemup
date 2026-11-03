// Render against the audio clock so the beat continues when the tab is in the background.
class BossKick extends AudioWorkletProcessor {
    static get parameterDescriptors() {
        return [{ name:'level', defaultValue:1 }];
    }
    constructor({ processorOptions }) {
        super();
        this.hits = processorOptions.hits;
        this.pcm = processorOptions.pcm;
        this.duck = processorOptions.duck;
        this.first = 0;
        this.port.onmessage = () => { this.stopped = true; };
    }
    process(inputs, outputs, { level }) {
        if (this.stopped) return false;
        const out = outputs[0][0], sidechain = outputs[1]?.[0], end = currentFrame + out.length;
        while (this.first < this.hits.length && this.hits[this.first] + this.pcm.length <= currentFrame) this.first++;
        for (let n = this.first; n < this.hits.length && this.hits[n] < end; n++) {
            const offset = this.hits[n] - currentFrame;
            const last = Math.min(out.length,offset + this.pcm.length);
            for (let i = Math.max(0,offset); i < last; i++) {
                const gate = level.length === 1 ? level[0] : level[i];
                out[i] += this.pcm[i-offset] * gate;
                if (sidechain) sidechain[i] = Math.min(sidechain[i], -this.duck[i-offset] * gate);
            }
        }
        return this.first < this.hits.length;
    }
}
registerProcessor('boss-kick',BossKick);
