# Industrial PSX visual pass

Open `Assets/Scenes/PSX_Showcase.unity` and enter Play mode for an automatically flying and firing preview with a sample enemy fleet. Click the Game view before using keyboard controls. F8 switches between autopilot and normal player controls; F9 enables the optional pixel filter. Native resolution is now the default.

`Assets/Scenes/L1.unity` contains the ship effects, sprite bullets, mesh enemies, rotating equipment and industrial field with normal enemy waves. The segmented industrial HUD is shared with the showcase. Autopilot belongs only to the showcase.

- Player ship: 236 triangles, original 256px steel texture atlas.
- Player and enemy rounds: sprite quads, shared unlit material, no per-bullet mesh models or trails; original collision and damage remain in use.
- Engines: two four-triangle additive exhaust plumes, stepped animation at 15 Hz, movement response and brief cannon flashes.
- Environment: six reusable textured meshes (12–36 triangles each), scrolling shaft panels and 22 recycled tumbling salvage pieces. Background pieces have no colliders.
- Screen: native resolution by default, no full-screen dithering, vignette or vertex wobble. Retro detail comes from pixel textures and flat lighting. F9 retains an optional 320-line comparison mode.

The editor captures 30 actual Game view frames a few seconds after the showcase starts. `PreviewReport.json` records the live projectile count and render resolution. The GIF is a cropped, nearest-neighbour scaled version of those frames.

See `ArtSource/IndustrialExpansion/README.md` for the current fleet, equipment and UI revision, model catalog and validation reports. Regenerate the current setup through **Tools > PSX Player > Install industrial fleet and UI** outside Play mode. The older visual-pass installer recreates the previous heavy-filter setup.
