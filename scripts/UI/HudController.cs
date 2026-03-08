using Godot;
using System;

public partial class HudController : Control
{

	Label SpeedLabel;
	ProgressBar HealthBar;
	ProgressBar BoostBar;

	public override void _Ready()
	{
		SpeedLabel = GetNode<Label>("Speed");
        HealthBar = GetNode<ProgressBar>("Health");
        BoostBar = GetNode<ProgressBar>("Boost");
	}

	public void SetSpeed(float Speed)
	{
		SpeedLabel.Text = Math.Floor(Speed).ToString();
	}

	public void SetHealth(float Health)
	{
        HealthBar.Value = Health;
	}

	public void SetBoost(float Boost)
	{
        BoostBar.Value = Boost;
	}
}
