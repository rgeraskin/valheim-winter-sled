using System.Collections.Generic;
using BepInEx.Configuration;

namespace WinterSled
{
    /// <summary>
    /// Everything that changes how the sled moves is admin-only and synced from the server,
    /// so every client simulates the same sled. Only cosmetic switches stay per-client.
    /// </summary>
    internal class SledConfig
    {
        public readonly ConfigEntry<string> Recipe;
        public readonly ConfigEntry<string> SeasonalRecipe;
        public readonly ConfigEntry<float> BaseMass;

        public readonly ConfigEntry<float> SnowFriction;
        public readonly ConfigEntry<float> PowderFriction;
        public readonly ConfigEntry<float> IceFriction;
        public readonly ConfigEntry<float> WoodFriction;
        public readonly ConfigEntry<float> StoneFriction;
        public readonly ConfigEntry<float> GroundFriction;
        public readonly ConfigEntry<float> FrozenGroundFriction;
        public readonly ConfigEntry<float> RunnerGrip;
        public readonly ConfigEntry<float> IceGrip;
        public readonly ConfigEntry<float> MaxSpeed;
        public readonly ConfigEntry<float> AirDrag;
        public readonly ConfigEntry<float> ParkSpeed;
        public readonly ConfigEntry<float> CliffAngle;
        public readonly ConfigEntry<float> PowderDepth;
        public readonly ConfigEntry<float> CrashMinSpeed;
        public readonly ConfigEntry<float> CrashMaxSpeed;

        public readonly ConfigEntry<float> GroundPullSpeed;
        public readonly ConfigEntry<float> FloorPullSpeed;
        public readonly ConfigEntry<float> FrozenPullSpeed;
        public readonly ConfigEntry<float> LoadPenalty;
        public readonly ConfigEntry<float> FullLoadWeight;
        public readonly ConfigEntry<float> GroundStaminaDrain;
        public readonly ConfigEntry<bool> SlowCartsInSnow;
        public readonly ConfigEntry<float> CartSnowPullSpeed;
        public readonly ConfigEntry<float> CartPowderPullSpeed;

        public readonly ConfigEntry<float> SteerStrength;
        public readonly ConfigEntry<float> BrakeFriction;
        public readonly ConfigEntry<float> PushForce;
        public readonly ConfigEntry<float> PushMaxSpeed;
        public readonly ConfigEntry<float> SteerResponse;

        public readonly ConfigEntry<string> SeatAnimation;
        public readonly ConfigEntry<bool> SnowSpray;
        public readonly ConfigEntry<float> SoundVolume;
        public readonly ConfigEntry<bool> DebugLog;

        // Sections from what players change most to the fine tuning; their numbers keep that order in the
        // .cfg file and in Configuration Manager, which both sort sections by name.
        private const string General = "1 - General";
        private const string Effects = "2 - Sound and effects";
        private const string Hauling = "3 - Hauling";
        private const string Riding = "4 - Riding";
        private const string Surfaces = "5 - Advanced: runner friction";
        private const string Physics = "6 - Advanced: physics";

        private readonly ConfigFile m_config;
        // Configuration Manager lists a section's settings by Order, highest first: count down so they show
        // in the order they are bound here (which is also their order in the .cfg file)
        private int m_order = 1000;

        public SledConfig(ConfigFile config)
        {
            m_config = config;

            Recipe = Bind(General, "Requirements", "Wood:20,FineWood:10,Iron:4,WolfPelt:2",
                "Build cost, ItemName:amount pairs separated by commas. Applies right away.");
            SeasonalRecipe = Bind(General, "SeasonalRequirements", "Wood:20,LeatherScraps:8,BronzeNails:10",
                "Build cost used instead when Seasons or Seasonality is installed: their winter brings snow to the " +
                "Meadows and Black Forest long before the Mountains, so the sled unlocks with bronze, like the cart. " +
                "Leave it empty to always use Requirements. Applies right away.");
            SlowCartsInSnow = Bind(General, "SlowCartsInSnow", true,
                "Wheeled vehicles (the vanilla cart) slow you down in snow, where the sled glides.");
            CartSnowPullSpeed = BindNumber(General, "CartSnowPullSpeed", 0.8f, "Speed multiplier while pulling a cart over snow.");
            CartPowderPullSpeed = BindNumber(General, "CartPowderPullSpeed", 0.6f, "Speed multiplier while pulling a cart through Deep North powder.");

            SoundVolume = BindNumber(Effects, "SoundVolume", 0.6f,
                "Volume of the runners' sound (snow hiss, ice song, ground scrape) and the wind when riding.", 0f, 1f, synced: false);
            SnowSpray = Bind(Effects, "SnowSpray", true, "Kick up snow behind the runners when moving fast on snow.", synced: false);
            SeatAnimation = Bind(Effects, "SeatAnimation", "emote_sit",
                "Animator flag used while riding: emote_sit sits on the deck with legs forward, attach_chair sits upright.", synced: false);

            GroundPullSpeed = BindNumber(Hauling, "GroundPullSpeed", 0.8f, "Walk/run speed multiplier while dragging an empty sled over bare ground, on top of the runners' own drag.");
            FrozenPullSpeed = BindNumber(Hauling, "FrozenPullSpeed", 0.9f, "Speed multiplier while dragging the sled over frozen bare ground, on top of the runners' own drag.");
            FloorPullSpeed = BindNumber(Hauling, "FloorPullSpeed", 0.9f, "Speed multiplier while dragging the sled over wood or stone floors.");
            LoadPenalty = BindNumber(Hauling, "LoadPenalty", 0.1f, "Extra speed lost on bare ground with a fully loaded sled (cargo already makes the runners drag harder).");
            FullLoadWeight = BindNumber(Hauling, "FullLoadWeight", 400f, "Cargo weight that counts as fully loaded for LoadPenalty.", 50f, 2000f);
            GroundStaminaDrain = BindNumber(Hauling, "GroundStaminaDrain", 1.5f, "Run stamina drain multiplier while dragging the sled over bare ground.", 1f, 5f);

            MaxSpeed = BindNumber(Riding, "MaxSpeed", 16f, "Speed (m/s) above which air drag grows steeply.", 4f, 40f);
            SteerStrength = BindNumber(Riding, "SteerStrength", 1f, "How hard A/D turn the sled when you ride it downhill.", 0f, 4f);
            BrakeFriction = BindNumber(Riding, "BrakeFriction", 0.2f, "Extra runner friction while you hold S to brake.");
            PushForce = BindNumber(Riding, "PushForce", 2.5f, "Acceleration (m/s²) when you push off with W at low speed.", 0f, 10f);
            PushMaxSpeed = BindNumber(Riding, "PushMaxSpeed", 3f, "Pushing off with W works below this speed (m/s).", 0f, 15f);
            CrashMinSpeed = BindNumber(Riding, "CrashMinSpeed", 6f, "Impact speed (m/s) from which hitting something damages the sled and what it hits.", 1f, 40f);
            CrashMaxSpeed = BindNumber(Riding, "CrashMaxSpeed", 14f, "Impact speed (m/s) of full crash damage.", 1f, 60f);

            SnowFriction = BindNumber(Surfaces, "SnowFriction", 0.06f, "Runner friction along the sled on packed snow (mountains, Deep North).");
            PowderFriction = BindNumber(Surfaces, "PowderFriction", 0.15f, "Runner friction in deep Deep North powder.");
            IceFriction = BindNumber(Surfaces, "IceFriction", 0.012f, "Runner friction on ice.");
            FrozenGroundFriction = BindNumber(Surfaces, "FrozenGroundFriction", 0.25f, "Runner friction on frozen bare ground: snow-free (dirt-cleared) ground in the cold biomes, or anywhere a seasons mod has winter.");
            WoodFriction = BindNumber(Surfaces, "WoodFriction", 0.22f, "Runner friction on wooden floors and ramps.");
            StoneFriction = BindNumber(Surfaces, "StoneFriction", 0.35f, "Runner friction on stone and metal floors, rock, and paved roads in the cold biomes.");
            GroundFriction = BindNumber(Surfaces, "GroundFriction", 0.55f, "Runner friction on bare, unfrozen ground: grass, dirt, sand.");
            RunnerGrip = BindNumber(Surfaces, "RunnerGrip", 0.6f, "Sideways friction of the runners: how well the sled holds its line on slopes and in turns.");
            IceGrip = BindNumber(Surfaces, "IceGrip", 0.15f, "Sideways friction of the runners on ice.");

            BaseMass = BindNumber(Physics, "BaseMass", 40f, "Empty sled mass. Cargo adds 10% of its weight, like the cart.", 10f, 200f);
            AirDrag = BindNumber(Physics, "AirDrag", 0.55f, "Air drag (N per (m/s)²) of a sled with a rider: what tops out a downhill run before MaxSpeed.", 0f, 5f);
            ParkSpeed = BindNumber(Physics, "ParkSpeed", 1.5f, "Below this speed (m/s) a sled nobody pulls or rides sets its brake and stays put on a slope.", 0f, 10f);
            SteerResponse = BindNumber(Physics, "SteerResponse", 20f, "How quickly the sled answers A/D (turn rate gain); the runners' grip fights it.", 1f, 60f);
            CliffAngle = BindNumber(Physics, "CliffAngle", 52f, "Slopes steeper than this (degrees) in the cold biomes are bare rock, not snow.", 20f, 89f);
            PowderDepth = BindNumber(Physics, "PowderDepth", 0.3f, "Deep North snow deeper than this (m) is powder.", 0f, 2f);
            DebugLog = Bind(Physics, "DebugLog", false, "Log the surface and forces under the sled once a second.", synced: false);
        }

        // Synced settings are admin-only and come from the server; the rest are each player's own. The
        // Advanced sections stay hidden in Configuration Manager until "Advanced settings" is ticked.
        private ConfigurationManagerAttributes Attributes(string section, bool synced)
        {
            return new ConfigurationManagerAttributes
            {
                IsAdminOnly = synced,
                IsAdvanced = section == Surfaces || section == Physics,
                Order = m_order--,
            };
        }

        // Multipliers and friction coefficients: 0..2 unless a range is given
        private ConfigEntry<float> BindNumber(string section, string key, float value, string description, float min = 0f, float max = 2f, bool synced = true)
        {
            return m_config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max), Attributes(section, synced)));
        }

        private ConfigEntry<T> Bind<T>(string section, string key, T value, string description, bool synced = true)
        {
            return m_config.Bind(section, key, value, new ConfigDescription(description, null, Attributes(section, synced)));
        }

        /// <summary>The recipe in effect: the seasonal one when a seasons mod is installed (and it is set), else the regular one.</summary>
        public string ActiveRecipe(out bool seasonal)
        {
            seasonal = (SeasonsCompat.Installed || SeasonalityCompat.Installed) && !string.IsNullOrWhiteSpace(SeasonalRecipe.Value);
            return seasonal ? SeasonalRecipe.Value : Recipe.Value;
        }

        public IEnumerable<KeyValuePair<string, int>> ParseRecipe(string recipe)
        {
            foreach (var part in recipe.Split(','))
            {
                var kv = part.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[1].Trim(), out var amount) && amount > 0)
                {
                    yield return new KeyValuePair<string, int>(kv[0].Trim(), amount);
                }
                else if (part.Trim().Length > 0)
                {
                    Plugin.Log?.LogWarning($"Ignoring recipe entry '{part}', expected ItemName:amount");
                }
            }
        }
    }
}
