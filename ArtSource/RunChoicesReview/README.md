# Exploding shells, coins, shops and encounter choices

Real Unity 6000.6.2f1 gameplay captures and integration checks in an isolated project. The active game editor was left alone.

## Simultaneous shell release

Sixteen shells release their full 192-child pattern in one frame, using the original sprites, exact pixel colliders and 280-bullet limit.

| Measured section | Before | After |
| --- | ---: | ---: |
| First simultaneous detonation CPU | 43.9195 ms | 20.6233 ms |
| New child bullet objects in combat | 192 | 0 |
| Ignition particle systems | 16 | 1 |

This is a specific editor CPU workload, not an overall FPS claim. Child bullets are prewarmed before combat, with pool capacity reserved for them. Ignition shares a bounded particle renderer. Mass bullet erasure uses shared particles rather than hundreds of individual shockwave renderers; other shockwaves also respect the existing fireball budget. A real moving shell barrage is captured in the first part of the movie.

## Economy and decisions

- Coin pickups add one to the wallet, independently of score and multiplier. The wallet persists across districts and resets on a new run.
- Coin shop prices are 12 / 22 / 36 / 55 by rarity; a low-health kit costs 10. Multiplier shops retain multiplier prices and never spend coins or score.
- Both paid shops allow multiple purchases and remain open even when sold out until the player holds the bottom SKIP zone. Free rewards still grant one choice.
- After the third and sixth combat phrases, the familiar steering panels offer two distinct options from SHOP, MULT SHOP, ITEM, HEALTH and CHALLENGE.
- HEALTH gives one existing health pickup. CHALLENGE raises the next three wave budgets by three (up to 20), adds 20% enemy health and 15% movement / bullet speed, then grants a free item choice. Its third wave must clear before the reward. Later enemies return to normal difficulty; challenge modifiers never alter global run stats.
- City boss base health is 1000 fast / 1400 heavy, down from 1500 / 2100. The existing survivor healing and aggression remain.

`Verification.txt` covers real coin collection, independent currencies, multiple purchases, sold-out exit, steering, all five encounter types, actual challenge enemies, exactly three completed waves, the reward pickup, restored normal combat, City health scaling, wallet travel and new-run reset. `GameplayReview.jpg` and `RunChoicesMotion.mp4` show the new HUD, shops, choices and shell motion.

Reproduce with `VerifyRunChoices.Baseline` before the changes, then `VerifyRunChoices.Run` in a separate project. Package using `Tools/render_run_choices_review.py`.
