using System;
using System.Collections.Generic;
using UnityEngine;

namespace WinterSled
{
    internal enum Surface
    {
        Ground,
        FrozenGround,
        Snow,
        Powder,
        Ice,
        Wood,
        Stone,
        Water,
    }

    /// <summary>Works out what the runners are sliding on from the collider they touch.</summary>
    internal static class SurfaceProbe
    {
        // Deep North snow depth is stored in the paint mask's green channel, scaled like Character does.
        private const float DeepSnowMaxHeight = 2f;
        // WearNTear shows snow on a piece once its buildup passes this.
        private const float SnowyPiece = 0.25f;
        private static readonly int s_waterLayer = LayerMask.NameToLayer("Water");

        private enum Kind
        {
            Ice,
            Terrain,
            Piece,
            Other,
        }

        private struct ColliderInfo
        {
            public Kind Kind;
            public Heightmap Heightmap;
            public WearNTear Piece;
        }

        // Classify runs for every runner contact each physics step: what a collider is (by name and parents)
        // doesn't change, so it is worked out once per collider
        private static readonly Dictionary<int, ColliderInfo> s_colliders = new Dictionary<int, ColliderInfo>();

        public static Surface Classify(Collider collider, Vector3 point, Vector3 normal)
        {
            if (collider == null)
            {
                return Surface.Ground;
            }
            var info = Lookup(collider);
            if (info.Kind == Kind.Ice)
            {
                return Surface.Ice;
            }
            if (info.Kind == Kind.Terrain)
            {
                return ClassifyTerrain(info.Heightmap, point, normal);
            }
            if (info.Kind == Kind.Piece)
            {
                var piece = info.Piece;
                if (PieceSnow(piece) > SnowyPiece)
                {
                    return Surface.Snow;
                }
                switch (piece.m_materialType)
                {
                    case WearNTear.MaterialType.Wood:
                    case WearNTear.MaterialType.HardWood:
                    case WearNTear.MaterialType.Timberwood:
                        return Surface.Wood;
                    case WearNTear.MaterialType.Ice:
                        return Surface.Ice;
                    default:
                        return Surface.Stone;
                }
            }
            // rocks, tree stumps and other props: rough and grippy
            return Surface.Stone;
        }

        private static ColliderInfo Lookup(Collider collider)
        {
            int id = collider.GetInstanceID();
            if (s_colliders.TryGetValue(id, out var info)
                && (info.Kind == Kind.Ice || info.Kind == Kind.Other || info.Heightmap != null || info.Piece != null))
            {
                return info;
            }
            if (s_colliders.Count > 2048)
            {
                // zones come and go; forget the ones left behind
                s_colliders.Clear();
            }
            // ice by name (ice floes, Seasons' frozen ocean "IceSurface"), or a solid water surface (Seasonality's frozen water)
            if (IsIce(collider.transform) || (!collider.isTrigger && collider.gameObject.layer == s_waterLayer))
            {
                info = new ColliderInfo { Kind = Kind.Ice };
            }
            else if (collider.GetComponentInParent<Heightmap>() is Heightmap heightmap)
            {
                info = new ColliderInfo { Kind = Kind.Terrain, Heightmap = heightmap };
            }
            else if (collider.GetComponentInParent<WearNTear>() is WearNTear piece)
            {
                info = new ColliderInfo { Kind = Kind.Piece, Piece = piece };
            }
            else
            {
                info = new ColliderInfo { Kind = Kind.Other };
            }
            s_colliders[id] = info;
            return info;
        }

        public static Surface ClassifyTerrain(Heightmap heightmap, Vector3 point, Vector3 normal)
        {
            var biome = heightmap.GetBiome(point);
            // with Seasons or Seasonality, a meadow in winter is painted (and so ridden) as snow, and so on
            return ClassifyGround(biome, SeasonsCompat.LooksLike(biome), heightmap.GetPaintMask(point), normal.y);
        }

        /// <summary>
        /// The terrain rules on their own: the biome, the biome the season paints it as, the paint mask there
        /// (r dirt, g cultivated or Deep North snow depth, b paved) and the ground's normal.y.
        /// </summary>
        public static Surface ClassifyGround(Heightmap.Biome biome, Heightmap.Biome looksLike, Color paint, float normalY)
        {
            if (looksLike != Heightmap.Biome.Mountain && looksLike != Heightmap.Biome.DeepNorth)
            {
                return Surface.Ground;
            }
            // paved (b) paint clears the snow off a stone road; cliffs show bare rock
            if (paint.b > 0.5f || normalY < Mathf.Cos(Plugin.Cfg.CliffAngle.Value * Mathf.Deg2Rad))
            {
                return Surface.Stone;
            }
            // dirt (r) clears the snow too, but in the cold the earth under it is frozen hard
            if (paint.r > 0.5f)
            {
                return Surface.FrozenGround;
            }
            // powder depth is the Deep North's own (its paint mask), whatever the season
            if (biome == Heightmap.Biome.DeepNorth && paint.g * DeepSnowMaxHeight > Plugin.Cfg.PowderDepth.Value)
            {
                return Surface.Powder;
            }
            return Surface.Snow;
        }

        private static readonly int s_seasonsSnow = "Seasons_SeasonalSnowValue".GetStableHashCode();

        /// <summary>Snow lying on a building piece: the game's own, or Seasons' seasonal snow when that mod is used.</summary>
        private static float PieceSnow(WearNTear piece)
        {
            float snow = piece.m_snowBuildup;
            if (SeasonsCompat.Installed && piece.m_nview != null && piece.m_nview.IsValid())
            {
                snow = Mathf.Max(snow, piece.m_nview.GetZDO().GetFloat(s_seasonsSnow, 0f));
            }
            return snow;
        }

        private static readonly RaycastHit[] s_hits = new RaycastHit[16];

        /// <summary>What lies under a vehicle, looking straight down and skipping the vehicle's own colliders.</summary>
        public static bool ProbeBelow(Transform self, Vector3 origin, float distance, out Surface surface)
        {
            int n = Physics.RaycastNonAlloc(origin, Vector3.down, s_hits, distance, SledBuilder.GroundMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            RaycastHit hit = default;
            for (int i = 0; i < n; i++)
            {
                if (s_hits[i].collider.transform.IsChildOf(self) || s_hits[i].distance >= best)
                {
                    continue;
                }
                best = s_hits[i].distance;
                hit = s_hits[i];
            }
            surface = best < float.MaxValue ? Classify(hit.collider, hit.point, hit.normal) : Surface.Ground;
            return best < float.MaxValue;
        }

        public static bool IsSnowy(Surface surface)
        {
            return surface == Surface.Snow || surface == Surface.Powder || surface == Surface.Ice;
        }

        /// <summary>Ice floes, frozen lakes and ice rocks are named after ice (the same test Shield Sledding uses).</summary>
        private static bool IsIce(Transform t)
        {
            for (int depth = 0; depth < 6 && t != null; depth++, t = t.parent)
            {
                var name = t.name.ToLowerInvariant();
                if (name.Contains("vfx_") || name.Contains("fx_") || name.Contains("projectile"))
                {
                    return false;
                }
                if (name.StartsWith("ice", StringComparison.Ordinal) || name.Contains("_ice") || name.Contains("iceblocker") || name.Contains("ice_rock"))
                {
                    return true;
                }
            }
            return false;
        }

        public static float Friction(Surface surface)
        {
            var cfg = Plugin.Cfg;
            switch (surface)
            {
                case Surface.Snow: return cfg.SnowFriction.Value;
                case Surface.Powder: return cfg.PowderFriction.Value;
                case Surface.Ice: return cfg.IceFriction.Value;
                case Surface.Wood: return cfg.WoodFriction.Value;
                case Surface.Stone: return cfg.StoneFriction.Value;
                case Surface.Water: return 0f;
                case Surface.FrozenGround: return cfg.FrozenGroundFriction.Value;
                default: return cfg.GroundFriction.Value;
            }
        }

        public static float Grip(Surface surface)
        {
            return surface == Surface.Ice ? Plugin.Cfg.IceGrip.Value : Plugin.Cfg.RunnerGrip.Value;
        }

        public static string LocalizationKey(Surface surface)
        {
            return surface == Surface.FrozenGround ? "$wintersled_surface_frozen" : "$wintersled_surface_" + surface.ToString().ToLowerInvariant();
        }
    }
}
