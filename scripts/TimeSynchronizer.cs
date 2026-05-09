using System.Reactive.Linq;
using System.Reactive.Subjects;
using AirBrawl2.scripts;
using Godot;

namespace AirBrawl2.Time;

// Algorithm based on https://en.wikipedia.org/wiki/Network_Time_Protocol#Clock_synchronization_algorithm
/// <summary>
/// Generate clock delta to synchronize clocks between peers.
/// The peer with the lowest peer id is the reference.
/// </summary>
public partial class TimeSynchronizer : Node
{
    private CancellationToken? timeSyncCancellationToken;
    private Subject<long> clockDeltaMeasurements = new();

    public BehaviorSubject<TimeSpan?> ClockDeltas = new(null);
    public TimeSpan ClockDelta => ClockDeltas.Value ?? TimeSpan.Zero;

    public void Start()
    {
        if (Singletons.RoomManager.Room is null)
        {
            GD.PrintErr("Cannot start time synchronization: Not in a room");
            return;
        }
        var room = Singletons.RoomManager.Room;

        // Determine reference player
        if (room.Players.Count == 0)
        {
            GD.PrintErr("Failed to start time synchronization, no players in room");
            return;
        }
        var referencePlayer = room.Players.MinBy(kv => kv.Key).Value;
        if (referencePlayer.PeerId == room.LocalPlayer.PeerId)
        {
            GD.Print("Stopping time synchronization because we are now the time reference");
            return;
        }

        // Cancel time synchronization when we quit the room
        var cts = new CancellationTokenSource();
        room.PlayerJoined.Subscribe(_ => { }, onCompleted: cts.Cancel, token: cts.Token); // Completes when quitting the room
        timeSyncCancellationToken = cts.Token;

        room.PlayerLeft.Subscribe(p => // When reference disconnects, restart time sync
            {
                if (p.PeerId != referencePlayer.PeerId) return;
                cts.Cancel();
                Start();
            },
            token: cts.Token
        );

        // Start
        CallDeferred(nameof(StartTimeSync), referencePlayer.PeerId);
    }

    private void StartTimeSync(int referencePeerId)
    {
        if (!timeSyncCancellationToken.HasValue)
        {
            GD.PrintErr("Time synchronization should have a cancellation token set.");
            return;
        }
        var ct = timeSyncCancellationToken.Value;

        // Reset observables
        clockDeltaMeasurements = new Subject<long>();
        ClockDeltas = new BehaviorSubject<TimeSpan?>(null);

        // Log when clock delta is updated
        // ClockDeltas.Subscribe(
        //     delta =>
        //     {
        //         if (delta is not null) GD.Print("[Time Sync] Clock delta changed: {Delta}", delta);
        //     },
        //     ct
        // );

        // `clockDeltaMeasurements` are the latencies measured. They are independent.
        // When a new delta is emitted we determine the average clock delta with the reference.
        // We discard the deltas that differ by more than 1 standard deviation from the median.
        // The purpose is to eliminate the packets that were retransmitted by tcp.
        const int windowLength = 9;
        var deltaWindow = new List<long>(windowLength);
        clockDeltaMeasurements
            .Select(newValue =>
            {
                if (deltaWindow.Count >= windowLength) deltaWindow.RemoveAt(0);
                deltaWindow.Add(newValue);

                var deltas = deltaWindow.ToArray();
                if (deltas.Length == 0) return (TimeSpan?)null;

                // Calculating standard deviation
                var average = deltas.Average();
                var variance = deltas
                    .Select(delta => Math.Pow(delta - average, 2))
                    .Average();
                var standardDeviation = Math.Sqrt(variance);

                // Finding median
                Array.Sort(deltas);
                var (q, r) = Math.DivRem(deltas.Length, 2);
                var median =
                    r == 1
                    ? deltas[q]
                    : (deltas[q] + deltas[q - 1]) / 2;

                // Filtering deltas
                var min = median - standardDeviation;
                var max = median + standardDeviation;
                var averageDelta = deltas
                    .Where(delta => min <= delta && delta <= max)
                    .Average();

                return TimeSpan.FromMilliseconds(averageDelta);
            })
            .Subscribe(avgDelta => ClockDeltas.OnNext(avgDelta), ct);

        // Start to poll
        _ = Task.Run(async () =>
        {
            for (var i = 0; i < windowLength * 3; i++)
            {
                if (ct.IsCancellationRequested) break;
                CallDeferred(nameof(PollTime), referencePeerId);
                try { await Task.Delay(1000, ct); }
                catch { /* ignored */ }
            }
        }, ct);
    }

    private void PollTime(int referencePeerId)
    {
        var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        RpcId(referencePeerId, nameof(PollTimeRpc), time);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void PollTimeRpc(long requestTime)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        RpcId(Multiplayer.GetRemoteSenderId(), nameof(AnswerRpc), requestTime, now);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void AnswerRpc(long requestTime, long receivedTime)
    {
        var currentTime = (DateTimeOffset.UtcNow + ClockDelta).ToUnixTimeMilliseconds();
        var clockDelta = (receivedTime - requestTime + receivedTime - currentTime) / 2;
        clockDeltaMeasurements.OnNext(clockDelta);
    }
}
