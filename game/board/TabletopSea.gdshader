shader_type spatial;
render_mode unshaded, cull_disabled;

uniform vec4 sea_color : source_color = vec4(0.38, 0.56, 0.60, 1.0);
uniform vec4 sky_color : source_color = vec4(0.73, 0.82, 0.83, 1.0);
uniform vec4 haze_color : source_color = vec4(0.84, 0.88, 0.86, 1.0);
varying vec2 map_position;

void vertex() {
    map_position = VERTEX.xz;
    VERTEX.y += 0.008 * sin(VERTEX.x * 0.23 + TIME * 0.18);
}

void fragment() {
    float swell = sin(map_position.y * 2.4 + 0.4 * sin(map_position.x * 0.4 - TIME * 0.1) - TIME * 0.27);
    float cross_swell = sin(map_position.y * 5.7 - map_position.x * 0.25 + TIME * 0.21);
    float crest = smoothstep(0.94, 1.0, swell);
    vec3 sea = sea_color.rgb + swell * 0.006 + cross_swell * 0.003 + crest * 0.01;
    // An illustrated distant horizon above the orthographic operating area.
    float horizon = smoothstep(0.09, 0.17, SCREEN_UV.y);
    vec3 sky = mix(sky_color.rgb, haze_color.rgb, smoothstep(0.0, 0.12, SCREEN_UV.y));
    ALBEDO = mix(sky, sea, horizon);
    ROUGHNESS = 1.0;
}
