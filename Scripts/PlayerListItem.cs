using Godot;
using System;

public partial class PlayerListItem : PanelContainer
{
    private Label _playerNameLabel;
    private RichTextLabel _readyStatusLabel;
    public override void _Ready()
    {
        GD.Print("PlayerListItem _Ready called");

        // Initialize player name label - try both possible names
        _playerNameLabel = GetNodeOrNull<Label>("PlayerNameLabel");
        _readyStatusLabel = GetNodeOrNull<RichTextLabel>("Ready");
        if (_playerNameLabel == null)
        {
            _playerNameLabel = GetNodeOrNull<Label>("PlayerLabelName");
        }

        if (_playerNameLabel == null)
        {
            GD.PrintErr("Neither PlayerNameLabel nor PlayerLabelName found in PlayerListItem");
            GD.Print("Available children:");
            foreach (Node child in GetChildren())
            {
                GD.Print($"  - {child.Name} ({child.GetType().Name})");
            }
            return;
        }

        GD.Print($"PlayerListItem is ready with player label initialized: {_playerNameLabel.Name}");
    }
    public void SetPlayerName(string name)
    {
        GD.Print($"SetPlayerName called with: {name}");

        if (_playerNameLabel == null)
        {
            GD.PrintErr("_playerNameLabel is null in SetPlayerName - trying to find it again");
            _playerNameLabel = GetNodeOrNull<Label>("PlayerNameLabel");
            if (_playerNameLabel == null)
            {
                _playerNameLabel = GetNodeOrNull<Label>("PlayerLabelName");
            }

            if (_playerNameLabel == null)
            {
                GD.PrintErr("Could not find any label node for player name");
                return;
            }
        }

        GD.Print($"Setting player name label text to: {name}");
        _playerNameLabel.Text = name;
        GD.Print("Player name set successfully");
    }
    public void SetReadyStatus(bool ready){
        if(ready){
            _readyStatusLabel.Text = "Ready";
        } else{
             _readyStatusLabel.Text = "Not Ready";
        }
    }
}
