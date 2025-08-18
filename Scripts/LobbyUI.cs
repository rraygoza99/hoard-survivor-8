using Godot;
using System;
using System.Collections.Generic;

public partial class LobbyUI : Control
{
    [Export] public PackedScene PlayerListItemScene;
    private VBoxContainer _playerListContainer;
    private Button _readyButton;
    private Label _readyStatusLabel;
    private Label _countdownLabel;
    private Timer _countdownTimer;
    private Dictionary<string, bool> _currentReadyStatus = new Dictionary<string, bool>();
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
        SetupReadyButton();
        
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
        SteamManager.OnAllPlayersReady += OnAllPlayersReady;
        SteamManager.OnNotAllPlayersReady += OnNotAllPlayersReady;
        SteamManager.OnGameStartSignaled += OnGameStartSignaled;
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
    
    private void SetupReadyButton()
    {
        // Try to find existing ready button or create one
        _readyButton = GetNodeOrNull<Button>("ReadyButton");
        _readyStatusLabel = GetNodeOrNull<Label>("ReadyButton/ReadyStatusLabel");
        if (_readyButton == null)
        {
            GD.Print("ReadyButton not found, creating one programmatically");
            _readyButton = new Button();
            _readyButton.Name = "ReadyButton";
            _readyButton.Text = ""; // We'll use internal label for precise centering
            _readyButton.Size = new Vector2(140, 40); // Wider so both "Ready" and "Not Ready" stay centered
            
            // Position it in bottom right
            _readyButton.AnchorLeft = 1.0f;
            _readyButton.AnchorRight = 1.0f;
            _readyButton.AnchorTop = 1.0f;
            _readyButton.AnchorBottom = 1.0f;
            _readyButton.OffsetLeft = -120;
            _readyButton.OffsetRight = -20;
            _readyButton.OffsetTop = -60;
            _readyButton.OffsetBottom = -20;
            
            AddChild(_readyButton);
        }
        else
        {
            // Ensure consistent size if it already existed
            _readyButton.CustomMinimumSize = new Vector2(140, 40);
        }

        // If we rely on an internal label for text, ensure it exists & centered
        if (_readyStatusLabel == null)
        {
            _readyStatusLabel = new Label();
            _readyStatusLabel.Name = "ReadyStatusLabel";
            _readyStatusLabel.Text = "Ready";
            // Fill the button area
            _readyStatusLabel.AnchorLeft = 0; _readyStatusLabel.AnchorTop = 0; _readyStatusLabel.AnchorRight = 1; _readyStatusLabel.AnchorBottom = 1;
            _readyStatusLabel.OffsetLeft = 0; _readyStatusLabel.OffsetTop = 0; _readyStatusLabel.OffsetRight = 0; _readyStatusLabel.OffsetBottom = 0;
            _readyStatusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _readyStatusLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _readyStatusLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _readyStatusLabel.VerticalAlignment = VerticalAlignment.Center;
            _readyButton.AddChild(_readyStatusLabel);
        }
        else
        {
            _readyStatusLabel.AnchorLeft = 0; _readyStatusLabel.AnchorTop = 0; _readyStatusLabel.AnchorRight = 1; _readyStatusLabel.AnchorBottom = 1;
            _readyStatusLabel.OffsetLeft = 0; _readyStatusLabel.OffsetTop = 0; _readyStatusLabel.OffsetRight = 0; _readyStatusLabel.OffsetBottom = 0;
            _readyStatusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _readyStatusLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _readyStatusLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _readyStatusLabel.VerticalAlignment = VerticalAlignment.Center;
            _readyStatusLabel.Text = "Ready";
        }
        
        _readyButton.Pressed += OnReadyButtonPressed;
        GD.Print("Ready button setup complete");
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
    
    private void OnReadyButtonPressed()
    {
        if (SteamManager.Manager == null) return;
        SteamManager.Manager.SteamConnectionManager.Connection.SendMessage("test");
        bool newReadyState = !SteamManager.Manager.IsLocalPlayerReady;
        SteamManager.Manager.SetPlayerReady(newReadyState);
        
        _readyStatusLabel.Text = newReadyState ? "Not Ready" : "Ready";
        
        _readyButton.Modulate = newReadyState ? Colors.Green : Colors.White;
        
        
        GD.Print($"Local player ready status changed to: {newReadyState}");
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
            GameData.SetLobbyPlayers(memberNames);
            
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
        List<string> memberNames = SteamManager.Manager.GetLobbyMemberNames();
        GD.Print($"Retrieved {memberNames.Count} member names");
        
        // Check if PlayerListItemScene is assigned
        if (PlayerListItemScene == null)
        {
            GD.PrintErr("PlayerListItemScene is null! Make sure to assign it in the inspector");
            return;
        }
        
        // Create player list items
        foreach (string memberName in memberNames)
        {
            GD.Print($"Creating PlayerListItem for: {memberName}");
            
            try
            {
                PlayerListItem playerListItem = null;
                
                if (PlayerListItemScene != null)
                {
                    GD.Print("Step 1: About to instantiate PlayerListItemScene");
                    playerListItem = PlayerListItemScene.Instantiate<PlayerListItem>();
                    GD.Print("Step 2: PlayerListItem instantiated from scene");
                }
                else
                {
                    GD.Print("PlayerListItemScene is null, creating enhanced fallback");
                    // Create a nicer fallback PlayerListItem with ready status
                    var container = new PanelContainer();
                    var styleBox = new StyleBoxFlat();
                    styleBox.BgColor = new Color(0.2f, 0.2f, 0.3f, 0.8f);
                    styleBox.CornerRadiusTopLeft = 5;
                    styleBox.CornerRadiusTopRight = 5;
                    styleBox.CornerRadiusBottomLeft = 5;
                    styleBox.CornerRadiusBottomRight = 5;
                    container.AddThemeStyleboxOverride("panel", styleBox);
                    
                    // Create horizontal container for name and checkmark
                    var hContainer = new HBoxContainer();
                    
                    var label = new Label();
                    label.Text = memberName;
                    label.Name = "PlayerNameLabel";
                    label.AddThemeColorOverride("font_color", Colors.White);
                    label.VerticalAlignment = VerticalAlignment.Center;
                    label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                    
                    var checkmark = new Label();
                    checkmark.Name = "ReadyCheckmark";
                    checkmark.Text = _currentReadyStatus.ContainsKey(memberName) && _currentReadyStatus[memberName] ? "✓" : "";
                    checkmark.AddThemeColorOverride("font_color", Colors.Green);
                    checkmark.HorizontalAlignment = HorizontalAlignment.Center;
                    checkmark.VerticalAlignment = VerticalAlignment.Center;
                    checkmark.CustomMinimumSize = new Vector2(30, 0);
                    
                    hContainer.AddChild(label);
                    hContainer.AddChild(checkmark);
                    
                    // Add some padding
                    var margin = new MarginContainer();
                    margin.AddThemeConstantOverride("margin_left", 10);
                    margin.AddThemeConstantOverride("margin_right", 10);
                    margin.AddThemeConstantOverride("margin_top", 5);
                    margin.AddThemeConstantOverride("margin_bottom", 5);
                    
                    margin.AddChild(hContainer);
                    container.AddChild(margin);
                    _playerListContainer.AddChild(container);
                    GD.Print($"Created enhanced fallback item for: {memberName} (Ready: {(_currentReadyStatus.ContainsKey(memberName) && _currentReadyStatus[memberName])})");
                    continue;
                }
                
                if (playerListItem == null)
                {
                    GD.PrintErr("PlayerListItem is null after instantiation");
                    continue;
                }
                
                GD.Print("Step 3: About to call SetPlayerName");
                playerListItem.SetPlayerName(memberName);
                
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
