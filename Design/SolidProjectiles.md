# Solid projectile prototype (retired)

This prototype was rejected and replaced by the approved white sprite set. Its runtime renderer and review entry point have been removed. The notes below describe the historical experiment; see `SpriteProjectiles.md` for the current implementation.

All twelve moving hostile projectile types, six player deliveries, and the legacy bullet prefabs now render shared 3D meshes. Continuous blue beams and chain lightning retain their dedicated energy renderers. Existing sprite components remain hidden as the gameplay tint/visibility channel and to preserve existing player sizing calculations.

Enemy models have a faceted solid core aligned with the existing centred CircleCollider2D, with a narrow dark bevel outside it. Fins, the delayed arrow, and carrier emitters identify behavior outside the core. No collision size, speed, damage, firing cadence or bullet cap was changed. Carrier side shots originate at the new emitter tips. Under nonuniform scale, the rendered core follows the actual circular physics radius, including needles and guided missiles; this exposes the full existing hit area rather than hiding it behind a thin sprite.

Player rounds use elongated capsule geometry and a cooler palette. Hostile models draw above player rounds and hulls. No lights, shadows, vertex snapping, bloom, or size-pulsing effects are added. Existing charge sparks, warning circles and helix wakes remain.

`ProjectileSolidGeometry` builds each cached mesh once. `ProjectileSolid` uses one Resources material with GPU instancing enabled and per-instance tint/scale properties. The serialized material also retains instancing shader variants in builds. Pooled hostile shots retain their model. A child renderer is required because Unity excludes MeshFilter from objects carrying SpriteRenderer; legacy visibility callbacks are forwarded to the original root.

Run `VerifySolidProjectiles.Run` in an isolated graphical Unity editor. It checks all model registrations, shared resources, shader compilation, actual physics boundary samples for nonuniform scales, pooling, six legacy prefabs, six districts of moving samples, and both simultaneous caps (280 hostile + 180 player). It also alternates sprite and 3D rendering in the same frozen scene for an uncapped editor comparison. Timings are not a standalone build or target-device GPU benchmark. Inspect the saved images as well as the report.

The catalog capture shows enlarged examples with small companions, followed by the six player deliveries at the bottom. Rows are: simple, needle, helix, shell; ricochet, delayed, missile, plasma; mine, returning disc, carrier, laser side round.
