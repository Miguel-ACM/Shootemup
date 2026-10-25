The previous Unity compile log contained 80 distinct C# warnings:

- 75 obsolete-API warnings from 36 unsorted object searches and three singleton
  searches. The replacements preserve unsorted queries; the singleton lookups
  belong to isolated editor previews with one player/HUD.
- Three write-only fields in WaveOrchestrer: isShopUp, claimedItem and currShowing.
  Removed their assignments and stale reflection setters in older review probes.
  The current DecisionShop and CombatDecision flow remains authoritative.
- Two ambiguous reflection-result comparisons in the City boss ownership review.
  Those assertions now explicitly compare the encounter owner references.

VerifyCleanCompilation.Run requests CleanBuildCache in an isolated Unity project.
Its final check reads the fresh compiler log because Unity 6's clean-cache build
does not emit every per-assembly callback. It checks compiler diagnostics and
requires the game, editor and both PathCreator assemblies to have compiled.

Verification.txt records the final full rebuild: 108 distinct Csc assemblies,
zero warnings and zero errors. Assemblies.txt lists them; Diagnostics.txt is empty.
All production C# sources were synchronized into the isolated project before the
rebuild. No gameplay runs, warning suppressions, gameplay balancing, artwork or
package-version changes were needed.

The unrelated projectile sprite importer edits that preceded this task were
preserved and excluded from the cleanup commits.
