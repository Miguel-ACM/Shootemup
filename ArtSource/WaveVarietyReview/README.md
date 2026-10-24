# Modular wave review

The game now builds seeded compositions from 80 authored formation parts and 50 district-exclusive routes. Each wave combines two or three parts, varying their arrangement, side, count and entrance timing. Adjacent waves cannot reuse a part. Enemy roles unlock progressively; shops, minibosses and bosses retain their progression positions.

Spacing uses spheres containing the complete ship meshes, including their turns, plus a margin. The composer schedules swept occupancy over time and retries combinations that would collide or stretch the encounter. A wave's duration includes its final ship's departure. The complete plan is prepared before the opening breather and reused during combat.

In **Title → Wave Watcher**, **NEW SET** generates a fresh district plan; **REPLAY** preserves the current seed and composition. The small caption lists the active formation parts. All firing and movement use the game implementations, with an invisible observer target.

`Verification.txt` records the Unity audit, harder-loop checks, normal director handoffs, prepared-cache timings and fifteen complete live encounters. The review runs in the disposable `Temp/FeedbackUnityProject` copy without interacting with the player's Unity session.

`Formations.jpg` shows three successive moments from wave 8 in every district. `WaveWatcher.jpg` shows the full viewer. The two MP4s contain complete real-time pattern captures at five frames per second.

Run `VerifyWaveVariety.Run` through Unity's `-executeMethod` in the disposable project, then `Tools/render_wave_variety_review.py` to package the passing captures. `VerifyWaveVariety.Integration` uses one seed plus harder-loop checks; `VerifyWaveVariety.Preparation` checks only preparation and viewer set/replay behavior.
