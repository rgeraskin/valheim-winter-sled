# Winter Sled

A cargo sled for Valheim. Hitch it, load it, haul it — just like the cart, except this one actually likes winter. Reindeer not included: you are the reindeer.

It rides on runners instead of wheels, so how it moves depends on what is under it:

- **Snow and ice**: it glides as if it was born for this. Pulling it feels like cheating.
- **Frozen ground** (cleared ground in the Mountains and the Deep North): the runners scrape and complain, but keep going.
- **Bare ground** in the warm biomes: the sled becomes a very heavy plough. You walk slowly, tire quickly and start to question your choices, more so with a full load.
- **Wood floors, roads and rock**: neither here nor there.

That makes it the cart's winter rival: the cart wins on grass, the sled wins in snow. To keep things fair, **wheels now sink in snow** — the cart had it too easy (you can turn that off if you feel sorry for it).

The vanilla cart gets a seat too, for a passenger (no steering or brakes: someone pulls, or it rolls) — fair is fair (`CartSeat` turns it off).

You can also **sit in it and ride down a snowy slope**: steer with A/D, brake with your heels on S, push off with W (or back with S, once stopped: out of a dead end), and jump to bail out. A gamepad works too: the left stick steers (the further you tilt it, the sharper the turn), a flick forward or back pushes off, and the hint shows the stick. A sled left standing stays put on a slope: its runners settle into the snow. A sled you let go of at full speed does not: it has places to be. Odin does not cover runaway sleds.

While you pull it, an icon shows what the runners are on — snow, deep snow, ice, frozen ground, wood, stone or ground — in case your legs haven't told you already.

## Sounds

The runners hiss on snow, sing on ice, scrape on frozen ground and grumble on dirt, and at speed the wind joins in. The volume is in the settings, for those who prefer to sled in dignified silence.

## Seasons

Plays along with [Seasons](https://thunderstore.io/c/valheim/p/shudnal/Seasons/) (shudnal) and [Seasonality](https://thunderstore.io/c/valheim/p/RustyMods/Seasonality/) (RustyMods): when winter reaches the Meadows, the sled is the first to celebrate. It glides through snowy meadows, forests and plains, and frozen lakes and sea become the fastest roads in Midgard (with Seasonality, turn on its "Water Freezes" option). Neither mod is required.

## Languages

All the game's languages: English, Russian, Ukrainian, Bulgarian, Macedonian, Serbian, Croatian, Polish, Czech, Slovak, German, Dutch, Swedish, Danish, Norwegian, Icelandic, Finnish, French, Italian, Spanish, Portuguese (European and Brazilian), Romanian, Hungarian, Lithuanian, Latvian, Turkish, Greek, Georgian, Hindi, Thai, Chinese (Simplified and Traditional), Japanese and Korean. The riding hint even shows your own key bindings.

## Building

Hammer → Misc, at a workbench: **20 Wood, 10 Fine wood, 4 Iron, 2 Wolf pelt**. It unlocks with your first iron and wolf pelt — about when the Mountains start trying to freeze you to death. With Seasons or Seasonality winter comes much sooner, and so does the sled: it costs **20 Wood, 8 Leather scraps, 10 Bronze nails** instead, like the cart. Both recipes can be changed in the settings.

## Looks

The body is the cart's, borrowed without asking. Underneath are curved runners with Norse curled tips, iron shoes, rope lashings and a wolf pelt on the seat (the wolf wasn't asked either). Everything uses the game's own textures, so the sled looks like it was always there.

## Configuration

`BepInEx/config/rgeraskin.WinterSled.cfg`. The settings go from what you are most likely to change to the fine tuning; the two Advanced sections are hidden in Configuration Manager until you tick "Advanced settings". Everything that changes how the sled moves is synced from the server (admin only), so all players simulate the same sled; sound and effects are each player's own.

| Section | Key | Default | |
|---|---|---|---|
| 1 - General | Requirements | `Wood:20,FineWood:10,Iron:4,WolfPelt:2` | build cost |
| 1 - General | SeasonalRequirements | `Wood:20,LeatherScraps:8,BronzeNails:10` | used with Seasons/Seasonality; empty: never |
| 1 - General | SlowCartsInSnow | true | wheeled vehicles slow you in snow |
| 1 - General | CartSnowPullSpeed / CartPowderPullSpeed | 0.8 / 0.6 | |
| 1 - General | CartSeat | true | a passenger seat in the vanilla cart |
| 2 - Sound and effects | SoundVolume / SnowSpray | 0.6 / true | per player |
| 2 - Sound and effects | SeatAnimation | `emote_sit` | `attach_chair` sits upright |
| 3 - Hauling | GroundPullSpeed / FrozenPullSpeed / FloorPullSpeed | 0.8 / 0.9 / 0.9 | on top of the runners' own drag |
| 3 - Hauling | SureFootedPulling | on | pulling a sled, you keep your footing on slippery ice (Deep North, shallow frozen water with Seasons), where the vanilla cart can't be pulled at all; off = vanilla rules |
| 3 - Hauling | LoadPenalty / FullLoadWeight | 0.1 / 400 | |
| 3 - Hauling | GroundStaminaDrain | 1.5 | run stamina multiplier on bare ground |
| 4 - Riding | MaxSpeed | 16 m/s | air drag grows steeply above it |
| 4 - Riding | SteerStrength / BrakeFriction | 1 / 0.2 | |
| 4 - Riding | PushImpulse / PushStamina / PushMaxSpeed | 1 m/s / 6 / 3 m/s | one push per press of W, or of S backwards once the sled has stopped |
| 4 - Riding | CrashMinSpeed / CrashMaxSpeed | 6 / 14 m/s | impact damage from / in full |
| 5 - Advanced: runner friction | SnowFriction / PowderFriction / IceFriction | 0.06 / 0.15 / 0.012 | along the runners |
| 5 - Advanced: runner friction | FrozenGroundFriction / WoodFriction / StoneFriction / GroundFriction | 0.25 / 0.22 / 0.35 / 0.55 | |
| 5 - Advanced: runner friction | RunnerGrip / IceGrip | 0.6 / 0.15 | across the runners |
| 6 - Advanced: physics | BaseMass | 40 | cargo adds 10% of its weight |
| 6 - Advanced: physics | AirResistance / Stability / StaticFrictionSpeed / SteerResponse | 0.12 / 1 / 1.5 m/s / 20 | Stability keeps the sled level with the ground (against tipping over on bumps); below StaticFrictionSpeed a sled nobody pulls or rides is held by static friction (not on ice) |
| 6 - Advanced: physics | CliffAngle / PowderDepth | 52° / 0.3 m | steeper cold ground is stone; Deep North snow deeper than this is powder |
| 6 - Advanced: physics | DebugLog | false | |

Changes apply right away, also to sleds already built (with [Configuration Manager](https://thunderstore.io/c/valheim/p/Azumatt/Official_BepInEx_ConfigurationManager/), F1, without leaving the game).

## Installation

Needs [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/). Install with a mod manager, or put `WinterSled.dll` into `BepInEx/plugins`. Everyone on a server needs the mod.

Tested on Valheim 1.0.20 with BepInExPack 5.4.2351, Jötunn 2.30.2, Seasons 1.10.5 and Seasonality 3.8.4, single player and with two players.

## Development

Building, tests, the in-game test harness and releasing: [DEVELOPMENT.md](DEVELOPMENT.md).

## Credits

- Inspired by [Shield Sledding](https://thunderstore.io/c/valheim/p/ModdedWolf/ShieldSledding/) (ModdedWolf), whose surface rules (ice by name, Deep North powder depth) this mod follows.
- [Valheim World Editor](https://github.com/Shuiei/valheim-world-editor) built the test courses: snowy slopes, a strip of bare frozen ground, a flat pad for coasting tests, all edited live in the test world. It helped a lot: every surface could be tested under the same, repeatable conditions. Its release script is also the base of this mod's publishing to the mod sites.

## Made with AI

This mod — code, 3D model, sounds, translations, tests and this text — was made entirely with AI, and tested in the game.

## License

MIT, see [LICENSE](LICENSE).
