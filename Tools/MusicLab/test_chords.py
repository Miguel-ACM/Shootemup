"""Exercise the offline chord estimator on a scored passage with known change times."""
import json
import subprocess
import unittest
from bisect import bisect_right
from pathlib import Path

import numpy as np

from analyze import SR, chord_cues


class ChordRecognitionTest(unittest.TestCase):
    def test_score(self):
        score = [("N", []), ("C", [36, 60, 64, 67]), ("Am", [45, 57, 60, 64]),
                 ("Fmaj7", [41, 60, 64, 65, 69]), ("G7", [43, 59, 62, 65, 67]),
                 ("C/E", [40, 60, 64, 67]), ("Dm7", [38, 57, 60, 62, 65]),
                 ("G", [43, 59, 62, 67]), ("Em", [40, 55, 59, 64]),
                 ("A#", [46, 58, 62, 65]), ("F", [41, 57, 60, 65]), ("N", [])]
        beat = .43
        starts = beat * np.array([0, 2, 6, 10, 14, 17.5, 22, 26, 30.5, 34, 38, 42, 44])
        samples = np.zeros(round(starts[-1] * SR), dtype="<f4")
        for (_, notes), start, end in zip(score, starts, starts[1:]):
            block = samples[round(start*SR):round(end*SR)]
            t = np.arange(len(block)) / SR
            gate = np.minimum(1, t/.012) * np.clip((end-start-t)/.025, 0, 1)
            for i, note in enumerate(notes):
                freq = 440 * 2**((note-69)/12)
                tone = sum(np.sin(2*np.pi*freq*h*t + .12*h) / h**1.5 for h in range(1, 7))
                level, decay = (.23, .28) if i == 0 else (.15, .4)
                block += level * gate * np.exp(-decay*t) * tone
        result = subprocess.check_output(["node", str(Path(__file__).with_name("chords.mjs")), str(SR)],
                                         input=samples.tobytes())
        cues, _ = chord_cues(json.loads(result), np.arange(0, starts[-1]-.01, beat), samples)
        times = [c["time"] for c in cues]
        for (name, _), start, end in zip(score, starts, starts[1:]):
            index = bisect_right(times, (start+end)/2) - 1
            self.assertEqual(cues[index]["name"], name)
            self.assertAlmostEqual(times[index], start, delta=.10)


if __name__ == "__main__":
    unittest.main()
