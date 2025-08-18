using Godot;
using Newtonsoft.Json;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;

public partial class SceneManager : Node
{
	[Export] public PackedScene LobbyElementScene { get; set; }
	[Export]
    public PackedScene LobbyPlayer;
    [Export]
    public PackedScene Player;

    private bool isPlayerReady;
	public override void _Ready()
	{
		SteamManager.OnLobbyRefreshCompleted += OnLobbyRefreshCompletedCallback;
		SteamManager.OnPlayerJoinLobby += OnPlayerJoinLobbyCallback;
		SteamManager.OnPlayerLeftLobby += OnPlayerLeftLobbyCallback;
		DataParser.OnReadyMessage += OnPlayerReadyMessageCallback;
		DataParser.OnGameStartMessage += OnStartGameCallback;
		GameManager.SceneManager = this;
	}
	private void OnLobbiesRefreshCompletedCallback(List<Lobby> lobbies)
	{
		GD.Print("Lobbies refreshed, count: " + lobbies.Count);
		foreach (var lobby in lobbies)
		{
			var element = LobbyElementScene.Instantiate<LobbyItem>();
			element.SetLabels(lobby.Id.ToString(), lobby.GetData("ownerNameDataString") + " lobby", lobby);
			GetNode<VBoxContainer>("LobbyContainer").AddChild(element);
		}
	}
	private void OnPlayerJoinLobbyCallback(Friend friend){
        var element = LobbyPlayer.Instantiate() as PlayerListItem;
        element.Name = friend.Id.AccountId.ToString(); 
        element.SetPlayerName(friend.Name);
        GetNode<VBoxContainer>("Lobby Users").AddChild(element);
        GameManager.OnPlayerJoinedLobby(friend);
    }

    private void OnPlayerLeftLobbyCallback(Friend friend){
        GetNode<PlayerListItem>($"Lobby Users/{friend.Id.AccountId.ToString()}").QueueFree();
    }

    private void OnLobbyRefreshCompletedCallback(List<Lobby> lobbies){
        foreach (var item in lobbies)
        {
            var element = LobbyElementScene.Instantiate<LobbyItem>();
            element.SetLabels(item.Id.ToString(), item.GetData("ownerNameDataString") + " Lobby" , item);
            GetNode<VBoxContainer>("Lobby Container").AddChild(element);
        }
    }
	 private void _on_LobbyButton_button_down(){
        isPlayerReady = !isPlayerReady;
        Dictionary<string, string> playerDict = new Dictionary<string, string>();
        playerDict.Add("DataType", "Ready");
        playerDict.Add("PlayerName", SteamManager.Manager.PlayerSteamID.AccountId.ToString());
        playerDict.Add("IsReady", isPlayerReady.ToString());// True or False
        string str = JsonConvert.SerializeObject(playerDict);
        OnPlayerReadyMessageCallback(playerDict);
        if(SteamManager.Manager.IsHost){
            SteamManager.Manager.Broadcast(str);
        }else{
            SteamManager.Manager.SteamConnectionManager.Connection.SendMessage(str);
        }
    }

    private void OnPlayerReadyMessageCallback(Dictionary<string, string> dict){
        GetNode<PlayerListItem>($"Lobby Users/{dict["PlayerName"]}").SetReadyStatus(bool.Parse(dict["IsReady"]));
        GameManager.OnPlayerReady(dict);
    }

    public void OnStartGameCallback(Dictionary<string, string> dict){
       int i = 1;
       foreach (var item in GameManager.CurrentPlayers)
       {
            var p = Player.Instantiate() as Player;

            GetNode<Node2D>("../PlayersSpawnPositions/" + i.ToString()).AddChild(p);
            i ++;
            p.Name = item.FriendData.Id.AccountId.ToString();
            p.FriendData = item.FriendData;
            if(p.Name == SteamManager.Manager.PlayerSteamID.AccountId.ToString()){
                p.Controlled = true;
            }
       }
    }

    

    private void _on_CreateLobby_button_down(){
        SteamManager.Manager.CreateLobby();
    }

    private void _on_GetLobbies_button_down(){
        SteamManager.Manager.GetMultiplayerLobbies();
    }
	private void _on_create_lobby_pressed()
	{
		SteamManager.Manager.CreateLobby();
	}
	private void _on_get_lobbies_pressed()
	{
		var lobbies = SteamManager.Manager.GetMultiplayerLobbies();
	}
	public void GoToScene(string path)
	{
		GetTree().ChangeSceneToFile(path);
	}
}
