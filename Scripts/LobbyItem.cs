using Godot;
using System;

public partial class LobbyItem : Control
{
	private Label _lobbyIdLabel;
	private Label _lobbyNameLabel;
	private Button _joinButton;

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

	private async void OnJoinButtonPressed()
	{
		
	}
}
