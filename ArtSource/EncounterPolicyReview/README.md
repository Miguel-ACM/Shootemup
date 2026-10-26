# Encounter policy review

Every district keeps the opening free item, ten ordinary combat waves and the
earned choice after a timely miniboss kill. One pre-miniboss encounter choice
remains, after the first three ordinary waves; three more ordinary waves follow
it before the miniboss, so a challenge can still finish and pay out.

HEALTH can appear at or below 70% maximum health. ITEM and CHALLENGE cannot
appear in the same pair. Ordinary life drops and the reusable Health packs item
are unchanged; the paid shop's large health kit already requires health below
45%.

`Verification.txt` records the isolated Unity editor check over every district
and 50,000 deterministic draws at and around the health boundary. The initial
check exposed a floating-point edge case at exactly 70%; the final check passed
after the comparison was corrected. No Play mode or live game was interrupted.
