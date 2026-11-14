shader_type spatial;

// Drawn over the highlighted asset's own surfaces, so only its geometry is tinted, never the space around it
render_mode unshaded, cull_back, blend_mix, depth_draw_never;

#include "res://shaders/highlight_fill.gdshaderinc"

varying vec3 lab;

void vertex()
{
	lab = axial_lab(MODEL_NORMAL_MATRIX * NORMAL);
}

void fragment()
{
	ALBEDO = fill_albedo(lab);
	ALPHA = fill_alpha();
}
