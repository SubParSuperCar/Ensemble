shader_type spatial;

render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, depth_test_disabled;

uniform vec3 box_size;
uniform vec3 tint : source_color = vec3(1.0);

uniform float edge_thickness;
uniform float face_alpha = 0.1;

varying vec3 local_position;

void vertex()
{
	local_position = VERTEX;
}

float edge_mask(float distance_to_edge)
{
	float aa = fwidth(distance_to_edge);

	return 1.0 - smoothstep(
		edge_thickness - aa,
		edge_thickness + aa,
		distance_to_edge
	);
}

void fragment()
{
	vec3 half_size = box_size * 0.5;
	vec3 p = abs(local_position);

	float dx = half_size.x - p.x;
	float dy = half_size.y - p.y;
	float dz = half_size.z - p.z;

	float line = max(
		edge_mask(max(dy, dz)),
		max(edge_mask(max(dx, dz)), edge_mask(max(dx, dy)))
	);

	ALBEDO = tint;
	ALPHA = FRONT_FACING ? mix(face_alpha, 1.0, line) : line;
}
