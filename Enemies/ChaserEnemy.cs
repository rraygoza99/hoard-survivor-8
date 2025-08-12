using Godot;
using System;
using Godot.Collections;

public partial class ChaserEnemy : CharacterBody3D
{
	[Export] public float Speed {get;set;} = 3.0f;
	[Export] public float Health {get; set;} = 30.0f;
	[Export] public float Damage {get; set;} = 10.0f; // Damage dealt to player
	[Export] public float AttackCooldown {get; set;} = 1.0f; // Time between attacks
	
	[ExportGroup("Loot")]
	[Export] private PackedScene _xpOrbScene;
	[Export] private int _xpAmount = 10;
	[Export] private float _mergeRadius = 1.5f;
	
	private Node3D _player;
	private NavigationAgent3D _navAgent;
	private AnimationTree _animationTree;
	private float _lastAttackTime = 0.0f;
	
	public override void _Ready(){
		_player = GetTree().GetFirstNodeInGroup("player") as Node3D;
		_navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		_animationTree = GetNode<AnimationTree>("AnimationTree");
	}
	
	public override void _PhysicsProcess(double delta){
		if(_player == null) {
			GD.Print("Player is null");
			return;
		}
		
		_lastAttackTime += (float)delta;
		
		_navAgent.TargetPosition = _player.GlobalPosition;
		Vector3 nextPathPosition = _navAgent.GetNextPathPosition();
		
		Vector3 direction = (nextPathPosition - GlobalPosition).Normalized();
		
		Velocity = direction * Speed;
		if(direction != Vector3.Zero)
		{
			LookAt(GlobalPosition + direction);
		}
		MoveAndSlide();
		
		// Check for collision with player
		CheckPlayerCollision();
	}
	
	private void CheckPlayerCollision()
	{
		if (_player == null || _lastAttackTime < AttackCooldown) return;
		
		// Check if we're close enough to the player to deal damage
		float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);
		float damageRange = 1.5f; // Close enough to deal damage
		
		if (distanceToPlayer <= damageRange)
		{
			// Try to deal damage to the player
			if (_player.HasMethod("TakeDamage"))
			{
				_player.Call("TakeDamage", Damage);
				_lastAttackTime = 0.0f; // Reset attack cooldown
				GD.Print($"Enemy dealt {Damage} damage to player");
			}
		}
	}
	private void DropXpOrb()
	{
		if (_xpOrbScene == null) return;

		var spaceState = GetWorld3D().DirectSpaceState;
		var query = new PhysicsShapeQueryParameters3D();
		var sphereShape = new SphereShape3D { Radius = _mergeRadius };
		
		query.Transform = new Transform3D(Basis.Identity, GlobalPosition);
		query.Shape = sphereShape;
		query.CollideWithAreas = true;

		var nearbyObjects = spaceState.IntersectShape(query);
		foreach(Dictionary obj in nearbyObjects){
			if(obj["collider"].As<Node>() is XpOrb existingOrb)
			{
				existingOrb.Combine(_xpAmount);
				return;
			}
		}
		XpOrb newOrb = _xpOrbScene.Instantiate<XpOrb>();
		newOrb.SetInitialValue(_xpAmount);
		GetParent().AddChild(newOrb); // Add to the main scene
		newOrb.GlobalPosition = this.GlobalPosition;
	}
	public void TakeDamage(float damage){
		Health -= damage;
		if(Health <= 0){
			DropXpOrb();
			QueueFree();
		}
	}
}
