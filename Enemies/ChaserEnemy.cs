using Godot;
using System;

public partial class ChaserEnemy : CharacterBody3D
{
	[Export] public float Speed {get;set;} = 3.0f;
	[Export] public float Health {get; set;} = 30.0f;
	
	private Node3D _player;
	private NavigationAgent3D _navAgent;
	
	public override void _Ready(){
		_player = GetTree().GetFirstNodeInGroup("player") as Node3D;
		_navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
	}
	
	public override void _PhysicsProcess(double delta){
		if(_player == null) {
			GD.Print("Player is null");
			return;
			}
		
		_navAgent.TargetPosition = _player.GlobalPosition;
		Vector3 nextPathPosition = _navAgent.GetNextPathPosition();
		
		Vector3 direction = (nextPathPosition - GlobalPosition).Normalized();
		
		Velocity = direction * Speed;
		MoveAndSlide();
	}
	
	public void TakeDamage(float damage){
		GD.Print("Taking damage");
		Health -= damage;
		if(Health <= 0)
			QueueFree();
	}
}
