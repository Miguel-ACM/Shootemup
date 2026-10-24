# Wave Watcher

Open **WAVE WATCHER** on the title menu. Select a district, then any wave (the game's authored IDs 2–20), its miniboss, or either boss phase.

The viewer runs the real enemy factories, movement, firing schedules, aiming and projectiles. The invisible player cannot shoot, take damage or absorb bullets. AIM moves its real targeting position left, centre or right. Controls stay in the side margins.

- Replay restarts the selected encounter.
- Loop waits until the enemies and their remaining projectiles clear before repeating a wave.
- Pause, 0.5x, 1x and 2x control the actual simulation.
- Back / Escape / controller Back returns to the title. Normal START remains a normal run.

City phase two shows its actual enraged survivor. Tunnel phase two deploys the two real smaller saws. Bosses continue until a different encounter or Replay is selected.

`Comprehensive.txt` records all 95 wave selections, live shooting in every district, all minibosses, phase-two encounters, observer behavior and return to normal play. `FinalChecks.txt` adds non-wrapping label checks and automatic replay after the last projectiles clear. Both passes used an isolated Unity project copy and reviewed actual rendered gameplay; the open working editor was left alone.

`WaveWatcher.jpg`, `TitleEntry.jpg` and `WaveWatcher.mp4` show the final UI.

For verification, launch a disposable project copy with `-executeMethod VerifyWaveWatcher.Run`; `RunFinal` runs the shorter UI/replay regression pass. These command-line verifiers exit that editor on completion.
