using Godot;
using Newtonsoft.Json.Converters;
using System;
using GodotSteam;
public static class SteamManager
{
    private const uint AppId = 3965800;
    
    public static void InitializeSteam()
    {
        Steam.SteamInit();
        var isSteamRunning = Steam.IsSteamRunning();
        if (!isSteamRunning)
        {
            GD.PrintErr("Steam is not running. Please start Steam to use this feature.");
            return;
        }
        var steamId = Steam.GetSteamID();
        var name = Steam.GetFriendPersonaName(steamId);
        GD.Print("Your steam name is: " + name);
    }
}
