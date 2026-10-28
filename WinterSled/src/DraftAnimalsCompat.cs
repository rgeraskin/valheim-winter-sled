using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WinterSled
{
    /// <summary>
    /// Optional support for Draft Animals (4gestr / Fionov Alexander), found by reflection so the sled neither needs nor
    /// references it. Draft Animals fits the vanilla cart with a hitch for a harnessed animal and a driver's bench, but
    /// only the "Cart" prefab, and only once the scene is up: long after the sled was cloned from it. So the sled gets
    /// the same two parts here, right after Draft Animals has fitted the cart. Other animal mods (Beasts of Burden,
    /// OdinHorse) hitch any vehicle and need nothing.
    /// </summary>
    internal static class DraftAnimalsCompat
    {
        public const string Guid = "draftanimals";
        // Draft Animals' names: its hitch component and the bench it adds to the cart
        private const string HitchTypeName = "DraftAnimals.CartHitch";
        private const string SeatName = "da_seat";

        private static Type s_hitchType;
        private static PropertyInfo s_isHitched; // CartHitch.IsHitched (from the vehicle's ZDO, so every peer knows)

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
                s_hitchType = info.Instance.GetType().Assembly.GetType(HitchTypeName);
                s_isHitched = s_hitchType?.GetProperty("IsHitched", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                Plugin.Log.LogInfo(s_hitchType != null && s_isHitched != null
                    ? $"Draft Animals {info.Metadata.Version} found: harnessed animals pull the sled too"
                    : $"Draft Animals {info.Metadata.Version} found, but its internals changed; it can't hitch the sled");
            }
            catch (Exception e)
            {
                s_hitchType = null;
                Plugin.Log.LogWarning($"Draft Animals support disabled: {e.Message}");
            }
        }

        private static bool Ready => s_hitchType != null && s_isHitched != null;

        /// <summary>Draft Animals' hitch on this vehicle, or null (not installed, or not fitted).</summary>
        public static Component HitchOf(GameObject vehicle)
        {
            return Ready && vehicle != null ? vehicle.GetComponent(s_hitchType) : null;
        }

        /// <summary>Whether a harnessed animal is hitched to this hitch (on any peer: Draft Animals keeps it in the ZDO).</summary>
        public static bool IsHitched(Component hitch)
        {
            if (hitch == null || !Ready)
            {
                return false;
            }
            try
            {
                return (bool)s_isHitched.GetValue(hitch, null);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Gives the sled the hitch and the bench Draft Animals gave the cart (the bench lower, as the sled's body is).</summary>
        public static void FitSled(ZNetScene scene)
        {
            if (!Ready || scene == null)
            {
                return;
            }
            try
            {
                var sled = PrefabManager.Instance.GetPrefab(SledBuilder.PrefabName);
                var cart = scene.GetPrefab("Cart");
                if (sled == null || cart == null || cart.GetComponent(s_hitchType) == null || sled.GetComponent(s_hitchType) != null)
                {
                    return;
                }
                var bench = cart.transform.Find(SeatName);
                if (bench != null)
                {
                    // the seat finds the hitch on its parent when it wakes up, so it goes in first
                    var copy = Object.Instantiate(bench.gameObject, sled.transform, false);
                    copy.name = SeatName;
                    copy.transform.localPosition = bench.localPosition + Vector3.down * SledBuilder.BodyDrop;
                }
                sled.AddComponent(s_hitchType);
                Plugin.Log.LogInfo("Draft Animals: the sled has a hitch" + (bench != null ? " and a driver's bench" : ""));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Draft Animals: fitting the sled failed: {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    [HarmonyAfter(DraftAnimalsCompat.Guid)]
    internal static class FitSledForDraftAnimals
    {
        private static void Postfix(ZNetScene __instance) => DraftAnimalsCompat.FitSled(__instance);
    }
}
