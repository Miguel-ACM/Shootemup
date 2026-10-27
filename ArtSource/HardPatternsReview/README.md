# Boss-specific Hard patterns — 27 October 2026

The previous Hard balance is now Easy. Hard develops a separate attack identity for each boss, using evolving geometry and coordinated pressure rather than renamed fans.

- [Early bosses](EarlyHard.jpg), [late bosses](LateHard.jpg), [four pattern examples](HardPatterns.jpg)
- [Space orbital motion](SpaceOrbits.gif), assembled from actual Unity captures at four frames per second
- [Verification](Verification.txt), [exact Easy parity](EasyParity.txt), [motion/contact regressions](MotionChecks.txt)
- [Complete-frame performance](Performance.txt), [raw dense frames](DenseFrames.csv), [runtime report](Runtime.txt)
- [Clean compiler rebuild](Compilation.txt), [Windows player build](Build.txt)

The observer review covers long enough sequences for deployed circles and wakes to travel through the arena, with actual collisions retained. It is a visual and technical review, not a human difficulty playthrough. The captured animation's sampling rate is not the game's frame rate. The first pure regression attempt was corrected to account for edit-mode automation not dispatching MonoBehaviour.OnDisable; clearing is checked through inactive delivery objects, while Play mode maintains its live registry.

The submarine's first pressure crown now restarts correctly even when a phase change interrupts the preceding eruption. Projectile sprites, per-district hues, native contacts, exact opaque-pixel collision, shared bloom and the 280 live / 768 pooled budget are preserved.

Research and individual boss intentions: `Design/HardBossPatterns.md`. Player build: `Build/HardPatterns/Shootemup.exe` with its adjacent data folder.
