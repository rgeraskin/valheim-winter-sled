# Winter Sled

A cargo sled for Valheim. Hitch it, load it, haul it — just like the cart, except this one actually likes winter. Reindeer not included: you are the reindeer. Unless you bring a deer from an animal mod: the sled is animal-friendly (see [Draft animals](#draft-animals)).

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

The runners hiss on snow, crunch through deep powder, rasp on ice, grind on frozen ground and stone, rumble over wooden floors and drag through dirt, and at speed the wind joins in: real recordings, not synthesized noise (credits below). The volume is in the settings, for those who prefer to sled in dignified silence.

## Seasons

Plays along with [Seasons](https://thunderstore.io/c/valheim/p/shudnal/Seasons/) (shudnal) and [Seasonality](https://thunderstore.io/c/valheim/p/RustyMods/Seasonality/) (RustyMods): when winter reaches the Meadows, the sled is the first to celebrate. It glides through snowy meadows, forests and plains, and frozen lakes and sea become the fastest roads in Midgard (with Seasonality, turn on its "Water Freezes" option). Neither mod is required.

## Draft animals

An animal-friendly sled. New: tame animals can pull it too, with a mod that hitches carts to them. You can ride in the sled while they do. Tested with:

- [Draft Animals](https://www.nexusmods.com/valheim/mods/4284) 1.1.3 (4gestr): put a harness on a tame boar, deer or wolf, press Shift+E at the sled's shafts, then drive it from the bench at the front or let it follow you. The sled gets the same hitch and driver's bench as the cart. A deer is as close to a reindeer as Midgard gets.
- [Beasts of Burden](https://thunderstore.io/c/valheim/p/AWLGaming/BeastsOfBurden/) 1.0.8 (AWL Gaming's maintained fork): use the sled's handles with a tame lox, wolf or boar next to them, and the closest one gets hitched. To take the handles yourself with an animal nearby, hold Shift (the alternate placement key) while pressing E.
- [OdinHorse](https://thunderstore.io/c/valheim/p/OdinPlus/OdinHorse/) 1.9.3: horses, and other tame animals, pull it.

When the animal slows down or stops, it holds the sled back (`AnimalHoldBack`), so the sled doesn't glide on into it on snow. On slopes the shafts stay at hip height above the ground (`LevelAnimalHitch`): the animal mods measure the hitch from the animal's feet, so behind a big animal the sled used to nose into the snow downhill and lift off uphill. The vanilla cart gets this too.

One difference from pulling it yourself: an animal keeps its own pace on any ground. The snow-versus-ground trade-off (the sled slow on bare ground, the cart slow in snow) applies to players only for now. If that makes the sled too good with animals, tell me (a comment or a GitHub issue) and I'll fix it.

Some quirks come from those mods or the game and happen with the vanilla cart too: with Beasts of Burden a hitch doesn't survive leaving the area. On Deep North ice the game takes an animal's footing away, as it does yours: pulling the sled, it keeps its footing like you do (`SureFootedPulling`); with the vanilla cart it slips and crawls, as in vanilla.

Use one cart-hitching mod at a time. Not compatible: [LetsGo](https://thunderstore.io/c/valheim/p/Hardwire99/LetsGo/) (Hardwire99) hitches carts to animals itself and its page asks to remove Beasts of Burden, so it fights the mods above over the same hitch; it isn't tested with the sled.

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
| 3 - Hauling | SureFootedPulling | on | pulling a sled, you (or an animal) keep your footing on slippery ice (Deep North, shallow frozen water with Seasons), where the vanilla cart can't be pulled at all; off = vanilla rules |
| 3 - Hauling | AnimalHoldBack | 20 | with a mod that hitches carts to tame animals (Draft Animals, Beasts of Burden, OdinHorse): how hard (m/s²) the animal holds the sled back when it slows or stops, so the sled doesn't glide on into it; 0 = off |
| 3 - Hauling | LevelAnimalHitch | true | with an animal mod: the shafts (sled and cart) meet the animal at hip height above the ground under them, so on slopes the vehicle doesn't nose into the ground downhill or lift off uphill; false = the animal mod's own hitch point |
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

Tested on Valheim 1.0.20 with BepInExPack 5.4.2351, Jötunn 2.30.2, Seasons 1.10.5 and Seasonality 3.8.4, single player and with two players; with Draft Animals 1.1.3, Beasts of Burden 1.0.8 and OdinHorse 1.9.3 in single player.

## Development

Building, tests, the in-game test harness and releasing: [DEVELOPMENT.md](DEVELOPMENT.md).

## Credits

- Inspired by [Shield Sledding](https://thunderstore.io/c/valheim/p/ModdedWolf/ShieldSledding/) (ModdedWolf), whose surface rules (ice by name, Deep North powder depth) this mod follows.
- Sounds, recorded by Freesound users: snow and powder by Nox_Sound ([Snowboard_Slide_Loop_Mono_02](https://freesound.org/s/612664/) and [_01](https://freesound.org/s/612665/), CC0), ice by alexarje ([Sledging (SoundAction 359)](https://freesound.org/s/863338/), [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)), frozen ground and stone by Percy Duke ([Ice Block - Sliding, Crunching, Scraping](https://freesound.org/s/420634/), [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/)), ground by brunoboselli ([Dragging Kayak](https://freesound.org/s/488335/), [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)), wood floors by papimre ([Wood Plank Pushed On Wood Floor Long](https://freesound.org/s/710621/), CC0) and wind by FunWithSound ([Car Strong Wind Noise](https://freesound.org/s/439242/), CC0). Each loop is rebuilt from short grains of its recording, so it keeps the recording's tone but stays even and has nothing in it you'd hear repeat ([details](https://github.com/rgeraskin/valheim-winter-sled/blob/master/sounds/SOURCES.md)).
- [Valheim World Editor](https://github.com/Shuiei/valheim-world-editor) built the test courses: snowy slopes, a strip of bare frozen ground, a flat pad for coasting tests, all edited live in the test world. It helped a lot: every surface could be tested under the same, repeatable conditions. Its release script is also the base of this mod's publishing to the mod sites.

## Made with AI

This mod — code, 3D model, translations, tests and this text — was made entirely with AI, and tested in the game. The sounds are real recordings by the people credited above, picked by ear and turned into loops by AI.

## License

MIT, see [LICENSE](LICENSE). The sound recordings keep their own licenses (CC0, CC BY), see Credits.
