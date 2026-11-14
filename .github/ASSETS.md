# Making Assets

A build asset is a Godot scene: a model, a few colliders, and, for moving parts, ordinary Godot joints. Most assets need
no code. This page covers how to make one and try it in the game, and lists the art and audio Ensemble still needs. To
set up the project first, see [**CONTRIBUTING.md**](/.github/CONTRIBUTING.md).

## The Scene

Each asset is one scene under `build_assets/`, named in PascalCase (`Wedge.tscn`). Its folder is its category in the
Asset Selector. Copy the closest existing asset and change it; `build_assets/Basic/Wedge.tscn` is a good start.

The root is a `StaticBody3D` with the `AssetHandle` script, whose inspector holds the asset's settings:

| Setting            | Meaning                                                                                  |
|--------------------|------------------------------------------------------------------------------------------|
| `AssetId`          | A number no other asset has. Saves store it, so it never changes once the asset ships    |
| `AssetName`        | The name players see                                                                     |
| `Description`      | The Asset Selector's tooltip                                                             |
| `Properties`       | Each instance's editable values, with their defaults (see [**Properties**](#properties)) |
| `MaxInstanceCount` | How many of it one plot may hold                                                         |
| `Density`          | Its weight per volume when no material sets one; the default suits a hollow block        |
| `BoundaryAabb`     | The box that snaps to the grid. Leave it empty to have it calculated from the colliders  |

Under the root go the colliders (`CollisionShape3D`), the meshes (`MeshInstance3D`), and usually a `Paintable` node,
which makes every mesh follow the instance's color and material.

## Scale and the Grid

One Godot unit is one meter, and one grid cell is half a meter: the Cube is 0.5 m on each side. Model around the scene's
origin, and size the asset in whole cells wherever its shape allows, so it lines up with everything else. Players can
snap more finely than a cell, so odd sizes work too; they are just harder to build with.

The origin is where the player's cursor holds the asset, and what it turns around. Most assets center it; a long one can
put it in an end cell instead (the beams do), so players place it from that end.

## Colliders

- Colliders must be direct children of the root (or of a piece; see [**Moving Parts**](#moving-parts)). Godot ignores
  deeper ones.
- Prefer primitive shapes (box, sphere, cylinder, capsule): they are the cheapest to simulate, and their weight is
  exact. Use a convex polygon for anything else, and several colliders for a shape with a hollow.
- An asset's weight is its colliders' volume times its density, so make the colliders fill what the mesh shows.
- Faces that touch weld when a creation spawns. To keep a face from welding (a wheel beside its mount), move its
  collider a millimeter or two inside the asset's cell.

## Meshes and Materials

- Primitive meshes from Godot are fine. Custom models are `OBJ` files in `assets/meshes/`, named in snake case
  (`prism_triangular_right.obj`), at real size (meters), with normals, and as few triangles as the shape allows: a plot
  may hold a thousand of them.
- Give each mesh a `StandardMaterial3D` with per-vertex shading, as the existing assets do. It is a deliberate choice
  for frame rate.
- Leave the base color white. `Paintable` tints it with the player's color and swaps in the chosen material; list a
  mesh under its `Excluded` setting to keep that mesh as authored.

## Properties

`Properties` is a dictionary of names and default values (numbers, text, or true / false). Players edit them with the
Edit tool, and saves keep them.

- A name starting with an underscore is internal, hidden unless the player asks. Every paintable asset has
  `_colorHex` (`"FFFFFF"`) and `_materialId` (`-1`, for none).
- A name ending in `Key` is a key binding, edited with a key picker.
- Lengths are in grid cells, not meters.

## Moving Parts

A part that moves on its own while a creation is live is a piece: a `StaticBody3D` with the `AssetPiece` script, a
direct child of the root, with its own colliders and meshes. Link pieces (or a piece and the root) with any Godot joint,
also a direct child of the root, set up in the inspector as usual. While building, the asset is rigid; when its plot
spawns, the joints take over. `build_assets/Advanced/Hinges/FreeHinge.tscn` does this without a line of code.

Leave a small gap between the pieces a joint links, or they weld into one body and the joint is skipped (the log says
so).

An asset whose behavior depends on its properties or on keys needs a script: a class deriving from `AssetHandle`, beside
its scene. `Suspension.cs` and `Hinges/Motor.cs` are the examples to follow.

## Testing In-Game

1. Run the game from the Godot editor or your IDE, and start single-player (`Esc`).
2. Read the log (`F9`, or the terminal): an asset with a duplicate ID, a stray physics body, or a misplaced joint is
   reported there by path.
3. Pick the asset in the Asset Selector and place a few, against each other and against other assets, on every face.
   They should snap flush and never overlap.
4. Spawn the plot (the Spawn button, or `set_plot_spawned(true)` in the console). `dmp_creation()` lists the bodies the
   creation became, with each one's mass: check that what should weld did, and that moving parts didn't.
5. Push it around, stack it, and drop it. Then despawn, save it (`save_creation("test")`), restart, and load it.

## Help Wanted

Ensemble's code is ahead of its content. These would help most, roughly in order; open an issue or a discussion before
starting something large, so two people don't make the same thing.

- **Build asset meshes.** Shapes the basic set lacks (arches, pipes, corner rounds, plates), and parts with character
  (wheels with tread, a steering wheel, lights, a proper seat). The rules above apply: `OBJ`, real size, whole cells,
  low triangle counts, white base color.
- **Music.** Loopable background tracks for building and for driving, and a short menu theme, as `OGG` (or `WAV`, which
  we convert), stereo, without clipping, with a loop point that doesn't click.
- **Sound effects.** Placing and deleting, collisions by strength, motors, and UI clicks, as short mono `WAV` files.
- **A character.** The placeholder is a crystal. A replacement needs a rigged, low-poly model (`glTF`) that fits the
  current character's capsule (about 1.4 m tall), with idle, run, jump, fall, and sit animations.
- **A map.** Terrain is [Terrain3D](https://github.com/TokisanGames/Terrain3D): heightmaps, textures, and ideas for a
  world with flat building space, roads, ramps, and something to drive toward are all welcome.

Contributed assets are licensed under CC BY-NC-SA 4.0 like the rest (see [**LICENSE.md**](/LICENSE.md)), and their
authors are credited in the README. Only submit work that is your own, or that its license lets us share on those terms,
and say which.
