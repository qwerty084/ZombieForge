using System;
using Xunit;
using ZombieForge.Services;

namespace ZombieForge.Tests.Services
{
    public class ProcessWatcherTests
    {
        [Fact]
        public void NormalizeProcessName_NullInput_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ProcessWatcher.NormalizeProcessName(null!));
        }

        [Theory]
        [InlineData("BlackOps", "BlackOps.exe")]
        [InlineData("BlackOps.exe", "BlackOps.exe")]
        [InlineData("  BlackOps.exe  ", "BlackOps.exe")]
        [InlineData("O'Brien", "O'Brien.exe")]
        public void NormalizeProcessName_ValidInputs_ReturnNormalizedExe(string input, string expected)
        {
            string normalized = ProcessWatcher.NormalizeProcessName(input);

            Assert.Equal(expected, normalized);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("BlackOps.dll")]
        [InlineData(@"C:\Games\BlackOps.exe")]
        [InlineData("BlackOps?.exe")]
        [InlineData(".exe")]
        public void NormalizeProcessName_InvalidInputs_ThrowsArgumentException(string input)
        {
            Assert.Throws<ArgumentException>(() => ProcessWatcher.NormalizeProcessName(input));
        }
    }
}
