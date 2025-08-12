using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
	[Export] public PackedScene PlayerScene { get; set; }
	
	private List<Node3D> spawnedPlayers = new List<Node3D>();
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
		GD.Print("GameManager _Ready called");
		GD.Print($"GameData.Instance exists: {GameData.Instance != null}");
		GD.Print($"Lobby players count: {GameData.LobbyPlayerNames.Count}");
		GD.Print($"Lobby players: [{string.Join(", ", GameData.LobbyPlayerNames)}]");
		
		if (GameData.IsMultiplayerGame())
		{
			GD.Print("Multiplayer game detected, spawning players for lobby members");
			SpawnLobbyPlayers();
		}
		else
		{
			GD.Print("Single player game, spawning default player");
			SpawnSinglePlayer();
		}
	}
	
	public override void _Input(InputEvent @event)
	{
		// Test key T to spawn test players
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.T)
		{
			TestSpawnPlayers();
		}
	}
	
	private void SpawnLobbyPlayers()
	{
		var playerNames = GameData.LobbyPlayerNames;
		var localPlayerName = SteamManager.Manager?.PlayerName ?? "Player";
		
		GD.Print($"Spawning {playerNames.Count} players: {string.Join(", ", playerNames)}");
		GD.Print($"Local player name: {localPlayerName}");
		
		for (int i = 0; i < playerNames.Count && i < spawnPositions.Length; i++)
		{
			// Check if this player is the local player by comparing names
			bool isLocalPlayer = playerNames[i] == localPlayerName;
			GD.Print($"Player {i}: {playerNames[i]} - Local: {isLocalPlayer}");
			SpawnPlayer(playerNames[i], spawnPositions[i], isLocalPlayer);
		}
	}
	
	private void SpawnSinglePlayer()
	{
		// Get the local player name from Steam or use default
		string playerName = SteamManager.Manager?.PlayerName ?? "Player";
		GD.Print($"Spawning single player: {playerName}");
		SpawnPlayer(playerName, spawnPositions[0], true);
	}
	
	private void SpawnPlayer(string playerName, Vector3 position, bool isLocalPlayer)
	{
		GD.Print($"Spawning player: {playerName} at position: {position} (Local: {isLocalPlayer})");
		GD.Print($"PlayerScene export is: {(PlayerScene != null ? "SET" : "NULL")}");
		
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
			GD.Print("Called InitializeHealth on player");
		}
		else
		{
			// Fallback: Ensure health is properly initialized
			var maxHealth = player.Get("MaxHealth");
			player.Set("CurrentHealth", maxHealth);
			GD.Print($"Manually set CurrentHealth to {maxHealth}");
		}
		
		// Add to scene
		var parent = GetParent();
		GD.Print($"Adding player to parent: {parent.Name} (Type: {parent.GetType().Name})");
		parent.AddChild(player);
		spawnedPlayers.Add(player);
		
		GD.Print($"Successfully spawned player: {playerName}");
		GD.Print($"Player position: {player.Position}");
		GD.Print($"Player global position: {player.GlobalPosition}");
		GD.Print($"Player visible: {player.Visible}");
		GD.Print($"Total spawned players: {spawnedPlayers.Count}");
		
		// If this is the local player, set up camera follow
		if (isLocalPlayer)
		{
			SetupCameraForPlayer(player);
			SetupUIForPlayer(player);
		}
	}
	
	private void SetupCameraForPlayer(Node3D player)
	{
		// Find the camera - it should be a child of the root node (same as this GameManager)
		var camera = GetNode<Camera3D>("Camera3D");
		
		if (camera != null)
		{
			GD.Print($"Found camera: {camera.Name} at path: {camera.GetPath()}");
			if (camera.HasMethod("SetTarget"))
			{
				camera.Call("SetTarget", player);
				GD.Print($"Camera set to follow local player: {player.Name}");
			}
			else
			{
				GD.Print("Camera doesn't have SetTarget method");
			}
		}
		else
		{
			GD.Print("Camera3D not found as child of root node");
		}
	}
	
	private void SetupPlayerSpells(Node3D player)
	{
		GD.Print($"Setting up spells for player: {player.Name}");
		
		// Load the spell scenes
		var magicSphereScene = GD.Load<PackedScene>("res://magic_sphere.tscn");
		var arcaneWaveScene = GD.Load<PackedScene>("res://arcane_wave.tscn");
		var mortarBoulderScene = GD.Load<PackedScene>("res://Spells/mortar_boulder.tscn");
		
		// Set the exported properties on the player
		if (magicSphereScene != null)
		{
			player.Set("_magicSphereScene", magicSphereScene);
			GD.Print("Magic sphere scene assigned");
		}
		else
		{
			GD.PrintErr("Could not load magic sphere scene");
		}
		
		if (arcaneWaveScene != null)
		{
			player.Set("_arcaneWaveScene", arcaneWaveScene);
			GD.Print("Arcane wave scene assigned");
		}
		else
		{
			GD.PrintErr("Could not load arcane wave scene");
		}
		
		if (mortarBoulderScene != null)
		{
			player.Set("_mortarBoulderScene", mortarBoulderScene);
			GD.Print("Mortar boulder scene assigned");
		}
		else
		{
			GD.PrintErr("Could not load mortar boulder scene");
		}
	}
	
	private void SetupUIForPlayer(Node3D player)
	{
		GD.Print($"Setting up UI for local player: {player.Name}");
		
		// Find the PlayerUI in the scene
		var playerUI = GetNode<Control>("PlayerUI");
		if (playerUI != null)
		{
			// Get the UI components
			var healthBar = playerUI.GetNode<ProgressBar>("HealthBar");
			var healthLabel = playerUI.GetNode<Label>("HealthBar/HealthLabel");
			var xpCircle = playerUI.GetNode<TextureProgressBar>("XpCircle");
			
			// Set these UI elements on the player
			if (healthBar != null)
			{
				player.Set("_healthBar", healthBar);
				GD.Print("Health bar connected to local player");
			}
			else
			{
				GD.PrintErr("Could not find HealthBar in PlayerUI");
			}
			
			if (healthLabel != null)
			{
				player.Set("_healthLabel", healthLabel);
				GD.Print("Health label connected to local player");
			}
			else
			{
				GD.PrintErr("Could not find HealthLabel in PlayerUI");
			}
			
			if (xpCircle != null)
			{
				player.Set("_xpCircle", xpCircle);
				GD.Print("XP circle connected to local player");
			}
			else
			{
				GD.PrintErr("Could not find XpCircle in PlayerUI");
			}
			
			// Trigger initial UI update
			if (player.HasMethod("UpdateHealthBar"))
			{
				player.Call("UpdateHealthBar");
				GD.Print("Triggered health bar update");
			}
			if (player.HasMethod("UpdateXpCircle"))
			{
				player.Call("UpdateXpCircle");
				GD.Print("Triggered XP circle update");
			}
			
			// Also call the new ForceUIUpdate method
			if (player.HasMethod("ForceUIUpdate"))
			{
				player.Call("ForceUIUpdate");
				GD.Print("Called ForceUIUpdate");
			}
			
			// Also schedule a delayed update to make sure everything is connected
			GetTree().CreateTimer(0.1f).Timeout += () => {
				GD.Print("Delayed UI update triggered");
				if (player.HasMethod("ForceUIUpdate"))
				{
					player.Call("ForceUIUpdate");
				}
			};
			
			// Also print current player stats for debugging
			var currentHealth = player.Get("CurrentHealth");
			var maxHealth = player.Get("MaxHealth");
			var currentXp = player.Get("CurrentXp");
			GD.Print($"Player stats - Health: {currentHealth}/{maxHealth}, XP: {currentXp}");
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
			GD.Print("Level up screen connected to local player");
			
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
	
	// Test method to manually spawn players for debugging
	public void TestSpawnPlayers()
	{
		GD.Print("Test spawning players manually");
		var testPlayers = new List<string> { "TestPlayer1", "TestPlayer2" };
		GameData.SetLobbyPlayers(testPlayers);
		SpawnLobbyPlayers();
	}
}
