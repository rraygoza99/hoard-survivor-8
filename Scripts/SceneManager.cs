using Godot;
using Steamworks.Data;
using System;
using System.Collections.Generic;

public partial class SceneManager : Control
{
	[Export]public PackedScene LobbyElementScene { get; set; }
	public override void _Ready()
	{
		SteamManager.OnLobbiesRefreshCompleted += OnLobbiesRefreshCompletedCallback;
	}
	private void OnLobbiesRefreshCompletedCallback(List<Lobby> lobbies)
	{
		GD.Print("Lobbies refreshed, count: " + lobbies.Count);
		foreach (var lobby in lobbies)
		{
			var element = LobbyElementScene.Instantiate<LobbyItem>();
			element.SetLabels(lobby.Id.ToString(), lobby.GetData("ownerNameString") + " lobby", lobby);
			GetNode<VBoxContainer>("LobbyContainer").AddChild(element);
		}
	}
	private void _on_create_lobby_pressed()
	{
		SteamManager.Manager.CreateLobby();
	}
	private void _on_get_lobbies_pressed(){
		var lobbies = SteamManager.Manager.GetMultiplayerLobbies();
	}
}
