using Godot;
using System;
using Steamworks;
using Steamworks.Data;

public class SteamSocketManager : SocketManager
{
	public override void OnConnected(Connection connection, ConnectionInfo info){
		
	}
	public override void OnConnecting(Connection connection, ConnectionInfo info){
		
	}
	public override void OnDisconnected(Connection connection, ConnectionInfo info){
		
	}
	public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recbTime, int channel){
		
	}
}
