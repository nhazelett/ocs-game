shader_type canvas_item;
render_mode unshaded;

uniform vec4 paper_color : source_color;

void fragment() {
    vec4 harbor = texture(TEXTURE, UV);
    vec2 edge = smoothstep(vec2(0.0), vec2(0.10), UV)
        * (vec2(1.0) - smoothstep(vec2(0.90), vec2(1.0), UV));
    COLOR = vec4(mix(paper_color.rgb, harbor.rgb, edge.x * edge.y), 1.0);
}
