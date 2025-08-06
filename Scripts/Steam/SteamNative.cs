using Godot;
using System;
using System.Runtime.InteropServices;

public enum ELobbyType
{
	Private = 0,
	FriendsOnly = 1,
	Public = 2,
	Invisible = 3,
}

public partial class SteamNative
{
	[DllImport("steam_api64", CallingConvention=CallingConvention.Cdecl, EntryPoint = "SteamAPI_Init")]
	[return:MarshalAs(UnmanagedType.I1)]
	public static extern bool SteamAPI_Init();
	
	[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_RunCallbacks")]
	public static extern void SteamAPI_RunCallbacks();
	
	[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_Shutdown")]
	public static extern void SteamAPI_Shutdown();
	
	[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamMatchmaking_CreateLobby")]
	public static extern ulong CreateLobby(ELobbyType eLobbyType, int cMaxMembers8);
}
