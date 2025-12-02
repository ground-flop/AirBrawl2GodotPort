extends RigidBody3D

@export var singleplayer := false

@export var camera_object: Node3D
@export var camera_target: Node3D
@export var menu: Control
@export var hyperdrive_indicator: Node
@export var canvas: Control

@export var start_pos: Vector3
@export var max_speed := 100.0
@export var speed := 10.0
@export var acceleration := 5.0
@export var sensitivity := 10.0

@export var speed_text: Label

var original_max_speed := 0.0
var original_acceleration := 0.0

var x_rot := 0.0
var y_rot := 0.0
var z_rot := 0.0

var mouse_yaw := 1
var inverted := 0

var cam: Camera3D

# --------------------------- START --------------------------

func _ready():
	linear_damp = 10.0
	angular_damp = 30.0 
	global_position = start_pos

	hyperdrive_indicator.visible = false

	original_max_speed = max_speed
	original_acceleration = acceleration

	load_settings()
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

	cam = camera_object.get_node_or_null("Camera3D")
	if cam == null and camera_object is Camera3D:
		cam = camera_object

# --------------------------- UPDATE --------------------------

func _process(delta):
	speed_text.text = "speed: " + str(speed)

	if menu.visible:
		Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
		return
	else:
		Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
		pass

	# Speed control only
	if speed > max_speed:
		speed = max_speed
	if speed < 0:
		speed = 0

	if Input.is_key_pressed(KEY_W):
		speed += 10 * acceleration * delta
	if Input.is_key_pressed(KEY_S):
		speed -= 10 * acceleration * delta

	if Input.is_action_just_pressed("hyperdrive"):
		activate_hyperdrive()

	update_camera(delta)
	update_fov()


# --------------------------- ROTATION --------------------------

func plane_physics_control(state):
	var mouse_delta = Input.get_last_mouse_velocity() * sensitivity * state.step

	var mouse_x = -mouse_delta.x
	var mouse_y = mouse_delta.y

	var pitch: float
	var yaw: float
	var roll: float

	if mouse_yaw == 1:
		pitch = ad_rotation() * 10 * state.step
		yaw = mouse_y * 2
		roll = mouse_x * 0.5
	else:
		pitch = -mouse_x
		yaw = mouse_y
		roll = -(ad_rotation() * 10 * state.step)

	if inverted == 1:
		yaw = -yaw

	# Apply torque (rotation) instead of rotate_x/y/z
	var torque_mult := 2.0

	# local axes
	var axis_pitch = global_transform.basis.x   # rotate around local X
	var axis_yaw   = global_transform.basis.y   # rotate around local Y
	var axis_roll  = global_transform.basis.z   # rotate around local Z

	apply_torque(axis_pitch * -yaw * torque_mult)
	apply_torque(axis_yaw   * -pitch * torque_mult)
	apply_torque(axis_roll  * -roll * torque_mult)

	# Forward engine thrust
	var forward_dir = global_transform.basis.z
	apply_central_force(forward_dir * speed * 10)


func _integrate_forces(state: PhysicsDirectBodyState3D) -> void:
	plane_physics_control(state)


func ad_rotation() -> float:
	if Input.is_key_pressed(KEY_A): return -300.0
	if Input.is_key_pressed(KEY_D): return 300.0
	return 0.0

# --------------------------- HYPERDRIVE --------------------------

func activate_hyperdrive():
	hyperdrive_indicator.visible = true
	max_speed = 5000
	acceleration = 500

	await get_tree().create_timer(5).timeout
	reset_max_speed()

func reset_max_speed():
	max_speed = original_max_speed
	speed = max_speed
	acceleration = original_acceleration
	hyperdrive_indicator.visible = false

# --------------------------- CAMERA --------------------------

func update_camera(delta):
	# Position smoothing — delta based
	var t = 1.0 - pow(0.001, delta)  # feels smooth but responsive

	camera_object.global_transform.origin = camera_object.global_transform.origin.lerp(
		camera_target.global_transform.origin,
		t
	)

	camera_object.global_transform.basis = camera_target.global_transform.basis


func update_fov():
	if cam:
		cam.fov = lerp(60.0, 120.0, speed / max_speed)

# --------------------------- SETTINGS --------------------------

func load_settings():
	var cfg := ConfigFile.new()
	if cfg.load("user://settings.cfg") == OK:
		inverted = cfg.get_value("controls", "inverted", 0)
		mouse_yaw = cfg.get_value("controls", "mouse_yaw", 0)
		sensitivity = cfg.get_value("controls", "sensitivity", sensitivity)

func save_settings():
	var cfg := ConfigFile.new()
	cfg.set_value("controls", "inverted", inverted)
	cfg.set_value("controls", "mouse_yaw", mouse_yaw)
	cfg.set_value("controls", "sensitivity", sensitivity)
	cfg.save("user://settings.cfg")

func toggle_mouse_yaw(new_value: bool):
	mouse_yaw = 1 if new_value else 0
	save_settings()

func toggle_inverted(new_value: bool):
	inverted = 1 if new_value else 0
	save_settings()

func change_sensitivity(new_value: float):
	sensitivity = new_value
	save_settings()
