# Heaven and Void

Space and Underwater now offer Hell or Heaven. Either fifth-tier district leads directly to Void after its boss reward. Void ends the campaign after its own reward. District travel retains the ship, equipment, wallet, score and seed. Existing simulator contracts still end after their contracted boss.

Heaven retains the ten-wave district structure, opening item, two encounter decisions and earned miniboss reward. Its five regular enemies each have a modeled silhouette, explicit weapon lips and three scored formation parts:

| Enemy | Role |
| --- | --- |
| Choir Lancer | Feather-winged fighter; widening downward lance |
| Aegis Reliquary | Shield-shaped twin battery; alternating lance and accelerating arrows |
| Prism Cantor | Crystal fork; outward beams and ceiling-rebounding needles |
| Votive Spear | Fast escort; stacked, diverging aimed arrows |
| Throne Barge | Heavy crown-shaped siege platform; giant central beam and shoulder canopy |
| Dawn Gatekeeper | Timed miniboss; three mirrored moves using separate lance banks |
| Seraph Judicator | Ophanim angel: a living central eye inside three independently rotating golden wheels covered with eyes; Gates of Dawn, First Trumpet, Firmament, Judgment Pendulum, Last Trumpet and Broken Covenant |

Siege lasers reuse the existing pooled PulseLaser, continuous beam shader, shared bloom and analytic contact check. Their final footprint is visible during charging; width expands over 0.65 enemy-tempo seconds and damage follows the same envelope. Existing laser envelopes remain unchanged. Heaven's warm amber-orange fire contrasts against ivory cloud banks and golden ramparts.

Miguel's later Heaven-boss direction replaces the winged spacecraft with an eye-covered, ringed angel. Its three interlocking gold wheels carry 36 sculpted eyes around a larger pearl-white central eye. The wheels turn in different directions with slow precession; lasers remain attached to their chosen eye for the entire charge and release, while needle volleys leave the outer eyes. The core and individual eyes share boss health, with open collision space between them. The health tuning, phase count, card scores and approved laser sounds are preserved. `VerifyHeavenVoid.RunAngel` reviews the revised model and all three phases in isolation, including moving contacts, attached beams and the continuous finale.

Void contains only its boss. Normal and Hard have four forms; Easy uses the mask, mantis and final wound. Each form has two distinct attack phrases. The normal round programs provide gravity, splits, speed changes, stop-turns, ricochets, zigzags and wall runners; no projectile sprite was replaced. The boss retains a target at its body and uses violet fire throughout.

Each transition hides the boss for exactly 15 active-play seconds, suspending the timer during pause/time stop. The successive barrages use opposing margin rows, stop-turn lattices and four-edge woven sheets. Margin tells precede the releases, gaps move predictably, and rounds cannot materialize within 1.6 world units of the player. The boss rejects damage and homing while hidden, clears the barrage at reappearance and has a settling interval before its next attack. Damage is clamped at phase boundaries so large bursts cannot skip a form. All forms use the established phase break, finale and boss-reward pipeline.

Void reallocates its existing projectile reserve behind the cloud curtain to 800 standard rounds and 900 needles. It preserves the 900-live/1850-pooled limits. The full three-difficulty review created no projectile objects during these fights.

New locally generated laser charge/ignition takes and their original/best versions are retained under `ArtSource/HeavenVoidReview`, with standalone audition pages. After automatic approval review initially blocked import without listening approval, Miguel explicitly chose to keep the approved laser sounds for now. The game uses those existing beam samples with a deeper siege hum; the generated takes remain review-only. Sound Lab selections and its published bank are unchanged. Heaven/Void music is intentionally unset until Miguel supplies tracks; the previous district's song is stopped.

Validation uses `VerifyHeavenVoid.Run` in the isolated `Build/ReviewCache/Temp/DecisionHoverReview` project, followed only by `RunFixes` for the failed Heaven seed and revised cloud meshes. The targeted pass covers 399 Heaven compositions across seven seeds and all three difficulties. The original batch verifies Void's phase counts, burst-damage gates, targeting, fifteen-second intervals, pools, complete main-thread frames and final destruction. Native model and gameplay captures are retained in the review folder.
