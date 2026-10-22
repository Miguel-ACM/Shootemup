# Enemy movement patterns

Attach one concrete mover from `Assets/Scripts/Enemy/Movers` to an enemy root, replacing its previous mover. Keep its existing `AEnemy`, collider and shooter components. These movers preserve Z, use `GameRules.enemySpeed`, and expose their settings in the Inspector. `PatternMover` is shared support, not a component to attach directly.

| File / component | Setup |
|---|---|
| `TopTracker.cs` | Set the top offset, tracking time, lock warning, firing time and recovery. It follows player X, then freezes both its position and the aim of `SingleBurst` / `FocusingSingleBurst`. |
| `Zigzag.cs` | Set width, depth per turn, first direction and turns per firing window. Stagger spawn times and alternate first direction for crossing columns. |
| `Slalom.cs` | Set amplitude, period, phase offset and descent ratio. Phase offsets let paired ships travel in opposing parts of the curve without teleporting at spawn. Firing opens near the outer turns. |
| `FreeReposition.cs` | Set a world-space destination rectangle, spacing, player clearance, preview delay, hold duration and maximum moves. Active instances reserve destinations and travel corridors. `Destination` exposes the preview point for future effects. |
| `StrafeAndBrake.cs` | Set a world-space attack position, braking distance and settle/fire/recovery times. Spawn to one side; the ship crosses through its attack position and leaves on the opposite side. |
| `CommittedDive.cs` | Set the top offset, windup and dive speed. It samples the player's position once, waits, then dives through that point without retargeting. `CommittedTarget` exposes that point for a warning effect. |
| `EscortOrbit.cs` | Assign the leader's transform as Anchor, then set radius, period and different phase offsets for the escorts. Leader loss makes them leave along their current tangents. Runtime spawners can call `SetAnchor`. |
| `CompressAndExpand.cs` | Give a row the same Formation Anchor (or Centre X), transition times and compressed scale. Spawn X positions define the expanded slots. Spawn members together to align their cycles. |
| `AlternatingAdvance.cs` | Assign each member's Partner to the other member and give one Moves First. It advances one step, holds, then hands movement to its partner. A survivor continues alone. Runtime spawners can call `SetPartner` on both members before their first frame. |
| `Reform.cs` | Assign a shared Formation Anchor and distinct Slot Order values. Members center themselves around the anchor and close gaps after a member disappears. Firing pauses through the delay and reposition. Separate groups need separate anchors. |

For a basic shooter, use activation **onArrive** to start after entry. **onVisible** and **onTrigger** still work and respect the movement firing gate. A shooter set to **None** stays inactive. Composite shooters retain their own child-trigger configuration. Firing gates suspend existing firing sequences between yields, so the shooter's own initial delay and cadence still apply; set those alongside the mover's firing window. Custom aimed shooters can use `AimPosition(target)` to honor tracker aim locks.

`Maximum Lifetime` defaults to 30 scaled seconds. On timeout the mover stops firing and flies up out of view; the existing `AEnemy` visibility callback handles removal and wave notification. These patterns are intended for normal enemy roots with that lifecycle. `SetMove(false)` pauses movement; it does not pause shooter timing.

The movement components expose state and destinations but do not add preview graphics, new weapons, prefabs or waves. Existing prefabs retain their current mover until you select a replacement.

Run **Tools → Enemy Movement → Verify patterns** for deterministic editor checks covering all ten patterns at 30/60/144 FPS, pause and lifetime behavior, aim locking, partner/leader loss, reservations, reforming and the firing gate. Results are written to `Logs/MovementPatternsVerification.txt`. The verifier creates temporary objects and cleans them up without saving a scene; run outside Play mode in an empty scene for isolated player targeting.
