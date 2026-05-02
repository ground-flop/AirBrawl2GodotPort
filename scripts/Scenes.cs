using Godot;

namespace AirBrawl2.scripts;

public partial class Scenes : Node
{
    public readonly PackedScene HomeScene = ResourceLoader.Load<PackedScene>("res://Scenes/Home.tscn");
    public readonly PackedScene GameScene = ResourceLoader.Load<PackedScene>("res://Scenes/Game.tscn");
}
