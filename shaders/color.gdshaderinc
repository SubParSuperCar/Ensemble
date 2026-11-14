// Colors are given in sRGB, as they look, and decoded to the linear light shaders work in

vec3 srgb_to_linear(vec3 color)
{
	return mix(color / 12.92, pow((color + 0.055) / 1.055, vec3(2.4)), step(0.04045, color));
}

// Blending happens in linear light too, where a surface would look more opaque than its opacity; decoding the opacity
// as sRGB makes it show at exactly that opacity, but only over black
float opacity_to_alpha(float opacity)
{
	return srgb_to_linear(vec3(opacity)).x;
}

// Bjorn Ottosson's Oklab, where equal steps look equally different, so a gradient through it changes evenly. Each
// matrix's vectors are its rows, applied as v * M
const mat3 LINEAR_TO_LMS = mat3(
	vec3(0.4122214708, 0.5363325363, 0.0514459929), vec3(0.2119034982, 0.6806995451, 0.1073969566),
	vec3(0.0883024619, 0.2817188376, 0.6299787005));

const mat3 LMS_TO_OKLAB = mat3(
	vec3(0.2104542553, 0.7936177850, -0.0040720468), vec3(1.9779984951, -2.4285922050, 0.4505937099),
	vec3(0.0259040371, 0.7827717662, -0.8086757660));

const mat3 OKLAB_TO_LMS = mat3(
	vec3(1.0, 0.3963377774, 0.2158037573), vec3(1.0, -0.1055613458, -0.0638541728),
	vec3(1.0, -0.0894841775, -1.2914855480));

const mat3 LMS_TO_LINEAR = mat3(
	vec3(4.0767416621, -3.3077115913, 0.2309699292), vec3(-1.2684380046, 2.6097574011, -0.3413193965),
	vec3(-0.0041960863, -0.7034186147, 1.7076147010));

vec3 linear_to_oklab(vec3 color)
{
	return pow(color * LINEAR_TO_LMS, vec3(1.0 / 3.0)) * LMS_TO_OKLAB;
}

vec3 oklab_to_linear(vec3 lab)
{
	vec3 lms = lab * OKLAB_TO_LMS;

	// Blends between colors inside the sRGB gamut can stray just outside it
	return max(lms * lms * lms * LMS_TO_LINEAR, vec3(0.0));
}

// A blend of two linear colors that changes evenly to the eye
vec3 mix_perceptual(vec3 from, vec3 to, float weight)
{
	return oklab_to_linear(mix(linear_to_oklab(from), linear_to_oklab(to), weight));
}
