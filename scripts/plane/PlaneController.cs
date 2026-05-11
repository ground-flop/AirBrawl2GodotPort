using AirBrawl2.Networking;
using AirBrawl2.scripts;
using Godot;

[SceneTree("../../Scenes/Plane/plane.tscn", true)]
public partial class PlaneController : Node3D
{
    private Camera3D CameraObject => _.PlaneCamera;
    private Node3D CameraTarget => _.PlaneBody.CamInterpolateTo;
    private PanelContainer Menu => _.PlaneBody.Control.menu;
    private PlaneBodyController PlaneBody => _.PlaneBody;
    private Godot.Timer RegenerationTimer => _.RegenerationTimer;
    private MultiplayerSynchronizer Synchronizer => _.MultiplayerSynchronizer;

    [Export] private bool singlePlayer;
    [Export] private bool mouseYaw;
    [Export] private float yawSensitivity = 3.0f;
    [Export] private float rollSensitivity = 3.0f;
    [Export] private float maxSpeed = 250.0f;
    [Export] private Vector3 startPosition = new(100, 100, 0);
    [Export] private float minFov = 60.0f;
    [Export] private float maxFov = 120.0f;
    [Export] private float regenerationRequirementTime = 1f;
    [Export] private float regenerationDuration = 1.5f;

    private float health = 100f;
    [Export] private float maxHealth = 100f;

    private Room room = null!;
    private readonly CancellationTokenSource nodeAliveCts = new();
    private CancellationToken NodeAliveCt => nodeAliveCts.Token;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(int.Parse(Name));
        GlobalPosition = startPosition;

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

        health = maxHealth;
        RegenerationTimer.WaitTime = regenerationRequirementTime;
        RegenerationTimer.OneShot = true;

        CameraObject.MakeCurrent();

        InitializePlaneBody();
    }

    public override void _ExitTree()
    {
        if (IsMultiplayerAuthority()) return;
        if (!room.Players.ContainsKey(GetMultiplayerAuthority())) return;
        room.DespawnedPlane(GetMultiplayerAuthority());
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        // Update camera position
        var t = 1.0f - Mathf.Pow(0.001f, (float)delta);
        var from = CameraObject.GlobalTransform;
        var to = CameraTarget.GlobalTransform;
        CameraObject.GlobalTransform = from.InterpolateWith(to, t);

        // Update FOV
        CameraObject.Fov = float.Lerp(minFov, maxFov, PlaneBody.LinearVelocity.Length() / PlaneBody.MaxSpeed);
    }

    private void LoadSettings()
    {
        var cfg = new ConfigFile();
        if (cfg.Load("user://settings.cfg") != Error.Ok) return;

        mouseYaw = (bool)cfg.GetValue("controls", "MouseYaw", true);
        yawSensitivity = (float)cfg.GetValue("controls", "YawSensitivity", 3.0f);
        rollSensitivity = (float)cfg.GetValue("controls", "RollSensitivity", 3.0f);
    }

    private void SetMouseYaw(bool newMouseYaw)
    {
        mouseYaw = newMouseYaw;
        PlaneBody.MouseYaw = newMouseYaw;
        SaveSettings();
    }

    private void SetYawSensitivity(float newYawSensitivity)
    {
        yawSensitivity = newYawSensitivity;
        PlaneBody.YawSensitivity = newYawSensitivity;
        SaveSettings();
    }

    private void SetRollSensitivity(float newRollSensitivity)
    {
        rollSensitivity = newRollSensitivity;
        PlaneBody.RollSensitivity = newRollSensitivity;
        SaveSettings();
    }

    private void SaveSettings()
    {
        var cfg = new ConfigFile();

        cfg.SetValue("controls", "MouseYaw", mouseYaw);
        cfg.SetValue("controls", "YawSensitivity", yawSensitivity);
        cfg.SetValue("controls", "RollSensitivity", rollSensitivity);

        cfg.Save("user://settings.cfg");
    }

    private void ChangeHealth(float changeHealth)
    {
        health += changeHealth;
        if (health < 1f)
            RegenerationTimer.Stop();
        else
            RegenerationTimer.Start();
    }

    private void RegenerateHealth() =>
        CreateTween().TweenProperty(this, nameof(health), maxHealth, regenerationDuration);

    private void OnImpact(float impactVelocity) =>
        ChangeHealth(-PlaneBody.LinearVelocity.Length() * 2);

    private void InitializePlaneBody()
    {
        PlaneBody.MaxSpeed = maxSpeed;
        PlaneBody.YawSensitivity = yawSensitivity;
        PlaneBody.RollSensitivity = rollSensitivity;
        PlaneBody.MouseYaw = mouseYaw;

        PlaneBody.OnImpactEvent += OnImpact;
    }
}
