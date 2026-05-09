using AirBrawl2.Networking;
using Godot;

namespace AirBrawl2.scripts;

[SceneTree("../Scenes/Game.tscn", root: "nodes")]
public partial class Game : Node
{
    private Room room = null!;
    private RoomConfiguration roomConfig = null!;
    private bool countdownRunning = true;

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

        SetupUi();
    }

    public override void _Process(double delta)
    {
        if (!countdownRunning) return;
        var elapsed = UpdateCountdown();
        if (!elapsed) return;

        countdownRunning = false;
        nodes.Pre_game.Get().Hide();
        nodes.PlaneSpawner.SpawnPlanes(room);
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
