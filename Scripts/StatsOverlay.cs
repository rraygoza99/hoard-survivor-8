using Godot;
using System;

public partial class StatsOverlay : Control
{
	// Labels for each stat
	private Label _healthLabel;
	private Label _armorLabel;
	private Label _movementSpeedLabel;
	private Label _criticalChanceLabel;
	private Label _criticalDamageLabel;
	private Label _lifeStealLabel;
	private Label _xpGainLabel;
	private Label _cooldownReductionLabel;
	private Label _luckyLabel;
	private Label _xpLabel;
	
	public override void _Ready()
	{
		// Get all the label references
		_healthLabel = GetNode<Label>("BackgroundPanel/StatsContainer/HealthLabel");
		_armorLabel = GetNode<Label>("BackgroundPanel/StatsContainer/ArmorLabel");
		_movementSpeedLabel = GetNode<Label>("BackgroundPanel/StatsContainer/MovementSpeedLabel");
		_criticalChanceLabel = GetNode<Label>("BackgroundPanel/StatsContainer/CriticalChanceLabel");
		_criticalDamageLabel = GetNode<Label>("BackgroundPanel/StatsContainer/CriticalDamageLabel");
		_lifeStealLabel = GetNode<Label>("BackgroundPanel/StatsContainer/LifeStealLabel");
		_xpGainLabel = GetNode<Label>("BackgroundPanel/StatsContainer/XpGainLabel");
		_cooldownReductionLabel = GetNode<Label>("BackgroundPanel/StatsContainer/CooldownReductionLabel");
		_luckyLabel = GetNode<Label>("BackgroundPanel/StatsContainer/LuckyLabel");
		_xpLabel = GetNode<Label>("BackgroundPanel/StatsContainer/XpLabel");
	}
	
	public void UpdateStats(Node3D player)
	{
		if (player == null) return;
		
		// Get all the stats from the player
		var currentHealth = (float)player.Get("CurrentHealth");
		var maxHealth = (float)player.Get("MaxHealth");
		var armor = (float)player.Get("Armor");
		var movementSpeed = (float)player.Get("MovementSpeed");
		var criticalChance = (float)player.Get("CriticalChance");
		var criticalDamageMultiplier = (float)player.Get("CriticalDamageMultiplier");
		var lifeSteal = (float)player.Get("LifeSteal");
		var xpGainMultiplier = (float)player.Get("XpGainMultiplier");
		var cooldownReduction = (float)player.Get("CooldownReduction");
		var lucky = (float)player.Get("Lucky");
		var currentXp = (int)player.Get("CurrentXp");
		var xpToNextLevel = (int)player.Get("XpToNextLevel");
		
		// Update all the labels
		_healthLabel.Text = $"Health: {Mathf.Round(currentHealth)}/{Mathf.Round(maxHealth)}";
		_armorLabel.Text = $"Armor: {armor}";
		_movementSpeedLabel.Text = $"Movement Speed: {movementSpeed:F1}";
		_criticalChanceLabel.Text = $"Critical Chance: {(criticalChance * 100):F1}%";
		_criticalDamageLabel.Text = $"Critical Damage: {(criticalDamageMultiplier * 100):F0}%";
		_lifeStealLabel.Text = $"Life Steal: {(lifeSteal * 100):F1}%";
		_xpGainLabel.Text = $"XP Gain: {(xpGainMultiplier * 100):F0}%";
		_cooldownReductionLabel.Text = $"Cooldown Reduction: {(cooldownReduction * 100):F1}%";
		_luckyLabel.Text = $"Lucky: {lucky}";
		_xpLabel.Text = $"XP: {currentXp}/{xpToNextLevel}";
	}
	
	public void ShowOverlay()
	{
		Visible = true;
	}
	
	public void HideOverlay()
	{
		Visible = false;
	}
}
