namespace WinterSled
{
    /// <summary>
    /// The surface as the player is told about it (status icon, hauling speed, sound). Under the runners it can
    /// flip every physics step — Deep North snow around the powder depth, a patch of bare ground — so the shown
    /// surface holds while it keeps coming back: it changes only once it hasn't been seen for <see cref="Hold"/>
    /// seconds (the way ZenPath keeps its path effect alive). The runners' friction still follows every contact.
    /// </summary>
    internal sealed class SurfaceHold
    {
        public const float Hold = 1f;

        private float m_seen = float.NegativeInfinity;

        public Surface Shown { get; private set; } = Surface.Ground;

        public Surface Update(Surface current, float now)
        {
            if (current == Shown)
            {
                m_seen = now;
            }
            else if (now - m_seen >= Hold)
            {
                Shown = current;
                m_seen = now;
            }
            return Shown;
        }
    }
}
