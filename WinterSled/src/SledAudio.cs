using System;
using System.Collections.Generic;
using UnityEngine;

namespace WinterSled
{
    /// <summary>
    /// Runner sounds, synthesized at startup from filtered noise (the game has no sled sounds to borrow,
    /// and the cart's wheel creak is the wrong noise): a soft crunching hiss on snow, a thin singing hiss
    /// on ice, a low gritty scrape on ground (brighter when frozen), and wind at speed. Each surface has
    /// its own loop; they crossfade as the surface changes, louder and higher as the sled goes faster.
    /// </summary>
    internal class SledAudio : MonoBehaviour
    {
        private const int Rate = 22050;
        private const float LoopSeconds = 2f;

        internal enum Loop { Snow, Ice, Ground, Frozen, Wind }

        private static readonly Dictionary<Loop, AudioClip> s_clips = new Dictionary<Loop, AudioClip>();
        private static readonly System.Reflection.MethodInfo s_setData = typeof(AudioClip).GetMethod("SetData", new[] { typeof(float[]), typeof(int) });

        private readonly Dictionary<Loop, AudioSource> m_sources = new Dictionary<Loop, AudioSource>();
        private SledController m_sled;

        private void Start()
        {
            m_sled = GetComponent<SledController>();
            // the cart's own loops play through the game's sound effects mixer group; share it,
            // so the game's volume settings apply
            var mixer = GetComponentInChildren<AudioSource>(true)?.outputAudioMixerGroup;
            if (mixer == null && AudioMan.instance != null && AudioMan.instance.m_masterMixer != null)
            {
                var groups = AudioMan.instance.m_masterMixer.FindMatchingGroups("SFX");
                mixer = groups.Length > 0 ? groups[0] : null;
            }
            foreach (Loop loop in Enum.GetValues(typeof(Loop)))
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.clip = Clip(loop);
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 2f;
                source.maxDistance = 40f;
                source.dopplerLevel = 0f;
                source.volume = 0f;
                source.outputAudioMixerGroup = mixer;
                // start each loop at a different point, so they never phase together
                source.time = UnityEngine.Random.Range(0f, LoopSeconds);
                m_sources[loop] = source;
            }
        }

        private void Update()
        {
            if (m_sled == null)
            {
                return;
            }
            float speed = m_sled.Speed;
            float master = Plugin.Cfg.SoundVolume.Value;
            float t = Mathf.Clamp01(speed / 8f);
            var surface = m_sled.ShownSurface;
            bool touching = m_sled.Touching;

            Loop? active = null;
            switch (surface)
            {
                case Surface.Snow:
                case Surface.Powder:
                    active = Loop.Snow;
                    break;
                case Surface.Ice:
                    active = Loop.Ice;
                    break;
                case Surface.Ground:
                case Surface.Wood:
                case Surface.Stone:
                    active = Loop.Ground;
                    break;
                case Surface.FrozenGround:
                    active = Loop.Frozen;
                    break;
            }

            float dt = Time.deltaTime;
            foreach (var pair in m_sources)
            {
                float target = 0f;
                float pitch = 1f;
                if (pair.Key == Loop.Wind)
                {
                    // rushing air for a rider going fast, not for someone running with the sled in tow
                    target = m_sled.HasRider() ? Mathf.Clamp01((speed - 6f) / 10f) * 0.5f : 0f;
                    pitch = Mathf.Lerp(0.8f, 1.3f, Mathf.Clamp01(speed / 18f));
                }
                else if (touching && active == pair.Key && speed > 0.15f)
                {
                    target = Mathf.Lerp(0.15f, 1f, t) * Mathf.Clamp01(speed / 0.8f);
                    pitch = Mathf.Lerp(0.85f, 1.25f, t);
                    if (surface == Surface.Powder)
                    {
                        // ploughing powder is duller and louder
                        pitch *= 0.8f;
                        target = Mathf.Min(1f, target * 1.2f);
                    }
                }
                var source = pair.Value;
                source.volume = Mathf.MoveTowards(source.volume, target * master, dt * 2.5f);
                source.pitch = Mathf.MoveTowards(source.pitch, pitch, dt * 2f);
                if (source.volume > 0.001f && !source.isPlaying)
                {
                    source.Play();
                }
                else if (source.volume <= 0.001f && source.isPlaying)
                {
                    source.Stop();
                }
            }
        }

        public float VolumeOf(string loop)
        {
            return Enum.TryParse(loop, true, out Loop l) && m_sources.TryGetValue(l, out var s) ? s.volume : 0f;
        }

        public string Describe()
        {
            var parts = new List<string>();
            foreach (var pair in m_sources)
            {
                parts.Add($"{pair.Key}={pair.Value.volume:0.00}{(pair.Value.isPlaying ? "▶" : "")}@{pair.Value.pitch:0.00}");
            }
            return string.Join(" ", parts);
        }

        // ------------------------------------------------------------------ synthesis

        private static AudioClip Clip(Loop loop)
        {
            if (!s_clips.TryGetValue(loop, out var clip) || clip == null)
            {
                clip = AudioClip.Create("wintersled_" + loop.ToString().ToLowerInvariant(), (int)(Rate * LoopSeconds), 1, Rate, false);
                // the float[] overload, by reflection: Unity 6 adds a Span overload that net462 can't compile against
                s_setData.Invoke(clip, new object[] { Synthesize(loop), 0 });
                s_clips[loop] = clip;
            }
            return clip;
        }

        /// <summary>Filtered noise for one loop, its ends crossfaded so it repeats without a click.</summary>
        internal static float[] Synthesize(Loop loop)
        {
            int n = (int)(Rate * LoopSeconds);
            int fade = Rate / 5;
            var rng = new System.Random(1000 + (int)loop);
            var raw = new float[n + fade];
            float lp1 = 0f, lp2 = 0f, hpPrev = 0f, hpOut = 0f, crackle = 0f, phase = 0f, wobble = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                float sample;
                switch (loop)
                {
                    case Loop.Snow:
                        // soft hiss (low-passed noise) with crunchy crackles
                        lp1 += (white - lp1) * 0.18f;
                        if (rng.NextDouble() < 0.0025)
                        {
                            crackle = 0.9f * (float)(rng.NextDouble() * 0.6 + 0.4);
                        }
                        crackle *= 0.985f;
                        sample = lp1 * 0.55f + white * crackle * 0.6f;
                        break;
                    case Loop.Ice:
                        // thin high hiss (high-passed noise) and a faint singing tone of steel on ice
                        hpOut = 0.97f * (hpOut + white - hpPrev);
                        hpPrev = white;
                        wobble += 0.00004f;
                        phase += 2f * Mathf.PI * (1450f + 40f * Mathf.Sin(wobble * 6f)) / Rate;
                        sample = hpOut * 0.35f + Mathf.Sin(phase) * 0.06f;
                        break;
                    case Loop.Ground:
                    case Loop.Frozen:
                        // a low, grinding rumble (twice low-passed noise) with grit; frozen ground is brighter
                        bool frozen = loop == Loop.Frozen;
                        lp1 += (white - lp1) * (frozen ? 0.12f : 0.05f);
                        lp2 += (lp1 - lp2) * (frozen ? 0.25f : 0.12f);
                        if (rng.NextDouble() < (frozen ? 0.006 : 0.004))
                        {
                            crackle = (float)(rng.NextDouble() * 0.7 + 0.3);
                        }
                        crackle *= frozen ? 0.93f : 0.9f;
                        sample = lp2 * (frozen ? 2.2f : 3.2f) + white * crackle * (frozen ? 0.45f : 0.35f);
                        break;
                    default:
                        // wind: slow-moving band of noise
                        lp1 += (white - lp1) * 0.03f;
                        wobble += 1f / Rate;
                        sample = lp1 * (1.6f + 0.6f * Mathf.Sin(wobble * 2.1f) * Mathf.Sin(wobble * 0.7f));
                        break;
                }
                raw[i] = sample;
            }
            var data = new float[n];
            Array.Copy(raw, data, n);
            for (int i = 0; i < fade; i++)
            {
                // the extra tail fades into the head: the loop point is seamless
                float w = (float)i / fade;
                data[i] = raw[n + i] * (1f - w) + raw[i] * w;
            }
            float peak = 1e-4f;
            foreach (var s in data)
            {
                peak = Mathf.Max(peak, Mathf.Abs(s));
            }
            for (int i = 0; i < n; i++)
            {
                data[i] *= 0.8f / peak;
            }
            return data;
        }
    }
}
