# Room management in AirBrawl 2

_The connection process between players is elaborated in the README of the `Networking` folder._

## Room
A room represent of group a players each connected to each others, forming a mesh.
AirBrawl keeps track of the connected players using the `Room` class. The definition of this class
is spread across several files.  

The `Room` class is created during the connection process by the [`RoomManager`](#room-manager)

### Room configuration
The room configuration contains information about the room like the game start time.
It is synced during [registration](#registration).

### Registration
_Registration methods and properties of a room are in `Room.Registration.cs`_  
Registration is the process of informing players in the room that a new player just joined it.  

### Spawning
For multiplayer synchronizers to not bug, the room need to keep track of who spawned which player's plane.  

A `MultiplayerSynchronizer` tries to sync its data as soon as two players are connected.  
If a `MultiplayerSynchronizer` node exists on one machine, it will send data to its corresponding
`MultiplayerSynchronizer` on the other machine.  
But if this happens when the synchronizer is only spawned on a machine `A` and not on a machine `B`,
when `A` sends its data to `B`, Godot will look for a corresponding synchronizer and fail to find it.  
When `B` eventually spawns the corresponding synchronizer, it will not sync data.

To fix that, each player disable the visibility of its own `MultiplayerSynchronizer` for everyone,  
and when someone spawns a plane, it asks the concerned player to enable visibility for them.

That is the purpose of the events defined in `Room.Spawning.cs`.  
Events are consumed in the `PlaneController` class.

## Room manager
The `RoomManager` is the class permits to create or join a room.  
This class glues the `Signaling`, `PeerConnection` and `Room` classes to connect players.

It is an Autoload/Singleton meaning it is instantiated only once by Godot when the game starts.

For more information, read the `RoomManager.cs` file.

## Time synchronization
Time is synced between players.  
Each player computes a time offset with the reference player/machine using an algorithm similar to NTP.  
The reference is the player that joined the room the soonest (i.e. with the lowest peer id).  
For more information, see the `TimeSynchronizer.cs` file. (It is not in the `Room` folder)
