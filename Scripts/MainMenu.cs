using Godot;
using System;
using System.Threading.Tasks;

public partial class MainMenu : Control
{
    [Export] public PackedScene JoinLobbyPopupScene { get; set; }
    
    public override void _Ready()
    {
        // Add debug info to see what's happening
        GD.Print("=== MainMenu _Ready called ===");
        GD.Print($"Node name: {Name}");
        GD.Print($"Node path: {GetPath()}");
        
        // Check if buttons exist before connecting
        var hostButton = GetNodeOrNull<Button>("VBoxContainer/HostButton");
        var joinButton = GetNodeOrNull<Button>("VBoxContainer/JoinButton");
        var quitButton = GetNodeOrNull<Button>("VBoxContainer/QuitButton");
        
        if (hostButton != null)
        {
            GD.Print("Host button found, connecting signal");
            hostButton.Pressed += _on_host_button_pressed;
        }
        else
        {
            GD.PrintErr("Host button not found at path: VBoxContainer/HostButton");
            // Try to find all buttons in the scene
            PrintAllButtons();
        }
        
        if (joinButton != null)
        {
            GD.Print("Join button found, connecting signal");
            joinButton.Pressed += OnJoinButtonPressed;
        }
        else
        {
            GD.PrintErr("Join button not found at path: VBoxContainer/JoinButton");
        }
        
        if (quitButton != null)
        {
            GD.Print("Quit button found, connecting signal");
            quitButton.Pressed += on_quit_button_pressed;
        }
        else
        {
            GD.PrintErr("Quit button not found at path: VBoxContainer/QuitButton");
        }
        
        GD.Print("=== MainMenu _Ready finished ===");
    }
    
    private void PrintAllButtons()
    {
        GD.Print("=== Searching for buttons in scene ===");
        var buttons = GetTree().GetNodesInGroup("ui_buttons");
        if (buttons.Count == 0)
        {
            // Try to find buttons recursively
            FindButtonsRecursively(this, "");
        }
    }
    
    private void FindButtonsRecursively(Node node, string path)
    {
        foreach (Node child in node.GetChildren())
        {
            string childPath = path + "/" + child.Name;
            if (child is Button)
            {
                GD.Print($"Found button at: {childPath}");
            }
            FindButtonsRecursively(child, childPath);
        }
    }
    private async void _on_host_button_pressed()
    {
        GD.Print("=== HOST BUTTON PRESSED! ===");
        
        // Check if SteamManager.Manager exists
        if (SteamManager.Manager == null)
        {
            GD.PrintErr("SteamManager.Manager is null! Steam may not be initialized.");
            return;
        }
        
        // Check if Steam is initialized
        if (!SteamManager.Manager.IsSteamInitialized)
        {
            GD.PrintErr("Steam is not initialized!");
            return;
        }
        
        GD.Print("Creating lobby...");
        try
        {
            bool success = await SteamManager.Manager.CreateLobby();
            if (success)
            {
                GD.Print("Lobby created successfully!");
                string lobbyId = SteamManager.Manager.GetCurrentLobbyId();
                GD.Print($"Lobby ID: {lobbyId}");
            }
            else
            {
                GD.PrintErr("Failed to create lobby");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Exception while creating lobby: {e.Message}");
        }
        
        GD.Print("=== HOST BUTTON FINISHED ===");
    }
    private void OnJoinButtonPressed()
    {
        GD.Print("Join button pressed - showing join lobby popup");
        
        // Load and show the join lobby popup
        PackedScene popupScene = null;
        
        if (JoinLobbyPopupScene != null)
        {
            popupScene = JoinLobbyPopupScene;
        }
        else
        {
            // Try to load the popup scene directly
            popupScene = GD.Load<PackedScene>("res://UtilityScenes/join_lobby_popup.tscn");
        }
        
        if (popupScene != null)
        {
            var popup = popupScene.Instantiate<AcceptDialog>();
            GetTree().CurrentScene.AddChild(popup);
            popup.PopupCentered();
            GD.Print("Join lobby popup shown");
        }
        else
        {
            GD.PrintErr("Could not load join lobby popup scene");
        }
    }
    private void on_quit_button_pressed()
    {
        GD.Print("Quit button pressed");
        GetTree().Quit();
    }

}
