# Native ART-20 matched cast sheets; three pages, 80 / 48 / 36 pixel views.
# --script res://tests/CastReview.gd -- --legacy --screenshots /absolute/prefix
extends SceneTree
const KEYS = ["ruiz", "cole", "whitfield", "mills", "hale", "bell", "park", "nair", "delgado", "mensah", "okafor", "brandt", "lin", "ortiz", "dunn", "ward", "reyna", "brooks", "silva", "haddad", "kowalski", "tan", "moreau", "sato", "pryce", "carvalho", "fallback"]
var font: FontFile
var textures: Array[Texture2D] = []
func _initialize() -> void:
    call_deferred("review")
func review() -> void:
    var args = OS.get_cmdline_user_args()
    var legacy = "--legacy" in args
    var at = args.find("--screenshots")
    var asset_root = ProjectSettings.globalize_path("res://").path_join("../assets").simplify_path()
    font = FontFile.new()
    font.data = FileAccess.get_file_as_bytes(asset_root.path_join("art-studies/fonts/Barlow-Regular.ttf"))
    for page in range(3):
        var canvas = Control.new()
        root.add_child(canvas)
        var paper = ColorRect.new()
        paper.color = Color("e9e3d6"); paper.size = Vector2(1280, 800); canvas.add_child(paper)
        label(canvas, "The Tabletop cast / " + str(page + 1) + " of 3", Vector2(24,16), 30)
        label(canvas, "Original fictional people · card at 80 px · assignment at 48 px · strip at 36 px", Vector2(24,60), 20)
        for cell in range(9):
            var key: String = KEYS[page * 9 + cell]
            var texture: Texture2D
            if legacy:
                var image = Image.new()
                if image.load_svg_from_string(FileAccess.get_file_as_string(asset_root.path_join("portraits/" + key + ".svg"))) != OK:
                    fail("Cannot render legacy " + key); return
                texture = ImageTexture.create_from_image(image)
            else:
                texture = load("res://art/cast/" + key + ".png")
                if texture == null:
                    fail("Cannot load rendered " + key); return
            textures.append(texture)
            var card = Panel.new()
            var style = StyleBoxFlat.new()
            style.bg_color=Color("faf5ea"); style.border_color=Color("c0b8a4")
            style.set_border_width_all(1); style.set_corner_radius_all(9)
            card.add_theme_stylebox_override("panel",style)
            card.position=Vector2(24+cell%3*416,110+cell/3*222);card.size=Vector2(400,208);canvas.add_child(card)
            portrait(card,texture,Vector2(16,24),Vector2(80,100))
            label(card,"Unassigned" if key=="fallback" else key.capitalize(),Vector2(125,19),24)
            label(card,"Card     Assignment     Strip",Vector2(16,160),18)
            portrait(card,texture,Vector2(136,76),Vector2(48,60))
            portrait(card,texture,Vector2(258,87),Vector2(36,45))
        await process_frame
        await RenderingServer.frame_post_draw
        if at>=0:
            var shot=root.get_texture().get_image()
            if shot.get_width()!=1280 or shot.get_height()!=800 or shot.save_png(args[at+1]+"-cast-"+str(page+1)+".png")!=OK:
                fail("Expected 1280x800 PNG");return
        canvas.queue_free();await process_frame;await process_frame
    print("CAST_REVIEW_PASS: 26 people and neutral fallback, three matched pages, actual card/assignment/strip sizes.")
    quit()
func portrait(parent: Control, texture: Texture2D, at: Vector2, dimensions: Vector2) -> void:
    var rect=TextureRect.new();rect.texture=texture;rect.expand_mode=TextureRect.EXPAND_IGNORE_SIZE
    rect.stretch_mode=TextureRect.STRETCH_KEEP_ASPECT_CENTERED;rect.texture_filter=CanvasItem.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS
    rect.position=at;rect.size=dimensions;parent.add_child(rect)
func label(parent: Control, words: String, at: Vector2, point_size: int) -> void:
    var text=Label.new();text.text=words;text.position=at
    text.add_theme_font_override("font",font);text.add_theme_font_size_override("font_size",point_size)
    text.add_theme_color_override("font_color",Color("343d4b"));parent.add_child(text)
func fail(words: String) -> void:
    push_error("CAST_REVIEW_FAIL: "+words);quit(1)
