using System;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using Xunit;

namespace WinterSled.Tests
{
    public class ConfigLayoutTests
    {
        /// <summary>WINTERSLED_CFG=path writes the default .cfg there to look at.</summary>
        [Fact]
        public void CommonSettingsComeFirstAndCoefficientsLast()
        {
            string path = Environment.GetEnvironmentVariable("WINTERSLED_CFG");
            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(Path.GetTempPath(), "wintersled-layout-" + Guid.NewGuid().ToString("N") + ".cfg");
            }
            File.Delete(path);
            var file = new ConfigFile(path, saveOnInit: false);
            new SledConfig(file);
            file.Save();
            var lines = File.ReadAllLines(path);
            var sections = lines.Where(l => l.StartsWith("[")).ToList();
            var keys = lines.Where(l => l.Length > 0 && char.IsLetter(l[0]) && l.Contains(" = ")).Select(l => l.Substring(0, l.IndexOf(' '))).ToList();

            Assert.Equal("[1 - General]", sections.First());
            Assert.StartsWith("[6 - Advanced", sections.Last());
            Assert.Equal(sections.OrderBy(s => s, StringComparer.Ordinal), sections);
            Assert.Equal("Requirements", keys.First());
            Assert.True(keys.IndexOf("CartSnowPullSpeed") < keys.IndexOf("SoundVolume"), "cart penalty near the top");
            Assert.True(keys.IndexOf("SoundVolume") < keys.IndexOf("SnowFriction"), "friction coefficients below the everyday settings");
        }

        [Fact]
        public void ConfigurationManagerShowsThemInTheSameOrder()
        {
            var file = new ConfigFile(Path.Combine(Path.GetTempPath(), "wintersled-order-" + Guid.NewGuid().ToString("N") + ".cfg"), saveOnInit: false);
            new SledConfig(file);
            // Configuration Manager lists a section's settings by Order, highest first
            foreach (var section in file.Keys.GroupBy(k => k.Section))
            {
                var orders = section.Select(k => ((ConfigurationManagerAttributes)file[k].Description.Tags[0]).Order).ToList();
                Assert.Equal(orders.OrderByDescending(o => o), orders);
                bool advanced = section.Key.Contains("Advanced");
                Assert.All(section, k => Assert.Equal(advanced, ((ConfigurationManagerAttributes)file[k].Description.Tags[0]).IsAdvanced == true));
            }
        }
    }
}
