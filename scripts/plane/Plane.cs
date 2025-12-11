using Godot;

public partial class Plane : RigidBody3D
{
    // --- OBJECTS ---
    [Export] public Node3D CameraObject;
    [Export] public Node3D CameraTarget;

    // --- SPEED ---
    [Export] public float MaxSpeed = 250f;
    [Export] public float Acceleration = 80f;

    // --- ROTATION ---
    [Export] public float PitchRate = 4.0f;
    [Export] public float YawRate   = 2.0f;         
    [Export] public float RollRate  = 4.0f;

    [Export] public float MaxPitch = 4f;
    [Export] public float MaxYaw = 4f;
    [Export] public float MaxRoll = 4f;

    // --- SMOOTHING ---
    [Export] public float RotationSmooth = 6f;
    [Export] public float SpeedSmooth = 5f;

    private float targetSpeed = 0f;
    private Camera3D cam;

    public override void _Ready()
    {
        LinearDamp = 2f;
        AngularDamp = 4f;
        Input.MouseMode = Input.MouseModeEnum.Captured;

        cam = (Camera3D)CameraObject.GetNodeOrNull("Camera3D");
        if (cam == null && CameraObject is Camera3D) {
            cam = (Camera3D)CameraObject;
        }
    }

    public override void _Process(double delta)
    {
        UpdateCamera(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        // --- Inputs ---
        Vector2 mouse = Input.GetLastMouseVelocity() / DisplayServer.ScreenGetSize();
        float pitch = -mouse.Y;
        float yaw   = -mouse.X;
        float roll  = Input.GetActionStrength("D") - Input.GetActionStrength("A");
        float throttle = Input.GetActionStrength("W") - Input.GetActionStrength("S");
        throttle = Mathf.Clamp(throttle, -1f, 1f);

        targetSpeed += Acceleration * throttle * (float)delta;
        targetSpeed = Mathf.Clamp(targetSpeed, 0f, MaxSpeed);

        float currentSpeed = LinearVelocity.Length();
        float newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, SpeedSmooth * (float)delta);

        Basis basis = GlobalTransform.Basis;
        Vector3 forward = basis.Z;
        Vector3 right   = basis.X;
        Vector3 up      = basis.Y;

        // targeted angular velocity
        Vector3 targetAngularVelocity =
            right   * Mathf.Clamp(-pitch * PitchRate, -MaxPitch, MaxPitch) +
            up      * Mathf.Clamp(yaw   * YawRate, -MaxYaw, MaxYaw) +
            forward * Mathf.Clamp(roll  * RollRate, -MaxRoll, MaxRoll);

        // smooth 
        AngularVelocity = AngularVelocity.Lerp(targetAngularVelocity, RotationSmooth * (float)delta);

        LinearVelocity = forward * newSpeed + new Vector3 (0f, -1f, 0f) * 9.81f * Mass;
    }

    public void UpdateCamera(double delta) {
	// Position smoothing — delta based
	var t = 1.0f - Mathf.Pow(0.001f, (float)delta);  // feels smooth but responsive

        Transform3D from = CameraObject.GlobalTransform;
        Transform3D to   = CameraTarget.GlobalTransform;

        CameraObject.GlobalTransform = from.InterpolateWith(to, t);
    }
}