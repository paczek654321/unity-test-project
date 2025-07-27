using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using System.Linq;

[UnityEngine.Scripting.Preserve]
public class AutoConnectBootstrap: ClientServerBootstrap
{
	private string _ip = "127.0.0.1";
	private ushort _port = 7979;

	private RefRW<NetworkStreamDriver> GetNetworkStreamDriver(World world)
	{
		EntityQuery query = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
		return query.GetSingletonRW<NetworkStreamDriver>();
	}
	
	public override bool Initialize(string defaultWorldName)
	{
		PlayType role = PlayType.Client;
        if (Application.isEditor)
		{
			role = PlayType.ClientAndServer;
		}
		else if (new[] {RuntimePlatform.LinuxServer, RuntimePlatform.WindowsServer, RuntimePlatform.OSXServer}.Contains(Application.platform))
		{
			role = PlayType.Server;
		}
		
		World serverWorld = null;
		World clientWorld = null;

		if (role == PlayType.ClientAndServer || role == PlayType.Server)
		{
			serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");

			GetNetworkStreamDriver(serverWorld).ValueRW.Listen(ClientServerBootstrap.DefaultListenAddress.WithPort(_port));

			World.DefaultGameObjectInjectionWorld = serverWorld;
		}
		if (role == PlayType.ClientAndServer || role == PlayType.Client)
		{
			clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");

			GetNetworkStreamDriver(clientWorld).ValueRW.Connect(clientWorld.EntityManager, Unity.Networking.Transport.NetworkEndpoint.Parse(_ip, _port));
			
			if (role == PlayType.Client) { World.DefaultGameObjectInjectionWorld = clientWorld; }
		}

		return true;
	}
}