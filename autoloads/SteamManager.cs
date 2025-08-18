using Godot;
using System;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public partial class SteamManager : Node
{
	public static SteamManager Manager { get; set; }
	private static uint gameAppId { get; set; } = 480;
	public string PlayerName { get; set; }
	public SteamId PlayerSteamID { get; set; }
	private Lobby hostedLobby { get; set; }
	public bool IsHost { get; private set; } = false;
	private List<Lobby> availableLobbies { get; set; } = new List<Lobby>();
	public bool IsSteamInitialized { get; private set; } = false;
	private SceneManager _sceneManager;
	public SteamSocketManager SteamSocketManager;
	public SteamConnectionManager SteamConnectionManager;

	// For testing: allows multiple instances with same Steam account
	private static bool enableTestMode = true;
	public static event Action<List<Lobby>> OnLobbyRefreshCompleted;
	public static event Action<Friend> OnPlayerJoinLobby;
    public static event Action<Friend> OnPlayerLeftLobby;
	public static event Action<int> OnLobbyMemberCountChanged;
	public static event Action<Dictionary<string, bool>> OnPlayerReadyStatusChanged;
	public static event Action OnAllPlayersReady;
	public static event Action OnNotAllPlayersReady;
	public static event Action OnGameStartSignaled;
	// Pause system (simplified)
	public static event Action<bool, string, int, int> OnPauseStateChanged; // paused, initiator, votes(current phase), total

	// Ready system
	private Dictionary<string, bool> playerReadyStatus = new Dictionary<string, bool>();
	public bool IsLocalPlayerReady { get; private set; } = false;

	// State tracking to prevent repeated messages
	private bool gameStartProcessed = false;
	private bool lastAllPlayersReadyState = false;
	private float lastLobbyDataChangeTime = 0.0f;
	private bool lastPausedState = false;
	private string lastPauseInitiator = "";
	// Track last vote count for current phase
	private int lastPhaseVotes = 0;

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
				// GD.Print("Attempting to initialize Steam client...");
				SteamClient.Init(gameAppId, enableTestMode);
				if (!SteamClient.IsValid)
				{
					// GD.PrintErr("SteamClient not valid");
					return;
				}
				PlayerName = SteamClient.Name;
				PlayerSteamID = SteamClient.SteamId;
				IsSteamInitialized = true;
			}
			catch (Exception e)
			{
				// GD.PrintErr($"Steam init error: {e.Message}");
			}
		}
	}

	public override void _Ready()
	{
		_sceneManager = GetNodeOrNull<SceneManager>("/root/SceneManager");
		if (!IsSteamInitialized) return;
		SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreatedCallback;
		SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedCallback;
		SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoinedCallback;
		SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeaveCallback;
		SteamMatchmaking.OnLobbyEntered += OnLobbyEnteredCallback;
		SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataChangedCallback;
		
	}

	// Simplified unified vote (placed after initialization)
	public void TogglePausePhaseVote()
	{
		if (hostedLobby.Id == 0)
		{
			lastPausedState = !lastPausedState;
			lastPauseInitiator = lastPausedState ? PlayerName : "";
			OnPauseStateChanged?.Invoke(lastPausedState, lastPauseInitiator, (lastPausedState ? 1 : 0), 1);
			return;
		}
		bool paused = hostedLobby.GetData("game_paused") == "true";
		Friend self = default;
		foreach (Friend f in hostedLobby.Members) { if (f.Name == PlayerName) { self = f; break; } }
		string myVote = hostedLobby.GetMemberData(self, "vote");
		string newVote = (myVote == "1") ? "0" : "1";
		hostedLobby.SetMemberData("vote", newVote);
		// GD.Print($"[PauseSystem] {PlayerName} vote -> {newVote} phase={(paused ? "RESUME" : "PAUSE")}");
		if (IsLobbyOwner()) RecalculatePhase(paused);
		else
		{
			string ping = hostedLobby.GetData("vote_ping");
			hostedLobby.SetData("vote_ping", (ping == "1" ? "0" : "1"));
		}
	}

	private void OnLobbyMemberLeaveCallback(Lobby lobby, Friend friend)
	{
		// GD.Print("User Has left Disconnectd from lobby: " + friend.Name);
        OnPlayerLeftLobby(friend);
		OnLobbyMemberCountChanged?.Invoke(lobby.MemberCount);
	}
	private void OnLobbyGameCreatedCallback(Lobby lobby, uint id, ushort port, SteamId steamId)
	{
		// GD.Print("Firing callback for lobby game created");
	}
	private void OnLobbyCreatedCallback(Result result, Lobby lobby)
	{
		if (result != Result.OK)
		{
			// GD.Print("lobby was not created");
		}
		else
		{
			// GD.Print("Lobby was created " + lobby.Id);
			CreateSteamSocketServer();
			_sceneManager.GoToScene("res://UtilityScenes/lobby.tscn");
		}
		
	}
	private void OnLobbyMemberJoinedCallback(Lobby lobby, Friend friend)
	{
		// GD.Print($"User has joined the lobby: {friend.Name}");
		// GD.Print($"Current lobby members: {lobby.MemberCount}");
		
        OnPlayerJoinLobby(friend);
		OnLobbyMemberCountChanged?.Invoke(lobby.MemberCount);
	}
	private void OnLobbyEnteredCallback(Lobby lobby)
	{
		if (lobby.MemberCount > 0)
		{
			// GD.Print($"You joined {lobby.Owner.Name}'s lobby");
			// GD.Print($"Current lobby members: {lobby.MemberCount}");
			OnLobbyMemberCountChanged?.Invoke(lobby.MemberCount);
			hostedLobby = lobby;
			foreach (var item in lobby.Members)
			{
				OnPlayerJoinLobby(item);
			}
			JoinSteamSocketServer(lobby.Owner.Id);
			_sceneManager.GoToScene("res://UtilityScenes/lobby.tscn");
			
		}
	}

	private void OnLobbyDataChangedCallback(Lobby lobby)
	{
		// Only print lobby data change message occasionally to reduce spam
		float currentTime = Time.GetTicksMsec() / 1000.0f;
		if (currentTime - lastLobbyDataChangeTime > 2.0f) // Print at most every 2 seconds
		{
			// GD.Print("Lobby data changed");
			lastLobbyDataChangeTime = currentTime;
		}

		// Check if game start signal was set (only process once)
		if (lobby.GetData("game_start") == "true" && !gameStartProcessed)
		{
			// GD.Print("Game start signal received from host!");
			gameStartProcessed = true;
			OnGameStartSignaled?.Invoke();
		}

		// Also refresh ready status when lobby data changes
		RefreshAllPlayerReadyStatus();

		// Host: check unified vote tally when ping toggled or any vote present
		if (IsLobbyOwner())
		{
			string pingVal = lobby.GetData("vote_ping");
			bool hostPaused = lobby.GetData("game_paused") == "true";
			bool anyVote = false;
			foreach (Friend m in lobby.Members)
			{
				if (lobby.GetMemberData(m, "vote") == "1") { anyVote = true; break; }
			}
			if (anyVote || !string.IsNullOrEmpty(pingVal)) RecalculatePhase(hostPaused);
		}

		// Pause / resume state change detection
		bool isPaused = lobby.GetData("game_paused") == "true";
		string initiator = lobby.GetData("pause_initiator");
		int total = lobby.MemberCount;
		int votes = 0;
		int.TryParse(lobby.GetData(isPaused ? "resume_vote_count" : "pause_vote_count"), out votes);
		if (isPaused != lastPausedState || initiator != lastPauseInitiator || votes != lastPhaseVotes)
		{
			lastPausedState = isPaused;
			lastPauseInitiator = initiator;
			lastPhaseVotes = votes;
			OnPauseStateChanged?.Invoke(isPaused, initiator, votes, total);
		}
	}

	public override void _Process(double delta)
	{
		try
		{
			if (IsSteamInitialized && SteamClient.IsValid)
			{
				SteamClient.RunCallbacks();
			}
			if (SteamSocketManager != null)
			{
				SteamSocketManager.Receive();
			}
			if (SteamConnectionManager != null && SteamConnectionManager.Connected)
			{
				SteamConnectionManager.Receive();
			}
		}
		catch (Exception e)
		{
				// GD.PrintErr($"Error running Steam callbacks: {e.Message}");
		}
	}

	public async Task<bool> CreateLobby()
	{
		try
		{
			// GD.Print("creating lobby");
			Lobby? createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(16);

			if (!createLobbyOutput.HasValue)
			{
				// GD.Print("lobby created but no instance correctly");
				return false;
			}
			// GD.Print("setting lobby");
			hostedLobby = createLobbyOutput.Value;
			hostedLobby.SetPublic();
			hostedLobby.SetJoinable(true);
			hostedLobby.SetData("ownerNameString", PlayerName);
			// GD.Print($"Lobby created successfully! Lobby ID: {hostedLobby.Id}");
			return true;
		}
		catch (Exception e)
		{
			// GD.Print("Error creating the lobby " + e.Message);
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

	public async Task<bool> GetMultiplayerLobbies()
	{
		try
		{
			availableLobbies.Clear(); // ensure fresh list each refresh
			Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithMaxResults(10).RequestAsync();
			if (lobbies != null)
			{
				foreach (var lobby in lobbies)
				{
					// GD.Print("Lobby: " + lobby.Id);
					availableLobbies.Add(lobby);
				}
			}

			OnLobbyRefreshCompleted?.Invoke(availableLobbies);
			return true;
		}
		catch (Exception e)
		{
			// GD.Print("Error fetching lobbies " + e.Message);
			return false;
		}
	}

	public async Task<bool> JoinLobby(string lobbyIdString)
	{
		if (!IsSteamInitialized)
		{
			// GD.PrintErr("Steam is not initialized, cannot join lobby");
			return false;
		}

		try
		{
			// Parse the lobby ID from string to ulong
			if (!ulong.TryParse(lobbyIdString, out ulong lobbyId))
			{
				// GD.PrintErr($"Invalid lobby ID format: {lobbyIdString}");
				return false;
			}

			// GD.Print($"Attempting to join lobby: {lobbyId}");

			// Create a SteamId from the lobby ID
			SteamId steamLobbyId = lobbyId;

			// Join the lobby
			Lobby? joinResult = await SteamMatchmaking.JoinLobbyAsync(steamLobbyId);

			if (joinResult.HasValue)
			{
				// GD.Print("Successfully joined lobby!");
				hostedLobby = joinResult.Value;
				return true;
			}
			else
			{
				// GD.PrintErr("Failed to join lobby - lobby not found or inaccessible");
				return false;
			}
		}
		catch (Exception e)
		{
			// GD.PrintErr($"Error joining lobby: {e.Message}");
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

		}


		// Notify UI of ready status changes
		OnPlayerReadyStatusChanged?.Invoke(new Dictionary<string, bool>(playerReadyStatus));

		// Check if all players are ready
		bool allPlayersReady = (totalPlayers > 0 && readyCount == totalPlayers);

		// Only print and trigger events if the state changed
		if (allPlayersReady != lastAllPlayersReadyState)
		{
			lastAllPlayersReadyState = allPlayersReady;

			if (allPlayersReady)
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
	}

	public Dictionary<string, bool> GetPlayerReadyStatus()
	{
		return new Dictionary<string, bool>(playerReadyStatus);
	}

	public bool IsLobbyOwner()
	{
		if (hostedLobby.Id != 0)
		{
			return hostedLobby.Owner.Name == PlayerName;
		}
		return false;
	}

	public void StartGameForAllPlayers()
	{
		if (hostedLobby.Id != 0 && IsLobbyOwner())
		{
			GD.Print("Host starting game for all players");
			hostedLobby.SetData("game_start", "true");
		}
	}

	// ===== PAUSE SYSTEM =====
	public void SetPauseVote(bool vote)
	{
		if (hostedLobby.Id == 0)
		{
			// Single-player fallback: directly fire event
			lastPausedState = vote;
			lastPauseInitiator = PlayerName;
			OnPauseStateChanged?.Invoke(vote, PlayerName, vote ? 1 : 0, 1);
			return;
		}
		// Ignore new pause votes while already paused (but allow clearing vote=false)
		if (hostedLobby.GetData("game_paused") == "true" && vote) return;
		GD.Print($"[PauseVote] vote={vote} paused={hostedLobby.GetData("game_paused")} members={hostedLobby.MemberCount}");
		// Single-member direct pause/unpause handling
		if (hostedLobby.MemberCount <= 1 && vote)
		{
			hostedLobby.SetData("game_paused", "true");
			hostedLobby.SetData("pause_initiator", PlayerName);
			hostedLobby.SetData("pause_vote_count", "1");
			lastPausedState = true;
			lastPauseInitiator = PlayerName;
			OnPauseStateChanged?.Invoke(true, PlayerName, 1, 1);
			return;
		}
		// OLD SYSTEM - Use TogglePausePhaseVote instead
		GD.PrintErr("SetPauseVote is deprecated. Use TogglePausePhaseVote instead.");
	}

	public void SetResumeVote(bool vote)
	{
		if (hostedLobby.Id == 0)
		{
			// single player immediate resume
			if (lastPausedState && vote)
			{
				lastPausedState = false;
				OnPauseStateChanged?.Invoke(false, "", 0, 1);
			}
			return;
		}
		if (hostedLobby.GetData("game_paused") != "true") return; // only valid when paused

		// OLD SYSTEM - Use TogglePausePhaseVote instead
		GD.PrintErr("SetResumeVote is deprecated. Use TogglePausePhaseVote instead.");
	}

	private void ClearAllMemberVotes()
	{
		if (hostedLobby.Id == 0) return;
		foreach (Friend m in hostedLobby.Members)
		{
			hostedLobby.SetMemberData("vote", "0");
		}
	}

	private void RecalculatePhase(bool pausedPhase)
	{
		// pausedPhase true => counting resume votes. false => counting pause votes.
		if (hostedLobby.Id == 0) return;
		int total = hostedLobby.MemberCount;
		int votes = 0;
		foreach (Friend m in hostedLobby.Members)
		{
			if (hostedLobby.GetMemberData(m, "vote") == "1") votes++;
		}
		bool threshold = votes > total / 2; // strict majority
		hostedLobby.SetData(pausedPhase ? "resume_vote_count" : "pause_vote_count", votes.ToString());
		if (threshold)
		{
			bool newPaused = !pausedPhase; // if we were unpaused (pausedPhase==false) we now pause; else resume
			hostedLobby.SetData("game_paused", newPaused ? "true" : "false");
			hostedLobby.SetData("pause_initiator", newPaused ? PlayerName : "");

			// Clear all vote counts when state changes
			hostedLobby.SetData("pause_vote_count", "0");
			hostedLobby.SetData("resume_vote_count", "0");

			ClearAllMemberVotes();
			lastPhaseVotes = 0;
		}
		else
		{
			lastPhaseVotes = votes;
		}
	}
	// Position synchronization methods



	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationWMCloseRequest)
		{
			SteamClient.Shutdown();
			GetTree().Quit();
		}
	}
	public void CreateSteamSocketServer()
	{
		SteamSocketManager = SteamNetworkingSockets.CreateRelaySocket<SteamSocketManager>(0);
		SteamConnectionManager = SteamNetworkingSockets.ConnectRelay<SteamConnectionManager>(PlayerSteamID, 0);

		IsHost = true;
		GD.Print("Steam socket server created successfully");

	}
	 public void JoinSteamSocketServer(SteamId host){
        if(!IsHost){
            GD.Print("Joining Socket Server!");
            SteamConnectionManager = SteamNetworkingSockets.ConnectRelay<SteamConnectionManager>(host, 0);
        }
    }

    public void Broadcast(string data){
		GD.Print(data);
        foreach (var item in SteamSocketManager.Connected.Skip(1).ToArray())
		{
			item.SendMessage(data);
		}
    }
}
