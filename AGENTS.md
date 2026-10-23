# Working direction from Miguel

These are the player's instructions. Preserve them across sessions and apply them to new work.

## Trust the player

**“Trust the player please, minimal UI, don't overexplain everything with a lot of text.”**

- Prefer clear shapes, animation, sound, key/button sprites and visual feedback over instructions and labels.
- Do not fill combat or equipment selection with explanatory text. Show only information that helps an immediate decision.
- Keep equipment outside combat: compact stylized weapon octagons at bottom left/right, with smaller active octagons touching the upper outer corner of each weapon octagon.
- Keep score, multiplier (including its progress indicator), and enemy/boss health outside the main combat area too. Use the side margins without overlapping equipment or the collected-item column.
- Show actual 3D equipment models, including a deliberately modeled base gun. Keep the whole loadout display small.
- Choose replacements while hovering over the item, never in advance. Show both key/button sprites only when both corresponding slots are occupied; blink those equipment octagons. Pressing a control immediately picks up the item and replaces that slot. Fill empty slots automatically.

## Combat should look like a dance

- Shoot-em-up waves need authored choreography: coherent formations, mirrored paths, purposeful timing, coordinated volleys, entrances and exits.
- Avoid slow stop-and-wait encounters, arbitrary collections of independently wandering enemies, or filling time by adding random enemies.
- Speed up slow enemies themselves; removing inter-wave delays alone does not fix pacing.
- Shops and reward decisions must be infrequent enough to preserve sustained combat stretches.
- Player fire must remain clearly visible at actual gameplay resolution: bright cores, sufficient pixel width and distinction from hostile fire.
- Bullets must render above the player hull so incoming fire remains visible and avoidable.
- Keep momentum between combat phrases. Compare changes with the existing game; do not quietly make it much easier.
- Keep first-level time to kill reasonable with the unmodified base weapon. Check actual projectile hits against moving Plains enemies as well as sustained damage; preserve challenge through patterns instead of excessive health.
- Give every enemy a recognizable silhouette and role. Recolors or minor variations of one generic hull are insufficient.
- Snow designs need particular care. A round boss must look like a designed spacecraft, not a plate covered with buttons.
- Use the established projectile art style. Miguel preferred the older bullets to the initial expansion's plain discs.
- Thrusters are separate animated effects like the player's, never baked into hull geometry. Exceptions such as the spinning tunnel saw have no thrusters.
- Iterate models and rendered views. There is no hurry; visual inspection and quality matter.
- Improve the city's architectural models and variation between runs.

## District and boss identities

- Runs start in Plains, then choose Snow or City, then Thunder or Tunnel. Beating either final district loops directly back to harder Plains while retaining the run's equipment and score.

- Snow: many large shells that explode into smaller bullets; a very round boss with several patterns and a harder second phase.
- Thunder: beams and quick aimed fire; a slim boss with side-emitting lasers, then 6–8 alternating sweeping beams plus a barrage in phase two.
- City: busy formations of small fast enemies. The boss is a fast/heavy duo with separate health summed in one bar and tracking attacks. The first death drops nothing; the survivor becomes aggressive and heals half its missing health.
- Tunnel: bouncing projectiles. The boss is a rapidly spinning saw that travels like a DVD logo across the full visible arena, including its corners. **It fires dense mixed-size bursts only when it bounces, never when hit or on a timer, and has no thrusters.** It speeds up as damaged; phase two pauses to deploy two smaller, sturdy, destroyable saws with the same bounce behavior.
- The latest explicit feedback supersedes earlier ideas (in particular the saw's original on-hit emission).

## Items and decisions

- Broad weapon and active variety, burn damage, shots bouncing between enemies, and surprising combinations.
- No item at the start of a level. Free rewards normally offer two choices; luck can rarely increase that to three or four.
- Two distinct shops: one accepts only score, the other only multiplier. Freeze multiplier during decisions.
- Shops offer three or four items, can include a large health kit when health is low, and can be skipped by staying at the very bottom.
- Require close contact to collect decision items so passing nearby cannot cause accidental purchases or replacements.
- Make rarity effects noticeable and distinct.
- Shop names and prices need opaque black backgrounds and must stay readable with the filter. The bottom skip zone must be reachable at the visible screen edge, with a minimal hold indicator.
- Death shows huge GAME OVER letters, then returns to the title.

## Workflow

- Implement each individual feature/enemy/item in its own commit. Commit authorized existing work, but preserve unrelated work being changed by another task.
- Batch a substantial set of changes before opening the game for testing. Avoid repeated play sessions for every tiny edit.
- Do not interrupt Miguel's live run. Wait until he finishes; once he says he has finished, proceed with testing.
- If Computer Use is stopped with Escape, stop interacting with the app for that turn.
- Do not declare the visual work finished based only on compilation or scripted checks. Review the actual models, HUD and moving formations.
