using Godot;
using System;

public partial class SteamManager : Node
{
	public override void _Ready(){
		if(SteamNative.SteamAPI_Init())
		{
			GD.Print("Steam initialiez");
		} else {
			GD.PrintErr("Failed to initialize steam");
		}
	}
	public override void _Process(double delta){
		SteamNative.SteamAPI_RunCallbacks();
	}
	
	public override void _Notification(int what){
		if(what == NotificationWMCloseRequest){
			SteamNative.SteamAPI_Shutdown();
			GetTree().Quit();
		}
	}
	public void CreateLobby(){
		GD.Print("Requestion create a Lobby");
		SteamNative.CreateLobby(ELobbyType.FriendsOnly, 4);
	}
	
	private void OnLobbyCreated(LobbyCreated_t callbackData)
	{
		if(callbackData.m_eResult == EResult.OK)
		{
			GD.Print($"Lobby created successfully! Lobby ID: {callbackData.m_ulSteamIDLobby}");
		}
		else
		{
			GD.PrintErr("Failed to create lobby.");
		}
	}
	
}
