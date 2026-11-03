# Music Lab

From the project root, run `node Tools/MusicLab/serve.mjs` and open [Music Lab](http://127.0.0.1:8766/). The server reads the game's MP3s and cue sheets directly. An optional numeric argument selects another local port.

Click the waveform to seek to that time. While paused, the click sets the start point for Play. With the waveform focused, the left and right arrow keys move the playhead by one second.

Choose a track and press Play. Intro runs into Flight. Request a section or another song to look for its opening chord in the next ten seconds of playback. The queue shows the chosen chord or fallback and the cut time. **Preview the change** moves the audition playhead to two beats before a queued section change. Boss repeats a long high-energy passage at its original tempo with a 4.5 dB low shelf at 145 Hz. The outro releases the bass over eight beats. The short Stage Complete cue plays through once.

Choose **Intro**, **Flight**, **Boss** or **Outro** under **Section markers**. Drag either waveform handle or use the arrows beside **Start** and **End** to step one four-beat bar. The file edges are available for pickups and tails. Each section must include one full bar. **Hear ending** auditions the last two beats and what follows them. Intro hands off to Flight, Flight and Boss loop, and Outro plays out. **Save for game** writes all edited sections for the song together. The next Unity play session and future builds read those markers.

**Boss (fast)** is an audition-only 6% tempo lift at the original pitch. On first use the server renders and caches a fast copy of the recording so both boss modes can audition edits immediately. Both crossfade into the original outro with no speed ramp.

Boss adds a hard kick on every beat in the song’s cue sheet. The game and both boss previews use the same mono mix of **Monster Kick 127** from FL Studio 20’s `Data/Patches/Packs/Drums/Kicks` folder. In the fast preview the hits move closer together and keep their original pitch.

## Cue analysis

Run `python Tools/MusicLab/analyze.py` with NumPy and FFmpeg installed to regenerate the cue sheets. Append MP3 basenames to analyze individual tracks. The analysis uses audio onsets for pulse, bass and upper pitch classes for major/minor chord estimates, and recurring bar sequences for progression endings. The boss cue favors sustained rhythmic activity over isolated loud hits. Plains uses the existing trailer downbeat anchors where available.

The cue times are seconds in the original recording. Reanalysis keeps hand-picked sections on the updated bar grid. Chord matching uses the chord sounding at the destination’s saved IN marker, or its first pitched chord if it opens with a silent pickup. Major and minor chords match separately. When no matching change fits inside ten seconds and the outgoing section, use the next progression ending or the selected OUT, whichever comes first. If both fall beyond the limit, use the last available bar line within it, then a beat or the deadline. Flight can pass through the boss passage during a long level. A boss request engages the bass boost. Chord and phrase labels are listening estimates. The final passage serves as the outro for recordings that end without a composed fade.
