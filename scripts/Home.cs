using AirBrawl2.Networking.FSharpInterop;
using Godot;
using SignalingServer.Signaling;

namespace AirBrawl2.scripts;

[SceneTree("../Scenes/Home.tscn", root: "nodes")]
public partial class Home : Node
{
    private Control ConnectRoomUi => nodes.UI.Margin.Connect;
    private LineEdit RoomIdLineEdit => nodes.UI.Margin.Connect.Join.RoomId;

    private Control LoadingRoomUi => nodes.UI.Margin.Loading;

    public override void _EnterTree()
    {
        ConnectRoomUi.Hide();
        LoadingRoomUi.Show();

        if (Singletons.RoomManager.Ready)
        {
            ConnectRoomUi.Show();
            LoadingRoomUi.Hide();
        }
        else
            Singletons.RoomManager.RoomManagerReady += () =>
            {
                LoadingRoomUi.CallDeferred(CanvasItem.MethodName.Hide);
                ConnectRoomUi.CallDeferred(CanvasItem.MethodName.Show);
            };
    }

    private void ResetUi()
    {
        ConnectRoomUi.Show();
        LoadingRoomUi.Hide();
    }

    private void ChangeScene() => GetTree().ChangeSceneToPacked(Singletons.Scenes.GameScene);

    private async void CreateRoom()
    {
        try
        {
            // Set loading
            ConnectRoomUi.Hide();
            LoadingRoomUi.Show();

            var room = await Singletons.RoomManager.CreateRoom();
            DisplayServer.ClipboardSet(room.RoomId.ToString()); // Copy room id to clipboard

            ChangeScene();
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
            var roomId = RoomId.tryParse(RoomIdLineEdit.Text).ToNullable();
            if (roomId is null)
            {
                GD.PrintErr("Invalid room id");
                return;
            }

            // Set loading
            ConnectRoomUi.Hide();
            LoadingRoomUi.Show();

            await Singletons.RoomManager.JoinRoom(roomId);
            ChangeScene();
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to join room: ", e);
            ResetUi();
        }
    }

    private void RoomIdSubmitted(string _) => JoinRoom();
}
