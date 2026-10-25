CALDERA / SHORT CLAWS / ABYSSAL HUNTER

Hell now uses InfernalCaldera instead of the old DepthBackdrop river shader and scatter scenery. It has modeled, beveled basalt shelves with fissured mineral surfaces, reflected warm undercuts, raised rubble and volcanic craters above a separate molten bed. The former bright gold crater disk was recolored as dark ceramic lava. Volcano firing remains attached to the scenery. Hell-Final.png is the final material preview; Hell-Caldera.png and boss captures document the geometry and poses before the final surface pass.

Crab hands are 1.75 times the old size, on four short, overlapping arm sections per side. The claw travel stays within 2.7 world units vertically of the body, and the physical hit radius follows the larger pincers. The limit is clamped throughout entry and motion. The claws intentionally do not reach the bottom of the screen. Both eye weakpoints and existing bullet phrases remain.

The rejected rectangular submarine mesh was removed. AbyssHunterMesh builds a continuous arched pressure hull with a broad prow, swept ivory shoulders, recessed launch channels, a compact raised sail and paired stern impellers. The central hull collider was widened to match. Existing eruptions, submergence and charge patterns remain. Hunter-Model.png and Hunter-Patrol.png show the new model in the native renderer.

Runtime.txt records the focused scene/pose checks: separate new Hell environment, enlarged pincers, eight short arm sections, bounded reach, new hunter hull, broader collider and native lava shader. Materials.txt records the final native basalt/lava surface render. No C# or shader compiler diagnostics remained. Only these affected scenes and poses were tested; no full run or live player session was controlled.
