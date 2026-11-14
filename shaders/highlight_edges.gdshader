shader_type spatial;

render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, depth_test_disabled;

#include "res://shaders/highlight.gdshaderinc"
#include "res://shaders/lines.gdshaderinc"

uniform vec3 box_size;
uniform float edge_thickness;

// Whether only the edges from the box's lowest corner show, so its axes read at a glance
uniform bool origin_edges_only;

uniform bool marching_ants;
uniform float ant_spacing = 0.083333;
uniform float ant_speed = 0.125;

varying vec3 local_position;

void vertex()
{
	local_position = VERTEX;
}

// How far a local position is from the box's faces along each axis
vec3 face_distances(vec3 position)
{
	return box_size * 0.5 - abs(position);
}

// Coverage of the edges running along the X, Y, and Z axes. Each face shows its half of a line twice edge_thickness
// wide, centered on the edge it shares with its neighbor
vec3 edge_masks(vec3 d)
{
	vec3 distance_to_edge = max(d.yxx, d.zzy);
	vec3 footprints = vec3(
		pixel_footprint(local_position, vec3(1.0, 0.0, 0.0)), pixel_footprint(local_position, vec3(0.0, 1.0, 0.0)),
		pixel_footprint(local_position, vec3(0.0, 0.0, 1.0)));

	// Across each edge runs whichever axis its distance came from
	vec3 across = mix(footprints.zzy, footprints.yxx, step(d.zzy, d.yxx));
	float width = 2.0 * edge_thickness;
	vec3 masks = vec3(
		line_coverage(distance_to_edge.x, width, across.x), line_coverage(distance_to_edge.y, width, across.y),
		line_coverage(distance_to_edge.z, width, across.z));

	return origin_edges_only
		? masks * step(local_position.yxx, vec3(0.0)) * step(local_position.zzy, vec3(0.0))
		: masks;
}

// One-hot mask of the largest component; ties go to the earlier axis
vec3 argmax(vec3 v)
{
	float x = step(v.y, v.x) * step(v.z, v.x);
	float y = (1.0 - x) * step(v.z, v.y);

	return vec3(x, y, 1.0 - x - y);
}

// Antialiased dashes along an edge, marching toward its positive end. Every axis's dashes are found before choosing
// one, as the edge axis jumps between neighboring pixels at a line's border, and a derivative across that jump would
// smear a dash over the gaps
float ants(vec3 edge_axis)
{
	// A triangle wave, as it stays continuous where a sawtooth would wrap and break fwidth
	vec3 wave = abs(fract((local_position - TIME * ant_speed) / (2.0 * ant_spacing)) - 0.5);
	vec3 aa = fwidth(wave);

	return marching_ants ? dot(edge_axis, smoothstep(0.25 - aa, 0.25 + aa, wave)) : 1.0;
}

// An axial edge fades in from white at its negative end, where the axes start
vec3 edge_color(vec3 edge_axis)
{
	if (!axial)
		return srgb_to_linear(tint);

	int axis = int(dot(edge_axis, vec3(0.0, 1.0, 2.0)));
	float along = dot(edge_axis, local_position / box_size) + 0.5;

	return mix_perceptual(vec3(1.0), axis_color(axis), along);
}

void fragment()
{
	vec3 lines = edge_masks(face_distances(local_position));
	vec3 edge_axis = argmax(lines);

	ALBEDO = edge_color(edge_axis);
	ALPHA = dot(edge_axis, lines) * ants(edge_axis) * opacity * pulse();
}
