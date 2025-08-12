using Godot;
using System;

public partial class LittleShroomEnemy : ChaserEnemy
{
	public override void _Ready()
	{
		base._Ready();

		Speed = 2.0f;  
		Health = 5.0f;
	}
}
