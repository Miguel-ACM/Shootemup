# Music Lab

From the project root, run `node Tools/MusicLab/serve.mjs` and open [Music Lab](http://127.0.0.1:8766/). The server reads the game's MP3s and cue sheets directly. An optional numeric argument selects another local port.

Choose a track and press Play. Intro runs into Flight. Select Boss or Outro to queue a crossfade at a detected progression ending. **Preview the change** moves the audition playhead to two beats before that cut. Boss repeats a long high-energy passage at its original tempo with a 4.5 dB low shelf at 145 Hz. The outro releases the bass over eight beats. The short Stage Complete cue plays through once.

Choose **Intro**, **Flight**, **Boss** or **Outro** under **Section markers**. Drag either waveform handle or use the arrows beside **Start** and **End** to step one four-beat bar. The file edges are available for pickups and tails. Each section must include one full bar. **Hear ending** auditions the last two beats and what follows them. Intro hands off to Flight, Flight and Boss loop, and Outro plays out. **Save for game** writes all edited sections for the song together. The next Unity play session and future builds read those markers.

**Boss (fast)** is an audition-only 6% tempo lift at the original pitch. On first use the server renders and caches a fast copy of the recording so both boss modes can audition edits immediately. Both crossfade into the original outro with no speed ramp.

## Cue analysis

Run `python Tools/MusicLab/analyze.py` with NumPy and FFmpeg installed to regenerate the cue sheets. Append MP3 basenames to analyze individual tracks. The analysis uses audio onsets for pulse, bass and upper pitch classes for major/minor chord estimates, and recurring bar sequences for progression endings. The boss cue favors sustained rhythmic activity over isolated loud hits. Plains uses the existing trailer downbeat anchors where available.

The cue times are seconds in the original recording. Reanalysis keeps hand-picked sections on the updated bar grid. A requested change uses the next detected progression ending within the outgoing section, or its selected end if that comes first. Flight can pass through the boss passage during a long level. A boss request engages the bass boost. Chord and phrase labels are listening estimates. The final passage serves as the outro for recordings that end without a composed fade.
