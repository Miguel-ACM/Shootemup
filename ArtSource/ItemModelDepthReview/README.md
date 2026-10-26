# Colourful item review — 26 October 2026

Miguel's original item pool is preserved. `OriginalItemAudit.txt` lists the 13
items in the 2023 pool plus AK45 from 2025 and checks that their item scripts and
original model assets were not changed during this pass. Original item materials
are selected before the new palette logic.

The later item models use deliberately varied colours and more recognizable
objects: returning rounds have a boomerang, splitting rounds show cartridges,
the ricochet spring holds a ball, the countershot shield carries a round, the
energy reserve is a battery, and the proximity fuse is a sensor charge. Stasis
has a frost crystal, linked triggers have grips, and the coin launcher and
charged rocket show the objects they use. Deliberately flat objects retain a
plausible thickness. Names for unclear later items were changed individually;
the mapping is in `Design/ItemIdentityProposals.md`. Internal type IDs and
mechanics were not renamed.

- `Palette0.png` and `Palette1.png` show the newer equipment together.
- `Angles.png` and `Small.png` show six revised items from front and side views.
- `Verification.txt` records the native Unity render and mesh checks.
- `Compilation.txt` records the isolated clean rebuild: 108 assemblies, zero
  compiler warnings or errors.
- `NameMetrics.txt` measures the 22 new names in the compact offer-plate area.
- `OriginalItemAudit.txt` records the original-item boundary check.

These are isolated Unity renderer captures, not a gameplay session. Miguel's
live game was not interrupted. A scene transition during review exposed a stale
mesh cache; the cache now rebuilds a mesh when Unity has destroyed it, and the
same transition passed in the final capture.
