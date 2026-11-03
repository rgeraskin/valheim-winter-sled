using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WinterSled
{
    /// <summary>
    /// Shows other players sitting on a sled or in a cart as seated. Sitting down sets the seat's animator flag
    /// (<c>emote_sit</c> by default) through the player's <see cref="ZSyncAnimation"/>, which stores it in the player's
    /// ZDO, but the other clients copy only the flags in the player prefab's sync list onto their animator, and the
    /// emote flags aren't in it (the game's own emotes travel as an emote name instead): everyone but the rider saw
    /// them standing. Here a remote player's animator follows the stored seat flags too. Only changes are copied, so
    /// the game's /sit emote, which sets the same flag without storing it, isn't cancelled.
    /// </summary>
    [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SyncParameters))]
    internal static class RemoteSeatPose
    {
        // ZSyncAnimation's key offset for animator parameters in the ZDO (its private c_ZDOSalt; a test compares them)
        internal const int ZdoSalt = 438569;

        // The patched method is private to the game, so a game update may rename it or change its parameters. Then
        // only this fix is skipped (riders show standing to others again), not the whole plugin's patching.
        // (Plain reflection, as Harmony's AccessTools.DeclaredMethod looks it up: AccessTools doesn't load in the tests.)
        private static bool Prepare()
        {
            string problem;
            try
            {
                problem = typeof(ZSyncAnimation).GetMethod(nameof(ZSyncAnimation.SyncParameters), AllDeclared) != null ? null : "not found";
            }
            catch (AmbiguousMatchException)
            {
                problem = "overloaded";
            }
            if (problem == null)
            {
                return true;
            }
            Plugin.Log?.LogWarning($"ZSyncAnimation.SyncParameters {problem} (game update?): other players on a sled will show standing");
            return false;
        }

        private const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private sealed class Shown
        {
            public readonly bool IsPlayer;
            public readonly HashSet<int> On = new HashSet<int>();

            public Shown(bool isPlayer)
            {
                IsPlayer = isPlayer;
            }
        }

        private static readonly ConditionalWeakTable<ZSyncAnimation, Shown> s_shown = new ConditionalWeakTable<ZSyncAnimation, Shown>();

        // the rider picks the flag with their own (client-side) SeatAnimation: both offered values, and this client's
        // (hashed on first use: Animator.StringToHash is native, and the tests load this class without Unity)
        private static int[] s_known;
        private static string s_configured;
        private static int[] s_poses;

        private static int[] Poses()
        {
            string name = Plugin.Cfg?.SeatAnimation.Value;
            if (s_poses == null || name != s_configured)
            {
                if (s_known == null)
                {
                    s_known = new[] { ZSyncAnimation.GetHash("emote_sit"), ZSyncAnimation.GetHash("attach_chair") };
                }
                s_configured = name;
                int hash = string.IsNullOrEmpty(name) ? 0 : ZSyncAnimation.GetHash(name);
                s_poses = hash == 0 || System.Array.IndexOf(s_known, hash) >= 0 ? s_known : new[] { s_known[0], s_known[1], hash };
            }
            return s_poses;
        }

        private static void Postfix(ZSyncAnimation __instance)
        {
            var nview = __instance.m_nview;
            var zdo = nview != null ? nview.GetZDO() : null;
            if (zdo == null || nview.IsOwner())
            {
                return;
            }
            var shown = s_shown.GetValue(__instance, z => new Shown(z.GetComponent<Player>() != null));
            if (!shown.IsPlayer)
            {
                return;
            }
            foreach (int hash in Poses())
            {
                bool on = zdo.GetInt(ZdoSalt + hash, 0) != 0;
                if (on == shown.On.Contains(hash))
                {
                    continue;
                }
                if (on)
                {
                    shown.On.Add(hash);
                }
                else
                {
                    shown.On.Remove(hash);
                }
                __instance.m_animator.SetBool(hash, on);
            }
        }
    }
}
