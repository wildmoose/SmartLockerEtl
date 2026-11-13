using System;
using LockerEtl.Processing;

namespace LockerEtl.Tests.Processing
{
    public class PinGeneratorTests
    {
        [Fact]
        public void Generate_ReturnsDigitsOfConfiguredLength()
        {
            var pin = new PinGenerator(8).Generate();

            Assert.Equal(8, pin.Length);
            Assert.All(pin, c => Assert.True(char.IsAsciiDigit(c)));
        }

        [Fact]
        public void Generate_IsNotConstant()
        {
            var generator = new PinGenerator();
            var pins = Enumerable.Range(0, 100).Select(_ => generator.Generate()).ToHashSet();

            Assert.True(pins.Count > 90);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(13)]
        public void Constructor_InvalidLength_Throws(int length)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PinGenerator(length));
        }
    }
}