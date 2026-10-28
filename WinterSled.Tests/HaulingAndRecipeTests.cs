using System.Linq;
using Xunit;
using Biome = Heightmap.Biome;

namespace WinterSled.Tests
{
    public class HaulingTests
    {
        public HaulingTests()
        {
            TestConfig.Fresh();
        }

        [Theory]
        [InlineData("Snow")]
        [InlineData("Powder")]
        [InlineData("Ice")]
        [InlineData("Water")]
        public void SnowAndIceCostNothing(string surface)
        {
            Assert.Equal(1f, SledHaulEffect.SpeedFactorFor((Surface)System.Enum.Parse(typeof(Surface), surface), 300f));
        }

        [Fact]
        public void GroundIsTheSlowestAndLoadMakesItSlower()
        {
            float empty = SledHaulEffect.SpeedFactorFor(Surface.Ground, 0f);
            float full = SledHaulEffect.SpeedFactorFor(Surface.Ground, Plugin.Cfg.FullLoadWeight.Value);
            float beyond = SledHaulEffect.SpeedFactorFor(Surface.Ground, Plugin.Cfg.FullLoadWeight.Value * 3f);
            Assert.Equal(Plugin.Cfg.GroundPullSpeed.Value, empty, 3);
            Assert.True(full < empty);
            Assert.Equal(full, beyond, 3);
            Assert.True(empty < SledHaulEffect.SpeedFactorFor(Surface.FrozenGround, 0f), "frozen ground is easier than thawed ground");
            Assert.True(empty < SledHaulEffect.SpeedFactorFor(Surface.Wood, 0f));
        }

        [Fact]
        public void SpeedNeverDropsBelowAFifth()
        {
            Plugin.Cfg.GroundPullSpeed.Value = 0.1f;
            Plugin.Cfg.LoadPenalty.Value = 1f;
            Assert.Equal(0.2f, SledHaulEffect.SpeedFactorFor(Surface.Ground, 10000f), 3);
        }
    }

    public class AnimalHoldBackTests
    {
        private const float Dt = 0.02f;

        public AnimalHoldBackTests()
        {
            TestConfig.Fresh();
        }

        [Fact]
        public void APullingAnimalIsLeftAlone()
        {
            // the animal ahead walks as fast as the sled moves, or faster (it is starting off): nothing to hold back
            Assert.Equal(0f, SledController.HoldBack(0f, 12f, Dt));
            Assert.Equal(0f, SledController.HoldBack(-3f, 12f, Dt));
        }

        [Fact]
        public void SmallCatchUpIsCancelledAtOnce()
        {
            Assert.Equal(0.1f, SledController.HoldBack(0.1f, 12f, Dt), 4);
        }

        [Fact]
        public void AStopIsSpreadOverTime()
        {
            // a sled gliding at 8 m/s when its animal stops: at most maxDecel*dt a step
            Assert.Equal(12f * Dt, SledController.HoldBack(8f, 12f, Dt), 4);
            float v = 8f, t = 0f;
            while (v > 0f && t < 10f)
            {
                v -= SledController.HoldBack(v, Plugin.Cfg.AnimalHoldBack.Value, Dt);
                t += Dt;
            }
            Assert.InRange(t, 0.3f, 0.6f);
        }

        [Fact]
        public void HoldsTheSledOnTheSteepestSnow()
        {
            // what gravity adds on a slope at the cliff angle, less the snow's friction, must be within the hold
            float slope = Plugin.Cfg.CliffAngle.Value * UnityEngine.Mathf.Deg2Rad;
            const float g = 20f;
            float downhill = g * (UnityEngine.Mathf.Sin(slope) - Plugin.Cfg.SnowFriction.Value * UnityEngine.Mathf.Cos(slope));
            Assert.True(Plugin.Cfg.AnimalHoldBack.Value > downhill, $"hold {Plugin.Cfg.AnimalHoldBack.Value} vs {downhill:0.0} m/s²");
        }

        [Fact]
        public void ZeroTurnsItOff()
        {
            Assert.Equal(0f, SledController.HoldBack(5f, 0f, Dt));
            Assert.Equal(0f, SledController.HoldUp(-2f, -2f, 0f, 0f, Dt));
        }

        [Fact]
        public void OnAClimbTheSledDoesntRollBack()
        {
            // found in the demo: a wolf stopping on 22°, the sled rolling back down at 2 m/s and dragging it along
            Assert.Equal(12f * Dt, SledController.HoldUp(-2f, -2f, 0f, 12f, Dt), 4);
            Assert.Equal(0.1f, SledController.HoldUp(-0.1f, -0.1f, 0f, 12f, Dt), 4);
            // an animal walking backwards on purpose: the sled may follow it
            Assert.Equal(0f, SledController.HoldUp(0f, -1f, 0f, 12f, Dt));
        }

        [Fact]
        public void AStandingSledDoesntCreepDownTheSlope()
        {
            // found after the demo: on 22° the sled crept back at 0.16 m/s, by what gravity adds in each step
            float slope = 20f * UnityEngine.Mathf.Sin(22f * UnityEngine.Mathf.Deg2Rad);
            // the old rule (no slope term) leaves the sled at what one step of gravity adds
            float v = -slope * Dt;
            v += SledController.HoldUp(v, v, 0f, 20f, Dt) - slope * Dt;
            Assert.Equal(-slope * Dt, v, 4);
            // with the slope's pull for the step: it stands (the animal stands too: catching up = its own speed)
            v = 0f;
            for (int i = 0; i < 500; i++)
            {
                v += SledController.HoldUp(v, v, slope, 20f, Dt);
                v -= slope * Dt;
            }
            Assert.InRange(v, -0.001f, 0.001f);
        }

        [Fact]
        public void StartingOffIsTheAnimalsJob()
        {
            // the animal walks off, the sled still stands: nothing pushes it after the animal (only against the slope)
            Assert.Equal(0f, SledController.HoldUp(-3f, 0f, 0f, 12f, Dt));
            Assert.Equal(0f, SledController.HoldUp(-3f, 0.5f, 0f, 12f, Dt));
        }
    }

    public class RecipeTests
    {
        private readonly SledConfig m_cfg;

        public RecipeTests()
        {
            m_cfg = TestConfig.Fresh();
        }

        [Fact]
        public void ParsesItemAmountPairs()
        {
            var reqs = m_cfg.ParseRecipe(" Wood:20, FineWood : 10,Iron:4 ").ToList();
            Assert.Equal(new[] { "Wood", "FineWood", "Iron" }, reqs.Select(r => r.Key));
            Assert.Equal(new[] { 20, 10, 4 }, reqs.Select(r => r.Value));
        }

        [Fact]
        public void SkipsMalformedEntries()
        {
            var reqs = m_cfg.ParseRecipe("Wood:20,Iron,Stone:0,Flint:x,,Resin:-3,LeatherScraps:8").ToList();
            Assert.Equal(new[] { "Wood", "LeatherScraps" }, reqs.Select(r => r.Key));
        }

        [Fact]
        public void TheDefaultRecipeIsTheMountainOneWithoutASeasonsMod()
        {
            string recipe = m_cfg.ActiveRecipe(out bool seasonal);
            Assert.False(seasonal);
            Assert.Equal(m_cfg.Recipe.Value, recipe);
            Assert.Contains("Iron", recipe);
            Assert.Contains("WolfPelt", recipe);
            Assert.Contains("BronzeNails", m_cfg.SeasonalRecipe.Value);
        }
    }

    public class SeasonTests
    {
        public SeasonTests()
        {
            TestConfig.Fresh();
        }

        [Theory]
        [InlineData(Biome.Meadows)]
        [InlineData(Biome.Swamp)]
        [InlineData(Biome.BlackForest)]
        [InlineData(Biome.Plains)]
        [InlineData(Biome.Mistlands)]
        public void SeasonalitysWinterTurnsFiveBiomesToSnow(Biome biome)
        {
            Assert.Equal(Biome.Mountain, SeasonalityCompat.WinterLooksLike(biome));
        }

        [Theory]
        [InlineData(Biome.Mountain)]
        [InlineData(Biome.DeepNorth)]
        [InlineData(Biome.AshLands)]
        [InlineData(Biome.Ocean)]
        public void SeasonalitysWinterLeavesTheRest(Biome biome)
        {
            Assert.Equal(biome, SeasonalityCompat.WinterLooksLike(biome));
        }

        [Fact]
        public void WithoutASeasonsModBiomesLookLikeThemselves()
        {
            Assert.False(SeasonsCompat.Installed);
            Assert.False(SeasonalityCompat.Installed);
            Assert.Equal(Biome.Meadows, SeasonsCompat.LooksLike(Biome.Meadows));
        }
    }

    public class AnimalHitchTests
    {
        private const float Height = 0.8f;
        private const float Max = 1.5f;

        [Fact]
        public void OnTheFlatTheModsPointStays()
        {
            Assert.Equal(10.8f, AnimalHitch.LevelHeight(10f, Height, 10f, 10f, Max), 4);
        }

        [Fact]
        public void DownhillTheHitchComesUpOutOfTheSnow()
        {
            // a lox heading down 22°: the ground 2.6 m behind it is 1.05 m higher than under its feet
            float rise = 2.6f * UnityEngine.Mathf.Tan(22f * UnityEngine.Mathf.Deg2Rad);
            Assert.Equal(10f + rise + Height, AnimalHitch.LevelHeight(10f, Height, 10f, 10f + rise, Max), 4);
        }

        [Fact]
        public void UphillTheHitchComesDownToTheGround()
        {
            Assert.Equal(10f - 1.05f + Height, AnimalHitch.LevelHeight(10f, Height, 10f, 10f - 1.05f, Max), 4);
        }

        [Fact]
        public void AnAnimalOffTheGroundIsNotLiftedFurther()
        {
            // found in the demo: a wolf 1.5 m up in the air, held there by its own hitch. Flat ground: the hitch stays
            // where the mod put it relative to the animal, however high the animal is.
            Assert.Equal(11.5f + Height, AnimalHitch.LevelHeight(11.5f, Height, 10f, 10f, Max), 4);
        }

        [Fact]
        public void NeverMovedFarFromTheModsPoint()
        {
            // the ground under the hitch is the bottom of a cliff, or the top of a wall
            Assert.Equal(10.8f - Max, AnimalHitch.LevelHeight(10f, Height, 10f, 0f, Max), 4);
            Assert.Equal(10.8f + Max, AnimalHitch.LevelHeight(10f, Height, 10f, 30f, Max), 4);
        }

        [Fact]
        public void OnByDefault()
        {
            Assert.True(TestConfig.Fresh().LevelAnimalHitch.Value);
        }
    }

    public class DraftAnimalsTests
    {
        [Fact]
        public void WithoutDraftAnimalsNothingIsHitchedOrFitted()
        {
            Assert.False(DraftAnimalsCompat.Installed);
            Assert.Null(DraftAnimalsCompat.HitchOf(null));
            Assert.False(DraftAnimalsCompat.IsHitched(null));
            // no scene, no Draft Animals: a no-op, not an exception
            DraftAnimalsCompat.FitSled(null);
        }
    }
}
