# Lure Bot

A slow, cheap, fragile ground drone that makes noise. Drive it toward an enemy base, hop off, and it keeps
crawling forward on its own while dragging nearby zombies along behind it. Enemies can snipe it; when it dies
the horde scatters wherever it was.

Any vehicle becomes a Lure Bot by adding `Lure_Noise_Radius` to its `.dat`. Everything else (model, health,
speed, cost) is regular vehicle content.

## How it plays

1. **Drive it** like any small vehicle to aim it.
2. **Hop off** (the driver's seat becomes empty). The bot *arms*:
   - It crawls straight ahead along the heading you left it on, at `Lure_Autopilot_Speed`. It does not steer.
   - Every `Lure_Noise_Interval` seconds it pulses noise. Zombies within `Lure_Noise_Radius` drop what they're
     doing and follow it, trailing in a loose pack a few meters behind so they don't block it.
   - It drains fuel (`Fuel_Burn_Rate`) or, if `Battery_Powered`, battery (`Battery_Burn_Rate`).
3. **It stops luring** when it dies, drowns, runs out of fuel/battery, or someone takes the driver's seat.
   Every follower then **scatters** to a random point within `Lure_Scatter_Radius` of where the bot was, and
   settles there instead of walking back home.

Other rules:

- **Players come first.** A zombie already chasing a player ignores the bot. A lured zombie that notices a player
  (sight or sound, same as always) switches to that player. So a horde led into a base goes for the people in it.
- **Followers don't attack the bot.** Stuck zombies normally smash nearby vehicles; they skip Lure Bots. They will
  still chew through barricades and structures in their way. Zombies chasing a player who is *driving* the bot
  will attack it as usual.
- **Bosses ignore lures.**
- **Leash:** a follower more than 64 m from the bot gives up and goes home, as with any other target. That's why
  `Lure_Noise_Radius` is capped at 64.
- Zombies only exist inside the map's zombie navigation areas. A bot that drives out of one can't pull zombies
  beyond its edge.
- Armed state lives only on the server and is not saved: after a restart, bots wait for someone to drive them again.

## `.dat` keys

| Key | Default | Meaning |
| --- | --- | --- |
| `Lure_Noise_Radius` | `0` (off) | Meters, max 64. Greater than 0 makes the vehicle a Lure Bot. |
| `Lure_Noise_Interval` | `1.5` | Seconds between noise pulses (min 0.1). |
| `Lure_Autopilot_Speed` | `3` | Crawl speed in m/s while unmanned. `0` makes a stationary beacon. |
| `Lure_Scatter_Radius` | `12` | Meters. How far followers spread out when the bot dies. |
| `Lure_Pulse_Effect` | none | Optional effect GUID (for example a beep or alarm sound) played at every pulse. |

The autopilot needs wheel colliders on the model the server loads to know it's on the ground, so use a wheeled
vehicle. A legacy `Clip` server prefab without wheel colliders won't move on a dedicated server. It only pushes
while at least one wheel is grounded and the bot is upright.

Power: fuel vehicles burn `Fuel_Burn_Rate` per second and need a charged battery for ignition, like normal cars.
`Battery_Powered` bots burn `Battery_Burn_Rate`. `Stamina_Powered` bots never run out.

## Example

Copy a small wheeled vehicle's folder into your mod, give it a new `GUID` and `ID`, then tune it to be slow,
cheap and fragile and add the lure keys:

```
Engine Car
Speed_Min -2
Speed_Max 5

Health_Min 60
Health_Max 60
Health 60

Battery_Powered
BatteryMode_Driving Burn
Battery_Burn_Rate 40

Lure_Noise_Radius 48
Lure_Noise_Interval 1.5
Lure_Autopilot_Speed 3
Lure_Scatter_Radius 14
Lure_Pulse_Effect <effect GUID>
```

A low health value is what makes it "snipeable": a few rifle hits kill it. "Cheap" comes from its crafting
blueprint or spawn tables, which work as for any other vehicle.

## Code

- `VehicleAsset`: parses the `Lure_*` keys (`isLureBot`, `lureNoiseRadius`, ...).
- `InteractableVehicle`: arms and disarms on driver seat changes (`SetLureArmed`), and on the server runs the
  noise pulse and power drain (`UpdateLureBot`) and the autopilot push (`ApplyLureAutopilot`, in `FixedUpdate`).
  `IsLureActive` is the single "is it luring right now" check.
- `Zombie`: `EHuntType.LURE`, `alertLure` (called by the pulse), `GetLureFollowPosition`, and `scatterFromLure`
  (called from `tick` once `IsLureActive` goes false or the vehicle is destroyed). `findTargetWhileStuck` skips
  Lure Bots.
