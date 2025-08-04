using Godot;
using System;

public partial class MortarBoulder : Area3D
{
	[Export]
	public float Damage { get; set; } = 25.0f;
	[Export] public float ArcHeight {get; set; } = 5.0f;
	
	private Vector3 _targetPosition {get; set;}
	private Vector3 _startPosition;
	private float _journeyDistance;
	private float _journeyProgress = 0.0f;
	private float _speed = 5.0f;
	
	public void Initialize(Vector3 startPos, Vector3 targetPos)
	{
		GlobalPosition = startPos;
		_startPosition = startPos;
		_targetPosition = targetPos;
		_journeyDistance = _startPosition.DistanceTo(_targetPosition);
	}
	
	public override void _PhysicsProcess(double delta){
		if(_journeyDistance <= 0) return;
		
		_journeyProgress += _speed * (float)delta / _journeyDistance;
		
		if(_journeyProgress >= 1.0f){
			Explode();
			return;
		}
		Vector3 currentPos = _startPosition.Lerp(_targetPosition, _journeyProgress);
		float arc = Mathf.Sin(_journeyProgress * Mathf.Pi) * ArcHeight;
		currentPos.Y += arc;
		GlobalPosition = currentPos;
	}
	
	private void Explode(){
		var explosionArea = GetNode<Area3D>("ExplosionArea");
		var bodies = explosionArea.GetOverlappingBodies();
		
		foreach(var body in bodies){
			if (body.IsInGroup("enemies") && body.HasMethod("TakeDamage")){
				body.Call("TakeDamage", Damage);
			}
		}
		QueueFree();
	}
	
	private void _on_body_entered(Node3D body){
		if(body.IsInGroup("enemies")){
			Explode();
		}
	}
}
