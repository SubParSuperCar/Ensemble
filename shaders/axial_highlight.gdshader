shader_type spatial;

render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, depth_test_disabled;

#include "res://shaders/highlight.gdshaderinc"

// About equally bright to the eye, as pure green reads far brighter than pure red, and pure blue far darker; the Asset
// Selector's rotation buttons share these colors
const mat3 AXIS_COLORS = mat3(vec3(1.0, 0.0, 0.0), vec3(0.0, 0.5, 0.0), vec3(0.125, 0.25, 1.0));

void fragment()
{
	vec3 d = face_distances(local_position);
	vec3 lines = edge_masks(d);

	vec3 face_axis = argmax(-d);
	vec3 edge_axis = argmax(lines);

	float along = dot(edge_axis, local_position);
	float position = along / dot(edge_axis, box_size) + 0.5;
	vec3 edge_color = mix(vec3(1.0), AXIS_COLORS * edge_axis, sqrt(position));
	float line = dot(edge_axis, lines) * ants(edge_axis) * pulse();

	ALBEDO = mix(AXIS_COLORS * face_axis, edge_color, line);
	ALPHA = (FRONT_FACING ? mix(face_alpha, 1.0, line) : line) * opacity;
}
