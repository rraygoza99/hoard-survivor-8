using Godot;
using System;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

public partial class SteamManager : Node
{
	public static SteamManager Manager {get; set;}
	private static uint gameAppId {get;set;} = 480;
	public string PlayerName {get;set;}
	private Lobby hostedLobby {get; set;}
	private List<Lobby> availableLobbies {get;set;} = new List<Lobby>();
	public bool IsSteamInitialized { get; private set; } = false;
	private SceneManager _sceneManager;
	
	// For testing: allows multiple instances with same Steam account
	private static bool enableTestMode = true;
	public static event Action<List<Lobby>> OnLobbiesRefreshCompleted;
	public static event Action<int> OnLobbyMemberCountChanged;
	public static event Action<Dictionary<string, bool>> OnPlayerReadyStatusChanged;
	public static event Action OnAllPlayersReady;
	public static event Action OnNotAllPlayersReady;
	
	// Ready system
	private Dictionary<string, bool> playerReadyStatus = new Dictionary<string, bool>();
	public bool IsLocalPlayerReady { get; private set; } = false;
	
	// Property to get current lobby member count
	public int CurrentLobbyMemberCount 
	{ 
		get 
		{ 
			if (hostedLobby.Id != 0)
				return hostedLobby.MemberCount;
			return 0;
		} 
	}
	public SteamManager()
	{
		if (Manager == null)
		{
			Manager = this;
			try
			{
				GD.Print("Attempting to initialize Steam client...");
				GD.Print($"App ID: {gameAppId}");
				GD.Print($"Test Mode: {enableTestMode}");
				
				SteamClient.Init(gameAppId, enableTestMode);				if (!SteamClient.IsValid)
				{
					GD.PrintErr("Error initializing steam client - SteamClient is not valid");
					GD.PrintErr("Make sure Steam is running and you have the correct steam_api64.dll version");
					return;
				}

				GD.Print("Successfully initialized steam client");
				PlayerName = SteamClient.Name;
				GD.Print($"Player name: {PlayerName}");
				IsSteamInitialized = true;

			}
			catch (System.DllNotFoundException dllEx)
			{
				GD.PrintErr($"DLL not found: {dllEx.Message}");
				GD.PrintErr("Make sure steam_api64.dll is in the correct location and is the right version");
			}
			catch (System.EntryPointNotFoundException entryEx)
			{
				GD.PrintErr($"Entry point not found: {entryEx.Message}");
				GD.PrintErr("This usually means there's a version mismatch between Facepunch.Steamworks and steam_api64.dll");
				GD.PrintErr("Try updating both to the latest compatible versions");
			}
			catch (Exception e)
			{
				GD.PrintErr($"Steam initialization error: {e.Message}");
				GD.PrintErr($"Exception type: {e.GetType().Name}");
			}
		}
	}
	public override void _Ready()
	{
		_sceneManager = GetNode<SceneManager>("/root/SceneManager");
		if (!IsSteamInitialized)
		{
			GD.PrintErr("Steam is not initialized, skipping lobby callbacks setup");
			return;
		}

		SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreatedCallback;
		SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedCallback;
		SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoinedCallback;
		SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnectedCallback;
		SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeaveCallback;
		SteamMatchmaking.OnLobbyEntered += OnLobbyEnteredCallback;
	}
	
	private void OnLobbyMemberLeaveCallback(Lobby lobby, Friend friend){
		GD.Print($"User has left the lobby: {friend.Name}");
		GD.Print($"Current lobby members: {lobby.MemberCount}");
		OnLobbyMemberCountChanged?.Invoke(lobby.MemberCount);
	}
	private void OnLobbyMemberDisconnectedCallback(Lobby lobby, Friend friend){
		GD.Print($"User has disconnected from the lobby: {friend.Name}");
		GD.Print($"Current lobby members: {lobby.MemberCount}");
		OnLobbyMemberCountChanged?.Invoke(lobby.MemberCount);
	}
	private void OnLobbyGameCreatedCallback(Lobby lobby, uint id, ushort port, SteamId steamId){
		GD.Print("Firing callback for lobby game created");
	}
	private void OnLobbyCreatedCallback(Result result, Lobby lobby){
		if (result != Result.OK)
		{
			GD.Print("lobby was not created");
		}
		else
		{
			GD.Print("Lobby was created " + lobby.Id);
			_sceneManager.GoToScene("res://UtilityScenes/lobby.tscn");
		}
	}
	private void OnLobbyMemberJoinedCallback(Lobby lobby, Friend friend){
		GD.Print($"User has joined the lobby: {friend.Name}");
		GD.Print($"Current lobby members: {lobby.MemberCount}");
		OnLobbyMemberCountChanged?.Invoke(lobby.MemberCount);
	}
	private void OnLobbyEnteredCallback(Lobby lobby){
		if (lobby.MemberCount > 0)
		{
			GD.Print($"You joined {lobby.Owner.Name}'s lobby");
			GD.Print($"Current lobby members: {lobby.MemberCount}");
			OnLobbyMemberCountChanged?.Invoke(lobby.MemberCount);
			_sceneManager.GoToScene("res://UtilityScenes/lobby.tscn");
		}
	}
	public override void _Process(double delta){
		try{
			if(IsSteamInitialized && SteamClient.IsValid){
				SteamClient.RunCallbacks();
			}
		} catch(Exception e){
			GD.PrintErr($"Error running Steam callbacks: {e.Message}");
		}
	}
	
	public async Task<bool> CreateLobby(){
		try
		{
			GD.Print("creating lobby");
			Lobby? createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(16);
			
			if(!createLobbyOutput.HasValue){
				GD.Print("lobby created but no instance correctly");
				return false;
			}
			GD.Print("setting lobby");
			hostedLobby = createLobbyOutput.Value;
			hostedLobby.SetPublic();
			hostedLobby.SetJoinable(true);
			hostedLobby.SetData("ownerNameString", PlayerName);
			GD.Print($"Lobby created successfully! Lobby ID: {hostedLobby.Id}");
			return true;
		} catch(Exception e){
			GD.Print("Error creating the lobby " + e.Message);
			return false;
		}
	}
	
	public string GetCurrentLobbyId()
	{
		if (hostedLobby.Id != 0)
		{
			return hostedLobby.Id.ToString();
		}
		return string.Empty;
	}
	
	public List<string> GetLobbyMemberNames()
	{
		List<string> memberNames = new List<string>();
		
		if (hostedLobby.Id != 0)
		{
			foreach (Friend member in hostedLobby.Members)
			{
				memberNames.Add(member.Name);
			}
		}
		
		return memberNames;
	}
	
	public void PrintLobbyInfo()
	{
		if (hostedLobby.Id != 0)
		{
			GD.Print($"=== Lobby Info ===");
			GD.Print($"Lobby ID: {hostedLobby.Id}");
			GD.Print($"Owner: {hostedLobby.Owner.Name}");
			GD.Print($"Member Count: {hostedLobby.MemberCount}");
			GD.Print($"Max Members: {hostedLobby.MaxMembers}");
			GD.Print($"Members:");
			
			foreach (Friend member in hostedLobby.Members)
			{
				GD.Print($"  - {member.Name} (ID: {member.Id})");
			}
			GD.Print($"==================");
		}
		else
		{
			GD.Print("Not currently in a lobby");
		}
	}
	
	public async Task<bool> GetMultiplayerLobbies(){
		try{
			Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithMaxResults(10).RequestAsync();
			if(lobbies != null){
				foreach(var lobby in lobbies){
					GD.Print("Lobby: " + lobby.Id);
					availableLobbies.Add(lobby);
				}
			}

			OnLobbiesRefreshCompleted?.Invoke(availableLobbies);
			return true;
		} catch(Exception e){
			GD.Print("Error fetching lobbies " +e.Message);
			return false;
		}
	}
	
	public async Task<bool> JoinLobby(string lobbyIdString){
		if (!IsSteamInitialized)
		{
			GD.PrintErr("Steam is not initialized, cannot join lobby");
			return false;
		}
		
		try
		{
			// Parse the lobby ID from string to ulong
			if (!ulong.TryParse(lobbyIdString, out ulong lobbyId))
			{
				GD.PrintErr($"Invalid lobby ID format: {lobbyIdString}");
				return false;
			}
			
			GD.Print($"Attempting to join lobby: {lobbyId}");
			
			// Create a SteamId from the lobby ID
			SteamId steamLobbyId = lobbyId;
			
			// Join the lobby
			Lobby? joinResult = await SteamMatchmaking.JoinLobbyAsync(steamLobbyId);
			
			if (joinResult.HasValue)
			{
				GD.Print("Successfully joined lobby!");
				hostedLobby = joinResult.Value;
				return true;
			}
			else
			{
				GD.PrintErr("Failed to join lobby - lobby not found or inaccessible");
				return false;
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error joining lobby: {e.Message}");
			return false;
		}
	}
	
	// Ready system methods
	public void SetPlayerReady(bool ready)
	{
		if (!IsSteamInitialized || hostedLobby.Id == 0)
		{
			GD.PrintErr("Cannot set ready status - not in a lobby");
			return;
		}
		
		IsLocalPlayerReady = ready;
		string readyData = ready ? "1" : "0";
		
		// Set player ready status in lobby data
		hostedLobby.SetMemberData("ready", readyData);
		GD.Print($"Set local player ready status to: {ready}");
		
		// Refresh ready status for all players
		RefreshAllPlayerReadyStatus();
	}
	
	public void RefreshAllPlayerReadyStatus()
	{
		if (hostedLobby.Id == 0) return;
		
		playerReadyStatus.Clear();
		int readyCount = 0;
		int totalPlayers = 0;
		
		foreach (Friend member in hostedLobby.Members)
		{
			totalPlayers++;
			string readyData = hostedLobby.GetMemberData(member, "ready");
			bool isReady = readyData == "1";
			playerReadyStatus[member.Name] = isReady;
			
			if (isReady) readyCount++;
			
			GD.Print($"Player {member.Name} ready status: {isReady}");
		}
		
		GD.Print($"Ready players: {readyCount}/{totalPlayers}");
		
		// Notify UI of ready status changes
		OnPlayerReadyStatusChanged?.Invoke(new Dictionary<string, bool>(playerReadyStatus));
		
		// Check if all players are ready
		if (totalPlayers > 0 && readyCount == totalPlayers)
		{
			GD.Print("All players are ready!");
			OnAllPlayersReady?.Invoke();
		}
		else if (totalPlayers > 0)
		{
			GD.Print($"Not all players ready ({readyCount}/{totalPlayers})");
			OnNotAllPlayersReady?.Invoke();
		}
	}
	
	public Dictionary<string, bool> GetPlayerReadyStatus()
	{
		return new Dictionary<string, bool>(playerReadyStatus);
	}
	
	public override void _Notification(int what){
		base._Notification(what);
		if(what == NotificationWMCloseRequest){
			SteamClient.Shutdown();
			GetTree().Quit();
		}
	}
}
