# Winter Sled — development notes

## Layout

- `WinterSled/` — the plugin (BepInEx + Jötunn, net462).
  - `SledBuilder` clones the vanilla `Cart`, removes the wheels, lowers the body by 0.10 m, adds the undercarriage mesh, runner capsule colliders, the seat and `SledController`.
  - `SledController` — runner physics on the ZDO owner: frictionless runner material plus its own Coulomb friction, separately along (surface-dependent) and across (grip) each runner; static friction (a high-friction material: the runners settle into the snow) when nobody pulls or rides, speed < `StaticFrictionSpeed` and not on ice; air drag with a soft cap at `MaxSpeed`; rider steering/brake/push; snow spray, powder trail, sound.
  - `SurfaceProbe` — what is under a runner: `ClassifyGround` (pure, unit-tested) turns biome, the season's look, terrain paint and slope into Snow / Powder (Deep North, deep) / FrozenGround (cleared ground in the cold) / Stone (paved or cliff in the cold) / Ground (warm); Ice by collider name or a solid Water-layer collider (seasonal frozen water); Wood/Stone pieces by WearNTear material (snowy pieces count as snow).
  - `SledSeat` — `IDoodadController` like a ship's rudder, so WASD steers instead of standing up.
  - `SledHaulEffect` — status effect on the puller: speed factor per surface, run stamina drain on ground, icon text shows the surface. `CartSnowDrag`/`CartSnowEffect` slow wheeled vehicles in snow.
  - `SledAudio` — runner loops per surface plus wind, synthesized at startup (`Synthesize`, unit-tested).
  - `SledConfig` / `LiveSettings` — every setting (server-synced) and applying changes to built sleds live.
  - `SeasonsCompat` / `SeasonalityCompat` — soft dependencies, by reflection.
  - `Translations` — all the game's languages.
- `WinterSled.Tests/` — xunit (net10.0): surface rules, friction order, hauling, recipes, seasons, translations, sound synthesis. `mise run test`; `mise run sounds` writes the loops to `dist/sounds/`.
- `model/build_model.py` — generates `WinterSled/Resources/sled_undercarriage.obj` (stdlib only, checks winding).
- `model/render_preview.py` — Blender previews with the game's textures (`mise run preview`, needs `.extract/`).
- `tools/DevHarness/` — dev-only plugin, never shipped: creates a local test character/world and runs commands from `$WINTERSLED_HARNESS_DIR/cmd.txt`.
- `package/icon.png` (256 px, Thunderstore) and `package/logo.png` (1024 px, Nexus Mods/Hexium): the in-game screenshot of the sled pulled across Seasonality's frozen sea, hover text inpainted out, title in Cinzel (OFL).
- `package/nexus-header.jpg` (1920×480) and `package/gallery/*.jpg`: the Nexus Mods header and screenshots, shot with the harness (`hud off`, `equip`, `shot`).
- `package/nexus-description.bbcode` — the Nexus Mods page description (BBCode, in Nexus' sections); keep it in step with README.md.
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

Harness commands: `waitplayer`, `god [time]`, `hud on|off` (also hides messages and name plates), `tp x z [yaw]`, `findbiome <Biome> [radius] [minSlope] [maxSlope]`, `tpwater x z [yaw]` (over water, onto whatever is there), `equip <item...>` (dress the test character), `loadvis` (where the cargo crates sit), `body [interp|none]` (rigidbody interpolation), `chairs` (seats on the vehicle), `colliders <r>` (colliders around it, with layers), `burst <name> <frames> [yaw pitch dist height]` (consecutive frames, camera locked to the sled) or `burst <name> <frames> game [interval]` (the player's own camera), `god off` (mortal again: real stamina and damage), `clear <r>`, `look <yaw>`, `faceslope up|down`, `spawn [dist] [prefab] [keep]`, `sledpos <behind>`, `attach`, `detach`, `sit`, `move fwd|back|left|right|fl|fr|stop [run]`, `input x z`, `jump`, `release`, `load <item> <n>`, `state`, `record <sec> [step]`, `shot <name> [sled|player|game] [yaw] [pitch] [dist] [height]`, `anims`, `materials`, `quit`.

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

Two machines: `WINTERSLED_OPEN=1` in `D:\WinterSledTest\run.cmd` opens the host's world; on the Mac `WINTERSLED_JOIN=steam:<host SteamID64> WINTERSLED_CHAR=SledRider mise run game`, commands through `tools/harness/local-cmd.sh`. Harness extras: `nearest`, `goto <player>`, `clearsleds <r>`, `cfg <Key> <value>`, `recipe`, `langs`, `floor`, `count`. Seasons/Seasonality for tests live in `D:\WinterSledTest\disabled` (they can't both be loaded; a client without Seasonality is kicked by its ServerSync).
