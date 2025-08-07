using Godot;
using Steamworks.Data;
using System;

public partial class LobbyItem : Control
{
    public Lobby lobby { get; set; }
    public override void _Ready()
    {
        GD.Print("LobbyItem is ready");
    }
    public void SetLabels(string id, string name, Lobby lobby)
    {
        this.lobby = lobby;
        GetNode<RichTextLabel>("ID").Text = id;
        GetNode<RichTextLabel>("LobbyName").Text = name;
        this.lobby = lobby;
    }
    public void _on_join_button_pressed()
    {
        lobby.Join();
    }
}
