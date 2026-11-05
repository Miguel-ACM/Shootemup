# Boss round-bullet and stationary-position correction — 5 November 2026

Miguel asked for more circular bullets across all bosses and no obvious permanent places to stand idle. This pass covers the sixteen active boss types in `DistrictWaves`, including the alternate Plains, Snow and City encounters and both City survivors. It preserves the approved sprite textures, health, district palettes, phase breaks, rewards and pool limits.

Most circular rounds replace selected arrow lanes or rows with the same timing and trajectory. Extra pressure is sparse and tied to each owner's existing firing accents. Aiming fans retain a central shot after difficulty thinning; an even lane count previously left some of them pointing around the stationary target.

| Boss | Circular accents and stationary-position pressure |
| --- | --- |
| Plains bomber | Round wing tracers and nose answers punctuate payloads and the rolling orb wave. |
| Red Kite | Round sightline bursts and nose tracers pass between the paired wing streams; crossing ribbons mix both silhouettes. |
| Threshing Drum | Round windrows retain their shared release and speed; chute kernels cross the furrow between row releases. |
| Snow siege | Circular ice pellets freeze and reacquire the pilot; the top comb stays direct falling arrows with separate mouth answers. |
| Icebreaker | Floe accents release hanging, re-aiming pellets; circular wake beads keep the existing rising arcs. |
| City duo | Round headlights answer the orbiting ribbons, rear guard and street lattice. Both survivor identities retain their scores. |
| Switchyard | Circular carriages mix into the trains, and aimed express rails keep an exact central headlight on every difficulty. The express heading locks at release, after the moving engine's charge. |
| Thunder | Circular capacitors interleave with zigzags and straight aimed beads cross their stationary gaps; pylon beam choreography is preserved. |
| Tunnel | Round teeth join the widening rings and impact bursts. Aimed answers come from the centre and remain impact-triggered during bouncing phases. |
| Desert worm | A pair of planted mines lunges after hanging; other mines keep their outward spread. Round sandblast and flank accents follow surviving reactors. |
| Candy Regent | Straight first beads and curling taffy share one release; round canopy bounces and worm accents cover the old ribbon gaps. |
| Space | Circular beads are woven into the full-arena inward fronts, outward arms and collapsing rings; aimed comets cross the rotating escape seam. |
| Submarine | Round pressure fronts, plume layers, emergence crowns and wake rows keep the same movement; aimed fronts retain their middle lane. |
| Hell crab | Round canopy beads and pincer scissors complement its existing souls. The moving sweep crosses its aim line on all difficulties. |
| Heaven | The central eye sends a small aimed round fan through the moving aisle; round rows interleave with the vault, windows and remote choir. |
| Void | Each form has paired or cycling round answers matching its ports. Side interludes retain one charged side and mix circular strands into the brocade. |

## Verification

`Assets/Editor/Checks/VerifyBossRoundCoverage.cs` runs only in the isolated headless Unity checkout. It moves the real aiming target to nine fixed positions: left/centre/right by bottom/middle/top, inset 0.35 world units from the arena edges. Each position stays still until an exact approved-sprite contact or active laser footprint reaches it, with a 36-second timeout. The probe copies the actual player's polygon hitbox and scale. This deliberately does not count boss hull contact as a pass.

The batch exercises every Normal phase, each Hard final phase, each Easy opening phase, and both City survivor scores. It also records circular/arrow emissions, peak live rounds and any rounds constructed after preparation. This samples persistent parking holes; it is not a proof covering every coordinate or a human assessment of difficulty. Rendering and subjective visual approval are outside this command-line check.

The initial batch is retained separately when a case needs correction. `VerifyBossRoundCoverage.RunFixes` selects only failed or revised cases through `Cases.txt`, retaining the unaffected results. `Run` always exercises the complete batch. Detailed results are recorded in the adjacent reports.

Results: **84/84 cases, 756/756 sampled stationary targets reached**. All cases emitted approved circular rounds and constructed zero rounds during the sampled combat. The highest live count was **2,257 / 3,600** in Heaven's Hard finale. The original batch found Switchyard's lower-left position on Normal and its lower-left/centre positions on Easy could remain untouched. Recomputing its express heading after the moving charge corrected all three; only that revised boss was replayed.

The final eighteen gameplay files were byte-compared with the isolated checkout before building. The Windows build passed with **zero errors and zero warnings**, recorded in `Build.txt`; `Build/BossRoundCoverage/Shootemup.exe` is the output.
