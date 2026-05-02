using Godot;
using System.Reactive.Subjects;
using SignalingServer.Signaling;

namespace AirBrawl2.Networking;

/// <summary>
/// Extension of WebRtcPeerConnection
/// </summary>
public partial class PeerConnection : WebRtcPeerConnection
{
    private readonly ReplaySubject<IceCandidate> iceCandidates = new();
    private static readonly Godot.Collections.Dictionary IceServers = new() {
        {
            "iceServers",
            new Godot.Collections.Array {
                new Godot.Collections.Dictionary { { "urls", "stun:stun.relay.metered.ca:80" } },
                new Godot.Collections.Dictionary { { "urls", "stun:stun.l.google.com:19302" } },
                new Godot.Collections.Dictionary { { "urls", "stun:stun.l.google.com:5349" } },
                new Godot.Collections.Dictionary { { "urls", "stun:stun1.l.google.com:3478" } },
                new Godot.Collections.Dictionary { { "urls", "stun:stun1.l.google.com:5349" } },
                new Godot.Collections.Dictionary { { "urls", "stun:stun2.l.google.com:19302" } },
                new Godot.Collections.Dictionary { { "urls", "stun:stun2.l.google.com:5349" } },
                new Godot.Collections.Dictionary { { "urls", "stun:stun3.l.google.com:3478" } },
                new Godot.Collections.Dictionary { { "urls", "stun:stun3.l.google.com:5349" } },
                new Godot.Collections.Dictionary { { "urls", "stun:stun4.l.google.com:19302" } },
                new Godot.Collections.Dictionary { { "urls", "stun:stun4.l.google.com:5349" } }
                // TODO: Add turn servers
            }
        }
    };

    public PeerConnection()
    {
        IceCandidateCreated += (media, index, name) =>
        {
            var ic = new IceCandidate(media, (int)index, name);
            iceCandidates.OnNext(ic);
        };

        Initialize(IceServers);
    }

    private bool IsConnected() => GetConnectionState() == ConnectionState.Connected;
    private void SetRemoteSdpDescription(SdpDescription sdp) => SetRemoteDescription(sdp.type, sdp.sdp);
    private void AddIceCandidate(IceCandidate ice) => AddIceCandidate(ice.media, ice.index, ice.name);

    /// <summary>
    /// Publish an offer on the signaling server, create a connection attempt and handle connection.
    /// </summary>
    /// <returns>Return a connection attempt id</returns>
    public Task<ConnectionAttemptId> PublishOffer(Signaling signaling)
    {
        var tcs = new TaskCompletionSource<ConnectionAttemptId>();

        // SDP offer created => Create a connection attempt by publishing the offer
        SessionDescriptionCreated += async (type, sdp) =>
        {
            var connAttempt = await signaling.StartConnectionAttempt(new SdpDescription(type, sdp));
            tcs.TrySetResult(connAttempt.Id);

            var answer = await connAttempt.WaitAnswer();
            SetRemoteSdpDescription(answer);

            // Exchange ice candidates
            var sub1 = connAttempt.Candidates.Subscribe(AddIceCandidate);
            var sub2 = iceCandidates.Subscribe(connAttempt.SendIceCandidate);

            // WaitUntil IsConnected
            await Task.Run(async () =>
            {
                while (!IsConnected()) await Task.Delay(25);
            });
            sub1.Dispose();
            sub2.Dispose();

            // GD.Print("Connection established.");
            connAttempt.End();
        };

        CreateOffer(); // Create offer and trigger the event
        return tcs.Task;
    }

    /// <summary>
    /// Join connection attempt, send answer and handle ice candidate exchange
    /// </summary>
    /// <remarks>Returns only when connection is established</remarks>
    public async Task AnswerConnectionOffer(Signaling signaling, ConnectionAttemptId connectionAttemptId)
    {
        var tcs = new TaskCompletionSource();

        // Retrieve offer
        var connAttempt = await signaling.JoinConnectionAttempt(connectionAttemptId);

        // Send answer when created
        SessionDescriptionCreated += async (type, sdp) =>
        {
            var answer = new SdpDescription(type, sdp);
            connAttempt.SendAnswer(answer);

            // Exchange ice candidates
            var sub1 = connAttempt.Candidates.Subscribe(AddIceCandidate);
            var sub2 = iceCandidates.Subscribe(connAttempt.SendIceCandidate);

            // Await connection
            await Task.Run(async () =>
            {
                while (!IsConnected()) await Task.Delay(25);
            });
            sub1.Dispose();
            sub2.Dispose();
            tcs.SetResult();
        };

        SetRemoteSdpDescription(connAttempt.Offer);
        await tcs.Task;
    }
}
