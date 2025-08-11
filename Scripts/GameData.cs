using Godot;
using System;
using System.Collections.Generic;

public partial class GameData : Node
{
	public static GameData Instance { get; private set; }
	
	private List<string> _lobbyPlayerNames = new List<string>();
	private bool _cameFromLobby = false;
	
	public static List<string> LobbyPlayerNames => Instance?._lobbyPlayerNames ?? new List<string>();
	public static bool CameFromLobby => Instance?._cameFromLobby ?? false;
	
	public override void _Ready()
	{
		GD.Print("GameData _Ready called");
		if (Instance == null)
		{
			Instance = this;
			GD.Print("GameData Instance set successfully");
			// Don't destroy on scene change
			ProcessMode = ProcessModeEnum.Always;
		}
		else
		{
			GD.Print("GameData Instance already exists, destroying duplicate");
			QueueFree();
		}
	}
	
	public static void SetLobbyPlayers(List<string> playerNames)
	{
		if (Instance != null)
		{
			Instance._lobbyPlayerNames = new List<string>(playerNames);
			GD.Print($"GameData: Stored {playerNames.Count} lobby players");
		}
	}
	
	public static void ClearLobbyPlayers()
	{
		if (Instance != null)
		{
			Instance._lobbyPlayerNames.Clear();
			GD.Print("GameData: Cleared lobby players");
		}
	}
	
	public static bool IsMultiplayerGame()
	{
		return LobbyPlayerNames.Count > 1;
	}
}
