# Full combat and scenery pass — 26 October 2026

Miguel requested a complete district pass, individual commits, and a player build after all work. Existing untracked warning captures and recovery scenes belong to other work and must remain untouched.

## Completion ledger

- [x] Actual muzzle preparation/release, independent mounts, logical nose aim and strafing attitudes; audit every regular enemy and champion.
- [x] Slower fair hostile travel on Hard; individually authored Easy counts, scores and warning times, with consistent route tiers.
- [x] Plains: two new enemies and dedicated formation parts.
- [x] Snow: two new enemies and dedicated formation parts.
- [x] City: two new enemies and dedicated formation parts; faster existing craft.
- [x] Tunnel: two new enemies and dedicated formation parts.
- [x] Thunder: two new enemies and dedicated formation parts; coherent downward lasers.
- [x] Desert: two new enemies and dedicated formation parts.
- [x] Candyland: two new enemies and dedicated formation parts.
- [x] Space: two new enemies and dedicated formation parts; remodel existing satellite fleet.
- [x] Underwater: two new enemies and dedicated formation parts.
- [x] Hell: two new enemies and dedicated formation parts.
- [x] Redesign all ten boss scores and distinctive second-phase entries; preserve multipart ownership, gates and rewards.
- [x] Reduce Tunnel bounce density; preserve bounce-only complete circular releases and physical patrol.
- [x] Keep Candyland boss in upper arena; remove Hell boss minions.
- [x] Quiet, dark Hell scenery with convincing craters; coherent colorful City architecture; underwater scenery and moving boid schools.
- [x] Batched isolated compilation, all-district moving visual review and targeted regression checks.
- [ ] Final player build and build verification.

## Design rules

Author bullet arrival and reachable negative space. Use large slow shapes, narrow committed answers, rests and clear handoffs. Each enemy gets a silhouette, role, modeled weapon and district-specific route. Keep existing projectile artwork and tint families. New bullets use the existing approved sprites at different scales; this does not authorize replacement projectile artwork. Boss health depends on route/loop factors only. Easy is a deliberately composed alternative, not every other bullet removed from Hard.

The fixed ten-wave district sequence, item identities and route graph remain authoritative. New content must enter the production wave composer, Wave Watcher and tuning catalog. Review visuals in actual Unity output before calling the pass complete. Use an isolated editor so Miguel's running game is not interrupted.

## Added regular fleet

Each entry has its own modeled silhouette, weapon layout, three named routes, shared volley accents and a separately authored Easy score.

| District | New craft and role |
|---|---|
| Plains | Reed Skimmer: light paired reedbank shots; Harrow Bomber: slow large scalloped fronts. |
| Snow | Icicle Weaver: suspended alternating combs; Hail Lantern: delivered exploding crowns. |
| City | Metro Lancer: fast committed needle passes; Terrace Gunboat: strafing paired gun curtains. |
| Tunnel | Baffle Spinner: slow returning discs; Rivet Mantis: aimed bouncing combs. |
| Thunder | Capacitor Kite: large paired plasma with narrow answers; Storm Organ: charged vertical beam columns. |
| Desert | Dune Moth: mirrored speed-graded wing fans; Mirage Obelisk: staggered suspended gates. |
| Candyland | Ribbon Glider: sparse slow helix ribbons; Gumdrop Mortar: alternating delivered candy shells. |
| Space | Eclipse Sail: slow wide orbital fronts alternating with committed needles; Comet Tug: offset suspended combs from asymmetric weapon pods. |
| Underwater | Nautilus Bell: large slow tidal fans and rising routes; Jet Squid: short committed needle jets. |
| Hell | Ash Censer: large slow plasma calls and aimed fan answers; Bone Harrier: diving talon formations with narrow needle fans. |

The full graphical tour found and corrected a multipart muzzle registration error that interrupted Desert's body construction. Six new wide formations also needed farther offscreen endpoints; their swept native model envelopes are checked on both sides and both difficulties. The completed early districts are retained; follow-up gameplay targets only outstanding districts and revised additions.

## Verification before the player build

- Both difficulties: production waves and both phases of every district boss. All existing regular enemies/champions and all twenty additions were observed in native Unity output.
- All sixty revised formation parts completed. Swept model spacing and deterministic composition replay passed on both sides, both difficulties, two seeds and six wave budgets per district.
- The rising Nautilus initially departed below the screen; formation-owned lifecycle now permits its lower entrance. The targeted correction produced a complete three-ship ascent and 27 live rounds.
- Six actual moving Plains stock-gun kills: Harrow Bomber 0.83–0.97 seconds and Reed Skimmer 0.30–1.14 seconds from first hit. The predictive pilot is capped at normal movement speed; these are aim benchmarks, not a human difficulty playthrough.
- Candyland's lower bound, Hell's minion exclusion, Space's exposed-core pressure and moving underwater schools passed. The final Hell score now uses inward crossing claw fans and reliable eye accents; the earlier physical-cycle gate had dropped attacks and made the finale too quiet.
- Thirty seconds of complete Tunnel frames without captures: 1,800 frames, 138 peak rounds, 16.663 ms median / 18.842 ms p99 / 21.634 ms maximum. Physics p99 0.284 ms, maximum 0.374 ms. Pooled round creation stayed at 768 throughout, including dense bounces and following physics steps.
- Native models, all district patterns, scenery and enlarged phase mechanisms were visually inspected. Hell received a further contrast reduction after its first captures remained too bright.

The initial Desert failure and subsequent successful corrections are retained in the review evidence rather than discarded. Compiler/build results and selected native captures are packaged in `ArtSource/CombatReworkReview/`.
