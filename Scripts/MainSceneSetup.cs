using Godot;
using System;

public partial class MainSceneSetup : Node
{
	public override void _Ready()
	{
		GD.Print("MainSceneSetup _Ready called");
		
		// Create and add GameManager to the scene
		var gameManager = new GameManager();
		gameManager.Name = "GameManager";
		GetParent().AddChild(gameManager);
		
		GD.Print("GameManager added to main scene");
	}
}
