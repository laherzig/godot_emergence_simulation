using System;
using Godot;

namespace Settings;

[Tool]
[GlobalClass]
public partial class Species : Resource
{

	[Export] public float MoveSpeed { get; set; } = 40;
	[Export] public float TurnSpeed { get; set; } = 1;
	[Export] public float SensorAngle { get; set; } = 0.79f;
	[Export] public float SensorDistance { get; set; } = 15; //TODO: Make int
	[Export] public float SensorSize { get; set; } = 4; // TODO: Make int
	[Export] public Color Color { get; set; } = new Color(1, 1, 1, 1);
}