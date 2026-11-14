shader_type spatial;

render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, depth_test_disabled;

#include "res://shaders/highlight.gdshaderinc"

void fragment()
{
	vec3 lines = edge_masks(face_distances(local_position));
	vec3 edge_axis = argmax(lines);

	ALBEDO = srgb_to_linear(axis_color(edge_axis));
	ALPHA = dot(edge_axis, lines) * ants(edge_axis) * opacity_to_alpha(opacity * pulse());
}
