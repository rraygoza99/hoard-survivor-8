using Godot;
using System;

public partial class XpOrb : Area3D
{
	public int XpAmount { get; set; } = 1;
	[Export] private float _speed = 15.0f;
	
	private Node3D _targetPlayer;
	private bool _isSeeking = false;
	
	public void Combine(int amount)
	{
		XpAmount += amount;
	}
	public void SetInitialValue(int amount)
	{
		XpAmount = amount;
	}
	public override void _PhysicsProcess(double delta)
	{
		if (!_isSeeking || _targetPlayer == null)
		{
			return;
		}
		Vector3 direction = (_targetPlayer.GlobalPosition - GlobalPosition).Normalized();
		GlobalPosition += direction * _speed * (float)delta;
	}
	public void StartSeeking(Node3D player)
	{
		_targetPlayer = player;
		_isSeeking = true;
	}
}
