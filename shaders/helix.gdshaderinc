// Bends a CylinderMesh (along Y, 1 unit tall) into a helix that stands on the node's origin and reaches up
// helix_length, so each instance stretches on its own while sharing one material (e.g., a spring's coil). The cylinder
// is the tube: its height runs along the helix and its round section stays the tube's, so it needs enough rings for
// smooth turns (about 24 a turn). Set a custom AABB on the instance, as the helix reaches beyond the cylinder's own.

uniform vec3 color : source_color = vec3(1.0);
uniform float turns = 5.0;

// From the helix's axis to the tube's center
uniform float helix_radius = 0.1625;

// Half the tube's thickness
uniform float tube_radius = 0.025;

uniform float roughness : hint_range(0.0, 1.0) = 0.5;
uniform float metallic : hint_range(0.0, 1.0) = 0.0;

#ifdef TRANSLUCENT
uniform float opacity : hint_range(0.0, 1.0) = 1.0;
#endif

// From the bottom of the tube to its top
instance uniform float helix_length = 1.0;

void vertex()
{
	float along = VERTEX.y + 0.5;
	float around = atan(VERTEX.z, VERTEX.x);
	float spin = turns * TAU;
	float angle = along * spin;

	// The tube's ends touch the planes it spans rather than sinking into them
	float rise = max(helix_length - 2.0 * tube_radius, 0.0);
	vec3 center = vec3(helix_radius * cos(angle), tube_radius + along * rise, helix_radius * sin(angle));

	// A frame that follows the tube, so its section stays round however steep the helix
	vec3 tangent = normalize(vec3(-helix_radius * spin * sin(angle), rise, helix_radius * spin * cos(angle)));
	vec3 outward = vec3(cos(angle), 0.0, sin(angle));
	vec3 binormal = normalize(cross(tangent, outward));
	vec3 normal = cross(binormal, tangent);

	// The cylinder's X, Y, and Z become the normal, the tangent, and the reversed binormal, which keeps the frame
	// right-handed, so triangles keep facing outward
	vec3 offset = cos(around) * normal - sin(around) * binormal;

	VERTEX = center + tube_radius * offset;
	NORMAL = offset;
}

void fragment()
{
	ALBEDO = color;
	ROUGHNESS = roughness;
	METALLIC = metallic;
#ifdef TRANSLUCENT
	ALPHA = opacity;
#endif
}
