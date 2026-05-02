using AirBrawl2.Networking;
using AirBrawl2.Networking.FSharpInterop;
using Godot;
using SignalingServer.Signaling;

namespace AirBrawl2.scripts;

[SceneTree("../Scenes/level.tscn", root: "nodes")]
public partial class RoomUi : Node
{
    private Control ConnectRoomUi => nodes.UI.Margin.Connect;
    private LineEdit RoomIdLineEdit => nodes.UI.Margin.Connect.Join.RoomId;

    private Control LoadingRoomUi => nodes.UI.Margin.Loading;

    private Control QuitRoomUi => nodes.UI.Margin.QuitRoom;
    private Label RoomIdLabel => nodes.UI.Margin.QuitRoom.RoomId;
    private Control PlayersListUi => nodes.UI.Margin.Players;

    public override void _EnterTree()
    {
        ConnectRoomUi.Hide();
        QuitRoomUi.Hide();
        LoadingRoomUi.Show();
        PlayersListUi.Hide();

        RoomManager.Instance.RoomManagerReady += () =>
        {
            LoadingRoomUi.CallDeferred(CanvasItem.MethodName.Hide);
            QuitRoomUi.CallDeferred(CanvasItem.MethodName.Hide);
            ConnectRoomUi.CallDeferred(CanvasItem.MethodName.Show);
        };
    }

    private void ResetUi()
    {
        ConnectRoomUi.Show();
        QuitRoomUi.Hide();
        LoadingRoomUi.Hide();
        PlayersListUi.Hide();
    }

    private async void CreateRoom()
    {
        try
        {
            // Set loading
            ConnectRoomUi.Hide();
            LoadingRoomUi.Show();

            var room = await RoomManager.Instance.CreateRoom();

            // Set joined
            RoomIdLabel.Text = room.RoomId.ToString();
            LoadingRoomUi.Hide();
            QuitRoomUi.Show();
            PlayersListUi.Show();
            BindRoomToPlayerList(room);
            DisplayServer.ClipboardSet(room.RoomId.ToString()); // Copy room id to clipboard
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

            var room = await RoomManager.Instance.JoinRoom(roomId);

            // Set joined
            RoomIdLineEdit.Clear();
            RoomIdLabel.Text = roomId.ToString();
            LoadingRoomUi.Hide();
            QuitRoomUi.Show();
            PlayersListUi.Show();
            BindRoomToPlayerList(room);
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to join room: ", e);
            ResetUi();
        }
    }

    private void BindRoomToPlayerList(Room room)
    {
        room.PlayerJoined.Subscribe(AddPlayerToUi);
        foreach (var playersValue in room.Players.Values) AddPlayerToUi(playersValue);

        room.PlayerLeft.Subscribe(player => PlayersListUi
            .GetNodeOrNull(player.PeerId.ToString())
            ?.QueueFree()
        );

        return;
        void AddPlayerToUi(Player newPlayer) => PlayersListUi.AddChild(new Label { Name = newPlayer.PeerId.ToString(), Text = newPlayer.Name });
    }

    private async void QuitRoom()
    {
        try
        {
            // Set loading
            QuitRoomUi.Hide();
            LoadingRoomUi.Show();
            PlayersListUi.Hide();
            foreach (var child in PlayersListUi.GetChildren())
                if (child is Label) child.QueueFree();

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
