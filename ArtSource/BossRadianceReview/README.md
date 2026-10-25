# Boss patterns and projectile radiance

Native Unity 6000.6.2f1 renders using the production sprites, models, shaders, compositor and district backgrounds.

- `Before.png` / `After.png`: the twelve approved projectile designs in all eight district palettes, followed by player rounds. PNG artwork, UVs, alpha silhouettes and pixel colliders are unchanged.
- `Bosses.png`: native arena crops from the final gameplay review. Individual full HUD captures are retained beside it.
- `Runtime.txt`: both phases of every boss, including the duo survivor, worm reactors, exposed station core and auxiliary saws. The existing 280-round cap, sprite rendering, shared hostile materials and filter resizing passed.
- `GlowRuntime.txt`: final glow review against every district background, plus a capped dense field and filter toggle/resize checks.
- `Editor.txt` / `Radiance.txt`: shader review and measured HDR output. Radiance exceeds white; an empty background contributes zero bloom.
- `Compilation.txt`: clean compilation diagnostics.

One projectile render pass still uses the original texture. One shared half-resolution extraction and two separable blur passes collect HDR energy for the entire field. No per-bullet lights, halo objects, extra projectile passes or hostile material updates are introduced. Player emission is lower to preserve readable cyan fire without glaring white columns. Glow remains active with the pixel filter disabled.

The bomber now shares one twelve-beat score across its batteries. Other bosses retain their established attack identities, counts and pauses, with symmetric bowed fans, interleaved half-lane replies and locked phrase axes. The tunnel saw's existing bounce-only combs are preserved.

Two controlled gameplay batches covered firing and the final visual correction; no full player run was needed. These captures verify appearance and behavior, not an FPS benchmark.
