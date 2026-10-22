using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WinterSled
{
    /// <summary>
    /// Builds the sled from a clone of the vanilla cart: the cart keeps doing everything a cart
    /// does (harness, storage, mass from cargo, wear, floating), the wheels make way for runners.
    /// </summary>
    internal static class SledBuilder
    {
        public const string PrefabName = "WinterSled";

        // keep in sync with model/build_model.py
        public const float BodyDrop = 0.10f;
        public const float DeckTop = 0.582f - BodyDrop;
        private const float RunnerX = 0.70f;
        private const float RunnerRadius = 0.05f;

        private const string MeshResource = "WinterSled.sled_undercarriage.obj";

        public static int GroundMask { get; private set; }

        public static void Register()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;
            try
            {
                GroundMask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece", "Default_small", "vehicle");
                var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, "Cart");
                if (prefab == null)
                {
                    Plugin.Log.LogError("The vanilla Cart prefab is missing, the sled can't be built");
                    return;
                }
                Build(prefab);

                var icon = RenderIcon(prefab) ?? prefab.GetComponent<Piece>().m_icon;
                var config = new PieceConfig
                {
                    Name = "$piece_wintersled",
                    Description = "$piece_wintersled_desc",
                    PieceTable = PieceTables.Hammer,
                    Category = PieceCategories.Misc,
                    CraftingStation = CraftingStations.Workbench,
                    Icon = icon,
                };
                var recipe = Plugin.Cfg.ActiveRecipe(out bool seasonal);
                Plugin.Log.LogInfo($"Sled recipe ({(seasonal ? "seasons mod found" : "regular")}): {recipe}");
                foreach (var req in Plugin.Cfg.ParseRecipe(recipe))
                {
                    config.AddRequirement(new RequirementConfig(req.Key, req.Value, 0, true));
                }
                PieceManager.Instance.AddPiece(new CustomPiece(prefab, false, config));
                ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(SledHaulEffect.Create(icon), false));
                var cart = PrefabManager.Instance.GetPrefab("Cart");
                ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(CartSnowEffect.Create(cart != null ? cart.GetComponent<Piece>().m_icon : icon), false));
                Plugin.Log.LogInfo("Winter sled registered");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Building the sled failed: {e}");
            }
        }

        private static void Build(GameObject prefab)
        {
            var root = prefab.transform;
            var vagon = prefab.GetComponent<Vagon>();

            // wheels out (their joints and audio go with them)
            foreach (var wheel in vagon.m_wheels.Where(w => w != null))
            {
                Object.DestroyImmediate(wheel.gameObject);
            }
            vagon.m_wheels = Array.Empty<Rigidbody>();

            // a sled sits lower than a cart on wheels
            foreach (Transform child in root)
            {
                child.localPosition += Vector3.down * BodyDrop;
            }
            // the harness point at a standing player's hip height when the sled is level,
            // so pulling doesn't lift the front of the runners off the snow
            vagon.m_attachPoint.localPosition = new Vector3(0f, 0.8f, vagon.m_attachPoint.localPosition.z + 0.05f);

            vagon.m_name = "$piece_wintersled";
            vagon.m_baseMass = Plugin.Cfg.BaseMass.Value;
            vagon.m_slipperyCollider = null;
            // the cargo crates start hidden (the icon would show them); Vagon shows them as the sled fills up
            foreach (var load in vagon.m_loadVis)
            {
                load.m_gameobject.SetActive(false);
            }

            var piece = prefab.GetComponent<Piece>();
            piece.m_allowedInDeepSnow = true;

            var wear = prefab.GetComponent<WearNTear>();
            if (wear != null && wear.m_fragmentRoots != null)
            {
                wear.m_fragmentRoots = wear.m_fragmentRoots.Where(r => r != null).ToArray();
            }

            // sleds go fast downhill: only real crashes should hurt it
            var impact = prefab.GetComponent<ImpactEffect>();
            if (impact != null)
            {
                impact.m_minVelocity = Plugin.Cfg.CrashMinSpeed.Value;
                impact.m_maxVelocity = Plugin.Cfg.CrashMaxSpeed.Value;
            }

            int visualLayer = prefab.GetComponentsInChildren<MeshRenderer>(true).Select(r => r.gameObject.layer).FirstOrDefault();
            var undercarriage = BuildUndercarriage(root, visualLayer, vagon);
            AddToLods(prefab, undercarriage);
            BuildRunnerColliders(root, vagon);
            BuildSeat(root, vagon);

            prefab.AddComponent<SledController>();
            prefab.AddComponent<SledAudio>();
        }

        private static Renderer BuildUndercarriage(Transform root, int layer, Vagon vagon)
        {
            var mesh = ObjMesh.Load(MeshResource, out var materialNames);
            var go = new GameObject("sled_undercarriage") { layer = layer };
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var fallback = root.GetComponentsInChildren<MeshRenderer>(true).Select(r => r.sharedMaterial).FirstOrDefault(m => m != null);
            renderer.sharedMaterials = SledMaterials.Resolve(materialNames, fallback);
            Plugin.Log.LogInfo("Undercarriage materials: " + string.Join(", ", materialNames.Zip(renderer.sharedMaterials, (n, m) => $"{n}={m?.name}")));
            return renderer;
        }

        private static void AddToLods(GameObject prefab, Renderer renderer)
        {
            var lodGroup = prefab.GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                return;
            }
            var lods = lodGroup.GetLODs();
            if (lods.Length == 0)
            {
                return;
            }
            lods[0].renderers = lods[0].renderers.Where(r => r != null).Concat(new[] { renderer }).ToArray();
            for (int i = 1; i < lods.Length; i++)
            {
                lods[i].renderers = lods[i].renderers.Where(r => r != null).ToArray();
            }
            lodGroup.SetLODs(lods);
        }

        private static void BuildRunnerColliders(Transform root, Vagon vagon)
        {
            var parent = new GameObject("sled_runners");
            // same layer as the cart's own body colliders, so it collides with what the cart did
            var bodyCollider = root.Find("Vagon")?.GetComponent<Collider>();
            parent.layer = bodyCollider != null ? bodyCollider.gameObject.layer : root.gameObject.layer;
            parent.transform.SetParent(root, false);
            foreach (var x in new[] { -RunnerX, RunnerX })
            {
                string side = x < 0 ? "left" : "right";
                // the flat of the runner
                AddCapsule(parent.transform, $"{SledController.RunnerPrefix}{side}", new Vector3(x, RunnerRadius, 0.02f), 2.12f, Quaternion.identity);
                // the curl rides up over bumps and steps
                var a = new Vector3(x, RunnerRadius + 0.01f, 1.0f);
                var b = new Vector3(x, 0.36f, 1.24f);
                AddCapsule(parent.transform, $"{SledController.RunnerPrefix}{side}_curl", (a + b) / 2f, Vector3.Distance(a, b) + 2f * RunnerRadius, Quaternion.LookRotation(b - a));
                // the short upturn at the back
                var c = new Vector3(x, RunnerRadius, -1.0f);
                var d = new Vector3(x, 0.11f, -1.16f);
                AddCapsule(parent.transform, $"{SledController.RunnerPrefix}{side}_tail", (c + d) / 2f, Vector3.Distance(c, d) + 2f * RunnerRadius, Quaternion.LookRotation(d - c));
                // where snow sprays from
                var tail = new GameObject("runner_tail");
                tail.transform.SetParent(parent.transform, false);
                tail.transform.localPosition = new Vector3(x, 0.05f, -1.15f);
            }
        }

        private static void AddCapsule(Transform parent, string name, Vector3 center, float length, Quaternion rotation)
        {
            var go = new GameObject(name) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = rotation;
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.direction = 2;
            capsule.radius = RunnerRadius;
            capsule.height = length;
        }

        private static void BuildSeat(Transform root, Vagon vagon)
        {
            // the storage's hover box covered the whole bed; the back part is now the seat
            if (vagon.m_container != null)
            {
                var c = vagon.m_container.transform;
                c.localPosition += new Vector3(0f, 0f, 0.25f);
                c.localScale = Vector3.Scale(c.localScale, new Vector3(1f, 1f, 0.72f));
            }

            var seat = new GameObject("sled_seat") { layer = vagon.m_container != null ? vagon.m_container.gameObject.layer : root.gameObject.layer };
            seat.transform.SetParent(root, false);
            seat.transform.localPosition = new Vector3(0f, DeckTop + 0.33f, -0.66f);
            var box = seat.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.2f, 0.4f, 0.62f);

            // where the rider's feet-level root goes: on the pelt, facing the shafts
            var attach = new GameObject("attach");
            attach.transform.SetParent(seat.transform, false);
            attach.transform.position = root.TransformPoint(new Vector3(0f, DeckTop + 0.02f, -0.58f));

            var comp = seat.AddComponent<SledSeat>();
            comp.m_attachPoint = attach.transform;
        }

        private static Sprite RenderIcon(GameObject prefab)
        {
            try
            {
                // three-quarter view from the front left, looking down a little
                var rotation = Quaternion.AngleAxis(25f, Vector3.right) * Quaternion.Euler(0f, 145f, 0f);
                return RenderManager.Instance.Render(new RenderManager.RenderRequest(prefab)
                {
                    Rotation = rotation,
                    Width = 128,
                    Height = 128,
                    UseCache = false,
                });
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Icon render failed, using the cart's icon: {e.Message}");
                return null;
            }
        }
    }
}
