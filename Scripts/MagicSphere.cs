using Godot;
using System;

public partial class MagicSphere : Area3D
{
	[Export]
	public float Speed { get; set; } = 10.0f;
	[Export]
	public float Damage { get; set; } = 10.0f;
	[Export]
	public float Lifetime { get; set; } = 3.0f; // in seconds

	public override void _Ready()
	{
		// Find the Timer node and start it.
		var timer = GetNode<Timer>("Timer");
		timer.WaitTime = Lifetime;
		timer.Start();
	}
	public override void _PhysicsProcess(double delta)
	{
		// Move the sphere forward based on its own rotation.
		// We use GlobalTransform.Basis.Z for the forward direction.
		Position -= GlobalTransform.Basis.Z * Speed * (float)delta;
	}

	private void _on_body_entered(Node3D body)
	{
		// Check if the body that entered is part of the "enemies" group.
		if (body.IsInGroup("enemies"))
		{
			// If it's an enemy, try to call its TakeDamage function.
			if (body is DummyEnemy enemy) // More specific check
			{
				enemy.TakeDamage(Damage);
			}
			// The spell disappears on hit.
			QueueFree();
		}
	}

	private void _on_timer_timeout()
	{
		// When the timer runs out, the spell disappears.
		QueueFree();
	}
}
