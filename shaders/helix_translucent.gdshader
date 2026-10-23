shader_type spatial;

render_mode vertex_lighting;

// The helix, faded, for previews: writing ALPHA moves a shader into the transparent pass, so only previews pay for it
#define TRANSLUCENT

#include "res://shaders/helix.gdshaderinc"
