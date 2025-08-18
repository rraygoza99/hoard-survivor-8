using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public partial class ChatBox : Control
{
    public override void _Ready()
    {
        DataParser.OnChatMessage += OnChatMessageCallback;
    }
    private void _on_button_button_down(){
        GD.Print("Button pressed");
        Dictionary<string, string> dict = new Dictionary<string, string>();
        dict.Add("DataType","ChatMessage");
        dict.Add("UserID", SteamManager.Manager.PlayerName);
        dict.Add("Message", GetNode<LineEdit>("LineEdit").Text);
        
        //OnChatMessageCallback(dict);
        string json = JsonConvert.SerializeObject(dict);
        GD.Print("Is host? " + SteamManager.Manager.IsHost);
        if(SteamManager.Manager.IsHost){
            SteamManager.Manager.Broadcast(json);
        }else{
            SteamManager.Manager.SteamConnectionManager.Connection.SendMessage(json);
        }
    }

    private void OnChatMessageCallback(Dictionary<string,string> data){
        GD.Print("Chat message received: " + data["Message"]);
        GetNode<RichTextLabel>("ChatBox").Text = GetNode<RichTextLabel>("ChatBox").Text + System.Environment.NewLine + data["UserID"] + ": " + data["Message"];
    }
}
