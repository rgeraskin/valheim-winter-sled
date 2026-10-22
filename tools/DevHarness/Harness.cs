using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using WinterSled;

namespace WinterSledDevHarness
{
    /// <summary>
    /// Development-only test driver. With WINTERSLED_HARNESS_DIR set it creates a local test
    /// character and world (never touching real saves), loads in, and then runs commands that
    /// appear in $WINTERSLED_HARNESS_DIR/cmd.txt, logging to log.txt and saving screenshots.
    /// </summary>
    [BepInPlugin("rgeraskin.WinterSled.DevHarness", "WinterSled DevHarness", "0.0.1")]
    [BepInDependency(Plugin.Guid)]
    public class Harness : BaseUnityPlugin
    {
        private const string CharName = "SledTester";
        private const string WorldName = "SledTest";
        private const string WorldSeed = "SledTest1";

        private string m_dir;
        private StreamWriter m_log;
        private GameObject m_sled;
        private Vector3 m_move;
        private bool m_run;
        private bool m_driving;
        private bool m_jump;

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static Harness s_instance;
        private bool m_keepHealth;

        private void Awake()
        {
            m_dir = Environment.GetEnvironmentVariable("WINTERSLED_HARNESS_DIR");
            if (string.IsNullOrEmpty(m_dir))
            {
                Logger.LogInfo("WINTERSLED_HARNESS_DIR not set, harness idle");
                enabled = false;
                return;
            }
            Directory.CreateDirectory(Path.Combine(m_dir, "shots"));
            m_log = new StreamWriter(Path.Combine(m_dir, "log.txt"), append: true) { AutoFlush = true };
            Application.runInBackground = true;
            s_instance = this;
            new Harmony("rgeraskin.WinterSled.DevHarness").PatchAll(typeof(Harness).Assembly);
            Log("harness up");
            StartCoroutine(Run());
        }

        internal static void LogStatic(string msg) => s_instance?.Log(msg);

        private void Log(string msg)
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
            m_log?.WriteLine(line);
            Logger.LogInfo(msg);
        }

        private IEnumerator Run()
        {
            while (FejdStartup.instance == null || FejdStartup.instance.m_profiles == null)
            {
                yield return new WaitForSeconds(0.5f);
            }
            yield return new WaitForSeconds(2f);
            var join = Environment.GetEnvironmentVariable("WINTERSLED_JOIN");
            if (!string.IsNullOrEmpty(join))
            {
                JoinTestWorld(join);
            }
            else if (Environment.GetEnvironmentVariable("WINTERSLED_AUTOSTART") == "1")
            {
                StartTestWorld();
            }
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                var cmdFile = Path.Combine(m_dir, "cmd.txt");
                if (!File.Exists(cmdFile))
                {
                    continue;
                }
                var lines = File.ReadAllLines(cmdFile);
                File.Delete(cmdFile);
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#"))
                    {
                        continue;
                    }
                    Log("> " + line);
                    IEnumerator routine = null;
                    try
                    {
                        routine = Execute(line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                    catch (Exception e)
                    {
                        Log("! " + e);
                    }
                    if (routine != null)
                    {
                        yield return SafeRun(routine);
                    }
                }
                Log("done");
            }
        }

        private IEnumerator SafeRun(IEnumerator routine)
        {
            while (true)
            {
                object current;
                try
                {
                    if (!routine.MoveNext())
                    {
                        yield break;
                    }
                    current = routine.Current;
                }
                catch (Exception e)
                {
                    Log("! " + e);
                    yield break;
                }
                yield return current;
            }
        }

        private static string CharacterName => Environment.GetEnvironmentVariable("WINTERSLED_CHAR") is string c && c.Length > 0 ? c : CharName;

        /// <summary>A local test character that skips the Valkyrie intro.</summary>
        private string EnsureCharacter()
        {
            var name = CharacterName;
            var file = name.ToLowerInvariant();
            var profile = new PlayerProfile(file, FileHelpers.FileSource.Local);
            if (!PlayerProfile.HaveProfile(file))
            {
                profile.SetName(name);
                profile.m_firstSpawn = false;
                profile.Save();
                Log("created local character " + name);
            }
            else if (profile.Load() && profile.m_firstSpawn)
            {
                profile.m_firstSpawn = false;
                profile.Save();
                Log("skipped the intro for " + name);
            }
            return file;
        }

        /// <summary>Joins a game hosted elsewhere (ip:port) with the test character, like -joinserverwithcharacter.</summary>
        private void JoinTestWorld(string address)
        {
            var fs = FejdStartup.instance;
            var file = EnsureCharacter();
            fs.UpdateCharacterList();
            // steam:<SteamID64> joins a game hosted by that player (Steam P2P); ip:port a dedicated server
            fs.m_joinServer = address.StartsWith("steam:")
                ? new ServerJoinData(new ServerJoinDataSteamUser(ulong.Parse(address.Substring(6))))
                : new ServerJoinData(new ServerJoinDataDedicated(address));
            fs.SelectCharacter(file, FileHelpers.FileSource.Local);
            fs.JoinServer();
            Log($"joining {address} as {CharacterName}");
        }

        private void StartTestWorld()
        {
            var fs = FejdStartup.instance;
            var file = CharacterName.ToLowerInvariant();
            if (!PlayerProfile.HaveProfile(file))
            {
                var profile = new PlayerProfile(file, FileHelpers.FileSource.Local);
                profile.SetName(CharName);
                // no Valkyrie ride and intro text for the test character
                profile.m_firstSpawn = false;
                profile.Save();
                Log("created local character " + CharName);
            }
            else
            {
                // the profile may still be waiting for its Valkyrie ride (the game was killed before it saved)
                var profile = new PlayerProfile(file, FileHelpers.FileSource.Local);
                if (profile.Load() && profile.m_firstSpawn)
                {
                    profile.m_firstSpawn = false;
                    profile.Save();
                    Log("skipped the intro for " + CharName);
                }
            }
            fs.UpdateCharacterList();
            fs.SetSelectedProfile(file);
            fs.OnCharacterStart();
            var world = World.GetCreateWorld(WorldName, FileHelpers.FileSource.Local);
            if (!World.HaveWorld(WorldName))
            {
                world = new World(WorldName, WorldSeed) { m_fileSource = FileHelpers.FileSource.Local, m_needsDB = false };
                world.SaveWorldFWLData(DateTime.Now);
                Log("created local world " + WorldName);
            }
            fs.m_world = world;
            // WINTERSLED_OPEN=1 lets a second client join over the LAN (not listed, no password)
            fs.m_openServerToggle.isOn = Environment.GetEnvironmentVariable("WINTERSLED_OPEN") == "1";
            fs.m_publicServerToggle.isOn = false;
            fs.m_crossplayServerToggle.isOn = false;
            fs.OnWorldStart();
            Log("starting world " + WorldName);
        }

        // ------------------------------------------------------------------ driving the player

        private void Update()
        {
            var player = Player.m_localPlayer;
            if (player != null && m_keepHealth && player.GetHealth() < player.GetMaxHealth())
            {
                player.SetHealth(player.GetMaxHealth());
            }
            if (player == null || !m_driving)
            {
                return;
            }
            player.SetControls(m_move, false, false, false, false, false, false, m_jump, false, m_run, false);
            m_jump = false;
        }

        private void Drive(bool on)
        {
            var player = Player.m_localPlayer;
            m_driving = on;
            if (player != null && player.TryGetComponent<PlayerController>(out var pc))
            {
                pc.enabled = !on;
            }
        }

        // ------------------------------------------------------------------ commands

        private IEnumerator Execute(string[] a)
        {
            var player = Player.m_localPlayer;
            switch (a[0])
            {
                case "start":
                    StartTestWorld();
                    break;
                case "wait":
                    yield return new WaitForSeconds(F(a[1]));
                    break;
                case "waitplayer":
                    while (Player.m_localPlayer == null || Player.m_localPlayer.IsTeleporting() || Player.m_localPlayer.InIntro())
                    {
                        yield return new WaitForSeconds(0.5f);
                    }
                    Log("player ready at " + Player.m_localPlayer.transform.position);
                    break;
                case "god" when a.Length > 1 && a[1] == "off":
                    // a real character again (stamina, damage), keeping the clear weather and peaceful mobs
                    player.SetGodMode(false);
                    player.SetGhostMode(false);
                    break;
                case "god":
                    player.SetGodMode(true);
                    player.SetGhostMode(true);
                    player.m_nview.GetZDO().Set(ZDOVars.s_stamina, 999f);
                    EnvMan.instance.m_debugTimeOfDay = true;
                    EnvMan.instance.m_debugTime = a.Length > 1 ? F(a[1]) : 0.45f;
                    EnvMan.instance.m_debugEnv = "Clear";
                    // creatures don't start fights (or chew on the sled), no raids
                    ZoneSystem.instance.SetGlobalKey(GlobalKeys.PassiveMobs);
                    ZoneSystem.instance.SetGlobalKey(GlobalKeys.EventRate, 0f);
                    m_keepHealth = true;
                    Log("god/ghost on, health kept full, clear noon, passive mobs, no raids");
                    break;
                case "time":
                    EnvMan.instance.m_debugTime = F(a[1]);
                    break;
                case "findbiome":
                    FindBiome((Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), a[1]), a.Length > 2 ? F(a[2]) : 3000f, a.Length > 3 ? F(a[3]) : 8f, a.Length > 4 ? F(a[4]) : 25f);
                    break;
                case "tp":
                    yield return Teleport(new Vector3(F(a[1]), 0f, F(a[2])), a.Length > 3 ? F(a[3]) : 0f);
                    break;
                case "tpwater":
                    // over water: start above the water level and fall onto whatever is there (frozen water, or swim)
                    yield return Teleport(new Vector3(F(a[1]), 0f, F(a[2])), a.Length > 3 ? F(a[3]) : 0f, onWater: true);
                    break;
                case "spawn":
                    SpawnSled(a.Length > 1 ? F(a[1]) : 4f, a.Length > 2 ? a[2] : SledBuilder.PrefabName, a.Length > 3 && a[3] == "keep");
                    break;
                case "attach":
                    yield return AttachToSled();
                    break;
                case "detach":
                    m_sled.GetComponent<Vagon>().Interact(player, false, false);
                    break;
                case "sit":
                    m_sled.GetComponentInChildren<SledSeat>().Interact(player, false, false);
                    yield return new WaitForSeconds(0.5f);
                    Log($"attached={player.IsAttached()} doodad={player.GetDoodadController() != null}");
                    break;
                case "move":
                    Drive(true);
                    m_move = a[1] switch
                    {
                        "fwd" => Vector3.forward,
                        "back" => Vector3.back,
                        "left" => Vector3.left,
                        "right" => Vector3.right,
                        "fl" => (Vector3.forward + Vector3.left).normalized,
                        "fr" => (Vector3.forward + Vector3.right).normalized,
                        _ => Vector3.zero,
                    };
                    m_run = a.Length > 2 && a[2] == "run";
                    if (m_move == Vector3.zero)
                    {
                        // move stop: hand the keyboard and mouse back to whoever is playing
                        Drive(false);
                    }
                    break;
                case "input":
                    Drive(true);
                    m_move = new Vector3(F(a[1]), 0f, F(a[2]));
                    break;
                case "jump":
                    Drive(true);
                    m_jump = true;
                    break;
                case "release":
                    m_move = Vector3.zero;
                    Drive(false);
                    break;
                case "look":
                    Face(Quaternion.Euler(0f, F(a[1]), 0f) * Vector3.forward);
                    break;
                case "clear":
                    Clear(F(a[1]));
                    break;
                case "faceslope":
                    FaceSlope(a.Length > 1 && a[1] == "up");
                    break;
                case "body":
                    {
                        // body [interp|none]: the sled's rigidbody interpolation (and switch it, to compare)
                        var rb = m_sled.GetComponent<Rigidbody>();
                        if (a.Length > 1)
                        {
                            rb.interpolation = a[1] == "interp" ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
                        }
                        Log($"body interpolation={rb.interpolation} fixedDt={Time.fixedDeltaTime:0.000} fps={1f / Time.smoothDeltaTime:0} owner={m_sled.GetComponent<ZNetView>().IsOwner()}");
                    }
                    break;
                case "colliders":
                    {
                        // colliders <r>: what is around the last spawned vehicle, with layers (and its own colliders' layers)
                        float r = a.Length > 1 ? F(a[1]) : 3f;
                        var own = new HashSet<Collider>(m_sled.GetComponentsInChildren<Collider>(true));
                        foreach (var c in own)
                        {
                            Log($"own {c.name} [{c.GetType().Name}] layer={LayerMask.LayerToName(c.gameObject.layer)} trigger={c.isTrigger} enabled={c.enabled} mat={(c.sharedMaterial ? c.sharedMaterial.name : "-")}");
                        }
                        foreach (var c in Physics.OverlapSphere(m_sled.transform.position, r, ~0, QueryTriggerInteraction.Collide).Where(c => !own.Contains(c)).Take(40))
                        {
                            var root = c.transform.root;
                            Log($"near {c.name} (root {root.name}) [{c.GetType().Name}] layer={LayerMask.LayerToName(c.gameObject.layer)} trigger={c.isTrigger} mat={(c.sharedMaterial ? c.sharedMaterial.name : "-")}");
                        }
                    }
                    break;
                case "chairs":
                    // the chairs and seats on the last spawned vehicle, active or not
                    foreach (var c in m_sled.GetComponentsInChildren<Chair>(true))
                    {
                        var col = c.GetComponent<Collider>();
                        Log($"Chair {c.name} activeInHierarchy={c.gameObject.activeInHierarchy} enabled={c.enabled} collider={(col ? col.GetType().Name + (col.enabled ? "" : "(off)") : "none")} name={c.m_name} attach={(c.m_attachPoint ? m_sled.transform.InverseTransformPoint(c.m_attachPoint.position).ToString("F2") : "-")} anim={c.m_attachAnimation} layer={LayerMask.LayerToName(c.gameObject.layer)}");
                    }
                    foreach (var c in m_sled.GetComponentsInChildren<SledSeat>(true))
                    {
                        Log($"SledSeat {c.name} attach={m_sled.transform.InverseTransformPoint(c.m_attachPoint.position).ToString("F2")}");
                    }
                    break;
                case "loadvis":
                    // where the cargo crates sit in the bed, in the sled's own space
                    foreach (var lv in m_sled.GetComponent<Vagon>().m_loadVis)
                    {
                        var go = lv.m_gameobject;
                        var r = go.GetComponentsInChildren<Renderer>(true);
                        var b = r.Length > 0 ? r[0].bounds : new Bounds(go.transform.position, Vector3.zero);
                        foreach (var x in r) b.Encapsulate(x.bounds);
                        Log($"loadvis {go.name} min%={lv.m_minPercentage} active={go.activeSelf} local={m_sled.transform.InverseTransformPoint(b.center).ToString("F2")} size={b.size.ToString("F2")}");
                    }
                    var seatTf = m_sled.GetComponentInChildren<SledSeat>().transform;
                    Log($"seat local={m_sled.transform.InverseTransformPoint(seatTf.position).ToString("F2")}");
                    break;
                case "load":
                    var inv = m_sled.GetComponent<Vagon>().m_container.GetInventory();
                    inv.AddItem(ObjectDB.instance.GetItemPrefab(a[1]), int.Parse(a[2]));
                    Log($"cargo weight {inv.GetTotalWeight()}");
                    break;
                case "state":
                    LogState(a.Length > 1 ? a[1] : "");
                    break;
                case "record":
                    float until = Time.time + F(a[1]);
                    float step = a.Length > 2 ? F(a[2]) : 0.5f;
                    while (Time.time < until)
                    {
                        LogState("rec");
                        yield return new WaitForSeconds(step);
                    }
                    break;
                case "burst":
                    yield return Burst(a);
                    break;
                case "shot":
                    yield return Shot(a);
                    break;
                case "hud":
                    Hud.instance.m_rootObject.SetActive(a[1] == "on");
                    // messages (the riding hint) live outside the HUD's root
                    MessageHud.instance.m_messageText.gameObject.SetActive(a[1] == "on");
                    MessageHud.instance.m_messageCenterText.gameObject.SetActive(a[1] == "on");
                    // name plates over characters
                    EnemyHud.instance.m_hudRoot.SetActive(a[1] == "on");
                    break;
                case "equip":
                    // equip <item...>: dress the test character (e.g. for screenshots)
                    foreach (var itemName in a.Skip(1))
                    {
                        var prefab = ObjectDB.instance.GetItemPrefab(itemName);
                        ItemDrop.ItemData item = null;
                        if (prefab != null && player.GetInventory().AddItem(prefab, 1))
                        {
                            item = player.GetInventory().GetItem(prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
                        }
                        if (item != null)
                        {
                            player.EquipItem(item);
                        }
                        Log(item != null ? "equipped " + itemName : "no item " + itemName);
                    }
                    break;
                case "sledpos":
                    PlaceSled(F(a[1]));
                    break;
                case "anims":
                    var names = player.m_animator.parameters.Select(p => p.name).Where(n => n.StartsWith("attach") || n.StartsWith("emote"));
                    Log("animator: " + string.Join(", ", names));
                    break;
                case "materials":
                    foreach (var r in m_sled.GetComponentsInChildren<Renderer>())
                    {
                        Log($"{r.name} [{r.GetType().Name}, motion vectors {r.motionVectorGenerationMode}, parent {r.transform.parent?.name}]: {string.Join(", ", r.sharedMaterials.Select(m => m ? $"{m.name}({m.shader.name})" : "null"))} enabled={r.enabled} visible={r.isVisible}");
                    }
                    foreach (var lod in m_sled.GetComponentsInChildren<LODGroup>())
                    {
                        Log($"LODGroup {lod.name} fade={lod.fadeMode} crossfade={lod.animateCrossFading}: " + string.Join(" | ", lod.GetLODs().Select((l, i) => $"LOD{i}@{l.screenRelativeTransitionHeight:0.00}: " + string.Join(",", l.renderers.Where(x => x).Select(x => x.name)))));
                    }
                    Log($"camera: {Camera.main?.name} depthTextureMode={Camera.main?.depthTextureMode} allowMSAA={Camera.main?.allowMSAA}");
                    break;
                case "icon":
                    {
                        var sprite = ZNetScene.instance.GetPrefab(SledBuilder.PrefabName).GetComponent<Piece>().m_icon;
                        var r = sprite.textureRect;
                        var rt = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height);
                        Graphics.Blit(sprite.texture, rt);
                        var prev = RenderTexture.active;
                        RenderTexture.active = rt;
                        var tex = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
                        tex.ReadPixels(new Rect(r.x, sprite.texture.height - r.y - r.height, r.width, r.height), 0, 0);
                        tex.Apply();
                        RenderTexture.active = prev;
                        RenderTexture.ReleaseTemporary(rt);
                        File.WriteAllBytes(Path.Combine(m_dir, "shots", "icon.png"), tex.EncodeToPNG());
                        Log($"icon {sprite.name} {r.width}x{r.height}");
                    }
                    break;
                case "langs":
                    foreach (var lang in Translations.Languages.Concat(new[] { "Abenaki" }))
                    {
                        Localization.instance.SetLanguage(lang);
                        Log($"{lang}: {Localization.instance.Localize("$piece_wintersled | $se_wintersled | $wintersled_surface_snow | $se_wintersled_cart | " + Translations.ControlsHint)}");
                    }
                    Localization.instance.SetLanguage(a.Length > 1 ? a[1] : "English");
                    break;
                case "floor":
                    {
                        // floor <prefab> <cols> <rows> <start metres ahead>: a strip of pieces along the look direction
                        string prefab = a[1];
                        int cols = int.Parse(a[2]), rows = int.Parse(a[3]);
                        float start = F(a[4]);
                        var fwd = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
                        var right = Vector3.Cross(Vector3.up, fwd);
                        var go = ZNetScene.instance.GetPrefab(prefab);
                        var origin = player.transform.position + fwd * start;
                        ZoneSystem.instance.GetSolidHeight(origin, out float h0);
                        int n = 0;
                        for (int r = 0; r < rows; r++)
                            for (int c = 0; c < cols; c++)
                            {
                                var pos = origin + fwd * (r * 2f) + right * ((c - (cols - 1) / 2f) * 2f);
                                pos.y = h0 + 0.05f;
                                Instantiate(go, pos, Quaternion.LookRotation(fwd));
                                n++;
                            }
                        Log($"placed {n} {prefab} from {origin} at y {h0 + 0.05f:0.00}");
                    }
                    break;
                case "count":
                    {
                        float r = F(a[2]);
                        var c = player.transform.position;
                        int n = FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.parent == null && t.name.StartsWith(a[1]) && Vector3.Distance(t.position, c) < r);
                        Log($"{n} objects named {a[1]}* within {r} m");
                    }
                    break;
                case "nearest":
                    {
                        // nearest <r>: work with the closest sled (one another player spawned)
                        float r = a.Length > 1 ? F(a[1]) : 20f;
                        m_sled = FindObjectsByType<SledController>(FindObjectsSortMode.None)
                            .Where(sc => Vector3.Distance(sc.transform.position, player.transform.position) < r)
                            .OrderBy(sc => Vector3.Distance(sc.transform.position, player.transform.position))
                            .Select(sc => sc.gameObject).FirstOrDefault();
                        Log(m_sled ? $"sled at {m_sled.transform.position}" : "no sled near");
                    }
                    break;
                case "goto":
                    {
                        // goto <player name>: stand 3 m behind another player
                        var other = Player.GetAllPlayers().FirstOrDefault(pl => pl != player && pl.GetPlayerName() == a[1]);
                        if (other)
                        {
                            var pos = other.transform.position - other.transform.forward * 3f + Vector3.up * 0.5f;
                            player.transform.position = pos;
                            player.m_body.position = pos;
                            Log($"next to {a[1]} at {pos}");
                        }
                        else
                        {
                            Log("players: " + string.Join(", ", Player.GetAllPlayers().Select(pl => pl.GetPlayerName())));
                        }
                    }
                    break;
                case "clearsleds":
                    {
                        float r = F(a[1]);
                        int n = 0;
                        foreach (var v in FindObjectsByType<Vagon>(FindObjectsSortMode.None))
                        {
                            if (Vector3.Distance(v.transform.position, player.transform.position) < r && v.TryGetComponent<ZNetView>(out var nv) && nv.IsValid())
                            {
                                nv.ClaimOwnership();
                                ZNetScene.instance.Destroy(v.gameObject);
                                n++;
                            }
                        }
                        m_sled = null;
                        Log($"removed {n} sleds/carts within {r} m");
                    }
                    break;
                case "cfg":
                    {
                        // cfg <Key> <value>: change a sled setting live (same names as in the .cfg)
                        var field = typeof(SledConfig).GetField(a[1]);
                        if (field?.GetValue(Plugin.Cfg) is BepInEx.Configuration.ConfigEntryBase entry)
                        {
                            entry.BoxedValue = Convert.ChangeType(a[2], entry.SettingType, Inv);
                            Log($"{a[1]} = {entry.BoxedValue}");
                        }
                        else
                        {
                            Log("no setting " + a[1]);
                        }
                    }
                    break;
                case "audio":
                    {
                        float until2 = Time.time + (a.Length > 1 ? F(a[1]) : 0f);
                        float step2 = a.Length > 2 ? F(a[2]) : 0.5f;
                        do
                        {
                            var au = m_sled ? m_sled.GetComponent<SledAudio>() : null;
                            var sc = m_sled ? m_sled.GetComponent<SledController>() : null;
                            Log(au ? $"audio speed={sc.Speed:0.0} surface={sc.Surface} touching={sc.Touching}: {au.Describe()}" : "no sled audio");
                            yield return new WaitForSeconds(step2);
                        } while (Time.time < until2);
                    }
                    break;
                case "recipe":
                    {
                        var piece = ZNetScene.instance.GetPrefab(SledBuilder.PrefabName).GetComponent<Piece>();
                        Log("sled recipe: " + string.Join(", ", piece.m_resources.Select(r => $"{r.m_resItem?.name}x{r.m_amount}")) + $" station={piece.m_craftingStation?.m_name}");
                    }
                    break;
                case "quit":
                    Application.Quit();
                    break;
                default:
                    Log("unknown command " + a[0]);
                    break;
            }
        }

        private static float F(string s) => float.Parse(s, Inv);

        // System.ValueTuple isn't in the game's runtime, so no tuples here
        private struct Spot
        {
            public float dist;
            public Vector3 pos;
            public float slope;
        }

        private void FindBiome(Heightmap.Biome biome, float radius, float minSlope, float maxSlope)
        {
            var origin = Player.m_localPlayer.transform.position;
            var wg = WorldGenerator.instance;
            var found = new List<Spot>();
            for (float r = 0; r <= radius; r += 32f)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(2 * Mathf.PI * r / 32f));
                for (int i = 0; i < steps; i++)
                {
                    float ang = 2 * Mathf.PI * i / steps;
                    var p = origin + new Vector3(Mathf.Cos(ang) * r, 0, Mathf.Sin(ang) * r);
                    if (wg.GetBiome(p.x, p.z) != biome)
                    {
                        continue;
                    }
                    float h = wg.GetHeight(p.x, p.z);
                    float dx = wg.GetHeight(p.x + 3, p.z) - wg.GetHeight(p.x - 3, p.z);
                    float dz = wg.GetHeight(p.x, p.z + 3) - wg.GetHeight(p.x, p.z - 3);
                    float slope = Mathf.Atan(new Vector2(dx, dz).magnitude / 6f) * Mathf.Rad2Deg;
                    if (slope >= minSlope && slope <= maxSlope && (biome == Heightmap.Biome.Ocean ? h < 28f : h > 30f))
                    {
                        found.Add(new Spot { dist = r, pos = new Vector3(p.x, h, p.z), slope = slope });
                    }
                }
                if (found.Count >= 5)
                {
                    break;
                }
            }
            foreach (var f in found.Take(5))
            {
                Log($"{biome} at {f.pos.x:0} {f.pos.z:0} h={f.pos.y:0.0} slope={f.slope:0.0} dist={f.dist:0}");
            }
            if (found.Count == 0)
            {
                Log($"no {biome} with slope {minSlope}-{maxSlope} within {radius}");
            }
        }

        private IEnumerator Teleport(Vector3 pos, float yaw, bool onWater = false)
        {
            var player = Player.m_localPlayer;
            pos.y = Mathf.Max(WorldGenerator.instance.GetHeight(pos.x, pos.z), onWater ? ZoneSystem.instance.m_waterLevel : float.MinValue) + 2f;
            // right after loading in, the game sometimes ignores a teleport: ask again until it takes
            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (player.TeleportTo(pos, Quaternion.Euler(0, yaw, 0), true) || player.IsTeleporting())
                {
                    break;
                }
                yield return new WaitForSeconds(1f);
            }
            yield return new WaitForSeconds(1f);
            while (player.IsTeleporting())
            {
                yield return new WaitForSeconds(0.5f);
            }
            yield return new WaitForSeconds(2f);
            if (onWater)
            {
                player.transform.position = new Vector3(pos.x, ZoneSystem.instance.m_waterLevel + 1f, pos.z);
                yield return new WaitForSeconds(2f);
            }
            else if (ZoneSystem.instance.GetSolidHeight(pos, out float h))
            {
                player.transform.position = new Vector3(pos.x, h + 0.1f, pos.z);
            }
            Log($"at {player.transform.position} biome {player.GetCurrentBiome()}");
        }

        private void FaceSlope(bool up)
        {
            var player = Player.m_localPlayer;
            var p = player.transform.position;
            var wg = WorldGenerator.instance;
            float dx = wg.GetHeight(p.x + 3, p.z) - wg.GetHeight(p.x - 3, p.z);
            float dz = wg.GetHeight(p.x, p.z + 3) - wg.GetHeight(p.x, p.z - 3);
            var downhill = new Vector3(-dx, 0, -dz).normalized;
            var dir = up ? -downhill : downhill;
            Face(dir);
            Log($"facing {(up ? "uphill" : "downhill")} yaw={Quaternion.LookRotation(dir).eulerAngles.y:0}");
        }

        private static void Face(Vector3 dir)
        {
            var player = Player.m_localPlayer;
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
            var rot = Quaternion.LookRotation(dir);
            player.m_lookYaw = rot;
            player.SetLookDir(dir);
            player.transform.rotation = rot;
            player.m_body.rotation = rot;
        }

        /// <summary>Removes rocks, trees, logs and bushes around the player for a clean test run.</summary>
        private void Clear(float radius)
        {
            var center = Player.m_localPlayer.transform.position;
            int removed = 0;
            foreach (var nview in FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
            {
                if (!nview || !nview.IsValid() || Vector3.Distance(nview.transform.position, center) > radius)
                {
                    continue;
                }
                var go = nview.gameObject;
                if (go.GetComponent<MineRock>() || go.GetComponent<MineRock5>() || go.GetComponent<Destructible>() || go.GetComponent<TreeBase>() || go.GetComponent<TreeLog>() || go.GetComponent<Pickable>())
                {
                    nview.ClaimOwnership();
                    ZNetScene.instance.Destroy(go);
                    removed++;
                }
            }
            Log($"cleared {removed} objects within {radius} m");
        }

        private void SpawnSled(float distance, string prefabName, bool keep = false)
        {
            var player = Player.m_localPlayer;
            var fwd = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            var pos = player.transform.position + fwd * distance;
            if (ZoneSystem.instance.GetSolidHeight(pos, out float h))
            {
                pos.y = Mathf.Max(h, ZoneSystem.instance.m_waterLevel) + 0.3f;
            }
            // keep: leave the previous one standing (e.g. a cart next to the sled, to compare)
            if (m_sled != null && !keep)
            {
                ZNetScene.instance.Destroy(m_sled);
            }
            m_sled = Instantiate(ZNetScene.instance.GetPrefab(prefabName), pos, Quaternion.LookRotation(fwd));
            Log($"{prefabName} spawned at {pos}");
        }

        /// <summary>Puts the sled behind the player, shafts toward them, ready to be pulled.</summary>
        private void PlaceSled(float behind)
        {
            var player = Player.m_localPlayer;
            var fwd = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            var pos = player.transform.position - fwd * behind;
            if (ZoneSystem.instance.GetSolidHeight(pos, out float h))
            {
                pos.y = Mathf.Max(h, ZoneSystem.instance.m_waterLevel) + 0.2f;
            }
            var body = m_sled.GetComponent<Rigidbody>();
            body.position = pos;
            body.rotation = Quaternion.LookRotation(fwd);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            m_sled.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd));
        }

        private IEnumerator AttachToSled()
        {
            var player = Player.m_localPlayer;
            var vagon = m_sled.GetComponent<Vagon>();
            var fwd = Vector3.ProjectOnPlane(m_sled.transform.forward, Vector3.up).normalized;
            var pos = vagon.m_attachPoint.position - Vector3.up * 0.8f + fwd * 0.1f;
            if (ZoneSystem.instance.GetSolidHeight(pos, out float h))
            {
                pos.y = Mathf.Max(h, ZoneSystem.instance.m_waterLevel);
            }
            player.transform.position = pos;
            player.m_body.position = pos;
            Face(fwd);
            yield return new WaitForSeconds(0.3f);
            vagon.Interact(player, false, false);
            yield return new WaitForSeconds(0.5f);
            Log($"attached={vagon.IsAttached(player)} dist={Vector3.Distance(player.transform.position + vagon.m_attachOffset, vagon.m_attachPoint.position):0.00}");
        }

        private void LogState(string tag)
        {
            var player = Player.m_localPlayer;
            var sb = new System.Text.StringBuilder(tag + " ");
            if (player != null)
            {
                var pv = player.m_body.linearVelocity;
                sb.Append($"player hp={player.GetHealth():0} pos={player.transform.position.ToString("F1")} v={new Vector2(pv.x, pv.z).magnitude:0.00} biome={player.GetCurrentBiome()} stamina={player.GetStamina():0} attached={player.IsAttached()} ");
                var se = player.GetSEMan().GetStatusEffect(SledHaulEffect.Hash);
                if (se != null)
                {
                    sb.Append($"SE[{se.GetIconText()} factor={SledHaulEffect.SpeedFactor():0.00}] ");
                }
                var cse = player.GetSEMan().GetStatusEffect(CartSnowEffect.Hash);
                if (cse != null)
                {
                    sb.Append($"CartSE[{cse.GetIconText()} factor={(CartSnowDrag.LocalPulled ? CartSnowDrag.LocalPulled.SpeedFactor() : 1f):0.00}] ");
                }
            }
            if (m_sled != null)
            {
                var sled = m_sled.GetComponent<SledController>();
                var vagon = m_sled.GetComponent<Vagon>();
                var body = m_sled.GetComponent<Rigidbody>();
                var v = body.linearVelocity;
                float along = Vector3.Dot(v, m_sled.transform.forward);
                sb.Append($"sled pos={m_sled.transform.position.ToString("F1")} speed={v.magnitude:0.00} along={along:0.00} side={Vector3.Dot(v, m_sled.transform.right):0.00} up.y={m_sled.transform.up.y:0.00} surface={(sled ? sled.Surface.ToString() : "-")} pulled={vagon.IsAttached()} rider={(sled ? sled.HasRider() : false)} mass={body.mass:0} hp={m_sled.GetComponent<ZNetView>().GetZDO().GetFloat(ZDOVars.s_health, -1f):0} yaw={m_sled.transform.eulerAngles.y:0.00} pitch={Mathf.DeltaAngle(0f, m_sled.transform.eulerAngles.x):0.00} roll={Mathf.DeltaAngle(0f, m_sled.transform.eulerAngles.z):0.00} w=({body.angularVelocity.x:0.000},{body.angularVelocity.y:0.000},{body.angularVelocity.z:0.000}) owner={m_sled.GetComponent<ZNetView>().IsOwner()}");
            }
            Log(sb.ToString());
        }

        // burst <name> <frames> [yaw] [pitch] [dist] [height]: consecutive frames, the camera locked to the sled;
        // burst <name> <frames> game [interval]: the game's own camera, as the player sees it, every interval seconds
        private IEnumerator Burst(string[] a)
        {
            string name = a[1];
            int frames = a.Length > 2 ? int.Parse(a[2]) : 10;
            if (a.Length > 3 && a[3] == "game")
            {
                float interval = a.Length > 4 ? F(a[4]) : 0f;
                for (int i = 0; i < frames; i++)
                {
                    if (interval > 0f && i > 0)
                    {
                        yield return new WaitForSeconds(interval);
                    }
                    yield return new WaitForEndOfFrame();
                    LogState($"frame{i:00}");
                    var shot = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(m_dir, "shots", $"{name}_{i:00}.png"), shot.EncodeToPNG());
                    Destroy(shot);
                    yield return null;
                }
                Log($"burst {name} x{frames} (game camera)");
                yield break;
            }
            float yaw = a.Length > 3 ? F(a[3]) : 90f;
            float pitch = a.Length > 4 ? F(a[4]) : 15f;
            float dist = a.Length > 5 ? F(a[5]) : 3f;
            float height = a.Length > 6 ? F(a[6]) : 0.3f;
            var gc = GameCamera.instance;
            var cam = gc.m_camera;
            gc.enabled = false;
            for (int i = 0; i < frames; i++)
            {
                // place the camera in this frame's update, after the sled moved, so the sled stands still in the shots
                yield return null;
                var focus = m_sled.transform;
                var center = focus.position + Vector3.up * height;
                var dir = Quaternion.Euler(pitch, focus.eulerAngles.y + 180f + yaw, 0f) * Vector3.forward;
                cam.transform.position = center - dir * dist;
                cam.transform.rotation = Quaternion.LookRotation(dir);
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(m_dir, "shots", $"{name}_{i:00}.png"), tex.EncodeToPNG());
                Destroy(tex);
            }
            gc.enabled = true;
            Log($"burst {name} x{frames}");
        }

        private IEnumerator Shot(string[] a)
        {
            // shot <name> [target sled|player] [yaw] [pitch] [dist] [height]
            string name = a[1];
            string target = a.Length > 2 ? a[2] : "sled";
            float yaw = a.Length > 3 ? F(a[3]) : 35f;
            float pitch = a.Length > 4 ? F(a[4]) : 20f;
            float dist = a.Length > 5 ? F(a[5]) : 5f;
            float height = a.Length > 6 ? F(a[6]) : 0.5f;
            var gc = GameCamera.instance;
            var cam = gc.m_camera;
            Transform focus = target == "player" || m_sled == null ? Player.m_localPlayer.transform : m_sled.transform;
            if (target != "game")
            {
                gc.enabled = false;
                var center = focus.position + Vector3.up * height;
                var dir = Quaternion.Euler(pitch, focus.eulerAngles.y + 180f + yaw, 0f) * Vector3.forward;
                cam.transform.position = center - dir * dist;
                cam.transform.rotation = Quaternion.LookRotation(dir);
            }
            // hold still for a few frames so the game's motion blur settles
            for (int i = 0; i < 8; i++)
            {
                yield return null;
            }
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            var path = Path.Combine(m_dir, "shots", name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
            gc.enabled = true;
            Log("shot " + path);
        }
    }

    /// <summary>Logs every hit the local player takes, to find what hurts a rider.</summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    internal static class LogPlayerDamage
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            if (__instance != Player.m_localPlayer || hit == null)
            {
                return;
            }
            var attacker = hit.GetAttacker();
            s_instanceLog($"DAMAGE total={hit.GetTotalDamage():0.0} type={hit.m_hitType} blunt={hit.m_damage.m_blunt:0.0} attacker={(attacker ? attacker.name : "-")} at {hit.m_point} attached={Player.m_localPlayer.IsAttached()}");
        }

        private static void s_instanceLog(string msg) => Harness.LogStatic(msg);
    }
}
