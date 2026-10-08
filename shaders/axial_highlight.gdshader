shader_type spatial;

render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, depth_test_disabled;

#include "res://shaders/highlight.gdshaderinc"

vec3 axis_color(float position, vec3 color)
{
	return mix(vec3(1.0), color, sqrt(position));
}

void fragment()
{
	vec3 half_size = box_size * 0.5;
	vec3 d = face_distances(local_position);

	float x_line = edge_mask(max(d.y, d.z));
	float y_line = edge_mask(max(d.x, d.z));
	float z_line = edge_mask(max(d.x, d.y));

	vec3 face_color;

	if (d.x <= d.y && d.x <= d.z) {
		face_color = vec3(1.0, 0.0, 0.0);
	} else if (d.y <= d.z) {
		face_color = vec3(0.0, 1.0, 0.0);
	} else {
		face_color = vec3(0.0, 0.0, 1.0);
	}

	vec3 edge_color;
	float line;

	if (x_line >= y_line && x_line >= z_line) {
		float position = (local_position.x + half_size.x) / box_size.x;
		edge_color = axis_color(position, vec3(1.0, 0.0, 0.0));
		line = x_line;
	} else if (y_line >= z_line) {
		float position = (local_position.y + half_size.y) / box_size.y;
		edge_color = axis_color(position, vec3(0.0, 1.0, 0.0));
		line = y_line;
	} else {
		float position = (local_position.z + half_size.z) / box_size.z;
		edge_color = axis_color(position, vec3(0.0, 0.0, 1.0));
		line = z_line;
	}

	ALBEDO = mix(face_color, edge_color, line);
	ALPHA = FRONT_FACING ? mix(face_alpha, 1.0, line) : line;
}
