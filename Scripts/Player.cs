using Godot;
using System;
using System.Linq;

public partial class Player : CharacterBody3D
{
	[ExportGroup("Player Stats")]
	[Export]
	public float MovementSpeed { get; set; } = 5.0f;

	// XP Gain: A multiplier for experience points gained (e.g., 1.1 for +10%).
	[Export]
	public float XpGainMultiplier { get; set; } = 1.0f;

	// Health: The player's maximum health points.
	[Export]
	public float MaxHealth { get; set; } = 100.0f;

	// Health Regen: Health points regenerated per second.
	[Export]
	public float HealthRegen { get; set; } = 1.0f;

	// Cooldown Reduction: A percentage to reduce ability cooldowns (e.g., 0.1 for 10%).
	[Export(PropertyHint.Range, "0,1")]
	public float CooldownReduction { get; set; } = 0.0f;

	// Critical Chance: The probability of landing a critical hit (e.g., 0.05 for 5%).
	[Export(PropertyHint.Range, "0,1")]
	public float CriticalChance { get; set; } = 0.05f;

	// Critical Damage: The damage multiplier for critical hits (e.g., 1.5 for 150% damage).
	[Export]
	public float CriticalDamageMultiplier { get; set; } = 1.5f;

	// Armor: Reduces incoming damage by a flat amount.
	[Export]
	public float Armor { get; set; } = 0.0f;

	// Life Steal: The percentage of damage dealt that is returned as health (e.g., 0.05 for 5%).
	[Export(PropertyHint.Range, "0,1")]
	public float LifeSteal { get; set; } = 0.0f;
	
	
	[ExportGroup("Combat")]
	[Export]
	private PackedScene _magicSphereScene;
	[Export] private float _baseFireCooldown = 1.0f;
	[Export] private PackedScene _arcaneWaveScene;
	[Export] private float _baseWaveCooldown = 3.0f;
	
	[Export] private PackedScene _mortarBoulderScene;
	[Export] private float _baseMortarCooldown = 5.0f;
	[Export] private float _mortarRange = 15.0f;
	
	
	private Timer _fireTimer;
	private Timer _waveTimer;
	private Timer _mortarTimer;
	private Marker3D _spellSpawnPoint;
	
	public override void _Ready(){
		_fireTimer = GetNode<Timer>("FireTimer");
		_waveTimer = GetNode<Timer>("WaveTimer");
		_mortarTimer = GetNode<Timer>("MortarTimer");
		_spellSpawnPoint = GetNode<Marker3D>("SpellSpawnPoint");
		
		UpdateFireCooldown();
		UpdateWaveCooldown();
		UpdateMortarCooldown();
	}
	
	private void UpdateFireCooldown(){
		_fireTimer.WaitTime = _baseFireCooldown * (1.0f - CooldownReduction);
	}
	private void UpdateWaveCooldown()
	{
		_waveTimer.WaitTime = _baseWaveCooldown * (1.0f - CooldownReduction);
	}
	private void UpdateMortarCooldown()
	{
		_mortarTimer.WaitTime = _baseMortarCooldown * (1.0f - CooldownReduction);
	}
	
	private Node3D FindClosestEnemy(){
		var enemies = GetTree().GetNodesInGroup("enemies").Cast<Node3D>();
		
		Node3D closestEnemy = enemies.OrderBy(enemy=> this.GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition))
			.FirstOrDefault();
		
		return closestEnemy;
	}
	private Node3D FindRandomEnemyInRange()
	{
		// Get all enemies within the mortar's range.
		var enemiesInRange = GetTree().GetNodesInGroup("enemies")
			.Cast<Node3D>()
			.Where(e => this.GlobalPosition.DistanceTo(e.GlobalPosition) <= _mortarRange)
			.ToList();

		if (enemiesInRange.Count > 0)
		{
			var rng = new RandomNumberGenerator();
			return enemiesInRange[rng.RandiRange(0, enemiesInRange.Count - 1)];
		}

		return null; // No enemies in range.
	}
	
	private void LaunchMortar(Vector3 targetPos)
	{
		if (_mortarBoulderScene == null) return;
		MortarBoulder boulder = _mortarBoulderScene.Instantiate<MortarBoulder>();
		GetTree().Root.AddChild(boulder);
		Vector3 spawnPos = _spellSpawnPoint.GlobalPosition;
		boulder.Initialize(spawnPos, targetPos);
	}
	
	private void FireSpell(Node3D target)
	{
		if (_magicSphereScene == null || target == null) return;

		MagicSphere sphere = _magicSphereScene.Instantiate<MagicSphere>();

		GetTree().Root.AddChild(sphere);

		sphere.GlobalTransform = this.GlobalTransform;
		
		sphere.LookAt(target.GlobalPosition);
	}
	private void FireWave(Node3D target)
	{
		if (_arcaneWaveScene == null || target == null) return;
		ArcaneWave wave = _arcaneWaveScene.Instantiate<ArcaneWave>();
		GetTree().Root.AddChild(wave);
		wave.GlobalPosition = this.GlobalPosition;
	
		wave.LookAt(target.GlobalPosition);
	}
	
	private void _on_fire_timer_timeout(){
		Node3D target = FindClosestEnemy();
		if(target!= null)
			FireSpell(target);
			
		UpdateFireCooldown();
	}

	private void _on_wave_timer_timeout()
	{
		Node3D target = FindClosestEnemy();
		if (target != null)
		{
			FireWave(target);
		}
		UpdateWaveCooldown();
	}
	 private void _on_mortar_timer_timeout()
	{
		Vector3 forwardDir = -GlobalTransform.Basis.Z;
		Vector3 targetPos = GlobalPosition + forwardDir * _mortarRange;

		LaunchMortar(targetPos);
		UpdateMortarCooldown();
	}
	
	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		Vector3 direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

		if (direction != Vector3.Zero)
		{
			Velocity = new Vector3(direction.X * MovementSpeed, Velocity.Y, direction.Z * MovementSpeed);
			LookAt(Position + direction);
		}
		else
		{
			Velocity = new Vector3(Mathf.MoveToward(Velocity.X, 0, MovementSpeed), Velocity.Y, Mathf.MoveToward(Velocity.Z, 0, MovementSpeed));
		}
		MoveAndSlide();
	}
}
