using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;

// the development test driver in tools/DevHarness pokes at the sled's state
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WinterSledDevHarness")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WinterSled.Tests")]

namespace WinterSled
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(SeasonsCompat.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(SeasonalityCompat.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class Plugin : BaseUnityPlugin
    {
        public const string Guid = "rgeraskin.WinterSled";
        public const string Name = "WinterSled";
        public const string Version = "0.1.0";

        internal static ManualLogSource Log;
        internal static SledConfig Cfg;

        private void Awake()
        {
            Log = Logger;
            Cfg = new SledConfig(Config);
            Translations.Register();
            SeasonsCompat.Init();
            SeasonalityCompat.Init();
            LiveSettings.Hook();
            PrefabManager.OnVanillaPrefabsAvailable += SledBuilder.Register;
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
        }
    }
}
