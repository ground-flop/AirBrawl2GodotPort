extends Node3D

@export var singleplayer := false

@onready var camera_object = $Camera3D
@onready var camera_target = $planeBody/CamInterpolateTo
@onready var menu = $planeBody/Control/menu
@onready var planeBody = $planeBody

@export var start_pos = Vector3(100, 100, 0)
@export var max_speed : float = 100
@export var sensitivity := 10.0
@export var mouse_yaw := true
@export var inverted := true

var original_max_speed := 0.0
var original_acceleration := 0.0

var x_rot := 0.0
var y_rot := 0.0
var z_rot := 0.0

var cam: Camera3D


# --------------------------- START --------------------------

func _ready():
	global_position = start_pos

	planeBody.max_speed = max_speed
	planeBody.sensitivity = sensitivity
	planeBody.mouse_yaw = mouse_yaw
	planeBody.inverted = inverted

	load_settings()
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

	cam = camera_object.get_node_or_null("Camera3D")
	if cam == null and camera_object is Camera3D:
		cam = camera_object

# --------------------------- UPDATE --------------------------

func _process(delta):

	if menu.visible:
		Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
		return
	else:
		Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
		pass
		
	update_camera(delta)
	update_fov()

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
		cam.fov = lerp(60.0, 120.0, planeBody.speed / planeBody.max_speed)

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

func set_mouse_yaw(new_yaw: bool):
	mouse_yaw = new_yaw
	planeBody.mouse_yaw = mouse_yaw
	save_settings()

func set_inverted(new_inverted: bool):
	inverted = new_inverted
	planeBody.inverted = inverted
	save_settings()

func set_sensitivity(new_sensitivity: float):
	sensitivity = new_sensitivity
	planeBody.sensitivity = sensitivity
	save_settings()
