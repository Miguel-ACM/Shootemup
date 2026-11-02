# Boss targets and difficulty — 2 November 2026

The Space boss is a connected orbital fortress with swept solar wings, radiators,
an armored reactor aperture and a recessed cyan lens. One central reactor owns
all boss health. Exterior generators and gun weakpoints are removed. The former
generator gate and shutters are gone. The existing gravity cards and attached
laser remain, with normal phase changes at half health and Hard's third phase
at one quarter. Its authored base health stays 2500, before district and existing
boss sturdiness factors.

The crab has one polygonal weakpoint across its whole head, including both cheeks
and the center. The eyes are visual firing ports sharing that head's health.
Its authored base health remains 3000. Half health accelerates the claws and
opens phase two; Hard retains Inferno at one quarter. Head hit feedback moves
and flashes only the head model, keeping its collider and pincers steady.

Both bosses preserve their continuous explosion/breakup finale, health drops,
mandatory item reward, phase invulnerability and approved projectile artwork.

| Setting | Easy | Normal | Hard |
| --- | --- | --- | --- |
| Score factor | 0.5 | 1 | 2 |
| Authored round density | 42% | 65% | 100% |
| Rest factor | 1.7 | 1.3 | 1 |
| Hostile travel factor | 0.64 | 0.76 | 0.82 |
| Enemy tempo | 0.94 | 1 | 1.08 |
| Enemy health factor | 0.85 | 1 | 1 |
| Boss phases | 2 | 2 | 3 |

Normal exactly preserves the former Easy configuration: all global factors,
76 configured actors and 40 seeded production wave signatures matched the
snapshot taken before the change. Easy also removes alternate authored ordinary
wave firing accents, preserving shared formation movement. Existing Snow and
submarine low-density geometry remains on Normal, with additional thinning on
Easy. Every hit still removes one player health segment.

New runs default to Normal. The title and Wave Watcher offer all three tiers.
Difficulty multiplies score awards independently of combo growth and coins.
Easy retains half-points internally so repeated small hits still earn exactly
half score; the integer HUD stays compact.

Verification used one isolated native combat batch, covering both bosses on all
three tiers, native projectile hits, homing, protected phase changes, Hard-only
third phases, prepared breakup and boss rewards. Updated multipart meter,
invulnerability and detached-target regressions passed. The title menu was
checked separately after correcting the review harness's paused-time wait.
