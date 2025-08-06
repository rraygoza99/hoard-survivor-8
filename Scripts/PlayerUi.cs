using Godot;
using System;

public partial class PlayerUi : Control
{
	private SteamManager _steamManager;
	
	public override void _Ready()
	{
		_steamManager = GetNode<SteamManager>("/root/SteamManager");
		GetNode<Button>("HostButton").Pressed += _on_host_button_pressed;
	}
	private void _on_host_button_pressed()
	{
		_steamManager.CreateLobby();
	}
}
