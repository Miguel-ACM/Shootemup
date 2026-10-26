# Miguel requests review — 26 October 2026

Implemented each requested feature in its own commit; follow-up corrections also remain separated by feature.

- Boss wreckage lasts five seconds from release.
- Companion laser fires straight upward.
- Player beam persists while held, bends through its target with Telekinesis, and uses the drawn path for damage. Locked station reactors remain protected. Capacitor bonuses expire after their initial phrase rather than multiplying an infinite beam forever.
- Every charge-cannon release fires immediately. The next press starts a fresh charge with no cooldown. Attack speed shortens charge time: 20 attack speed reaches full charge in 0.6 seconds versus 1.2 at the base 10.
- Charged shots detonate on contact or their flight fuse, leaving a stationary damage field. Ordinary taps retain their immediate shot. Existing approved projectile sprites are reused.
- Backdraft: rear flame cone with stacking burn. Depth Charge: rear shells that brake and fragment. Both appear in the normal item pool and have separate models.
- Repeated equipment boxes/discs now have distinct outer contours. Equipment0–2 show every pool item; original equipment portraits use the original meshes/materials. Proposed names are in Design/ItemIdentityProposals.md; proposals have not been applied.
- New Game opens Easy / Hard / Back. Hard retains existing factors. Easy uses 80% enemy health, 88% tempo, 80% hostile shot speed and 65% incoming damage, preserving authored patterns and tier/loop scaling. The choice persists across districts.
- Replacement key sprites are attached above the item card; the whole card sits above the bottom skip zone.
- Boss/miniboss bottom shadows follow their horizontal position, including multiple encounter hulls.
- Snow throws, charges, shifts and recalls physical comb emitters. The final render pass moved them below the hull and made their pale silhouette legible. Firing counts/rests remain authored. Sources and design interpretation are in Design/TouhouPatternDirection.md.

Validation: an isolated Unity 6000.6.2f1 project, separate from Miguel's editor. Initial full CleanBuildCache compilation rebuilt 108 assemblies with zero C# warnings/errors. Later source updates were compiled by the focused reviews with zero C# diagnostics. One batched gameplay review found a close-target beam miss; its corrected curve and all remaining cases passed the focused review. Only Snow emitter visibility and difficulty-label readability received later targeted visual checks. No full district run was repeated.

InitialReview.txt preserves the initial failed beam assertion for traceability. FocusedVerification.txt records the correction and remaining weapon/UI/boss checks. SnowVerification.txt records the final emitter pass. Verification.txt combines distinct passed checks. Captures use the live game, models and HUD in a controlled Plains arena; Snow's hull and firing score are exercised there to inspect delivery, rather than claiming a full Snow scenery review. Equipment catalogue images were also visually inspected. These checks establish behavior and layout; subjective weapon feel and Easy balance still benefit from Miguel's play feedback.
