# Music Lab

From the project root, run `node Tools/MusicLab/serve.mjs` and open [Music Lab](http://127.0.0.1:8766/). The server reads the game's MP3s and cue sheets directly. An optional numeric argument selects another local port.

Choose a track and press Play. Intro runs into Flight. Select Boss or Outro to queue a crossfade at a detected progression ending. **Preview the change** moves the audition playhead to two beats before that cut. Boss repeats a long high-energy passage at its original tempo with a 4.5 dB low shelf at 145 Hz. The outro releases the bass over eight beats. The short Stage Complete cue plays through once.

Drag **IN** and **OUT** on the waveform, or choose **Start** and **End** below it. Both snap to detected progression endings. Use **Hear loop seam** to play the last two beats and hear them wrap back to the start. **Save for game** writes the selected song’s boss loop to its Unity cue sheet. The next Unity play session and future builds use the saved range.

**Boss (fast)** is an audition-only 6% tempo lift at the original pitch. On first use the server renders and caches a fast copy of the recording so both boss modes can audition edits immediately. Both crossfade into the original outro with no speed ramp.

## Cue analysis

Run `python Tools/MusicLab/analyze.py` with NumPy and FFmpeg installed to regenerate the cue sheets. Append MP3 basenames to analyze individual tracks. The analysis uses audio onsets for pulse, bass and upper pitch classes for major/minor chord estimates, and recurring bar sequences for progression endings. The boss cue favors sustained rhythmic activity over isolated loud hits. Plains uses the existing trailer downbeat anchors where available.

The cue times are seconds in the original recording. Reanalysis keeps hand-picked boss loops and snaps their endpoints to the updated progression grid. Flight can pass through the boss passage during a long level. A boss request engages the bass boost. Chord and phrase labels are listening estimates. The final passage serves as the outro for recordings that end without a composed fade.
