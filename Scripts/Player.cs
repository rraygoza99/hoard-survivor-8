using Godot;
using System;
using System.Linq;

public partial class Player : CharacterBody3D
{
	[ExportGroup("Player Stats")]
	[Export] public int CurrentXp { get; private set; } = 0;
	[Export] public int XpToNextLevel {get; private set;} =10;
	[Export] public float MaxHealth { get; set; } = 100.0f;
	public float CurrentHealth { get; private set; }
	[Export] float MovementSpeed { get; set; } = 5.0f;
	// XP Gain: A multiplier for experience points gained (e.g., 1.1 for +10%).
	[Export] float XpGainMultiplier { get; set; } = 1.0f;
	// Health Regen: Health points regenerated per second.
	[Export] float HealthRegen { get; set; } = 1.0f;
	// Cooldown Reduction: A percentage to reduce ability cooldowns (e.g., 0.1 for 10%).
	[Export(PropertyHint.Range, "0,1")]	public float CooldownReduction { get; set; } = 0.0f;
	// Critical Chance: The probability of landing a critical hit (e.g., 0.05 for 5%).
	[Export(PropertyHint.Range, "0,1")]	public float CriticalChance { get; set; } = 0.05f;
	// Critical Damage: The damage multiplier for critical hits (e.g., 1.5 for 150% damage).
	[Export] float CriticalDamageMultiplier { get; set; } = 1.5f;
	// Armor: Reduces incoming damage by a flat amount.
	[Export] float Armor { get; set; } = 0.0f;
	// Life Steal: The percentage of damage dealt that is returned as health (e.g., 0.05 for 5%).
	[Export(PropertyHint.Range, "0,1")]	public float LifeSteal { get; set; } = 0.0f;
	[Export] public float Lucky {get; set; } = 0.0f;
	
	[ExportGroup("Combat")]
	[Export] PackedScene _magicSphereScene;
	[Export] private float _baseFireCooldown = 1.0f;
	[Export] private float _sphereRange = 20.0f;
	
	[Export] private PackedScene _arcaneWaveScene;
	[Export] private float _baseWaveCooldown = 3.0f;
	[Export] private float _waveRange = 15.0f;
	
	[Export] private PackedScene _mortarBoulderScene;
	[Export] private float _baseMortarCooldown = 5.0f;
	[Export] private float _mortarRange = 15.0f;
	
	[ExportGroup("UI")]
	[Export] private ProgressBar _healthBar;
	[Export] private TextureProgressBar _xpCircle;
	[Export] private LevelUpScreen _levelUpScreen;
	
	private Timer _fireTimer, _waveTimer, _mortarTimer;
	private Marker3D _spellSpawnPoint;
	private AnimationTree _animationTree;
	private UpgradeManager _upgradeManager;
	
	public override void _Ready(){
		_upgradeManager = GetNode<UpgradeManager>("/root/Node3D/UpgradeManager");
		_animationTree = GetNode<AnimationTree>("AnimationTree");
		_spellSpawnPoint = GetNode<Marker3D>("SpellSpawnPoint");
		_fireTimer = GetNode<Timer>("FireTimer");
		_waveTimer = GetNode<Timer>("WaveTimer");
		_mortarTimer = GetNode<Timer>("MortarTimer");
		
		_levelUpScreen.UpgradeChosen += OnUpgradeChosen;
		CurrentHealth = MaxHealth;
		UpdateHealthBar();
		UpdateXpCircle();
		UpdateFireCooldown();
		UpdateWaveCooldown();
		UpdateMortarCooldown();
		
		_animationTree.Active = true;
		
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
	public void GainXp(int amount){
		CurrentXp += amount;
		if(CurrentXp >= XpToNextLevel)
		{
			LevelUp();
		}
		
		UpdateXpCircle();
	}
	private void UpdateHealthBar()
	{
		if (_healthBar != null)
		{
			_healthBar.Value = (CurrentHealth / MaxHealth) * 100;
		}
	}
	private void UpdateXpCircle()
	{
		if (_xpCircle != null)
		{
			_xpCircle.Value = (float)CurrentXp / XpToNextLevel * 100;
		}
	}
	
	private void LevelUp(){
		GD.Print("LevelUp function called!");
		CurrentXp -= XpToNextLevel;
		XpToNextLevel = (int)(XpToNextLevel *1.5f);
		
		var choices = _upgradeManager.GetUpgradeChoices(Lucky);
		_levelUpScreen.DisplayUpgrades(choices);
	}
	
	private Node3D FindClosestEnemy(float range){
		var enemies = GetTree().GetNodesInGroup("enemies").Cast<Node3D>();
		
		return enemies
			.Cast<Node3D>()
			.Where(e=> this.GlobalPosition.DistanceTo(e.GlobalPosition) <= range)
			.OrderBy(e=> this.GlobalPosition.DistanceSquaredTo(e.GlobalPosition))
			.FirstOrDefault();
	}
	public void TakeDamage(float damage)
	{
		CurrentHealth -= damage;
		if (CurrentHealth < 0) CurrentHealth = 0;
		UpdateHealthBar();

		if (CurrentHealth <= 0)
		{
			GD.Print("Player has been defeated!");
		}
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
	
	private void OnUpgradeChosen(Upgrade chosenUpgrade)
	{
		ApplyUpgrade(chosenUpgrade);
	}
	
	
	private void _on_fire_timer_timeout(){
		Node3D target = FindClosestEnemy(_sphereRange);
		if(target!= null)
			FireSpell(target);
			
		UpdateFireCooldown();
	}
	private void ApplyUpgrade(Upgrade upgrade)
	{
		switch (upgrade.StatToUpgrade)
		{
			case Stat.MaxHealth:
				MaxHealth += upgrade.Value;
				CurrentHealth += upgrade.Value; // Also heal the player
				UpdateHealthBar();
				break;
			case Stat.MovementSpeed:
				MovementSpeed += upgrade.Value;
				break;
			case Stat.XpGain:
				XpGainMultiplier += upgrade.Value;
				break;
			case Stat.CooldownReduction:
				CooldownReduction += upgrade.Value;
				break;
			case Stat.Lucky: // NEW
				Lucky += upgrade.Value;
				break;
		}
		GD.Print($"Applied upgrade: {upgrade.Name}");
	}

	private void _on_wave_timer_timeout()
	{
		Node3D target = FindClosestEnemy(_waveRange);
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
	private void _on_pickup_area_area_entered(Area3D area){
		if (area is XpOrb orb)
		{
			orb.StartSeeking(this);
		}
	}
	private void _on_collection_area_area_entered(Area3D area)
	{
		if(area is XpOrb orb){
			GainXp(orb.XpAmount);
			orb.QueueFree();
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		Vector3 direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

		if (direction != Vector3.Zero)
		{
			Velocity = new Vector3(direction.X * MovementSpeed, Velocity.Y, direction.Z * MovementSpeed);
			LookAt(Position + direction);
			
			_animationTree.Set("parameters/conditions/Run", true);
			_animationTree.Set("parameters/conditions/Idle", false);
		}
		else
		{
			Velocity = new Vector3(Mathf.MoveToward(Velocity.X, 0, MovementSpeed), Velocity.Y, Mathf.MoveToward(Velocity.Z, 0, MovementSpeed));
			_animationTree.Set("parameters/conditions/Run", false);
			_animationTree.Set("parameters/conditions/Idle", true);
		}
		MoveAndSlide();
	}
}
