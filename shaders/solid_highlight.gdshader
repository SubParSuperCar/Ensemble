shader_type spatial;

render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, depth_test_disabled;

#include "res://shaders/highlight.gdshaderinc"

uniform vec3 tint : source_color = vec3(1.0);

void fragment()
{
	vec3 lines = edge_masks(face_distances(local_position));
	vec3 edge_axis = argmax(lines);
	float line = dot(edge_axis, lines) * ants(dot(edge_axis, local_position));

	ALBEDO = tint;
	ALPHA = (FRONT_FACING ? mix(face_alpha, 1.0, line) : line) * opacity;
}
