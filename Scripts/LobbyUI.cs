using Godot;
using Newtonsoft.Json;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LobbyUI : Control
{
    [Export] public PackedScene PlayerListItemScene;
    private VBoxContainer _playerListContainer;
    private Button _readyButton;
    private Label _readyStatusLabel;
    private Label _countdownLabel;
    private Timer _countdownTimer;
    public List<Player> CurrentPlayers = new List<Player>();
    private Dictionary<string, bool> _currentReadyStatus = new Dictionary<string, bool>();
	public bool IsPlayerReady { get; set; }

    public override void _Ready()
    {
        GD.Print("=== LobbyUI _Ready called ===");

        _playerListContainer = GetNode<VBoxContainer>("PlayerListContainer");
        if (_playerListContainer == null)
        {
            GD.PrintErr("PlayerListContainer not found in Lobby scene");
            return;
        }
        GD.Print("PlayerListContainer found successfully");

        // Setup ready button

        // Setup countdown label and timer
        SetupCountdown();

        // Check if PlayerListItemScene is assigned
        if (PlayerListItemScene == null)
        {
            GD.PrintErr("PlayerListItemScene is null! Make sure to assign it in the inspector");
        }
        else
        {
            GD.Print("PlayerListItemScene is assigned");
        }

        // Subscribe to events
        SteamManager.OnLobbyMemberCountChanged += RefreshPlayerList;
        SteamManager.OnPlayerReadyStatusChanged += OnPlayerReadyStatusChanged;
        SteamManager.OnPlayerJoinLobby += OnPlayerJoinLobbyCallback;
        SteamManager.OnAllPlayersReady += OnAllPlayersReady;
        SteamManager.OnNotAllPlayersReady += OnNotAllPlayersReady;
        SteamManager.OnGameStartSignaled += OnGameStartSignaled;
        DataParser.OnReadyMessage += OnPlayerReadyMessageCallback;
        GD.Print("Subscribed to Steam events");

        // Initial refresh
        if (SteamManager.Manager != null)
        {
            int currentCount = SteamManager.Manager.CurrentLobbyMemberCount;
            GD.Print($"Initial lobby member count: {currentCount}");
            RefreshPlayerList(currentCount);
        }
        else
        {
            GD.PrintErr("SteamManager.Manager is null in LobbyUI _Ready");
        }

        GD.Print("=== LobbyUI _Ready finished ===");
    }
    public override void _ExitTree()
    {
        // It's good practice to unsubscribe from events when the node is destroyed.
        SteamManager.OnLobbyMemberCountChanged -= RefreshPlayerList;
        SteamManager.OnPlayerReadyStatusChanged -= OnPlayerReadyStatusChanged;
        SteamManager.OnAllPlayersReady -= OnAllPlayersReady;
        SteamManager.OnNotAllPlayersReady -= OnNotAllPlayersReady;
        SteamManager.OnGameStartSignaled -= OnGameStartSignaled;

        if (_countdownTimer != null)
        {
            _countdownTimer.Timeout -= OnCountdownTimeout;
        }
    }
    private void OnPlayerJoinLobbyCallback(Friend friend){
        GD.Print($"Player joined lobby: {friend.Name} (ID: {friend.Id.AccountId})");
        Player p = new Player();
        p.FriendData = friend;
        CurrentPlayers.Add(p);
        var element = PlayerListItemScene.Instantiate() as PlayerListItem;
        element.Name = friend.Id.AccountId.ToString(); 
        element.SetPlayerName(friend.Name);
        GetNode<VBoxContainer>("Lobby Users").AddChild(element);
        GameManager.OnPlayerJoinLobby(friend);
    }

    private void SetupCountdown()
    {
        // Try to find existing countdown label or create one
        _countdownLabel = GetNodeOrNull<Label>("CountdownLabel");

        if (_countdownLabel == null)
        {
            GD.Print("CountdownLabel not found, creating one programmatically");
            _countdownLabel = new Label();
            _countdownLabel.Name = "CountdownLabel";
            _countdownLabel.Text = "";
            _countdownLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _countdownLabel.VerticalAlignment = VerticalAlignment.Center;

            // Position it in center
            _countdownLabel.AnchorLeft = 0.5f;
            _countdownLabel.AnchorRight = 0.5f;
            _countdownLabel.AnchorTop = 0.5f;
            _countdownLabel.AnchorBottom = 0.5f;
            _countdownLabel.OffsetLeft = -100;
            _countdownLabel.OffsetRight = 100;
            _countdownLabel.OffsetTop = -50;
            _countdownLabel.OffsetBottom = 50;

            // Style the countdown label
            _countdownLabel.AddThemeColorOverride("font_color", Colors.Yellow);
            _countdownLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat());

            AddChild(_countdownLabel);
        }

        // Setup countdown timer
        _countdownTimer = new Timer();
        _countdownTimer.WaitTime = 1.0f;
        _countdownTimer.Timeout += OnCountdownTimeout;
        AddChild(_countdownTimer);

        _countdownLabel.Visible = false;
        GD.Print("Countdown setup complete");
    }

    

    private void OnPlayerReadyStatusChanged(Dictionary<string, bool> readyStatus)
    {
        _currentReadyStatus = readyStatus;
        GD.Print("Player ready status updated, refreshing player list");

        // Refresh the player list to show checkmarks
        if (SteamManager.Manager != null)
        {
            RefreshPlayerList(SteamManager.Manager.CurrentLobbyMemberCount);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
        {
            GD.Print("ESC pressed in lobby - returning to main menu");
            ReturnToMainMenu();
            GetViewport().SetInputAsHandled();
        }
    }
    private void _on_return_button_pressed()
    {
        ReturnToMainMenu();
    }

    private void _on_ready_button_pressed()
    {
        GD.Print("Ready button pressed, toggling player ready state");
        IsPlayerReady = !IsPlayerReady;
        Dictionary<string, string> playerDict = new Dictionary<string, string>();
        playerDict.Add("DataType", "Ready");
        playerDict.Add("PlayerName", SteamManager.Manager.PlayerSteamID.AccountId.ToString());
        playerDict.Add("IsReady", IsPlayerReady.ToString());
        string str = JsonConvert.SerializeObject(playerDict);
        OnPlayerReadyMessageCallback(playerDict);
        
        _readyStatusLabel.Text = IsPlayerReady ? "Not Ready" : "Ready";

        _readyButton.Modulate = IsPlayerReady ? Colors.Green : Colors.White;
        if (SteamManager.Manager.IsHost)
        {
            SteamManager.Manager.Broadcast(str);
        }
        else
        {
            SteamManager.Manager.SteamConnectionManager.Connection.SendMessage(str);
        }
    }
    private void OnAllPlayersReady()
    {
        GD.Print("All players ready - starting countdown!");
        StartCountdown();
    }

    private void OnNotAllPlayersReady()
    {
        GD.Print("Not all players are ready - stopping countdown!");
        StopCountdown();
    }

    private int _countdownValue = 3;

    private void StartCountdown()
    {
        _countdownValue = 5;
        _countdownLabel.Text = $"Starting in {_countdownValue}...";
        _countdownLabel.Visible = true;
        _countdownTimer.Start();
    }

    private void StopCountdown()
    {
        if (_countdownTimer.IsStopped() == false)
        {
            _countdownTimer.Stop();
            _countdownLabel.Visible = false;
            GD.Print("Countdown stopped - not all players are ready");
        }
    }

    private void OnCountdownTimeout()
    {
        _countdownValue--;

        if (_countdownValue > 0)
        {
            _countdownLabel.Text = $"Starting in {_countdownValue}...";
        }
        else
        {
            _countdownLabel.Text = "Starting now!";
            _countdownTimer.Stop();

            // Only the host should signal game start to all players
            if (SteamManager.Manager != null && SteamManager.Manager.IsLobbyOwner())
            {
                GD.Print("Host triggering game start for all players");
                SteamManager.Manager.StartGameForAllPlayers();
            }

            // Hide countdown after a moment and start the game (for host)
            GetTree().CreateTimer(1.0).Timeout += () => {
                _countdownLabel.Visible = false;
                if (SteamManager.Manager != null && SteamManager.Manager.IsLobbyOwner())
                {
                    StartGame();
                }
            };
        }
    }

    private void OnGameStartSignaled()
    {
        GD.Print("Received game start signal from host!");
        // Stop countdown if running
        if (_countdownTimer != null)
        {
            _countdownTimer.Stop();
        }

        // Hide countdown label
        if (_countdownLabel != null)
        {
            _countdownLabel.Visible = false;
        }

        // Start the game for this client
        StartGame();
    }

    private void StartGame()
    {
        GD.Print("Starting the game!");

        // Get all lobby members before transitioning
        if (SteamManager.Manager != null)
        {
            var memberNames = SteamManager.Manager.GetLobbyMemberNames();
            GD.Print($"Starting game with {memberNames.Count} players: {string.Join(", ", memberNames)}");

            // Store player data for the game scene
            GameData.SetLobbyPlayers(memberNames.Values.ToList());

            // Additional debug
            GD.Print($"GameData after setting: {GameData.LobbyPlayerNames.Count} players");
        }
        else
        {
            GD.Print("SteamManager.Manager is null, cannot get lobby members");
        }

        // Transition to the main game scene
        GetTree().ChangeSceneToFile("res://main.tscn");
    }

    private void ReturnToMainMenu()
    {
        // Best-effort: clear lobby data flag so others know we left
        if (SteamManager.Manager != null)
        {
            GD.Print("Returning to main menu: clearing ready status");
            // Mark not ready to inform others (if still in lobby)
            SteamManager.Manager.SetPlayerReady(false);
        }
        GetTree().ChangeSceneToFile("res://UtilityScenes/main_menu.tscn");
    }
    private void OnPlayerReadyMessageCallback(Dictionary<string, string> dict)
    {
        GD.Print($"Player ready message received: {dict["PlayerName"]} is ready: {dict["IsReady"]}");
        GetNode<PlayerListItem>("PlayerListContainer/" + dict["PlayerName"]).SetReadyStatus(bool.Parse(dict["IsReady"]));
        GameManager.OnPlayerReady(dict);
    }
    private void RefreshPlayerList(int memberCount)
    {
        GD.Print($"=== RefreshPlayerList called with memberCount: {memberCount} ===");

        // Clear existing items
        foreach (Node child in _playerListContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Check if SteamManager exists
        if (SteamManager.Manager == null)
        {
            GD.PrintErr("SteamManager.Manager is null in RefreshPlayerList");
            return;
        }

        // Get member names
        Dictionary<string, string> memberNames = SteamManager.Manager.GetLobbyMemberNames();
        GD.Print($"Retrieved {memberNames.Count} member names");

        // Check if PlayerListItemScene is assigned
        if (PlayerListItemScene == null)
        {
            GD.PrintErr("PlayerListItemScene is null! Make sure to assign it in the inspector");
            return;
        }

        // Create player list items
        foreach (KeyValuePair<string, string> kvp in memberNames)
        {
            string memberId = kvp.Key;
            string memberName = kvp.Value;

            GD.Print($"Creating PlayerListItem for: {memberName}");

            try
            {
                PlayerListItem playerListItem = null;


                GD.Print("Step 1: About to instantiate PlayerListItemScene");
                playerListItem = PlayerListItemScene.Instantiate<PlayerListItem>();
                GD.Print("Step 2: PlayerListItem instantiated from scene");

                if (playerListItem == null)
                {
                    GD.PrintErr("PlayerListItem is null after instantiation");
                    continue;
                }

                GD.Print("Step 3: About to call SetPlayerName");
                playerListItem.SetPlayerName(memberName);
                playerListItem.Name = memberId;

                GD.Print("Step 4: About to add to container");
                _playerListContainer.AddChild(playerListItem);

                GD.Print($"Successfully added PlayerListItem for: {memberName}");
            }
            catch (Exception e)
            {
                GD.PrintErr($"Error creating PlayerListItem for {memberName}: {e.Message}");
                GD.PrintErr($"Stack trace: {e.StackTrace}");
            }
        }

        GD.Print($"=== RefreshPlayerList finished. Total children in container: {_playerListContainer.GetChildCount()} ===");
    }
}
