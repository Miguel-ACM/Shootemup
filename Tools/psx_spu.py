"""A small model of the PlayStation 1 sound chain, for making procedural sounds read as PSX.

PS1 games stored sound effects in SPU RAM (512 KB) as 4-bit ADPCM, usually at low sample
rates, played them back with 4-point Gaussian interpolation and ran them through the SPU's
22050 Hz reverb. Each step adds part of the console's character:

  adpcm()   PS1/XA 4-bit ADPCM round trip: 28-sample blocks, five prediction filters
  resample  storage at 11025/22050 Hz (the usual SPU RAM budget) and pitched playback
  reverb()  the SPU reverb formula with Sony's SDK presets (Room, Studio Small/Medium)

Reference: psx-spx, "Sound Processing Unit (SPU)" (https://psx-spx.consoledev.net/soundprocessingunitspu/).
"""
import numpy as np
from scipy.signal import resample_poly

SR = 44100
FILTERS = [(0, 0), (60, 0), (115, -52), (98, -55), (122, -60)]

# 32 reverb registers in hardware order (psx-spx, SPU reverb examples).
PRESETS = {
    'room': (0x26C0, [
        0x007D, 0x005B, 0x6D80, 0x54B8, 0xBED0, 0x0000, 0x0000, 0xBA80,
        0x5800, 0x5300, 0x04D6, 0x0333, 0x03F0, 0x0227, 0x0374, 0x01EF,
        0x0334, 0x01B5, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x01B4, 0x0136, 0x00B8, 0x005C, 0x8000, 0x8000]),
    'studioSmall': (0x1F40, [
        0x0033, 0x0025, 0x70F0, 0x4FA8, 0xBCE0, 0x4410, 0xC0F0, 0x9C00,
        0x5280, 0x4EC0, 0x03E4, 0x031B, 0x03A4, 0x02AF, 0x0372, 0x0266,
        0x031C, 0x025D, 0x025C, 0x018E, 0x022F, 0x0135, 0x01D2, 0x00B7,
        0x018F, 0x00B5, 0x00B4, 0x0080, 0x004C, 0x0026, 0x8000, 0x8000]),
    'studioMedium': (0x4840, [
        0x00B1, 0x007F, 0x70F0, 0x4FA8, 0xBCE0, 0x4510, 0xBEF0, 0xB4C0,
        0x5280, 0x4EC0, 0x0904, 0x076B, 0x0824, 0x065F, 0x07A2, 0x0616,
        0x076C, 0x05ED, 0x05EC, 0x042E, 0x050F, 0x0305, 0x0462, 0x02B7,
        0x042F, 0x0265, 0x0264, 0x01B2, 0x0100, 0x0080, 0x8000, 0x8000]),
}
NAMES = ['dAPF1', 'dAPF2', 'vIIR', 'vCOMB1', 'vCOMB2', 'vCOMB3', 'vCOMB4', 'vWALL', 'vAPF1', 'vAPF2',
         'mLSAME', 'mRSAME', 'mLCOMB1', 'mRCOMB1', 'mLCOMB2', 'mRCOMB2', 'dLSAME', 'dRSAME',
         'mLDIFF', 'mRDIFF', 'mLCOMB3', 'mRCOMB3', 'mLCOMB4', 'mRCOMB4', 'dLDIFF', 'dRDIFF',
         'mLAPF1', 'mRAPF1', 'mLAPF2', 'mRAPF2', 'vLIN', 'vRIN']


def adpcm(x):
    """Encode to PS1 4-bit ADPCM and decode again, keeping the codec's quantization character."""
    pcm = np.clip(np.round(x * 32767), -32768, 32767)
    n = int(np.ceil(len(pcm) / 28)) * 28
    pcm = np.pad(pcm, (0, n - len(pcm)))
    out = np.zeros(n)
    s1 = s2 = 0.0
    for b in range(0, n, 28):
        block = pcm[b:b + 28]
        # Choose the filter from the open-loop residual, then the smallest shift that fits it.
        best = None
        for f, (c0, c1) in enumerate(FILTERS):
            prev1 = np.concatenate([[s1], block[:-1]])
            prev2 = np.concatenate([[s2, s1], block[:-2]])
            peak = np.max(np.abs(block - (prev1 * c0 + prev2 * c1) / 64))
            if best is None or peak < best[0]:
                best = (peak, f)
        peak, f = best
        shift = 12
        while shift > 0 and peak > (7 << (12 - shift)):
            shift -= 1
        c0, c1 = FILTERS[f]
        for i in range(28):
            pred = (s1 * c0 + s2 * c1) / 64
            nib = int(np.clip(round((block[i] - pred) / (1 << (12 - shift))), -8, 7))
            value = np.clip((nib << 12 >> shift) + pred, -32768, 32767)
            out[b + i] = value
            s2, s1 = s1, value
    return out[:len(x)] / 32768


def store_and_play(x, storage_rate=22050, pitch=1.0):
    """Downsample to the SPU RAM rate, round-trip ADPCM, play back at a pitch to 44.1 kHz."""
    stored = resample_poly(x, storage_rate, SR)
    stored = adpcm(stored)
    # Playback rate = storage_rate * pitch. Interpolating up (scipy's polyphase filter stands in
    # for the SPU's 4-point Gaussian, which is a similarly soft low-pass).
    play = int(round(storage_rate * pitch))
    from math import gcd
    g = gcd(SR, play)
    return resample_poly(stored, SR // g, play // g, window=('kaiser', 4.0))


def reverb(x, preset='room', wet=.3):
    """Mono SPU reverb at 22050 Hz with Sony's presets; returns dry + wet at 44.1 kHz."""
    size, regs = PRESETS[preset]
    r = dict(zip(NAMES, regs))
    signed = {k: (v - 0x10000 if v >= 0x8000 else v) / 0x8000 for k, v in r.items() if k[0] == 'v'}
    addr = {k: v * 4 for k, v in r.items() if k[0] in 'md'}  # 8-byte units -> halfwords
    tail = .25 + size / 0x4000
    x22 = resample_poly(np.pad(x, (0, int(tail * SR))), 1, 2)
    N = size // 2
    buf = np.zeros(N)
    out = np.empty(len(x22))
    vIIR, vWALL, vAPF1, vAPF2 = signed['vIIR'], signed['vWALL'], signed['vAPF1'], signed['vAPF2']
    combs = [(signed[f'vCOMB{i}'], addr[f'mLCOMB{i}']) for i in range(1, 5)]
    a = addr
    vin = abs(signed['vLIN'])
    cur = 0
    clip = lambda v: -1.0 if v < -1 else 1.0 if v > 1 else v
    for i, s in enumerate(x22.tolist()):
        lin = vin * s
        p = (cur + a['mLSAME'] - 1) % N
        buf[(cur + a['mLSAME']) % N] = clip((lin + buf[(cur + a['dLSAME']) % N] * vWALL - buf[p]) * vIIR + buf[p])
        p = (cur + a['mRSAME'] - 1) % N
        buf[(cur + a['mRSAME']) % N] = clip((lin + buf[(cur + a['dRSAME']) % N] * vWALL - buf[p]) * vIIR + buf[p])
        p = (cur + a['mLDIFF'] - 1) % N
        buf[(cur + a['mLDIFF']) % N] = clip((lin + buf[(cur + a['dRDIFF']) % N] * vWALL - buf[p]) * vIIR + buf[p])
        p = (cur + a['mRDIFF'] - 1) % N
        buf[(cur + a['mRDIFF']) % N] = clip((lin + buf[(cur + a['dLDIFF']) % N] * vWALL - buf[p]) * vIIR + buf[p])
        o = 0.0
        for g, m in combs:
            o += g * buf[(cur + m) % N]
        for v, m, d in ((vAPF1, a['mLAPF1'], a['dAPF1']), (vAPF2, a['mLAPF2'], a['dAPF2'])):
            back = buf[(cur + m - d) % N]
            o = o - v * back
            buf[(cur + m) % N] = clip(o)
            o = o * v + back
        out[i] = o
        cur = (cur + 1) % N
    wet_sig = resample_poly(out, 2, 1)[:len(x) + int(tail * SR)]
    dry = np.pad(x, (0, len(wet_sig) - len(x)))
    peak = max(np.max(np.abs(wet_sig)), 1e-9)
    return dry + wet_sig / peak * np.max(np.abs(x)) * wet
