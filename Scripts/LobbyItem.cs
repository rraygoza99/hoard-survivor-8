using Godot;
using System;
using Steamworks.Data;

public partial class LobbyItem : Control
{
	private Label _lobbyIdLabel;
	private Label _lobbyNameLabel;
	private Button _joinButton;
	private Lobby _lobby;

	public override void _Ready()
	{
		_lobbyIdLabel = GetNodeOrNull<Label>("LobbyIdLabel");
		_lobbyNameLabel = GetNodeOrNull<Label>("LobbyNameLabel");
		_joinButton = GetNodeOrNull<Button>("JoinButton");
		
		if (_joinButton != null)
		{
			_joinButton.Pressed += OnJoinButtonPressed;
		}
	}

	public void SetLabels(string lobbyId, string lobbyName, Lobby lobby)
	{
		_lobby = lobby;
		
		if (_lobbyIdLabel != null)
			_lobbyIdLabel.Text = lobbyId;
			
		if (_lobbyNameLabel != null)
			_lobbyNameLabel.Text = lobbyName;
	}

	private async void OnJoinButtonPressed()
	{
		if (SteamManager.Manager != null)
		{
			string lobbyIdString = _lobby.Id.ToString();
			await SteamManager.Manager.JoinLobby(lobbyIdString);
		}
	}
}
