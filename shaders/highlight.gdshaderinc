uniform vec3 box_size;

uniform float edge_thickness;
uniform float face_alpha = 0.125;
uniform float opacity = 1.0;

uniform bool marching_ants;
uniform float ant_spacing = 0.083333;
uniform float ant_speed = 0.125;

uniform bool pulsing;
uniform float pulse_minimum = 0.5;
uniform float pulse_period = 1.0;

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

// One-hot mask of the largest component; ties go to the earlier axis
vec3 argmax(vec3 v)
{
	float x = step(v.y, v.x) * step(v.z, v.x);
	float y = (1.0 - x) * step(v.z, v.y);

	return vec3(x, y, 1.0 - x - y);
}

// Antialiased dashes along an edge, from a position along it in meters, marching toward the positive end
float ants(float along)
{
	// A triangle wave, as it stays continuous where a sawtooth would wrap and break fwidth
	float wave = abs(fract((along - TIME * ant_speed) / (2.0 * ant_spacing)) - 0.5);
	float aa = fwidth(wave);

	return marching_ants ? smoothstep(0.25 - aa, 0.25 + aa, wave) : 1.0;
}

// The edges' opacity, swinging from 1 to pulse_minimum and back once per pulse_period in seconds
float pulse()
{
	return pulsing ? mix(pulse_minimum, 1.0, 0.5 + 0.5 * cos(TIME * TAU / pulse_period)) : 1.0;
}
