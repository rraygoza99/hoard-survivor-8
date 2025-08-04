using Godot;
using System;
using System.Collections.Generic;

public partial class ArcaneWave : Node3D
{
	[Export] public float Speed { get; set;} = 8.0f;
	[Export] public float Damage { get; set; } = 15.0f;
	[Export] public float LifeTime { get; set; } = 2.0f;
	[Export] public int PierceCount {get; set; } = 3;
	
	private List<Node3D> _hitEnemies = new List<Node3D>();
	
	public override void _Ready(){
		GetNode<Timer>("Timer").WaitTime = LifeTime;
		GetNode<Timer>("Timer").Start();
	}
	
	public override void _PhysicsProcess(double delta){
		Position -= GlobalTransform.Basis.Z * Speed * (float) delta;
	}
	
	private void _on_body_entered(Node3D body){
		if(PierceCount <= 0 || _hitEnemies.Contains(body)){
			return;
		}
		
		if(body.IsInGroup("enemies")){
			if(body.HasMethod("TakeDamage")){
				body.Call("TakeDamage", Damage);
				_hitEnemies.Add(body);
				
				PierceCount--;
			}
			
			if(PierceCount <= 0)
			{
				QueueFree();
			}
		}
	}
	
	private void _on_timer_timeout(){
		QueueFree();
	}
}
