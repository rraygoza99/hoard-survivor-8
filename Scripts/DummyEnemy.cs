using Godot;
using System;

public partial class DummyEnemy : StaticBody3D
{
	[Export]
	public float Health {get;set;} = 10000.0f;
	
	public void TakeDamage(float damage){
		Health -= damage;
		
		if(Health <= 0){
			GD.Print("Enemy died");
			QueueFree();
		}
	}
}
