using System;
using ZombieForge.Services;
using ZombieForge.Services.Games;

namespace ZombieForge.Tests;

public class MemoryReadContractTests
{
    [Fact]
    public void MemoryService_DoesNotExposeLossyReadInt32()
    {
        var method = typeof(MemoryService).GetMethod(
            "ReadInt32",
            new[] { typeof(IntPtr), typeof(long) });

        Assert.Null(method);
    }

    [Fact]
    public void TryReadInt32_WithInvalidHandle_ReturnsFailure()
    {
        bool success = MemoryService.TryReadInt32(IntPtr.Zero, 0x1234, out int value, out int win32Error);

        Assert.False(success);
        Assert.Equal(0, value);
        Assert.NotEqual(0, win32Error);
    }
}

public class GameHandlerReadContractTests
{
    [Fact]
    public void BlackOps1Handler_TryReadPlayerStats_WithInvalidHandle_ReturnsFailure()
    {
        var handler = new BlackOps1Handler();

        bool success = handler.TryReadPlayerStats(IntPtr.Zero, 0, 0, out var stats, out int win32Error);

        Assert.False(success);
        Assert.Equal(0, stats.Points);
        Assert.Equal(0, stats.Kills);
        Assert.Equal(0, stats.Downs);
        Assert.Equal(0, stats.Headshots);
        Assert.NotEqual(0, win32Error);
    }

    [Fact]
    public void BlackOps1Handler_TryReadLevelTime_WithInvalidHandle_ReturnsFailure()
    {
        var handler = new BlackOps1Handler();

        bool success = handler.TryReadLevelTime(IntPtr.Zero, out int levelTime, out int win32Error);

        Assert.False(success);
        Assert.Equal(0, levelTime);
        Assert.NotEqual(0, win32Error);
    }

    [Fact]
    public void BlackOps2Handler_TryReads_ReturnFalse()
    {
        var handler = new BlackOps2Handler();

        bool statsSuccess = handler.TryReadPlayerStats(IntPtr.Zero, 0, 0, out var stats, out int statsError);
        bool timeSuccess = handler.TryReadLevelTime(IntPtr.Zero, out int levelTime, out int timeError);

        Assert.False(statsSuccess);
        Assert.False(timeSuccess);
        Assert.Equal(0, stats.Points);
        Assert.Equal(0, stats.Kills);
        Assert.Equal(0, stats.Downs);
        Assert.Equal(0, stats.Headshots);
        Assert.Equal(0, levelTime);
        Assert.Equal(0, statsError);
        Assert.Equal(0, timeError);
    }
}
