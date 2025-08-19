using Godot;
using System;
using System.Collections.Generic;

public partial class GameData : Node
{
	public static GameData Instance { get; private set; }
	
	// Multiplayer lobby data removed. Keep placeholder for future expansion if needed.
	
	public override void _Ready()
	{
		GD.Print("GameData _Ready called");
		if (Instance == null)
		{
			Instance = this;
			GD.Print("GameData Instance set successfully");
			// Don't destroy on scene change
			ProcessMode = ProcessModeEnum.Always;
		}
		else
		{
			GD.Print("GameData Instance already exists, destroying duplicate");
			QueueFree();
		}
	}
	
	// Multiplayer helpers removed.
}
