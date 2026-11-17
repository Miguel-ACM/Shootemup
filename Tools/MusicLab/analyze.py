"""Build cue sheets from the shipped MP3s. Requires NumPy, FFmpeg, Node 18+ and the Music Lab npm dependencies.

Run from any directory: python Tools/MusicLab/analyze.py [track_id ...]
All times refer to the gapless decoded recording, in seconds. No audio is rewritten.
"""
import argparse
import json
import subprocess
import uuid
from pathlib import Path

import numpy as np


ROOT = Path(__file__).resolve().parents[2]
MUSIC = ROOT / "Assets/Resources/Music"
OUTPUT = ROOT / "Assets/Resources/MusicAnalysis"
SR = 22050
HOP = 220
DT = HOP / SR
TITLES = {"greenlandsOST": "Greenlands OST", "stage1bossOST": "Stage 1 Boss OST",
          "stageComplete": "Stage Complete"}


def unit(values):
    """Robust scaling for comparing different features of the same recording."""
    low, high = np.percentile(values, (8, 92))
    return np.clip((values - low) / max(high - low, 1e-7), 0, 1)


def fft_chunks(samples, length, hop):
    padded = np.pad(samples, (length // 2, length // 2))
    frames = np.lib.stride_tricks.sliding_window_view(padded, length)[::hop]
    window = np.hanning(length).astype(np.float32)
    for i in range(0, len(frames), 256):
        yield abs(np.fft.rfft(frames[i:i + 256] * window, axis=1))


def extract(samples):
    # Multi-band spectral flux separates drum attacks from sustained loud notes.
    size = 1024
    freqs = np.fft.rfftfreq(size, 1 / SR)
    edges = (20, 150, 500, 2000, 5000, 10000)
    masks = [(freqs >= low) & (freqs < high) for low, high in zip(edges, edges[1:])]
    last = np.zeros((1, len(freqs)))
    flux, energies = [], []
    for spectrum in fft_chunks(samples, size, HOP):
        logged = np.log1p(spectrum * 2)
        attack = np.maximum(np.diff(np.vstack((last, logged)), axis=0), 0)
        flux.append(np.column_stack([attack[:, mask].mean(1) for mask in masks]))
        energies.append(np.column_stack([np.log1p((spectrum[:, mask]**2).mean(1)) for mask in masks]))
        last = logged[-1:]
    flux = np.concatenate(flux)
    flux /= np.maximum(np.percentile(flux, 95, axis=0), .001)
    return flux, np.concatenate(energies)


def tonal_frames(samples):
    # Spectral peaks at sub-bin-interpolated frequencies reject broadband attacks.
    # Separate the low register so bass evidence can influence the inferred root.
    size = 8192
    hop = HOP * 5
    pitch_class, bass_class = [], []
    bins = np.arange(size // 2 + 1)
    for spec in fft_chunks(samples, size, hop):
        peaks = (spec[:, 1:-1] > spec[:, :-2]) & (spec[:, 1:-1] >= spec[:, 2:])
        ti, k = np.nonzero(peaks & (bins[1:-1] >= 12) & (bins[1:-1] <= 1250))
        k += 1
        left, center, right = [np.log(np.maximum(spec[ti, k + offset], 1e-10)) for offset in (-1, 0, 1)]
        shift = .5 * (left - right) / np.minimum(left - 2 * center + right, -1e-9)
        freq = (k + shift) * SR / size
        midi = 69 + 12 * np.log2(freq / 440)
        # Weight peaks by proximity to equal-tempered pitch centres.
        nearest = np.rint(midi).astype(int)
        strength = np.sqrt(spec[ti, k]) * np.cos(np.pi * (midi - nearest) * .84)**2
        pitch = np.zeros((len(spec), 12))
        bass = np.zeros_like(pitch)
        np.add.at(pitch, (ti, nearest % 12), strength * np.clip((3400 - freq) / 2600, .18, 1))
        np.add.at(bass, (ti, nearest % 12), strength * np.clip((230 - freq) / 100, 0, 1))
        pitch_class.append(pitch)
        bass_class.append(bass)
    return np.concatenate(pitch_class), np.concatenate(bass_class)


def tempo_map(onset, hint):
    # Estimate local periods rather than accumulating phase error from a single BPM.
    # Normalized autocorrelation at both the pulse and its multiples rejects subdivisions.
    tempos = np.arange(70, 196.1, .25)
    periods = 60 / (DT * tempos)
    centers = np.arange(0, len(onset), round(4 / DT))
    scores = []
    for center in centers:
        window = onset[max(0, center - 850):center + 850]
        window = (window - np.mean(window)) * np.hanning(len(window))
        spec = np.fft.rfft(window, 4096)
        acf = np.fft.irfft(abs(spec)**2)
        acf /= max(acf[0], 1e-8)
        score = sum(weight * np.interp(periods * m, np.arange(360), acf[:360])
                    for m, weight in ((1, .45), (2, .32), (4, .23)))
        scores.append(score)
    scores = np.asarray(scores)
    profile = np.mean(scores, axis=0)
    # A known tempo resolves octave ambiguity. Phase still comes from the recording.
    prior = hint or tempos[np.argmax(profile - .045 * np.log2(tempos / 132)**2)]
    allowed = np.abs(np.log2(tempos / prior)) < .32
    global_bpm = tempos[np.argmax(np.where(allowed, profile, -1))]
    # Allow real tempo changes; anchor poorly articulated intros/tails to the dominant pulse.
    penalty = 1.8 * abs(np.log2(tempos[:, None] / tempos[None, :]))
    best = scores[0] - .35 * abs(np.log2(tempos / global_bpm))
    parents = []
    for score in scores[1:]:
        paths = best[:, None] - penalty
        parents.append(paths.argmax(0))
        best = paths.max(0) + score - .11 * abs(np.log2(tempos / global_bpm))
    path = [int(np.argmax(best))]
    for parent in reversed(parents):
        path.append(parent[path[-1]])
    bpms = tempos[path[::-1]]
    return np.interp(np.arange(len(onset)), centers, 60 / (DT * bpms)), profile.max()


def track_beats(flux, hint):
    onset = flux @ np.array([.25, .17, .17, .19, .22])
    periods, confidence = tempo_map(onset, hint)
    # Reward attacks near a beat; smoothly track consecutive pulses through fills and rests.
    evidence = np.convolve(np.minimum(onset, 3), np.array([.12, .23, .30, .23, .12]), "same")
    cost = np.zeros(len(onset))
    previous = np.full(len(onset), -1, int)
    shortest, longest = int(periods.min() * .78), int(periods.max() * 1.22) + 1
    lags = np.arange(shortest, longest + 1)
    penalties = 24 * np.log(lags / periods[:, None])**2
    for t in range(len(onset)):
        n = min(len(lags), t - shortest + 1)
        if n > 0:
            candidates = cost[t - lags[:n]] - penalties[t, :n]
            winner = int(np.argmax(candidates))
            cost[t] = candidates[winner]
            previous[t] = t - lags[winner]
        cost[t] += evidence[t]
    last = len(onset) - int(periods[-1])
    beat = last + int(np.argmax(cost[last:]))
    beats = []
    while beat >= 0:
        beats.append(beat)
        beat = previous[beat]
    times = np.array(beats[::-1]) * DT
    # STFT windows are centred; flux peaks usually just precede the audible transient.
    return times, float(np.clip(confidence, 0, 1)), onset


def aggregate(values, times, step, end, offset=0):
    edges = np.clip(np.rint((np.r_[times, end] + offset) / step).astype(int), 0, len(values))
    return np.array([values[min(i, len(values)-1):max(min(i + 1, len(values)), j)].mean(0)
                     for i, j in zip(edges, edges[1:])])


def bar_features(samples, beats, duration):
    pitch, bass = tonal_frames(samples)
    pc = aggregate(pitch, beats, DT * 5, duration)
    low = aggregate(bass, beats, DT * 5, duration)
    pc /= np.maximum(pc.sum(1, keepdims=True), 1e-6)
    low /= np.maximum(low.sum(1, keepdims=True), 1e-6)
    return pc, low


def silent_spans(samples):
    # The chord decoder can carry the last harmony through digital silence.
    frame = round(.02 * SR)
    starts = np.arange(0, len(samples), frame)
    rms = np.sqrt(np.add.reduceat(samples * samples, starts) / np.diff(np.r_[starts, len(samples)]))
    quiet = rms < max(.0002, np.percentile(rms, 90) * .003)
    edges = np.flatnonzero(np.diff(np.r_[False, quiet, False]))
    return [(a * frame / SR, min(b * frame, len(samples)) / SR)
            for a, b in edges.reshape(-1, 2) if b - a >= 8]


def chord_cues(segments, beats, samples):
    duration = len(samples) / SR
    quiet = silent_spans(samples)
    onsets = [s["time"] for s in segments]
    times = sorted({*onsets, *(t for span in quiet for t in span if t < duration)})
    indices = np.searchsorted(onsets, times, side="right") - 1
    silent = np.searchsorted(np.ravel(quiet), times, side="right") % 2
    changes = []
    fit_sum = pitched_seconds = 0.
    for i, (time, idx) in enumerate(zip(times, indices)):
        segment = segments[idx]
        name = "N" if silent[i] else segment["label"]
        if not changes or changes[-1][1] != name:
            changes.append((time, name))
        if name != "N":
            length = (times[i+1] if i+1 < len(times) else duration) - time
            fit_sum += segment["confidence"] * length
            pitched_seconds += length

    # The decoder uses overlapping frames. Align near-beat attacks to the measured
    # pulse for transitions, preserving syncopation and the order of short chords.
    beats = np.asarray(beats)
    events = []
    for i, (time, name) in enumerate(changes):
        if i and len(beats) >= 2:
            index = int(np.argmin(abs(beats - time)))
            interval = beats[index] - beats[index-1] if index else beats[1] - beats[0]
            left = (changes[i-1][0] + time) / 2
            right = (time + changes[i+1][0]) / 2 if i+1 < len(changes) else duration
            beat = beats[index]
            if left < beat < right and abs(beat - time) <= min(.10, interval * .23):
                time = float(beat)
        events.append({"time": round(time, 3), "name": name})
    return events, round(fit_sum / pitched_seconds, 2) if pitched_seconds else 0.


def downbeat_index(beats, pc, low, onset):
    harmony_change = np.r_[0, np.linalg.norm(np.diff(pc, axis=0), axis=1)]
    bass_change = np.r_[0, np.linalg.norm(np.diff(low, axis=0), axis=1)]
    positions = np.clip(np.rint(beats / DT).astype(int), 0, len(onset) - 1)
    weight = .45 * unit(harmony_change) + .35 * unit(bass_change) + .2 * unit(onset[positions])
    return int(np.argmax([np.mean(weight[k::4]) for k in range(4)]))


def find_phrases(beats, down_index, pc, low, energies, duration):
    starts = beats[down_index::4]
    count = len(starts)
    # Summarize four beats of harmony, bass and timbre, retaining their order within the bar.
    bar_rows = []
    for i in range(down_index, len(beats), 4):
        idx = np.minimum(np.arange(i, i + 4), len(beats) - 1)
        bar_rows.append(np.column_stack([pc[idx] * 3, low[idx] * 1.2]).flatten())
    harmonic = np.array(bar_rows)
    levels = aggregate(energies, starts, DT, duration)
    zlevels = (levels - levels.mean(0)) / np.maximum(levels.std(0), .25)
    # Compute acoustic novelty across each candidate cut. Include multi-bar context
    # so a drum fill alone is insufficient evidence for an arrangement change.
    novelty = np.zeros(count)
    for i in range(1, count):
        novelty[i] = (np.linalg.norm(harmonic[i] - harmonic[i-1]) * .45
                      + np.linalg.norm(zlevels[max(0, i-2):i].mean(0) - zlevels[i:i+2].mean(0)) * .32)
    novelty = unit(novelty)
    # Compare the actual bar sequences at different lags, penalizing false half-cycles.
    # Use the shortest convincing cycle when an eight-bar loop contains two equal halves.
    if count < 7:
        return starts, np.arange(count), novelty, 0.
    cycles = [v for v in (2, 4, 8) if count > v + 2]
    match = {lag: np.median(np.linalg.norm(harmonic[lag:] - harmonic[:-lag], axis=1))
             for lag in range(1, max(cycles) + 1)}
    shortest_match = min(match[k] for k in cycles)
    # Four bars is a weak prior in case several periods are harmonically ambiguous.
    cycle = next((k for k in cycles if match[k] <= shortest_match * 1.08 and
                  (k > 2 or match[2] < match[1] * .8)), cycles[-1])
    if max(match.values()) / max(min(match.values()), 1e-6) < 1.12:
        cycle = 4
    # Score each rotation of the cycle by repeated arrival harmony and timbral contrast.
    # Fit the phase back to the recording rather than treating file time zero as bar one.
    phases = []
    for k in range(cycle):
        selected = harmonic[k::cycle]
        arrival_similarity = np.linalg.norm(selected - np.median(selected, axis=0), axis=1).mean()
        phases.append(novelty[k::cycle].mean() - arrival_similarity * .12)
    phase = int(np.argmax(phases))
    # Detect arrangement changes that insert/remove bars. Dynamic programming balances
    # the measured cadence, the global cycle, and localized timbral/harmonic entrances.
    arrival = np.median(harmonic[phase::cycle], axis=0)
    score = .5 * novelty + .18 * (1 - unit(np.linalg.norm(harmonic - arrival, axis=1)))
    initial = phase
    chosen = [initial]
    i = initial
    while i + cycle < count:
        # Move a phrase boundary only for strong evidence of a shifted phrase grid over
        # the *following* cycles, not simply because one adjacent chord is accented.
        targets = range(max(i + 2, i + cycle - 2), min(count, i + cycle + 3))
        def fit(j):
            accents = score[j:min(j + 3 * cycle, count):cycle]
            return accents.mean() - .16 * abs(j - i - cycle)
        i = max(targets, key=fit)
        chosen.append(i)
    contrast = 1 - shortest_match / max(match[1], 1e-6)
    confidence = np.clip(.35 + .75 * contrast + .5 * np.std(phases), .1, .95)
    return starts, np.array(chosen), novelty, float(confidence)


def section_cues(times, starts, changes, energies, onset, duration):
    # Measure local attack rate, energetic high-register motion and bass activity.
    # Loud sustained passages alone should not win the boss cue.
    attack = onset[1:-1]
    smooth = np.convolve(onset, np.ones(31) / 31, "same")[1:-1]
    peak_frames = np.flatnonzero((attack > onset[:-2]) & (attack >= onset[2:]) &
                                 (attack > smooth * 1.25 + .08)) + 1
    rates = np.histogram(peak_frames * DT, bins=times)[0] / np.diff(times)
    means = aggregate(energies, times[:-1], DT, duration)
    flux_mean = aggregate(onset[:, None], times[:-1], DT, duration)[:, 0]
    beat_rate = np.diff(np.interp(times, starts, np.arange(len(starts)))) / np.diff(times)
    # The modest tempo term is only useful where a recording really varies in tempo.
    tempo = np.clip((beat_rate / max(np.median(beat_rate), .01) - 1) * 3, -.15, .15)
    drive = (.36 * unit(rates) + .30 * unit(flux_mean) + .19 * unit(means[:, 3:].mean(1))
             + .15 * unit(means[:, :2].mean(1)) + tempo)
    count = len(times) - 1
    def boundary_strength(i):
        return changes[int(np.argmin(abs(starts - times[i])))]
    # First well-established arrangement change in the opening, strongest last return
    # among the concluding cycles. File endpoint includes the natural release / tail.
    intro_options = [i for i in range(1, count-1) if 3 <= times[i] <= min(30, duration * .23)]
    intro = max(intro_options, key=lambda i: boundary_strength(i) + .3 * (drive[i] - drive[i-1]),
                default=1)
    outro_options = [i for i in range(intro+2, count)
                    if 8 <= duration - times[i] <= min(38, duration * .30)]
    outro = max(outro_options, key=lambda i: boundary_strength(i) + .45 * (drive[i-1] - drive[i]),
                default=count - 1)
    # Locate the high-energy anchor first. Score full progressions so a single fill
    # cannot win, then build a longer boss performance around the whole anchor.
    bar_length = np.median(np.diff(starts))
    available = times[outro] - times[intro]
    windows = []
    for start in range(intro, outro):
        for end in range(start + 1, outro + 1):
            length = times[end] - times[start]
            phrase_drive = drive[start:end]
            sustained = .80 * np.average(phrase_drive, weights=np.diff(times[start:end+1]))
            sustained += .20 * np.percentile(phrase_drive, 25)
            windows.append((sustained, length, start, end))

    def best_span(candidates):
        if not candidates:
            return intro, outro
        best = max(row[0] for row in candidates)
        # Near ties favor more complete musical material.
        _, _, start, end = max((row for row in candidates if row[0] >= best - .025),
                               key=lambda row: (row[1], row[0]))
        return start, end

    anchor_min = min(15.5 * bar_length, available * .45)
    anchor, anchor_end = best_span([w for w in windows if anchor_min <= w[1] <= 32.5 * bar_length])
    # Aim for 40–64 bars and at least one minute, fitting short tracks between their
    # bookends. The initial progression must also have enough drive for the boss arrival.
    anchor_length = times[anchor_end] - times[anchor]
    boss_min = min(available, max(60, 39.5 * bar_length, anchor_length * 1.5))
    boss_max = max(boss_min, 64.5 * bar_length)
    candidates = [(.78 * sustained + .22 * drive[start], length, start, end)
                  for sustained, length, start, end in windows
                  if boss_min <= length <= boss_max and start <= anchor and end >= anchor_end]
    peak, peak_end = best_span(candidates)
    spans = dict(intro=(0, intro), body=(intro, outro), peak=(peak, peak_end), outro=(outro, count))
    return {name: {"start": float(times[a]), "end": float(times[b])} for name, (a, b) in spans.items()}


def write_meta(path, folder=False):
    meta = Path(str(path) + ".meta")
    if meta.exists():
        return
    guid = uuid.uuid5(uuid.NAMESPACE_URL, "shootemup/" + path.relative_to(ROOT).as_posix()).hex
    importer = ("folderAsset: yes\nDefaultImporter:\n" if folder else "TextScriptImporter:\n")
    meta.write_text(f"fileFormatVersion: 2\nguid: {guid}\n{importer}  externalObjects: {{}}\n"
                    "  userData:\n  assetBundleName:\n  assetBundleVariant:\n", encoding="utf-8")


def score_features(path, samples, duration):
    flux, energies = extract(samples)
    # Space, Tunnel, Core and Void have a strong dotted-quarter rhythm. Keep their
    # quarter-note pulse so the boss kick does not follow the syncopated accents.
    hints = {"fields": 169, "greenlandsOST": 140, "stage1bossOST": 172,
             "space": 173.5, "tunnel": 170, "core": 175, "void": 170}
    beats, pulse_confidence, onset = track_beats(flux, hints.get(path.stem))
    beats = beats[beats < duration]
    pc, low = bar_features(samples, beats, duration)
    offset = downbeat_index(beats, pc, low, onset)
    downbeats, phrases, novelty, phrase_confidence = find_phrases(
        beats, offset, pc, low, energies, duration)
    times = np.r_[0, downbeats[phrases], duration]
    # A leading pickup or a trailing reverb tail cannot independently be auditioned as a loop.
    times = np.r_[0, times[(times > 2) & (times < duration - 2.5)], duration]
    times = np.round(times, 3)
    adaptive = len(times) >= 6
    if adaptive:
        sections = section_cues(times, downbeats, novelty, energies, onset, duration)
    else:
        sections = {k: {"start": 0., "end": round(duration, 3)} for k in ("intro", "body", "peak", "outro")}
    # One short file is a completion sting. Still expose its detected beat/chord observations.
    edge = np.linspace(0, len(samples), 513, dtype=int)
    waveform = np.array([np.sqrt(np.mean(samples[a:b] ** 2)) for a, b in zip(edge, edge[1:])])
    waveform = np.round(np.minimum(1, waveform / max(np.percentile(waveform, 99), 1e-6)), 3)
    # Use the full grid to avoid quantizing steady BPM to the frame hop.
    intervals = (beats[8:] - beats[:-8]) / 8
    bpm = round(float(60 / np.median(intervals if len(intervals) else np.diff(beats))), 1)
    return {"id": path.stem, "title": TITLES.get(path.stem, path.stem.title()),
            "adaptive": adaptive, "duration": round(duration, 3), "bpm": bpm,
            "beats": np.round(beats, 3).tolist(), "downbeats": np.round(downbeats, 3).tolist(),
            "boundaries": times.tolist(), "sections": sections, "waveform": waveform.tolist(),
            "confidence": {"beat": round(pulse_confidence, 2), "phrase": round(phrase_confidence, 2)}}


def analyze(path, chords_only=False):
    decoded = subprocess.check_output(["ffmpeg", "-v", "error", "-i", str(path),
                                       "-ac", "1", "-ar", str(SR), "-f", "f32le", "-"])
    samples = np.frombuffer(decoded, dtype="<f4")
    duration = len(samples) / SR
    detector = Path(__file__).with_name("chords.mjs")
    segments = json.loads(subprocess.check_output(["node", str(detector), str(SR)], input=decoded))
    if not segments or segments[0]["time"] != 0:
        raise ValueError(f"{path.stem}: chord analysis did not cover the recording")
    data = None if chords_only else score_features(path, samples, duration)
    if not chords_only:
        # Cue times use gapless PCM. Unity's MP3 importer can retain the priming samples.
        info = json.loads(subprocess.check_output([
            "ffprobe", "-v", "error", "-select_streams", "a:0", "-read_intervals", "%+#1",
            "-show_entries", "stream=sample_rate:packet_side_data=skip_samples", "-of", "json", str(path)]))
        packet = next(iter(info["packets"]), {})
        skipped = sum(side.get("skip_samples", 0) for side in packet.get("side_data_list", []))
        if skipped:
            data["mp3Lead"] = skipped / int(info["streams"][0]["sample_rate"])
    OUTPUT.mkdir(exist_ok=True)
    write_meta(OUTPUT, folder=True)
    dest = OUTPUT / (path.stem + ".json")
    # Share the publication lock with the previewer. A save made while FFT analysis
    # was running must survive the final write.
    lock, tmp = Path(str(dest) + ".lock~"), Path(str(dest) + ".new~")
    with lock.open("x"):
        pass
    try:
        previous = json.loads(dest.read_text(encoding="utf-8")) if dest.exists() else {}
        edited = set(previous.get("editedSections", []))
        if previous.get("peakEdited"):
            edited.add("peak")
        if chords_only:
            if previous.get("duration") != round(duration, 3):
                raise ValueError(f"{path.stem}: run full analysis to generate a matching beat grid")
            data = previous
        elif edited:
            grid = sorted({0, *data["downbeats"], data["duration"]})
            for name in edited:
                span = previous["sections"][name]
                start, end = (min(grid, key=lambda b: abs(b - span[k])) for k in ("start", "end"))
                if not data["adaptive"] or sum(start <= b <= end for b in data["downbeats"]) < 2:
                    raise ValueError(f"{path.stem}: new bar grid cannot fit the saved {name}; kept the original sheet")
                data["sections"][name] = {"start": start, "end": end}
            data["editedSections"] = [name for name in data["sections"] if name in edited]
            if "peak" in edited:
                data["peakEdited"] = True
        data["chords"], data["confidence"]["chord"] = chord_cues(segments, data["beats"], samples)
        data["chordDetector"] = "Chordino NNLS 1.0.0"
        tmp.write_text(json.dumps(data, separators=(",", ":"), allow_nan=False) + "\n", encoding="utf-8")
        tmp.replace(dest)
    finally:
        tmp.unlink(missing_ok=True)
        lock.unlink()
    write_meta(dest)
    print(f"{path.stem:15} {len(data['chords']):3} chord segments, {data['bpm']} BPM", flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--chords-only", action="store_true", help="refresh harmony on existing cue sheets")
    parser.add_argument("tracks", nargs="*", help="MP3 basenames; omit to analyze the complete library")
    args = parser.parse_args()
    for track in sorted(MUSIC.glob("*.mp3")):
        if not args.tracks or track.stem in args.tracks:
            analyze(track, args.chords_only)
