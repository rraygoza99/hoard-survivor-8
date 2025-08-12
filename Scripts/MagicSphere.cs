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
	
	// Player stats for critical hit calculation
	private float _criticalChance = 0.05f;
	private float _criticalDamageMultiplier = 1.5f;
	private float _lifeSteal = 0.0f;
	private Node3D _caster = null;

	public void SetPlayerStats(float criticalChance, float criticalDamageMultiplier, float lifeSteal, Node3D caster)
	{
		_criticalChance = criticalChance;
		_criticalDamageMultiplier = criticalDamageMultiplier;
		_lifeSteal = lifeSteal;
		_caster = caster;
	}

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
			if (body.IsInGroup("enemies") && body.HasMethod("TakeDamage"))
			{
				float finalDamage = CalculateDamage();
				body.Call("TakeDamage", finalDamage);
				
				// Apply life steal if caster exists
				if (_caster != null && _lifeSteal > 0 && _caster.HasMethod("Heal"))
				{
					float healAmount = finalDamage * _lifeSteal;
					_caster.Call("Heal", healAmount);
				}
			}
			// The spell disappears on hit.
			QueueFree();
		}
	}
	
	private float CalculateDamage()
	{
		// Roll for critical hit
		var rng = new RandomNumberGenerator();
		bool isCritical = rng.Randf() < _criticalChance;
		
		float finalDamage = Damage;
		if (isCritical)
		{
			finalDamage *= _criticalDamageMultiplier;
			GD.Print($"CRITICAL HIT! Damage: {finalDamage:F1} (base: {Damage}, crit multiplier: {_criticalDamageMultiplier:F1}x)");
		}
		
		return finalDamage;
	}

	private void _on_timer_timeout()
	{
		// When the timer runs out, the spell disappears.
		QueueFree();
	}
}
