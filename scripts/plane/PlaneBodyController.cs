using Godot;

public partial class PlaneBodyController : RigidBody3D
{
	// --- SPEED ---
	 public float MaxSpeed = 250f;
	 public float Acceleration = 80f;

	// --- ROTATION ---
	 public float PitchRate = 4.0f;
	 public float YawRate   = 2.0f;         
	 public float RollRate  = 4.0f;

	 public float MaxPitch = 4f;
	 public float MaxYaw = 4f;
	 public float MaxRoll = 4f;

	 public bool MouseYaw = false;

	 public float YawSensitivity;
	 public float RollSensitivity;

	// --- SMOOTHING ---
	 public float RotationSmooth = 6f;
	 public float SpeedSmooth = 5f;

	private float targetSpeed = 0f;

	public override void _Ready()
	{
		LinearDamp = 2f;
		AngularDamp = 4f;
	}

	public override void _PhysicsProcess(double delta)
	{
		// --- Inputs ---
		Vector2 mouse = Input.GetLastMouseVelocity() / DisplayServer.ScreenGetSize();

		float pitch = -mouse.Y * YawSensitivity;

		float yaw = 0f;
		float roll = 0f;
		if (!MouseYaw) {
		roll   = mouse.X * RollSensitivity;
		yaw  = Input.GetActionStrength("A") - Input.GetActionStrength("D");
		} else {
		yaw   = -mouse.X;
		roll  = Input.GetActionStrength("D") - Input.GetActionStrength("A");
		}

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
}
