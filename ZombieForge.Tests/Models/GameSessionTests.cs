using Xunit;
using ZombieForge.Models;

namespace ZombieForge.Tests.Models
{
    public class GameSessionTests
    {
        [Fact]
        public void FormatGameTime_ReturnsPlaceholder_WhenSessionIsInactive()
        {
            var session = new GameSession();

            Assert.Equal("--:--:--", session.FormatGameTime(5000));
        }

        [Fact]
        public void FormatGameTime_UsesFirstRoundStartAsSessionStart()
        {
            var session = new GameSession();
            session.OnRoundStart(1000);

            Assert.Equal("00:00:05", session.FormatGameTime(6000));
        }

        [Fact]
        public void FormatRoundTime_FreezesAfterRoundEnd_UntilNextRoundStarts()
        {
            var session = new GameSession();
            session.OnRoundStart(1000);

            Assert.Equal("00:06", session.FormatRoundTime(7000));

            session.OnRoundEnd(9000);
            Assert.Equal("00:08", session.FormatRoundTime(20000));

            session.OnRoundStart(21000);
            Assert.Equal("00:01", session.FormatRoundTime(22000));
        }

        [Fact]
        public void Reset_ClearsAllState()
        {
            var session = new GameSession();
            session.OnRoundStart(1000);
            session.OnRoundEnd(2000);

            session.Reset();

            Assert.False(session.IsActive);
            Assert.Equal(0u, session.CurrentRoundNumber);
            Assert.Equal("--:--:--", session.FormatGameTime(3000));
            Assert.Equal("--:--", session.FormatRoundTime(3000));
        }
    }
}
