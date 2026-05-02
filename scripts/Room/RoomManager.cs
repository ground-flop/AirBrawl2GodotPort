using Godot;
using SignalingServer.Signaling;

namespace AirBrawl2.Networking;

public partial class RoomManager : Node
{
    // TODO: Use a source generator
    public static RoomManager Instance =>
        field ??= (Engine.GetMainLoop() as SceneTree)!.Root.GetNodeOrNull<RoomManager>("/root/RoomManager");

    private readonly Signaling signaling = new();
    private Room? room;

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

        room = new Room(roomId, 1);
        AddChild(room);

        return room;
    }

    public async Task<Room> JoinRoom(RoomId roomId)
    {
        var peerId = await signaling.JoinRoom(roomId);

        var multiplayer = new WebRtcMultiplayerPeer();
        multiplayer.CreateMesh(peerId);
        Multiplayer.MultiplayerPeer = multiplayer;

        room = new Room(roomId, peerId);
        AddChild(room);

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
        return room;
    }

    public async Task QuitRoom()
    {
        if (room is null) return;
        room.Quit();
        room.QueueFree();

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
