using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WinterSled
{
    /// <summary>
    /// Runner sounds: a loop per surface (the game has no sled sounds to borrow, and the cart's wheel creak is the
    /// wrong noise) and wind at speed for a rider. Each loop is a steady texture rebuilt from a recording by
    /// sounds/build_sounds.py (short grains from random places in it: its tone without anything to recognize when
    /// it repeats) and embedded in the plugin; sounds/SOURCES.md says whose the recordings are. The loops crossfade
    /// as the surface changes, louder and a little higher as the sled goes faster.
    /// </summary>
    internal class SledAudio : MonoBehaviour
    {
        // keep in sync with sounds/build_sounds.py
        internal const int Rate = 24000;
        internal const float LoopSeconds = 8f;
        // powder is the snow made heavier (build_sounds.py) and played lower
        private const float PowderPitch = 0.85f;
        // the sled's speed rises and falls with every step of whoever pulls it: the sound follows it smoothed
        private const float SpeedSmoothing = 0.4f;

        internal enum Loop { Snow, Powder, Ice, Ground, Frozen, Wood, Wind }

        /// <summary>
        /// How loud each loop plays next to the others (1: as built, every loop is leveled alike): earth and rock
        /// grind, and at the same level as the snow's hiss they stood out, so they are 3.5 dB down.
        /// </summary>
        internal static float Gain(Loop loop)
        {
            switch (loop)
            {
                case Loop.Ground:
                case Loop.Frozen:
                    return 0.668f; // -3.5 dB
                default:
                    return 1f;
            }
        }

        private static readonly Dictionary<Loop, AudioClip> s_clips = new Dictionary<Loop, AudioClip>();
        private static readonly System.Reflection.MethodInfo s_setData = typeof(AudioClip).GetMethod("SetData", new[] { typeof(float[]), typeof(int) });

        // the dev harness's A/B test: every loop at this pitch instead of the speed's (NaN: off)
        internal static float s_pitchOverride = float.NaN;

        private readonly Dictionary<Loop, AudioSource> m_sources = new Dictionary<Loop, AudioSource>();
        private SledController m_sled;
        private float m_speed;

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
                m_sources[loop] = source;
            }
        }

        private void Update()
        {
            if (m_sled == null)
            {
                return;
            }
            m_speed += (m_sled.Speed - m_speed) * (1f - Mathf.Exp(-Time.deltaTime / SpeedSmoothing));
            float speed = m_speed;
            float master = Plugin.Cfg.SoundVolume.Value;
            float t = Mathf.Clamp01(speed / 8f);
            var surface = m_sled.ShownSurface;
            bool touching = m_sled.Touching;

            Loop? active = LoopFor(surface);

            float dt = Time.deltaTime;
            foreach (var pair in m_sources)
            {
                float target = 0f;
                float pitch = 1f;
                if (pair.Key == Loop.Wind)
                {
                    // rushing air for a rider going fast, not for someone running with the sled in tow
                    target = m_sled.HasRider() ? Mathf.Clamp01((speed - 6f) / 10f) * 0.5f : 0f;
                    pitch = Mathf.Lerp(0.85f, 1.2f, Mathf.Clamp01(speed / 18f));
                }
                else if (touching && active == pair.Key && speed > 0.15f)
                {
                    target = Mathf.Lerp(0.15f, 1f, t) * Mathf.Clamp01(speed / 0.8f);
                    // a recording bends less than noise before it sounds wrong; ice least, its hiss turns shrill quickly
                    pitch = surface == Surface.Ice ? Mathf.Lerp(0.95f, 1.08f, t) : Mathf.Lerp(0.9f, 1.15f, t);
                    if (pair.Key == Loop.Powder)
                    {
                        pitch *= PowderPitch;
                    }
                }
                if (!float.IsNaN(s_pitchOverride))
                {
                    pitch = s_pitchOverride;
                }
                var source = pair.Value;
                source.volume = Mathf.MoveTowards(source.volume, target * Gain(pair.Key) * master, dt * 2.5f);
                source.pitch = Mathf.MoveTowards(source.pitch, pitch, dt * 2f);
                if (source.volume > 0.001f && !source.isPlaying)
                {
                    // Stop() rewinds to the loop's start: every stop and go would begin with the same second
                    source.timeSamples = UnityEngine.Random.Range(0, source.clip.samples);
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

        /// <summary>The loop that plays on a surface; none on water, where the sled floats in silence.</summary>
        internal static Loop? LoopFor(Surface surface)
        {
            switch (surface)
            {
                case Surface.Snow:
                    return Loop.Snow;
                case Surface.Powder:
                    return Loop.Powder;
                case Surface.Ice:
                    return Loop.Ice;
                case Surface.Ground:
                    return Loop.Ground;
                case Surface.Wood:
                    return Loop.Wood;
                // hard and gritty, both: rock and stone floors grind like frozen earth
                case Surface.Stone:
                case Surface.FrozenGround:
                    return Loop.Frozen;
                default:
                    return null;
            }
        }

        // ------------------------------------------------------------------ the recordings

        private static AudioClip Clip(Loop loop)
        {
            if (!s_clips.TryGetValue(loop, out var clip) || clip == null)
            {
                var data = LoadLoop(loop);
                clip = AudioClip.Create("wintersled_" + loop.ToString().ToLowerInvariant(), data.Length, 1, Rate, false);
                // the float[] overload, by reflection: Unity 6 adds a Span overload that net462 can't compile against
                s_setData.Invoke(clip, new object[] { data, 0 });
                s_clips[loop] = clip;
            }
            return clip;
        }

        internal static string ResourceName(Loop loop)
        {
            return "WinterSled.sounds.sled_" + loop.ToString().ToLowerInvariant() + ".wav";
        }

        /// <summary>One embedded loop's samples (16-bit mono PCM at <see cref="Rate"/>, as build_sounds.py writes it).</summary>
        internal static float[] LoadLoop(Loop loop)
        {
            string name = ResourceName(loop);
            using (var stream = typeof(SledAudio).Assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("embedded resource missing", name);
                }
                return ReadWav(stream, name);
            }
        }

        internal static float[] ReadWav(Stream stream, string name)
        {
            using (var reader = new BinaryReader(stream))
            {
                if (new string(reader.ReadChars(4)) != "RIFF")
                {
                    throw new InvalidDataException(name + " is not a WAV file");
                }
                reader.ReadInt32();
                if (new string(reader.ReadChars(4)) != "WAVE")
                {
                    throw new InvalidDataException(name + " is not a WAV file");
                }
                bool format = false;
                while (stream.Position + 8 <= stream.Length)
                {
                    string id = new string(reader.ReadChars(4));
                    int size = reader.ReadInt32();
                    if (id == "fmt ")
                    {
                        short pcm = reader.ReadInt16(), channels = reader.ReadInt16();
                        int rate = reader.ReadInt32();
                        reader.ReadBytes(6);
                        short bits = reader.ReadInt16();
                        if (pcm != 1 || channels != 1 || rate != Rate || bits != 16)
                        {
                            throw new InvalidDataException($"{name}: {channels} ch {rate} Hz {bits}-bit (format {pcm}), expected 16-bit mono PCM at {Rate} Hz");
                        }
                        reader.ReadBytes(size - 16);
                        format = true;
                    }
                    else if (id == "data" && format)
                    {
                        var data = new float[size / 2];
                        for (int i = 0; i < data.Length; i++)
                        {
                            data[i] = reader.ReadInt16() / 32768f;
                        }
                        return data;
                    }
                    else
                    {
                        reader.ReadBytes(size + (size & 1));
                    }
                }
                throw new InvalidDataException(name + " has no audio data");
            }
        }
    }
}
