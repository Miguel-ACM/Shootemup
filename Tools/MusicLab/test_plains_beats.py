"""Keep Plains' boss kick on the drum pulse when regenerating the cue sheet."""
import json
import subprocess
import unittest

import numpy as np

from analyze import MUSIC, OUTPUT, SR, score_features


class PlainsPulseTest(unittest.TestCase):
    def test_recorded_drum_attacks(self):
        track = MUSIC / "fields.mp3"
        pcm = subprocess.check_output(["ffmpeg", "-v", "error", "-i", str(track),
                                       "-ac", "1", "-ar", str(SR), "-f", "f32le", "-"])
        samples = np.frombuffer(pcm, dtype="<f4")
        rebuilt = score_features(track, samples, len(samples) / SR)
        shipped = json.loads((OUTPUT / "fields.json").read_text(encoding="utf-8"))
        # Unity retains this recording's 1,105 priming samples in its 44.1 kHz clip.
        self.assertAlmostEqual(shipped["mp3Lead"], 1105 / 44100, places=6)
        for sheet in (rebuilt, shipped):
            # Measured attacks in the recording, in source seconds. The trailer's
            # cuts sit on the intervening eighth notes and fail these landmarks.
            for key, attacks in (("beats", (90.887, 116.451, 138.461, 160.477, 175.739)),
                                 ("downbeats", (23.076, 101.186, 182.144))):
                for attack in attacks:
                    with self.subTest(grid=key, attack=attack):
                        self.assertLess(min(abs(t - attack) for t in sheet[key]), .025)


if __name__ == "__main__":
    unittest.main()
