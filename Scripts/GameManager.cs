using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
	[Export] public PackedScene PlayerScene { get; set; }
	
	private List<Node3D> spawnedPlayers = new List<Node3D>();
	private Node3D localPlayer = null;
	private StatsOverlay statsOverlay = null;
	private Vector3[] spawnPositions = {
		new Vector3(0, 1, 0),      // Center
		new Vector3(-2, 1, 0),     // Left
		new Vector3(2, 1, 0),      // Right  
		new Vector3(0, 1, -2),     // Back
		new Vector3(-2, 1, -2),    // Back left
		new Vector3(2, 1, -2),     // Back right
		new Vector3(0, 1, 2),      // Front
		new Vector3(-2, 1, 2),     // Front left
	};
	
	public override void _Ready()
	{
		// Find and initialize the stats overlay
		statsOverlay = GetNode<StatsOverlay>("StatsOverlay");
		
		if (GameData.IsMultiplayerGame())
		{
			SpawnLobbyPlayers();
		}
		else
		{
			SpawnSinglePlayer();
		}
	}
	
	public override void _Input(InputEvent @event)
	{
		// Handle TAB key for stats overlay
		if (@event is InputEventKey keyEvent && keyEvent.Keycode == Key.Tab)
		{
			if (keyEvent.Pressed && !keyEvent.Echo)
			{
				// Show stats overlay when TAB is pressed
				if (statsOverlay != null && localPlayer != null)
				{
					statsOverlay.UpdateStats(localPlayer);
					statsOverlay.ShowOverlay();
				}
			}
			else if (!keyEvent.Pressed)
			{
				// Hide stats overlay when TAB is released
				if (statsOverlay != null)
				{
					statsOverlay.HideOverlay();
				}
			}
		}
	}
	
	private void SpawnLobbyPlayers()
	{
		var playerNames = GameData.LobbyPlayerNames;
		var localPlayerName = SteamManager.Manager?.PlayerName ?? "Player";
		
		for (int i = 0; i < playerNames.Count && i < spawnPositions.Length; i++)
		{
			// Check if this player is the local player by comparing names
			bool isLocalPlayer = playerNames[i] == localPlayerName;
			SpawnPlayer(playerNames[i], spawnPositions[i], isLocalPlayer);
		}
	}
	
	private void SpawnSinglePlayer()
	{
		// Get the local player name from Steam or use default
		string playerName = SteamManager.Manager?.PlayerName ?? "Player";
		SpawnPlayer(playerName, spawnPositions[0], true);
	}
	
	private void SpawnPlayer(string playerName, Vector3 position, bool isLocalPlayer)
	{
		Node3D player = null;
		
		if (PlayerScene != null)
		{
			// Use the assigned player scene
			player = PlayerScene.Instantiate<Node3D>();
		}
		else
		{
			// Try to load the player scene directly
			var playerSceneResource = GD.Load<PackedScene>("res://player.tscn");
			if (playerSceneResource != null)
			{
				player = playerSceneResource.Instantiate<Node3D>();
			}
			else
			{
				GD.PrintErr("Could not load player scene!");
				return;
			}
		}
		
		// Set player properties
		player.Name = $"Player_{playerName}";
		player.Position = position;
		
		// If the player has a script with SetPlayerName method, call it
		if (player.HasMethod("SetPlayerName"))
		{
			player.Call("SetPlayerName", playerName);
		}
		
		// If the player has a script with SetIsLocalPlayer method, call it
		if (player.HasMethod("SetIsLocalPlayer"))
		{
			player.Call("SetIsLocalPlayer", isLocalPlayer);
		}
		
		// Set up spell scenes for the player
		SetupPlayerSpells(player);
		
		// Initialize health properly
		if (player.HasMethod("InitializeHealth"))
		{
			player.Call("InitializeHealth");
		}
		else
		{
			// Fallback: Ensure health is properly initialized
			var maxHealth = player.Get("MaxHealth");
			player.Set("CurrentHealth", maxHealth);
		}
		
		// Add to scene
		var parent = GetParent();
		parent.AddChild(player);
		spawnedPlayers.Add(player);
		
		// If this is the local player, set up camera follow
		if (isLocalPlayer)
		{
			localPlayer = player; // Store reference to local player for stats overlay
			SetupCameraForPlayer(player);
			SetupUIForPlayer(player);
		}
	}
	
	private void SetupCameraForPlayer(Node3D player)
	{
		// Find the camera - it should be a child of the root node (same as this GameManager)
		var camera = GetNode<Camera3D>("Camera3D");
		
		if (camera != null && camera.HasMethod("SetTarget"))
		{
			camera.Call("SetTarget", player);
		}
	}
	
	private void SetupPlayerSpells(Node3D player)
	{
		// Load the spell scenes
		var magicSphereScene = GD.Load<PackedScene>("res://magic_sphere.tscn");
		var arcaneWaveScene = GD.Load<PackedScene>("res://arcane_wave.tscn");
		var mortarBoulderScene = GD.Load<PackedScene>("res://Spells/mortar_boulder.tscn");
		
		// Set the exported properties on the player
		if (magicSphereScene != null)
		{
			player.Set("_magicSphereScene", magicSphereScene);
		}
		else
		{
			GD.PrintErr("Could not load magic sphere scene");
		}
		
		if (arcaneWaveScene != null)
		{
			player.Set("_arcaneWaveScene", arcaneWaveScene);
		}
		else
		{
			GD.PrintErr("Could not load arcane wave scene");
		}
		
		if (mortarBoulderScene != null)
		{
			player.Set("_mortarBoulderScene", mortarBoulderScene);
		}
		else
		{
			GD.PrintErr("Could not load mortar boulder scene");
		}
	}
	
	private void SetupUIForPlayer(Node3D player)
	{
		// Find the PlayerUI in the scene
		var playerUI = GetNodeOrNull<Control>("PlayerUI");
		if (playerUI != null)
		{
			// New bars
			var healthBar = playerUI.GetNodeOrNull<TextureProgressBar>("HealthBar");
			var xpBar = playerUI.GetNodeOrNull<TextureProgressBar>("XpBar");
			var xpCircle = playerUI.GetNodeOrNull<TextureProgressBar>("XpCircle");
			// Health label may be sibling or child
			Label healthLabel = playerUI.GetNodeOrNull<Label>("HealthLabel")
				?? healthBar?.GetNodeOrNull<Label>("HealthLabel");

			if (healthBar != null)
			{
				player.Set("_healthBar", healthBar);
				GD.Print("Assigned new health bar to player");
			}
			else
			{
				GD.PrintErr("Could not find HealthBar in PlayerUI");
			}
			if (xpBar != null)
			{
				player.Set("_xpBar", xpBar);
			}

			if (healthLabel != null)
			{
				player.Set("_healthLabel", healthLabel);
			}
			else
			{
				GD.PrintErr("Could not find HealthLabel in PlayerUI");
			}

			if (xpCircle != null)
			{
				player.Set("_xpCircle", xpCircle);
			}
			else
			{
				GD.PrintErr("Could not find XpCircle in PlayerUI");
			}

			// Force immediate UI sync
			if (player.HasMethod("ForceUIUpdate"))
			{
				player.Call("ForceUIUpdate");
			}
			// Delayed sync after one frame for safety
			GetTree().CreateTimer(0.05f).Timeout += () => {
				if (player.HasMethod("ForceUIUpdate"))
					player.Call("ForceUIUpdate");
			};
		}
		else
		{
			GD.PrintErr("Could not find PlayerUI in scene");
		}
		
		// Find and connect the LevelUpScreen for local player
		var levelUpScreen = GetNode<Control>("LevelUpScreen");
		if (levelUpScreen != null)
		{
			player.Set("_levelUpScreen", levelUpScreen);
			
			// Connect the level up screen event
			if (player.HasMethod("ConnectLevelUpScreen"))
			{
				player.Call("ConnectLevelUpScreen");
			}
		}
		else
		{
			GD.PrintErr("Could not find LevelUpScreen in scene");
		}
	}
	
	public List<Node3D> GetSpawnedPlayers()
	{
		return new List<Node3D>(spawnedPlayers);
	}
	
}
