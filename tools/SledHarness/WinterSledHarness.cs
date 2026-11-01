using System.Collections;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using ValheimDevHarness;
using WinterSled;

namespace WinterSledHarness;

/// <summary>
/// Winter Sled's commands for the shared valheim-dev-harness: a sled, and a tamed animal to pull it (with a mod that
/// hitches carts to animals: Beasts of Burden, OdinHorse). Dev-only, never shipped.
/// </summary>
[BepInPlugin("rgeraskin.WinterSledHarness", "WinterSled Harness", "0.0.1")]
[BepInDependency(Harness.Guid)]
[BepInDependency(Plugin.Guid)]
public class WinterSledHarness : BaseUnityPlugin
{
    static GameObject sled;
    static Character beast;
    static GameObject leadMarker;

    static Player P => Player.m_localPlayer;
    static float Opt(string[] a, int i, float fallback) => a.Length > i ? Harness.F(a[i]) : fallback;
    static Vector3 Flat(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up).normalized;

    void Awake()
    {
        Harness.Register("sled", "sled [dist=4] [prefab=WinterSled]: a sled in front of the player, handles away from them (replaces the last one)", a =>
        {
            var fwd = Flat(P.transform.forward);
            var pos = Ground(P.transform.position + fwd * Opt(a, 0, 4f)) + Vector3.up * 0.3f;
            if (sled) ZNetScene.instance.Destroy(sled);
            sled = Instantiate(ZNetScene.instance.GetPrefab(a.Length > 1 ? a[1] : SledBuilder.PrefabName), pos, Quaternion.LookRotation(fwd));
            Harness.Log(Inv($"{sled.name} at {Actions.Fmt(pos)}"));
        });
        Harness.Register("sledaudio", "sledaudio: the sled's surface and speed, and each sound loop's volume, pitch and whether it plays (▶)", a =>
        {
            var au = sled ? sled.GetComponent<SledAudio>() : null;
            var sc = sled ? sled.GetComponent<SledController>() : null;
            Harness.Log(au ? Inv($"audio surface={sc.ShownSurface} speed={sc.Speed:0.0} touching={sc.Touching}: {au.Describe()}") : "no sled audio");
        });
        Harness.Register("findsled", "findsled [r=30]: take the nearest vehicle (Vagon) as the sled", a =>
        {
            var v = Actions.Nearest<Vagon>(Opt(a, 0, 30f));
            if (!v) throw new System.ArgumentException("no vehicle near");
            sled = v.gameObject;
            Harness.Log(Inv($"sled = {sled.name} at {Actions.Fmt(sled.transform.position)}"));
        });
        Harness.Register("beast","beast <prefab> [level=1]: a tamed creature right in front of the sled's handles, facing the sled's way", a =>
        {
            NeedSled();
            var vagon = sled.GetComponent<Vagon>();
            var fwd = Flat(sled.transform.forward);
            var prefab = ZNetScene.instance.GetPrefab(a[0]) ?? throw new System.ArgumentException("no prefab " + a[0]);
            if (beast) ZNetScene.instance.Destroy(beast.gameObject);
            // the radius comes from the live collider: spawn well ahead, then step back to the handles
            beast = Instantiate(prefab, Ground(vagon.m_attachPoint.position + fwd * 3f), Quaternion.LookRotation(fwd)).GetComponent<Character>();
            var pos = PlaceBeast(0.2f);
            if (a.Length > 1) beast.SetLevel(Harness.I(a[1]));
            // deer and other AnimalAI creatures (tameable with Draft Animals) have no MonsterAI.MakeTame
            var monster = beast.GetComponent<MonsterAI>();
            if (monster) monster.MakeTame();
            else beast.SetTamed(true);
            Harness.Log(Inv($"{beast.m_name} tamed={beast.IsTamed()} radius={beast.GetRadius():0.00} at {Actions.Fmt(pos)}"));
        });
        Harness.Register("findbeast", "findbeast [r=30]: take the nearest tamed creature as the beast", a =>
        {
            var here = P.transform.position;
            beast = Character.GetAllCharacters().Where(c => !c.IsPlayer() && c.IsTamed() && Vector3.Distance(here, c.transform.position) < Opt(a, 0, 30f))
                .OrderBy(c => Vector3.Distance(here, c.transform.position)).FirstOrDefault() ?? throw new System.ArgumentException("no tamed creature near");
            Harness.Log(Inv($"beast = {beast.name}"));
        });
        Harness.Register("beastpos", "beastpos [gap=0.2]: put the beast back in front of the sled's handles", a =>
        {
            NeedSled();
            NeedBeast();
            PlaceBeast(Opt(a, 0, 0.2f));
        });
        Harness.Register("saddle", "saddle: puts a saddle on the beast (lox, asksvin, horse)", a =>
        {
            NeedBeast();
            var t = beast.GetComponent<Tameable>();
            if (!t || !t.m_saddle) throw new System.ArgumentException(beast.name + " takes no saddle");
            t.m_nview.InvokeRPC("AddSaddle");
        });
        Harness.Register("hitch", "hitch [alt]: walk to the sled's handles and press Use (alt: Shift+Use, Draft Animals' hitch) on them, as a player does (with an animal mod the nearest tame gets hitched)", Hitch);
        Harness.Register("useon", "useon <item>: gives the item and uses it on the beast, as from the hotbar while looking at it (Draft Animals' harness, food)", a =>
        {
            NeedBeast();
            var item = P.GetInventory().AddItem(a[0], 1, 1, 0, P.GetPlayerID(), P.GetPlayerName(), true) ?? throw new System.ArgumentException("no item " + a[0]);
            var used = beast.GetComponent<Tameable>().UseItem(P, item);
            Harness.Log(Inv($"used {a[0]} on {beast.name}: {used}"));
        });
        Harness.Register("load","load <item> <count>: puts cargo into the sled (the crates show as it fills up)", a =>
        {
            NeedSled();
            var vagon = sled.GetComponent<Vagon>();
            vagon.m_container.GetInventory().AddItem(a[0], Harness.I(a[1]), 1, 0, 0L, "", true);
            vagon.UpdateLoadVisualization();
            Harness.Log(Inv($"cargo {vagon.m_container.GetInventory().GetTotalWeight():0}"));
        });
        Harness.Register("addcomp","addcomp <Type, Assembly>: adds another mod's component to the sled (what an integration would add to the prefab)", a =>
        {
            NeedSled();
            var type = System.Type.GetType(string.Join(" ", a)) ?? throw new System.ArgumentException("no type " + string.Join(" ", a));
            sled.AddComponent(type);
            Harness.Log("added " + type.FullName);
        });
        Harness.Register("bench","bench: sit on the vehicle's interactable named da_seat (Draft Animals' driver's bench)", Bench);
        Harness.Register("parts", "parts: the sled's components and its seats (what other mods added to it)", a =>
        {
            NeedSled();
            Harness.Log("components: " + string.Join(", ", sled.GetComponents<Component>().Select(c => c.GetType().Name)));
            foreach (var i in sled.GetComponentsInChildren<Interactable>(true).OfType<Component>())
                Harness.Log(Inv($"  interactable {i.GetType().Name} on {i.name} at local {Actions.Fmt(sled.transform.InverseTransformPoint(i.transform.position))}"));
            Harness.Log("joints: " + sled.GetComponents<ConfigurableJoint>().Length);
        });
        Harness.Register("pull", "pull [alt]: stand at the sled's handles facing its way and press Use: the player takes the harness (alt: Shift+Use, Beasts of Burden's \"pull it yourself\" with a tame nearby)", Pull);
        Harness.Register("unhitch","unhitch: press Use on the sled's handles again", Unhitch);
        Harness.Register("lead", "lead <dist> | lead stop | lead me: the beast walks to a point that far ahead of it (the sled's way), stops, or follows the player", a =>
        {
            NeedBeast();
            var ai = beast.GetComponent<MonsterAI>();
            if (leadMarker) Destroy(leadMarker);
            if (a[0] == "stop")
            {
                ai.SetFollowTarget(null);
                return;
            }
            if (a[0] == "me")
            {
                ai.SetFollowTarget(P.gameObject);
                return;
            }
            var fwd = sled ? Flat(sled.transform.forward) : Flat(beast.transform.forward);
            leadMarker = new GameObject("harness_lead");
            leadMarker.transform.position = beast.transform.position + fwd * Harness.F(a[0]);
            ai.SetFollowTarget(leadMarker);
            Harness.Log("leading to " + Actions.Fmt(leadMarker.transform.position));
        });
        Harness.Register("leadto", "leadto <x> <z>: the beast walks there", a =>
        {
            NeedBeast();
            if (leadMarker) Destroy(leadMarker);
            leadMarker = new GameObject("harness_lead");
            leadMarker.transform.position = Ground(new Vector3(Harness.F(a[0]), 0f, Harness.F(a[1])));
            beast.GetComponent<MonsterAI>().SetFollowTarget(leadMarker);
            Harness.Log("leading to " + Actions.Fmt(leadMarker.transform.position));
        });
        Harness.Register("sledat", "sledat <x> <z> <yaw>: the sled stands there, facing yaw; the beast (if any) in front of the handles", a =>
        {
            NeedSled();
            var pos = Ground(new Vector3(Harness.F(a[0]), 0f, Harness.F(a[1]))) + Vector3.up * 0.3f;
            var rot = Quaternion.Euler(0f, Harness.F(a[2]), 0f);
            var body = sled.GetComponent<Rigidbody>();
            // a far object is simulated by nobody until claimed (and its synced transform would undo the move)
            sled.GetComponent<ZNetView>().ClaimOwnership();
            sled.transform.SetPositionAndRotation(pos, rot);
            body.position = pos;
            body.rotation = rot;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            if (beast) PlaceBeast(0.2f);
        });
        Harness.Register("ride","ride: sit down on the sled", Ride);
        Harness.Register("mount", "mount: ride the saddled beast", Mount);
        Harness.Register("dismount", "dismount: get off whatever the player sits on", a => P.AttachStop());
        Harness.Register("sledrec", "sledrec <seconds> [step=0.5]: the sled/beast state line every step", Record);
        Harness.Register("sledshot", "sledshot <name> [yaw=35] [pitch=20] [dist=6] [height=0.8] [focus=sled|beast|mid]: a screenshot around the sled (yaw from its front)", a =>
            Actions.CameraShot(a[0], Focus(a.Length > 5 ? a[5] : "sled"), Opt(a, 1, 35f), Opt(a, 2, 20f), Opt(a, 3, 6f), Opt(a, 4, 0.8f)));
        // replaces the harness's own film: the camera can also follow the sled's beast, or the middle between the two
        Harness.Register("film", "film <name> <frames> [yaw0=35] [yaw1=yaw0] [pitch=15] [dist=7] [height=0.8] [focus=sled|beast|mid|game] [fps=30]: video frames, game time stepped at fps (shots/<name>/0000.jpg)", Film);
        Harness.AddState(State);
    }

    static void NeedSled()
    {
        if (!sled) throw new System.InvalidOperationException("no sled: run sled or findsled");
    }

    static void NeedBeast()
    {
        if (!beast) throw new System.InvalidOperationException("no beast: run beast or findbeast");
    }

    static Vector3 PlaceBeast(float gap)
    {
        var fwd = Flat(sled.transform.forward);
        var pos = Ground(sled.GetComponent<Vagon>().m_attachPoint.position + fwd * (beast.GetRadius() + gap));
        beast.m_nview.ClaimOwnership();
        beast.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd));
        beast.m_body.position = pos;
        beast.m_body.linearVelocity = Vector3.zero;
        return pos;
    }

    /// <summary>Log text with invariant numbers (the test machine's locale writes decimal commas).</summary>
    static string Inv(System.FormattableString s) => s.ToString(Harness.Inv);

    static Vector3 Ground(Vector3 p)
    {
        if (ZoneSystem.instance.GetSolidHeight(p, out float h)) p.y = Mathf.Max(h, ZoneSystem.instance.m_waterLevel);
        return p;
    }

    /// <summary>The sled's own idea of its puller; null with a Winter Sled build older than SledController.Puller (regression runs).</summary>
    static Character JointPuller(SledController ctl)
    {
        if (!ctl) return null;
        try
        {
            return PullerOf(ctl);
        }
        catch (System.MissingMethodException)
        {
            return null;
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static Character PullerOf(SledController ctl) => ctl.Puller();

    static Character AttachedTo(Vagon v) => v && v.IsAttached() && v.m_attachJoin && v.m_attachJoin.connectedBody ? v.m_attachJoin.connectedBody.GetComponent<Character>() : null;

    static IEnumerator Hitch(string[] a)
    {
        NeedSled();
        var vagon = sled.GetComponent<Vagon>();
        var fwd = Flat(sled.transform.forward);
        // next to the handles, out of the beast's way: Use reaches 2-3 m
        var pos = Ground(vagon.m_attachPoint.position - Vector3.Cross(fwd, Vector3.up) * 1.2f);
        if (beast == null || Vector3.Distance(P.transform.position, vagon.m_attachPoint.position) > 2.5f)
        {
            P.transform.position = pos;
            P.m_body.position = pos;
            yield return new WaitForSecondsRealtime(0.5f);
        }
        vagon.Interact(P, false, a.Length > 0 && a[0] == "alt");
        yield return new WaitForSecondsRealtime(1f);
        var to = AttachedTo(vagon) ?? JointPuller(sled.GetComponent<SledController>());
        Harness.Log(Inv($"hitched={vagon.IsAttached()} to={(to ? to.name : "-")} joints={sled.GetComponents<ConfigurableJoint>().Length} offset={Actions.Fmt(vagon.m_attachOffset)} detachDist={vagon.m_detachDistance:0.00}"));
    }

    static IEnumerator Bench(string[] a)
    {
        NeedSled();
        var seat = sled.GetComponentsInChildren<Interactable>(true).OfType<Component>().FirstOrDefault(c => c.name == "da_seat")
            ?? throw new System.InvalidOperationException("no da_seat on " + sled.name);
        var pos = seat.transform.position + Flat(sled.transform.right) * 1.2f;
        P.transform.position = pos;
        P.m_body.position = pos;
        yield return new WaitForSecondsRealtime(0.3f);
        ((Interactable)seat).Interact(P, false, false);
        yield return new WaitForSecondsRealtime(1f);
        Harness.Log(Inv($"bench: attached={P.IsAttached()} doodad={(P.GetDoodadController() != null ? P.GetDoodadController().GetType().Name : "-")}"));
    }

    static IEnumerator Pull(string[] a)
    {
        NeedSled();
        var vagon = sled.GetComponent<Vagon>();
        var fwd = Flat(sled.transform.forward);
        // where the vanilla offset (0, 0.8, 0) puts the player's feet: under the handles
        var pos = Ground(vagon.m_attachPoint.position - Vector3.up * 0.8f + fwd * 0.1f);
        var rot = Quaternion.LookRotation(fwd);
        P.transform.SetPositionAndRotation(pos, rot);
        P.m_body.position = pos;
        P.m_lookYaw = rot;
        P.SetLookDir(fwd);
        yield return new WaitForSecondsRealtime(0.4f);
        vagon.Interact(P, false, a.Length > 0 && a[0] == "alt");
        yield return new WaitForSecondsRealtime(1f);
        var to = AttachedTo(vagon);
        Harness.Log(Inv($"hitched={vagon.IsAttached()} to={(to ? to.name : "-")} offset={Actions.Fmt(vagon.m_attachOffset)} detachDist={vagon.m_detachDistance:0.00} feet-to-handles={Vector3.Distance(P.transform.position + vagon.m_attachOffset, vagon.m_attachPoint.position):0.00}"));
    }

    static IEnumerator Unhitch(string[] a)
    {
        NeedSled();
        var vagon = sled.GetComponent<Vagon>();
        vagon.Interact(P, false, false);
        yield return new WaitForSecondsRealtime(1f);
        Harness.Log(Inv($"hitched={vagon.IsAttached()} offset={Actions.Fmt(vagon.m_attachOffset)} detachDist={vagon.m_detachDistance:0.00}"));
    }

    static IEnumerator Ride(string[] a)
    {
        NeedSled();
        var seat = sled.GetComponentInChildren<SledSeat>();
        var pos = seat.m_attachPoint.position + Flat(sled.transform.right) * 1.2f;
        P.transform.position = pos;
        P.m_body.position = pos;
        yield return new WaitForSecondsRealtime(0.3f);
        seat.Interact(P, false, false);
        yield return new WaitForSecondsRealtime(0.8f);
        Harness.Log(Inv($"riding={seat.IsLocalRider()} attached={P.IsAttached()}"));
    }

    static IEnumerator Mount(string[] a)
    {
        NeedBeast();
        var saddle = beast.GetComponentInChildren<Sadle>(true);
        if (!saddle || !saddle.gameObject.activeInHierarchy) throw new System.InvalidOperationException("no saddle on " + beast.name);
        var pos = beast.transform.position + Flat(beast.transform.right) * (beast.GetRadius() + 0.8f);
        P.transform.position = pos;
        P.m_body.position = pos;
        yield return new WaitForSecondsRealtime(0.3f);
        saddle.Interact(P, false, false);
        yield return new WaitForSecondsRealtime(1f);
        Harness.Log(Inv($"mounted={P.IsAttached()} doodad={(P.GetDoodadController() != null ? P.GetDoodadController().GetType().Name : "-")}"));
    }

    static IEnumerator Record(string[] a)
    {
        float until = Time.realtimeSinceStartup + Harness.F(a[0]);
        float step = Opt(a, 1, 0.5f);
        while (Time.realtimeSinceStartup < until)
        {
            Harness.Log("rec " + State());
            yield return new WaitForSecondsRealtime(step);
        }
    }

    static string State()
    {
        if (!sled) return "sled -";
        var vagon = sled.GetComponent<Vagon>();
        var ctl = sled.GetComponent<SledController>();
        var body = sled.GetComponent<Rigidbody>();
        var v = body.linearVelocity;
        var to = AttachedTo(vagon) ?? JointPuller(ctl);
        var s = Inv($"sled pos={Actions.Fmt(sled.transform.position)} speed={v.magnitude:0.00} along={Vector3.Dot(v, sled.transform.forward):0.00} side={Vector3.Dot(v, sled.transform.right):0.00}")
            + Inv($" pitch={Mathf.DeltaAngle(0f, sled.transform.eulerAngles.x):0} roll={Mathf.DeltaAngle(0f, sled.transform.eulerAngles.z):0}")
            + Inv($" surface={(ctl ? ctl.ShownSurface.ToString() : "-")} pulled={(to ? to.name : "-")} rider={(ctl && ctl.HasRider())} resting={(ctl && ctl.m_resting)} hp={sled.GetComponent<ZNetView>().GetZDO().GetFloat(ZDOVars.s_health, -1f):0}");
        if (beast)
        {
            var bv = beast.m_body.linearVelocity;
            s += Inv($" | beast {beast.name} pos={Actions.Fmt(beast.transform.position)} speed={new Vector2(bv.x, bv.z).magnitude:0.00} hp={beast.GetHealth():0} rider={beast.GetComponent<Tameable>()?.HaveRider()}")
                + Inv($" gap={Vector3.Distance(beast.transform.position + vagon.m_attachOffset, vagon.m_attachPoint.position):0.00}")
                + Inv($" feet={(beast.m_lastGroundCollider ? beast.m_lastGroundCollider.name : "-")} slip={beast.m_slipping}")
                // 0 = the animal looks the vehicle's way (pulling), 180 = it faces the vehicle
                + Inv($" facing={Mathf.Abs(Mathf.DeltaAngle(beast.transform.eulerAngles.y, sled.transform.eulerAngles.y)):0}");
        }
        if (P)
        {
            var pv = P.m_body.linearVelocity;
            s += Inv($" | player speed={new Vector2(pv.x, pv.z).magnitude:0.00} attached={P.IsAttached()} haul={SledController.LocalPulled != null}");
        }
        return s;
    }

    /// <summary>Where the camera looks: the sled, the beast, or between them.</summary>
    static Transform Focus(string what)
    {
        if (what == "beast")
        {
            NeedBeast();
            return beast.transform;
        }
        NeedSled();
        if (what != "mid" || !beast) return sled.transform;
        var mid = new GameObject("harness_focus").transform;
        mid.SetPositionAndRotation((sled.transform.position + beast.transform.position) / 2f, sled.transform.rotation);
        Destroy(mid.gameObject, 1f);
        return mid;
    }

    /// <summary>
    /// Frames for a video. Unity's capture frame rate steps game time by exactly 1/fps per frame however long saving a
    /// frame takes, so the motion stays smooth. The camera stays locked to the focus and turns from yaw0 to yaw1.
    /// </summary>
    static IEnumerator Film(string[] a)
    {
        string name = a[0];
        int frames = Harness.I(a[1]);
        float yaw0 = Opt(a, 2, 35f), yaw1 = Opt(a, 3, yaw0), pitch = Opt(a, 4, 15f), dist = Opt(a, 5, 7f), height = Opt(a, 6, 0.8f);
        string focus = a.Length > 7 ? a[7] : "sled";
        int fps = a.Length > 8 ? Harness.I(a[8]) : 30;
        if (focus != "game") NeedSled();
        var dir = Path.Combine(Path.Combine(Harness.Dir, "shots"), name);
        Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "*.jpg")) File.Delete(f);
        var gc = GameCamera.instance;
        var cam = gc.m_camera;
        int oldRate = Time.captureFramerate;
        Time.captureFramerate = fps;
        if (focus != "game") gc.enabled = false;
        try
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                if (focus != "game")
                {
                    var t = focus == "beast" && beast ? beast.transform : sled.transform;
                    var center = (focus == "mid" && beast ? (sled.transform.position + beast.transform.position) / 2f : t.position) + Vector3.up * height;
                    float yaw = Mathf.Lerp(yaw0, yaw1, frames > 1 ? i / (frames - 1f) : 0f);
                    // the sled's heading, not its pitch and roll: the camera doesn't bob with every bump
                    var look = Quaternion.Euler(pitch, sled.transform.eulerAngles.y + 180f + yaw, 0f) * Vector3.forward;
                    cam.transform.position = center - look * dist;
                    cam.transform.rotation = Quaternion.LookRotation(look);
                }
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(dir, $"{i:0000}.jpg"), tex.EncodeToJPG(90));
                Destroy(tex);
                if (i % fps == 0) Harness.Log("film " + i + " " + State());
            }
        }
        finally
        {
            Time.captureFramerate = oldRate;
            gc.enabled = true;
        }
        Harness.Log(Inv($"film {name} x{frames} at {fps} fps"));
    }
}
