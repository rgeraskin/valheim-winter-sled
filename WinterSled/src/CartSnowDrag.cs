using HarmonyLib;
using UnityEngine;

namespace WinterSled
{
    /// <summary>
    /// The other half of the sled's point: wheels sink in snow. While the local player pulls a wheeled
    /// vehicle (the vanilla cart) over snow, a status effect slows them down; the sled glides there instead.
    /// </summary>
    internal class CartSnowDrag : MonoBehaviour
    {
        private const float ProbeInterval = 0.25f;

        public static CartSnowDrag LocalPulled { get; private set; }

        public Surface Surface { get; private set; } = Surface.Ground;

        private Vagon m_vagon;
        private float m_probeTimer;

        private void Awake()
        {
            m_vagon = GetComponent<Vagon>();
        }

        private void OnDestroy()
        {
            if (LocalPulled == this)
            {
                LocalPulled = null;
            }
        }

        private void Update()
        {
            var player = Player.m_localPlayer;
            if (player == null || m_vagon == null)
            {
                return;
            }
            if (!Plugin.Cfg.SlowCartsInSnow.Value || !m_vagon.IsAttached(player))
            {
                if (LocalPulled == this)
                {
                    LocalPulled = null;
                    CartSnowEffect.Remove(player);
                }
                return;
            }
            if ((m_probeTimer -= Time.deltaTime) < 0f)
            {
                m_probeTimer = ProbeInterval;
                Surface = Probe();
            }
            LocalPulled = this;
            if (SurfaceProbe.IsSnowy(Surface) && Surface != Surface.Ice)
            {
                CartSnowEffect.Ensure(player);
            }
            else
            {
                CartSnowEffect.Remove(player);
            }
        }

        private Surface Probe()
        {
            SurfaceProbe.ProbeBelow(transform, transform.position + Vector3.up * 1.5f, 3f, out var surface);
            return surface;
        }

        public float SpeedFactor()
        {
            switch (Surface)
            {
                case Surface.Snow: return Plugin.Cfg.CartSnowPullSpeed.Value;
                case Surface.Powder: return Plugin.Cfg.CartPowderPullSpeed.Value;
                default: return 1f;
            }
        }
    }

    internal class CartSnowEffect : StatusEffect
    {
        public const string EffectName = "SE_WinterSledCartSnow";
        public static readonly int Hash = EffectName.GetStableHashCode();

        public static StatusEffect Create(Sprite icon)
        {
            var se = CreateInstance<CartSnowEffect>();
            se.name = EffectName;
            se.m_name = "$se_wintersled_cart";
            se.m_tooltip = "$se_wintersled_cart_tooltip";
            se.m_icon = icon;
            se.m_ttl = 0f;
            return se;
        }

        public static void Ensure(Player player)
        {
            var seman = player.GetSEMan();
            if (!seman.HaveStatusEffect(Hash))
            {
                seman.AddStatusEffect(Hash);
            }
        }

        public static void Remove(Player player)
        {
            if (player.GetSEMan().HaveStatusEffect(Hash))
            {
                player.GetSEMan().RemoveStatusEffect(Hash, quiet: true);
            }
        }

        public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
        {
            var cart = CartSnowDrag.LocalPulled;
            if (cart != null)
            {
                speed -= baseSpeed * (1f - cart.SpeedFactor());
            }
        }

        public override string GetIconText()
        {
            var cart = CartSnowDrag.LocalPulled;
            return cart != null ? Localization.instance.Localize(SurfaceProbe.LocalizationKey(cart.Surface)) : "";
        }

        public override bool IsDone()
        {
            return CartSnowDrag.LocalPulled == null;
        }
    }

    /// <summary>Gives every wheeled vehicle (the cart; the catapult and ram too) its snow drag.</summary>
    [HarmonyPatch(typeof(Vagon), "Awake")]
    internal static class AddCartSnowDrag
    {
        private static void Postfix(Vagon __instance)
        {
            if (__instance.m_wheels != null && __instance.m_wheels.Length > 0 && !__instance.GetComponent<SledController>() && !__instance.GetComponent<CartSnowDrag>())
            {
                __instance.gameObject.AddComponent<CartSnowDrag>();
            }
        }
    }
}
