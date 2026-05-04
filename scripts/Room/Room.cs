using Godot;
using SignalingServer.Signaling;

namespace AirBrawl2.Networking;

public record Player(int PeerId, string Name);

public partial class Room : Node
{
    public readonly RoomId RoomId;
    public readonly Player LocalPlayer;
    private readonly TaskCompletionSource<RoomConfiguration> roomConfigTcs = new();
    public Task<RoomConfiguration> RoomConfigTask => roomConfigTcs.Task;
    public RoomConfiguration? RoomConfig;

    public Room(RoomId roomId, int peerId)
    {
        Name = "Room";
        RoomId = roomId;
        LocalPlayer = new Player(peerId, $"Player {peerId}");

        if (peerId != 1) return;
        RoomConfig = new RoomConfiguration(
            DateTime.UtcNow + TimeSpan.FromSeconds(10)
        );
        roomConfigTcs.SetResult(RoomConfig);
    }

    [Obsolete("Use the constructor with parameters")]
    public Room() => throw new Exception("Room should not be instantiated with the parameterless constructor");

    public void Quit()
    {
        roomConfigTcs.TrySetCanceled();
        playerJoined.OnCompleted();
        playerLeft.OnCompleted();

        playerJoined.Dispose();
        playerLeft.Dispose();
    }
}
