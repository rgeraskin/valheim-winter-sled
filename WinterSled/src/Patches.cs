using HarmonyLib;
using UnityEngine;

namespace WinterSled
{
    [HarmonyPatch(typeof(Vagon), nameof(Vagon.UpdateLoadVisualization))]
    internal static class HideLoadUnderRider
    {
        // The rider sits at the back with the legs forward, over the crates in the back and middle of the bed: those
        // would poke through, so they hide while someone rides. The ones in the front stay, so the load still shows.
        private const float FrontOfRider = 0.3f;

        private static void Postfix(Vagon __instance)
        {
            // sleds and carts alike: both have a seat
            var seat = __instance.GetComponentInChildren<SledSeat>();
            if (seat == null || !seat.HaveValidRider())
            {
                return;
            }
            foreach (var load in __instance.m_loadVis)
            {
                if (__instance.transform.InverseTransformPoint(load.m_gameobject.transform.position).z < FrontOfRider)
                {
                    load.m_gameobject.SetActive(false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Floating), nameof(Floating.CustomFixedUpdate))]
    internal static class NoFloatingOnIce
    {
        // Frozen water (Seasons' frozen sea) sits at the water level, where Floating would buoy the sled up
        // and damp it as if it were swimming. While the runners stand on something solid, it isn't floating.
        private static bool Prefix(Floating __instance)
        {
            if (__instance.TryGetComponent<SledController>(out var sled) && sled.OnSolidGround)
            {
                // the ripples Floating would have switched off itself
                __instance.SetSurfaceEffect(false);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.UpdateWalking))]
    internal static class SureFootedWhilePulling
    {
        // Where the game takes the legs' control away on ice (Deep North ice; with Seasons, frozen water shallower than
        // 4 m), the sled on the harness drags the puller to a crawl, while elsewhere the same-looking ice runs normally.
        // Pulling a sled there, the puller keeps their footing: the game's own "ice shoes" flag, held for this step.
        // Ice where the legs keep control (Seasonality's, Seasons' deep water) is left alone, gliding and all.
        private static void Prefix(Character __instance, ref bool __state)
        {
            __state = __instance == Player.m_localPlayer && !__instance.m_iceShoes && SledController.LocalPulled != null
                && Plugin.Cfg.SureFootedPulling.Value && LosesFooting(__instance.m_lastGroundCollider);
            if (__state)
            {
                __instance.m_iceShoes = true;
            }
        }

        private static void Postfix(Character __instance, bool __state)
        {
            if (__state)
            {
                __instance.m_iceShoes = false;
            }
        }

        private static bool LosesFooting(Collider ground)
        {
            if (ground == null)
            {
                return false;
            }
            // Seasons' frozen sea: Seasons itself decides by the depth (and leaves deep water's legs alone either way)
            if (ground.name == "IceSurface")
            {
                return true;
            }
            // the game's own test in Character.ApplySlippery, quirk included: it masks with the layer's number, not
            // its bit, so a frozen Water layer (Seasonality) never loses footing
            var material = ground.sharedMaterial;
            return material != null && material.dynamicFriction <= 0f && (Character.s_slipperyStuffMask & ground.gameObject.layer) != 0;
        }
    }

    [HarmonyPatch(typeof(ImpactEffect), nameof(ImpactEffect.OnCollisionEnter))]
    internal static class CrashOnlyHeadOn
    {
        // The vanilla crash test takes the whole relative speed, so a sled sliding fast over snow "crashes" into the
        // ground at every bump and wears itself down on a long ride. Only the speed into what it hits counts.
        private static bool Prefix(ImpactEffect __instance, Collision info)
        {
            if (info.contactCount == 0 || !__instance.TryGetComponent<SledController>(out _))
            {
                return true;
            }
            float into = 0f;
            for (int i = 0; i < info.contactCount; i++)
            {
                into = Mathf.Max(into, Mathf.Abs(Vector3.Dot(info.relativeVelocity, info.GetContact(i).normal)));
            }
            return into >= __instance.m_minVelocity;
        }
    }
}
