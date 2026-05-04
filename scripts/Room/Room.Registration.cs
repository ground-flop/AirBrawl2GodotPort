using System.Reactive.Subjects;
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
    public readonly Dictionary<int, Player> Players = new();
    private readonly Subject<Player> playerJoined = new();
    private readonly Subject<Player> playerLeft = new();

    public IObservable<Player> PlayerJoined => playerJoined;
    public IObservable<Player> PlayerLeft => playerLeft;

    public override void _EnterTree()
    {
        Multiplayer.MultiplayerPeer.PeerConnected += MultiplayerOnPeerConnected; // Register with future players
        Multiplayer.MultiplayerPeer.PeerDisconnected += MultiplayerOnPeerDisconnected;

        // Register with already connected players
        RegisterPlayerRpc(LocalPlayer.PeerId, LocalPlayer.Name);
    }

    private void MultiplayerOnPeerDisconnected(long id)
    {
        if (!Players.TryGetValue((int)id, out var player)) return;
        playerLeft.OnNext(player);
        Players.Remove(player.PeerId);
    }

    private void MultiplayerOnPeerConnected(long id) =>
        RegisterPlayerRpcId(id, LocalPlayer.PeerId, LocalPlayer.Name);

    public void RequestRoomConfiguration() => RequestRoomConfigRpcId(Players.Keys.Min());

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RegisterPlayer(int peerId, string name)
    {
        var newPlayer = new Player(peerId, name);
        Players.Add(peerId, newPlayer);
        playerJoined.OnNext(newPlayer);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RequestRoomConfig()
    {
        if (RoomConfig is null)
        {
            GD.PrintErr("Failed to send room config: Config is null");
            return;
        }
        SendRoomConfigurationRpcId(
            Multiplayer.GetRemoteSenderId(),
            RoomConfig.StartTime.ToBinary()
        );
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SendRoomConfiguration(long utcStartDateTime)
    {
        var startTime = DateTime.FromBinary(utcStartDateTime);
        RoomConfig = new RoomConfiguration(startTime);
        roomConfigTcs.TrySetResult(RoomConfig);
    }
}
