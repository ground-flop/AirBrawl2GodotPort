using Godot;

namespace AirBrawl2.Networking;

// Manage players registration.
//
// - When one player connects to the room, they send their name
//   to the others and the others send their names to them.
// - The player with the minimum peer id sends the room
//   configuration to players when they connect
// That is the "registration" process.


public partial class Room
{
    public override void _EnterTree()
    {
        Multiplayer.MultiplayerPeer.PeerConnected += MultiplayerOnPeerConnected; // Register with future players
        Multiplayer.MultiplayerPeer.PeerDisconnected += MultiplayerOnPeerDisconnected; // TODO: Remove handlers when disconnected

        // Register with already connected players
        Rpc(nameof(RegisterPlayerRpc), LocalPlayer.PeerId, LocalPlayer.Name);
    }

    private void MultiplayerOnPeerDisconnected(long id)
    {
        GD.Print($"MultiplayerOnPeerDisconnected {(int)id}");
        if (!Players.TryGetValue((int)id, out var player)) return;
        playerLeft.OnNext(player);
        Players.Remove(player.PeerId);
    }

    private void MultiplayerOnPeerConnected(long id)
    {
        RpcId(id, nameof(RegisterPlayerRpc), LocalPlayer.PeerId, LocalPlayer.Name);

        if (Players.Count == 0) return;
        if (Multiplayer.GetUniqueId() == Players.Keys.Min())
            Task.Run(async () =>
            {
                var roomConfig = await RoomConfig;
                CallDeferred(Node.MethodName.RpcId,
                    id,
                    nameof(SendRoomConfigurationRpc),
                    roomConfig.StartTime.ToBinary()
                );
            });
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RegisterPlayerRpc(int peerId, string name)
    {
        var newPlayer = new Player(peerId, name);
        Players.Add(peerId, newPlayer);
        playerJoined.OnNext(newPlayer);

        GD.Print($"Registered player {name}");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SendRoomConfigurationRpc(long utcStartDateTime)
    {
        var startTime = DateTime.FromBinary(utcStartDateTime);
        var config = new RoomConfiguration(startTime);
        roomConfigTcs.TrySetResult(config);
    }
}
