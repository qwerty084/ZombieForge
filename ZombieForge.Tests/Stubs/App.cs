using Microsoft.Extensions.Logging;

namespace ZombieForge
{
    /// <summary>
    /// Minimal stub providing <see cref="LoggerFactory"/> for linked source files
    /// that call <c>App.LoggerFactory.CreateLogger&lt;T&gt;()</c> at runtime.
    /// </summary>
    internal static class App
    {
        public static ILoggerFactory LoggerFactory { get; } =
            Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });
    }
}
