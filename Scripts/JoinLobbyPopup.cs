using Godot;
using System;
using System.Threading.Tasks;

public partial class JoinLobbyPopup : AcceptDialog
{
	private LineEdit _lobbyIdLineEdit;
	private Button _joinButton;
	private Button _cancelButton;
	
	public override void _Ready()
	{
		GD.Print("JoinLobbyPopup _Ready called");
		
		// Get references to UI elements
		_lobbyIdLineEdit = GetNode<LineEdit>("VBoxContainer/LobbyIdLineEdit");
		_joinButton = GetNode<Button>("VBoxContainer/ButtonContainer/JoinButton");
		_cancelButton = GetNode<Button>("VBoxContainer/ButtonContainer/CancelButton");
		
		// Connect signals programmatically as backup
		_joinButton.Pressed += _on_join_button_pressed;
		_cancelButton.Pressed += _on_cancel_button_pressed;
		
		// Focus on the text input when popup opens
		_lobbyIdLineEdit.GrabFocus();
		
		// Allow Enter key to trigger join
		_lobbyIdLineEdit.TextSubmitted += OnLobbyIdSubmitted;
	}
	
	private void OnLobbyIdSubmitted(string lobbyId)
	{
		// Trigger join when Enter is pressed in the text field
		_on_join_button_pressed();
	}
	
	private async void _on_join_button_pressed()
	{
		var lobbyId = _lobbyIdLineEdit.Text.Trim();
		
		if (string.IsNullOrEmpty(lobbyId))
		{
			GD.Print("No lobby ID entered");
			ShowError("Please enter a lobby ID");
			return;
		}
		
		GD.Print($"Attempting to join lobby: {lobbyId}");
		
		// Check if SteamManager exists
		if (SteamManager.Manager == null)
		{
			GD.PrintErr("SteamManager.Manager is null!");
			ShowError("Steam is not initialized");
			return;
		}
		
		if (!SteamManager.Manager.IsSteamInitialized)
		{
			GD.PrintErr("Steam is not initialized!");
			ShowError("Steam is not initialized");
			return;
		}
		
		// Disable the join button while attempting to join
		_joinButton.Disabled = true;
		_joinButton.Text = "Joining...";
		
		try
		{
			// Attempt to join the lobby
			bool success = await SteamManager.Manager.JoinLobby(lobbyId);
			
			if (success)
			{
				GD.Print($"Successfully joined lobby: {lobbyId}");
				
				// Close the popup
				Hide();
				
				// Transition to lobby scene (you might need to adjust this path)
				GetTree().ChangeSceneToFile("res://UtilityScenes/lobby.tscn");
			}
			else
			{
				GD.PrintErr($"Failed to join lobby: {lobbyId}");
				ShowError("Failed to join lobby. Check the lobby ID and try again.");
				
				// Re-enable the button
				_joinButton.Disabled = false;
				_joinButton.Text = "Join";
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Exception while joining lobby: {e.Message}");
			ShowError($"Error joining lobby: {e.Message}");
			
			// Re-enable the button
			_joinButton.Disabled = false;
			_joinButton.Text = "Join";
		}
	}
	
	private void _on_cancel_button_pressed()
	{
		GD.Print("Join lobby cancelled");
		Hide();
	}
	
	private void ShowError(string message)
	{
		// Create a simple error dialog
		var errorDialog = new AcceptDialog();
		errorDialog.DialogText = message;
		errorDialog.Title = "Error";
		GetParent().AddChild(errorDialog);
		errorDialog.PopupCentered();
		
		// Auto-remove the dialog when closed
		errorDialog.Confirmed += () => errorDialog.QueueFree();
		errorDialog.Canceled += () => errorDialog.QueueFree();
	}
}
