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
        CallDeferred(Node.MethodName.AddChild, room);

        return roomId;
    }

    public async Task JoinRoom(RoomId roomId)
    {
        var peerId = await signaling.JoinRoom(roomId);
        room = new Room();
        CallDeferred(Node.MethodName.AddChild, room);

        // TODO: Connects other players
    }

    public async Task QuitRoom()
    {
        if (room is null) return;

        // TODO: Disconnect players

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
