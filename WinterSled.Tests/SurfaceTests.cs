using UnityEngine;
using Xunit;
using Biome = Heightmap.Biome;

// the tests share Plugin.Cfg
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace WinterSled.Tests
{
    public class SurfaceTests
    {
        private static readonly Color Clean = new Color(0f, 0f, 0f, 0f);
        private static readonly Color Dirt = new Color(1f, 0f, 0f, 0f);
        private static readonly Color Paved = new Color(0f, 0f, 1f, 0f);

        public SurfaceTests()
        {
            TestConfig.Fresh();
        }

        private static Color Snow(float depthMetres) => new Color(0f, depthMetres / 2f, 0f, 0f);

        [Theory]
        [InlineData(Biome.Meadows)]
        [InlineData(Biome.BlackForest)]
        [InlineData(Biome.Swamp)]
        [InlineData(Biome.Plains)]
        [InlineData(Biome.Mistlands)]
        [InlineData(Biome.AshLands)]
        public void WarmBiomesAreGround(Biome biome)
        {
            Assert.Equal(Surface.Ground, SurfaceProbe.ClassifyGround(biome, biome, Clean, 1f));
            Assert.Equal(Surface.Ground, SurfaceProbe.ClassifyGround(biome, biome, Dirt, 1f));
        }

        [Theory]
        [InlineData(Biome.Mountain)]
        [InlineData(Biome.DeepNorth)]
        public void ColdBiomesAreSnow(Biome biome)
        {
            Assert.Equal(Surface.Snow, SurfaceProbe.ClassifyGround(biome, biome, Clean, 1f));
        }

        [Fact]
        public void ClearedGroundInTheColdIsFrozen()
        {
            Assert.Equal(Surface.FrozenGround, SurfaceProbe.ClassifyGround(Biome.Mountain, Biome.Mountain, Dirt, 1f));
            Assert.Equal(Surface.FrozenGround, SurfaceProbe.ClassifyGround(Biome.DeepNorth, Biome.DeepNorth, Dirt, 1f));
        }

        [Fact]
        public void PavedRoadsAndCliffsInTheColdAreStone()
        {
            Assert.Equal(Surface.Stone, SurfaceProbe.ClassifyGround(Biome.Mountain, Biome.Mountain, Paved, 1f));
            float sixtyDegrees = Mathf.Cos(60f * Mathf.Deg2Rad);
            Assert.Equal(Surface.Stone, SurfaceProbe.ClassifyGround(Biome.Mountain, Biome.Mountain, Clean, sixtyDegrees));
            float fortyDegrees = Mathf.Cos(40f * Mathf.Deg2Rad);
            Assert.Equal(Surface.Snow, SurfaceProbe.ClassifyGround(Biome.Mountain, Biome.Mountain, Clean, fortyDegrees));
        }

        [Fact]
        public void CliffAngleIsASetting()
        {
            float fortyDegrees = Mathf.Cos(40f * Mathf.Deg2Rad);
            Plugin.Cfg.CliffAngle.Value = 30f;
            Assert.Equal(Surface.Stone, SurfaceProbe.ClassifyGround(Biome.Mountain, Biome.Mountain, Clean, fortyDegrees));
        }

        [Fact]
        public void DeepNorthPowderNeedsDepth()
        {
            Assert.Equal(Surface.Powder, SurfaceProbe.ClassifyGround(Biome.DeepNorth, Biome.DeepNorth, Snow(1.0f), 1f));
            Assert.Equal(Surface.Snow, SurfaceProbe.ClassifyGround(Biome.DeepNorth, Biome.DeepNorth, Snow(0.2f), 1f));
            Plugin.Cfg.PowderDepth.Value = 0.1f;
            Assert.Equal(Surface.Powder, SurfaceProbe.ClassifyGround(Biome.DeepNorth, Biome.DeepNorth, Snow(0.2f), 1f));
        }

        [Fact]
        public void PowderIsTheDeepNorthsOnly()
        {
            // the green channel elsewhere is cultivated ground, not snow depth
            Assert.Equal(Surface.Snow, SurfaceProbe.ClassifyGround(Biome.Mountain, Biome.Mountain, Snow(1.5f), 1f));
        }

        [Fact]
        public void WinterMeadowsFromASeasonsModAreSnow()
        {
            Assert.Equal(Surface.Snow, SurfaceProbe.ClassifyGround(Biome.Meadows, Biome.Mountain, Clean, 1f));
            Assert.Equal(Surface.FrozenGround, SurfaceProbe.ClassifyGround(Biome.Meadows, Biome.Mountain, Dirt, 1f));
            Assert.Equal(Surface.Snow, SurfaceProbe.ClassifyGround(Biome.Meadows, Biome.Mountain, Snow(1.5f), 1f));
        }

        [Fact]
        public void SummerMountainsFromASeasonsModAreGround()
        {
            Assert.Equal(Surface.Ground, SurfaceProbe.ClassifyGround(Biome.Mountain, Biome.Meadows, Clean, 1f));
        }

        [Fact]
        public void RunnersGlideBestOnIceAndWorstOnGround()
        {
            float ice = SurfaceProbe.Friction(Surface.Ice);
            float snow = SurfaceProbe.Friction(Surface.Snow);
            float powder = SurfaceProbe.Friction(Surface.Powder);
            float frozen = SurfaceProbe.Friction(Surface.FrozenGround);
            float wood = SurfaceProbe.Friction(Surface.Wood);
            float stone = SurfaceProbe.Friction(Surface.Stone);
            float ground = SurfaceProbe.Friction(Surface.Ground);
            Assert.True(ice < snow, "ice < snow");
            Assert.True(snow < powder, "snow < powder");
            Assert.True(powder < frozen, "powder < frozen ground");
            Assert.True(frozen < stone && stone < ground, "frozen < stone < ground");
            Assert.True(wood < ground, "wood < ground");
            Assert.Equal(0f, SurfaceProbe.Friction(Surface.Water));
            Assert.True(SurfaceProbe.Grip(Surface.Ice) < SurfaceProbe.Grip(Surface.Snow), "runners grip ice less than snow");
        }

        [Fact]
        public void EverySurfaceHasAText()
        {
            foreach (Surface surface in System.Enum.GetValues(typeof(Surface)))
            {
                string key = SurfaceProbe.LocalizationKey(surface).TrimStart('$');
                Assert.Contains(key, Translations.Keys);
            }
        }
    }
}
