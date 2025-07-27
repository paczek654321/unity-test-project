using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

public struct TestRpcCommand : IRpcCommand{ public FixedString64Bytes message; }
public struct GoInGameRpcCommand : IRpcCommand{}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class ServerSystem : SystemBase
{
	protected override void OnCreate() { Debug.Log("--- Starting a Server ---"); }

	protected override void OnUpdate()
	{
		EntityCommandBuffer buffer = new EntityCommandBuffer(Allocator.Temp);
		foreach (RefRW<Unity.Physics.PhysicsVelocity> velocity in SystemAPI.Query<RefRW<Unity.Physics.PhysicsVelocity>>())
		{
			velocity.ValueRW.Linear.y = 1;
		}
		foreach (var (request, command, entity) in SystemAPI.Query<ReceiveRpcCommandRequest, TestRpcCommand>().WithEntityAccess())
		{
			Debug.Log(request.SourceConnection.Index + ": " + command.message);
			buffer.DestroyEntity(entity);
		}
		foreach (var (request, command, entity) in SystemAPI.Query<ReceiveRpcCommandRequest, GoInGameRpcCommand>().WithEntityAccess())
		{
			buffer.AddComponent<NetworkStreamInGame>(request.SourceConnection);
			buffer.DestroyEntity(entity);
		}
		buffer.Playback(EntityManager);
		buffer.Dispose();
	}
}
	
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class ClientSystem : SystemBase
{
	private void SendRpc<T>(EntityCommandBuffer buffer, T command) where T : unmanaged, IComponentData
	{
		Entity entity = buffer.CreateEntity();
		buffer.AddComponent(entity, new SendRpcCommandRequest());
		buffer.AddComponent(entity, command);	
	}

	protected override void OnCreate() { Debug.Log("--- Starting a Client ---"); }
	
	protected override void OnUpdate()
	{
		EntityCommandBuffer buffer = new EntityCommandBuffer(Allocator.Temp);
		foreach((NetworkId networkId, Entity entity) in SystemAPI.Query<NetworkId>().WithNone<NetworkStreamInGame>().WithEntityAccess())
		{
			buffer.AddComponent<NetworkStreamInGame>(entity);
			SendRpc(buffer, new GoInGameRpcCommand());
		}
		if (Input.GetKeyDown(KeyCode.Space))
		{
			SendRpc(buffer, new TestRpcCommand{message = "test"});
		}
		buffer.Playback(EntityManager);
		buffer.Dispose();
	}
}
