uniform vec3 box_size;

uniform float edge_thickness;
uniform float face_alpha = 0.125;

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

// Antialiased coverage of the edges running along the X, Y, and Z axes
vec3 edge_masks(vec3 d)
{
	vec3 distance_to_edge = max(d.yxx, d.zzy);
	vec3 aa = fwidth(distance_to_edge);

	return 1.0 - smoothstep(edge_thickness - aa, edge_thickness + aa, distance_to_edge);
}
