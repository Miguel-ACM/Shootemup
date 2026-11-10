# City switchyard: proposed cards

Code: `Assets/Scripts/Enemies/Bosses/CitySwitchyardBoss.Proposed.cs` (the only change to existing code is the `partial` keyword on the class). The engine still brakes into a signal box for each card and changes platform between cards. All fire leaves the nose or the two shoulders after a visible prime or charge. The rounds are arrows, circles, kunai and gems. Pressure matches the current loop to within about 10%, in rounds per second on Hard: Signal Gantry about 56 and Points Change about 58 (current phase one 60), Level Crossing about 62 and Shunting about 59 (phase two 63), Grand Junction about 76 (phase three 72).

## Phase one

**Current:** Red Signal (traffic climbs to the ceiling, parks, then turns inward on horizontal tracks), then Express Service (locked-heading trains from changing platforms, with signal fans that rebound off the walls).

- **Signal Gantry.** A bloom of lamps from both shoulders brakes into a row along the visible top edge, with alternate lamps in the two tones. The bright aspect drops first, in a V from the middle out. The dark aspect follows from the edges in, after a gap. Each lamp falls as a short train: an arrow leading slower round cars. With fourteen lamps, same-tone columns are 1.37 units apart, leaving about one unit of visible clearance on Hard (more on Normal and Easy). The dark aspect has cleared before the next row parks. The pilot moves from under a dark lamp to under a bright one in rhythm. Express headlights (gem plus kunai, three tracks) and ground signals that rebound off the walls punish standing still. It is prettier than Red Signal because the two tones read as two signal aspects, and the falling Vs make a clear dance.
- **Points Change.** Rails of dark cars run from the nose. At every throw, all cars on the rails brake at the same moment, glint, and resume kinked 26 degrees onto the new heading, while the nose swings the same way. The tracks fold into zigzags that sweep across the arena. Bright gem switch lamps are aimed from the shoulders at each throw. The engine changes box every third throw.

## Phase two

**Current:** the same loop as phase one, with the second bank held longer and more cars.

- **Level Crossing.** Mirrored barrier arms leave the shoulders: a line of bright arrows at rising speeds, tipped by a dark gem lamp. They sweep from slightly above horizontal down past straight down and lift again. The middle closes and reopens, and the side rounds rebound off the walls into a lattice. The tone is bright.
- **Shunting.** The nose pushes fans of wagons out at four speeds. They brake and back up toward the engine with their arrows still facing out (a reversed ramp). Then they stop with a glint and run down the yard as level arcs at one speed. Every other fan is centred on the pilot. The tone is dark, with bright leaders.

## Hard final

- **Grand Junction.** The gantry hangs over the yard while the crossing arms sweep beneath it. An express follows each cycle, and the engine changes platform every cycle.

## Further ideas (not implemented)
- *Turnout*: one stream that divides at a point into two diverging tracks, its fork moving along the stream.
- *Coupling*: two half-trains from the shoulders that meet under the engine, pause together and leave as one aimed train.
- *Night Freight*: very long, slow trains of dark rounds with bright windows, crossing the arena on a diagonal.
