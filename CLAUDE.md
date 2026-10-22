# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Winter Sled: a Valheim mod (BepInEx 5 + Jötunn, C# targeting net462) that adds a cargo sled built from the vanilla cart. User docs: README.md; developer notes, in-game measurements and the test machine: DEVELOPMENT.md.

## Commands

Tools come from mise (`mise.toml`: dotnet 10, blender). `VALHEIM_DIR` (mise env) points at the macOS Steam install; the csproj files read the game's `Managed/` and `BepInEx/` from there.

- `mise run build` — Release build of `WinterSled/WinterSled.csproj`
- `mise run test` — xunit tests (`WinterSled.Tests`, net10.0; loads the plugin and game assemblies as plain libraries)
- single test: `dotnet test WinterSled.Tests/WinterSled.Tests.csproj -c Release --filter "FullyQualifiedName~SurfaceTests.DeepNorthPowderNeedsDepth"`
- `mise run package` — `dist/WinterSled-<version>.zip` via `tools/package.sh` (fails if manifest and `Plugin.Version` disagree)
- `mise run model` — regenerate `WinterSled/Resources/sled_undercarriage.obj` (CI checks it is reproducible: commit the regenerated file)
- `mise run sounds` — write the synthesized loops to `dist/sounds/*.wav`
- `mise run deploy` / `harness` / `game` — copy into the local game, add the dev harness, start the game (macOS, Rosetta)
- Build like CI: `tools/ci/fetch-deps.sh <dir>` then add `-p:ValheimDir=<dir>` to build/test (mise's `VALHEIM_DIR` would otherwise win).
- Lint workflows: `mise x aqua:rhysd/actionlint@1.7.12 -- actionlint`

## Architecture

- **Prefab** (`SledBuilder`): at Jötunn's prefab registration, clones `Cart` into `WinterSled`: wheels removed, body lowered, procedural undercarriage mesh (`ObjMesh` loads the embedded .obj; materials are borrowed from game prefabs at runtime by `SledMaterials` — the mod ships no game assets), capsule runner colliders `runner_*`, a seat, `SledController` + `SledAudio`. The vanilla `Vagon` stays and does the hitching and container; the sled's own logic layers on top.
- **Physics** (`SledController`): runs only on the ZDO owner. Runners use a frictionless physics material; the controller applies its own Coulomb friction from a weight-based normal force (PhysX per-contact impulses double-counted it), split along (per-surface friction) and across (grip) the runners. Static friction (a high-friction runner material) below `StaticFrictionSpeed` when nobody pulls or rides, not on ice. A rider of an unpulled sled requests ownership by RPC so steering works for any player. `SledController.Instances` is used for live config updates.
- **Surfaces** (`SurfaceProbe`): raycasts under the runners (skipping the sled's own colliders). `ClassifyGround(biome, looksLike, paint, normalY)` is pure and is where surface rules live (unit-tested). `looksLike` is the biome as a seasons mod renders it; seasonal frozen water is detected as a solid Water-layer collider.
- **Hauling**: `SledHaulEffect` (status effect on the puller) scales move speed by surface/cargo, because Valheim's character controller ignores what it drags. `CartSnowDrag`/`CartSnowEffect` (Harmony on `Vagon.Awake`) do the same for wheeled vehicles in snow. `Patches.cs`: hide the cargo under a rider; no `Floating` on frozen water.
- **Config** (`SledConfig`): almost everything is server-synced (Jötunn `IsAdminOnly`), so all peers simulate the same sled; `LiveSettings` applies changes (mass, recipe, crash speeds) to the prefab and built sleds. The recipe switches to `SeasonalRequirements` when a seasons mod is present.
- **Soft dependencies** (`SeasonsCompat.cs`: `SeasonsCompat`, `SeasonalityCompat`): pure reflection, no assembly references; must degrade to "not installed" if those mods change.
- **Audio** (`SledAudio`): loops synthesized at startup (`Synthesize` is deterministic and tested), crossfaded per surface plus wind; the cart's wheel sound is muted.
- **Text**: `Translations.cs` holds every game language × every key; tests fail if a `$wintersled_*`/`$piece_wintersled*` token used in `src/` has no row, or a language misses a text. Adding a key means adding it to all 35 languages.

## Testing in game

`tools/DevHarness` is a dev-only plugin (never shipped; `InternalsVisibleTo`) that loads a local test world and executes commands written to `$WINTERSLED_HARNESS_DIR/cmd.txt`, logging state (speed, surface, owner). Testing runs on a Windows machine over SSH via `tools/harness/remote-*.sh` (target in `WINTERSLED_REMOTE=user@host`, set in the untracked `mise.local.toml`; `remote-redeploy.sh` copies DLLs and restarts, `remote-cmd.sh` sends harness commands on stdin and prints the log); the command list and setup are in DEVELOPMENT.md. Valheim's gravity is ~20 m/s², so deceleration from friction is μ·20.

## Releasing

`.github/workflows/ci.yml` builds against the dedicated server's assemblies (steamcmd, anonymous) plus BepInExPack/Jötunn from Thunderstore (versions in `tools/ci/versions.env`). A tag `v<version>` runs `release.yml`: GitHub release + `tools/release/publish.cs` to Thunderstore, Hexium, Nexus Mods (settings in `tools/release/sites.json`; manual dispatch = dry run). The version lives in both `package/manifest.json` and `Plugin.Version`; CHANGELOG.md sections are `## <version>` and feed the release notes.

## Conventions

- Commit messages: no Claude/AI attribution footer.
- Default branch is `master`.
- Markdown: no hard line wraps. A paragraph or list item is one line; the renderer wraps long lines.
