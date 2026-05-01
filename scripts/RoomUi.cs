using AirBrawl2.Networking;
using AirBrawl2.Networking.FSharpInterop;
using Godot;
using SignalingServer.Signaling;

namespace AirBrawl2.scripts;

public partial class RoomUi : Node
{
    // TODO: Use a source generator
    private Control joinRoomUi = null!;
    private Control loadingRoomUi = null!;
    private Control quitRoomUi = null!;

    private readonly Signaling signaling = new();
    private Room? room;

    public override void _EnterTree()
    {
        joinRoomUi = GetNode<Control>("./JoinRoom");
        loadingRoomUi = GetNode<Control>("./LoadingRoom");
        quitRoomUi = GetNode<Control>("./QuitRoom");

        joinRoomUi.Hide();
        quitRoomUi.Hide();
        loadingRoomUi.Show();

        var multiplayer = Multiplayer;
        Task.Run(async () =>
        {
            await signaling.ConnectSignalingServer(multiplayer);
            loadingRoomUi.CallDeferred(CanvasItem.MethodName.Hide);
            quitRoomUi.CallDeferred(CanvasItem.MethodName.Hide);
            joinRoomUi.CallDeferred(CanvasItem.MethodName.Show);
        });
    }

    private void ResetUi()
    {
        joinRoomUi.Show();
        quitRoomUi.Hide();
        loadingRoomUi.Hide();
    }

    private async void CreateRoom()
    {
        try
        {
            // Set loading
            joinRoomUi.Hide();
            loadingRoomUi.Show();

            var roomId = await signaling.CreateRoom();

            GD.Print($"Room id is {roomId}");
            GD.Print("Connected as 1");
            room = new Room();
            CallDeferred(Node.MethodName.AddChild, room);

            // Set joined
            ((Label)quitRoomUi.FindChild("RoomId")).Text = roomId.ToString();
            loadingRoomUi.Hide();
            quitRoomUi.Show();
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to join room: ", e);
            ResetUi();
        }
    }

    private async void JoinRoom()
    {
        try
        {
            var lineEdit = joinRoomUi.GetNode<LineEdit>("./Join/RoomId");
            if (lineEdit is null)
            {
                GD.PrintErr("Cannot find child line edit for room id");
                return;
            }

            var roomId = RoomId.tryParse(lineEdit.Text).ToNullable();
            if (roomId is null)
            {
                GD.PrintErr("Invalid room id");
                return;
            }

            // Set loading
            joinRoomUi.Hide();
            loadingRoomUi.Show();

            // Join room
            var peerId = await signaling.JoinRoom(roomId);
            room = new Room();
            CallDeferred(Node.MethodName.AddChild, room);
            GD.Print($"Connected as {peerId}");

            // TODO: Connects other players

            // Set joined
            quitRoomUi.GetNode<Label>("./RoomId").Text = roomId.ToString();
            loadingRoomUi.Hide();
            quitRoomUi.Show();
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to join room: ", e);
            ResetUi();
        }
    }

    private async void QuitRoom()
    {
        try
        {
            if (room is null) return;

            // Set loading
            quitRoomUi.Hide();
            loadingRoomUi.Show();

            // TODO: Disconnect players
            await signaling.LeaveRoom();

            ResetUi();
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to leave room: ", e);
            ResetUi();
        }
    }

    private void RoomIdSubmitted(string _) => JoinRoom();
}
