# Winter Sled — development notes

## Layout

- `WinterSled/` — the plugin (BepInEx + Jötunn, net462).
  - `SledBuilder` clones the vanilla `Cart`, removes the wheels, lowers the body by 0.10 m, adds the undercarriage mesh, runner capsule colliders, the seat and `SledController`.
  - `SledController` — runner physics on the ZDO owner: frictionless runner material plus its own Coulomb friction, separately along (surface-dependent) and across (grip) each runner; static friction (a high-friction material: the runners settle into the snow) when nobody pulls or rides, speed < `StaticFrictionSpeed` and not on ice; air drag with a soft cap at `MaxSpeed`; rider steering/brake/push; snow spray, powder trail, sound.
  - `SurfaceProbe` — what is under a runner: `ClassifyGround` (pure, unit-tested) turns biome, the season's look, terrain paint and slope into Snow / Powder (Deep North, deep) / FrozenGround (cleared ground in the cold) / Stone (paved or cliff in the cold) / Ground (warm); Ice by collider name or a solid Water-layer collider (seasonal frozen water); Wood/Stone pieces by WearNTear material (snowy pieces count as snow).
  - `SledSeat` — `IDoodadController` like a ship's rudder, so WASD steers instead of standing up.
  - `SledHaulEffect` — status effect on the puller: speed factor per surface, run stamina drain on ground, icon text shows the surface. `CartSnowDrag`/`CartSnowEffect` slow wheeled vehicles in snow.
  - `SledAudio` — runner loops per surface (snow, powder, ice, frozen ground and stone, ground, wood) plus wind for a fast rider: Freesound recordings put together by `sounds/build_sounds.py` (`mise run sounds`; sources, licenses and the stretches used in `sounds/SOURCES.md`), embedded as 16-bit mono WAVs from `WinterSled/Resources/sounds/` and played as plain loops. What was tried: a 6 s loop cut from a recording (its pattern was easy to hear), random 1.2–2.5 s stretches crossfaded at play time (the sound changed by itself every few seconds); now each loop is an 8 s texture of short, leveled grains from random places in the recording (steady, nothing to recognize). Per loop the script can set grain size, speed, filters, a click filter (`crest`) and the seed; a voice or squeak is best left out by the stretches chosen (`sounds/SOURCES.md`), not notched: on the ice recording the notched "squeak" was the runners' own tone.
  - `SledConfig` / `LiveSettings` — every setting (server-synced, admin only, except the sound and effects settings and `DebugLog`, which are each player's own) and applying changes to built sleds live.
  - `SeasonsCompat` / `SeasonalityCompat` — soft dependencies, by reflection.
  - `Translations` — all the game's languages.
- `WinterSled.Tests/` — xunit (net10.0): surface rules, friction order, hauling, recipes, seasons, translations, sounds (embedded, leveled, seamless, steady; one for every surface). `mise run test`.
- `model/build_model.py` — generates `WinterSled/Resources/sled_undercarriage.obj` (stdlib only, checks winding).
- `model/render_preview.py` — Blender previews with the game's textures (`mise run preview`, needs `.extract/`).
- `tools/DevHarness/` — dev-only plugin, never shipped: creates a local test character/world and runs commands from `$WINTERSLED_HARNESS_DIR/cmd.txt`.
- `package/icon.png` (256 px, Thunderstore) and `package/logo.png` (1024 px, Nexus Mods/Hexium): the in-game screenshot of the sled pulled across Seasonality's frozen sea, hover text inpainted out, title in Cinzel (OFL).
- `package/nexus-header.jpg` (1920×480) and `package/gallery/*.jpg`: the Nexus Mods header and screenshots, shot with the harness (`hud off`, `equip`, `shot`).
- `package/nexus-description.bbcode` — the Nexus Mods page description (BBCode, in Nexus' sections); keep it in step with README.md. Each release sends it to the mod page (with the manifest's description as the summary).
- `tools/package.sh` — the Thunderstore zip `dist/WinterSled-<version>.zip` (`mise run package`); checks that `package/manifest.json` and `Plugin.Version` agree.
- `tools/ci/` — `fetch-deps.sh` puts together what CI compiles against (the dedicated server's Managed assemblies via anonymous steamcmd, BepInExPack and Jötunn from Thunderstore); versions in `versions.env`.
- `tools/release/` — `publish.cs` (adapted from Valheim World Editor's, MIT) publishes to Thunderstore, Hexium and Nexus Mods; settings in `sites.json`.

## CI and releases

- `.github/workflows/ci.yml` — on push to master and PRs: fetch deps (cached), build, unit tests, check that `model/build_model.py` reproduces the committed mesh, package (artifact `package`).
- `.github/workflows/release.yml` — on a tag `v<version>`: the CI build, release notes from CHANGELOG.md, a GitHub release with the zip, then Thunderstore, Hexium and Nexus Mods. Run by hand (Actions → Release → Run workflow) it only checks each site (`--dry-run`).
- Secrets: `THUNDERSTORE_TOKEN` (service account of the Thunderstore team in `sites.json`), `HEXIUM_TOKEN`, `NEXUS_API_KEY`.
- Before the first release: create the team `rgeraskin` on Thunderstore and Hexium (or change `team` in `sites.json`). Nexus Mods has no API to create a mod: upload 0.1.0 by hand, then put the mod's id and the file's id into `sites.json` (`modId`, `modFileId`); until then the Nexus step is skipped. Hexium publishes hidden: unhide the version on hexium.gg.
- To release: bump the version in `package/manifest.json` and `Plugin.cs`, add a `## <version>` section to CHANGELOG.md, commit, `git tag v<version> && git push --tags`.
- Locally: `tools/ci/fetch-deps.sh <dir>` then `dotnet build WinterSled/WinterSled.csproj -c Release -p:ValheimDir=<dir>` builds the way CI does; `dotnet run tools/release/publish.cs -- thunderstore dist --dry-run` checks a site.

## How the physics works

The runners carry no friction of their own; the mod applies it, separately along and across each runner. Along the runner it depends on the surface (almost nothing on snow, a lot on dirt); across it the runners bite, so the sled holds its line on a slope and carves when it turns instead of sliding like a puck. Air drag grows steeply above `MaxSpeed`. Pulling speed is set by a status effect, because Valheim's character controller would otherwise ignore what it drags.

## Textures (all borrowed from the game at runtime)

| part | material | texture |
|---|---|---|
| body | (cart's own) | Cart_d |
| runners, stanchions, crossbars | DarkWood_mat | DarkWoodBeams_d (dark half; crossbars light half) |
| runner shoes | metalwall | metalwall (32 px tiling iron) |
| lashings | cart_rope | rope_d |
| seat pelt | rug_wolf (Custom/Rug, alpha-tested) | rug_wolf (whole pelt, alpha gives the outline) |

## Testing on a Windows machine

macOS needs Rosetta for BepInEx and the Steam pipe there is flaky; testing moved to a Windows PC reached over SSH. Its `user@host` goes into `WINTERSLED_REMOTE`, kept out of git in `mise.local.toml`:

```toml
[env]
WINTERSLED_REMOTE = "user@windows-pc.local"
```

- Isolated profile `D:\WinterSledTest` (BepInEx core, Jötunn, WinterSled, harness). The game folder and the user's Gale/r2modman profiles are untouched: `D:\WinterSledTest\run.cmd` starts `valheim.exe` with `--doorstop-target-assembly D:\WinterSledTest\BepInEx\core\BepInEx.Preloader.dll`.
- The scheduled task `WinterSledTest` (`/IT`) runs it in the interactive session (SSH is session 0).
- Harness log/screenshots: `D:\WinterSledTest\harness\{log.txt,shots\}`; plugin log: `D:\WinterSledTest\BepInEx\LogOutput.log`.
- Test character `SledTester` and world `SledTest` (seed `SledTest1`) are local saves; the harness skips the Valkyrie intro and the `god` command sets passive mobs and no raids.

Scripts in `tools/harness/`: `remote-ps.sh` (PowerShell over SSH, script on stdin), `remote-cmd.sh` (send harness commands, wait for `done`), `remote-redeploy.sh [Key=value...]` (copy DLLs, set config keys, restart, wait for the world).

Harness commands: `waitplayer`, `god [time]`, `hud on|off` (also hides messages and name plates), `tp x z [yaw]`, `findbiome <Biome> [radius] [minSlope] [maxSlope]`, `tpwater x z [yaw]` (over water, onto whatever is there), `equip <item...>` (dress the test character), `loadvis` (where the cargo crates sit), `body [interp|none]` (rigidbody interpolation), `chairs` (seats on the vehicle), `colliders <r>` (colliders around it, with layers), `burst <name> <frames> [yaw pitch dist height]` (consecutive frames, camera locked to the sled) or `burst <name> <frames> game [interval]` (the player's own camera), `god off` (mortal again: real stamina and damage), `clear <r>`, `look <yaw>`, `faceslope up|down`, `spawn [dist] [prefab] [keep]`, `sledpos <behind>`, `attach`, `detach`, `sit`, `move fwd|back|left|right|fl|fr|stop [run]`, `input x z`, `jump`, `release`, `load <item> <n>`, `state`, `record <sec> [step]`, `shot <name> [sled|player|game] [yaw] [pitch] [dist] [height]`, `anims`, `materials`, `iceitems` (items with the ice shoes / ice skates flag), `findslip [r] [go]` (slippery colliders near the player: no friction, on the layers the game slips on; `go` stands on the nearest), `loc <text>` (what the game's localization makes of a text, `$KEY_` bindings included), `hoe [piece]` (a hoe ground edit at the player's feet, `path_v2` by default: a zone's ground needs one in-game edit before a world editor can save ground edits there), `audio [sec] [step]` (the sled's loops: volume, playing, pitch), `audiodiag` (each AudioSource, its clip and the listener), `audiopitch <p|off>` (every loop at one pitch), `loudness` (every AudioSource on the sled or cart, wheels too), `recaudio <sec> <name>` (what the game plays, into `<harness dir>/<name>.wav`), `finddepth <min> <max> [r]` (frozen-water test spots: water this deep, nearest first; shallow water belongs to the land biome, so `findbiome Ocean` only finds deep sea), `quit`. `state`/`record` log what the player stands on: `feet=<collider>/<friction> slip=<m_slipping> run= slipSpeed=`.

## Measured (Valheim 1.0.15, Windows)

Test course (built live with the World Editor in world SledTest, x -1628..-1604): z -216..-124 smooth snow at ~6°, a dirt strip across it at z -164..-156, z -124..-76 snow at ~22°, then Black Forest ground to z -40.

| | value |
|---|---|
| meadow, no sled: jog / sprint | 3.9 / 7.0 m/s |
| vanilla cart on meadow | 3.5 m/s |
| sled on ground: jog / sprint / sprint stamina | 2.25 / ~4.1 m/s / ~12 per s (vs ~7) |
| sled on snow: jog | 4.0 m/s |
| ride, 6° snow, from rest | 8.2 m/s after 7 s, straight (no sideways drift) |
| ride, 22° snow | tops out at ~17 m/s (MaxSpeed 16 soft cap) |
| ride into the dirt strip / onto ground | ~8 → 0 m/s in ~1.5 s |
| brake (S, BrakeFriction 0.2) | ~2.8 m/s² |
| steering (A/D) | ~45° heading change in 2 s at 6 m/s |
| left standing on a slope, unattended | stays put; let go at speed it keeps sliding |

Fixed along the way: PhysX per-point contact impulses double-counted the normal force (now weight based); a resting (sleeping) body ignored the rider; steering was too weak (torque gain 5 → 20); brake too harsh (0.45 → 0.2); the rider stayed inside the bed after jumping off (first stepped off beside the sled, which looked like sitting down beside it; now stands up on the deck, carried along with the sled, and steps or jumps off); the pelt was hidden under the uneven deck planks (raised); the build icon showed the cart's cargo crates.

Note: the test character takes Freezing and teleport-fall damage (harness artefacts); the harness `god` command keeps its health full. The mod itself never heals.

## Round 2 (Valheim 1.0.20)

- Valheim's gravity is about 20 m/s², so the runners' friction decelerates at μ·20: snow (0.06) 1.2 m/s², ice (0.012) 0.24 m/s². Coasting from 3 m/s: snow ~2.3 s / 3.5 m, ice ~10 s / 13.5 m (flat test pad, world SledTest x -1598..-1588, z -216..-176, built with the editor).
- Cart vs sled pulled at a jog: bare ground 3.5 vs 2.25 m/s; snow 3.1 (wheels-in-snow 0.8) vs 4.2 m/s; ice (Seasons frozen sea) — sled 3.7 m/s (the player slips on ice); wood floor — sled 3.3 m/s.
- Deep North: Snow/Powder detected, snow_walk trail objects appear behind the runners.
- Seasons 1.10.5 winter: meadows count as snow; frozen sea is ice (needs winter day 6-9, `Day override`). Seasonality 3.8.4 winter (`Water Freezes = On`, off by default): meadows count as snow; its frozen sea holds the player and the sled: surface Ice, pulled at 3.96 m/s, still 3.4 m/s a second after letting go. (An earlier "player swam" was the harness: `tp` puts you on the sea bed, which `GetSolidHeight` finds through the Water layer; `tpwater` drops you onto the surface.)
- Frozen ground (the dirt strip in the mountains): pulled at 3.0-3.3 m/s at a jog, between snow and ground.
- Crashes: the vanilla `ImpactEffect` takes the whole relative speed, so a sled sliding fast over snow "crashed" into the ground at every bump (damage numbers and wood chips on a downhill run). `CrashOnlyHeadOn` counts only the speed into what it hits: a full run down the 22° slope at up to 16.8 m/s leaves the sled untouched; flying off the cliff at the bottom still damages it.
- Sounds: the loop crossfades with the surface, wind above ~6 m/s; the cart's wheel creak is muted.
- Seasonal recipe switches automatically (log: `Sled recipe (seasons mod found)`).
- Multiplayer, host on the Windows PC + client on the Mac (another Steam account), Steam P2P join by SteamID: passenger rides while the host pulls (host owns the physics); a rider of an unpulled sled takes ownership and pushes/steers; getting off frees the seat on both sides.

Two machines: `WINTERSLED_OPEN=1` in `D:\WinterSledTest\run.cmd` opens the host's world; on the Mac `WINTERSLED_JOIN=steam:<host SteamID64> WINTERSLED_CHAR=SledRider mise run game`, commands through `tools/harness/local-cmd.sh`. Harness extras: `nearest`, `goto <player>`, `clearsleds [r]`, `cfg <Key> <value>`, `recipe`, `langs`, `floor`, `count`. Seasons/Seasonality for tests live in `D:\WinterSledTest\disabled` (they can't both be loaded; a client without Seasonality is kicked by its ServerSync).

## Round 3 (0.1.2, Valheim 1.0.20)

Downhill course in a second world (the author's own game, Seasons winter): a 230 m run from a mountain top at about 28° onto the frozen sea. Logged with `nearest` + `record` at 0.1 s while the player rode.

- Air drag was five times too strong. On the flat ice a coasting sled decelerated at 0.24 + 0.0145·v² m/s² (1.7 m/s² at 10 m/s): the ice friction as intended plus an `AirDrag` of 0.55 N/(m/s)², which was meant for a sled with a rider but acts on the sled's own body (40 kg; the rider isn't part of it). Now `AirResistance` 0.12 (a new key, so saved configs don't keep 0.55): 1.05 m/s² at 14 m/s, and the speed cap above `MaxSpeed` is twice as steep (top speed on the 28° slope 16.7-17.7 m/s instead of 15-16).
- Landing on the ice from a 29° slope with a sharp kink cost 15.3 → 10.4 m/s in one 0.1 s step. With a 30 m radius run-out onto the ice the sled went in at 14.8 m/s.
- Bumps: a crest throws the sled when its radius is under v²/(g·cos θ), about 14 m at 16 m/s on 28°. The first course had 0.7 m bumps 12 m long (radius ~10 m) and the sled rolled over on small bumps. With the centre of mass down at the runners (0.32 → 0.12 m) and `Stability` 1, on 1 m waves 30 m long (crest radius 21-25 m) the pitch swung 20-37°, the roll stayed within ±9° (one -22° that righted itself), no rollover, no damage.
- Turning on ice: A/D turns the sled into a sideways skid (`IceGrip` 0.15 across the runners against `IceFriction` 0.012 along), which stops it far sooner than going straight: 66 m from the shore instead of ~120 m.

- Kick back (S on a stopped sled): on the flat snow pad, sled pushed with W against a stone wall until W no longer moved it; each press of S then gave -0.6 to -0.7 m/s backwards (about 0.4 m on snow) for 6 stamina, and holding S after the kick doesn't brake it (it slows at the snow's 1.2 m/s², not the heels' ~5).

- Gamepad (the author's, through Steam, Xbox glyphs): sitting down with the gamepad shows `[LS◂/LS▸] steer [LS▾] brake / back [LS▴] push off [B] get off` with the game's stick glyphs (`burst` screenshots); steering, push-offs, the kick back and the brake with the stick felt right.

Pulling on ice: whether the legs keep control decides it, not the sled (the runners' friction on ice is 0.012 everywhere). The game's slippery ice (`Character.ApplySlippery`: a ground collider with no friction on the `static_solid`/`Default` layers) makes the next velocity mostly the body's current one, so the harness (a 5000/1000 spring-damper) drags the puller back. The test masks with the layer's number, not its bit, so a `Water`-layer collider never qualifies. Running forward from a standstill, test profile, `god` on; speeds after 4-5 s:

| Ice | `feet` | `slip` | No sled | Sled, `SureFootedPulling` off | Sled, on |
|---|---|---|---|---|---|
| Deep North (`IceShore_1`, material `SlipperyIce`, `static_solid`) | `model/0` | yes, loses control | 7.0 (0 → 7 in 2 s) | 2.6, still climbing (0.3 → 2.6 in 5 s) | 6.1 at once |
| Seasons 1.10.5, water 1.4-3 m deep (Meadows) | `IceSurface/0.1` | yes, vanilla slipping (Seasons' `Enable vanilla slipping on shallow frozen water`, up to 4 m, outside the Ocean biome) | 7.0 | 2.5, still climbing | 5.8 |
| Seasons, deep sea (Ocean) | `IceSurface/0.1` | no (Seasons' own sliding, only on stopping) | 5.75 | 5.7 | 5.7 |
| Seasonality 3.8.4 (`Water Freezes`), 1.4-3 m deep | `WaterSurface/0` | yes, forced by Seasonality, but keeps control (`Water` layer) | 6.6-7.4 | 7.0 | 7.0 |
| Seasonality, deep sea | `WaterSurface/0` | same | 7.0 | 7.0 | 7.0 |

The vanilla cart on the same Deep North ice doesn't move at all: running for 10 s, player and cart stay at 0.0-0.1 m/s (the mod adds nothing to a cart on ice; its snow drag is a speed effect that shows only in snow).

In the author's game the same showed as "pulling is heavy near the shore, the ice gets slippery farther out": a bay dug 3 m deep (Seasons shallow ice) pulled at 1.5-2.4 m/s, the open sea at 7.2 m/s. `SureFootedPulling` sets the game's ice shoes flag (`m_iceShoes`) for the puller's walking step only where the legs would lose control (`IceSurface` for Seasons, which then decides by depth; otherwise the game's own test), so Seasonality's and Seasons' deep ice keep their glide (a blanket flag cost 7.0 → 6.05 m/s there). The game has an `IceShoes` item (utility slot) but it is unfinished in 1.0.20: no name, no description, the Megingjord icon, not craftable.


## Round 4: draft animals (0.2.0, Valheim 1.0.20)

Tested on the SledTest course with the shared harness (`tools/SledHarness`, a companion of `../valheim-dev-harness`): `sled`, `beast <prefab>`, `saddle`, `useon <item>`, `hitch [alt]`, `pull`, `lead`/`leadto`, `ride`, `mount`, `bench`, `load`, `parts`, `film` (the harness's, with the sled, its beast or the middle between them as a camera target), `sledshot`; from the harness: `tidy`, `probe`, `hud off` (also hides name plates). Every finding was checked against the vanilla cart in the same scene, to tell the sled's problems from the animal mods'.

- Beasts of Burden 1.0.7 (AWL Gaming), OdinHorse 1.9.3: both patch any `Vagon`, so they hitch the sled with no help. Lox 7 m/s on 6° snow, 8-10 on 22°; wolf 8-9; horse 11-12 with a rider; a ridden, saddled lox pulls it too. Theirs, not ours (the cart does the same): the hitch offset of an animal stays for the next player, the high rump hitch pitches a vehicle nose-down on steep slopes (cart 35°, sled 41-43° on 22°), a Beasts of Burden hitch is lost when the area unloads. Beasts of Burden 1.0.8 (2026-10-29, our issues AWL-Gaming/BeastsOfBurden#4 and #5) fixed the offset leak and added Shift+Use to take the handles yourself next to a tame; e2e on the sled: after a lox or wolf unhitches the sled is back to offset (0, 0.8, 0) and detach distance 1.0, Shift+Use at the handles gives them to the player with a wolf 1.3 m away, plain Use still hitches the animal, a lox pulls at 7 m/s and stops the sled within a second. Shift+Use needs the player at the handles (vanilla's distance check): the harness `pull alt` stands there, `hitch alt` stands 1.2 m aside and gets nothing.
- The sled's own problem, fixed: when the animal stopped on snow, the gliding sled shoved it; the hitch on its rump swung round and the sled ended inside it (wolf: sled 1.0 m behind, cart 3.2; lox on 22°: sled level with it, cart 4.5 behind). With `AnimalHoldBack` the sled stops 3.2 m and 4.6 m behind, like the cart; pulling speed matches the animal's.
- Draft Animals 1.1.3: the sled gets its hitch and bench (`DraftAnimalsCompat.FitSled`). Wolf, boar and deer hitched with Shift+E, led (3.5 m/s walking) and driven from the bench (3.5 walk, 7.5 run, S stops, held on 22°); unhitching hands the sled back to players; the hitch survives a restart. The vanilla cart works as before.
- Without animals nothing changed: `regression.txt` (pull on snow and frozen dirt, standing on a slope, riding 6° and 22°, the brake) gives the same numbers on 0.1.2 and the new build, within run-to-run noise.
- Hitch levelling (`LevelAnimalHitch`): the animal mods put the hitch a fixed height above the animal's feet, some way behind it (Beasts of Burden and OdinHorse: one body radius, a lox 2.6 m; Draft Animals: 0.85-1.1 m), so on 22° it is 1.05 m (lox) to 0.35 m (boar) off the ground under it: in the snow downhill (pitch 41-43° on 22°; the cart 35°), in the air uphill. `AnimalHitch` (on every `Vagon`: the sled and the vanilla cart) raycasts the ground under the mod's point before each physics step and keeps the mod's height above it (at most 1.5 m from the mod's point). In game, 22° slope (sled pitch, the slope is 22): Beasts of Burden lox uphill -20..-22 with it, -25..-31 and the front lifting off without (cart without: -38..-52); downhill 14-22 with, 23-29 without (cart 11-20 with, 28-32 without, same spots). Draft Animals deer (hitch 1.1 m behind): uphill -22 with, -26 without; downhill 22 either way. OdinHorse horse uphill -20..-22 with, -27..-29 without (downhill the horse galloped and stopped, no clean numbers). Uphill on 22° the cart unhitches from a lox within seconds with it and without (Beasts of Burden's own check), the sled stays hitched.
- Deep North (Draft Animals wolf): on snow it pulls the sled at its 3.5 m/s walk; on Deep North ice (`IceShore`, friction 0) the game takes its footing away (`m_slipping`), as a player's, and it pulls at 2 m/s falling to 1 m/s; with the vanilla cart the same (0.75 m/s). `SureFootedPulling` now covers an animal pulling a sled too (the sled's owner records its puller, `SledController.IsHauledBy`; the patch gives it ice shoes for the walking step): 3.0-3.6 m/s on that ice with it, 2.6 falling to 1.4 without; the player pulling the same sled there is unchanged.
- Manual demo (a player on the test machine, guided over Telegram; all three mods): found two bugs, both fixed during it. The hitch levelling lifted an animal that was off the ground further up, to the 1.5 m limit (a wolf hanging in the air): it measured the ground under the hitch against the animal's feet; now against the ground under the animal (`LevelHeight(feet, height, groundAtFeet, groundAtHitch, max)`). On a 22° climb the sled rolled back down at 2 m/s and dragged a stopped wolf along: the hold-back only braked a sled catching up; `HoldUp` now gives back speed to a sled rolling backwards behind its animal Afterwards the creep it left (0.16 m/s on 22°) turned out to be what gravity adds within each physics step after the hold-up cancelled the speed; `HoldUp` now also cancels the slope's pull for the step while the sled stands or rolls back (0.0125 m/s: 0.2 m in 16 s; a reload moved the team 0.3 m instead of 2.6). Static friction now goes by a real joint (vanilla or any mod's) rather than Draft Animals' hitched flag, which can come seconds before its joint after a load. The team that went over the cliff in the demo: a lox parked hitched at the bottom of the 22° slope moves 5-6 m a minute on its own (an idle tame's steps, vanilla AI; up or down), the sled swinging round its hitch; with the creep it drifted down to the cliff edge 20 m below. Park teams away from edges. Also seen: Draft Animals' bench drops you 1.1 m to the right (its `m_detachOffset`, by design); Beasts of Burden adds no hover hint for animals; OdinHorse rides with W held and turns with the right mouse button.
- Not tested: animals with two players (the hitched state of Draft Animals is read from the ZDO on every peer; the other mods' joints exist on the owner only).
