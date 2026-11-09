# Hordes are the siege weapon

Players fight players, zombies fight walls. If players can shoot or blow through a wall, nobody bothers
leading a horde to it, so this build makes players nearly harmless to buildings while zombies keep full
damage.

These are the build's **default** gameplay config values, for every difficulty and for singleplayer. A server
can still change any of them in its `Config.txt` (see [Going back to vanilla](#going-back-to-vanilla)).

## What changes

- **Guns and melee barely scratch buildings.** Player gun damage (low and high caliber), melee damage and
  fists do 10% of normal damage to barricades and structures. Sentry guns use the same gun multipliers, so a
  sentry can't be used to get around them.
- **Explosions barely scratch buildings.** Grenades, rockets, charges, explosive traps and vehicle explosions
  do 10% of normal damage to barricades and structures. Damage to players, zombies and vehicles is unchanged.
- **No explosives in loot.** Charges, explosive throwables, explosive traps, explosive magazines (rockets,
  explosive rounds) and exploding consumeables never come from: item spawns, zombie drops, airdrops, horde
  beacons, vehicle wrecks or map objects. When a spawn table rolls one, it rolls again (up to 8 times, then
  spawns nothing).
- **No crafting explosives.** Blueprints that produce any of the items above are hidden from the crafting menu
  and refused by the server. Salvaging an explosive into parts still works.
- **Zombies are unchanged.** They still attack barricades and structures in their way at full damage, and
  boss specials (electric spark, stomp, fire breath) still hit buildings at full damage. Repairing is
  unchanged too.

## Config keys

| Key | This build | Vanilla | Meaning |
| --- | --- | --- | --- |
| `Barricades.Gun_Lowcal_Damage_Multiplier` | `0.1` | `1` | Damage to barricades from non-"Heavy Weapon" guns. |
| `Barricades.Gun_Highcal_Damage_Multiplier` | `0.1` | `1` | Damage to barricades from "Heavy Weapon" guns. |
| `Barricades.Melee_Damage_Multiplier` | `0.1` | `1` | Damage to barricades from melee weapons and fists. |
| `Barricades.Explosion_Damage_Multiplier` | `0.1` | `1` | **New.** Damage to barricades from explosions. Zombie attacks aren't affected. |
| `Structures.Gun_Lowcal_Damage_Multiplier` | `0.1` | `1` | Same as above, for structures. |
| `Structures.Gun_Highcal_Damage_Multiplier` | `0.1` | `1` | |
| `Structures.Melee_Damage_Multiplier` | `0.1` | `1` | |
| `Structures.Explosion_Damage_Multiplier` | `0.1` | `1` | **New.** |
| `Items.Explosive_Loot_Chance` | `0` | `1` | **New.** Chance [0 to 1] to keep an explosive rolled as loot. `0` removes them, a small value like `0.05` makes them rare. |
| `Items.Allow_Explosive_Crafting` | `false` | `true` | **New.** Whether blueprints that produce explosives can be crafted. |
| `Zombies.Can_Target_Barricades` | `true` | `true` | Unchanged. Zombies attack barricades in their way. |
| `Zombies.Can_Target_Structures` | `true` | `true` | Unchanged. Zombies attack structures in their way. |

`Armor_Lowtier_Multiplier` and `Armor_Hightier_Multiplier` still apply on top of these, for players and zombies
alike.

### Per building type

Each barricade or structure `.dat` can opt out of zombie attacks with `Can_Zombies_Target false` (default
`true`). Stuck zombies skip that building type when picking something to attack.

## What this doesn't cover

- **Vendors and quest rewards** still give explosives as written in the map's content. To make explosives
  "rare and expensive" rather than gone, raise their vendor prices or remove them from vendors in your map,
  and set `Explosive_Loot_Chance` above `0`.
- **Launchers found as loot** can come loaded with their default round. That one round still only does 10%
  damage to buildings.
- **Vehicle turrets** fire whatever ammo the vehicle is set up with. Their explosions are covered by
  `Explosion_Damage_Multiplier`.
- **Vehicles** have their own `Vehicles.*` multipliers, which this build leaves at vanilla. Players can still
  shoot vehicles normally.
- Explosives already in a saved world (inventories, storage) stay there.

## Going back to vanilla

Add the overrides you want to the server's `Config.txt`. For example, to restore vanilla explosives but keep
guns and melee weak against buildings:

```
Barricades
{
	Explosion_Damage_Multiplier 1
}
Structures
{
	Explosion_Damage_Multiplier 1
}
Items
{
	Explosive_Loot_Chance 1
	Allow_Explosive_Crafting true
}
```

`Allow_Explosive_Crafting` is sent to clients when they join so the crafting menu matches the server. That
changes the join message, so clients and server must both run this build.
