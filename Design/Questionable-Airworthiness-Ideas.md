# Shootemup — combat and upgrade ideas

**Design catalogue · Revised 22 October 2026**

The game is a spaceship shooter with a relaxed attitude and room for strange upgrades. The fleet, environments and weapons should feel like they share a world. Humor can come from a coffee mug, an extra arm or an angry moon turning out to be useful. Most enemies can simply be interesting enemy ships.

The direction here is **recognizable spacecraft, distinct combat behaviors, and upgrades that produce surprising combinations**. Names can be casual. An object does not need a joke attached to it to be fun to get.

These are proposals grounded in the existing Fields, Tunnel, Snow, Thunder and City environments and the current item roster. Interactions involving existing items are suggested additions, not descriptions of current functionality. Timings and quantities are playtest starting points.

**Best first additions:** a flat directional round, a shell that stops and bursts, a charged laser, a ship that tracks your horizontal position from above, zigzag formations, and several projectile modifiers that work with existing items.

---

## 1. Projectile types

Keep projectile shapes simple enough to recognize in a busy fight. Their movement and firing rhythm provide most of the variety.

| Type | Behavior | Combat use and visual cue |
|---|---|---|
| **Needle round** | A flat, elongated shot with its nose facing its current movement direction. Curving shots rotate as they turn. | Tight aimed fans and fast precision fire. Match its narrow collider to the visible body. Retain the last heading when stationary. |
| **Burst shell** | Travels to a point, slows to a stop, charges for about 0.8 seconds, then breaks into smaller rounds. | Creates a delayed second pattern. Use a brightening core and fuse ring; early versions leave a broad gap in the burst. |
| **Pulse laser** | A harmless thin line aims, locks, then becomes a short damaging beam. | Bait the aim and move after lock. Start with fixed beams; add slow sweeps later. |
| **Returning disc** | Flies outward, curves through a visible turnaround, then returns toward its original launch point. | Makes the space behind a volley temporarily safe. It does not suddenly home on the player when its owner dies. |
| **Suspended round** | Launches quickly, visibly decelerates, hangs briefly, then accelerates in its original direction. | Changes the timing of otherwise simple patterns. Give the suspended state a pulse and limit the total lifetime. |
| **Scatter carrier** | A slow shell releases pairs of small rounds sideways along its path. | Builds lateral barriers with a navigable wake. Three releases per carrier is a useful first limit. |
| **Ricochet bolt** | Reflects once from an explicitly marked boundary. | Useful in Tunnel for angled attacks. Designate gameplay reflectors with lights or energy panels; do not make arbitrary scenery collide with bullets. |
| **Guided missile** | Turns toward the player for a short period, then flashes and commits to straight flight. | Movement baiting with a clear moment to dodge. Stop guidance before it reaches the main player area. |
| **Helix pair** | Two small rounds spiral around a shared forward path. | A rhythmic obstacle with periodic openings. Keep formations sparse enough to read the spiral. |
| **Plasma orb** | A large slow projectile that player fire can destroy. | Lets the player shoot a passage open. Give it a different outline and impact response from ordinary bullets. |
| **Proximity mine** | Drifts to a marked position, arms, then bursts after a fuse or when approached. | Controls a small area. Clearly show its trigger radius and arming delay; never arm directly on the player. |
| **Segmented beam** | Short beam sections activate in an announced sequence. | A timing challenge for crossing between pulses. Use a broken warning outline and solid active core. |

**Player versions:** needles can pierce; burst shells detonate on impact or at their fuse point; lasers reward holding a line on a target; returning discs get another chance to hit on the way back. Each weapon needs a useful default behavior before any upgrades.

**Clearing rule:** Blank Bullet and moon blocks suppress a projectile's secondary payload. Clearing a burst shell should not release its hostile fragments. Converted shots change ownership and appearance immediately.

## 2. Enemies that fit the environments

Use environment-specific silhouettes and equipment: open wings over the valley, compact hulls underground, insulated engines in snow, electrical assemblies in storms, and maneuvering thrusters among buildings. Each enemy has a clear job in combat.

### Fields — open air and long approaches

| Enemy | Movement and attack | Role / response |
|---|---|---|
| **Scout interceptor** | Fast shallow zigzags; fires paired needles after alternating turns. Long wings and a light fuselage. | A moving target that teaches turn timing. Cross its wake after it turns. |
| **Patrol gunship** | Holds the upper screen and slowly follows the player's x-position. Locks before firing a narrow aimed burst. | Creates pressure without diving onto the player. Bait it to one side and cross after lock. |
| **Heavy bomber** | Slow straight passage; drops one or two burst shells before leaving. Wide hull with visible payload pods. | A priority target. Destroying it before release prevents the delayed pattern. |

### Tunnel — compact hulls and fixed defenses

| Enemy | Movement and attack | Role / response |
|---|---|---|
| **Wall turret** | Scrolls with a marked wall mount and fires ricochet bolts at known angles. | A predictable anchor. Clear it before dealing with a moving formation. |
| **Maintenance drone** | A small industrial drone moves between wall stations, stops and fires a helix pair. | Makes an existing industrial shape useful as an enemy without needing a comic character. Attack during its station pause. |
| **Armored carrier** | Advances slowly with two escorts, then opens side bays to release mines. | A formation to dismantle. Shoot the bay while it opens; armor reduces frontal damage without invalidating a weapon. |

### Snow — heavy engines and slow threats

| Enemy | Movement and attack | Role / response |
|---|---|---|
| **Cold-weather fighter** | Mirrored slaloms in pairs; alternates short needle volleys. Broad stabilizers and enclosed engines. | Paired movement rhythm. Avoid firing at point-blank range during their central crossing. |
| **Siege gunship** | Slides into a top position and fires suspended rounds in separated columns. | Tempo disruption with visible lanes between columns. Reposition during its long reload. |
| **Shield transport** | Drifts across the upper area, launching destructible plasma orbs. Thick hull and external shield coils. | Shoot a passage before the supporting fighters arrive. No actual ally shielding is required for its first version. |

### Thunder — energy weapons and drifting platforms

| Enemy | Movement and attack | Role / response |
|---|---|---|
| **Beam drone** | Relocates between upper-screen positions; stops to aim and fire a pulse laser. | A clean bait-and-dodge enemy. Charge coils show when its aim locks. |
| **Relay pair** | Two drones establish a short damaging energy link. One moves at a time; the link shuts off during movement. | Destroy either drone to break the formation. Keep the link in the upper playfield. |
| **Storm skimmer** | A light ship drifts laterally and launches short-guidance missiles. Small stabilizer fins move as it corrects course. | Soft homing pressure between beam cycles. Missiles visibly stop tracking. |

### City — maneuverability and coordinated fire

| Enemy | Movement and attack | Role / response |
|---|---|---|
| **Pursuit fighter** | Tracks the player horizontally from the top, then locks a narrow laser line. | The strongest version of the top tracker. Tracking stops before the firing commitment. |
| **Attack drone** | Darts between available upper-screen slots and launches a burst shell during each pause. | A freely repositioning target. Thrusters announce departure; movement follows a visible path. |
| **Command gunship** | Holds a position behind two escorts, firing returning discs while the escorts recover. | A formation anchor. Destroying it cancels the escorts' next coordinated volley. |

**Possible minibosses:** a twin-engine heavy bomber in Fields; a tunnel defense platform with opening gun bays; a low, wide siege craft in Snow; a storm reactor ship with relay drones; and an armored command carrier above City. Build each around two familiar district mechanics and a readable vulnerability phase.

## 3. Movement patterns

Keep movement separate from hull and weapon choice. Most enemies can follow **enter → position → attack → recover → relocate or leave**.

| Pattern | Behavior | Good formation |
|---|---|---|
| **Top tracker** | Follows player x with limited speed and slight lag, within a fixed upper band. Freezes aim before firing. | One tracker supporting a predictable side formation. |
| **Zigzag** | Descends through alternating angular turns. Fires on every second turn. | Two staggered columns with offset center crossings. |
| **Slalom** | Smooth sine motion around a descending centerline. | Mirrored fighters or a long procession. |
| **Free reposition** | Selects an unoccupied destination in an allowed region, travels there, pauses and attacks. | Two drones alternating movement and fire. |
| **Strafe and brake** | Fast lateral entrance, visible deceleration, one burst, then exit. | A brief attack between longer formations. |
| **Committed dive** | Samples the player's position, winds up and dives through that fixed point. | One diver with a slow turret. No retargeting halfway through the dive. |
| **Escort orbit** | Escorts circle a leader; survivors peel away when the leader dies. | A compact fortress with several ways to dismantle it. |
| **Compress and expand** | A row closes toward the center to fire, then opens while reloading. | A changing target formation with a predictable gap. |
| **Alternating advance** | One enemy relocates while its partner holds and fires; then they swap. | Sustained pressure with clear damage opportunities. |
| **Reform** | Survivors fill empty formation slots after a short delay, without firing while sliding. | Gives kills a visible effect on the group's shape. |

Repositioners reserve destinations and avoid crossing the player's immediate area. Give them minimum travel distances and a limited number of relocations so they cannot indefinitely evade damage. Random destination selection should produce understandable motion.

## 4. Wave compositions

Each wave combines a main threat, a supporting rhythm and space to respond. Interesting combinations matter more than adding enemies everywhere.

| Wave | Composition | Player decision |
|---|---|---|
| **Tracking fire** | One top tracker aims while three staggered fighters enter along one side. Tracker locks before fighter fire begins. | Bait the tracker away from the intended route, then cross. |
| **Crossing formation** | Two offset zigzag columns take turns crossing the center. They fire after moving outward. | Read the rhythm or remove one column to simplify it. |
| **Bombing run** | Two bombers enter on separate beats, each carrying a burst shell. The opposite lane stays open during the active drop. | Kill the carrier early or prepare for the delayed burst. |
| **Escort group** | A command gunship and two orbiting escorts alternate discs and aimed volleys. | Remove an escort for room or focus the leader to cancel coordination. |
| **Relay screen** | A linked drone pair arrives first; a beam drone starts charging during the link's movement break. | Handle alternating energy hazards. Introductory versions never close both routes at once. |
| **Heavy cover** | A transport launches two destructible orbs; slalom fighters arrive after time to open a passage. | Shoot a route, then use it to evade the fighters. |
| **Mobile battery** | Two repositioning drones alternate attacks above a slow central gunship. Only one drone fires per beat. | Chase the mobile targets or remove the anchor for space. |
| **Layered approach** | A fan volley, a delayed burst shell and a locked beam arrive as three short stages with recovery beats. | Apply familiar responses in sequence. Later variants shorten recovery without hiding tells. |

**Example introductory City wave, roughly 16 seconds:** 0–2s: pursuit fighter enters; 2–4s: tracks x; 4–5s: aim locks and warning brightens; 5–5.5s: laser fires; 5.5–8s: recovery while a small side formation enters; 8–11s: a sparse fan; 11–14s: second tracker cycle; 14–16s: exit and collect. Progression waits for scheduled hazards to resolve.

Increase difficulty through entry side, phase offsets, support count and shorter recovery. Avoid changing all of them at once. A top tracker and a solid wall of bullets across the bottom remain an incompatible pairing regardless of their wave costs.

## 5. Upgrades and synergies

Most additions can be recognizable weapon parts or ship equipment. Leave space for a few loose, physical items alongside the existing coffee, protein, extra arms and moons. Their effects should make sense quickly and be satisfying before a full build comes together.

| Item · suggested tier | Effect on pickup | Useful combinations |
|---|---|---|
| **Needle Cannon · Rare weapon** | Fast flat rounds that pierce one target. Long axes follow their travel direction. | Another Arm widens the volley; Telekinesis curves piercing shots toward targets behind the first. |
| **Burst Launcher · Rare weapon** | Shells burst on impact or after stopping at a fixed range. | Proximity Fuse moves the burst toward nearby enemies; Splitter Core changes the fragments into a wider pattern. |
| **Beam Emitter · Rare weapon** | Short sustained laser with charge and recovery. | Focusing Lens improves sustained damage; Splitter Core produces weaker branching beams. |
| **Splitter Core · Rare passive** | Periodically turns one projectile into two weaker shots on impact. A small split indicator marks the affected shot. | Needle Cannon splits after its final pierce; Beam Emitter forks; Telekinesis guides eligible fragments. |
| **Rebound Coil · Common passive** | Periodically lets a shot redirect toward a second nearby enemy after a hit. | Protein strengthens that hit; Burst Launcher lets eligible fragments bounce once. |
| **Proximity Fuse · Rare passive** | Periodically detonates a shot near an enemy, producing a small burst even with the default weapon. | Burst Launcher gains airbursts; Telekinesis makes guided shots burst near moving targets. |
| **Capacitor · Rare passive** | Charges while firing is released; the next volley spends that charge for a stronger attack. A visible cell fills on the ship. | Caffeine charges it faster; Beam Emitter turns charge into a stronger opening pulse. |
| **Focusing Lens · Common passive** | Consecutive hits on the same target gradually increase damage, up to a cap. Switching target or missing for a while resets it. | Beam Emitter and Electricity maintain contact well; Telekinesis reduces wasted shots. |
| **Recovery Coil · Rare passive** | Every several blocked or erased enemy rounds charges a small friendly electric discharge. Grants a modest blocking satellite if no blocker is owned. | Three Moons charges it faster; Angry Moon erasures and Blank Bullet clears contribute at capped rates. |
| **Salvage Converter · Common passive** | Every few collected coins charges one seeking energy shot. Shows progress on a small meter. | Magnet eases collection; Rebound Coil lets the discharge reach a second enemy. |
| **Drone Link · Rare passive** | Adds a small support drone and slightly improves companion damage. | Twerp and Baby You benefit; Another Arm adds a muzzle to the new support drone. |
| **Reserve Cell · Common passive** | Occasionally stores a small amount of collected energy beyond the normal capacity, spending it on the next active use. | Magnet makes energy easier to gather; Blank Bullet has a little more resource flexibility. |
| **Return Module · Rare passive** | Every few volleys, shots curve back at maximum range for a second pass. | Telekinesis steers the return; Another Arm makes a folding fan of shots. |
| **Ball Bearing · Common passive** | Periodically adds a small spread of metal fragments to a hit. A simple loose bearing is the pickup. | Protein makes the fragments larger; Telekinesis gives them limited guidance. |
| **Lighter · Rare passive** | Repeated hits apply a short burn, shown as restrained engine sparks. Ordinary weapon fire can build it up. | Rapid fire from Caffeine applies it quickly; piercing shots spread it across a line of enemies. |
| **Spare Battery · Epic passive** | Stores a fraction of direct hit damage and releases a heavy electric shot when full. A visibly patched battery is the pickup. | Capacitor fills it with strong opening hits; Baby You contributes at reduced strength. |

Flavor can stay brief: Spare Battery might say “Still works.” A bearing can just be a bearing. The discovery is what the item does to the ship.

### Six example builds

1. **Piercing spread:** Needle Cannon + Another Arm + Rebound Coil. Wide volleys punch through a formation and redirect into survivors. Strong against groups; less efficient against isolated targets.
2. **Guided bursts:** Burst Launcher + Proximity Fuse + Telekinesis. Shells bend toward enemies and detonate near them. Strong coverage, with travel time keeping fast targets interesting.
3. **Orbital defense:** Three Moons + Recovery Coil + Blank Bullet. Blocking and clearing accumulate controlled electric return fire. Lasers still require movement.
4. **Salvage:** Magnet + Salvage Converter + Rebound Coil. Collecting drops fuels seeking shots that can bounce between targets. Visually satisfying as a defeated formation's drops get collected.
5. **Companion squad:** Baby You + Twerp + Drone Link + Another Arm. A small fleet with shared improvements and recognizable firing patterns. Keep allied effects restrained.
6. **Focused beam:** Beam Emitter + Focusing Lens + Capacitor. Bait an enemy into a firing window, release a charged beam, then maintain contact for growing damage. Rewards timing and positioning.

### More life for existing items

- **Caffeine:** keep its speed identity. It can also shorten player weapon charge times; enemy tells and hostile fuse timings remain unaffected.
- **Protein Supplement:** larger, heavier projectiles; wider player beams and stronger primary shells, with bounded fragment counts.
- **Another Arm:** extra muzzles can change the shape of a volley. Beams gain parallel emitters with a shared damage budget instead of multiplying every secondary trigger.
- **Telekinesis:** steering becomes particularly valuable on returning shots, needles and fragments. It does not need to make every weapon home identically.
- **Magnet:** collection-based effects give its convenience a combat payoff.
- **Rabbit's Foot:** retain its item-luck role. It already helps the player find interesting builds; it does not need an unrelated combat trigger.
- **Moons and Blank Bullet:** Recovery Coil creates an optional connection from defense to offense without making these items depend on it.

### Rules that keep combinations useful

A weapon defines delivery; modifiers adapt to it. Splitting can mean fragments for shells and branches for beams. If an effect has no useful interpretation, explain that before pickup or offer another item.

Use shared cooldowns and generation limits for secondary attacks. Fragments may rebound once, but cannot generate an endless split-and-bounce loop. Salvage shots must not produce bonus coins that immediately recharge themselves. Reserve Cell cannot create energy through its own expenditure.

Companion inheritance needs an explicit subset. Baby You can copy selected projectile modifiers at reduced strength; it cannot spawn more Baby Yous or duplicate active use. Extra projectiles should not independently multiply every charge meter.

Show relevant connections in the item preview: “Your moons charge this” or a small demonstration of the current shot splitting. Give players enough information to recognize a promising pickup without requiring a recipe list.

## 6. First expansion to prototype

**City combat set:** Needle Round, Burst Shell and Pulse Laser; Pursuit Fighter and Attack Drone; top tracking, zigzag and free reposition; Tracking Fire, Crossing Formation and Bombing Run waves. Existing ships can supply the supporting formations while new hulls are made.

**Initial upgrades:** Needle Cannon, Burst Launcher, Splitter Core, Recovery Coil and Salvage Converter, mixed with existing items. This provides projectile changes, defense interactions and collection interactions without requiring the entire catalogue.

Check that a player can bait the tracker, stop a bombing run by killing its carrier, recognize when a shell will burst, and see a meaningful difference after picking up an upgrade. Try a companion-heavy build to check visibility.

Then expand one district at a time. Fields emphasizes open movement; Tunnel emphasizes angles and positioning; Snow emphasizes timing and destructible obstructions; Thunder emphasizes alternating energy hazards; City emphasizes repositioning and coordination.

Runs can begin in different environments. Every district needs an introductory version, and difficulty should reflect run stage and previous exposure to a mechanic.

## 7. Implementation starting points

The project already separates enemies, movers and shooters and has wave costs, conflict groups, passive stats and companions. The proposals extend those systems.

- **Projectiles:** add travel, brake, fuse and detonation states for burst shells. Laser warning, active collision and repeated damage need separate timing. Rotate directional rounds from actual velocity.
- **Movers:** the existing `FollowPlayer` reads historical player positions for companions. Top tracking needs its own fixed vertical band, speed limit and aim-lock phase. Free repositioning needs shared destination reservations.
- **Waves:** supplement costs and conflict groups with lane occupation and attack compatibility so valid selections retain an escape route.
- **Items:** add shared volley, hit, kill, pickup and block events with ownership and secondary-effect limits. Existing Electricity and Angry Moon line effects provide presentation groundwork for beams; a damaging laser still needs its own behavior.
- **Performance:** use bounded lifetimes, projectile caps and pooling before combining many burst shells, fragments and companions. Measure the budget on intended hardware.

### Existing project references

- [Environment roster](../Assets/Scripts/Flow/LevelRun.cs)
- [Current item pool](../Assets/Scripts/Items/General/ItemPool.cs)
- [Enemy projectile base](../Assets/Scripts/EnemyBullets/AEnemyBullet.cs)
- [Wave catalogue and selection](../Assets/Scripts/WaveControllers/WavePool.cs)
- [Art direction and equipment silhouettes](../ArtSource/IndustrialExpansion/README.md)
