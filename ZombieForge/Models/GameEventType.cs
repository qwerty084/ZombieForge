namespace ZombieForge.Models
{
    public enum GameEventType : int
    {
        None = 0,
        StartOfRound = 1,
        EndOfRound = 2,
        PowerupGrabbed = 3,
        DogRound = 4,
        PowerOn = 5,
        EndGame = 6,
    }
}
