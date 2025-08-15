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
	
	// Player stats for critical hit calculation
	private float _criticalChance = 0.05f;
	private float _criticalDamageMultiplier = 1.5f;
	private float _lifeSteal = 0.0f;
	private float _generalDamage = 0.0f;
	private float _arcaneWaveDamage = 0.0f;
	private Node3D _caster = null;

	public void SetPlayerStats(float criticalChance, float criticalDamageMultiplier, float lifeSteal, float generalDamage, float arcaneWaveDamage, Node3D caster)
	{
		_criticalChance = criticalChance;
		_criticalDamageMultiplier = criticalDamageMultiplier;
		_lifeSteal = lifeSteal;
		_generalDamage = generalDamage;
		_arcaneWaveDamage = arcaneWaveDamage;
		_caster = caster;
	}
	
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
				float finalDamage = CalculateDamage();
				body.Call("TakeDamage", finalDamage);
				_hitEnemies.Add(body);
				
				// Apply life steal if caster exists
				if (_caster != null && _lifeSteal > 0 && _caster.HasMethod("Heal"))
				{
					float healAmount = finalDamage * _lifeSteal;
					_caster.Call("Heal", healAmount);
				}
				
				PierceCount--;
			}
			
			if(PierceCount <= 0)
			{
				QueueFree();
			}
		}
	}
	
	private float CalculateDamage()
	{
		// Apply percentage damage bonuses (convert from percentage to multiplier)
		float damageMultiplier = 1.0f + (_generalDamage / 100.0f) + (_arcaneWaveDamage / 100.0f);
		float baseDamage = Damage * damageMultiplier;
		
		// Roll for critical hit
		var rng = new RandomNumberGenerator();
		bool isCritical = rng.Randf() < _criticalChance;
		
		float finalDamage = baseDamage;
		if (isCritical)
		{
			finalDamage *= _criticalDamageMultiplier;
			GD.Print($"Arcane Wave CRITICAL HIT! Damage: {finalDamage:F1} (base: {baseDamage:F1}, multiplier: {damageMultiplier:F2}x, crit: {_criticalDamageMultiplier:F1}x)");
		}
		
		return finalDamage;
	}
	
	private void _on_timer_timeout(){
		QueueFree();
	}
}
