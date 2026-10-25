# Coatings, balance and progression review

Native Unity 6000.6.2f1 renders and one combined controlled gameplay batch, after profiling and resolving the tunnel collision bottleneck.

- `Contacts.txt`: 4800 rotated/scaled collision comparisons against the original native pixel fixtures, using the production player hexagon and a blocking companion circle. Native polygon skin is preserved. All 400 complete five-district route loops passed.
- `Runtime.txt`: real player damage, transparent ring centres, later opaque contact within a broad trigger, companion blocking, distant plasma destruction, actual orbital waves, shield/core gating, gun-independent core pressure, electricity against exposed modules, poison/burn application, Snow's shell release timing and the actual two-route panel.
- `Coatings.png`: electricity and player rounds, in order: normal, fire, poison, both. Poison is muted jade; combined coatings retain both hues. Source projectile PNGs are unchanged.
- `Formations.txt`: two seeds at early/middle/late budgets for all eight districts. Existing movement geometry and choreography are preserved.
- `Space*.png`: actual coordinated orbital formations, shielded station and exposed core pressure after destroying both side guns.
- `Snow-Balanced.png`: dense rings remain, with three crowns per phrase instead of four. Phase two has six shells per crown instead of seven, avoiding prolonged waits at the shared cap. Peak observed was 253 rounds.
- `Two-Routes.png`: actual choice of only Desert and Candyland after Thunder/Tunnel. `Two-Routes-Fitted.png` is a follow-up editor render over a gameplay backdrop, verifying both captions fit after enabling size-to-fit text. Space still loops to harder Plains while keeping run progression.

Space's scouts now fire three needles on five shared accents, carriers fire five ricochets on four accents, escorts fire pairs on broader orbit arcs, and dish lasers aim and sweep with a one-second charge. The station keeps its existing health and part gates. It has faster scored releases, charged central beams, paired aimed needles, and independent exposed-core fan replies; breaking side guns still reduces ricochets.

City's enraged heavy releases five shells per accent instead of six. Plains, Thunder, Tunnel, Desert and Candyland retain their established health, wave identities and patterns; their intended challenge mechanisms differ from raw bullet count. In particular, Tunnel remains bounce-only and Candyland remains fast with minions.

Electricity resolves actual targetable enemy modules rather than their parent rigidbody, so protected hulls cannot intercept all chain damage. Its existing hit effects and coatings now reach exposed parts correctly. Static arcs share the same coating colors.

`Compilation.txt` records the final clean rebuild. Full-frame tunnel timings and the remaining recording limitation are documented in `../TunnelProfile`.
