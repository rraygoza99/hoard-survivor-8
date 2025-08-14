using Godot;
using System;

public partial class PauseOverlay : Control
{
    private Label _statusLabel;
    private Label _votesLabel;
    private Button _resumeButton;

    public override void _Ready()
    {
    // Ensure this overlay continues to receive input & processing while the game tree is paused
    ProcessMode = ProcessModeEnum.WhenPaused;
    MouseFilter = MouseFilterEnum.Stop; // block underlying input when shown
        _statusLabel = GetNodeOrNull<Label>("Center/StatusLabel");
        _votesLabel = GetNodeOrNull<Label>("Center/VotesLabel");
        _resumeButton = GetNodeOrNull<Button>("Center/ResumeButton");
        if (_resumeButton != null)
        {
            _resumeButton.Pressed += OnResumePressed;
        }
        Hide();
    }

    public void UpdatePauseState(bool paused, string initiator, int votes, int total)
    {
        if (paused)
        {
            _statusLabel.Text = string.IsNullOrEmpty(initiator) ? "Paused" : $"Paused by {initiator}";
            _votesLabel.Text = $"Resume votes: {votes}/{total} (>75% to resume)";
            if (_resumeButton != null)
            {
                _resumeButton.Visible = true;
                _resumeButton.Text = "Resume";
                _resumeButton.Disabled = false;
            }
            Show();
        }
        else
        {
            if (_resumeButton != null)
                _resumeButton.Visible = false;
            Hide();
        }
    }

    private void OnResumePressed()
    {
        // Additional safety: ignore presses if not actually paused
        if (!GetTree().Paused) return;
        
        // Use unified vote system - this will toggle the current vote
        SteamManager.Manager?.TogglePausePhaseVote();
        
        // Update button text - since we're using unified system, we can't track individual resume vote state
        if (_resumeButton != null)
        {
            _resumeButton.Text = "Waiting for players to resume...";
        }
    }
}
