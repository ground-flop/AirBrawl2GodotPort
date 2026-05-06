using AirBrawl2.scripts;
using Godot;
using SignalingServer.Signaling;

namespace AirBrawl2.Networking;

public partial class RoomManager : Node
{
    private readonly Signaling signaling = new();
    public Room? Room;

    public new bool Ready => signaling.Connected;
    public event Action? RoomManagerReady;

    public override void _EnterTree()
    {
        var multiplayer = Multiplayer;
        Task.Run(async () =>
        {
            await signaling.ConnectSignalingServer(multiplayer);
            RoomManagerReady?.Invoke();
        });
    }

    public async Task<Room> CreateRoom()
    {
        var roomId = await signaling.CreateRoom();

        var multiplayer = new WebRtcMultiplayerPeer();
        multiplayer.CreateMesh(1);
        Multiplayer.MultiplayerPeer = multiplayer;

        Room = new Room(roomId, 1);
        AddChild(Room);

        // Wait for the room config to be created
        await Room.RoomConfigTask;

        return Room;
    }

    public async Task JoinRoom(RoomId roomId)
    {
        var peerId = await signaling.JoinRoom(roomId);

        var multiplayer = new WebRtcMultiplayerPeer();
        multiplayer.CreateMesh(peerId);
        Multiplayer.MultiplayerPeer = multiplayer;

        Room = new Room(roomId, peerId);
        AddChild(Room);

        // Connect players
        var playersConnectionInfo = await signaling.GetConnectionInfo();
        var tasks =
            playersConnectionInfo.PlayersConnectionInfo
                .Select(async connInfo =>
                {
                    var peer = new PeerConnection();
                    multiplayer.AddPeer(peer, connInfo.PeerId);
                    await peer.AnswerConnectionOffer(signaling, connInfo.ConnectionAttemptId); // This return only when peer is actually connected
                })
                .ToArray();

        await Task.WhenAll(tasks);

        // Wait for all players to be connected
        var numberOfPlayers = playersConnectionInfo.PlayersConnectionInfo.Length;
        await Task.Run(() =>
        {
            while (Room.Players.Count <= numberOfPlayers) { }
        });

        Singletons.TimeSynchronizer.Start();

        // Wait for the room config to be synced
        Room.RequestRoomConfiguration();
        await Room.RoomConfigTask;
    }

    public async Task QuitRoom()
    {
        if (Room is null) return;
        Room.Quit();
        Room.QueueFree();

        if (Multiplayer.MultiplayerPeer is not WebRtcMultiplayerPeer multiplayer)
        {
            GD.PrintErr("Leaving a room without being connected to a room.");
            return;
        }

        // Disconnect from all peers
        foreach (var peer in multiplayer.GetPeers())
        {
            var peerId = peer.Key.As<int>();
            multiplayer.DisconnectPeer(peerId);
            GD.Print($"Disconnected {peerId}");
        }

        // Close the multiplayer
        multiplayer.Close();
        Multiplayer.MultiplayerPeer = null;

        try
        {
            await signaling.LeaveRoom();
        }
        catch (Exception)
        {
            // ignored
        }
    }
}
