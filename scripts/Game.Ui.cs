using AirBrawl2.Networking;
using Godot;

namespace AirBrawl2.scripts;

public partial class Game
{
    private Label RoomIdLabel => nodes.Pre_game.Margin.QuitRoom.RoomId;
    private Control PlayersList => nodes.Pre_game.Margin.Players;
    private Label CountdownLabel => nodes.Pre_game.Margin.StartCountdown;

    private void SetupUi()
    {
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

    private bool UpdateCountdown()
    {
        var remainingTime = roomConfig.StartTime - DateTime.UtcNow;
        var elapsed = remainingTime <= TimeSpan.Zero;
        CountdownLabel.Text =
            elapsed
                ? string.Empty
                : $@"Start in {remainingTime:s\.f}s";

        return elapsed;
    }
}
