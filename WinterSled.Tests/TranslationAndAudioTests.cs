using System;
using System.Collections.Generic;
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
        public static IEnumerable<object[]> Loops => Enum.GetNames(typeof(SledAudio.Loop)).Select(l => new object[] { l });

        private static float[] Loop(string loop) => SledAudio.LoadLoop((SledAudio.Loop)Enum.Parse(typeof(SledAudio.Loop), loop));

        [Theory]
        [MemberData(nameof(Loops))]
        public void LoopsAreEmbeddedAtTheSameLoudness(string loop)
        {
            var data = Loop(loop);
            // build_sounds.py: -18 dBFS RMS, peaks softly limited below 0.95 (the limiting takes off a little)
            double rms = 20 * Math.Log10(Math.Sqrt(data.Average(s => (double)s * s)));
            Assert.InRange(rms, -19.0, -17.5);
            Assert.True(data.Max(Math.Abs) <= 0.951f, "a peak goes past the limit");
        }

        [Theory]
        [MemberData(nameof(Loops))]
        public void LoopsAreSeamless(string loop)
        {
            var data = Loop(loop);
            Assert.Equal((int)(SledAudio.Rate * SledAudio.LoopSeconds), data.Length);
            // the jump from the last sample back to the first is no bigger than the loop's own steps
            float biggestStep = 0f;
            for (int i = 1; i < data.Length; i++)
            {
                biggestStep = Math.Max(biggestStep, Math.Abs(data[i] - data[i - 1]));
            }
            Assert.True(Math.Abs(data[0] - data[data.Length - 1]) <= biggestStep, "the loop point clicks");
        }

        [Theory]
        [MemberData(nameof(Loops))]
        public void LoopsAreSteady(string loop)
        {
            // build_sounds.py evens the recording out: no quarter second much louder or quieter than the rest,
            // or the loop would change by itself and be heard repeating
            var data = Loop(loop);
            int frame = SledAudio.Rate / 4;
            var db = Enumerable.Range(0, data.Length / frame)
                .Select(f => 10 * Math.Log10(data.Skip(f * frame).Take(frame).Average(s => (double)s * s)))
                .ToArray();
            double mean = db.Average();
            Assert.True(db.All(d => Math.Abs(d - mean) < 3.0), $"{loop}: {db.Min() - mean:0.0} .. {db.Max() - mean:+0.0} dB");
        }

        [Fact]
        public void EverySurfaceButWaterHasALoop()
        {
            foreach (Surface surface in Enum.GetValues(typeof(Surface)))
            {
                Assert.True(SledAudio.LoopFor(surface).HasValue == (surface != Surface.Water), surface.ToString());
            }
        }

        [Fact]
        public void EveryRunnerLoopPlaysSomewhere()
        {
            var used = Enum.GetValues(typeof(Surface)).Cast<Surface>().Select(SledAudio.LoopFor).Where(l => l.HasValue).Select(l => l.Value).ToHashSet();
            foreach (SledAudio.Loop loop in Enum.GetValues(typeof(SledAudio.Loop)))
            {
                Assert.True(loop == SledAudio.Loop.Wind || used.Contains(loop), loop.ToString());
            }
        }
    }
}
