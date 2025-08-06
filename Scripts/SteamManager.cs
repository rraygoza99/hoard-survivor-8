using Godot;
using System;
using GodotSteam;

public partial class SteamManager : Node
{
	public override void _Ready(){
		uint appId= 480;
		
		if(Steam.Init(appId))
		{
			GD.Print("Steam initialized successfully!");
			Steam.LobbyCreated += OnLobbyCreated;
			Steam.LobbyJoined += OnLobbyJoined;
		} else {
			GD.PrintErr("Failed to initialized steam");
		}
	}
	
	public override void _Process(double delta){
		Steam.RunCallbacks();
	}
	
	public void CreateLobby()
	{
		Steam.CreateLobby(LobbyType.FriendsOnly, 8);
	}
	
	private void OnLobbyCreated(long steamIdLobby, Result result){
		if(result == Result.OK)
		{
			GD.Print("Lobby created successfully!");
		} else {
			GD.PrintErr("Failed to create the lobby");
		}
	}
	
	private void OnLobbyJoined(long steamIdLobby, ulong steamIdFriend, bool hasChatAccess, Steam.ChatMemberStateChange stateChange)
	{
		GD.Prin("Successfully joined");
	}
}
