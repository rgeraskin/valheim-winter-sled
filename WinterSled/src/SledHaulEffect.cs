using UnityEngine;

namespace WinterSled
{
    /// <summary>
    /// Shown while the local player pulls a sled. The character controller holds its own
    /// speed no matter what it drags, so a sled's friction would never be felt through the
    /// harness; this effect is where dragging over bare ground gets slow and tiring.
    /// The icon text names the surface under the runners.
    /// </summary>
    internal class SledHaulEffect : StatusEffect
    {
        public const string EffectName = "SE_WinterSledHaul";
        public static readonly int Hash = EffectName.GetStableHashCode();

        public static StatusEffect Create(Sprite icon)
        {
            var se = CreateInstance<SledHaulEffect>();
            se.name = EffectName;
            se.m_name = "$se_wintersled";
            se.m_tooltip = "$se_wintersled_tooltip";
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
            player.GetSEMan().RemoveStatusEffect(Hash, quiet: true);
        }

        private static Surface CurrentSurface()
        {
            var sled = SledController.LocalPulled;
            return sled != null ? sled.ShownSurface : Surface.Snow;
        }

        /// <summary>Speed multiplier for the puller on the current surface, with cargo.</summary>
        public static float SpeedFactor()
        {
            var sled = SledController.LocalPulled;
            return sled != null ? SpeedFactorFor(sled.ShownSurface, sled.CargoWeight()) : 1f;
        }

        public static float SpeedFactorFor(Surface surface, float cargoWeight)
        {
            var cfg = Plugin.Cfg;
            switch (surface)
            {
                case Surface.Ground:
                    float load = Mathf.Clamp01(cargoWeight / cfg.FullLoadWeight.Value);
                    return Mathf.Max(0.2f, cfg.GroundPullSpeed.Value - cfg.LoadPenalty.Value * load);
                case Surface.FrozenGround:
                    return cfg.FrozenPullSpeed.Value;
                case Surface.Wood:
                case Surface.Stone:
                    return cfg.FloorPullSpeed.Value;
                default:
                    return 1f;
            }
        }

        public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
        {
            speed -= baseSpeed * (1f - SpeedFactor());
        }

        public override void ModifyRunStaminaDrain(float baseDrain, ref float drain, Vector3 dir)
        {
            if (CurrentSurface() == Surface.Ground)
            {
                drain += baseDrain * (Plugin.Cfg.GroundStaminaDrain.Value - 1f);
            }
        }

        public override string GetIconText()
        {
            return Localization.instance.Localize(SurfaceProbe.LocalizationKey(CurrentSurface()));
        }

        public override bool IsDone()
        {
            return SledController.LocalPulled == null;
        }
    }
}
