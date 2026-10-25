# Projectile spawning and tunnel revision

One isolated Unity 6000.6.2f1 gameplay batch covered preparation, comparative spawn timings, both saw phases, auxiliary saws, hit/timer silence and natural wall bounces. Native renders were visually reviewed at gameplay resolution.

The previous preparation filled only the basic-round pool. The same 768-object budget now prepares all thirteen hostile deliveries, their exact pixel colliders and their special visuals in slices behind the cloud curtain. The projectile shader variant and player artwork are also prepared before combat. The shared projectile material is cached; helix gradients are reused per district.

`Runtime.txt` compares identical bursts with the new preparation and with the previous basic-round-only preparation. Both cases retain hot sprite and pixel-shape caches, so the cold comparison excludes disk/texture/driver work. First prepared needle launch was 4.408 ms; its repeat was 2.018 ms, versus 9.059 ms and 128 newly constructed objects without preparation. Ricochets, shells and lasers also avoid constructing objects during their prepared volleys. These are isolated Editor CPU spawn measurements, not a whole-game FPS benchmark. The managed allocation counter returned zero even during object construction, so that unsupported metric is omitted.

Shared glow targets now have one quarter of the scene's width and height, reducing the three bloom passes' pixel count by 75%. Blur offsets preserve the original glow footprint. `Glow.png` and `Radiance.txt` check the approved sprites, all eight palettes, actual HDR emission and a clean black background.

The main tunnel saw has 1800 base health (previously 2800); each auxiliary saw has 360 (previously 480). Both sizes emit evenly spaced, mixed-size 360-degree rings only on physical bounces. Counts, speeds, damage scaling, phase behavior, full-arena movement and the 280-live-round cap are retained. `Full-Circle.png` shows the ring expansion; `Phase-Two.png` shows the actual three-saw patrol.

`Compilation.txt` records the final clean build.
