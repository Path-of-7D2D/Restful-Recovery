# Restful Recovery

Restful Recovery is a 7 Days To Die V3.0 mod that lets you actually sit down.
Activate a chair or couch to rest: the camera switches to third person, your
character visibly sits on the furniture, and untreated sprains and breaks
recover at 4x natural speed while you stay seated.

## Features

- Adds a Rest action to upright, usable chairs and couches.
- Your character is visibly seated on the furniture while resting.
- The camera switches to third person while resting and returns to your
  previous camera preference when you get up.
- Untreated limb injuries (`buffLegSprained`, `buffLegBroken`,
  `buffArmSprained`, `buffArmBroken`) recover at 4x natural speed while
  resting.
- A green Mending status icon appears next to the injury while the rest
  bonus is actively speeding up recovery.
- The camera pulls back for a comfortable view of your character while
  seated.
- Splinted and cast limbs are intentionally untouched: treating an injury is
  still the better option, resting is for when you have no supplies.
- Rest cancels automatically when you move, jump, attack, take damage, die,
  enter a vehicle, open a menu, or the furniture is destroyed or removed.

## Eligible furniture

Chairs: bar stools, camping chairs, unfolded metal folding chairs, office
chairs, school seats, old chairs, wooden chairs, and wheelchairs.

Couches: modern couches, ugly couches, and leather/plaid sectionals
(including the matching single chairs).

The seat height adapts to each piece of furniture by measuring the placed
model, so the character sits on the cushion rather than at a fixed height.

Broken, folded, and fallen chairs, car seats, and beds are not restable.

## Installation

1. Download the latest `RestfulRecovery-*.zip` from the
   [GitHub Releases](https://github.com/Path-of-7D2D/Restful-Recovery/releases)
   page.
2. Extract the release zip.
3. Copy the `1A-RestfulRecovery` folder into your `Mods` folder:

```text
7 Days To Die/Mods/1A-RestfulRecovery/
```

The folder is installed correctly when this file exists:

```text
7 Days To Die/Mods/1A-RestfulRecovery/ModInfo.xml
```

## Multiplayer

Install the mod on the server and every connecting client. The seated pose is
networked through the vanilla vehicle pose stats, and the recovery bonus is a
server-side buff patch.

## Easy Anti-Cheat

This mod uses Harmony patches, so Easy Anti-Cheat must be off. The mod is
marked with `SkipWithAntiCheat`.

## Compatibility

This mod patches `buffs.xml` entries for the four untreated limb injury buffs
and adds Harmony patches on `Block` activation (`GetActivationText`,
`HasBlockActivationCommands`, `GetBlockActivationCommands`,
`OnBlockActivated`). It appends its Rest command instead of replacing existing
commands, so pickup-able chairs keep their vanilla Take action. It may
conflict with another mod that rewrites activation commands for the same
furniture blocks.

## License

This project is licensed under the [MIT License](LICENSE).
