# Native ART-18 review sheet. Run with Godot --path game --script <absolute-path>
# and -- --screenshot <absolute.png>, or -- --verify-only for raster checks.
# This preview never changes scenario data or binds portraits to live characters.
extends SceneTree

const PEOPLE = [
    ["brooks", "Lena Brooks"], ["silva", "Marco Silva"], ["haddad", "Nadia Haddad"],
    ["kowalski", "Pete Kowalski"], ["tan", "Grace Tan"], ["moreau", "Andre Moreau"],
    ["sato", "Hana Sato"], ["pryce", "Owen Pryce"], ["carvalho", "Ines Carvalho"],
]
var asset_root: String
var textures: Array[ImageTexture] = []

func _initialize() -> void:
    call_deferred("review")

func review() -> void:
    asset_root = ProjectSettings.globalize_path("res://").path_join("../assets").simplify_path()
    var font = FontFile.new()
    font.data = FileAccess.get_file_as_bytes(asset_root.path_join("art-studies/fonts/Barlow-Regular.ttf"))
    if not font.get_font_name().contains("Barlow"):
        fail("Bundled Tabletop font did not load")
        return
    var canvas = Control.new()
    root.add_child(canvas)
    var paper = ColorRect.new()
    paper.color = Color("e9e3d6")
    paper.size = Vector2(1280, 800)
    canvas.add_child(paper)
    label(canvas, font, "Tabletop / nine more people to meet", Vector2(24, 16), 28)
    label(canvas, font, "Original fictional designs · same cast page before and after · desk-size inset at 36 px", Vector2(24, 57), 19)
    var seen: Dictionary = {}
    var named = 0
    for i in range(PEOPLE.size()):
        var key: String = PEOPLE[i][0]
        var path = asset_root.path_join("portraits/" + key + ".svg")
        var available = FileAccess.file_exists(path)
        if not available:
            path = asset_root.path_join("portraits/fallback.svg")
        var raster = Image.new()
        if raster.load_svg_from_string(FileAccess.get_file_as_string(path)) != OK:
            fail("Cannot render " + key)
            return
        if available:
            named += 1
            var fingerprint = raster.get_data().hex_encode().sha256_text()
            if seen.has(fingerprint):
                fail("Duplicate face: " + key)
                return
            seen[fingerprint] = true
            for side in range(2):
                var whites = 0
                var pupils = 0
                for y in range(110, 166):
                    for x in range(60 if side == 0 else 128, 128 if side == 0 else 200):
                        var pixel = raster.get_pixel(x, y)
                        if pixel.r > 0.82 and pixel.g > 0.82 and pixel.b > 0.72:
                            whites += 1
                        if pixel.r < 0.32 and pixel.g < 0.35 and pixel.b < 0.32:
                            pupils += 1
                if whites <= 32 or pupils <= 20:
                    fail("Eye opening or pupil missing: " + key)
                    return
        var texture = ImageTexture.create_from_image(raster)
        textures.append(texture)
        var cell = Panel.new()
        var style = StyleBoxFlat.new()
        style.bg_color = Color("faf5ea")
        style.border_color = Color("b8b19e")
        style.set_border_width_all(1)
        style.set_corner_radius_all(8)
        cell.add_theme_stylebox_override("panel", style)
        cell.position = Vector2(24 + i % 3 * 416, 108 + i / 3 * 222)
        cell.size = Vector2(400, 208)
        canvas.add_child(cell)
        portrait(cell, texture, Vector2(18, 19), Vector2(112, 140))
        label(cell, font, PEOPLE[i][1], Vector2(148, 25), 23)
        label(cell, font, "Desk size", Vector2(148, 67), 18)
        portrait(cell, texture, Vector2(153, 99), Vector2(36, 45))
    var args = OS.get_cmdline_user_args()
    if "--require-all" in args and named != PEOPLE.size():
        fail("Expected nine new faces, got " + str(named))
        return
    if not "--verify-only" in args:
        var at = args.find("--screenshot")
        if at < 0 or at + 1 >= args.size():
            fail("Pass --screenshot <absolute.png> or --verify-only")
            return
        await process_frame
        await RenderingServer.frame_post_draw
        var shot = root.get_texture().get_image()
        if shot.get_width() != 1280 or shot.get_height() != 800 or shot.save_png(args[at + 1]) != OK:
            fail("Expected a saved 1280x800 native PNG")
            return
    print("PORTRAIT_GALLERY_PASS: ", named, " named faces, ", PEOPLE.size() - named, " silhouettes; distinct renders and two open eyes per named face.")
    canvas.queue_free()
    await process_frame
    await process_frame
    quit()

func portrait(parent: Control, texture: ImageTexture, at: Vector2, dimensions: Vector2) -> void:
    var rect = TextureRect.new()
    rect.texture = texture
    rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
    rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
    rect.position = at
    rect.size = dimensions
    parent.add_child(rect)

func label(parent: Control, font: FontFile, words: String, at: Vector2, font_size: int) -> void:
    var text = Label.new()
    text.text = words
    text.position = at
    text.add_theme_font_override("font", font)
    text.add_theme_font_size_override("font_size", font_size)
    text.add_theme_color_override("font_color", Color("343d4b"))
    parent.add_child(text)

func fail(message: String) -> void:
    push_error("PORTRAIT_GALLERY_FAIL: " + message)
    quit(1)
