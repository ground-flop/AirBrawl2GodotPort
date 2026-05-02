using AirBrawl2.Networking;
using Godot;

namespace AirBrawl2.scripts;

[SceneTree("../Scenes/Game.tscn", root: "nodes")]
public partial class GameUi : Node
{
    private Label RoomIdLabel => nodes.Pre_game.Margin.QuitRoom.RoomId;
    private Control PlayersList => nodes.Pre_game.Margin.Players;
    private Label CountdownLabel => nodes.Pre_game.Margin.StartCountdown;

    private Room room = null!;
    private RoomConfiguration roomConfig = null!;

    public override void _EnterTree()
    {
        if (Singletons.RoomManager.Room is null)
        {
            GD.PrintErr("Not in a room");
            return;
        }
        room = Singletons.RoomManager.Room;

        if (room.RoomConfig is null)
        {
            GD.PrintErr("Room config not synced");
            return;
        }
        roomConfig = room.RoomConfig;

        RoomIdLabel.Text = room.RoomId.ToString();

        // Bind players to UI
        foreach (var playersValue in room.Players.Values) AddPlayerToUi(playersValue);
        room.PlayerJoined.Subscribe(AddPlayerToUi);

        room.PlayerLeft.Subscribe(player => PlayersList
            .GetNodeOrNull(player.PeerId.ToString())
            ?.QueueFree()
        );

        return;
        void AddPlayerToUi(Player newPlayer) => PlayersList.AddChild(new Label { Name = newPlayer.PeerId.ToString(), Text = newPlayer.Name });
    }

    public override void _Process(double delta)
    {
        var remainingTime = roomConfig.StartTime - DateTime.UtcNow;
        CountdownLabel.Text =
            remainingTime > TimeSpan.Zero
                ? $@"Start in {remainingTime:s\.f}s"
                : "";
    }

    private async void QuitRoom()
    {
        try
        {
            foreach (var child in PlayersList.GetChildren())
                if (child is Label) child.QueueFree();

            await Singletons.RoomManager.QuitRoom();
            GetTree().ChangeSceneToPacked(Singletons.Scenes.HomeScene);
        }
        catch (Exception)
        {
            // ignore
        }
    }
}
