using Godot;
using System;

public partial class EnemySpawner : Node3D
{
	[Export] private PackedScene _chaserEnemyScene;
	[Export] public float SpawnRadius {get; set;} = 20.0f;
	[Export] public float SpawnInterval { get;set;} = 2.0f;

	// Scaling
	[Export] public float EnemyScalingFactor { get; set; } = 1.6f;
	[Export] public float ScalingIntervalSeconds { get; set; } = 60.0f;

	private Node3D _player;
	private Timer _spawnTimer;
	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	private float _elapsedTime = 0f;

	public override void _Ready(){
		_spawnTimer = GetNode<Timer>("Timer");
		_spawnTimer.WaitTime = SpawnInterval;
		SetProcess(true);
		FindPlayer();
	}

	public override void _Process(double delta)
	{
		_elapsedTime += (float)delta;
	}

	private void FindPlayer()
	{
		_player = GetTree().GetFirstNodeInGroup("player") as Node3D;
		if (_player != null)
		{
			GD.Print($"EnemySpawner found player: {_player.Name}");
			_spawnTimer.Start();
		}
		else
		{
			GD.Print("EnemySpawner: Player not found yet, will retry...");
			GetTree().CreateTimer(0.5f).Timeout += FindPlayer;
		}
	}

	private void _on_timer_timeout(){
		if(_player == null || _chaserEnemyScene == null)
		{
			GD.Print($"EnemySpawner: Cannot spawn - Player: {(_player != null ? "Found" : "NULL")}, Scene: {(_chaserEnemyScene != null ? "Found" : "NULL")}");
			return;
		}

		float randomAngle = _rng.RandfRange(0, Mathf.Pi *2);
		Vector3 direction = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle));

		Vector3 spawnPosition = _player.GlobalPosition + direction * SpawnRadius;

		Node3D newEnemy = _chaserEnemyScene.Instantiate<Node3D>();
		newEnemy.GlobalPosition = spawnPosition;

		// Calculate scaling
		float minutes = _elapsedTime / ScalingIntervalSeconds;
		float scale = Mathf.Pow(EnemyScalingFactor, minutes);

		// Set health and damage if available
		var type = newEnemy.GetType();
		var healthProp = type.GetProperty("Health");
		var damageProp = type.GetProperty("Damage");
		if (healthProp != null)
		{
			float baseHealth = (float)healthProp.GetValue(newEnemy);
			healthProp.SetValue(newEnemy, baseHealth * scale);
			GD.Print($"EnemySpawner: Set enemy health to {baseHealth * scale} (base {baseHealth}, scale {scale:F2})");
		}
		if (damageProp != null)
		{
			float baseDamage = (float)damageProp.GetValue(newEnemy);
			damageProp.SetValue(newEnemy, baseDamage * scale);
			GD.Print($"EnemySpawner: Set enemy damage to {baseDamage * scale} (base {baseDamage}, scale {scale:F2})");
		}

		GetParent().AddChild(newEnemy);

		GD.Print($"EnemySpawner: Spawned enemy at {spawnPosition}, near player at {_player.GlobalPosition}");
	}
}
