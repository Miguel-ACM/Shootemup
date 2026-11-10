"""Record what the default speakers play (WASAPI loopback) to a WAV.

Used to audition web-generated SFX (e.g. Suno) without spending a download:
start this, play the take in the browser, and it stops once the take falls
silent (or after --seconds at most).
Leading/trailing silence is trimmed; the raw capture is kept alongside.

    python Tools/record_loopback.py out.wav --seconds 12
"""
import argparse
import array
import sys
import wave

import pyaudiowpatch as pa


def default_loopback(p):
    wasapi = p.get_host_api_info_by_type(pa.paWASAPI)
    out = p.get_device_info_by_index(wasapi["defaultOutputDevice"])
    for dev in p.get_loopback_device_info_generator():
        if out["name"] in dev["name"]:
            return dev
    sys.exit(f"No loopback device for default output {out['name']!r}")


def trim(samples, channels, rate, threshold=0.004, pad=0.05):
    limit = int(threshold * 32767)
    frames = len(samples) // channels
    loud = [i for i in range(frames)
            if max(abs(samples[i * channels + c]) for c in range(channels)) > limit]
    if not loud:
        return samples
    start = max(0, loud[0] - int(pad * rate))
    end = min(frames, loud[-1] + int(pad * rate * 4))
    return samples[start * channels:end * channels]


def write(path, samples, channels, rate):
    with wave.open(path, "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(samples.tobytes())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out")
    ap.add_argument("--seconds", type=float, default=15.0, help="maximum length")
    ap.add_argument("--silence", type=float, default=0.6,
                    help="stop after this much silence once sound was heard")
    args = ap.parse_args()

    p = pa.PyAudio()
    dev = default_loopback(p)
    rate, channels = int(dev["defaultSampleRate"]), dev["maxInputChannels"]
    stream = p.open(format=pa.paInt16, channels=channels, rate=rate, input=True,
                    input_device_index=dev["index"], frames_per_buffer=1024)
    print(f"recording {dev['name']} for up to {args.seconds}s", flush=True)
    data = bytearray()
    heard, quiet = False, 0
    limit = int(0.004 * 32767)
    for _ in range(int(args.seconds * rate / 1024)):
        chunk = stream.read(1024, exception_on_overflow=False)
        data += chunk
        loud = max(abs(s) for s in array.array("h", chunk)) > limit
        heard |= loud
        quiet = 0 if loud else quiet + 1024
        # Stop once the take has played and fallen silent, before any autoplay.
        if heard and quiet > args.silence * rate:
            break
    stream.stop_stream()
    stream.close()
    p.terminate()

    samples = array.array("h", bytes(data))
    write(args.out.replace(".wav", ".raw.wav"), samples, channels, rate)
    trimmed = trim(samples, channels, rate)
    write(args.out, trimmed, channels, rate)
    peak = max((abs(s) for s in trimmed), default=0) / 32767
    print(f"wrote {args.out}: {len(trimmed) / channels / rate:.2f}s peak {peak:.3f}")


if __name__ == "__main__":
    main()
