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

    private readonly CancellationTokenSource nodeInTreeCts = new();
    private CancellationToken NodeInTreeCt => nodeInTreeCts.Token;
    public override void _ExitTree() => nodeInTreeCts.Cancel();

    public override void _EnterTree()
    {
        Singletons.RoomManager.State.Subscribe(connected =>
        {
            if (connected)
            {
                ConnectRoomUi.CallDeferred(CanvasItem.MethodName.Show);
                LoadingRoomUi.CallDeferred(CanvasItem.MethodName.Hide);
            }
            else
            {
                ConnectRoomUi.CallDeferred(CanvasItem.MethodName.Hide);
                LoadingRoomUi.CallDeferred(CanvasItem.MethodName.Show);
            }

        }, NodeInTreeCt);
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
