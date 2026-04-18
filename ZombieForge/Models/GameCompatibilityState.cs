namespace ZombieForge.Models
{
    public enum GameCompatibilityState : int
    {
        Unknown = 0,
        Compatible = 1,
        UnsupportedVersion = 2,
        HookInstallFailed = 3,
    }
}
