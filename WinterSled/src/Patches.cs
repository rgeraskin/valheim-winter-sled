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
            if (!__instance.TryGetComponent<SledController>(out var sled) || !sled.HasRider())
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
