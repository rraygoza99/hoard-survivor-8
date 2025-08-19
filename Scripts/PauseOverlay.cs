using Godot;
using System;

public partial class PauseOverlay : Control
{
    private Label _statusLabel;
    private Label _votesLabel;
    private Button _resumeButton;
    private Button _optionsButton;
    private Button _quitToMenuButton;
    private Button _quitGameButton;
    
    // Confirmation dialog references
    private AcceptDialog _quitToMenuConfirmDialog;
    private AcceptDialog _quitGameConfirmDialog;

    public override void _Ready()
    {
        // Ensure this overlay continues to receive input & processing while the game tree is paused
        ProcessMode = ProcessModeEnum.WhenPaused;
        MouseFilter = MouseFilterEnum.Stop; // block underlying input when shown
        
        _statusLabel = GetNodeOrNull<Label>("Center/StatusLabel");
        _votesLabel = GetNodeOrNull<Label>("Center/VotesLabel");
        _resumeButton = GetNodeOrNull<Button>("Center/ResumeButton");
        _optionsButton = GetNodeOrNull<Button>("Center/OptionsButton");
        _quitToMenuButton = GetNodeOrNull<Button>("Center/QuitToMenuButton");
        _quitGameButton = GetNodeOrNull<Button>("Center/QuitGameButton");
        
        if (_resumeButton != null)
        {
            _resumeButton.Pressed += OnResumePressed;
        }
        
        if (_optionsButton != null)
        {
            _optionsButton.Pressed += OnOptionsPressed;
        }
        
        if (_quitToMenuButton != null)
        {
            _quitToMenuButton.Pressed += OnQuitToMenuPressed;
        }
        
        if (_quitGameButton != null)
        {
            _quitGameButton.Pressed += OnQuitGamePressed;
        }
        
        CreateConfirmationDialogs();
        Hide();
    }

    public void UpdatePauseState(bool paused, string initiator, int votes, int total)
    {
        if (paused)
        {
            _statusLabel.Text = string.IsNullOrEmpty(initiator) ? "Paused" : $"Paused by {initiator}";
            _votesLabel.Text = $"Resume votes: {votes}/{total} (>50% to resume)";
            
            if (_resumeButton != null)
            {
                _resumeButton.Visible = true;
                _resumeButton.Text = "Resume";
                _resumeButton.Disabled = false;
            }
            
            // Show additional buttons when paused
            if (_optionsButton != null)
                _optionsButton.Visible = true;
            if (_quitToMenuButton != null)
                _quitToMenuButton.Visible = true;
            if (_quitGameButton != null)
                _quitGameButton.Visible = true;
            
            Show();
        }
        else
        {
            if (_resumeButton != null)
                _resumeButton.Visible = false;
            if (_optionsButton != null)
                _optionsButton.Visible = false;
            if (_quitToMenuButton != null)
                _quitToMenuButton.Visible = false;
            if (_quitGameButton != null)
                _quitGameButton.Visible = false;
            
            Hide();
        }
    }

    private void OnResumePressed()
    {
        // Additional safety: ignore presses if not actually paused
        if (!GetTree().Paused) return;
        
        // Use unified vote system - this will toggle the current vote
        
        // Update button text - since we're using unified system, we can't track individual resume vote state
        if (_resumeButton != null)
        {
            _resumeButton.Text = "Vote Toggled";
        }
    }
    
    private void CreateConfirmationDialogs()
    {
        // Create quit to menu confirmation dialog
        _quitToMenuConfirmDialog = new AcceptDialog();
        _quitToMenuConfirmDialog.DialogText = "Are you sure you want to quit to main menu?\nAny progress made will be lost.";
        _quitToMenuConfirmDialog.Title = "Quit to Menu";
        _quitToMenuConfirmDialog.ProcessMode = ProcessModeEnum.WhenPaused;
        _quitToMenuConfirmDialog.OkButtonText = "Stay";
        _quitToMenuConfirmDialog.AddButton("Yes, Quit to Menu", true, "quit_to_menu");
        _quitToMenuConfirmDialog.CustomAction += OnQuitToMenuConfirmed;
        AddChild(_quitToMenuConfirmDialog);
        
        // Create quit game confirmation dialog
        _quitGameConfirmDialog = new AcceptDialog();
        _quitGameConfirmDialog.DialogText = "Are you sure you want to quit the game?\nAny progress made will be lost.";
        _quitGameConfirmDialog.Title = "Quit Game";
        _quitGameConfirmDialog.ProcessMode = ProcessModeEnum.WhenPaused;
        _quitGameConfirmDialog.OkButtonText = "Stay";
        _quitGameConfirmDialog.AddButton("Yes, Quit Game", true, "quit_game");
        _quitGameConfirmDialog.CustomAction += OnQuitGameConfirmed;
        AddChild(_quitGameConfirmDialog);
    }
    
    private void OnOptionsPressed()
    {
        GD.Print("Options button pressed - functionality not implemented yet");
        // TODO: Implement options menu
    }
    
    private void OnQuitToMenuPressed()
    {
        if (_quitToMenuConfirmDialog != null)
        {
            _quitToMenuConfirmDialog.PopupCentered();
        }
    }
    
    private void OnQuitGamePressed()
    {
        if (_quitGameConfirmDialog != null)
        {
            _quitGameConfirmDialog.PopupCentered();
        }
    }
    
    private void OnQuitToMenuConfirmed(StringName action)
    {
        if (action == "quit_to_menu")
        {
            GD.Print("Quitting to main menu...");
            
            // Clear any Steam lobby data if in multiplayer

            
            // Unpause the game before changing scenes
            GetTree().Paused = false;
            
            // Change to main menu scene
            GetTree().ChangeSceneToFile("res://UtilityScenes/main_menu.tscn");
        }
    }
    
    private void OnQuitGameConfirmed(StringName action)
    {
        if (action == "quit_game")
        {
            GD.Print("Quitting game...");
            GetTree().Quit();
        }
    }
}
