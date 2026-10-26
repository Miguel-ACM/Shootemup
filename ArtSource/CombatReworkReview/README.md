# Combat rework review — 27 October 2026

Native Unity captures and measured reports for the complete combat pass. Screenshots use the actual game compositor and HUD. Existing projectile artwork is preserved.

- [New hulls and remodeled Space fleet](Models.jpg)
- [Early-route boss patterns](EarlyBossPatterns.jpg), [late-route boss patterns](LateBossPatterns.jpg)
- [Phase mechanism deployments](PhaseEntries.jpg)
- [City, Underwater and Hell scenery](Scenery.jpg)
- [Combined verification](Verification.txt), [clean compiler rebuild](Compilation.txt), [Windows player build](Build.txt)
- [Full-frame Tunnel measurements](TunnelPerformance.txt) and [raw frames](TunnelFrames.csv)

The initial tour found a Desert weakpoint-material error; its original failure is retained in InitialTour.txt. The fix restored complete body construction. RemainingDistricts.txt records the corrected outstanding districts. FocusedRevisions.txt covers all sixty revised routes, larger phase machinery, the corrected inward Hell score on both difficulties, moving stock-gun hits and the uncaptured Tunnel profile. Six wide formations also received offscreen entry clearance before the focused review. That review exposed the old bottom-edge cleanup removing the rising Nautilus before entry. RisingCorrection.txt records the targeted fix and confirms that the complete formation enters and fires.

The graphical tour is a deterministic observer review, with human inspection of native rendered sequences. Stock-gun measurements use a predictive pilot capped at normal movement speed. These validate real hits and timing; they are not a human difficulty playthrough.

The Windows player is in `Build/CombatRework/`. Keep its executable and adjacent data folder together. Source direction and the detailed feature ledger are in `Design/CombatRework.md` and `Design/BossAttackPatterns.md`.
