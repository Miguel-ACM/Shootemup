Thunder boss and enemy editor

Tools > Shootemup > Enemy Editor lists all 43 enemies and bosses. Select an enemy, check the values to override, then Apply. Reset enemy restores code defaults; Undo supports saved edits. Drag the model to orbit and scroll to zoom. Playtest in Wave Watcher starts an isolated encounter; Phase 2 previews a boss's second phase. An active normal run disables the playtest button.

Saved overrides live in Assets/Resources/EnemyTuning.asset. The shipping asset is empty. Base health receives the usual run and miniboss modifiers after tuning. Direct shot speed/damage affect shots launched by the selected enemy, not secondary fragment children. Formation movement scales the whole phrase and firing accents together; the composer invalidates cached geometry when tuning changes.

Thunder defaults: 1800 base health, six permanent clockwise beams, 18 degrees per second, 1.35-second charge. Phase two places the front muzzle at arena centre, with the hull behind it. The six shoulder guns retain coordinated aimed and bowed volleys plus deliberate rests.

Validation: isolated Unity editor persistence/Undo and runtime factory checks passed. Formation geometry rebuilt at 1.25x and 0.6x movement. A full rotor revolution retained the same six beam objects beyond their old lifetime; peak 38 rounds. Owner destruction released every beam and its looping audio. Native model and fight captures were reviewed. No C# or shader compiler diagnostics in Temp/ThunderEditor.log.
