Four HUD changes, each committed separately:

- Resource rows fit the available casing height with twelve reference pixels of
  bottom padding. Health, energy and boss meters are also clipped to their frames.
- The coin and counter share a centered twenty-pixel row with middle-aligned text.
- Multiplier progress is a continuous track and exact fractional fill beneath the
  centered multiplier number, replacing six discrete blocks.
- At positive health up to 25% of maximum, the existing health/energy casing
  flashes red for 0.4s and normal for 0.4s. Filling and labels remain steady.
  Healing clears it immediately; it does not compete with the startup flicker.

ReviewHudAlignment.Run uses the production HUD, game font and a recorded gameplay
backdrop. It renders normal and both warning phases at 640×480, 1280×720 and
1366×768, without entering Play mode. The boss meter is deliberately shown in the
preview to check its bounds. Equipment is outside this isolated widget preview.

Verification.txt records actual RectTransform bounds, wallet and multiplier
alignment, exact fractional progress, the blink phases, recovery and startup.
The final images were visually inspected at native gameplay resolution.
LowHealth.gif alternates the two native warning frames at the production blink rate.
