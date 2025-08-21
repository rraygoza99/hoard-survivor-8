using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerUi : Control
{
	private SteamManager _steamManager;
	private LineEdit _lobbyIdInput;
	private Label _memberCountLabel;
	
	public override void _Ready()
	{
		_steamManager = GetNode<SteamManager>("/root/SteamManager");
		_lobbyIdInput = GetNode<LineEdit>("LobbyIdInput");
		
		// Try to get member count label (optional, in case you don't have it in the scene yet)
		_memberCountLabel = GetNodeOrNull<Label>("MemberCountLabel");
		
		if(_steamManager == null)
			GD.PrintErr("Could not find SteamManager");
		
		GetNode<Button>("HostButton").Pressed += _on_host_button_pressed;
		GetNode<Button>("JoinButton").Pressed += _on_join_button_pressed;
		
		// Add a test button if it exists in your scene
		var testButton = GetNodeOrNull<Button>("TestButton");
		if (testButton != null)
		{
			testButton.Pressed += _on_test_button_pressed;
		}
		
		// Subscribe to member count changes
		SteamManager.OnLobbyMemberCountChanged += OnLobbyMemberCountChanged;
	}
	
	private void OnLobbyMemberCountChanged(int memberCount)
	{
		GD.Print($"Lobby member count changed: {memberCount}");
		
		// Update UI label if it exists
		if (_memberCountLabel != null)
		{
			_memberCountLabel.Text = $"Players in lobby: {memberCount}";
		}
		
		// Print detailed lobby info
		if (_steamManager != null)
		{
			_steamManager.PrintLobbyInfo();
		}
	}
	private async void _on_host_button_pressed()
	{
		if(_steamManager != null && _steamManager.IsSteamInitialized)
		{
			GD.Print("Creating lobby...");
			bool success = await _steamManager.CreateLobby();
			
			if (success)
			{
				string lobbyId = _steamManager.GetCurrentLobbyId();
				GD.Print($"Lobby created! Share this ID with friends: {lobbyId}");
				GD.Print($"Current players in lobby: {_steamManager.CurrentLobbyMemberCount}");
				
				// Update UI
				OnLobbyMemberCountChanged(_steamManager.CurrentLobbyMemberCount);
			}
			else
			{
				GD.PrintErr("Failed to create lobby");
			}
		}
		else{
			GD.PrintErr("Steam manager not available or Steam not initialized");
		}
	}
	private async void _on_join_button_pressed()
	{
		if(_steamManager != null && _steamManager.IsSteamInitialized){
			string lobbyId = _lobbyIdInput.Text.Trim();
			
			if (string.IsNullOrEmpty(lobbyId))
			{
				GD.PrintErr("Please enter a lobby ID");
				return;
			}
			
			GD.Print($"Attempting to join lobby with ID: {lobbyId}");
			bool success = await _steamManager.JoinLobby(lobbyId);
			
			if (success)
			{
				GD.Print("Successfully joined lobby!");
				GD.Print($"Current players in lobby: {_steamManager.CurrentLobbyMemberCount}");
				
				// Update UI
				OnLobbyMemberCountChanged(_steamManager.CurrentLobbyMemberCount);
			}
			else
			{
				GD.PrintErr("Failed to join lobby");
			}
		} else {
			GD.PrintErr("Steam manager not available or Steam not initialized");
		}
	}
	
	private void _on_test_button_pressed()
	{
		if (_steamManager != null)
		{
			GD.Print("=== STEAM TEST INFO ===");
			GD.Print($"Steam Initialized: {_steamManager.IsSteamInitialized}");
			GD.Print($"Player Name: {_steamManager.PlayerName}");
			GD.Print($"Current Lobby Member Count: {_steamManager.CurrentLobbyMemberCount}");
			GD.Print($"Current Lobby ID: {_steamManager.GetCurrentLobbyId()}");
			
			var memberNames = _steamManager.GetLobbyMemberNames();
			if (memberNames.Count > 0)
			{
				GD.Print("Lobby Members:");
				foreach (KeyValuePair<string, string> kvp in memberNames)
				{
					GD.Print($"  - {kvp.Value} (ID: {kvp.Key})");
				}
			}
			
			_steamManager.PrintLobbyInfo();
			GD.Print("=====================");
		}
	}
	
	public override void _ExitTree()
	{
		// Unsubscribe from events to prevent memory leaks
		SteamManager.OnLobbyMemberCountChanged -= OnLobbyMemberCountChanged;
	}
}
