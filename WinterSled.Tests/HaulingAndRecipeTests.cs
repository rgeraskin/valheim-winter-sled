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
}
