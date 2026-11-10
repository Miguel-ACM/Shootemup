# City switchyard: proposed cards

Code: `Assets/Scripts/Enemies/Bosses/CitySwitchyardBoss.Proposed.cs` (the only change to existing code is the `partial` keyword on the class). Each phase has **one concept** that runs for the whole phase. It develops through four movements of about fifteen seconds, so a 60-second recording shows every variation.

The engine still brakes into a signal box, changing platform between cycles or movements. All fire leaves the nose or the shoulders after a visible prime or charge.

Pressure is measured in rounds per second on Hard (current figure in brackets): Signal Gantry about 56 (60), Level Crossing about 61 (63), Grand Junction about 76 (72).

## Phase one: Signal Gantry

**Current:** Red Signal (parked traffic turned onto horizontal tracks) alternating with Express Service (trains from changing platforms).

Every 3 s a bloom of lamps from both shoulders brakes into a row along the visible top edge, with alternate lamps bright and dark. Each aspect falls as short trains (an arrow leading round cars) when it clears. Fourteen lamps leave about one unit of visible clearance between same-tone columns on Hard, and each row clears before the next parks. Expresses (gem plus kunai, three tracks) and ground signals that rebound off the walls keep the pilot moving between aspects.

The movements:
- **Clear:** bright falls in a V from the middle out, dark from the edges in.
- **Wave:** the aspects sweep across the row, alternating direction.
- **Double Block:** lamps pair up by tone into wider columns with a quicker change.
- **Flashing:** the dark aspect's trains launch at the pilot.

Alternative single concepts:
- *Points Change*: rails that freeze and kink together at every throw, folding into zigzags.
- *Timetable*: trains released on a fixed visible clock along the top edge.
- *Turnout*: one stream that divides into two diverging tracks at a moving point.

## Phase two: Level Crossing

**Current:** the phase-one loop with more cars and a longer hold.

Mirrored barrier arms leave the shoulders: lines of bright arrows at rising speeds, each tipped by a dark gem lamp. They lower from slightly above level, past straight down, and lift again, rebounding off the walls. Each movement opens with a prime at a new signal box.

The movements:
- **Barriers:** the arms move together.
- **Scissors:** one arm lowers while the other lifts.
- **Boom Gates:** the arms dwell fully lowered and fully raised, standing as walls.
- **Three Gates:** quicker sweeps, with a third arm from the nose.

Alternative single concepts:
- *Shunting*: fans that brake, back up toward the engine still facing out, then fall as level arcs.
- *Coupling*: half-trains from the shoulders that meet, pause and leave as one aimed train.
- *Hump Yard*: wagons lobbed over a crest that roll down into sorting lanes.

## Hard final: Grand Junction

Every cycle hangs a gantry and works the level crossing beneath it. The two step through their movements together, three cycles each, with an express after each cycle. The engine changes platform every cycle.

Alternatives: *Rush Hour Timetable* (gantry aspects released by a sweeping crossing arm), *Marshalling* (Points Change rails sorted by barrier arms), *Last Train* (one endless train that circles the yard walls while signals fall).
