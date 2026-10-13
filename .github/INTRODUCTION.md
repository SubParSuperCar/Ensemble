# Introducing Ensemble

> *"Nothing is Arbitrary; Everything is Relative."*

Ensemble is a free, open-source, multiplayer sandbox building game: claim a plot, grab some shapes, and build something
with your friends. It's the successor to [Baja Builders](https://www.roblox.com/games/85484945236913), rebuilt from
scratch outside of Roblox with Godot 4, C#, and Avalonia.

This page has three parts, so read as far as you like:

1. [**For Players**](#for-players): what Ensemble is, and what you can do in it.
2. [**For Developers: The Big Picture**](#for-developers-the-big-picture): how it's built, in a few minutes.
3. [**For Developers: The Deep Dive**](#for-developers-the-deep-dive): the stack, layout, and systems in detail.

> [!NOTE]
> Ensemble is in early development (alpha). It's playable, but it's a foundation, not a finished game yet.

---

## For Players

### What It Is

Ensemble is a building game about making things together. Everyone gets a plot, and everything you place on it is shared
live with everyone else in the session. Build alone in single-player, or host a session and invite your friends.

If you played Baja Builders on Roblox, you'll feel at home: the ideas are the same, but without Roblox's limits.

### What You Can Do Today

- **Build** with blocks, wedges, cylinders, cones, spheres, and more. Pieces snap neatly to the grid and to each other,
  rotate on any axis, and never clip into one another.
- **Play together** over the internet or your local network. Hosting can forward its port automatically, so friends can
  join with a single code. Sessions can have passwords and player limits, and run on dedicated servers.
- **Chat** with Roblox-style colored nametags, an optional profanity filter (hosts choose), and an optional "read aloud"
  mode that speaks new messages.
- **Explore** a world with terrain and a full day-night cycle that can follow your real clock.
- **Arrange your screen** with movable, resizable windows (called widgets), in light or dark themes that follow your
  system's accent color.
- **Tinker** with a built-in Lua console, and even a web browser and system terminal inside the game.

### What's Coming

The road to 1.0 includes saving and loading your creations, a real map, more building tools (editing properties,
painting, and moving things), a plot manager for inviting and removing builders, and plenty of polish. See the
[**roadmap**](./ROADMAP.md) for everything that's planned.

### Trying It

Ensemble runs on **Windows**, **Linux**, and **macOS**, with nothing to install: download the [latest
release](https://github.com/SubParSuperCar/Ensemble/releases/latest), unzip it, and run it. The
[**README**](./README.md#download) has the details, including a one-time step for macOS.

It checks for updates when it starts, and it's free forever: no ads, no purchases.

---

## For Developers: The Big Picture

Ensemble is a Godot 4.7 game written in C# 14 on .NET 10, with its whole interface built in [Avalonia
12](https://avaloniaui.net), a desktop-grade UI framework, rendered inside the game. It's free software under the GPL
(code) and CC BY-NC-SA (assets).

What makes it interesting under the hood:

- **An engine-like, layered architecture.** The game is split into modules (pure domain data, Godot wrappers,
  networking, scene logic, tools, UI) whose dependencies only point one way, and that rule is checked on every build.
  The domain model doesn't know Godot exists, so it's unit-tested on its own.
- **Avalonia inside Godot.** The UI runs through a heavily modified fork of
  [Estragonia](https://github.com/MrJul/Estragonia) that renders Avalonia with Godot's own Vulkan or Metal device. That
  gives the game real MVVM, data binding, rich text, Markdown, a code editor, and native web views.
- **Server-authoritative multiplayer.** Every change to the world, and every chat message, is a small, validated,
  rate-limited network action. The host checks it, may rewrite it (for example, to filter chat), applies it, and
  broadcasts it. Single-player runs the exact same path locally.
- **Native, trimmed builds.** Releases are compiled ahead of time with NativeAOT for each platform, and a CI pipeline
  builds, smoke-tests, and publishes Windows, Linux, and macOS packages from every tag.
- **Built to be poked at.** A Lua 5.2 console with dozens of game functions, structured logs, a watchdog for hangs, a
  diagnostic report at startup, and a headless dedicated server mode.

If that sounds like your kind of project, the deep dive below goes further, and the
[**README**](./README.md#architecture) has diagrams of each part. Contributions are welcome: see
[**CONTRIBUTING.md**](./CONTRIBUTING.md).

---

## For Developers: The Deep Dive

### Stack

| Layer          | Technology                                                                 |
|----------------|----------------------------------------------------------------------------|
| Engine         | Godot 4.7 (.NET build), Mobile rendering method                            |
| Language       | C# 14 on .NET 10, plus GDScript only inside third-party addons             |
| UI             | Avalonia 12 through a forked Estragonia bridge, MVVM with CommunityToolkit |
| Networking     | Godot's `SceneMultiplayer` over ENet, with UPnP port forwarding            |
| Scripting      | Lua 5.2 via Lua-CSharp                                                     |
| Logging        | Serilog (console, rolling JSON files, and an in-game log view)             |
| Builds         | NativeAOT with full trimming for releases; a JIT build for testing         |
| Tests and lint | xUnit v3, NsDepCop layering rules, Meziantou and .NET analyzers            |
| World addons   | Terrain3D (terrain), Sky3D (sky and day-night), Resonate (audio)           |

### Repository Layout

| Path                 | Contents                                                                             |
|----------------------|--------------------------------------------------------------------------------------|
| `Core/`              | `EnsembleCore`: the pure domain model (players, assets, instances, plots). No Godot. |
| `GdCore/`            | `RefCounted` wrappers that expose Core to Godot code, signals, and the network       |
| `Sessions/`          | `SessionManager`: transports, handshake, peers, RPCs, actions, and snapshots         |
| `Replication/`       | The game's concrete network actions and snapshots                                    |
| `Scripts/`           | Scene-tree nodes: managers and handles for plots, players, assets, and the world     |
| `Tooling/`           | The building tools (place and delete) and their manager                              |
| `Execution/`         | The Lua console's executor and functions                                             |
| `Ui/`                | Views, view models, widgets, and services                                            |
| `Common/`, `Saving/` | Shared utilities (text, audio, input, time) and the save format                      |
| `Autoloading/`       | The `[Autoload]` attribute, policies, and the source-generated registry              |
| `Estragonia/`        | The Avalonia-in-Godot bridge (forked and maintained here)                            |
| `Tests/`             | Unit tests for Core, saving, chat text, geometry, and more                           |
| `.github/`           | Docs, screenshots, and the CI workflows                                              |

Folders named `Impl/` hold implementation details and partial classes. They're hidden from Godot with `.gdignore`, so
Godot only sees the scripts it actually loads.

### Layering

Dependencies flow strictly downward, enforced by NsDepCop at build time:

```text
Ui -> Execution -> Tooling -> Replication -> Scripts -> Sessions
                                                     -> GdCore -> Core
(Common, Saving, and Autoloading sit beneath all of them)
```

Only `Ui` may touch Avalonia. `Core` has no engine dependencies at all, so its rules (quotas, ownership, limits) are
tested like any plain library. Cross-cutting singletons are reached through `Globals` accessors (`GSessionManager`,
`GPlots`, and so on) that throw a descriptive error instead of returning null when something isn't available yet.

### Startup

Godot's autoload list only contains the UI loader. Ensemble's own systems are classes marked
`[Autoload(Order, Scope, FailurePolicy)]`, collected at compile time by a source generator. After the loading screen has
drawn, `Main` creates them in order (logger, core, sessions, player sync, tools, chat, and onward), filtered by whether
it's a regular client or a headless server. Each one decides what a failure means: log and continue, ask the user, or
fail fast.

### World State

State lives in three layers:

- **Core** holds plain data and rules: assets with per-asset limits, plots with occupants, an owner, and an instance
  quota, and instances with transforms and properties.
- **GdCore** wraps Core in Godot `RefCounted` objects with signals, so scene code and the network layer can observe
  changes.
- **Scripts** turns that into nodes: a `PlotHandle` per plot and an `AssetHandle` per instance, with physics,
  highlights, and outlines.

Placement is validated by shared rules (bounds, overlaps, and quotas) that run on the client for previews and on the
host for authority. Overlaps are resolved with oriented bounding boxes and the separating axis theorem, so previews
slide out of collisions and snap flush against neighbors.

### Multiplayer

- **Transports.** `SessionManager` drives one `ISession` at a time: ENet for multiplayer, or an offline transport for
  single-player. Other transports can be plugged in.
- **Handshake.** Every connection is authenticated: versions must match exactly, and password-protected hosts send a
  nonce that clients answer with an HMAC-SHA256 of the password, so it never crosses the network.
- **Identity.** The host assigns every player a GUID and validates display names, replacing inappropriate ones when its
  profanity filter is on.
- **Actions.** Each change is a `readonly record struct` implementing `INetworkAction<TSelf>`: serialize, validate,
  optionally rewrite (host only), and apply. Actions register themselves through module initializers.
- **Rate limiting.** Each peer gets a token bucket of 100 tokens that refills in 5 seconds, and each action costs
  tokens, so costs read as percentages. Excess requests queue briefly, then get a polite rejection.
- **Late joiners** receive snapshots (plots and their instances, chat history, filter state) before any further action
  reaches them, so nothing arrives out of order.
- **Characters** replicate at 20 Hz while moving and once a second while idle, interpolated on other peers.
- **UPnP** runs off the main thread with leased, renewed mappings, and its join code is only ever copied, never shown,
  to protect the host's IP address on streams.

### UI

The UI is an Avalonia application drawn into a Godot `Control`. The Estragonia fork shares Godot's rendering device
(Vulkan, or native Metal on macOS) and forwards input, focus, clipboard, cursors, and windows between the two. On top of
that:

- **MVVM** with source-generated observable properties and commands, dependency injection, and self-registering
  services.
- **Widgets**: in-game windows that drag, resize, maximize, and reopen where you left them.
- **Theming** that follows the OS's light or dark mode and accent color, with colors adjusted in the Oklab color space
  to keep text readable.
- **Rich tools**: a Markdown document viewer (with Mermaid diagrams), a Lua editor with syntax highlighting, a native
  web view, and a pseudo-terminal.

### Chat and Moderation

Chat messages are sanitized (whitespace collapsed, control characters removed, length capped) and checked identically on
every peer. The host's profanity filter matches a compressed word list case-insensitively and through common
substitutions (like "4" for "a"), while avoiding false positives inside ordinary words. Messages can be logged to
per-session text files and read aloud with text-to-speech, which stops when you leave the session.

### Diagnostics and Scripting

- **Lua console**: over 40 functions for the session, world, display, and diagnostics, listed with their parameters by
  `help()`.
- **Logging**: Serilog writes colored console output, rolling JSON files, an in-game log view, and toasts for warnings.
- **Watchdog**: a background thread that fails fast if the main thread stops responding for 20 seconds, instead of
  hanging silently.
- **Startup report**: the OS, hardware, build, and commit are logged at startup, so bug reports carry their context.
- **Headless mode**: `--headless -- --port=7777` runs a dedicated server (every argument has a short form, like
  `--p=7777`).

### Saving

The save format is finished, though the game doesn't use it yet. Creations serialize to a compact binary format (or
JSON), wrapped in an envelope with optional Zstandard or Brotli compression, an optional SHA-256 checksum, and optional
AES-GCM encryption keyed with Argon2id. Readers reject corrupt data and bound every count and string, so a malicious
file can't exhaust memory. Separate layout and content versions leave room to migrate old saves, such as when asset IDs
change.

### Builds and CI

- **Release builds** are NativeAOT-compiled and fully trimmed per platform: Windows and Linux (`x86_64`) and macOS
  (universal). Debug symbols ship separately.
- **A JIT build** bundles Windows and Linux in one download, with every assembly as a separate file, for testing.
- **CI** (GitHub Actions) builds every tag, smoke-tests each package by booting a headless server, and publishes a
  release with checksums. Other workflows publish source archives and track lines of code.
- **Updates**: the game checks GitHub for newer releases on startup, and offers each one once.

### Philosophy

- **Canonical over clever.** Prefer the idiomatic, least-surprising solution, and keep it consistent everywhere.
- **Structure first.** Systems get clean boundaries before they get features, so features don't turn into a tangle.
- **Generic parts, specific callers.** Reusable components stay neutral; whoever uses them decides the specifics.
- **Nothing silent.** Failures are logged or shown, and background tasks never fail unobserved.
- **Tidy by default.** ASCII-only sources, 120-column lines, minimal comments that explain *why*, and zero build
  warnings.

---

Ensemble is made by [**SubParSuperCar**](https://github.com/SubParSuperCar). See the [**README**](./README.md) for
everything else, including credits and licensing.
