// A highlight's fill, for its own shader and for variants of shaders that move vertices, which a fill must move alike

#include "res://shaders/highlight.gdshaderinc"

uniform float fill_opacity = 0.25;

// From world space into the highlight's axes
uniform mat3 world_to_axes = mat3(1.0);

// The axes' colors in Oklab, weighted by how much a surface faces each: a unit normal's squared components, which sum
// to 1. A surface between two axes reads as both, and as the weighting is linear, it interpolates exactly
vec3 axial_lab(vec3 world_normal)
{
	vec3 normal = normalize(world_to_axes * world_normal);
	mat3 axis_labs = mat3(
		linear_to_oklab(axis_color(0)), linear_to_oklab(axis_color(1)), linear_to_oklab(axis_color(2)));

	return axis_labs * (normal * normal);
}

vec3 fill_albedo(vec3 lab)
{
	return axial ? oklab_to_linear(lab) : srgb_to_linear(tint);
}

float fill_alpha()
{
	return fill_opacity * opacity * pulse();
}
