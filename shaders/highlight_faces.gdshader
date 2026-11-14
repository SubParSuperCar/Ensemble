shader_type spatial;

// Multiplying keeps an asset's shading visible through the tint, and saturates the tint on bright surfaces, where
// mixing would wash it out. Back faces would tint every pixel twice, as depth testing is off
render_mode unshaded, cull_back, blend_mul, depth_draw_never, depth_test_disabled;

#include "res://shaders/highlight.gdshaderinc"

void fragment()
{
	vec3 face_axis = argmax(-face_distances(local_position));
	ALBEDO = srgb_to_linear(mix(vec3(1.0), axis_color(face_axis), face_tint * opacity * pulse()));
}
