extends RigidBody3D

@export var singleplayer := false

@onready var menu = $Control/menu
@onready var hyperdrive_indicator = $Control/TextEdit
@onready var speed_text = $Control/Label
@onready var canvas = $Control

var max_speed : float
var speed : float = 10.0
var acceleration : float = 5.0
var sensitivity : float = 10.0
var mouse_yaw : float= true
var inverted : float = false

var original_max_speed := 0.0
var original_acceleration := 0.0

var x_rot := 0.0
var y_rot := 0.0
var z_rot := 0.0


# --------------------------- START --------------------------

func _ready():
	linear_damp = 10.0
	angular_damp = 30.0 

	hyperdrive_indicator.visible = false

	original_max_speed = max_speed
	original_acceleration = acceleration

# --------------------------- UPDATE --------------------------

func _process(delta):

	if Input.is_key_pressed(KEY_W):
		speed += 10 * acceleration * delta
	if Input.is_key_pressed(KEY_S):
		speed -= 10 * acceleration * delta

	if Input.is_action_just_pressed("hyperdrive"):
		activate_hyperdrive()
		
	speed = clamp(speed, 0, max_speed)
	
	speed_text.text = "speed: " + str(speed)


# --------------------------- ROTATION --------------------------

func plane_physics_control(state):
	var mouse_delta = Input.get_last_mouse_velocity() * sensitivity * state.step

	var mouse_x = -mouse_delta.x
	var mouse_y = mouse_delta.y

	var pitch: float
	var yaw: float
	var roll: float

	if mouse_yaw:
		pitch = ad_rotation() * 10 * state.step
		yaw = mouse_y * 2
		roll = mouse_x * 0.5
	else:
		pitch = -mouse_x
		yaw = mouse_y
		roll = -(ad_rotation() * 10 * state.step)

	if inverted:
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
	original_max_speed = max_speed
	original_acceleration = acceleration
	max_speed = 5000
	acceleration = 500

	await get_tree().create_timer(5).timeout
	reset_max_speed()

func reset_max_speed():
	max_speed = original_max_speed
	if speed > max_speed:
		speed = max_speed
		
	acceleration = original_acceleration
	hyperdrive_indicator.visible = false
