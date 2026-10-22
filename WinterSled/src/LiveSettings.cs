using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace WinterSled
{
    /// <summary>
    /// Settings read once into the prefab (mass, crash speeds, recipe) are pushed into the prefab and into
    /// every sled already in the world when they change, so they can be tuned in the game (Configuration
    /// Manager, or the server's synced values) without a restart.
    /// </summary>
    internal static class LiveSettings
    {
        public static void Hook()
        {
            var cfg = Plugin.Cfg;
            cfg.BaseMass.SettingChanged += (_, __) => ApplyBody();
            cfg.CrashMinSpeed.SettingChanged += (_, __) => ApplyBody();
            cfg.CrashMaxSpeed.SettingChanged += (_, __) => ApplyBody();
            cfg.Recipe.SettingChanged += (_, __) => ApplyRecipe();
            cfg.SeasonalRecipe.SettingChanged += (_, __) => ApplyRecipe();
            // A server's values are reset to the local ones while the world is torn down (no ObjectDB then):
            // apply the recipe again whenever the game's items are there.
            PrefabManager.OnPrefabsRegistered += ApplyRecipe;
            ItemManager.OnItemsRegistered += ApplyRecipe;
        }

        private static GameObject Prefab()
        {
            // Jötunn's registry, not ZNetScene's: it outlives the world, so a change while there is none
            // (logging out of a server) still reaches the prefab
            return PrefabManager.Instance.GetPrefab(SledBuilder.PrefabName);
        }

        private static void ApplyBody()
        {
            var prefab = Prefab();
            var targets = SledController.Instances.Select(s => s.gameObject);
            if (prefab != null)
            {
                targets = targets.Concat(new[] { prefab });
            }
            foreach (var go in targets)
            {
                if (go.TryGetComponent<Vagon>(out var vagon))
                {
                    vagon.m_baseMass = Plugin.Cfg.BaseMass.Value;
                    if (vagon.m_nview != null && vagon.m_nview.IsValid() && vagon.m_nview.IsOwner())
                    {
                        vagon.UpdateMass();
                    }
                }
                if (go.TryGetComponent<ImpactEffect>(out var impact))
                {
                    impact.m_minVelocity = Plugin.Cfg.CrashMinSpeed.Value;
                    impact.m_maxVelocity = Plugin.Cfg.CrashMaxSpeed.Value;
                }
            }
        }

        public static void ApplyRecipe()
        {
            var prefab = Prefab();
            if (prefab == null || ObjectDB.instance == null)
            {
                return;
            }
            var recipe = Plugin.Cfg.ActiveRecipe(out _);
            // no tuples: System.ValueTuple isn't in the game's runtime
            var reqs = new System.Collections.Generic.List<Piece.Requirement>();
            foreach (var r in Plugin.Cfg.ParseRecipe(recipe))
            {
                var item = ObjectDB.instance.GetItemPrefab(r.Key)?.GetComponent<ItemDrop>();
                if (item == null)
                {
                    Plugin.Log.LogWarning($"Recipe item '{r.Key}' doesn't exist, skipped");
                    continue;
                }
                reqs.Add(new Piece.Requirement { m_resItem = item, m_amount = r.Value, m_recover = true });
            }
            if (reqs.Count > 0)
            {
                prefab.GetComponent<Piece>().m_resources = reqs.ToArray();
                Plugin.Log.LogInfo("Sled recipe now: " + recipe);
            }
        }
    }
}
