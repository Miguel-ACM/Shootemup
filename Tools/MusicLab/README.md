# Music Lab

From the project root, run `node Tools/MusicLab/serve.mjs` and open [Music Lab](http://127.0.0.1:8766/). The server reads the game's MP3s and cue sheets directly. An optional numeric argument selects another local port.

Choose a track and press Play. Intro runs into Flight. Select Boss or Outro to queue the next change at a detected progression ending. **Preview the change** moves the audition playhead to two beats before that cut. Boss repeats its high-energy passage with a 6% playback-rate lift and a 4.5 dB low shelf at 145 Hz. The outro brings both back to baseline over eight beats. The short Stage Complete cue plays through once.

## Cue analysis

Run `python Tools/MusicLab/analyze.py` with NumPy and FFmpeg installed to regenerate the cue sheets. Append MP3 basenames to analyze individual tracks. The analysis uses audio onsets for pulse, bass and upper pitch classes for major/minor chord estimates, and recurring bar sequences for progression endings. The boss cue favors sustained rhythmic activity over isolated loud hits. Plains uses the existing trailer downbeat anchors where available.

The cue times are seconds in the original recording. Flight can pass through the boss passage during a long level. Only a boss request engages the faster, bass-boosted loop. Chord and phrase labels are listening estimates. The final passage serves as the outro for recordings that end without a composed fade.
