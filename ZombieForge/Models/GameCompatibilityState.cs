namespace ZombieForge.Models
{
    /// <summary>
    /// Represents compatibility states reported by the injected monitor DLL.
    /// </summary>
    public enum GameCompatibilityState : int
    {
        /// <summary>
        /// Indicates that compatibility has not yet been determined.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Indicates that the current game build is compatible.
        /// </summary>
        Compatible = 1,

        /// <summary>
        /// Indicates that the current game build is unsupported.
        /// </summary>
        UnsupportedVersion = 2,

        /// <summary>
        /// Indicates that the native hook could not be installed.
        /// </summary>
        HookInstallFailed = 3,
    }
}
