using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerUi : Control
{
	private LineEdit _lobbyIdInput;
	private Label _memberCountLabel;
	
	public override void _Ready()
	{
		_lobbyIdInput = GetNode<LineEdit>("LobbyIdInput");
		
		// Try to get member count label (optional, in case you don't have it in the scene yet)
		_memberCountLabel = GetNodeOrNull<Label>("MemberCountLabel");
		
		
		
		// Add a test button if it exists in your scene
		var testButton = GetNodeOrNull<Button>("TestButton");
		if (testButton != null)
		{
			testButton.Pressed += _on_test_button_pressed;
		}
		
	}

	private async void _on_host_button_pressed()
	{
	
	}
	private async void _on_join_button_pressed()
	{

	}
	
	private void _on_test_button_pressed()
	{

	}
	
	public override void _ExitTree()
	{
	}
}
