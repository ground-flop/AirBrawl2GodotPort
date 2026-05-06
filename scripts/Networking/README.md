# The networking related files of AirBrawl 2

AirBrawl 2 uses [WebRTC](https://webrtc.org/) to connects players.  
Each player is connected to each other, forming a mesh.

## What happens during a connection initialization ?
A connection initialization happens between 2 peers.  
On peer is the offerer and post an offer.  
The other peer is the answerer and answer to the offer.  

After the offer is answered, each peer start emitting ice candidates. These contain the information of how the peers can connect through Internet.  
When the peers exchanged the ice candidates, the connection is set up and they are connected.

All of this is the **signaling** process. And it requires a server the two peers can connect to be done.  
After being exchanged, the offer, the answer and the ice candidates are processed by the WebRTC library embedded in Godot.  
So the only thing to code is the exchange of these objects.

## Signaling server  
The server used by AirBrawl 2 for that is located [here](https://github.com/ground-flop/WebRTC-Signaling-server)
It is made using [F#](https://fsharp.org/) and [SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction), a WebSocket library.

In order to communicate with the server, AirBrawl needs several things:
- The types used by the server.
  They are stored in the `scripts/External/signaling-server` git submodule.
- [FSharp.SystemTextJson](https://github.com/tarmil/fsharp.systemtextjson) to serialize F# types.
- [TypedSignalR.Client](https://github.com/nenoNaninu/TypedSignalR.Client) for type safety.

## Code structure
### Signaling.cs
This file contains the code to communicate with the server. 
- The `Signaling` class is used to send messages to the server.
- The `HubReceiver` class handles the messages from the server.
- The `OffererConnectionAttempt` and `AnswererConnectionAttempt` classes are abstractions to send and receive ice candidates.  
  Each instance is created and feed of incoming ice candidates by the `Signaling` class.

### PeerConnection.cs
`PeerConnection` is an extension of the Godot `WebRtcPeerConnection` class.  
It emits and sends the offer/answer and ice candidates through a `Signaling` instance passed in parameter.

### The `webrtc` folder
This folder contains the binaries of the WebRTC library of Godot for non-HTML5 platforms. (cf. [Godot's doc](https://docs.godotengine.org/en/stable/tutorials/networking/webrtc.html#using-webrtc-in-godot))  

Here are the sources: [Godot WebRTC Native Plugin](https://github.com/godotengine/webrtc-native)
