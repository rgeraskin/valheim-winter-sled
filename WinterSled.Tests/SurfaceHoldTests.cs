using Xunit;

namespace WinterSled.Tests
{
    public class SurfaceHoldTests
    {
        [Fact]
        public void TheFirstSurfaceShowsAtOnce()
        {
            var hold = new SurfaceHold();
            Assert.Equal(Surface.Snow, hold.Update(Surface.Snow, 0f));
        }

        [Fact]
        public void FlickeringSurfacesDontFlipTheShownOne()
        {
            // Deep North snow around the powder depth: snow and powder every other physics step
            var hold = new SurfaceHold();
            hold.Update(Surface.Snow, 0f);
            for (int step = 1; step < 200; step++)
            {
                var shown = hold.Update(step % 2 == 0 ? Surface.Snow : Surface.Powder, step * 0.02f);
                Assert.Equal(Surface.Snow, shown);
            }
        }

        [Fact]
        public void ARealChangeShowsAfterTheHold()
        {
            var hold = new SurfaceHold();
            hold.Update(Surface.Snow, 0f);
            Assert.Equal(Surface.Snow, hold.Update(Surface.Ground, 0.5f));
            Assert.Equal(Surface.Snow, hold.Update(Surface.Ground, 0.9f));
            Assert.Equal(Surface.Ground, hold.Update(Surface.Ground, SurfaceHold.Hold + 0.01f));
        }

        [Fact]
        public void TheShownSurfaceComingBackRestartsTheHold()
        {
            var hold = new SurfaceHold();
            hold.Update(Surface.Snow, 0f);
            hold.Update(Surface.Ground, 0.8f);
            hold.Update(Surface.Snow, 0.9f);
            // a second after the snow was last seen, not after the ground first was
            Assert.Equal(Surface.Snow, hold.Update(Surface.Ground, 1.5f));
            Assert.Equal(Surface.Ground, hold.Update(Surface.Ground, 1.95f));
        }
    }
}
