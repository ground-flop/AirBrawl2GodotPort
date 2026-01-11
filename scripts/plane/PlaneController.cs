using Godot;

public partial class PlaneController : Node3D
{
	Camera3D CameraObject;
	Node3D CameraTarget;
	PanelContainer Menu;
	PlaneBodyController PlaneBody;

	[Export]
	bool SinglePlayer = false;
	[Export]
	bool MouseYaw = false;
	[Export]
	float YawSensitivity = 3.0f;
	[Export]
	float RollSensitivity = 3.0f;
	[Export]
	float MaxSpeed = 250.0f;
	[Export]
	Vector3 StartPosition = new Vector3(100, 100, 0);
	[Export]
	float MinFov = 60.0f;
	[Export]
	float MaxFov = 120.0f;

	public override void _Ready()
	{
		CameraObject = GetNode<Camera3D>("PlaneCamera");
		CameraTarget = GetNode<Node3D>("PlaneBody/CamInterpolateTo");
		Menu = GetNode<PanelContainer>("PlaneBody/Control/menu");
		PlaneBody = GetNode<PlaneBodyController>("PlaneBody");

		loadSettings();
		GlobalPosition = StartPosition;

		PlaneBody.MaxSpeed = MaxSpeed;
		PlaneBody.Set("YawSensitivity", YawSensitivity);
		PlaneBody.Set("RollSensitivity", RollSensitivity);
		PlaneBody.Set("MouseYaw", MouseYaw);

		Godot.Input.SetMouseMode(Godot.Input.MouseModeEnum.Captured);
	}

	public override void _Process(double delta)
	{
		UpdateCamera(delta);
		if (Menu.Visible)
		{
			Godot.Input.SetMouseMode(Godot.Input.MouseModeEnum.Visible);
			return;
		}
		else
		{
			Godot.Input.SetMouseMode(Godot.Input.MouseModeEnum.Captured);
		}

		UpdateFov();
	}

	private void UpdateCamera(double delta)
	{
		float t = 1.0f - Mathf.Pow(0.001f, (float)delta);

		Transform3D from = CameraObject.GlobalTransform;
		Transform3D to = CameraTarget.GlobalTransform;

		CameraObject.GlobalTransform = from.InterpolateWith(to, t);
	}

	private void UpdateFov()
	{
		this.CameraObject.Fov = float.Lerp(MinFov, MaxFov, (float)(PlaneBody.Get("speed").AsDouble() / PlaneBody.Get("MaxSpeed").AsDouble()));
	}

	private void loadSettings()
	{
	}
}
