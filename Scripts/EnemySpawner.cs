using Godot;
using System;

public partial class EnemySpawner : Node3D
{
	[Export] private PackedScene _chaserEnemyScene;
	[Export] public float SpawnRadius {get; set;} = 20.0f;
	[Export] public float SpawnInterval { get;set;} = 2.0f;
	
	private Node3D _player;
	private Timer _spawnTimer;
	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	
	public override void _Ready(){
		_spawnTimer = GetNode<Timer>("Timer");
		_spawnTimer.WaitTime = SpawnInterval;
		
		// Don't start the timer immediately, wait for player to be ready
		// We'll start it when we find a player
		FindPlayer();
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
			// Retry finding the player after a short delay
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
		
		GetParent().AddChild(newEnemy);
		
		GD.Print($"EnemySpawner: Spawned enemy at {spawnPosition}, near player at {_player.GlobalPosition}");
	}
}
