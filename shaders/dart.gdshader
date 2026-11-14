shader_type spatial;

render_mode unshaded, blend_mix, depth_draw_never, depth_test_disabled, cull_back;

#include "res://shaders/color.gdshaderinc"
#include "res://shaders/lines.gdshaderinc"

uniform vec3 color : source_color = vec3(1.0);

// The dart's length and breadth in meters, pointing toward -Z; its mesh needs room past it for the line's width
uniform float size = 4.0;

// Halfway between the plot grid's cell and major lines, a step above the cells
uniform float width = 0.0234375;
uniform float opacity = 1.0;

varying vec3 local_position;

void vertex()
{
	local_position = VERTEX;
}

// From the nearest point of a segment to a point
vec2 segment_offset(vec2 point, vec2 a, vec2 b)
{
	vec2 ab = b - a;
	return point - (a + ab * clamp(dot(point - a, ab) / dot(ab, ab), 0.0, 1.0));
}

vec2 nearest(vec2 a, vec2 b)
{
	return dot(a, a) <= dot(b, b) ? a : b;
}

void fragment()
{
	// Tip, wings, and the notch at the center
	float half_size = size * 0.5;
	vec2 tip = vec2(0.0, -half_size);
	vec2 left = vec2(-half_size, half_size);
	vec2 right = vec2(half_size, half_size);
	vec2 point = local_position.xz;

	vec2 offset = nearest(
		nearest(segment_offset(point, tip, left), segment_offset(point, tip, right)),
		nearest(segment_offset(point, left, vec2(0.0)), segment_offset(point, right, vec2(0.0))));

	float distance = length(offset);
	vec2 across = distance > 0.0 ? offset / distance : vec2(1.0, 0.0);
	float footprint = pixel_footprint(local_position, vec3(across.x, 0.0, across.y));

	ALBEDO = color;
	ALPHA = line_coverage(distance, width, footprint) * opacity_to_alpha(opacity);
}
