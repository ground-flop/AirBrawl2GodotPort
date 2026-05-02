namespace AirBrawl2.Networking;

public record RoomConfiguration(DateTime StartTime)
{
    /// <summary>
    /// UTC time when the game starts
    /// </summary>
    public DateTime StartTime { get; } = StartTime;
}
