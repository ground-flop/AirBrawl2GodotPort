using System.Reactive.Subjects;
using Godot;

namespace AirBrawl2.Networking;

// Notifications to know who spawned which plane

public partial class Room
{
    private readonly ReplaySubject<int> planeSpawned = new();
    private readonly ReplaySubject<int> planeDespawned = new();
    public IObservable<int> PlaneSpawned => planeSpawned;
    public IObservable<int> PlaneDespawned => planeDespawned;

    public void SpawnedPlane(int planePeerId) => RpcId(planePeerId,  nameof(PlaneSpawnedRpc));
    public void DespawnedPlane(int planePeerId) => RpcId(planePeerId,  nameof(PlaneDespawnedRpc));

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void PlaneSpawnedRpc() => planeSpawned.OnNext(Multiplayer.GetRemoteSenderId());
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void PlaneDespawnedRpc() => planeDespawned.OnNext(Multiplayer.GetRemoteSenderId());
}
