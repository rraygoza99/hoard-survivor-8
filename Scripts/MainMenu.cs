using Godot;
using System;
using System.Threading.Tasks;

public partial class MainMenu : Control
{
    [Export] public PackedScene JoinLobbyPopupScene { get; set; } // legacy, unused now
    
    public override void _Ready()
    {
        SteamManager.InitializeSteam();
        // Add debug info to see what's happening
        GD.Print("=== MainMenu _Ready called ===");
        GD.Print($"Node name: {Name}");
        GD.Print($"Node path: {GetPath()}");
        
        // Check if buttons exist before connecting
    var hostButton = GetNodeOrNull<Button>("VBoxContainer/HostButton");
    var joinButton = GetNodeOrNull<Button>("VBoxContainer/JoinButton"); // legacy
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
    private void _on_host_button_pressed()
    {
        GD.Print("Start Game (single-player) button pressed");
        GetTree().ChangeSceneToFile("res://main.tscn");
    }
    private void OnJoinButtonPressed()
    {
        GD.Print("Join (legacy) pressed - disabled in single-player mode");
    }
    private void on_quit_button_pressed()
    {
        GD.Print("Quit button pressed");
        GetTree().Quit();
    }

}
