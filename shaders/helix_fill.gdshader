shader_type spatial;

// The helix as a highlight's fill
render_mode unshaded, cull_back, blend_mix, depth_draw_never;

#define FILL

#include "res://shaders/helix.gdshaderinc"
