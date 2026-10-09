shader_type spatial;

render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, depth_test_disabled;

#include "res://shaders/highlight.gdshaderinc"

uniform vec3 tint : source_color = vec3(1.0);

void fragment()
{
	vec3 d = face_distances(local_position);

	float line = max(edge_mask(max(d.y, d.z)), max(edge_mask(max(d.x, d.z)), edge_mask(max(d.x, d.y))));

	ALBEDO = tint;
	ALPHA = FRONT_FACING ? mix(face_alpha, 1.0, line) : line;
}
