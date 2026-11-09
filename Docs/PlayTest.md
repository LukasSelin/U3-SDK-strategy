# Play Test

One click from the Unity editor to standing in a singleplayer world with an enemy base in front of you, Lure Bots
parked beside you, and a rifle in your hands.

## Start

1. Steam running, Unturned installed (as for any editor play session).
2. In Unity: **Window > Unturned > Start Play Test** (Ctrl+Alt+P), or open **Window > Unturned > Play Test** and
   click **Start Play Test**.

The editor opens `Assets/GameStartup.unity`, enters play mode, skips the menus and loads the level. A second or so
after you spawn, the scenario is built and a green chat message confirms it.

The level defaults to `PEI`. Change it in the Play Test window (map folder name, e.g. `Washington`, `Russia`).

## What you get

- **An enemy base** 45 m straight ahead: 3x3 pine floors, walls all round, a doorway facing you, windows on the
  other sides, and a roof over the back row. It has no owner, so you cannot salvage it and zombies treat it like
  any other player-built structure.
- **Two Lure Bots**, one on each side of you, already pointed at the doorway. Get in, get out, and it arms and
  crawls toward the base. See [LureBot.md](LureBot.md).
- **A kit**: Timberwolf and Maplestrike with spare magazines, binoculars, medkits.
- **Zombies**: the site is picked inside a zombie navigation area, next to zombie spawns.
- Cheats on, time set to midday.

The site is chosen automatically and is the same every run on a given map: flat ground, no buildings or trees on
the base footprint or in the lane between you and the door, not underwater. If a map has nowhere that qualifies,
the scenario is built where you spawned instead.

## While playing

The Play Test window has buttons that act on the running game (click out of the game view first):

| Button | What it does |
| --- | --- |
| Reset Scenario | Rebuilds the base, respawns the two Lure Bots, puts you back at the start. |
| Move Scenario Here | Where you stand becomes the start; the base is rebuilt 45 m ahead of where you are looking. |
| Spawn Lure Bot | One more Lure Bot in front of you, pointing the way you are looking. |
| Give Kit | Another set of the kit items. |

Regular cheat commands work too, for example `/vehicle Lure_Bot` or `/day`.

## Things to know

- **Fresh save every time.** Play tests use their own world, `Builds/Shared/Worlds/PlayTest`, which is deleted at
  the start of each one. Your normal editor singleplayer saves are not touched.
- **Reset and Move wipe every structure in the play test world**, including anything you built during the test.
- The Lure Bot is a retuned Golfcart defined in `Builds/Shared/Sandbox/PlayTest/Vehicles/Lure_Bot/Lure_Bot.dat`.
  Edit the `Lure_*`, speed, health and battery values there and restart the play test to try different tuning. It
  is loaded in every editor session, not only play tests.
- The existing **Auto Load Level** editor setting is left alone; a play test request takes priority over it for
  that one run.

## Code

- `PlayTestSession` (`Assets/Runtime/Assembly-CSharp/Unturned/PlayTest`): starts singleplayer on the throwaway
  save and waits for the player to spawn. `MenuStartup` calls it when the editor asked for a play test, and
  `Provider.singleplayer` asks it which save folder to use.
- `PlayTestScenario`: site selection (`TryFindSite`), the base (`BuildBase`), bots, kit. To change the base
  layout, edit `BuildBase`; to change the kit, edit `KIT`.
- `PlayTestTool` (`Assets/Editor/Assembly-CSharp-Editor/Tools`): the editor window and menu items.
