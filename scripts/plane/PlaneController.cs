using System;
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

    float Health = 100f;
    [Export]
    float MaxHealth = 100f;

    public override void _Ready()
    {
        CameraObject = GetNode<Camera3D>("PlaneCamera");
        CameraTarget = GetNode<Node3D>("PlaneBody/CamInterpolateTo");
        Menu = GetNode<PanelContainer>("PlaneBody/Control/menu");
        PlaneBody = GetNode<PlaneBodyController>("PlaneBody");

        LoadSettings();
        GlobalPosition = StartPosition;


        Godot.Input.SetMouseMode(Godot.Input.MouseModeEnum.Captured);
        Health = MaxHealth;

        InitializePlaneBody();
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

    private void LoadSettings()
    {
        ConfigFile cfg = new ConfigFile();

        if (cfg.Load("user://settings.cfg") == Godot.Error.Ok)
        {
            MouseYaw = (bool)cfg.GetValue("controls", "MouseYaw", false);
            YawSensitivity = (float)cfg.GetValue("controls", "YawSensitivity", 3.0f);
            RollSensitivity = (float)cfg.GetValue("controls", "RollSensitivity", 3.0f);
        }
    }

    private void SetMouseYaw(bool MouseYaw)
    {
        this.MouseYaw = MouseYaw;
        PlaneBody.MouseYaw = MouseYaw;
        SaveSettings();
    }

    private void SetYawSensitivity(float YawSensitivity)
    {
        this.YawSensitivity = YawSensitivity;
        PlaneBody.YawSensitivity = YawSensitivity;
        SaveSettings();
    }

    private void SetRollSensitivity(float RollSensitivity)
    {
        this.RollSensitivity = RollSensitivity;
        PlaneBody.RollSensitivity = RollSensitivity;
        SaveSettings();
    }

    private void SaveSettings()
    {
        ConfigFile cfg = new ConfigFile();

        cfg.SetValue("controls", "MouseYaw", MouseYaw);
        cfg.SetValue("controls", "YawSensitivity", YawSensitivity);
        cfg.SetValue("controls", "RollSensitivity", RollSensitivity);

        cfg.Save("user://settings.cfg");
    }

    public void ChangeHealth(float changeHealth)
    {
        Health += changeHealth;
        if (Health < 1f)
        {
            Spawn();
        }
    }

    private void Spawn()
    {
        Health = MaxHealth;

        PlaneBodyController OldPlane = PlaneBody;
        Node NewPlane = GD.Load<PackedScene>("Scenes/Plane/PlaneBody.tscn").Instantiate();
        AddChild(NewPlane);
        PlaneBody = (PlaneBodyController)NewPlane;
        CameraTarget = PlaneBody.GetNode<Node3D>("CamInterpolateTo");
        InitializePlaneBody();
        OldPlane.QueueFree();
    }

    public void OnImpact(float ImpactVelocity)
    {

        ChangeHealth(-PlaneBody.LinearVelocity.Length() * 2);
    }

    private void InitializePlaneBody()
    {
        PlaneBody.MaxSpeed = MaxSpeed;
        PlaneBody.YawSensitivity = YawSensitivity;
        PlaneBody.RollSensitivity = RollSensitivity;
        PlaneBody.MouseYaw = MouseYaw;

        PlaneBody.OnImpactEvent += OnImpact;
    }
}
