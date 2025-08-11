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
		GD.Print($"Spawning {playerNames.Count} players: {string.Join(", ", playerNames)}");
		
		for (int i = 0; i < playerNames.Count && i < spawnPositions.Length; i++)
		{
			SpawnPlayer(playerNames[i], spawnPositions[i], i == 0); // First player is the local player
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
