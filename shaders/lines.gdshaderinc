// Lines with widths in meters, antialiased alike in every shader: a line keeps its width as it recedes, like geometry,
// and once it is thinner than a pixel it is drawn a pixel wide but dimmed in proportion, so distant lines keep their
// average brightness instead of thickening or flickering

// The size of a pixel along a direction at this fragment, in the units of the position
float pixel_footprint(vec3 position, vec3 direction)
{
	return length(vec2(dot(dFdx(position), direction), dot(dFdy(position), direction)));
}

// The share of a pixel, as a box filter, that a line covers at a distance from its center
float line_coverage(float distance, float width, float footprint)
{
	float drawn = max(width, footprint);
	return clamp((drawn * 0.5 - abs(distance)) / footprint + 0.5, 0.0, 1.0) * width / drawn;
}

// Evenly spaced lines through position 0. Lines under two pixels apart can't be told apart (the Nyquist limit), so
// they blend into their average coverage by one pixel apart, an even tint instead of moire
float grid_coverage(float position, float spacing, float width, float footprint)
{
	float distance = (fract(position / spacing + 0.5) - 0.5) * spacing;
	float resolved = clamp(2.0 - 2.0 * footprint / spacing, 0.0, 1.0);

	return mix(width / spacing, line_coverage(distance, width, footprint), resolved);
}
