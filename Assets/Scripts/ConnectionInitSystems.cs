using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using Unity.Mathematics;	
using UnityEngine;

public struct TestRpcCommand : IRpcCommand{ public FixedString64Bytes message; }
public struct GoInGameRpcCommand : IRpcCommand{}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerInitSystem : ISystem
{
	[Unity.Burst.BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		Debug.Log("--- Starting a Server ---");
		state.RequireForUpdate<PrefabManager>();
	}

	[Unity.Burst.BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		PrefabManager prefabManager = SystemAPI.GetSingleton<PrefabManager>();
		EntityCommandBuffer buffer = new EntityCommandBuffer(Allocator.Temp);
		foreach (var (request, command, entity) in SystemAPI.Query<ReceiveRpcCommandRequest, TestRpcCommand>().WithEntityAccess())
		{
			Debug.Log(command.message);
			buffer.DestroyEntity(entity);
		}

		int numPlayers = 0;
		foreach (NetworkStreamConnection connection in SystemAPI.Query<NetworkStreamConnection>().WithAll<NetworkStreamInGame>()) { numPlayers += 1; }

		foreach (var (request, command, entity) in SystemAPI.Query<ReceiveRpcCommandRequest, GoInGameRpcCommand>().WithEntityAccess())
		{
			numPlayers += 1;

			if (numPlayers > 2)
			{
				buffer.AddComponent<NetworkStreamRequestDisconnect>(request.SourceConnection);
			}
			else
			{
				buffer.AddComponent<NetworkStreamInGame>(request.SourceConnection);

				Entity player = buffer.Instantiate(prefabManager.player);
				buffer.SetComponent(player, Unity.Transforms.LocalTransform.FromPosition(new float3((numPlayers == 1) ? -2 : 2, 1, 0)));
				buffer.SetComponent(player, new PlayerData{ id = numPlayers - 1 });
				buffer.AddComponent(player, new GhostOwner{ NetworkId = SystemAPI.GetComponent<NetworkId>(request.SourceConnection).Value });

				buffer.DestroyEntity(entity);
			}
		}
		buffer.Playback(state.EntityManager);
		buffer.Dispose();
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientInitSystem : ISystem
{
	private void SendRpc<T>(EntityCommandBuffer buffer, T command) where T : unmanaged, IComponentData
	{
		Entity entity = buffer.CreateEntity();
		buffer.AddComponent(entity, new SendRpcCommandRequest());
		buffer.AddComponent(entity, command);	
	}

	[Unity.Burst.BurstCompile]
	public void OnCreate(ref SystemState state) { Debug.Log("--- Starting a Client ---"); }
	
	[Unity.Burst.BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		if (!SystemAPI.HasSingleton<NetworkStreamConnection>()) { Application.Quit(); }
		EntityCommandBuffer buffer = new EntityCommandBuffer(Allocator.Temp);
		foreach((NetworkId networkId, Entity entity) in SystemAPI.Query<NetworkId>().WithNone<NetworkStreamInGame>().WithEntityAccess())
		{
			buffer.AddComponent<NetworkStreamInGame>(entity);
			SendRpc(buffer, new GoInGameRpcCommand());
		}
		if (Input.GetKeyDown(KeyCode.T))
		{
			SendRpc(buffer, new TestRpcCommand{message = "test"});
		}
		buffer.Playback(state.EntityManager);
		buffer.Dispose();
	}
}
