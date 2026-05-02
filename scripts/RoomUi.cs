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

    public override void _EnterTree()
    {
        joinRoomUi = GetNode<Control>("./JoinRoom");
        loadingRoomUi = GetNode<Control>("./LoadingRoom");
        quitRoomUi = GetNode<Control>("./QuitRoom");

        joinRoomUi.Hide();
        quitRoomUi.Hide();
        loadingRoomUi.Show();

        RoomManager.Instance.RoomManagerReady += () =>
        {
            loadingRoomUi.CallDeferred(CanvasItem.MethodName.Hide);
            quitRoomUi.CallDeferred(CanvasItem.MethodName.Hide);
            joinRoomUi.CallDeferred(CanvasItem.MethodName.Show);
        };
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

            var roomId = await RoomManager.Instance.CreateRoom();

            // Set joined
            ((Label)quitRoomUi.FindChild("RoomId")).Text = roomId.ToString();
            loadingRoomUi.Hide();
            quitRoomUi.Show();
            DisplayServer.ClipboardSet(roomId.ToString());
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

            await RoomManager.Instance.JoinRoom(roomId);

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
            // Set loading
            quitRoomUi.Hide();
            loadingRoomUi.Show();

            await RoomManager.Instance.QuitRoom();
        }
        catch (Exception)
        {
            // ignore
        }
        finally
        {
            ResetUi();
        }
    }

    private void RoomIdSubmitted(string _) => JoinRoom();
}
