# Ensemble

A multiplayer, collaborative sandbox builder: players build creations on plots, spawn them into live physics, and play
together. Godot 4.7 + C# 14 / .NET 10, with an Avalonia 12 UI hosted in Godot through a vendored fork of Estragonia.
It's the spiritual successor to the owner's Roblox game Baja Builders (BB), and inherits its ideas: grid placement with
SAT overlap checks, plot ownership with co-op occupants, host authority, and widget-based UI.

Solo project by elio ("Noodle", they/them). They review every diff, design new UI layouts themselves, and decide on
big features through collaborative planning first.

## The Vision

**Good enough, and uniform.** Not perfection (it stalls), not accretion (it reads like 13 people took turns). The
codebase should read as if one careful author wrote all of it: the same idioms, the same density, the same decisions
everywhere. When adding to it, match what's there; when something's off, fix it everywhere, not just where you are.

- **The ideal design over the patch.** If a fix needs special cases, question the model first. Restructure when the
  model is wrong; don't speculate or abstract ahead of need.
- **Simple for asset authors.** Like BB: an asset is a model, a few parts, and authored Godot joints, not code.
- **Performance is a feature.** Measure render and physics changes; deliberate trade-offs (per-vertex shading) are
  not bugs.
- **Purity over ceremony.** Sparse comments, few but meaningful tests, no defensive boilerplate.

## Architecture

Layers, low to high, enforced by NsDepCop (`config.nsdepcop`):

- **Foundation:** `Common`, `Saving`, `Autoloading`, plus `Core`, the pure C# domain project.
- **`GdCore`** (Godot wrappers around Core) and **`Sessions`** (game-agnostic networking), independent of each other.
  One exception, by the owner's choice: GdCore gives Core the game's clock, `GTimeProvider` from Globals.
- **`Replication`** (network actions and snapshots) and **`Scripts`** (Godot node managers and handles). Scripts
  never uses Replication.
- **Asset scripts** (`EnsembleRoot.BuildAssets`, beside their scenes in `build_assets/`) build on Scripts and below;
  nothing depends on a concrete asset.
- **`Tooling`**, then **`Globals`**, **`Execution`** (the Lua console), and `Main.cs`.
- **`Ui`** (Avalonia), the only layer that may touch Avalonia or Estragonia.

Rules that carry the design:

- **Core is absolute.** Simple data and operations only: no geometry, placement, or tool concepts, ever. Its public
  API doesn't change without the owner's approval. Geometry lives in Scripts (managers, `AssetHandle`, placement).
- **The host is the authority.** Every world change is a validated `INetworkAction`; never trust IDs from payloads.
  The host is never restricted except to protect game integrity.
- **Godot sees only what it must.** Only a Godot class's main file is Godot-visible; partials and plain C# types go
  in a `.gdignore`d `Impl/` folder beside it. `Impl` never appears in a namespace: partials keep their class's, and
  plain types take their folder's (`Scripts/Plots/Impl/Placement/` is `EnsembleRoot.Scripts.Plots.Placement`).
- **One source of truth.** Paths and shared values live in `Common/Constants.cs`; a name has one owner (e.g.,
  `CommandLineSession` owns its flag names); identifiers inside strings use `nameof`.
- **Godot types at Godot boundaries.** GDScript-facing APIs use Godot types; internals use C# collections and .NET
  math (`MathF`, `float.Lerp`) over `Mathf` where equivalent.

## Code Style

The formatter of record is Rider's CleanupCode ("Built-in: Full Cleanup") with the repo's `.editorconfig`: tabs, 120
columns. Builds must have 0 warnings, and InspectCode should be clean on touched files.

- **Comments are rare.** A comment earns its place with a non-obvious *why*: an engine quirk, a threading or
  performance reason, a format or protocol invariant, a unit. Never restate the code or a name. XML docs only on major
  or ambiguous APIs. Single-sentence `//` comments have no trailing period. The owner's `// TODO:`s are backlog: keep
  them.
- **Tests are reserved for make-or-break behavior:** file formats and round trips, determinism, tricky math, parsing,
  and fixed regressions. Not for trivia, and not padded.
- **Terse and canonical.** Expression bodies, one-line private helpers, pattern matching, object initializers; the
  idiomatic, least-surprising form; clean literal values. No informal abbreviations in names (the Lua API's short
  names are the exception); "ID" in prose, `Id` in identifiers.
- **Statement grouping.** Blank lines separate logical groups; related statements clump. Subscribe after syncing the
  current state (`OnX(current); X += OnX;`), unsubscribe in `OnDispose` / `_ExitTree`, mirroring setup.
- **Literals.** `float` as `1f` / `0.5f`, `double` as `1d` / `0.5`; explicit forms in declarations. Prefer base-2
  fractions where they explain themselves (`3 / 16d`); hoist config-like values to named constants.
- **ASCII only**, everywhere (non-ASCII only through escapes); `-` for dashes in prose.
- **Suppressions:** a rule firing once gets a local `// ReSharper disable once X`; several times, one sorted file-level
  block after the usings. Prefer fixing over suppressing.
- **Logging (Serilog):** message templates have no trailing period; exception and UI sentences do.
- **XAML:** one attribute per line from about three attributes on, in the house order (name, layout, content, events,
  tooltip, colors, alignment, size, margin, padding, fonts, visibility).

Deliberate oddities exist (e.g., `ConstructTool.RotateX` rotates around `Vector3.Up`); if something looks wrong but
is long-standing, ask before "fixing" it.

## Settled Conventions

Decided during the 2026-10 refinement campaign; they apply everywhere.

- **Competing forms:** count them codebase-wide and take the majority unless this file decides. Settled: a `foreach`
  whose body is one `if` has no braces; a wrapped condition opens with `if (` alone on its line; a one-statement
  constructor keeps its block body; locals are `var x = new List<T>();` (collection expressions are for fields,
  properties, and target-typed returns); rounding names its `MidpointRounding`; a logged timing is
  `Stopwatch.StartNew()`, then `Stop()` before the log; removing from a dictionary while scanning it is in place,
  unless the loop raises events whose handlers may change it.
- **Design:** an abstraction with one implementation goes, as does a wrapper that only forwards to its owner. Unused
  isn't dead: keep members that complete an API (`IsOpen` beside `Open`, a `bool` saying whether a call acted).
  Parallel collections keyed by one ID become one collection of records; a helper duplicated across modules moves to
  `Common/Utils`, its callers keeping their own policies. A method that changes state through an existing setter
  reuses it.
- **Visibility:** private state is a field; plumbing an owner drives is `internal`; `public` is for asset scripts and
  GDScript.
- **Guards:** one per invariant, where the value is stored; drop guards an upstream invariant already guarantees, but
  keep every guard against a hostile peer. A fallback that hides a failure logs it. An action that passes `Validate`
  never throws in `Apply`. Parsers throw `InvalidDataException` for bad data.
- **Values:** a value a project setting owns is read from `ProjectSettings`; shared numbered resources (`PhysicsLayers`,
  `NetworkChannels`) live in `Constants`; asset length properties are in grid units, read through
  `AssetHandle.GetLengthProperty`.
- **Time:** a per-frame sampler has a `const double XInterval` in seconds and a `_sinceX` accumulator; timers and waits
  take a `TimeSpan` where the API has an overload, and `Task.Delay` passes `GTimeProvider`.
- **Godot:** lifecycle overrides live in the class's main file; `IsHost` means a peer hosts, `IsServer` that this
  machine does; native yes / no dialogs go through `Common/Interop/YesNoDialog`; relaunching passes
  `[.. OS.GetCmdlineArgs(), "--", .. OS.GetCmdlineUserArgs()]`.
- **Logging and Lua:** a plot is `plot {PlotId}`, and `with ID {Id}` is only for registry lookups. A Lua function's
  documentation is its `Functions` entry, not an XML doc.
- **Ui:** Godot reaches view models through `DispatcherService`; `WeakReferenceMessenger` is only for the layers below
  Ui; DI registers `AsSelf`; AXAML imports with `using:`; designer sizes are never added for uniformity.
- **Build:** the SDK's symbols (`DEBUG`, `GODOT_LINUXBSD` / `GODOT_MACOS` / `GODOT_WINDOWS`), custom ones only for
  `EXPORT` and `NATIVE_AOT`; what a running build is comes from `BuildInfo`. XML files indent 2 spaces with `"/>`;
  scripts use tabs and `#!/bin/sh`; CI names each tool by its full path.

## Working Agreements

- **Docs move with the code:** `.github/README.md` (players), `.github/TECHNICAL.md` (developers),
  `.github/ROADMAP.md` (plans). Markdown wraps at 120 columns and links with `./` paths.
- **Verify, don't assume.** Build, run the tests, and when behavior changes, run the game (headless probes or a nested
  KWin session) and read the logs for errors, not just for the expected lines.
- **Commits:** local commits after a finished piece of work are welcome (`git commit -s`, with the co-author
  trailer); pushing is always the owner's call. The owner does bulk renames and moves in Rider.
- **System changes:** least invasive first; explain anything needing `sudo`, its risks, and how to undo it.
- **New UI surfaces** wait for the owner's layout; build and verify the backend first.

## Commands

- Build: `dotnet build Ensemble.slnx`. NativeAOT release settings: `dotnet build EnsembleGame.csproj -c ExportRelease`.
- Test: `dotnet test --solution Ensemble.slnx` (xUnit v3 on Microsoft.Testing.Platform).
- Inspect and clean up: JetBrains' CLI tools (`jb inspectcode`, `jb cleanupcode`); run CleanupCode in a copy of the
  repo and apply its diff, never on Estragonia, addons, or Godot resource files.
