using System.Reactive.Subjects;
using Godot;
using SignalingServer.Signaling;

namespace AirBrawl2.Networking;

public record Player(int PeerId, string Name);

public partial class Room : Node
{
    public readonly RoomId RoomId;
    public readonly Player LocalPlayer;
    private readonly TaskCompletionSource<RoomConfiguration> roomConfigTcs = new();
    public Task<RoomConfiguration> RoomConfig => roomConfigTcs.Task;

    public readonly Dictionary<int, Player> Players = new();
    private readonly Subject<Player> playerJoined = new();
    private readonly Subject<Player> playerLeft = new();

    public IObservable<Player> PlayerJoined => playerJoined;
    public IObservable<Player> PlayerLeft => playerLeft;

    public Room(RoomId roomId, int peerId)
    {
        Name = "Room";
        RoomId = roomId;
        LocalPlayer = new Player(peerId, $"Player {peerId}");

        if (peerId == 1)
            roomConfigTcs.SetResult(new RoomConfiguration(
                DateTime.UtcNow + TimeSpan.FromSeconds(10)
            ));
    }

    [Obsolete("Use the constructor with parameters")]
    public Room() => throw new Exception("Room should not be instantiated with the parameterless constructor");
}
