using Godot;

namespace AirBrawl2.scripts;

public partial class PlaneSpawner : MultiplayerSpawner
{
    [Export] private PackedScene planeScene = null!;

    public void SpawnPlane()
    {
        GD.Print($"Spawning plane: {Multiplayer.GetPeers().Length}");
        RpcId(GetMultiplayerAuthority(), nameof(SpawnPlaneRpc));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SpawnPlaneRpc()
    {
        var peerIdToSpawn = Multiplayer.GetRemoteSenderId();

        var newPlane = planeScene.Instantiate<PlaneController>();
        newPlane.Name = peerIdToSpawn.ToString();

        GetNode(SpawnPath).AddChild(newPlane);
    }
}
