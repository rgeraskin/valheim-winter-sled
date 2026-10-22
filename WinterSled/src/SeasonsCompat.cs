using System;
using System.Reflection;
using BepInEx.Bootstrap;
using UnityEngine;

namespace WinterSled
{
    /// <summary>
    /// Optional support for shudnal's Seasons, found by reflection so the sled neither needs nor
    /// references it. Seasons paints the ground of a biome as another biome for the season (in winter
    /// Meadows, Black Forest, Swamp, Plains and Mistlands look like Mountain, i.e. snow); the sled
    /// treats the ground as whatever it looks like. Its frozen ocean ("IceSurface") is already ice
    /// by name.
    /// </summary>
    internal static class SeasonsCompat
    {
        public const string Guid = "shudnal.Seasons";

        private static bool s_ready;
        private static FieldInfo s_seasonState;          // Seasons.Seasons.seasonState
        private static MethodInfo s_getCurrentSeason;    // SeasonState.GetCurrentSeason()
        private static PropertyInfo s_isActive;          // SeasonState.IsActive
        private static MethodInfo s_useTextures;         // Seasons.Seasons.UseTextureControllers()
        private static MethodInfo s_hasBiomeOverride;    // Heightmap_GetBiomeColor_TerrainColor.HasBiomeOverride(Biome, Season, out Biome)

        private static object s_season;
        private static float s_seasonTime = -1f;

        public static bool Installed { get; private set; }

        public static void Init()
        {
            if (!Chainloader.PluginInfos.TryGetValue(Guid, out var info) || info.Instance == null)
            {
                return;
            }
            Installed = true;
            try
            {
                var asm = info.Instance.GetType().Assembly;
                var seasons = asm.GetType("Seasons.Seasons");
                var state = asm.GetType("Seasons.SeasonState");
                var terrain = asm.GetType("Seasons.Heightmap_GetBiomeColor_TerrainColor");
                const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
                s_seasonState = seasons?.GetField("seasonState", any);
                s_getCurrentSeason = state?.GetMethod("GetCurrentSeason", any, null, Type.EmptyTypes, null);
                s_isActive = state?.GetProperty("IsActive", any);
                s_useTextures = seasons?.GetMethod("UseTextureControllers", any, null, Type.EmptyTypes, null);
                s_hasBiomeOverride = terrain?.GetMethod("HasBiomeOverride", any);
                s_ready = s_seasonState != null && s_getCurrentSeason != null && s_hasBiomeOverride != null;
                Plugin.Log.LogInfo(s_ready
                    ? $"Seasons {info.Metadata.Version} found: snow follows the seasons"
                    : $"Seasons {info.Metadata.Version} found, but its internals changed; seasonal snow is ignored");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Seasons support disabled: {e.Message}");
            }
        }

        /// <summary>The biome the ground at this biome looks like this season (the biome itself when no seasons mod changes it).</summary>
        public static Heightmap.Biome LooksLike(Heightmap.Biome biome)
        {
            if (!s_ready && !SeasonalityCompat.Installed)
            {
                return biome;
            }
            // asked for every runner contact each physics step; the answer changes with the season only
            if (Time.time - s_looksTime > 2f)
            {
                s_looksTime = Time.time;
                s_looks.Clear();
            }
            if (!s_looks.TryGetValue(biome, out var looks))
            {
                looks = LooksLikeNow(biome);
                s_looks[biome] = looks;
            }
            return looks;
        }

        private static float s_looksTime = float.MinValue;
        private static readonly System.Collections.Generic.Dictionary<Heightmap.Biome, Heightmap.Biome> s_looks =
            new System.Collections.Generic.Dictionary<Heightmap.Biome, Heightmap.Biome>();

        private static Heightmap.Biome LooksLikeNow(Heightmap.Biome biome)
        {
            biome = SeasonalityCompat.LooksLike(biome);
            if (!s_ready)
            {
                return biome;
            }
            try
            {
                if (s_isActive != null && !(bool)s_isActive.GetValue(null))
                {
                    return biome;
                }
                if (s_useTextures != null && !(bool)s_useTextures.Invoke(null, null))
                {
                    // the ground isn't repainted, so no seasonal snow is shown
                    return biome;
                }
                var season = CurrentSeason();
                if (season == null)
                {
                    return biome;
                }
                var args = new object[] { biome, season, null };
                if ((bool)s_hasBiomeOverride.Invoke(null, args) && args[2] != null)
                {
                    return (Heightmap.Biome)Convert.ToInt32(args[2]);
                }
            }
            catch (Exception e)
            {
                s_ready = false;
                Plugin.Log.LogWarning($"Seasons support disabled after an error: {e.Message}");
            }
            return biome;
        }

        private static object CurrentSeason()
        {
            // the season changes once a game day; asking a few times a second is plenty
            if (Time.time - s_seasonTime > 2f || s_season == null)
            {
                s_seasonTime = Time.time;
                var state = s_seasonState.GetValue(null);
                s_season = state != null ? s_getCurrentSeason.Invoke(state, null) : null;
            }
            return s_season;
        }

        public static string CurrentSeasonName()
        {
            return s_ready ? CurrentSeason()?.ToString() : null;
        }
    }
}

namespace WinterSled
{
    /// <summary>
    /// Optional support for RustyMods' Seasonality, by reflection too. Its winter paints Meadows, Swamp,
    /// Black Forest, Plains and Mistlands as Mountain (hard-coded in its Heightmap.GetBiomeColor patch);
    /// its frozen water is a solid collider on the Water layer, which SurfaceProbe already calls ice.
    /// </summary>
    internal static class SeasonalityCompat
    {
        public const string Guid = "RustyMods.Seasonality";

        private static System.Reflection.PropertyInfo s_value;   // ConfigEntry<Season>.Value
        private static object s_entry;                          // Seasonality.Configs.m_season

        public static bool Installed => s_entry != null;

        public static void Init()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(Guid, out var info) || info.Instance == null)
            {
                return;
            }
            try
            {
                var configs = info.Instance.GetType().Assembly.GetType("Seasonality.Configs");
                var field = configs?.GetField("m_season", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                s_entry = field?.GetValue(null);
                s_value = s_entry?.GetType().GetProperty("Value");
                if (s_value == null)
                {
                    s_entry = null;
                }
                Plugin.Log.LogInfo(Installed
                    ? $"Seasonality {info.Metadata.Version} found: snow follows the seasons"
                    : $"Seasonality {info.Metadata.Version} found, but its internals changed; seasonal snow is ignored");
            }
            catch (Exception e)
            {
                s_entry = null;
                Plugin.Log.LogWarning($"Seasonality support disabled: {e.Message}");
            }
        }

        public static Heightmap.Biome LooksLike(Heightmap.Biome biome)
        {
            if (!Installed)
            {
                return biome;
            }
            try
            {
                // Seasonality.Season: Summer, Fall, Winter, Spring
                if (s_value.GetValue(s_entry)?.ToString() != "Winter")
                {
                    return biome;
                }
            }
            catch (Exception)
            {
                s_entry = null;
                return biome;
            }
            return WinterLooksLike(biome);
        }

        /// <summary>Seasonality's winter repaint (its GetBiomeColor patch): five biomes turn to Mountain snow.</summary>
        public static Heightmap.Biome WinterLooksLike(Heightmap.Biome biome)
        {
            switch (biome)
            {
                case Heightmap.Biome.Meadows:
                case Heightmap.Biome.Swamp:
                case Heightmap.Biome.BlackForest:
                case Heightmap.Biome.Plains:
                case Heightmap.Biome.Mistlands:
                    return Heightmap.Biome.Mountain;
                default:
                    return biome;
            }
        }
    }
}
