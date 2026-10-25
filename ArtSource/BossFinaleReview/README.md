# Boss finales and City encounter completion

Actual Unity 6000.6.2f1 captures from an isolated project. The active game was left alone.

## City progression fix

The fast ship was registered with the encounter controller, but its spawned heavy partner was not. A fast-first kill order therefore left the surviving heavy ship unable to complete the district. Both ships now share the encounter owner. Both kill orders were checked through survivor healing, final destruction, mandatory item pickup and path selection.

## Finale

The intact boss hull remains visible through five pre-explosion accents and two short authored simulation holds. A final half-second hold freezes the arena before shattering. Music stops exactly on the breakup; prepared polygon wreckage is released together with a larger explosion and shockwaves. Whole-body wreckage includes the Desert worm's articulated sections and excludes extinguished engine flames. Existing explosion samples are reused.

After the main blast opens, one random item falls at the wreck's position, clamped into collectible screen bounds. It has no SKIP action. The real item pickup callback unlocks the next path, or the existing final-district loop to Plains. Full weapon/active slots still use the established replacement controls. The first City ship and miniature saws do not trigger victory rewards.

## Verification scope

`Before-CityLock.txt` records the reproduced lock. `InitialBatch.txt` preserves the complete first review, including passing checks for both City orders, Plains, Snow, Thunder and Tunnel, and the Desert finale/item gate. That batch stopped at the Desert return because the old test harness loaded L1 directly and left the title's parked preload blocking speculative route-music loading. The harness now uses the real cloud-transition entry. Only Desert and Candyland were rerun; both passed their reward-gated returns (`FinalDistrictChecks.txt`). `Verification.txt` combines the completed checks and asserts every district's required outcome.

The checks also cover pause/resume during the final hold, cancelling active Bullet Time, intact hull visibility, prepared wreckage timing, exact one-item drops, no bottom-edge bypass, occupied-slot replacements, and music cut timing. The six-panel gallery shows the held models and the item/route ordering; it is not an audio listening review.

Reproduce a full batch with `VerifyBossFinale.Run`; use `VerifyBossFinale.RunRemaining` for the focused final-district continuation. Package with `Tools/render_boss_finale_review.py`.
