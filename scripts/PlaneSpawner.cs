using AirBrawl2.Networking;
using Godot;

namespace AirBrawl2.scripts;

public partial class PlaneSpawner : Node
{
    [Export] private NodePath spawnPath = null!;
    [Export] private PackedScene planeScene = null!;

    private void SpawnPlaneScene(int peerId)
    {
        var newPlane = planeScene.Instantiate<PlaneController>();
        newPlane.Name = peerId.ToString();
        GetNode(spawnPath).AddChild(newPlane);
    }

    public void SpawnPlanes(Room room)
    {
        foreach (var player in room.Players.Values)
            SpawnPlaneScene(player.PeerId);
        Rpc(nameof(SpawnPlaneRpc));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SpawnPlaneRpc()
    {
        var peerIdToSpawn = Multiplayer.GetRemoteSenderId();
        var spawnedPlanes = GetNode(spawnPath).GetChildren();
        if (spawnedPlanes.Any(plane => plane.GetMultiplayerAuthority() == peerIdToSpawn)) return;
        SpawnPlaneScene(peerIdToSpawn);
    }
}
