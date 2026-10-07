# Contributing

## How to Set Up and Contribute

1. Download and install [Git](https://git-scm.com/install/). Git is already installed by default on many Linux
   distributions.

2. Clone this repository into a directory you will remember:
   ```bash
   git clone https://github.com/SubParSuperCar/Ensemble.git
   ```

3. Download and install [Godot Mono 4.7.2 or newer](https://godotengine.org/download/archive/4.7.2-stable/). Be sure
   to download the **.NET (Mono)** version for your operating system and system architecture.

4. Download and install the [.NET SDK 10.0.100 or newer](https://dotnet.microsoft.com/en-us/download/dotnet/10.0/).
   The .NET SDK is required to build and run the project's C# code.

5. (Optional) Download and install [JetBrains Rider 2025.3 or newer](https://www.jetbrains.com/rider/download/).
   Rider is the recommended IDE for this project. See JetBrains' current licensing terms for information about its
   available licenses. **Visual Studio Code** is also supported, but its C# tooling is generally less comprehensive than
   Rider's. **Visual Studio 2026** can also be used, but only on Windows.

6. Open the project in Godot to import and initialize it. Then open the project in your preferred code editor (JetBrains
   Rider, Visual Studio Code, etc.) and begin developing.

   ### Rider PATH Configuration

   If you use JetBrains Rider, it is recommended that you add the Godot executable to your system's `PATH` using one of
   the following names: `godot`, `godot4`, or `godot-mono`. This allows the **PATH Launcher** run configuration to
   locate your Godot installation automatically without additional configuration.

   Alternatively, you can place the executable in the project's `bin/` directory using one of the previously listed
   names. Creating the directory may be required.

## Guidelines

- **Keep changes focused.** One feature or fix per pull request, with a short description of what changed and why.
- **Match the existing style.** Run Rider's **Code Cleanup** on changed files (formatting is defined by the
  repository's `.editorconfig`), keep lines within 120 columns, and use ASCII only. Comments are kept to a minimum:
  explain why, not what.
- **Respect the module boundaries.** Layering is enforced at build time by NsDepCop (`config.nsdepcop`); see
  [**Architecture**](./README.md#architecture). `Core` stays plain C# with no Godot dependencies.
- **Build cleanly and test.** The solution must build without warnings, and `dotnet test --solution Ensemble.slnx`
  must pass. Add tests for real behavior and regressions rather than trivial operations.
- **License your contributions.** By contributing, you agree that your code is licensed under GPL-3.0-or-later and your
  non-code assets under CC BY-NC-SA 4.0, as described in [**LICENSE.md**](../LICENSE.md).
