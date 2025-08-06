using Godot;
using System;

public partial class PlayerUi : Control
{
	private Node _steamManager;
	private LineEdit _lobbyIdInput;
	
	public override void _Ready()
	{
		_steamManager = GetNode<Node>("/root/SteamManager");
		_lobbyIdInput = GetNode<LineEdit>("LobbyIdInput");
		if(_steamManager == null)
			GD.PrintErr("Could not find SteamManager");
		GetNode<Button>("HostButton").Pressed += _on_host_button_pressed;
		GetNode<Button>("JoinButton").Pressed += _on_join_button_pressed;
	}
	private void _on_host_button_pressed()
	{
		if(_steamManager != null)
		{
			_steamManager.Call("create_lobby");
		}
		else{
			GD.PrintErr("Steam manager not available");
		}
	}
	private void _on_join_button_pressed()
	{
		if(_steamManager!= null){
			string lobbyId = _lobbyIdInput.Text;
			_steamManager.Call("join_lobby", lobbyId);
		} else {
			GD.PrintErr("Cannot join lobby");
		}
	}
}
