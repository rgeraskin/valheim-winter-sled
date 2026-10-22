using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WinterSled
{
    /// <summary>
    /// The undercarriage is textured with the game's own materials, borrowed at runtime,
    /// so it matches Valheim's look (and its wet/snow shading) without shipping any textures.
    /// </summary>
    internal static class SledMaterials
    {
        private struct Source
        {
            public string Material;
            public string[] Prefabs;
        }

        private static readonly Dictionary<string, Source> Sources = new Dictionary<string, Source>
        {
            // dark stained wood: the dark/light halves of DarkWoodBeams_d are laid out in the OBJ's UVs
            { "wood", new Source { Material = "DarkWood_mat", Prefabs = new[] { "darkwood_beam", "darkwood_pole", "darkwood_beam_26" } } },
            // tiling iron of the iron cage pieces, for the runner shoes
            { "iron", new Source { Material = "metalwall", Prefabs = new[] { "iron_grate", "iron_wall_2x2", "iron_wall_1x1", "iron_floor_2x2" } } },
            // the cart's own rope
            { "rope", new Source { Material = "cart_rope", Prefabs = new[] { "Cart" } } },
            // wolf pelt rug; its alpha cut-out gives the pelt its outline
            { "fur", new Source { Material = "rug_wolf", Prefabs = new[] { "rug_wolf" } } },
        };

        public static Material[] Resolve(IList<string> names, Material fallback)
        {
            var result = new Material[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                result[i] = Find(names[i]) ?? fallback;
                if (result[i] == fallback)
                {
                    Plugin.Log.LogWarning($"No game material found for '{names[i]}', using {fallback?.name}");
                }
            }
            return result;
        }

        private static Material Find(string key)
        {
            if (!Sources.TryGetValue(key, out var src))
            {
                return null;
            }
            foreach (var prefabName in src.Prefabs)
            {
                var prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(prefabName) : null;
                if (prefab == null)
                {
                    continue;
                }
                var mat = prefab.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(r => r.sharedMaterials)
                    .FirstOrDefault(m => m != null && m.name.StartsWith(src.Material));
                if (mat != null)
                {
                    return mat;
                }
                if (prefab.TryGetComponent<LineRenderer>(out var line) && line.sharedMaterial != null && line.sharedMaterial.name.StartsWith(src.Material))
                {
                    return line.sharedMaterial;
                }
            }
            // not on the expected prefabs (renamed in a game update?): look through everything loaded
            return Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == src.Material);
        }
    }
}
