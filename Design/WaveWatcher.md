# Wave Watcher review

Open **WAVE WATCHER** from the title. District buttons and the five catalog tabs cover generated waves, individual formation parts, enemy behaviors, boss states, and hostile projectiles. Wave budgets outside the shortened run remain available as EXTRA entries. Formation previews use the production spacing checks, including their fallback layout; both requested and resolved variants appear in the review reference.

Seed changes are explicit. Switching districts and replaying preserve the seed. The highlighted EASY/HARD button shows the preview difficulty; selecting either restarts the same entry with its authored firing score and difficulty factors. The watcher remembers this choice independently of normal New Game. Loop controls use normal run progression and difficulty scaling. Auto Tour moves through each catalog, then the next district; formation variations and mirrors can also be selected directly.

Space pauses, period advances 1/60 second, R replays, and C copies a reference for chat. The matching buttons work with mouse or controller navigation. Seed entry does not trigger these shortcuts.

The viewer writes its current selection and playback time to `WaveWatcherFeedback/Current.txt` every half second. Read that file when Miguel refers to the current preview in chat. **COPY FOR CHAT** pauses and saves `Latest.txt` plus `Latest.png`, and copies the exact reference to the clipboard. These local review files are ignored by Git. Editor files live at the project root; player builds use `Application.persistentDataPath`.

The reference identifies the district, stable catalog ID, difficulty, seed, loop, formation variant/mirror when relevant, aim, elapsed time, speed, and actual recipe. Use those values to locate the corresponding `WavePartLibrary`, enemy, boss, or projectile source. City has separate heavy and fast survivor entries, each triggered through the real partner-death behavior.

When scripts compile during an editor review, the watcher saves its selection, exits Play cleanly, and reopens the corrected preview in a fresh session. It retains the difficulty, seed, loop, variant, mirror, aim and playback controls, and replays from the beginning. Ordinary gameplay does not trigger this behavior. Compilation errors hold the pending preview until the code compiles successfully.

Run `VerifyWaveWatcher.RunCatalog` in a disposable Unity project copy to check catalog coverage, safe mirrored layouts, seed replay, pause/step behavior, actual spawns, both City survivors, attached beams, paging, feedback capture, tour transitions, and timing cleanup on exit. It also records live screenshots and motion sequences for visual review. `VerifyWaveWatcher.RunReload` forces a real script compilation and verifies that the corrected review reopens with its saved controls.
