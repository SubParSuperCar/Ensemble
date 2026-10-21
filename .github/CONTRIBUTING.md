# Contributing

Thanks for your interest in Ensemble. This page covers setting up a development environment, the everyday workflow, and
the conventions the codebase follows. For a map of the code, see [**TECHNICAL.md**](/.github/TECHNICAL.md).

## How to Set Up and Contribute

1. Download and install [Git](https://git-scm.com/install/). Git is already installed by default on many Linux
   distributions.

2. Clone this repository into a directory you will remember:
   ```bash
   git clone https://github.com/SubParSuperCar/Ensemble.git
   ```

3. Download and install [Godot Mono 4.7.2](https://godotengine.org/download/archive/4.7.2-stable/). Be sure to download
   the **.NET (Mono)** version for your operating system and architecture.

4. Download and install the [.NET SDK 10.0.100 or newer](https://dotnet.microsoft.com/en-us/download/dotnet/10.0/). The
   .NET SDK is required to build and run the project's C# code.

5. (Optional) Download and install [JetBrains Rider 2025.3 or newer](https://www.jetbrains.com/rider/download/). Rider
   is the recommended IDE for this project. See JetBrains' current licensing terms for information about its available
   licenses. **Visual Studio Code** is also supported, but its C# tooling is generally less comprehensive than Rider's.
   **Visual Studio 2026** can also be used, but only on Windows.

6. Open the project in Godot to import and initialize it. Then open the project in your preferred code editor (JetBrains
   Rider, Visual Studio Code, etc.) and begin developing.

   ### Rider PATH Configuration

   If you use JetBrains Rider, it is recommended that you add the Godot executable to your system's `PATH` under one of
   the following names: `godot`, `godot4`, or `godot-mono` (with `.exe` on Windows). This allows the **PATH Launcher**
   run configurations to locate your Godot installation automatically, without additional configuration.

   Alternatively, you can place the executable in the project's `bin/` directory under one of the previously listed
   names. The directory is created by the first build.

   To try multiplayer locally, the **Host + Client (Side by Side)** run configuration builds once, then hosts a session
   in one window and joins it from another, each filling half of a 2560-pixel-wide screen (adjust the size at the top
   of `Properties/side-by-side.sh`, or `.cmd` on Windows).

## Building and Testing

- **Build:** `dotnet build Ensemble.slnx` builds every project. The game's `Release` and `ExportRelease`
  configurations enable NativeAOT, whose analyzers flag trimming problems, so also check
  `dotnet build EnsembleGame.csproj -c ExportRelease` before submitting.
- **Test:** `dotnet test --solution Ensemble.slnx` runs the xUnit v3 tests. The repository uses
  Microsoft.Testing.Platform, so a bare `dotnet test <path>` doesn't work.
- **Try it:** the in-game console (`` ` `` or `F9`) runs Lua; `help()` lists every function, and functions such as
  `loc_add_rand_insts(plot_id, count)`, `set_plot_spawned(true)`, and `set_net_sim(...)` help exercise building, live
  physics, and poor connections without other players.

## Guidelines

- **Keep changes focused.** One feature or fix per pull request, with a short description of what changed and why.
- **Match the existing style.** Run Rider's **Code Cleanup** on changed files (formatting is defined by the repository's
  `.editorconfig`), keep lines within 120 columns, and use ASCII only (escape other characters, e.g., `\u2026`).
  Comments are kept to a minimum: explain why, not what.
- **Respect the module boundaries.** Layering is enforced at build time by NsDepCop (`config.nsdepcop`); see
  [**Module Layering**](/.github/TECHNICAL.md#module-layering). `Core` stays plain C# with no Godot dependencies, and
  only `Ui` may use Avalonia.
- **Keep Godot's view tidy.** Only the main file of a Godot class (a `Node`, `Resource`, or `RefCounted` script) belongs
  where Godot can see it, with its committed `.uid` file. Partial classes and plain C# types go in the nearest `Impl/`
  folder, which a `.gdignore` file hides from Godot; see [**What Godot Sees**](/.github/TECHNICAL.md#what-godot-sees).
- **Mind performance.** Ensemble aims for very high frame rates: measure rendering and per-frame changes, avoid reading
  many Godot objects through interop every frame, and keep cheap trade-offs (such as per-vertex shading on assets).
- **Keep asset IDs stable.** Saves store each instance's `AssetId`, so once an asset ships, its ID must never change or
  be reused; give new assets new IDs. Asset scenes live in `build_assets/`, with any asset script beside its scene.
- **Build cleanly and test.** The solution must build without warnings, and the tests must pass. Add tests for real
  behavior and regressions rather than trivial operations.
- **License your contributions.** By contributing, you agree that your code is licensed under GPL-3.0-or-later and your
  non-code assets under CC BY-NC-SA 4.0, as described in [**LICENSE.md**](/LICENSE.md).
