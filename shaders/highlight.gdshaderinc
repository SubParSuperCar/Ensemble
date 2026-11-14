#include "res://shaders/color.gdshaderinc"

// #FF0000, #00C000, and #0080FF, as the Asset Selector's rotation buttons. Pure green reads far brighter than pure red,
// and pure blue far darker
const mat3 AXIS_COLORS = mat3(vec3(1.0, 0.0, 0.0), vec3(0.0, 0.752941, 0.0), vec3(0.0, 0.501961, 1.0));

// Colored by axis, or else by the sRGB tint
uniform bool axial;
uniform vec3 tint = vec3(1.0);

// Drawn over surfaces of any brightness, where no decoding could make an opacity look as stated, so alphas stay the
// share of light replaced
uniform float opacity = 1.0;

uniform bool pulsing;
uniform float pulse_minimum = 0.5;
uniform float pulse_period = 0.5;
uniform float pulse_time;

// The highlight's opacity, swinging from 1 to pulse_minimum and back once per pulse_period in seconds
float pulse()
{
	return pulsing ? mix(pulse_minimum, 1.0, 0.5 + 0.5 * cos(pulse_time * TAU / pulse_period)) : 1.0;
}

vec3 axis_color(int axis)
{
	return srgb_to_linear(AXIS_COLORS[axis]);
}
