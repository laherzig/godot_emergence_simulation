using Godot;
using System;
using Simulation;

namespace Settings;

// public enum SpawnType
// {
// 	Circle,
// 	Rectangle
// }

[Tool]
[GlobalClass]
public partial class Settings : Resource
{
	[Export] public Vector2I MapSize { get; set; } = new Vector2I(1920, 1080);
	[Export] public int NumAgents { get; set; } = 65536;
	[Export] public float DecaySpeed { get; set; } = 0.12f;
	[Export] public float DiffuseSpeed { get; set; } = 10;
	[Export] public float TrailWeight { get; set; } = 5;
	[Export] public int AgentBehavior { get; set; } = 0;

	[Export]
	public Species[] Species
	{
		get
		{
			return _species;
		}
		set
		{
			if (value.Length > 4)
			{
				GD.PushWarning("The maximum number of species is 4.");
				return;
			}
			_species = value;
			for (int i = 0; i < _species.Length; i++)
			{
				if (_species[i] == null)
				{
					_species[i] = new Species();
				}
			}
		}
	}
	private Species[] _species = new Species[1] { new() };
}