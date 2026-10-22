using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace WinterSled.Tests
{
    public class TranslationTests
    {
        // the game's languages (its localization columns); Abenaki falls back to English on purpose
        private static readonly string[] GameLanguages =
        {
            "English", "Swedish", "French", "Italian", "German", "Spanish", "Russian", "Romanian", "Bulgarian",
            "Macedonian", "Finnish", "Danish", "Norwegian", "Icelandic", "Turkish", "Lithuanian", "Czech",
            "Hungarian", "Slovak", "Polish", "Dutch", "Portuguese_European", "Portuguese_Brazilian", "Chinese",
            "Chinese_Trad", "Japanese", "Korean", "Hindi", "Thai", "Croatian", "Georgian", "Greek", "Serbian",
            "Ukrainian", "Latvian",
        };

        [Fact]
        public void EveryGameLanguageIsThere()
        {
            Assert.Empty(GameLanguages.Except(Translations.Rows.Keys));
        }

        [Fact]
        public void EveryLanguageHasEveryText()
        {
            foreach (var row in Translations.Rows)
            {
                Assert.True(row.Value.Length == Translations.Keys.Length, $"{row.Key}: {row.Value.Length} texts for {Translations.Keys.Length} keys");
                Assert.DoesNotContain(row.Value, string.IsNullOrWhiteSpace);
            }
        }

        [Fact]
        public void TranslationsDifferFromEnglish()
        {
            // a row pasted from English by mistake would show here (short words like "Water" may match a few)
            var english = Translations.Rows["English"];
            foreach (var row in Translations.Rows.Where(r => r.Key != "English"))
            {
                int same = row.Value.Where((t, i) => t == english[i]).Count();
                Assert.True(same <= 2, $"{row.Key}: {same} texts are the English ones");
            }
        }

        [Fact]
        public void EveryTokenTheCodeUsesHasATranslation()
        {
            string src = Path.Combine(Path.GetDirectoryName(ThisFile()), "..", "WinterSled", "src");
            var used = Directory.GetFiles(src, "*.cs")
                .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"\$((?:piece_wintersled|se_wintersled|wintersled_)\w*)").Cast<Match>())
                .Select(m => m.Groups[1].Value)
                .Where(k => !k.EndsWith("_")) // prefixes built up in code
                .Distinct()
                .ToList();
            Assert.NotEmpty(used);
            Assert.Empty(used.Except(Translations.Keys));
        }

        private static string ThisFile([CallerFilePath] string path = "") => path;

        [Fact]
        public void TheRidingHintUsesTheGamesKeyNames()
        {
            foreach (var key in new[] { "$KEY_Left", "$KEY_Right", "$KEY_Backward", "$KEY_Forward", "$KEY_Jump" })
            {
                Assert.Contains(key, Translations.ControlsHint);
            }
        }
    }

    public class AudioTests
    {
        private static float[] Loop(SledAudio.Loop loop) => SledAudio.Synthesize(loop);

        [Theory]
        [InlineData("Snow")]
        [InlineData("Ice")]
        [InlineData("Ground")]
        [InlineData("Frozen")]
        [InlineData("Wind")]
        public void LoopsAreNormalisedAndSeamless(string loop)
        {
            var data = Loop((SledAudio.Loop)Enum.Parse(typeof(SledAudio.Loop), loop));
            Assert.Equal((int)(SledAudio.Rate * SledAudio.LoopSeconds), data.Length);
            Assert.Equal(0.8f, data.Max(Math.Abs), 3);
            // the jump from the last sample back to the first is no bigger than the loop's own steps
            float biggestStep = 0f;
            for (int i = 1; i < data.Length; i++)
            {
                biggestStep = Math.Max(biggestStep, Math.Abs(data[i] - data[i - 1]));
            }
            Assert.True(Math.Abs(data[0] - data[data.Length - 1]) <= biggestStep, "the loop point clicks");
        }

        /// <summary>WINTERSLED_SOUNDS=dir writes every loop there as a WAV to listen to (mise run sounds).</summary>
        [Fact]
        public void WritesLoopsToListenTo()
        {
            string dir = Environment.GetEnvironmentVariable("WINTERSLED_SOUNDS");
            if (string.IsNullOrEmpty(dir))
            {
                return;
            }
            Directory.CreateDirectory(dir);
            foreach (SledAudio.Loop loop in Enum.GetValues(typeof(SledAudio.Loop)))
            {
                // three times over, so the loop point can be heard
                var one = Loop(loop);
                var data = one.Concat(one).Concat(one).ToArray();
                using var w = new BinaryWriter(File.Create(Path.Combine(dir, $"sled_{loop.ToString().ToLowerInvariant()}.wav")));
                const int rate = 22050;
                w.Write("RIFF".ToCharArray()); w.Write(36 + data.Length * 2); w.Write("WAVE".ToCharArray());
                w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write("data".ToCharArray()); w.Write(data.Length * 2);
                foreach (float f in data)
                {
                    w.Write((short)(Math.Clamp(f, -1f, 1f) * short.MaxValue));
                }
            }
        }

        [Fact]
        public void LoopsAreTheSameEveryRun()
        {
            Assert.Equal(Loop(SledAudio.Loop.Snow), Loop(SledAudio.Loop.Snow));
        }

        [Fact]
        public void IceHissesHigherThanSnowAndSnowHigherThanGround()
        {
            // zero crossings per second: a rough brightness
            static int Crossings(float[] d) => Enumerable.Range(1, d.Length - 1).Count(i => (d[i] >= 0) != (d[i - 1] >= 0));
            int ice = Crossings(Loop(SledAudio.Loop.Ice));
            int snow = Crossings(Loop(SledAudio.Loop.Snow));
            int ground = Crossings(Loop(SledAudio.Loop.Ground));
            int frozen = Crossings(Loop(SledAudio.Loop.Frozen));
            Assert.True(ice > snow, $"ice {ice} vs snow {snow}");
            Assert.True(snow > ground, $"snow {snow} vs ground {ground}");
            Assert.True(frozen > ground, $"frozen {frozen} vs ground {ground}");
        }
    }
}
