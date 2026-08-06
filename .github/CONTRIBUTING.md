# Contributing

## How to Set Up / Contribute

1. Download and install **Git** from https://git-scm.com/install/. On many Linux distributions, Git is already installed
   by default.

2. Clone this repository into a directory you'll remember:
   ```bash
   git clone https://github.com/SubParSuperCar/Ensemble.git
   ```
3. Download and install **Godot Mono 4.7.1 or newer** from https://godotengine.org/download/archive/4.7.1-stable/. Be
   sure to download the **.NET (Mono)** version for your operating system and system architecture.

4. Download and install the **.NET SDK 10.0.302 or newer**
   from https://dotnet.microsoft.com/en-us/download/dotnet/10.0/. This is required for C# support in Godot Mono.

5. Download and install **JetBrains Rider 2026.2 or newer** from https://www.jetbrains.com/rider/download/. Rider is
   free for non-commercial use and is the recommended IDE for this project. **Visual Studio Code** is also supported,
   but its C# experience is generally not as comprehensive as Rider's.

6. Open the project in Godot and, optionally, your preferred code editor (JetBrains Rider, Visual Studio Code, etc.),
   then begin developing. If you use JetBrains Rider, it is recommended that you add the Godot executable to your
   system's `PATH` environment variable using one of the following names: `godot`, `godot4`, or `godot-mono`. This allows the
   **PATH Launcher** Run Configuration to automatically locate your Godot installation without requiring manual
   configuration. Alternatively, you can place the executable in `<Project Root>/bin/` using one of the previously listed
   names.
