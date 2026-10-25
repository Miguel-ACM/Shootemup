HELL / UNDERWATER EXPANSION

Route structure
Plains -> Snow or City
Snow -> Desert or Thunder
City -> Tunnel or Candyland
All four third-tier districts -> Space or Underwater
Space or Underwater -> Hell -> harder Plains
Boss item collection remains mandatory before continuing. Equipment, score, coins and seed persist.

New assets and enemies
Hell: horned skull revenants with portal arrivals; spectral galleons with broadside guns; cinder imps with helix fans. Twelve authored motifs use distinct infernal gates, descending hooks, crossings and flotillas. The giant armored crab has two breakable eyes, jointed side claws, warned physical sweeps and synchronized claw volleys. Both eyes are required; one lost eye accelerates the physical phase.
Underwater: reef torpedo fish, hydrofoil mantas and brass bathyspheres, with twelve current/trench formations. Seeded kelp, coral, wreck ribs, caustics and rising bubbles accompany a blue world grade. Movement is 21% slower with acceleration and braking inertia; focus tightens response. Decisions use normal handling. The submarine dives under the water, warns its emergence point, erupts in staggered crowns, then warns and charges down while laying delayed rounds on both sides. It returns from above.
All six enemies and both bosses are available in Wave Watcher and Tools > Shootemup > Enemy Editor. The shared source meshes are authored low-poly geometry; projectile sprite artwork is unchanged.

Music and feedback
Miguel's Desert, Candyland, Space, Underwater and Hell tracks now play in their own districts and load in the background. Multipart hit feedback is applied to the struck model only: a strong brief flash, small visual recoil and compression, with colliders and neighboring hull pieces steady. Dead modules remain dark. Boss death keeps the intact-hull explosions, dramatic breakup, music cut and item drop, while removing all deliberate frame holds.

Balance pass
Health factors by tier are 1.00, 1.16, 1.32, 1.48 and 1.64 before additional loops. Adjacent districts share these factors and speed scaling. Space scouts fire five needles on quicker, larger formations; carriers fire seven ricochets. The citadel has 2500 base health, faster turret phrases, larger paired aimed answers and independent reactor pressure in both phases. Desert's newly third-tier worm has 2200 base health. Guard health budgets are recorded in FinalChecks.txt; Space's orbiting escort health is additional to its listed main hulls. Candyland's fast swarm intentionally has less combined health. Hell has the largest guard budget and combines bullets with physical claw coverage.

Verification
Editor.txt: all eight diagram routes over 50 loops, invalid route rejection, and 570 seeded wave compositions across all ten districts. Every new model rendered natively; all 51 enemy/boss entries present.
Runtime.txt: real authored Hell/Underwater waves peaked at 126/37 rounds, both crab eyes required, local hit feedback, continuous boss destruction, submarine submergence/damage gate/eruption/charge/re-entry, blue world grade and depth shader compilation passed.
Feedback.txt: actual underwater movement retains braking momentum; the actual weakpoint renderer receives a strong flash while its neighbor stays unchanged.
FinalChecks.txt: updated route regression checks, miniboss budgets, background music loading and final compilation passed without C# or shader compiler diagnostics.
Native model sheets, moving-wave snapshots and boss views were inspected. The isolated Unity editor reports an existing SearchDatabase indexing exception at startup; no gameplay exceptions remained after fixing native material-property initialization.
This is a balance pass backed by production formation geometry and observed fights, not a claim that every weapon build has been exhaustively playtested. No live player run was controlled.
