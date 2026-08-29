shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear_mipmap;

uniform float highlight_threshold : hint_range(0.0, 1.0) = 0.62;
uniform float highlight_softness : hint_range(0.001, 0.5) = 0.10;
uniform float motion_strength : hint_range(0.0, 1.0) = 1.0;
uniform float motion_variation : hint_range(0.0, 1.0) = 0.25;
uniform float static_contrast : hint_range(0.1, 4.0) = 1.5;
uniform float static_brightness : hint_range(0.0, 1.0) = 0.5;
uniform float scroll_speed = 60.0;

uvec2 pcg2d(uvec2 v)
{
	v = v * 1664525u + 1013904223u;
	v.x += v.y * 1664525u;
	v.y += v.x * 1664525u;
	v ^= v >> 16u;
	v.x += v.y * 1664525u;
	v.y += v.x * 1664525u;
	v ^= v >> 16u;
	return v;
}

float unit_float(uint h)
{
	return float(h) * (1.0 / 4294967295.0);
}

float luminance(vec3 c)
{
	return dot(c, vec3(0.2126, 0.7152, 0.0722));
}

void fragment()
{
	ivec2 px = ivec2(int(FRAGCOORD.x), int(FRAGCOORD.y));

	vec3 scene_sharp = textureLod(screen_texture, SCREEN_UV, 0.0).rgb;
	vec3 scene_soft = textureLod(screen_texture, SCREEN_UV, 3.0).rgb;

	float bright_sharp = luminance(scene_sharp);
	float bright_soft = luminance(scene_soft);

	float highlight = smoothstep(
		highlight_threshold - highlight_softness,
		highlight_threshold + highlight_softness,
		bright_sharp
	);
	float highlight_soft = smoothstep(
		highlight_threshold * 0.75,
		highlight_threshold + 0.15,
		bright_soft
	);

	float activity = max(highlight, highlight_soft * 0.65);

	uvec2 px_u = uvec2(uint(px.x), uint(px.y));
	float variation = unit_float(pcg2d(px_u).x);
	float movement = activity * mix(1.0, variation, motion_variation) * motion_strength;

	float frame = floor(TIME * scroll_speed);
	int row_offset = int(floor(frame * movement));

	uvec2 lattice = uvec2(uint(px.x), uint(px.y + row_offset));
	float noise = unit_float(pcg2d(lattice).x);

	noise = clamp((noise - 0.5) * static_contrast + static_brightness, 0.0, 1.0);

	COLOR = vec4(vec3(noise), 1.0);
}
