# Ensemble

[![Create Release with Deterministic Source Archive and Its Digest](https://github.com/SubParSuperCar/Ensemble/actions/workflows/zip.yml/badge.svg)](https://github.com/SubParSuperCar/Ensemble/actions/workflows/zip.yml)
[![Upload Source Lines of Code](https://github.com/SubParSuperCar/Ensemble/actions/workflows/cloc.yml/badge.svg)](https://github.com/SubParSuperCar/Ensemble/actions/workflows/cloc.yml)

A multiplayer, collaborative sandbox building game made with Godot 4, C#, .NET, and Avalonia 12. This project is
created, maintained, and owned by **SubParSuperCar** ([GitHub profile](https://github.com/SubParSuperCar)).

![](/assets/images/ensemble_icon_square_colored.png "Ensemble's Icon (Made w/ Inkscape)")

---

> [!NOTE]
> - **Ensemble** is the direct successor to **Baja Builders** on Roblox: https://www.roblox.com/games/85484945236913
> - Code quality may be "sub-par" (pun intended) as the codebase continues to mature.

> [!WARNING]
> **Ensemble** is in the very early stages of development (pre-release) and should not be considered representative of
future 1.x or later releases. The project has been open-sourced early to encourage feedback, discussion, and
contributions while its architecture, systems, and implementation continue to evolve.

---

## Name

This game was originally called **Baja Builders** when it was on Roblox from approximately 2022-2025. However, the name
never really resonated with me, and it technically translates to "Below Builders." I ultimately renamed it to
**Ensemble** for two primary reasons:

1. "Ensemble" literally means a group of people, which reflects the game's multiplayer, collaborative nature.
2. It also sounds like "assemble," making it a fitting name for a building game.

---

## Structure

<details>
  <summary>Click here to expand/collapse the section.</summary>

The structure of this project is described in the high-level overview below:

- **Core**: Contains the data model for the entire game. It handles data-related operations such as adding, removing,
  and updating players, assets, asset instances (or simply "instances"), plots, and plot occupants (or simply
  "occupants"). It is divided into three distinct sections: `api`, `impl`, and `gd`.
    - `api` defines the contracts between the `impl` and `gd` layers.
    - `impl` contains the Godot-agnostic logic for the Core system, making it portable to any platform that supports
      .NET.
    - `gd` serves as a bridge for accessing Core from GDScript and other Godot-specific types.

- **Session Manager**: Contains the resources for managing session lifetimes. It supports both single-player and
  multiplayer through Godot's Multiplayer API and also contains an RPC partial for network replication. Depending on the
  context, it uses a hybrid of Host <-> Client and Client <-> Client networking. Like Core, it is divided into three
  sections: `api`, `impl`, and
  `gd`.
    - `api` contains contracts and interfaces.
    - `gd` contains Godot-facing objects (entry points, etc.).
    - `impl` contains implementation details that Godot should not be aware of.

- **UI**: Contains the game's user interface. It relies on Avalonia UI through a fork of
  [youfch's forked Estragonia](https://github.com/youfch/Estragonia), which itself is based
  on [MrJul's original Estragonia](https://github.com/MrJul/Estragonia) project. The `gd` section contains the entry
  points required to integrate the UI into the game, while the implementation is organized into conventional MVVM
  components such as views and view models for extensibility and maintainability.

- **Common**: Contains shared objects and resources used throughout the codebase, such as input extensions, utilities,
  and other reusable components that are essential to the game but do not constitute standalone systems.

- **Scripts**: Contains classes and other objects used for subsystems (autoloads) and Godot node scripts throughout the
  game. Examples include the hasher, logger, camera, character controller, watchdog, and more. Core would technically
  belong here, but especially large systems have been moved to the project root for better organization.

- **Lib**: Contains third-party resources, such as the Estragonia fork, and can generally be ignored when contributing.
  It is maintained but should be viewed as an implementation detail rather than a first-class project component.

Any omitted aspects of the codebase are either too niche or too commonplace to warrant mentioning here.
</details>

---

## License

This project is licensed under the terms of the license(s) found in [LICENSE.md](/LICENSE.md).
