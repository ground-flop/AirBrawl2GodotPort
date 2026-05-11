using Godot;

public partial class PlaneBodyController : RigidBody3D
{
    // --- SPEED ---
    public float MaxSpeed = 250f;
    private float acceleration = 80f;

    // --- ROTATION ---
    private float pitchRate = 4.0f;
    private float yawRate = 2.0f;
    private float rollRate = 4.0f;

    private float maxPitch = 4f;
    private float maxYaw = 4f;
    private float maxRoll = 4f;

    public bool MouseYaw = true;

    public float YawSensitivity;
    public float RollSensitivity;

    // --- SMOOTHING ---
    private float rotationSmooth = 6f;
    private float speedSmooth = 5f;

    // --- ACCUMULATORS ---
    private float targetSpeed;

    public delegate void ImpactEventHandler(float impactVelocity);
    public event ImpactEventHandler? OnImpactEvent;

    public override void _Ready()
    {
        GravityScale = Input.MouseMode == Input.MouseModeEnum.Captured ? 1 : 0;
        LinearDamp = 2f;
        AngularDamp = 4f;
        SetContactMonitor(true);
        MaxContactsReported = 5;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (!IsMultiplayerAuthority()) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        // --- Inputs ---
        var mouse = Input.GetLastMouseVelocity() / DisplayServer.ScreenGetSize();

        var pitch = -mouse.Y * YawSensitivity;
        var roll = MouseYaw ? Input.GetAxis("A", "D") : mouse.X * RollSensitivity;
        var yaw = MouseYaw ? -mouse.X : Input.GetAxis("D", "A");

        var throttle = Input.GetAxis("S", "W");

        // Compute new speed
        targetSpeed += acceleration * throttle * state.Step;
        targetSpeed = Mathf.Clamp(targetSpeed, 0f, MaxSpeed);

        var currentSpeed = LinearVelocity.Length();
        var newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedSmooth * state.Step);

        // Compute new angular velocity
        var basis = GlobalTransform.Basis;
        var forward = basis.Z;
        var right = basis.X;
        var up = basis.Y;

        var targetAngularVelocity =
            right * Mathf.Clamp(-pitch * pitchRate, -maxPitch, maxPitch) +
            up * Mathf.Clamp(yaw * yawRate, -maxYaw, maxYaw) +
            forward * Mathf.Clamp(roll * rollRate, -maxRoll, maxRoll);

        // Apply
        AngularVelocity = AngularVelocity.Lerp(targetAngularVelocity, rotationSmooth * state.Step);
        LinearVelocity = forward * newSpeed;

        for (var i = 0; i < state.GetContactCount(); i++)
        {
            var normal = state.GetContactLocalNormal(i);
            var impactSpeed = -state.LinearVelocity.Dot(normal);
            if (impactSpeed <= 0) return;

            OnImpactEvent?.Invoke(impactSpeed);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsMultiplayerAuthority()) return;
        if (@event is InputEventKey { Pressed: true, KeyLabel: Key.Escape })
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;

            GravityScale = Input.MouseMode == Input.MouseModeEnum.Captured ? 1 : 0;
        }
    }
}
