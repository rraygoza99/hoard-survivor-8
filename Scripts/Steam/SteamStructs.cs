using System;
using System.Runtime.InteropServices;

public enum EResult {
	OK=1,
	Fail=2
}

[StructLayout(LayoutKind.Sequential, Pack=4)]
public struct LobbyCreated_t
{
	public EResult m_eResult;
	public ulong m_ulSteamIDLobby;
}
