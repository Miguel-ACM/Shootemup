"""Check shipped and regenerated beat grids against attacks in the recordings."""
import json
import subprocess
import unittest

import numpy as np

from analyze import MUSIC, OUTPUT, SR, score_features


class MusicPulseTest(unittest.TestCase):
    def check_recording(self, name, bpm, beats, downbeats):
        track = MUSIC / (name + ".mp3")
        pcm = subprocess.check_output(["ffmpeg", "-v", "error", "-i", str(track),
                                       "-ac", "1", "-ar", str(SR), "-f", "f32le", "-"])
        samples = np.frombuffer(pcm, dtype="<f4")
        rebuilt = score_features(track, samples, len(samples) / SR)
        shipped = json.loads((OUTPUT / (name + ".json")).read_text(encoding="utf-8"))
        self.assertAlmostEqual(shipped["mp3Lead"], 1105 / 44100, places=6)
        for sheet in (rebuilt, shipped):
            self.assertAlmostEqual(sheet["bpm"], bpm, delta=1)
            for key, attacks in (("beats", beats), ("downbeats", downbeats)):
                for attack in attacks:
                    with self.subTest(grid=key, attack=attack):
                        self.assertLess(min(abs(t - attack) for t in sheet[key]), .025)

    def test_plains(self):
        self.check_recording("fields", 169, (90.887, 116.451, 138.461, 160.477, 175.739),
                             (23.076, 101.186, 182.144))

    def test_space(self):
        self.check_recording("space", 173.5, (44.726, 45.080, 45.419, 102.128, 149.178, 176.531),
                             (44.379, 77.569, 132.913, 177.230))

    def test_tunnel(self):
        self.check_recording("tunnel", 170, (45.664, 46.020, 46.380, 113.784, 149.084, 179.794),
                             (45.310, 56.609, 135.667, 180.854))


if __name__ == "__main__":
    unittest.main()
