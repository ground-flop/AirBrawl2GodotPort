using AirBrawl2.Networking;
using AirBrawl2.scripts;
using Godot;

[SceneTree("../../Scenes/Plane/plane.tscn", true)]
public partial class PlaneController : Node3D
{
    private Camera3D  CameraObject => _.PlaneCamera;
    private Node3D  CameraTarget => _.PlaneBody.CamInterpolateTo;
    private PanelContainer  Menu => _.PlaneBody.Control.menu;
    private PlaneBodyController  PlaneBody => _.PlaneBody;
    private Godot.Timer  RegenerationTimer => _.RegenerationTimer;
    private MultiplayerSynchronizer Synchronizer => _.MultiplayerSynchronizer;

    [Export] private bool SinglePlayer;
    [Export] private bool MouseYaw;
    [Export] private float YawSensitivity = 3.0f;
    [Export] private float RollSensitivity = 3.0f;
    [Export] private float MaxSpeed = 250.0f;
    [Export] private Vector3 StartPosition = new(100, 100, 0);
    [Export] private float MinFov = 60.0f;
    [Export] private float MaxFov = 120.0f;
    [Export] private float RegenerationRequirementTime = 1f;
    [Export] private float RegenerationDuration = 1.5f;

    private float Health = 100f;
    [Export] private float MaxHealth = 100f;

    private Room room = null!;
    private readonly CancellationTokenSource nodeAliveCts = new();
    private CancellationToken NodeAliveCt => nodeAliveCts.Token;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(int.Parse(Name));
        GlobalPosition = StartPosition;

        if (Singletons.RoomManager.Room is null)
        {
            GD.PrintErr("Cannot initialize plane: room is null");
            return;
        }
        room = Singletons.RoomManager.Room;

        if (!IsMultiplayerAuthority())
        {
            room.SpawnedPlane(GetMultiplayerAuthority());
            return;
        }

        Synchronizer.SetVisibilityPublic(false);
        room.PlaneSpawned.Subscribe(remotePeerId => Synchronizer.SetVisibilityFor(remotePeerId, true), NodeAliveCt);
        room.PlaneDespawned.Subscribe(remotePeerId => Synchronizer.SetVisibilityFor(remotePeerId, false), NodeAliveCt);

        LoadSettings();

        // Godot.Input.SetMouseMode(Godot.Input.MouseModeEnum.Captured);
        Health = MaxHealth;
        RegenerationTimer.WaitTime = RegenerationRequirementTime;
        RegenerationTimer.OneShot = true;

        CameraObject.MakeCurrent();

        InitializePlaneBody();
    }

    public override void _ExitTree()
    {
        if (IsMultiplayerAuthority()) return;
        room.DespawnedPlane(GetMultiplayerAuthority());
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        UpdateCamera(delta);
        if (Menu.Visible)
        {
            // Godot.Input.SetMouseMode(Godot.Input.MouseModeEnum.Visible);
            return;
        }
        else
        {
            // Godot.Input.SetMouseMode(Godot.Input.MouseModeEnum.Captured);
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
            // Spawn();
            RegenerationTimer.Stop();
        } else {
            RegenerationTimer.Start();
        }
    }

    private void RegenerateHealth() {

        Tween MyTween = GetTree().CreateTween();
        MyTween.TweenProperty(this, "Health", MaxHealth, RegenerationDuration);
    }

    // private void Spawn()
    // {
    //     Health = MaxHealth;
    //
    //     PlaneBodyController OldPlane = PlaneBody;
    //     Node NewPlane = GD.Load<PackedScene>("Scenes/Plane/PlaneBody.tscn").Instantiate();
    //     AddChild(NewPlane);
    //     PlaneBody = (PlaneBodyController)NewPlane;
    //     CameraTarget = PlaneBody.GetNode<Node3D>("CamInterpolateTo");
    //     InitializePlaneBody();
    //     OldPlane.QueueFree();
    // }

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
