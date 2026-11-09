extends SceneTree
## Standalone Godot viewer: the normal game and its editor screens are unchanged.

var mesh_count := 0

func _initialize() -> void:
	call_deferred("build_scene")

func vec(values: Array) -> Vector3:
	return Vector3(values[0], values[1], values[2])

func fail(message: String) -> void:
	push_error("DIORAMA_FAIL: " + message)
	quit(1)

func inspect(node: Node) -> bool:
	if node is MeshInstance3D:
		mesh_count += 1
		for surface in node.mesh.get_surface_count():
			var arrays: Array = node.mesh.surface_get_arrays(surface)
			if arrays[Mesh.ARRAY_COLOR] == null or arrays[Mesh.ARRAY_COLOR].is_empty():
				return false
	for child in node.get_children():
		if not inspect(child):
			return false
	return true

func build_scene() -> void:
	var repo: String = ProjectSettings.globalize_path(get_script().resource_path).get_base_dir().get_base_dir().get_base_dir()
	var folder: String = repo.path_join("assets/diorama")
	var layout = JSON.parse_string(FileAccess.get_file_as_string(folder.path_join("spoke-b.layout.json")))
	if not layout is Dictionary:
		fail("Missing or invalid layout")
		return
	var world := Node3D.new()
	root.add_child(world)
	GLTFDocument.register_gltf_document_extension(GLTFDocumentExtensionConvertImporterMesh.new(), true)
	var document := GLTFDocument.new()
	var state := GLTFState.new()
	if document.append_from_file(folder.path_join("spoke-b.glb"), state) != OK:
		fail("Cannot load diorama GLB; check Git LFS checkout")
		return
	var scene := document.generate_scene(state)
	if scene == null or not inspect(scene) or mesh_count == 0:
		fail("Every mesh must retain vertex colors")
		return
	world.add_child(scene)
	for instance in layout.instances:
		if scene.find_child(instance.id, true, false) == null:
			fail("Missing instance " + instance.id)
			return
	var light := DirectionalLight3D.new()
	light.rotation_degrees = vec(layout.light.rotation)
	light.light_energy = layout.light.energy
	light.shadow_enabled = true
	light.shadow_bias = 0.08
	light.shadow_normal_bias = 1.2
	light.directional_shadow_max_distance = 70
	world.add_child(light)
	var environment := Environment.new()
	environment.background_mode = Environment.BG_COLOR
	environment.background_color = Color("202c30")
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.ambient_light_color = Color("cfdbd9")
	environment.ambient_light_energy = 0.55
	var setting := WorldEnvironment.new()
	setting.environment = environment
	world.add_child(setting)
	var camera := Camera3D.new()
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.size = layout.camera.size
	camera.current = true
	world.add_child(camera)
	camera.look_at_from_position(vec(layout.camera.position), vec(layout.camera.target))
	root.size = Vector2i(1440, 900)
	root.content_scale_size = Vector2i(1440, 900)
	root.msaa_3d = Viewport.MSAA_4X
	var overlay := CanvasLayer.new()
	root.add_child(overlay)
	var title := Label.new()
	title.text = "OCS: LINES OF SUPPORT   /   " + layout.title
	title.position = Vector2(42, 30)
	title.add_theme_font_size_override("font_size", 26)
	overlay.add_child(title)
	var subtitle := Label.new()
	subtitle.text = layout.description + "\nM3 kit study  ·  vertex colors  ·  one key light"
	subtitle.position = Vector2(44, 69)
	subtitle.add_theme_color_override("font_color", Color("b7c7bf"))
	overlay.add_child(subtitle)
	var footer := Label.new()
	footer.text = "LIFE SUPPORT   tents / CHUs / water         UTILITIES   power / fuel         ACCESS   stores / vehicles\nPresentation study; no simulated quantities or capability status."
	footer.position = Vector2(44, 826)
	footer.add_theme_font_size_override("font_size", 16)
	footer.add_theme_color_override("font_color", Color("b7c7bf"))
	overlay.add_child(footer)
	var args := OS.get_cmdline_user_args()
	var shot := args.find("--screenshot")
	if shot >= 0:
		if shot + 1 >= args.size() or DisplayServer.get_name() == "headless":
			fail("Screenshot requires a display and output path")
			return
		for frame in 8:
			await process_frame
		await RenderingServer.frame_post_draw
		if root.get_texture().get_image().save_png(args[shot + 1]) != OK:
			fail("Could not save screenshot")
			return
	print("DIORAMA_PASS: %d instances, %d colored meshes, orthographic camera, one directional light" % [layout.instances.size(), mesh_count])
	if shot >= 0 or args.has("--smoke-test") or DisplayServer.get_name() == "headless":
		quit()
