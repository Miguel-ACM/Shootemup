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
- Give each district its own routes and a broad set of flowing compositions. Do not copy the same wave patterns between levels with different enemy names; favor continuous curves, linked formations, interweaving passes and smooth handoffs.
- Avoid slow stop-and-wait encounters, arbitrary collections of independently wandering enemies, or filling time by adding random enemies.
- Speed up slow enemies themselves; removing inter-wave delays alone does not fix pacing.
- Shops and reward decisions must be infrequent enough to preserve sustained combat stretches.
- Player fire must remain clearly visible at actual gameplay resolution: bright cores, sufficient pixel width and distinction from hostile fire.
- Bullets must render above the player hull so incoming fire remains visible and avoidable.
- Keep momentum between combat phrases. Compare changes with the existing game; do not quietly make it much easier.
- At the start of a level, wait until the cloud curtain has fully cleared, then allow a short breather before spawning the first enemies. Never hide opening enemy fire behind the clouds.
- Keep first-level time to kill reasonable with the unmodified base weapon. Check actual projectile hits against moving Plains enemies as well as sustained damage; preserve challenge through patterns instead of excessive health.
- Give every enemy a recognizable silhouette and role. Recolors or minor variations of one generic hull are insufficient.
- Snow designs need particular care. A round boss must look like a designed spacecraft, not a plate covered with buttons.
- Show projectile art previews and wait for Miguel's approval before replacing game artwork. The all-sphere 3D direction was rejected. Draw sprite pixels directly or with a Python script; do not use AI image generation. Keep distinct silhouettes and precise bullet-hell readability.
- Author bullet textures in neutral white/grayscale with transparent backgrounds. Apply all projectile colors in game code; never bake hue into the sprite pixels.
- Keep hostile bullets within one contrasting colour family per district, including beams and secondary shots. Preserve separate readable player fire and the approved pixel sprite shapes. Dense bullet patterns must run smoothly; preserve challenge while reducing collision, allocation and rendering overhead.
- Latest bullet polish: add small glossy enamel highlights through the existing sprite shader. Preserve source artwork, alpha, dark rims and pixel colliders; keep one texture sample and one render pass, with no extra lights or bullet objects.
- The standard bullet must be circular. The lopsided returning ring and bent-arrow helix previews were rejected; do not treat technical image checks as proof of good visual design.
- Thrusters are separate animated effects like the player's, never baked into hull geometry. Exceptions such as the spinning tunnel saw have no thrusters.
- Iterate models and rendered views. There is no hurry; visual inspection and quality matter.
- Bring the whole HUD on gradually with an irregular fluorescent-light startup flicker, then leave it steady and readable.
- Keep player rounds readable without glaring white columns. The latest feedback is that they are too bright.
- Choreograph firing as well as movement: shared accents, deliberate rests and coordinated releases within each wave.
- Remove the segmented laser completely. Thunder drones must not make arbitrary laser links between ships. Beams need visible charge, continuous energy, an attached emitter, a smooth release and appropriate sound.
- Preserve the snake enemy's flowing character; Miguel likes it.
- Use the existing Sound Lab/Frontlines sample workflow for new effects. Preserve saved user selections and distinguish technical audio checks from subjective listening.
- Improve the city's architectural models and variation between runs.

## District and boss identities

- Runs start in Plains, then choose Snow or City, then Thunder or Tunnel. After either third district, offer Desert, Candyland, or a direct loop to harder Plains. Desert and Candyland end by looping to harder Plains. Preserve equipment and score on every route.

- Snow: many large shells that explode into smaller bullets; a very round boss with several patterns and a harder second phase.
- Latest Snow feedback: reduce the boss's health; its base health is now 2100 rather than 3200, with the same phase patterns and run scaling.
- Plains also has tracked tank convoys on the valley road. Snow has distinct twin-auger ground siege crawlers. Ground vehicles need actual terrain contact, attached tracks or furrows, articulated weapons, and authored formations mixed with the air waves; no flying thrusters.
- Plains tank tracks and chassis follow the road's bends, driving downward relative to the scrolling terrain. The independently aiming turret must not determine the track direction.
- Improve the Plains riverbanks with natural shoreline shapes and coherent grass, gravel, reeds and stones. Keep the scrolling water and banks attached across section seams, and keep the road clear.
- Thunder: beams and quick aimed fire; a slim boss with side-emitting lasers, then 6–8 alternating sweeping beams plus a barrage in phase two.
- City: busy formations of small fast enemies. The boss is a fast/heavy duo with separate health summed in one bar and tracking attacks. The first death drops nothing; the survivor becomes aggressive and heals half its missing health.
- Latest City feedback: lower duo health; base health is now 1000 for the fast ship and 1400 for the heavy ship, with the survivor rules preserved.
- Both City ships must share encounter completion ownership so either kill order can finish the district.
- Boss victories hold the intact model through several explosions and deliberate short impact holds. Freeze the scene for half a second immediately before breakup, then cut the music on the shatter accent. Drop one random item at the wreck; require its actual collection before offering the next path or looping to Plains. The City duo's first death still gives no item and keeps the encounter going.
- Tunnel: bouncing projectiles. The boss is a rapidly spinning saw that travels like a DVD logo across the full visible arena, including its corners. **It fires dense mixed-size bursts only when it bounces, never when hit or on a timer, and has no thrusters.** It speeds up as damaged; phase two pauses to deploy two smaller, sturdy, destroyable saws with the same bounce behavior.
- The latest explicit feedback supersedes earlier ideas (in particular the saw's original on-hit emission).
- Desert: dunes, small silos and compact gray buildings; buried enemies emerge and fire rings, interceptors fire tightly spaced aimed bursts, and small articulated sandworms shoot from multiple body ports. Its boss is a giant white mechanical sandworm with red weakpoints along its body and a larger red head weakpoint. Every part must be destroyed to win.
- Desert visual feedback: Miguel chose angular, sculpted low-poly dunes. The previous striped terrain and repeated dark crescent dunes were rejected. Add drifting dust storms while keeping combat readable. Keep the worm weakpoints compact and a muted brick red.
- Desert buildings need coherent lighting and believable attached shadows. Avoid oversized dark rectangular foundations that read as crude shadow patches.
- Desert's buried ring enemy must emerge with sand particles, remain attached to the scrolling ground, and choose sites clear of buildings and silos. Its hull should rise from the sand rather than float or scale into view.
- Desert boss movement must cover the full arena, including the lower corners, while retaining the flowing articulated body and reachable weakpoints.
- Candyland: pink cloud background with falling modeled lollipops, chocolates and wrapped sweets. Many fast enemies, each firing fewer bullets. A very fast boss has less health and spawns destroyable minions. Candyland is an alternative to Desert after level 3.
- Latest Candyland feedback: make its boss much harder with more erratic movement, and improve the scenery's visual quality. Preserve the lighter firing of ordinary enemies.

## Items and decisions

- Broad weapon and active variety, burn damage, shots bouncing between enemies, and surprising combinations.
- No item at the start of a level. Free rewards normally offer two choices; luck can rarely increase that to three or four.
- Coins now add to a separate run wallet, one per pickup. They do not add score or scale with multiplier. Keep score as the combat scoring system and preserve coins across districts; reset the wallet on a new run.
- Two distinct shops: one accepts only coins, the other only multiplier. Freeze multiplier during decisions. Allow multiple purchases from the displayed stock, and keep shops open even when sold out until the player explicitly skips at the bottom.
- Shops offer three or four items, can include a large health kit when health is low, and can be skipped by staying at the very bottom.
- During the run, use the same spatial steering selection as district routing to offer two distinct encounters from SHOP, MULT SHOP, ITEM, HEALTH and CHALLENGE. Keep choices at authored wave breaks after sustained combat. HEALTH gives one health pickup; ITEM is a free item choice. CHALLENGE makes the next three waves harder, then awards an item before normal combat resumes. Do not offer a challenge without three remaining combat waves.
- Encounter choices use original Python-drawn pixel sprites rather than text: coin shop, multiplier shop, item crate, medkit and challenge crest. After selection, let the panels fade and move away gracefully before opening the chosen encounter.
- Require close contact to collect decision items so passing nearby cannot cause accidental purchases or replacements.
- Make rarity effects noticeable and distinct.
- Shop names and prices need opaque black backgrounds and must stay readable with the filter. The bottom skip zone must be reachable at the visible screen edge, with a minimal hold indicator.
- Death shows huge GAME OVER letters, then returns to the title.

## Workflow

- Implement each individual feature/enemy/item in its own commit. Commit authorized existing work, but preserve unrelated work being changed by another task.
- Batch a substantial set of changes before opening the game for testing. Avoid repeated play sessions for every tiny edit.
- Miguel's latest workflow feedback: do not test-run so much. Use one batched gameplay review, then check only a specific failed or outstanding case instead of repeating full runs.
- Do not interrupt Miguel's live run. Wait until he finishes; once he says he has finished, proceed with testing.
- If Computer Use is stopped with Escape, stop interacting with the app for that turn.
- Do not declare the visual work finished based only on compilation or scripted checks. Review the actual models, HUD and moving formations.
