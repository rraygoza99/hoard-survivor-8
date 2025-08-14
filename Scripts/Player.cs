using Godot;
using System;
using System.Linq;

public partial class Player : CharacterBody3D
{
	[ExportGroup("Player Stats")]
	[Export] public int CurrentXp { get; private set; } = 0;
	[Export] public int XpToNextLevel {get; private set;} =100;
	[Export] public float MaxHealth { get; set; } = 100.0f;
	public float CurrentHealth { get; set; }
	[Export] float MovementSpeed { get; set; } = 5.0f;
	
	// Multiplayer properties
	private bool _isLocalPlayer = false;
	private string _playerName = "";
	
	// Network synchronization
	private Vector3 _networkPosition = Vector3.Zero;
	private Vector3 _networkRotation = Vector3.Zero;
	private bool _networkIsMoving = false;
	private float _lastPositionUpdate = 0.0f;
	private const float POSITION_UPDATE_INTERVAL = 0.1f; // Update every 100ms
	
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
	// Use Range so either ProgressBar or TextureProgressBar can be assigned
	[Export] private TextureProgressBar _healthBar;
	[Export] private TextureProgressBar _xpBar;
	[Export] private Label _healthLabel;
	[Export] private TextureProgressBar _xpCircle;
	[Export] private LevelUpScreen _levelUpScreen;
	
	private Timer _fireTimer, _waveTimer, _mortarTimer;
	private Marker3D _spellSpawnPoint;
	private AnimationTree _animationTree;
	private UpgradeManager _upgradeManager;
	
	public override void _Ready(){
		GD.Print($"Player _Ready() called - MaxHealth: {MaxHealth}");
		
		_upgradeManager = GetNode<UpgradeManager>("/root/Node3D/UpgradeManager");
		_animationTree = GetNode<AnimationTree>("AnimationTree");
		_spellSpawnPoint = GetNode<Marker3D>("SpellSpawnPoint");
		_fireTimer = GetNode<Timer>("FireTimer");
		_waveTimer = GetNode<Timer>("WaveTimer");
		_mortarTimer = GetNode<Timer>("MortarTimer");
		
		// Only connect if _levelUpScreen is already set (for players spawned in editor)
		if (_levelUpScreen != null)
		{
			_levelUpScreen.UpgradeChosen += OnUpgradeChosen;
		}
		
		CurrentHealth = MaxHealth;
		
		UpdateHealthBar();
		UpdateXpCircle();
		UpdateFireCooldown();
		UpdateWaveCooldown();
		UpdateMortarCooldown();
		
		_animationTree.Active = true;
	}
	
	private void UpdateFireCooldown(){
		float newCooldown = _baseFireCooldown * (1.0f - CooldownReduction);
		_fireTimer.WaitTime = newCooldown;
		GD.Print($"Fire cooldown updated: {newCooldown:F2}s (base: {_baseFireCooldown}s, reduction: {CooldownReduction:P1})");
	}
	private void UpdateWaveCooldown()
	{
		float newCooldown = _baseWaveCooldown * (1.0f - CooldownReduction);
		_waveTimer.WaitTime = newCooldown;
		GD.Print($"Wave cooldown updated: {newCooldown:F2}s (base: {_baseWaveCooldown}s, reduction: {CooldownReduction:P1})");
	}
	private void UpdateMortarCooldown()
	{
		float newCooldown = _baseMortarCooldown * (1.0f - CooldownReduction);
		_mortarTimer.WaitTime = newCooldown;
		GD.Print($"Mortar cooldown updated: {newCooldown:F2}s (base: {_baseMortarCooldown}s, reduction: {CooldownReduction:P1})");
	}
	public void GainXp(int amount){
		int modifiedAmount = Mathf.RoundToInt(amount * XpGainMultiplier);
		GD.Print($"Player {_playerName} gaining {modifiedAmount} XP (base: {amount}, multiplier: {XpGainMultiplier:F2}x, current: {CurrentXp}, to next level: {XpToNextLevel})");
		CurrentXp += modifiedAmount;
		if(CurrentXp >= XpToNextLevel)
		{
			LevelUp();
		}
		
		UpdateXpCircle();
	}
	private void UpdateHealthBar()
	{
		// Only update UI for local player
		if (!_isLocalPlayer) return;
		
		GD.Print($"UpdateHealthBar called - CurrentHealth: {CurrentHealth}, MaxHealth: {MaxHealth}, _healthBar: {(_healthBar != null ? "Connected" : "NULL")}");
		
		float percent = CurrentHealth / MaxHealth;
		if (_healthBar != null)
		{
			if (_healthBar is SmoothBar smooth)
			{
				smooth.SetTargetPercent(percent);
				GD.Print($"Health smooth target set to: {percent * 100f}%");
			}
			else
			{
				_healthBar.Value = percent * 100f;
				GD.Print($"Health bar updated to: {_healthBar.Value}%");
			}
		}
		if (_healthLabel != null)
		{
			_healthLabel.Text = $"{Mathf.Round(CurrentHealth)} / {MaxHealth}";
			GD.Print($"Health label updated to: {_healthLabel.Text}");
		}
		else
		{
			GD.Print("No health bar reference - cannot update UI label");
		}
	}
	private void UpdateXpCircle()
	{
		if (!_isLocalPlayer) return;
		float pct = (float)CurrentXp / XpToNextLevel * 100f;
		if (_xpCircle != null)
			_xpCircle.Value = pct;
		if (_xpBar != null)
		{
			if (_xpBar is SmoothBar smooth)
			{
				smooth.SetTargetPercent(pct / 100f);
			}
			else
			{
				_xpBar.Value = pct;
			}
		}
	}
	
	private void LevelUp(){
		GD.Print("LevelUp function called!");
		CurrentXp -= XpToNextLevel;
		XpToNextLevel = (int)(XpToNextLevel *1.5f);
		
		var choices = _upgradeManager.GetUpgradeChoices(Lucky);
		
		GD.Print($"Level up screen reference: {(_levelUpScreen != null ? "Connected" : "NULL")}");
		if (_levelUpScreen != null)
		{
			GD.Print($"Displaying {choices.Count} upgrade choices");
			_levelUpScreen.DisplayUpgrades(choices);
		}
		else
		{
			GD.PrintErr("Level up screen is NULL - cannot display upgrades!");
		}
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
		// Apply armor reduction
		float reducedDamage = Mathf.Max(0, damage - Armor);
		CurrentHealth -= reducedDamage;
		if (CurrentHealth < 0) CurrentHealth = 0;
		UpdateHealthBar();

		if (CurrentHealth <= 0)
		{
			GD.Print($"Player {_playerName} has been defeated!");
		}
	}
	
	public void Heal(float amount)
	{
		CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
		UpdateHealthBar();
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
		
		// Set player stats for critical hits and life steal
		boulder.SetPlayerStats(CriticalChance, CriticalDamageMultiplier, LifeSteal, this);
		
		boulder.Initialize(spawnPos, targetPos);
	}
	
	private void FireSpell(Node3D target)
	{
		if (_magicSphereScene == null || target == null) return;

		MagicSphere sphere = _magicSphereScene.Instantiate<MagicSphere>();

		GetTree().Root.AddChild(sphere);

		sphere.GlobalTransform = this.GlobalTransform;
		
		// Set player stats for critical hits and life steal
		sphere.SetPlayerStats(CriticalChance, CriticalDamageMultiplier, LifeSteal, this);
		
		// Calculate direction only on the horizontal plane (ignore Y difference)
		Vector3 targetDirection = new Vector3(target.GlobalPosition.X, this.GlobalPosition.Y, target.GlobalPosition.Z);
		sphere.LookAt(targetDirection);
	}
	private void FireWave(Node3D target)
	{
		if (_arcaneWaveScene == null || target == null) return;

		ArcaneWave wave = _arcaneWaveScene.Instantiate<ArcaneWave>();

		GetTree().Root.AddChild(wave);

		wave.GlobalTransform = this.GlobalTransform;
		
		// Set player stats for critical hits and life steal
		wave.SetPlayerStats(CriticalChance, CriticalDamageMultiplier, LifeSteal, this);
		
		// Calculate direction only on the horizontal plane (ignore Y difference) - same as magic sphere
		Vector3 targetDirection = new Vector3(target.GlobalPosition.X, this.GlobalPosition.Y, target.GlobalPosition.Z);
		wave.LookAt(targetDirection);
	}	public void ConnectLevelUpScreen()
	{
		if (_levelUpScreen != null)
		{
			_levelUpScreen.UpgradeChosen += OnUpgradeChosen;
		}
		else
		{
			GD.PrintErr("Cannot connect level up screen - it's null");
		}
	}
	
	private void OnUpgradeChosen(Upgrade chosenUpgrade)
	{
		GD.Print($"OnUpgradeChosen called for player {_playerName} with upgrade: {chosenUpgrade.Name}");
		ApplyUpgrade(chosenUpgrade);
	}
	
	
	private void _on_fire_timer_timeout(){
		
		// Only allow local players to cast spells (instead of using Godot's multiplayer authority)
		if(_isLocalPlayer){
			Node3D target = FindClosestEnemy(_sphereRange);
			if(target!= null)
			{
				FireSpell(target);
			}
			
			UpdateFireCooldown();
		}
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
				XpGainMultiplier *= (1.0f + upgrade.Value);
				break;
			case Stat.CooldownReduction:
				CooldownReduction += upgrade.Value;
				// Update all cooldown timers when cooldown reduction changes
				UpdateFireCooldown();
				UpdateWaveCooldown();
				UpdateMortarCooldown();
				break;
			case Stat.Lucky: // NEW
				Lucky += upgrade.Value;
				break;
			case Stat.LifeSteal:
				LifeSteal += upgrade.Value;
				break;
			case Stat.CriticalChance:
				CriticalChance += upgrade.Value;
				break;
			case Stat.CriticalDamage:
				CriticalDamageMultiplier += upgrade.Value;
				break;
			case Stat.Armor:
				Armor += upgrade.Value;
				break;
		}
		GD.Print($"Applied upgrade: {upgrade.Name} - {upgrade.StatToUpgrade}: +{upgrade.Value}");
	}

	private void _on_wave_timer_timeout()
	{
		// Only allow local players to cast spells (instead of using Godot's multiplayer authority)
		if(_isLocalPlayer){
			Node3D target = FindClosestEnemy(_waveRange);
			if (target != null)
			{
				FireWave(target);
			}
			UpdateWaveCooldown();
		}
	}
	 private void _on_mortar_timer_timeout()
	{
		// Only allow local players to cast spells (instead of using Godot's multiplayer authority)
		if(_isLocalPlayer){
			Vector3 forwardDir = -GlobalTransform.Basis.Z;
			Vector3 targetPos = GlobalPosition + forwardDir * _mortarRange;

			LaunchMortar(targetPos);
			UpdateMortarCooldown();
		}
	}
	private void _on_pickup_area_area_entered(Area3D area){
		// Only local player should interact with pickups
		if (!_isLocalPlayer) return;
		
		if (area is XpOrb orb)
		{
			orb.StartSeeking(this);
		}
	}
	private void _on_collection_area_area_entered(Area3D area)
	{
		// Only local player should collect XP
		if (!_isLocalPlayer) return;
		
		if(area is XpOrb orb){
			GD.Print($"Local player {_playerName} collecting {orb.XpAmount} XP");
			GainXp(orb.XpAmount);
			orb.QueueFree();
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		// Only allow input if this is the local player
		if(_isLocalPlayer){
			Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
			Vector3 direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

			bool isMoving = direction != Vector3.Zero;
			
			if (isMoving)
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
			
			// Update position via Steam lobby data (throttled)
			_lastPositionUpdate += (float)delta;
			if (_lastPositionUpdate >= POSITION_UPDATE_INTERVAL && SteamManager.Manager != null)
			{
				SteamManager.Manager.UpdatePlayerPosition(_playerName, Position, Rotation, isMoving);
				_lastPositionUpdate = 0.0f;
				
			}
		}
		else if (!_isLocalPlayer)
		{
			// For remote players, get position from Steam lobby data
			if (SteamManager.Manager != null)
			{
				var playerData = SteamManager.Manager.GetPlayerPosition(_playerName);
				if (playerData.HasValue)
				{
					var (pos, rot, moving) = playerData.Value;
					
					// Debug: Print position updates for remote players
					if (Position.DistanceTo(pos) > 0.1f) // Only print if there's a significant change
					{
						GD.Print($"Remote player {_playerName} updating position from {Position} to {pos}");
					}
					
					// Smoothly interpolate to network position
					Position = Position.MoveToward(pos, MovementSpeed * (float)delta);
					Rotation = Rotation.MoveToward(rot, 5.0f * (float)delta);
					
					// Update animations based on network state
					_animationTree.Set("parameters/conditions/Run", moving);
					_animationTree.Set("parameters/conditions/Idle", !moving);
				}
				else
				{
					// Debug: Print when no position data is available
					if (Engine.GetProcessFrames() % 60 == 0) // Print once per second
					{
						GD.Print($"Remote player {_playerName} - no position data available for this player");
					}
				}
			}
		}
	}
	
	// Methods called by GameManager to set player properties
	public void SetIsLocalPlayer(bool isLocal)
	{
		_isLocalPlayer = isLocal;
		GD.Print($"Player {Name} set as local: {isLocal}");
		
		// You could add visual indicators here for debugging
		if (isLocal)
		{
			// Maybe change player color or add a nameplate
			GD.Print($"This is the LOCAL player: {_playerName}");
		}
	}
	
	public void SetPlayerName(string playerName)
	{
		_playerName = playerName;
		GD.Print($"Player name set to: {playerName}");
	}
	
	// Public method to force UI update (called by GameManager after UI connection)
	public void ForceUIUpdate()
	{
		GD.Print($"ForceUIUpdate called - CurrentHealth: {CurrentHealth}, MaxHealth: {MaxHealth}");
		UpdateHealthBar();
		UpdateXpCircle();
	}
	
	// Public method to initialize health properly (called by GameManager)
	public void InitializeHealth()
	{
		GD.Print($"InitializeHealth called - MaxHealth: {MaxHealth}");
		CurrentHealth = MaxHealth;
		GD.Print($"CurrentHealth set to: {CurrentHealth}");
		UpdateHealthBar();
	}
}
