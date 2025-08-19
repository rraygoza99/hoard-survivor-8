using Godot;
using System;

public partial class SceneManager : Node
{
    public override void _Ready()
    {
        GameManager.SceneManager = this;
    }

    public void GoToScene(string path)
    {
        var current = GetTree().CurrentScene;
        if (current != null)
            GD.Print("Previous scene: " + current.Name);
        GetTree().ChangeSceneToFile(path);
        var newScene = GetTree().CurrentScene;
        if (newScene != null)
            GD.Print("New scene: " + newScene.Name);
    }
}
