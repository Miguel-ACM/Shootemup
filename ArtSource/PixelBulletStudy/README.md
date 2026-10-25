# White bullet sprite study — preview only

Run `draw_preview.py` with Python and Pillow to regenerate the twelve transparent PNGs, `preview.png`, `rotation_check.png`, and the focused `corrections.png`. Source pixels come from authored character grids and direct per-pixel construction in `corrected_shapes.py`. No image-generation service is used. Source images contain only equal RGB channels and alpha values of 0 or 255. Multiply their RGB by a runtime color when an approved version is integrated; preserve alpha.

This folder is outside Unity's Assets directory. No gameplay renderer, collider, behavior, or sprite reference was changed.

The white study went through four reviewed passes:

- **02:** redrawn bodies in white; longer needles/missiles, larger arrow gap, thicker ring.
- **03:** stronger grayscale separation, curved helix, shell without the button-like seam, diamond-bodied carrier, directional side dart.
- **04:** removed the disc's awkward blade extensions and restored a clean opening.
- **05:** connected diagonal contour pixels after inspecting rotations on light backgrounds.
- **06:** player rejected the ring and helix. Replaced the standard with an exact circular mask, the helix with a tapered S stroke, and the disc with a centred cutting rim, broad face and small opening. These remain proposals, not approved final art.

The final contact sheet shows 6× enlarged designs and 1×/2× samples on dark and light backgrounds. The rotation sheet shows 0°, 45°, 90°, and 135° at 2×, plus an alpha-only silhouette. These are image-space art reviews, not in-game movement or collision verification. Integration still requires Miguel's approval.

The colored first study and its source are retained in `iterations/` for comparison only; the current `sprites/` exports are all neutral.
