using AirBrawl2.Networking;
using Godot;
using SignalingServer.Signaling;

namespace AirBrawl2.scripts;

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

    public async Task<RoomId> CreateRoom()
    {
        var roomId = await signaling.CreateRoom();
        room = new Room();
        AddChild(room);

        var multiplayer = new WebRtcMultiplayerPeer();
        multiplayer.CreateMesh(1);
        Multiplayer.MultiplayerPeer = multiplayer;

        return roomId;
    }

    public async Task JoinRoom(RoomId roomId)
    {
        var peerId = await signaling.JoinRoom(roomId);
        room = new Room();
        AddChild(room);

        var multiplayer = new WebRtcMultiplayerPeer();
        multiplayer.CreateMesh(peerId);
        Multiplayer.MultiplayerPeer = multiplayer;

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
    }

    public async Task QuitRoom()
    {
        if (room is null) return;
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
