"""Original, deterministic PSX-inspired one-shots and repeat/mix review reels.

Run: python Tools/build_psx_sfx.py [--verify-only]
Python standard library only. No downloads or external sample dependencies.
Uses Frontlines BuildSfx's layered render / faded boundaries / decoded verification
approach, with original synthesis instead of that project's source recordings.
"""
import array
import hashlib
import html
import json
import math
from pathlib import Path
import random
import sys
import uuid
import wave

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/Sounds/PSX'
REVIEW = ROOT / 'ArtSource/AudioReview'
RATE = 22050
VARIANTS = 4
# id, family, seconds, start Hz, end Hz, noise, cutoff Hz, gain, cooldown, bus
# Short, rounded attacks for repeated guns; weight lives below the presence band.
SPECS = [
    ('shot1', 'gun', .105, 760, 190, .35, 4200, .10, .035, 0),
    ('shotElectric', 'arc', .145, 510, 160, .40, 3800, .09, .050, 0),
    ('shotAK45', 'gun', .095, 310, 100, .75, 3900, .09, .045, 0),
    ('shotCompanion', 'gun', .080, 920, 290, .28, 3600, .065, .065, 0),
    ('shotNeedle', 'gun', .090, 1250, 380, .45, 4400, .09, .040, 0),
    ('shotRail', 'metal', .230, 390, 85, .62, 3800, .15, .120, 0),
    ('shotScatter', 'gun', .150, 240, 70, .82, 3800, .12, .075, 0),
    ('shotShell', 'gun', .210, 165, 48, .80, 3300, .13, .100, 0),
    ('shotDisc', 'disc', .190, 620, 190, .24, 3400, .10, .080, 0),
    ('shotMissile', 'rocket', .260, 205, 75, .86, 3000, .11, .080, 0),
    ('shotFlame', 'flame', .115, 140, 65, .95, 2300, .075, .050, 0),
    ('shotBeam', 'beam', .620, 410, 130, .45, 3400, .12, .150, 0),
    ('enemySimple', 'gun', .105, 400, 140, .42, 3400, .045, .080, 1),
    ('enemyNeedle', 'gun', .075, 1050, 340, .55, 4100, .04, .075, 1),
    ('enemyPlasma', 'plasma', .240, 330, 85, .23, 2900, .055, .110, 1),
    ('enemyMissile', 'rocket', .300, 170, 65, .86, 2700, .06, .130, 1),
    ('enemyBurst', 'gun', .180, 195, 62, .78, 3200, .055, .110, 1),
    ('enemyDisc', 'disc', .200, 460, 190, .22, 3200, .045, .100, 1),
    ('enemyRicochet', 'metal', .125, 720, 260, .40, 3800, .045, .090, 1),
    ('enemyHelix', 'plasma', .165, 580, 210, .22, 3300, .045, .100, 1),
    ('enemySuspended', 'plasma', .220, 220, 100, .34, 2800, .05, .110, 1),
    ('enemyScatter', 'gun', .160, 245, 72, .70, 3400, .05, .110, 1),
    ('enemyMine', 'metal', .180, 290, 140, .43, 2900, .05, .130, 1),
    ('enemyWave', 'disc', .320, 235, 85, .32, 2700, .055, .160, 1),
    ('beamCharge', 'charge', .360, 120, 480, .18, 2800, .05, .150, 1),
    ('beamFire', 'beam', .380, 310, 105, .56, 3300, .065, .120, 1),
    ('hit1', 'hit', .070, 470, 180, .82, 3400, .05, .040, 2),
    ('hitElectric', 'arc', .095, 670, 220, .64, 3300, .045, .055, 2),
    ('ricochetHit', 'metal', .090, 970, 390, .42, 3800, .045, .070, 2),
    ('enemyExplosion1', 'blast', .520, 115, 38, .90, 3700, .13, .055, 3),
    ('explosionHeavy', 'blast', .880, 85, 30, .95, 3000, .17, .110, 3),
    ('shellBurst', 'blast', .270, 150, 45, .86, 3400, .08, .090, 3),
    ('hurt1', 'damage', .260, 220, 75, .55, 3000, .20, .160, 4),
    ('playerDead', 'blast', 1.180, 100, 28, .94, 3300, .26, .500, 4),
    ('blankbullet', 'disc', .650, 540, 55, .60, 3300, .18, .250, 4),
    ('bossWarning', 'warning', .780, 270, 210, .08, 2500, .16, .500, 4),
]


def synth(spec, variant):
    name, kind, duration, start, end, noise_amount, cutoff, *_ = spec
    rng = random.Random(int.from_bytes(hashlib.sha256(f'{name}:{variant}:psx-v1'.encode()).digest()[:8], 'little'))
    tune = 1 + (variant - 1.5) * .018
    count = round(duration * RATE)
    phase = low = slow = filt1 = filt2 = 0.0
    a = 1 - math.exp(-2 * math.pi * cutoff / RATE)
    signal = []
    for i in range(count):
        t = i / RATE
        u = t / duration
        freq = (end + (start - end) * math.exp(-u * (6 if kind == 'gun' else 3))) * tune
        if kind == 'charge':
            freq = (start + (end - start) * u * u) * tune
        if kind == 'disc':
            freq *= 1 + .09 * math.sin(t * 2 * math.pi * 29)
        phase += 2 * math.pi * freq / RATE
        white = rng.uniform(-1, 1)
        low += .18 * (white - low)
        slow += .022 * (white - slow)
        grit = .55 * (white - low) + .65 * low
        tone = math.sin(phase + (.65 if kind in ('arc', 'plasma', 'beam') else .13) * math.sin(phase * 2.013))
        env = math.exp(-u * (7 if kind in ('gun', 'hit', 'metal') else 4.8))
        if kind == 'blast':
            # Low pressure body, mid crack and a darker, decorrelated debris tail.
            x = .48 * math.sin(phase) * math.exp(-u * 8) + 2.3 * low * env
            x += .55 * grit * math.exp(-u * 19) + .7 * slow * math.exp(-u * 3.6)
            x *= 1 + .14 * math.sin(t * 93) * math.sin(t * 171)
        elif kind == 'metal':
            x = (.38 * tone + .22 * math.sin(phase * 1.483) + noise_amount * grit) * env
        elif kind == 'rocket':
            x = (.32 * tone + 1.7 * noise_amount * low + .24 * grit) * env
        elif kind == 'flame':
            x = (.12 * tone + 2.4 * low + .24 * grit) * math.exp(-u * 3.2)
        elif kind == 'warning':
            gate = max(0, math.sin(t * 2 * math.pi * 3)) ** 2
            x = (.7 * math.sin(phase) + .2 * math.sin(phase * 1.5)) * gate * math.exp(-u * 1.8)
        elif kind == 'damage':
            x = (.55 * tone + .25 * math.sin(phase * .73) + noise_amount * grit) * env
        else:
            x = ((1 - noise_amount * .55) * tone + noise_amount * grit) * env
            if kind in ('arc', 'beam'):
                x *= .72 + .28 * math.sin(t * 2 * math.pi * 61) ** 2
        # Two low-pass stages tame abrasive aliasing without removing the crack.
        filt1 += a * (x - filt1)
        filt2 += a * (filt1 - filt2)
        signal.append(math.tanh(filt2 * 1.65))
    # A small mono room, baked once, never a long metallic reverb on every shot.
    dry = signal[:]
    for delay, gain in ((.019, .055), (.037, .028)):
        offset = round(delay * RATE)
        for i in range(offset, count):
            signal[i] += dry[i - offset] * gain
    # DC removal followed by cosine fades: the PCM boundaries are exactly zero.
    mean = sum(signal) / len(signal)
    attack = .0018 if kind != 'charge' else .012
    release = min(.035, duration * .22)
    for i in range(count):
        fade_in = .5 - .5 * math.cos(math.pi * min(1, i / (attack * RATE)))
        fade_out = .5 - .5 * math.cos(math.pi * min(1, (count - 1 - i) / (release * RATE)))
        signal[i] = (signal[i] - mean) * fade_in * fade_out
    peak = max(map(abs, signal))
    # 16-bit PCM at half SPU clock: sampled grit, without a fatiguing 8-bit buzz.
    return [round(x / peak * .62 * 32767) / 32768 for x in signal]


def write_wav(path, samples):
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = array.array('h', (round(max(-1, min(1, x)) * 32767) for x in samples))
    if sys.byteorder != 'little':
        pcm.byteswap()
    with wave.open(str(path), 'wb') as f:
        f.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        f.writeframes(pcm.tobytes())


def read_wav(path):
    with wave.open(str(path), 'rb') as f:
        assert (f.getnchannels(), f.getsampwidth(), f.getframerate()) == (1, 2, RATE), path
        data = array.array('h', f.readframes(f.getnframes()))
    if sys.byteorder != 'little':
        data.byteswap()
    return [x / 32768 for x in data]


def stats(samples):
    peak = max(map(abs, samples))
    rms = math.sqrt(sum(x*x for x in samples) / len(samples))
    return dict(seconds=round(len(samples)/RATE, 4), peak_db=round(20*math.log10(max(peak, 1e-10)), 2),
                rms_db=round(20*math.log10(max(rms, 1e-10)), 2), dc=round(sum(samples)/len(samples), 7))


def meta(path, folder=False):
    target = Path(str(path) + '.meta')
    if target.exists() and path.suffix != '.wav':
        return
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'shootemup/psx-audio/' + str(path.relative_to(ROOT)).replace('\\', '/')).hex
    if target.exists():
        guid = next(line.split(':', 1)[1].strip() for line in target.read_text(encoding='utf-8').splitlines() if line.startswith('guid:'))
    if folder:
        body = 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif path.suffix == '.wav':
        body = ('AudioImporter:\n  externalObjects: {}\n  serializedVersion: 7\n  defaultSettings:\n'
                '    serializedVersion: 2\n    loadType: 0\n    sampleRateSetting: 0\n    sampleRateOverride: 22050\n'
                '    compressionFormat: 0\n    quality: 1\n    conversionMode: 0\n    preloadAudioData: 1\n  platformSettingOverrides: {}\n'
                '  forceToMono: 1\n  normalize: 0\n  loadInBackground: 0\n  ambisonic: 0\n'
                '  3D: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    else:
        body = 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    target.write_text(f'fileFormatVersion: 2\nguid: {guid}\n' + body, encoding='utf-8')


def render():
    OUT.mkdir(parents=True, exist_ok=True)
    meta(OUT, True)
    for spec in SPECS:
        for v in range(VARIANTS):
            path = OUT / f'{spec[0]}_{v+1:02}.wav'
            write_wav(path, synth(spec, v))
            meta(path)
    catalog = ROOT / 'Assets/Scripts/Sound/SfxCatalog.cs'
    entries = '\n'.join(f'        new Cue("{s[0]}", {s[7]}f, {s[8]}f, {s[9]}),' for s in SPECS)
    catalog.write_text('''// Generated by Tools/build_psx_sfx.py. Edit SPECS there and rebuild.
public static class SfxCatalog
{
    public const int Variants = 4;
    public sealed class Cue
    {
        public readonly string Name;
        public readonly float Gain, Cooldown;
        public readonly int Bus;
        public Cue(string name, float gain, float cooldown, int bus)
        { Name=name; Gain=gain; Cooldown=cooldown; Bus=bus; }
    }
    public static readonly Cue[] All = {
''' + entries + '\n    };\n}\n', encoding='utf-8')
    meta(catalog)


def verify():
    REVIEW.mkdir(parents=True, exist_ok=True)
    banks = {}
    report = {'sample_rate': RATE, 'variants': VARIANTS, 'effects': [], 'repetition': [], 'mixes': []}
    for spec in SPECS:
        name = spec[0]
        clips = [read_wav(OUT / f'{name}_{v+1:02}.wav') for v in range(VARIANTS)]
        assert len({tuple(c) for c in clips}) == VARIANTS, f'Duplicate variants: {name}'
        for v, clip in enumerate(clips):
            m = stats(clip)
            assert clip[0] == clip[-1] == 0 and max(map(abs, clip)) < .63, (name, m)
            assert abs(m['dc']) < .012 and m['rms_db'] > -38, (name, m)
            assert abs(len(clip)/RATE-spec[2]) < 1/RATE, name
            # Compare rerendered source with actual PCM, catching stale assets.
            expected = synth(spec, v)
            assert max(abs(x-y) for x,y in zip(expected, clip)) < .00007, name
            report['effects'].append(dict(name=name, variant=v+1, **m,
                sha256=hashlib.sha256((OUT / f'{name}_{v+1:02}.wav').read_bytes()).hexdigest()))
        banks[name] = clips
    # Per-cue 8-second repeated reels: rapid guns/hits and dense explosions.
    for spec in SPECS:
        name, *_, gain, cooldown, bus = spec
        interval = max(cooldown, .075) if bus == 0 else .060 if bus == 2 else .15 if bus == 1 else .20 if bus == 3 else .95
        reel = [0.0] * (RATE * 9)
        for n in range(int(8/interval)):
            add(reel, banks[name][n % VARIANTS], n*interval, gain)
        m = stats(reel)
        assert m['peak_db'] < -7, (name, m)
        write_wav(REVIEW / f'repeat_{name}.wav', reel)
        report['repetition'].append(dict(name=name, interval=interval, **m))
    # Representative authored-volume combat, plus stress with simultaneous volleys.
    for stress in (False, True):
        reel = [0.0] * (RATE * 18)
        events = []
        for n in range(150):
            events.append((n*.1, 'shot1', .10))
            if n % 2 == 0:
                events.append((n*.1+.045, 'hit1', .05))
            if stress and n % 3 == 0:
                events.extend((n*.1, name, gain) for name, gain in [('shotElectric', .09), ('shotAK45', .09), ('enemyNeedle', .04)])
        enemy_names = [s[0] for s in SPECS if s[9] == 1]
        for n in range(60 if stress else 30):
            name = enemy_names[n % len(enemy_names)]
            spec = next(s for s in SPECS if s[0] == name)
            events.append((n*(.24 if stress else .48), name, spec[7]))
        for n in range(40 if stress else 16):
            events.append((n*(.35 if stress else .9)+.22, 'explosionHeavy' if n%4==0 else 'enemyExplosion1', .17 if n%4==0 else .13))
        events.extend([(3.2, 'hurt1', .20), (8.5, 'blankbullet', .18), (14.5, 'playerDead', .26)])
        for n, (t, name, gain) in enumerate(sorted(events)):
            add(reel, banks[name][n % VARIANTS], t, gain)
        m = stats(reel)
        assert m['peak_db'] < -3, m
        name = 'stress_combat' if stress else 'combat'
        write_wav(REVIEW / f'{name}.wav', reel)
        report['mixes'].append(dict(name=name, events=len(events), **m))
    # A full minute with every player weapon, repeated hits and continuous enemy fire.
    # No master limiter: the file retains the raw authored gains for honest review.
    endurance = [0.0] * (RATE * 62)
    player_specs = [s for s in SPECS if s[9] == 0]
    for n in range(720):
        spec = player_specs[(n // 60) % len(player_specs)]
        # Slow weapons keep their authored cue gate rather than restarting every frame.
        if n == 0 or n % max(1, math.ceil(spec[8]*12)) == 0:
            add(endurance, banks[spec[0]][n%VARIANTS], n/12, spec[7])
        if n % 2 == 0:
            add(endurance, banks['hit1'][n//2%VARIANTS], n/12+.025, .05)
        if n % 3 == 0:
            spec = next(s for s in SPECS if s[0] == enemy_names[n//3%len(enemy_names)])
            add(endurance, banks[spec[0]][n//3%VARIANTS], n/12, spec[7])
        if n % 5 == 0:
            name = 'explosionHeavy' if n%20==0 else 'enemyExplosion1'
            add(endurance, banks[name][n//5%VARIANTS], n/12+.06, .17 if n%20==0 else .13)
    m = stats(endurance)
    assert m['peak_db'] < -3, m
    write_wav(REVIEW / 'endurance.wav', endurance)
    report['mixes'].append(dict(name='endurance', **m))
    (REVIEW / 'verification.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    rows = ''.join(f'<article><b>{html.escape(s[0])}</b><small>{s[1]} · {s[2]:.3f}s · repeat every {next(r["interval"] for r in report["repetition"] if r["name"] == s[0]):.3f}s</small><label>Single <audio controls preload="none" src="../../Assets/Resources/Sounds/PSX/{s[0]}_01.wav"></audio></label><label>Repeated · game gain <audio controls preload="none" src="repeat_{s[0]}.wav"></audio></label></article>' for s in SPECS)
    (REVIEW / 'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><title>PSX combat sound review</title>
<style>body{background:#10151c;color:#e4eaf0;font:16px system-ui;max-width:1100px;margin:36px auto;padding:0 24px}h1{color:#f8b766}p,small{color:#a8b7c5}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(310px,1fr));gap:16px}article{padding:18px;background:#1c2530;border-radius:8px}b,small,label{display:block}small{margin:6px 0 16px}audio{display:block;width:100%;height:36px;margin:8px 0 16px}button{padding:10px;background:#f8b766;border:0;border-radius:4px}</style>
<h1>PSX combat / listening bench</h1><p>Original effects, four deterministic variations each. Rounded attacks, dark tails, short mono room. Single previews are at asset gain; repetition and battle reels use game gains. No loudness normalization or limiting is applied to the battle reels.</p>
<button onclick="document.querySelectorAll('audio').forEach(a=>{a.pause();a.currentTime=0})">Stop all</button>
<label><input type="checkbox" onchange="document.querySelectorAll('audio').forEach(a=>a.loop=this.checked)"> Loop previews</label>
<h2>18-second combat mixes</h2><label>Representative battle<audio controls src="combat.wav"></audio></label><label>Dense battle: concurrent player weapons, rapid hits and explosions<audio controls src="stress_combat.wav"></audio></label>
<label>One-minute endurance · every player weapon with hits, enemy fire and blasts<audio controls src="endurance.wav"></audio></label>
<main>''' + rows + '</main><script>document.querySelectorAll("audio").forEach(a=>a.addEventListener("play",()=>document.querySelectorAll("audio").forEach(b=>{if(b!==a)b.pause()})))</script></html>', encoding='utf-8')
    print(json.dumps({'assets':len(report['effects']), 'repeat_reels':len(report['repetition']), 'mixes':report['mixes'], 'review':str(REVIEW/'index.html')}, indent=2))


def add(target, clip, seconds, gain):
    offset = round(seconds * RATE)
    for i in range(min(len(clip), len(target)-offset)):
        target[offset+i] += clip[i]*gain


if __name__ == '__main__':
    if '--verify-only' not in sys.argv:
        render()
    verify()
