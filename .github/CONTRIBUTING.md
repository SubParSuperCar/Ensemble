# Contributing

## How to Set Up and Contribute

1. Download and install **Git** from https://git-scm.com/install/. Git is already installed by default on many Linux
   distributions.

2. Clone this repository into a directory you will remember:
   ```bash
   git clone https://github.com/SubParSuperCar/Ensemble.git
   ```

3. Download and install **Godot Mono 4.7.2 or newer** from https://godotengine.org/download/archive/4.7.2-stable/. Be
   sure to download the **.NET (Mono)** version for your operating system and system architecture.

4. Download and install the **.NET SDK 10.0.100 or newer**
   from https://dotnet.microsoft.com/en-us/download/dotnet/10.0/. The .NET SDK is required to build and run the
   project's C# code.

5. Download and install **JetBrains Rider 2025.3 or newer** from https://www.jetbrains.com/rider/download/. Rider is
   free for non-commercial use (as of the last update of this document) and is the recommended IDE for this project.
   **Visual Studio Code** is also supported, but its C# tooling is generally less comprehensive than Rider's. **Visual
   Studio 2026** can also be used, but only on Windows.

6. Open the project in Godot and, optionally, in your preferred code editor (JetBrains Rider, Visual Studio Code, etc.),
   then begin developing.

   If you use JetBrains Rider, it is recommended that you add the Godot executable to your system's `PATH` using one of
   the following names: `godot`, `godot4`, or `godot-mono`. This allows the **PATH Launcher** run configuration to
   locate your Godot installation automatically without additional configuration.

   Alternatively, you can place the executable in `/bin/` using one of the previously listed names. Creating the
   directory may be required.
