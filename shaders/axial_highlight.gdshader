shader_type spatial;

render_mode unshaded, cull_disabled, blend_mix, depth_draw_never, depth_test_disabled;

uniform float edge_thickness;
uniform vec3 box_size;

varying vec3 local_position;

void vertex()
{
	local_position = VERTEX;
}

float edge_mask(float distance_to_edge)
{
	float aa = fwidth(distance_to_edge);

	return 1.0 - smoothstep(
		edge_thickness - aa,
		edge_thickness + aa,
		distance_to_edge
	);
}

vec3 axis_color(float position, vec3 color)
{
	return mix(vec3(1.0), color, sqrt(position));
}

void fragment()
{
	vec3 half_size = box_size * 0.5;
	vec3 p = abs(local_position);

	float dx = half_size.x - p.x;
	float dy = half_size.y - p.y;
	float dz = half_size.z - p.z;

	float x_line = edge_mask(max(dy, dz));
	float y_line = edge_mask(max(dx, dz));
	float z_line = edge_mask(max(dx, dy));

	float x_position = (local_position.x + half_size.x) / box_size.x;
	float y_position = (local_position.y + half_size.y) / box_size.y;
	float z_position = (local_position.z + half_size.z) / box_size.z;

	vec3 x_color = axis_color(x_position, vec3(1.0, 0.0, 0.0));
	vec3 y_color = axis_color(y_position, vec3(0.0, 1.0, 0.0));
	vec3 z_color = axis_color(z_position, vec3(0.0, 0.0, 1.0));

	vec3 color;
	float alpha;

	if (x_line >= y_line && x_line >= z_line)
	{
		color = x_color;
		alpha = x_line;
	}
		else if (y_line >= z_line)
		{
			color = y_color;
			alpha = y_line;
		}
			else
			{
				color = z_color;
				alpha = z_line;
			}

	ALBEDO = color;
	ALPHA = alpha;
}
