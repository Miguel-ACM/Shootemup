# Patterns with a visible cause

Research, 26 October 2026:

- [ZUN, Garakuta interview (2023)](https://en.touhougarakuta.com/article/specialtaidan_zun_hiroyuki_5-en/): attacks become interesting when they have an identity connected to their owner. Names alone do not supply that identity.
- [ZUN and Cave's IKD interview (2024)](https://news.denfaminicogamer.jp/interview/241225z): ZUN connects geometric patterns, music and emotional expression; IKD starts with enjoyable dodging and refines the shape. Beauty includes how a player moves through the pattern.

Our application of those ideas (design decisions, not claims of Touhou internals):

1. Give each phrase one legible movement problem: lead an aimed accent, cross a comb, weave an expanding crown. Keep intentional rests and handoffs.
2. Commit aimed fire before release. A lane should stay a lane long enough to recognize and traverse; avoid continually correcting every bullet toward the player.
3. Build a family through rotation, phase offsets, speed gradients and alternating gaps. Escalation should develop that family rather than stack unrelated attacks.
4. Every remote release needs a visible delivery: a thrown seed, shell, deployed drone, attached muzzle or previously established environmental emitter. No unexplained firing coordinates.
5. Retain approved projectile silhouettes, district hue, budget and exact contact path. Pattern shape should supply interest without excessive health or indiscriminate density.

Snow implementation: three shell crowns establish the exploding-shell identity. During their settling beat, the hull throws ten/thirteen small mechanical emitters into a horizontal array. They charge on the way out, release the first suspended comb, visibly shift into the alternating lanes, release the answer, and return before the needle fans. Counts, velocities and the twelve-beat firing/rest score remain the Hard baseline. Emitters are built once with the boss; their shots reuse the already prepared SuspendedRound pool.

Origin audit: the other district bosses release from hull/weakpoint/claw ports or from a visible projectile delivery. Tunnel remains strictly bounce-only. Snow's screen-wide comb was the unsupported direct spawn found in this audit. Future scores should pass the same origin check and be judged in motion, including the following physics steps at dense accents.
