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
		_player = GetTree().GetFirstNodeInGroup("player") as Node3D;
		_spawnTimer = GetNode<Timer>("Timer");
		
		_spawnTimer.WaitTime = SpawnInterval;
		_spawnTimer.Start();
	}
	
	private void _on_timer_timeout(){
		if(_player == null || _chaserEnemyScene == null)
			return;
		float randomAngle = _rng.RandfRange(0, Mathf.Pi *2);
		Vector3 direction = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle));
		
		Vector3 spawnPosition = _player.GlobalPosition + direction * SpawnRadius;
		
		Node3D newEnemy = _chaserEnemyScene.Instantiate<Node3D>();
		newEnemy.GlobalPosition = spawnPosition;
		
		GetParent().AddChild(newEnemy);
	}
}
