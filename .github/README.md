# Ensemble

[![Create Release with Deterministic Source Archive and Its Digest](https://github.com/SubParSuperCar/Ensemble/actions/workflows/zip.yml/badge.svg)](https://github.com/SubParSuperCar/Ensemble/actions/workflows/zip.yml)
[![Upload Source Lines of Code](https://github.com/SubParSuperCar/Ensemble/actions/workflows/cloc.yml/badge.svg)](https://github.com/SubParSuperCar/Ensemble/actions/workflows/cloc.yml)

A multiplayer, collaborative sandbox building game made with Godot 4, C#, .NET, and Avalonia 12. This project is
created, maintained, and owned by **SubParSuperCar** ([GitHub profile](https://github.com/SubParSuperCar)).

<img align="left" src="/assets/images/ensemble_icon_square_colored.png" alt="Ensemble's Icon (Made w/ Inkscape)">

*“Nothing is Arbitrary; Everything is Relative.”*
<br clear="left"/>

---

> [!NOTE]
> - **Ensemble** is the direct successor to **Baja Builders** on Roblox: https://www.roblox.com/games/85484945236913
> - Code quality may be "sub-par" (pun intended) as the codebase continues to mature.

> [!WARNING]
> **Ensemble** is in the very early stages of development (pre-release) and should not be considered representative of
future 1.x or later releases. The project has been open-sourced early to encourage feedback, discussion, and
contributions while its architecture, systems, and implementation continue to evolve.

---

## Name Rationale

This game was originally called **Baja Builders** when it was on Roblox from approximately 2022-2025. However, the name
never really resonated with me, and it technically translates to "Below Builders." I ultimately renamed it to
**Ensemble** for two primary reasons:

1. "Ensemble" literally means a group of people, which reflects the game's multiplayer, collaborative nature.
2. It also sounds like "assemble," making it a fitting name for a building game.

---

## Structure

<details open>
  <summary>Click here to expand/collapse the section.</summary>

The structure of this project is described in the high-level overview below:

- **Core**: Contains the data model for the entire game. It handles data-related operations such as adding, removing,
  and updating players, assets, asset instances (or simply "instances"), plots, and plot occupants (or simply
  "occupants"). It is divided into three distinct sections: `api`, `impl`, and `gd`.
    - `api` defines the contracts between the `impl` and `gd` layers.
    - `impl` contains the Godot-agnostic logic for the Core system, making it portable to any platform that supports
      .NET.
    - `gd` serves as a bridge for accessing Core from GDScript and other Godot-specific types.

- **Session Manager**: Manages session lifetimes. It supports both single-player and multiplayer through Godot's
  Multiplayer API and also contains an RPC partial for network replication (W.I.P.). Depending on the context, it uses a
  hybrid of Host <-> Client and Client <-> Client networking. Like Core, it is divided into three sections: `api`,
  `impl`, and `gd`.
    - `api` contains contracts and interfaces.
    - `gd` contains Godot-facing objects (entry points, etc.).
    - `impl` contains implementation details that are independent of Godot.

- **Save Manager**: (W.I.P.) Contains the resources for saving creations to disk and loading them back. Currently, it
  supports both binary and `JSON` formats, along with Zstandard (Zstd) compression. SHA-256 hash checking and AES-256
  encryption may be supported in the future.

- **Tool Manager**: (W.I.P.) Contains the building tools for modifying creations.

- **Common**: Contains shared objects and resources used throughout the codebase, such as input extensions, `HttpClient`
  objects, utilities, and other reusable components that are essential to the game but do not constitute standalone
  systems.

- **Common/Lua Executor**: Provides **Lua 5.2** script execution through the Lua-CSharp NuGet package, with custom
  functions for debugging and runtime inspection. It is used by the Console UI component for runtime scripting and
  testing.

- **Scripts**: Contains classes and other objects used for subsystems (autoloads) and Godot node scripts throughout the
  game. Examples include the hasher, logger, camera, character controller, watchdog, and more. Core would technically
  belong here, but especially large systems have been moved to the project root for better organization.

- **UI**: Contains the game's user interface. It relies on Avalonia UI through a fork of
  [youfch's forked Estragonia](https://github.com/youfch/Estragonia), which itself is based
  on [MrJul's original Estragonia](https://github.com/MrJul/Estragonia) project. The `gd` section contains the entry
  points required to integrate the UI with Godot, while the implementation follows a conventional MVVM architecture
  consisting of views, view models, and services for extensibility and maintainability. It also features
  trimming-compatible compile-time assembly scanning for DI (Dependency Injection) and matching view models to their
  respective views.

- **Lib**: Contains third-party resources, such as the Estragonia fork, and can generally be ignored when contributing.
  It is maintained but should be viewed as an implementation detail rather than a first-class project component.

Any omitted aspects of the codebase are either too niche or too commonplace to warrant mentioning here.
</details>

---

## How to Use Core API

<details open>
    <summary>Click here to expand/collapse the section.</summary>

The following C# example demonstrates a typical workflow for using the Core API, from registering assets and plots to
creating and modifying players and instances. It is intended as a basic introduction and is not exhaustive of the Core
API's full capabilities; additional functionality and usage patterns are available throughout the API.

  ```cs
    // GdCore is the Godot-facing wrapper around Impl.Core.
    // The underlying Core data model remains independent of Godot.
    var core = new GdCore();

    // These local references are optional; they simply make the example
    // more concise when working with Core's top-level collections.
    var players = core.Players;
    var assets = core.Assets;
    var plots = core.Plots;


    // Register Core data before using it.

    // Register an asset definition. The returned GdAsset represents the
    // newly registered asset within Core.
    //
    // Name, Properties, and Max Instance Count are optional.
    var newAsset = assets.Add(
        0, // Asset ID
        "Block", // Name
        [
            (new Variant("_colorHex"), new Variant("FFFFFF")),
            (new Variant("_materialId"), new Variant(0))
        ], // Properties
        1000); // Maximum number of instances

    // GdAssets.Added is emitted when the asset is registered.


    // Lock the asset collection once registration is complete.
    // This prevents additional assets from being registered.
    assets.Lock();


    // Register a plot definition.
    //
    // Max Occupant Count and Max Instance Count are optional.
    var newPlot = plots.Add(
        0, // Plot ID
        10, // Maximum number of occupants
        5000); // Maximum number of instances

    // GdPlots.Added is emitted when the plot is registered.


    // Lock the plot collection once registration is complete.
    plots.Lock();


    // Core can now be used after registration.


    // Add a player. When no arguments are given, Core automatically
    // generates the player's ID and name.
    var player = players.Add();

    // GdPlayers.Added is emitted when the player is registered.


    // Retrieve the plot we registered above.
    var plot = plots.GetPlot(newPlot.Id)!;

    // These local references are optional and are provided only to
    // make subsequent operations more concise.
    var occupants = plot.Occupants;
    var instances = plot.Instances;


    // Assign the player to the plot.
    //
    // This may automatically assign the player as the plot owner.
    // GdOccupant.PlotChanged and GdOccupants.Added are emitted.
    plots.SetPlot(player.Id, plot.Id);


    // Explicitly set the owner, even if ownership was already assigned
    // by SetPlot. This demonstrates the ownership API directly.
    //
    // GdOccupants.OwnerChanged is emitted when the owner changes.
    occupants.SetOwner(player.Id);


    // Add an instance of the "Block" asset to the plot.
    //
    // Instance positions are expressed in grid-space coordinates rather
    // than Godot's actual 3D world-space units.
    var newInstance = instances.Add(
        newAsset.Id, // Asset ID
        Vector3.Zero, // Grid-space position
        Quaternion.Identity); // Rotation

    // GdInstances.Added is emitted when the instance is registered.


    // Retrieve the instance and access its property collection.
    var instance = instances.GetInstance(newInstance.Id)!;
    var properties = instance.Properties;


    // Modify an instance property. Here, the block's color is changed
    // from white to bright red.
    //
    // GdProperties.Changed is emitted when the property changes.
    properties.Update("_colorHex", "FF0000");


    // Spawn the plot to weld and unfreeze instances, etc.
    //
    // GdPlot.IsSpawnedChanged is emitted if the plot was not already spawned.
    plot.Spawn();


    // Reset Core to its initial state.
    //
    // Reset removes all registered objects, emits the corresponding
    // Removed events, and unlocks collections that were previously locked.
    core.Reset();
  ```

</details>

---

## Credits

All `OBJ` files under `/assets/meshes/` except for `plots_base.obj` are created by "Shrimp Fried Koishi." Other
third-party resources, such as NuGet packages and files under `/addons/` and `/lib/`, belong to their owners.

---

## License

This project is licensed under the terms of the licenses found in [LICENSE.md](/LICENSE.md).
