shader_type spatial;

render_mode vertex_lighting;

// Bends a CylinderMesh (along Y, 1 unit tall) into a coil spring that stands on the node's origin and reaches up
// coil_length, so each spring stretches on its own while sharing one material. The cylinder is the wire: its height
// runs along the coil and its round section stays the wire's, so it needs enough rings for smooth turns (about 24 a
// turn). Set a custom AABB on the instance, as the coil reaches beyond the cylinder's own.

uniform vec3 color : source_color = vec3(1.0);
uniform float turns = 5.0;

// From the coil's axis to the wire's center
uniform float coil_radius = 0.1625;

// Half the wire's thickness
uniform float wire_radius = 0.025;

uniform float roughness : hint_range(0.0, 1.0) = 0.5;
uniform float metallic : hint_range(0.0, 1.0) = 0.0;

// From the bottom of the wire to its top
instance uniform float coil_length = 1.0;

void vertex()
{
	float along = VERTEX.y + 0.5;
	float around = atan(VERTEX.z, VERTEX.x);
	float spin = turns * TAU;
	float angle = along * spin;

	// The wire's ends touch the plates rather than sinking into them
	float rise = max(coil_length - 2.0 * wire_radius, 0.0);
	vec3 center = vec3(coil_radius * cos(angle), wire_radius + along * rise, coil_radius * sin(angle));

	// A frame that follows the wire, so its section stays round however steep the coil
	vec3 tangent = normalize(vec3(-coil_radius * spin * sin(angle), rise, coil_radius * spin * cos(angle)));
	vec3 outward = vec3(cos(angle), 0.0, sin(angle));
	vec3 binormal = normalize(cross(tangent, outward));
	vec3 normal = cross(binormal, tangent);

	// The cylinder's X, Y, and Z become the normal, the tangent, and the reversed binormal, which keeps the frame
	// right-handed, so triangles keep facing outward
	vec3 offset = cos(around) * normal - sin(around) * binormal;

	VERTEX = center + wire_radius * offset;
	NORMAL = offset;
}

void fragment()
{
	ALBEDO = color;
	ROUGHNESS = roughness;
	METALLIC = metallic;
}
