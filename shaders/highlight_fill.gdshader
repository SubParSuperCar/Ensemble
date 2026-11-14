shader_type spatial;

// Drawn over the highlighted asset's own surfaces, so only its geometry is tinted, never the space around it
render_mode unshaded, cull_back, blend_mix, depth_draw_never;

#include "res://shaders/highlight.gdshaderinc"

uniform float fill_opacity = 0.25;

// From world space into the highlight's axes
uniform mat3 world_to_axes = mat3(1.0);

// The axes' colors in Oklab, weighted by how much the surface faces each: a unit normal's squared components, which sum
// to 1. A surface between two axes reads as both, and as the weighting is linear, it interpolates exactly
varying vec3 axial_lab;

void vertex()
{
	vec3 normal = normalize(world_to_axes * (MODEL_NORMAL_MATRIX * NORMAL));
	mat3 axis_labs = mat3(
		linear_to_oklab(axis_color(0)), linear_to_oklab(axis_color(1)), linear_to_oklab(axis_color(2)));

	axial_lab = axis_labs * (normal * normal);
}

void fragment()
{
	ALBEDO = axial ? oklab_to_linear(axial_lab) : srgb_to_linear(tint);
	ALPHA = fill_opacity * opacity * pulse();
}
