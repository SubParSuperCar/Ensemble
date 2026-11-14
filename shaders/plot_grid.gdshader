shader_type spatial;

render_mode unshaded, blend_mix, depth_draw_never, cull_back;

#include "res://shaders/color.gdshaderinc"
#include "res://shaders/lines.gdshaderinc"

uniform vec3 color : source_color = vec3(1.0);

// Per level, finest first: the spacing between lines and their width in meters, and their opacity as it should look
uniform vec3 spacings = vec3(0.0625, 0.5, 4.0);
uniform vec3 widths = vec3(0.0078125, 0.015625, 0.03125);
uniform vec3 opacities = vec3(0.333333, 0.666667, 1.0);

varying vec3 local_position;

void vertex()
{
	local_position = VERTEX;
}

float level_alpha(float spacing, float width, float opacity, vec2 footprints)
{
	float x = grid_coverage(local_position.x, spacing, width, footprints.x);
	float z = grid_coverage(local_position.z, spacing, width, footprints.y);

	return max(x, z) * opacity_to_alpha(opacity);
}

void fragment()
{
	vec2 footprints = vec2(
		pixel_footprint(local_position, vec3(1.0, 0.0, 0.0)), pixel_footprint(local_position, vec3(0.0, 0.0, 1.0)));

	ALBEDO = color;

	// The strongest level wins where lines cross, so crossings don't stack
	ALPHA = max(
		level_alpha(spacings.x, widths.x, opacities.x, footprints),
		max(level_alpha(spacings.y, widths.y, opacities.y, footprints),
			level_alpha(spacings.z, widths.z, opacities.z, footprints)));
}
