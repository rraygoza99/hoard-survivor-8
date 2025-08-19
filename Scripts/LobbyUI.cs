using Godot;
using System;

public partial class LobbyUI : Control
{
    public override void _Ready()
    {
        GD.Print("LobbyUI placeholder: multiplayer removed. Redirecting to main menu.");
        // If this scene still loads, just go back.
        CallDeferred(nameof(ReturnToMainMenu));
    }

    private void ReturnToMainMenu()
    {
        if (!IsInstanceValid(this)) return;
        if (GetTree().CurrentScene == this)
        {
            GetTree().ChangeSceneToFile("res://UtilityScenes/main_menu.tscn");
        }
        else
        {
            QueueFree();
        }
    }
}
