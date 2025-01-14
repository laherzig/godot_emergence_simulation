# Godot Emergence Simulator

This is a simple simulation of swarm behavior in Godot.

## Installation

### Install Dependencies

-   [Godot 4.3 (.NET Version)](https://godotengine.org/download/archive/4.3-stable/)
-   [.NET SDK 6.0 or later](https://dotnet.microsoft.com/en-us/download)

#### Scoop

```shell
# Godot
scoop bucket add extras
scoop install godot-mono@4.3

# .NET
# newest version
scoop install dotnet-sdk
# Newest lts versions
scoop bucket add versions
scoop install dotnet-sdk-lts
# Specific version
scoop install dotnet6-sdk
```

### Repository

After installing the dependencies, simply clone the repository

```shell
git clone https://github.com/laherzig/godot_emergence_simulation.git
```

## Usage

To run the simulation, open the `project.godot` file with Godot. The simulation can be run by pressing the "Run Project" button in the top right corner of the Godot editor, or simply press F5.

Parameters can be modified in the `SlimeSimulation` scene, which can be found at `res://scenes/SlimeSimulation.tscn`. You can change the values of the Settings resource in the Inspector.

## Contribute

The code is quite a mess, but if you want to contribute, feel free to do so and open a pull request.
If there are any questions or you want to request a feature, open an issue.
