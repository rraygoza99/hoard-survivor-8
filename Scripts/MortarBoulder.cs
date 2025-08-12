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
				float finalDamage = CalculateDamage();
				body.Call("TakeDamage", finalDamage);
				
				// Apply life steal if caster exists
				if (_caster != null && _lifeSteal > 0 && _caster.HasMethod("Heal"))
				{
					float healAmount = finalDamage * _lifeSteal;
					_caster.Call("Heal", healAmount);
				}
			}
		}
		QueueFree();
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
			GD.Print($"CRITICAL HIT! Mortar damage: {finalDamage:F1} (base: {Damage}, crit multiplier: {_criticalDamageMultiplier:F1}x)");
		}
		
		return finalDamage;
	}
	
	private void _on_body_entered(Node3D body){
		if(body.IsInGroup("enemies")){
			Explode();
		}
	}
}
