using System;
using System.IO;
using BepInEx.Configuration;

namespace WinterSled.Tests
{
    /// <summary>A fresh SledConfig with its defaults, in a throwaway file, installed as the plugin's.</summary>
    internal static class TestConfig
    {
        private static readonly object Lock = new object();

        public static SledConfig Fresh()
        {
            lock (Lock)
            {
                var path = Path.Combine(Path.GetTempPath(), "wintersled-tests-" + Guid.NewGuid().ToString("N") + ".cfg");
                var cfg = new SledConfig(new ConfigFile(path, saveOnInit: false));
                Plugin.Cfg = cfg;
                return cfg;
            }
        }
    }
}
