using Godot;
using System.Text.Json.Serialization;
using System.Reactive.Subjects;
using AirBrawl2.Networking.FSharpInterop;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SignalingServer.Signaling;
using TypedSignalR.Client;

// ReSharper disable once CheckNamespace
namespace AirBrawl2.Networking;

public abstract class ConnectionAttempt(ConnectionAttemptId id, ISignalingHub hub)
{
    public readonly ConnectionAttemptId Id = id;
    protected readonly ISignalingHub Hub = hub;
    public readonly ReplaySubject<IceCandidate> Candidates = new();

    public void SendIceCandidate(IceCandidate candidate)
    {
        Task.Run(async () =>
        {
            var res = await Hub.SendIceCandidate(Id, candidate);
            if (res.HasError(out var error)) GD.PrintErr(error.ToLocalizedString());
        });
    }

    public void End()
    {
        Task.Run(async () =>
        {
            var res = await Hub.EndConnectionAttempt(Id);
            if (res.HasError(out var error)) GD.PrintErr(error.ToLocalizedString());
        });
    }
}

public class OffererConnectionAttempt(ConnectionAttemptId id, ISignalingHub hub) : ConnectionAttempt(id, hub)
{
    public readonly TaskCompletionSource<SdpDescription> Answer = new();
    public Task<SdpDescription> WaitAnswer() => Answer.Task;
}

public class AnswererConnectionAttempt(ConnectionAttemptId id, ISignalingHub hub, SdpDescription offer) : ConnectionAttempt(id, hub)
{
    public readonly SdpDescription Offer = offer;
    public void SendAnswer(SdpDescription answer)
    {
        Task.Run(async () =>
        {
            var res = await Hub.SendAnswer(Id, answer);
            if (res.HasError(out var error)) GD.PrintErr(error.ToLocalizedString());
        });
    }
}

/// <summary>
/// Interfaces the signaling hub for easier usage.
/// Also connects remote peers.
/// </summary>
public class Signaling
{
    private HubConnection connection = null!;
    private ISignalingHub hub = null!;
    private HubReceiver receiver = null!;

    /// <summary>
    /// Receive messages from signaling hub and populate connection attempts
    /// </summary>
    private class HubReceiver(MultiplayerApi multiplayer, Signaling signaling) : ISignalingClient
    {
        public readonly Dictionary<ConnectionAttemptId, ConnectionAttempt> ConnectionAttempts = [];

        public async Task<ConnectionAttemptId?> ConnectionRequested(int askingPeerId)
        {
            var peer = new PeerConnection();
            var multiplayerPeer = (WebRtcMultiplayerPeer)multiplayer.MultiplayerPeer;
            multiplayerPeer.AddPeer(peer, askingPeerId);

            return await peer.PublishOffer(signaling);
        }

        public Task SdpAnswerReceived(ConnectionAttemptId offerId, SdpDescription answer)
        {
            if (ConnectionAttempts[offerId] is OffererConnectionAttempt o) o.Answer.TrySetResult(answer);
            return Task.CompletedTask;
        }

        public Task IceCandidateReceived(ConnectionAttemptId offerId, IceCandidate iceCandidate)
        {
            ConnectionAttempts[offerId].Candidates.OnNext(iceCandidate);
            return Task.CompletedTask;
        }
    }

    public Task ConnectSignalingServer(MultiplayerApi multiplayer)
    {
        connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5001/webrtc-signaling")
            .WithAutomaticReconnect()
            .ConfigureLogging(builder => builder.AddProvider(new GodotLoggingProvider()))
            .AddJsonProtocol(options =>
            {
                // Enable F# types serialization
                JsonFSharpOptions
                    .Default()
                    .AddToJsonSerializerOptions(options.PayloadSerializerOptions);
            })
            .Build();

        connection.Closed += _ => Task.Run(() => GD.PrintErr("SignalR connection closed"));

        // Creating typed hub
        hub = connection.CreateHubProxy<ISignalingHub>();

        // Set up hub request handlers
        receiver = new HubReceiver(multiplayer, this);
        connection.Register<ISignalingClient>(receiver);

        // Starting the connection
        return connection.StartAsync();
    }

    public async Task<OffererConnectionAttempt> StartConnectionAttempt(SdpDescription offer)
    {
        var res = (await hub.StartConnectionAttempt(offer));
        if (res.HasError(out var error))
        {
            GD.PrintErr("Failed to publish offer: " + error.ToLocalizedString());
            throw new Exception(error.ToLocalizedString());
        }

        var connAttempt = new OffererConnectionAttempt(res.ResultValue, hub);
        receiver.ConnectionAttempts.Add(connAttempt.Id, connAttempt);

        return connAttempt;
    }

    public async Task<AnswererConnectionAttempt> JoinConnectionAttempt(ConnectionAttemptId connectionAttemptId)
    {
        var res = (await hub.JoinConnectionAttempt(connectionAttemptId));
        if (res.HasError(out var error))
        {
            GD.PrintErr(error.ToLocalizedString());
            throw new Exception(error.ToLocalizedString());
        }

        var connAttempt = new AnswererConnectionAttempt(connectionAttemptId, hub, res.ResultValue);
        receiver.ConnectionAttempts.Add(connAttempt.Id, connAttempt);

        return connAttempt;
    }

    public async Task<RoomId> CreateRoom()
    {
        var res = await hub.CreateRoom();
        if (!res.HasError(out var error)) return res.ResultValue;

        GD.PrintErr(error.ToLocalizedString());
        throw new Exception(error.ToLocalizedString());
    }

    public async Task<int> JoinRoom(RoomId roomId)
    {
        var res = await hub.JoinRoom(roomId);
        if (!res.HasError(out var error)) return res.ResultValue;

        GD.PrintErr(error.ToLocalizedString());
        throw new Exception(error.ToLocalizedString());
    }

    public async Task<RoomConnectionInfo> GetConnectionInfo()
    {
        var res = await hub.ConnectToRoomPlayers();
        if (!res.HasError(out var error)) return res.ResultValue;

        GD.PrintErr(error.ToLocalizedString());
        throw new Exception(error.ToLocalizedString());
    }

    public async Task LeaveRoom()
    {
        var res = await hub.LeaveRoom();
        if (!res.HasError(out var error)) return;

        GD.PrintErr(error.ToLocalizedString());
        throw new Exception(error.ToLocalizedString());
    }
}

public class GodotLoggingProvider : ILoggerProvider
{
    public void Dispose() {}
    public ILogger CreateLogger(string categoryName) => new GodotLogger();
}

public class GodotLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        switch (logLevel)
        {
            case LogLevel.Critical:
            case LogLevel.Error:
                var s1 = formatter.Invoke(state, exception);
                GD.PrintErr(s1);
                GD.PushError(s1);
                break;
            case LogLevel.Warning:
                var s2 = formatter.Invoke(state, exception);
                GD.PrintS("WARNING: ", s2);
                GD.PushWarning(s2);
                break;
            case LogLevel.Debug:
            case LogLevel.Information:
            case LogLevel.Trace:
            case LogLevel.None:
                GD.PrintS(formatter.Invoke(state, exception));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }
}
