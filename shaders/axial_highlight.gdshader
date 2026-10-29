shader_type spatial;

render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, depth_test_disabled;

#include "res://shaders/highlight.gdshaderinc"

void fragment()
{
	vec3 d = face_distances(local_position);
	vec3 lines = edge_masks(d);

	vec3 face_axis = argmax(-d);
	vec3 edge_axis = argmax(lines);

	float along = dot(edge_axis, local_position);
	float position = along / dot(edge_axis, box_size) + 0.5;
	vec3 edge_color = mix(vec3(1.0), edge_axis, sqrt(position));
	float line = dot(edge_axis, lines) * ants(along) * pulse();

	ALBEDO = mix(face_axis, edge_color, line);
	ALPHA = (FRONT_FACING ? mix(face_alpha, 1.0, line) : line) * opacity;
}
