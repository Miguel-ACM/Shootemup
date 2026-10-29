"""Layered procedural sound effects for Shootemup, rendered offline to WAV.

Run: python Tools/synth_sfx.py [--only coin,shot1]
Needs numpy and scipy. Writes Assets/Resources/Sounds/Synth/<cue>_NN.wav (44.1 kHz mono)
and an A/B review page in ArtSource/AudioReview/Synth/ (view it with
python -m http.server -d ArtSource/AudioReview/Synth 8770, then open http://127.0.0.1:8770).

Each cue is a small patch: a transient, a pitched body (square/saw/FM) and a filtered noise
layer, shaped by envelopes, saturation and a short PSX-style room. Unlike the rejected single
oscillator bank, every sound is designed per cue. Replaced cues are loudness-matched to the
clip they replace so existing call volumes stay meaningful.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import uuid
import wave

import numpy as np
from scipy.signal import butter, lfilter, sosfilt

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/Sounds/Synth'
REVIEW = ROOT / 'ArtSource/AudioReview/Synth'
SR = 44100
rng = np.random.default_rng(0)


# ---------------------------------------------------------------- building blocks

def n_of(seconds):
    return max(1, int(round(seconds * SR)))


def time(seconds):
    return np.arange(n_of(seconds)) / SR


def env(seconds, attack=.001, tau=.05, hold=0.0, power=1.0):
    """Linear attack, optional hold, exponential decay with time constant tau."""
    t = time(seconds)
    e = np.exp(-np.maximum(t - attack - hold, 0) / tau) ** power
    if attack > 0:
        e *= np.clip(t / attack, 0, 1)
    return e


def glide(seconds, start, end, rate=20.0):
    """Exponential approach from start to end; rate is in 1/seconds."""
    t = time(seconds)
    return end + (start - end) * np.exp(-t * rate)


def ramp(seconds, start, end, curve=1.0):
    u = np.linspace(0, 1, n_of(seconds)) ** curve
    return start * (end / start) ** u if start > 0 and end > 0 else start + (end - start) * u


def phase_of(freq):
    return np.cumsum(np.broadcast_to(freq, freq.shape) / SR)


def _blep(ph, dt):
    out = np.zeros_like(ph)
    a = ph < dt
    x = ph[a] / dt[a]
    out[a] = x + x - x * x - 1
    b = ph > 1 - dt
    x = (ph[b] - 1) / dt[b]
    out[b] = x * x + x + x + 1
    return out


def osc(freq, shape='sine', pw=.5, phase0=0.0):
    freq = np.asarray(freq, dtype=float)
    ph = (phase_of(freq) + phase0) % 1.0
    dt = np.clip(freq / SR, 1e-6, .5)
    if shape == 'sine':
        return np.sin(2 * np.pi * ph)
    if shape == 'saw':
        return 2 * ph - 1 - _blep(ph, dt)
    if shape == 'square':
        sq = np.where(ph < pw, 1.0, -1.0)
        return sq + _blep(ph, dt) - _blep((ph - pw) % 1.0, dt)
    if shape == 'tri':
        return 2 * np.abs(2 * ph - 1) - 1
    raise ValueError(shape)


def fm(freq, ratio, index, shape='sine'):
    """Two-operator FM; index may be an envelope array."""
    freq = np.asarray(freq, dtype=float)
    mod = np.sin(2 * np.pi * phase_of(freq * ratio))
    return np.sin(2 * np.pi * phase_of(freq) + index * mod)


def noise(seconds, color='white'):
    x = rng.standard_normal(n_of(seconds))
    if color == 'pink':
        b, a = [0.049922035, -0.095993537, 0.050612699, -0.004408786], [1, -2.494956002, 2.017265875, -0.522189400]
        x = lfilter(b, a, x) * 4
    elif color == 'brown':
        x = lfilter([1], [1, -.995], x) * .08
    return x


def svf(x, cutoff, q=.7, mode='lp'):
    """Time-varying TPT state-variable filter (Zavalishin). cutoff may be an array."""
    cutoff = np.broadcast_to(np.asarray(cutoff, dtype=float), x.shape)
    g = np.tan(np.pi * np.clip(cutoff, 20, SR * .45) / SR)
    k = 1 / q
    a1 = 1 / (1 + g * (g + k))
    ic1 = ic2 = 0.0
    out = np.empty_like(x)
    want = {'lp': 0, 'bp': 1, 'hp': 2}[mode]
    xs = x.tolist()
    gs = g.tolist()
    a1s = a1.tolist()
    for i in range(len(xs)):
        gi = gs[i]
        v3 = xs[i] - ic2
        v1 = a1s[i] * ic1 + gi * a1s[i] * v3
        v2 = ic2 + gi * v1
        ic1 = 2 * v1 - ic1
        ic2 = 2 * v2 - ic2
        out[i] = v2 if want == 0 else v1 if want == 1 else xs[i] - k * v1 - v2
    return out


def lp(x, hz, order=2):
    return sosfilt(butter(order, hz, 'low', fs=SR, output='sos'), x)


def hp(x, hz, order=2):
    return sosfilt(butter(order, hz, 'high', fs=SR, output='sos'), x)


def bp(x, lo, hi, order=2):
    return sosfilt(butter(order, [lo, hi], 'band', fs=SR, output='sos'), x)


def drive(x, amount):
    return np.tanh(x * amount) / np.tanh(amount)


def crush(x, rate, bits=None):
    """Sample-and-hold decimation (aliasing grit) plus optional bit reduction."""
    step = SR / rate
    idx = (np.floor(np.arange(len(x)) / step) * step).astype(int)
    y = x[np.clip(idx, 0, len(x) - 1)]
    if bits:
        q = 2 ** (bits - 1)
        y = np.round(y * q) / q
    return y


def crackle(seconds, density, decay, lo=1500, hi=7000, seed=None):
    """Sparse random impulses whose density decays: debris and embers."""
    r = np.random.default_rng(seed) if seed is not None else rng
    n = n_of(seconds)
    t = np.arange(n) / SR
    p = density / SR * np.exp(-t / decay)
    hits = (r.random(n) < p) * r.uniform(.3, 1, n) * r.choice([-1, 1], n)
    grain = np.exp(-np.arange(n_of(.004)) / (SR * .0008))
    return bp(np.convolve(hits, grain)[:n], lo, hi)


def sparkle(seconds, density, decay, base=2400, spread=2.2, seed=None):
    """Random short FM bell grains: glints, ice and magic dust."""
    r = np.random.default_rng(seed) if seed is not None else rng
    out = np.zeros(n_of(seconds))
    t = 0.0
    while t < seconds:
        t += r.exponential(1 / max(density * np.exp(-t / decay), 1e-3))
        if t >= seconds:
            break
        f = base * spread ** r.uniform(0, 1)
        g = np.exp(-t / decay) * r.uniform(.4, 1)
        grain = fm(np.full(n_of(.09), f), 3.5, 1.2 * env(.09, .0005, .02)) * env(.09, .0005, .025) * g
        place(out, grain, t)
    return out


def chord(freqs, seconds, shape='square', pw=.5, detune=0.0):
    out = np.zeros(n_of(seconds))
    for f in freqs:
        out += osc(np.full(len(out), f * (1 + detune)), shape, pw)
        if detune:
            out += osc(np.full(len(out), f * (1 - detune)), shape, pw)
    return out / max(1, len(freqs))


def place(out, x, at):
    i = int(round(at * SR))
    if i >= len(out):
        return out
    m = min(len(x), len(out) - i)
    seg = np.array(x[:m], dtype=float)
    # Layers that end (or are cut) while still sounding would click; ease every layer out.
    f = min(m, n_of(.006))
    seg[m - f:] *= np.linspace(1, 0, f) ** 2
    out[i:i + m] += seg
    return out


def mix(seconds, *layers):
    out = np.zeros(n_of(seconds))
    for at, gain, x in layers:
        place(out, gain * x, at)
    return out


def fit(x, seconds):
    n = n_of(seconds)
    return np.pad(x, (0, max(0, n - len(x))))[:n]


def room(x, size=.5, decay=.45, mix_amount=.15, damp=5000, predelay=.008):
    """Small Schroeder room in the spirit of the PS1 SPU reverb: four combs, two allpasses."""
    tail = n_of(.25 + size * 1.2)
    dry = np.pad(x, (0, tail))
    wet = np.zeros_like(dry)
    pre = np.pad(dry, (n_of(predelay), 0))[:len(dry)]
    for ms in (29.7, 37.1, 41.1, 43.7):
        d = n_of(ms / 1000 * (.6 + size))
        g = decay ** (d / n_of(.05))
        a = np.zeros(d + 1)
        a[0], a[d] = 1, -g
        wet += lfilter([1], a, pre)
    for ms, g in ((5.0, .7), (1.7, .7)):
        d = n_of(ms / 1000)
        b = np.zeros(d + 1)
        a = np.zeros(d + 1)
        b[0], b[d] = -g, 1
        a[0], a[d] = 1, -g
        wet = lfilter(b, a, wet)
    wet = lp(wet, damp)
    wet *= mix_amount / max(np.max(np.abs(wet)), 1e-9) * max(np.max(np.abs(x)), 1e-9)
    return dry + wet


def finish(x, psx=True, hp_hz=35):
    """DC removal, PSX-like band limit, de-click fades, trim of silent tail, peak normalize."""
    x = hp(x, hp_hz)
    if psx:
        x = lp(x, 11000, 4)
    a = np.abs(x)
    above = np.nonzero(a > a.max() * 10 ** (-62 / 20))[0]
    x = x[:above[-1] + n_of(.01)] if len(above) else x
    fade_in, fade_out = n_of(.0006), n_of(.012)
    x[:fade_in] *= np.linspace(0, 1, fade_in)
    x[-fade_out:] *= np.linspace(1, 0, fade_out)
    x = drive(x / np.max(np.abs(x)) * 1.05, 1.15)
    return x / np.max(np.abs(x)) * 10 ** (-1 / 20)


# ---------------------------------------------------------------- cue designs
# Every design receives v (variant 0..3) and returns raw float audio.

def detune(v, amount=.025):
    return 1 + (v - 1.5) * amount


def shot1(v):
    # Base pulse cannon: a rounded square "pew" over a short sub thump, clicked in.
    d = .1
    k = detune(v, .03)
    body = osc(glide(d, 1250 * k, 420 * k, 38), 'square', pw=.28 + .03 * v)
    body = svf(body, glide(d, 7000, 1400, 30), q=1.1) * env(d, .0012, .026)
    sub = osc(glide(d, 230 * k, 85, 45), 'sine') * env(d, .001, .022)
    click = hp(noise(.006), 3500) * env(.006, 0, .0012)
    return mix(d, (0, .55, body), (0, .55, sub), (0, .35, click))


def enemyHit(v):
    # Bright, tiny armour tick for player rounds landing on ordinary craft.
    d = .055
    k = detune(v, .05)
    tick = osc(glide(d, 2300 * k, 1250 * k, 70), 'square', .3) * env(d, .0005, .009)
    grit = svf(noise(d), 3200 * k, q=2.5, mode='bp') * env(d, 0, .006)
    knock = osc(glide(d, 420 * k, 260, 50), 'sine') * env(d, .0005, .012)
    return room(mix(d, (0, .4, tick), (0, .7, grit), (0, .5, knock)), .1, .3, .06)


def bossHit(v):
    # Heavier inharmonic clank on boss plating and weakpoints.
    d = .16
    k = detune(v, .04)
    clank = fm(np.full(n_of(d), 610 * k), 1.414, 4 * env(d, 0, .02)) * env(d, .0005, .035)
    ring = fm(np.full(n_of(d), 1480 * k), 2.76, 1.5 * env(d, 0, .03)) * env(d, .0005, .05)
    thud = osc(glide(d, 190 * k, 70, 40), 'sine') * env(d, .0008, .03)
    grit = hp(noise(d), 2500) * env(d, 0, .007)
    return room(mix(d, (0, .45, clank), (0, .2, ring), (0, .6, thud), (0, .4, grit)), .2, .35, .1)


def hurt1(v):
    # Player loses a hull segment: crunch, a falling glitch buzz and a short alarm chirp.
    d = .46
    k = detune(v, .03)
    crunch = drive(svf(noise(d), glide(d, 6000, 500, 18), q=.9) * env(d, .0008, .06), 3)
    buzz = osc(glide(d, 900 * k, 95, 9), 'square', .35) * osc(np.full(n_of(d), 47), 'square')
    buzz = svf(crush(buzz, 5500, 6), 2600, q=1.4) * env(d, .002, .12)
    thump = osc(glide(d, 160, 42, 22), 'sine') * env(d, .001, .09)
    chirp = fit(osc(np.repeat([1320 * k, 990 * k], n_of(.045)), 'square', .25), .09) * env(.09, .001, .05)
    return room(mix(d, (0, .55, crunch), (0, .32, buzz), (0, .7, thump), (.035, .14, chirp)), .3, .4, .12)


def enemyExplosion1(v):
    # Small craft pop: punchy filtered blast with a sub thump and a few embers.
    d = .55
    k = detune(v, .05)
    blast = svf(noise(d, 'pink'), glide(d, 7000 * k, 220, 14), q=.8) * env(d, .0012, .085)
    blast = drive(blast * 1.6, 2.2)
    thump = osc(glide(d, 165 * k, 46, 26), 'sine') * env(d, .001, .07)
    pop = hp(noise(.01), 1800) * env(.01, 0, .002)
    embers = crackle(d, 160, .11, seed=v)
    return room(mix(d, (0, .6, blast), (0, .8, thump), (0, .3, pop), (.03, .35, embers)), .35, .4, .13)


def explosionLarge(v):
    # Phase changes and big wrecks: a doubled blast, rolling rumble and debris.
    d = 1.3
    k = detune(v, .04)
    first = drive(svf(noise(d, 'pink'), glide(d, 6500 * k, 160, 7), q=.75) * env(d, .002, .22), 2.5)
    second = drive(svf(noise(d, 'pink'), glide(d, 3500 * k, 120, 6), q=.8) * env(d, .002, .25), 2)
    rumble = lp(noise(d, 'brown'), 220) * env(d, .03, .5)
    boom = osc(glide(d, 120 * k, 32, 9), 'sine') * env(d, .002, .25)
    debris = crackle(d, 240, .35, 900, 6000, seed=10 + v)
    return room(mix(d, (0, .55, first), (.085, .45, second), (0, .6, rumble), (0, .9, boom),
                    (.06, .4, debris)), .7, .55, .22)


def bossDeath(v):
    # Boss shatter accent: a cluster of booms, a downward power failure and a long tail.
    d = 3.2
    out = mix(d, (0, 1.0, explosionLarge(v)), (.18, .7, explosionLarge((v + 1) % 4)),
              (.42, .55, enemyExplosion1((v + 2) % 4)))
    failing = svf(osc(ramp(1.8, 700, 35, .6), 'saw'), ramp(1.8, 3000, 200), q=2.2) * env(1.8, .01, .6)
    sub = osc(glide(d, 70, 26, 2.2), 'sine') * env(d, .01, .8)
    debris = crackle(d, 120, 1.0, 600, 5000, seed=30 + v)
    out += mix(d, (.05, .28, failing), (0, .8, sub), (.3, .35, debris))
    return room(out, 1.0, .65, .3, damp=3500, predelay=.02)


def playerDead(v):
    # The ship breaks up: crunch, a long falling engine whine, booms and settling debris.
    d = 2.6
    crunch = mix(d, (0, 1, hurt1(v)))
    whine = svf(osc(ramp(1.9, 1100, 28, .5), 'saw') + .5 * osc(ramp(1.9, 1650, 40, .5), 'square', .3),
                ramp(1.9, 4500, 150), q=2.8) * env(1.9, .005, .9)
    whine = crush(whine, 9000)
    boom = mix(d, (0, 1, explosionLarge(v)))
    sub = osc(glide(d, 85, 24, 1.8), 'sine') * env(d, .01, .7)
    debris = crackle(d, 140, .9, 500, 4500, seed=50 + v)
    out = mix(d, (0, .5, crunch), (.02, .35, whine), (.12, .9, boom), (.1, .8, sub), (.25, .4, debris))
    return room(out, .9, .6, .28, damp=3800, predelay=.015)


def blankbullet(v):
    # Screen clear / shield burst: a shockwave sweep, a sub drop and a falling glass shimmer.
    d = 1.3
    k = detune(v, .03)
    sweep = svf(noise(d, 'pink'), np.concatenate([ramp(.09, 400, 7000, .8), ramp(d - .09, 7000, 500, .5)])[:n_of(d)],
                q=2.2, mode='bp') * env(d, .015, .3)
    thump = osc(glide(d, 140 * k, 32, 7), 'sine') * env(d, .003, .22)
    notes = [2637, 1976, 1568, 1319, 988]
    shimmer = np.zeros(n_of(d))
    for i, f in enumerate(notes):
        b = fm(np.full(n_of(.5), f * k), 3.0, 2 * env(.5, 0, .08)) * env(.5, .001, .14)
        place(shimmer, b, .04 + i * .055)
    hiss = hp(noise(d), 5000) * env(d, .002, .06)
    out = mix(d, (0, .7, sweep), (0, .8, thump), (0, .22, shimmer), (0, .25, hiss))
    return room(out, .8, .55, .3, damp=6000)


def bossWarning(v):
    # Klaxon: two detuned square tones alternating over a rising saw drone.
    d = 2.9
    k = detune(v, .02)
    period = .3
    steps = int(2.4 / period)
    seq = np.concatenate([np.full(n_of(period), (622 if i % 2 == 0 else 466) * k) for i in range(steps)])
    vib = 1 + .006 * np.sin(2 * np.pi * 5.5 * np.arange(len(seq)) / SR)
    horn = osc(seq * vib, 'square', .42) + osc(seq * vib * 1.004, 'saw')
    gate = np.concatenate([env(period, .012, .25, hold=period * .55) for _ in range(steps)])
    horn = svf(horn * gate, 2200, q=1.6)
    drone_f = np.full(n_of(d), 55 * k)
    drone = svf(osc(drone_f, 'saw') + osc(drone_f * 1.006, 'saw'), ramp(d, 200, 1800), q=1.5) * env(d, .5, .9, hold=1.6)
    out = mix(d, (0, .45, horn), (0, .4, drone))
    return room(out, .9, .55, .25, damp=4000)


def coin(v):
    # Coin: the classic two-note square chime with a little bell on the second note.
    d = .3
    pw = (.5, .25, .35, .42)[v]
    a = osc(np.full(n_of(.055), 1976), 'square', pw) * env(.055, .001, .2)
    b = osc(np.full(n_of(d), 2637), 'square', pw) * env(d, .001, .07, hold=.02)
    bell = fm(np.full(n_of(d), 2637), 2.0, 1.2 * env(d, 0, .05)) * env(d, .001, .09)
    out = mix(d, (0, .35, a), (.05, .35, b), (.05, .3, bell))
    return room(lp(out, 9000), .2, .35, .12)


def mana(v):
    # Energy cell: a quick upward FM glissando with dust.
    d = .4
    k = detune(v, .03)
    gl = fm(ramp(.22, 700 * k, 2100 * k, .7), 1.5, 1.5 * env(.22, 0, .12)) * env(.22, .004, .1, hold=.05)
    ping = fm(np.full(n_of(.3), 2100 * k), 3.0, env(.3, 0, .05)) * env(.3, .001, .08)
    dust = sparkle(d, 40, .15, 3000, 2, seed=70 + v)
    return room(mix(d, (0, .45, gl), (.16, .3, ping), (.02, .2, dust)), .35, .45, .18)


def itemGet(v):
    # Item collected: a rising four-note arpeggio landing on a shimmering chord.
    d = 1.0
    k = detune(v, .015)
    notes = [784, 1047, 1319, 1568]
    out = np.zeros(n_of(d))
    for i, f in enumerate(notes):
        place(out, .3 * osc(np.full(n_of(.08), f * k), 'square', .3) * env(.08, .001, .05), i * .055)
    land = chord([1047 * k, 1319 * k, 1568 * k, 2093 * k], .7, 'square', .25, .003)
    vib = 1 + .5 * np.sin(2 * np.pi * 6 * time(.7))
    land = svf(land, 4500, q=.9) * env(.7, .004, .22) * (.85 + .15 * vib)
    bell = fm(np.full(n_of(.7), 2093 * k), 3.5, 1.4 * env(.7, 0, .1)) * env(.7, .001, .2)
    place(out, .35 * land + .25 * bell, .22)
    return room(out, .5, .5, .22)




def uiConfirm(v):
    # Confirm / purchase: two bright notes up.
    d = .26
    k = detune(v, .015)
    a = osc(np.full(n_of(.06), 1047 * k), 'square', .3) * env(.06, .001, .04)
    b = osc(np.full(n_of(.2), 1568 * k), 'square', .3) * env(.2, .001, .06)
    bell = fm(np.full(n_of(.2), 3136 * k), 2.0, env(.2, 0, .04)) * env(.2, .001, .05)
    return room(lp(mix(d, (0, .4, a), (.055, .4, b), (.055, .2, bell)), 8000), .2, .3, .1)


def uiSkip(v):
    # Skip / back: two softer notes down.
    d = .24
    k = detune(v, .015)
    a = osc(np.full(n_of(.06), 1175 * k), 'tri') * env(.06, .001, .04)
    b = osc(np.full(n_of(.18), 784 * k), 'tri') * env(.18, .001, .05)
    return room(mix(d, (0, .55, a), (.06, .55, b)), .2, .3, .08)




# ---- item cues (names match Sounds/Items so ItemSound can swap them)

def heal(v):
    d = .75
    k = detune(v, .02)
    f = ramp(.45, 420 * k, 880 * k, .8)
    rise = osc(f, 'sine') + .12 * osc(f * 3, 'sine') + osc(f * 1.009, 'sine')
    rise = rise * env(.45, .03, .2, hold=.15) * (1 + .3 * np.sin(2 * np.pi * 9 * time(.45)))
    tones = np.zeros(n_of(d))
    for i, f in enumerate([523, 659, 784, 1047]):
        place(tones, osc(np.full(n_of(.35), f * k), 'sine') * env(.35, .004, .12), .06 + i * .06)
    dust = sparkle(d, 50, .25, 2800, 2, seed=90 + v)
    return room(mix(d, (0, .35, rise), (0, .4, tones), (.05, .18, dust)), .5, .5, .25)


def timeStop(v):
    d = 1.1
    k = detune(v, .02)
    # A chord that grinds to a halt (tape-stop), struck by a clock-crystal ping.
    stop = np.exp(-time(.8) * 4.5)
    tape = osc(220 * k * stop, 'saw') + osc(330 * k * stop, 'saw') + .6 * osc(440 * k * stop, 'square', .3)
    tape = svf(tape, 2000 * stop + 120, q=1.4) * env(.8, .002, .5)
    ping = fm(np.full(n_of(1.0), 1760 * k), 2.01, 3 * env(1.0, 0, .05)) * env(1.0, .001, .35)
    tick = hp(noise(.01), 3000) * env(.01, 0, .002)
    whoosh = svf(noise(.6, 'pink'), ramp(.6, 6000, 200), q=1.2) * env(.6, .01, .2)
    out = mix(d, (0, .3, tape), (0, .3, ping), (0, .3, tick), (0, .3, whoosh))
    return room(out, .9, .6, .32)


def powerdown(v):
    d = .55
    k = detune(v, .03)
    f = ramp(.5, 700 * k, 60, .7)
    tone = svf(osc(f, 'saw') + osc(f * 1.5, 'square', .3) * .5, ramp(.5, 3500, 180), q=2) * env(.5, .003, .2)
    return room(mix(d, (0, .7, crush(tone, 11000))), .3, .4, .12)


def overdrive(v):
    d = .7
    k = detune(v, .03)
    f = ramp(.6, 70 * k, 240 * k, 1.3)
    growl = drive(osc(f, 'saw') + osc(f * 1.01, 'saw') + osc(f * .5, 'square'), 3)
    growl = svf(growl, ramp(.6, 300, 4500, 1.5), q=2.5) * env(.6, .02, .25, hold=.3)
    pulse = .7 + .3 * np.tanh(4 * np.sin(2 * np.pi * ramp(.6, 8, 30, 1.2).cumsum() / SR))
    snap = hp(noise(.02), 2500) * env(.02, 0, .004)
    return room(mix(d, (0, .55, growl * pulse), (.58, .3, snap)), .3, .4, .14)


def roll(v):
    d = .42
    k = detune(v, .05)
    sweep = svf(noise(d, 'pink'), np.concatenate([ramp(.16, 500 * k, 3600 * k), ramp(d - .16, 3600 * k, 700 * k)])[:n_of(d)],
                q=2.8, mode='bp')
    shape = np.sin(np.pi * np.clip(time(d) / d, 0, 1)) ** 1.5
    return mix(d, (0, .9, sweep * shape))


def ready(v):
    d = .38
    k = detune(v, .015)
    a = fm(np.full(n_of(.1), 1319 * k), 2.0, 1.5 * env(.1, 0, .03)) * env(.1, .001, .05)
    b = fm(np.full(n_of(.3), 1976 * k), 2.0, 1.5 * env(.3, 0, .04)) * env(.3, .001, .09)
    return room(mix(d, (0, .45, a), (.06, .5, b)), .3, .4, .15)


def shatter(v):
    d = .6
    crunch = drive(hp(noise(d), 1500) * env(d, .001, .03), 2)
    shards = np.zeros(n_of(d))
    r = np.random.default_rng(110 + v)
    for i in range(14):
        f = r.uniform(2200, 7000)
        at = r.exponential(.06)
        g = fm(np.full(n_of(.2), f), r.uniform(1.3, 3.7), 2 * env(.2, 0, .02)) * env(.2, .0003, r.uniform(.015, .05))
        place(shards, g * r.uniform(.3, 1), at)
    thud = osc(glide(d, 260, 90, 30), 'sine') * env(d, .001, .03)
    return room(mix(d, (0, .45, crunch), (0, .35, shards), (0, .45, thud)), .4, .45, .2, damp=8000)


def capacitor(v):
    d = .42
    k = detune(v, .03)
    buzz = svf(osc(np.full(n_of(.22), 110 * k), 'square', .2), ramp(.22, 300, 5000, 1.4), q=3) * env(.22, .02, .1, hold=.15)
    zap = osc(glide(.2, 3200 * k, 180, 22), 'square', .4) * env(.2, .0008, .05)
    arc = crackle(.2, 900, .05, 2500, 9000, seed=130 + v)
    return room(mix(d, (0, .35, buzz), (.2, .4, zap), (.2, .35, arc)), .3, .4, .12)


def fan(v):
    d = .7
    k = detune(v, .05)
    blade = .6 + .4 * np.sin(2 * np.pi * ramp(d, 9 * k, 24 * k).cumsum() / SR) ** 2
    wind = svf(noise(d, 'pink'), ramp(d, 900 * k, 2600 * k), q=1.1, mode='bp') * blade
    return mix(d, (0, .9, wind * env(d, .1, .25, hold=.3)))


def frost(v):
    d = .45
    k = detune(v, .03)
    crackles = crackle(d, 500, .1, 3500, 10000, seed=150 + v)
    tinkle = np.zeros(n_of(d))
    for i, f in enumerate([3520, 2960, 2489, 2093]):
        place(tinkle, fm(np.full(n_of(.2), f * k), 2.76, 1.2 * env(.2, 0, .02)) * env(.2, .0005, .05), i * .035)
    hiss = hp(noise(d), 6000) * env(d, .005, .07)
    return room(mix(d, (0, .4, crackles), (0, .35, tinkle), (0, .25, hiss)), .4, .45, .2, damp=8000)


def decoy(v):
    d = .65
    k = detune(v, .03)
    f = ramp(.55, 520 * k, 690 * k)
    vib = 1 + .015 * np.sin(2 * np.pi * 6.5 * time(.55))
    ghost = sum(osc(f * vib * m, 'sine') for m in (1, 1.007, .993, 2.002)) * env(.55, .12, .09, hold=.15)
    air = svf(noise(.55), 1400 * k, q=4, mode='bp') * env(.55, .1, .09, hold=.15)
    return room(mix(d, (0, .3, ghost), (0, .3, air)), .7, .55, .35)


def popcorn(v):
    d = .16
    k = detune(v, .08)
    pop = svf(noise(.03), 1500 * k, q=3, mode='bp') * env(.03, 0, .006)
    plop = osc(glide(d, 900 * k, 260 * k, 60), 'sine') * env(d, .0006, .02)
    return room(mix(d, (0, .7, pop), (0, .6, plop)), .1, .3, .07)


def static(v):
    d = .3
    k = detune(v, .05)
    hum = svf(osc(np.full(n_of(d), 120 * k), 'saw'), 1800, q=2) * env(d, .004, .1)
    arcs = crackle(d, 1200, .1, 2000, 9000, seed=170 + v)
    zip_ = osc(glide(.08, 4000 * k, 900, 40), 'square', .5) * env(.08, .0005, .02)
    return mix(d, (0, .3, hum), (0, .7, arcs), (0, .2, zip_))


# cue -> (design, original clip used for loudness match or None, A/B label, group)
CUES = {
    'shot1': (shot1, 'Sounds/shot1', 'Base gun', 'Shots'),
    'enemyHit': (enemyHit, ('Sounds/shot1', .55), 'Hit on enemy', 'Hits'),
    'bossHit': (bossHit, ('Sounds/shot1', .65), 'Hit on boss', 'Hits'),
    'hurt1': (hurt1, 'Sounds/hurt1', 'Player hurt', 'Hits'),
    'enemyExplosion1': (enemyExplosion1, 'Sounds/enemyExplosion1', 'Enemy explosion', 'Explosions'),
    'explosionLarge': (explosionLarge, ('Sounds/enemyExplosion1', 1.25), 'Boss phase change', 'Explosions'),
    'bossDeath': (bossDeath, ('Sounds/enemyExplosion1', 1.5), 'Boss death', 'Bosses'),
    'bossWarning': (bossWarning, 'Sounds/bosswarning', 'Boss warning', 'Bosses'),
    'playerDead': (playerDead, 'Sounds/playerDead', 'Player death', 'Explosions'),
    'blankbullet': (blankbullet, 'Sounds/blankbullet', 'Screen clear / shield', 'Items'),
    'coin': (coin, ('Sounds/shot1', .8), 'Coin', 'Pickups'),
    'mana': (mana, ('Sounds/shot1', .8), 'Energy', 'Pickups'),
    'itemGet': (itemGet, ('Sounds/shot1', 1.3), 'Item chosen', 'Pickups'),
    'uiConfirm': (uiConfirm, ('Sounds/shot1', 1.0), 'Purchase / confirm', 'UI'),
    'uiSkip': (uiSkip, ('Sounds/shot1', .8), 'Skip', 'UI'),
    'heal': (heal, 'Sounds/Items/heal', 'Heal', 'Items'),
    'timeStop': (timeStop, 'Sounds/Items/timeStop', 'Time stop', 'Items'),
    'powerdown': (powerdown, 'Sounds/Items/powerdown', 'Power down', 'Items'),
    'overdrive': (overdrive, 'Sounds/Items/overdrive', 'Overdrive', 'Items'),
    'roll': (roll, 'Sounds/Items/roll', 'Roll / dash', 'Items'),
    'ready': (ready, 'Sounds/Items/ready', 'Ready', 'Items'),
    'shatter': (shatter, 'Sounds/Items/shatter', 'Shatter', 'Items'),
    'capacitor': (capacitor, 'Sounds/Items/capacitor', 'Capacitor', 'Items'),
    'fan': (fan, 'Sounds/Items/fan', 'Fan', 'Items'),
    'frost': (frost, 'Sounds/Items/frost', 'Frost', 'Items'),
    'decoy': (decoy, 'Sounds/Items/decoy', 'Decoy', 'Items'),
    'popcorn': (popcorn, 'Sounds/Items/popcorn', 'Popcorn', 'Items'),
    'static': (static, 'Sounds/Items/static', 'Static', 'Items'),
}
VARIANTS = 4
# How often the A/B page repeats a cue in its rapid-fire preview (seconds), matching game use.
REPEAT = {'shot1': .075, 'enemyHit': .06, 'bossHit': .08, 'enemyExplosion1': .22, 'coin': .09, 'mana': .15,
          'popcorn': .12}


# ---------------------------------------------------------------- io and loudness

def read_wav(path):
    with wave.open(str(path), 'rb') as f:
        ch, sw, rate, n = f.getnchannels(), f.getsampwidth(), f.getframerate(), f.getnframes()
        data = np.frombuffer(f.readframes(n), dtype=np.int16).astype(float) / 32768
    if ch > 1:
        data = data.reshape(-1, ch).mean(1)
    if rate != SR:
        data = np.interp(np.arange(int(len(data) * SR / rate)) * rate / SR, np.arange(len(data)), data)
    return data


def write_wav(path, x):
    data = (np.clip(x, -1, 1) * 32767).astype('<i2')
    with wave.open(str(path), 'wb') as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(data.tobytes())


def loudness(x):
    """Peak short-term (30 ms) RMS after a presence-weighted high-pass: close to perceived punch."""
    w = hp(x, 150)
    win = n_of(.03)
    if len(w) < win:
        return np.sqrt(np.mean(w * w) + 1e-12)
    power = np.convolve(w * w, np.ones(win) / win, 'valid')
    return np.sqrt(power.max() + 1e-12)


def meta(path, folder=False):
    target = Path(str(path) + '.meta')
    if target.exists():
        return
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'shootemup/synth-audio/' + str(path.relative_to(ROOT)).replace('\\', '/')).hex
    if folder:
        body = 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    else:
        body = ('AudioImporter:\n  externalObjects: {}\n  serializedVersion: 7\n  defaultSettings:\n'
                '    serializedVersion: 2\n    loadType: 0\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n'
                '    compressionFormat: 0\n    quality: 1\n    conversionMode: 0\n    preloadAudioData: 1\n  platformSettingOverrides: {}\n'
                '  forceToMono: 1\n  normalize: 0\n  loadInBackground: 0\n  ambisonic: 0\n'
                '  3D: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    target.write_text(f'fileFormatVersion: 2\nguid: {guid}\n' + body, encoding='utf-8')


def render(only=None):
    global rng
    OUT.mkdir(parents=True, exist_ok=True)
    meta(OUT, True)
    report = {}
    for name, (design, original, label, group) in CUES.items():
        if only and name not in only:
            continue
        # A plain path replaces that clip at its loudness; (path, level) sets a new cue relative to it.
        source, level = (original, 1.0) if isinstance(original, str) or original is None else original
        reference = ROOT / 'Assets/Resources' / (source + '.wav') if source else None
        target = loudness(read_wav(reference)) * level if reference else None
        original = original if isinstance(original, str) else None
        takes = []
        for v in range(VARIANTS):
            rng = np.random.default_rng(int.from_bytes(hashlib.sha256(f'{name}:{v}:synth-v1'.encode()).digest()[:8], 'little'))
            x = finish(design(v))
            if target is not None:
                x *= min(target / loudness(x), 1 / np.max(np.abs(x)) * .98)
            path = OUT / f'{name}_{v + 1:02}.wav'
            write_wav(path, x)
            meta(path)
            takes.append(dict(file=path.name, seconds=round(len(x) / SR, 3), peak=round(float(np.max(np.abs(x))), 3),
                              loudness=round(float(loudness(x)), 4)))
        report[name] = dict(label=label, group=group, original=original, takes=takes,
                            original_loudness=round(float(target), 4) if target is not None else None)
        print(f'{name:16s} {takes[0]["seconds"]:5.2f}s peak {takes[0]["peak"]:.2f}')
    return report


def review(report):
    """Standalone A/B page: current vs synth, single and rapid-fire, level matched."""
    REVIEW.mkdir(parents=True, exist_ok=True)
    audio = REVIEW / 'audio'
    audio.mkdir(exist_ok=True)
    rows = []
    for name, r in report.items():
        for take in r['takes']:
            shutil.copyfile(OUT / take['file'], audio / take['file'])
        current = None
        if r['original']:
            current = f'current_{name}.wav'
            shutil.copyfile(ROOT / 'Assets/Resources' / (r['original'] + '.wav'), audio / current)
        rows.append(dict(id=name, label=r['label'], group=r['group'], current=current,
                         takes=[t['file'] for t in r['takes']], repeat=REPEAT.get(name)))
    (REVIEW / 'cues.json').write_text(json.dumps(rows, indent=1), encoding='utf-8')
    page = (Path(__file__).with_name('synth_sfx_review.html')).read_text(encoding='utf-8')
    (REVIEW / 'index.html').write_text(page.replace('__CUES__', json.dumps(rows)), encoding='utf-8')
    (REVIEW / 'report.json').write_text(json.dumps(report, indent=1), encoding='utf-8')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--only', default='')
    args = parser.parse_args()
    only = set(filter(None, args.only.split(',')))
    report = render(only)
    if not only:
        review(report)
