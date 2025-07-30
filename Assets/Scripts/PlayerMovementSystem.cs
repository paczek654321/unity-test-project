using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Collections;

public struct PlayerInput : IInputComponentData
{
	public float2 movementXZ;
	public bool jump;
}

[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class PlayerInputSystem : SystemBase
{
	private Controls _input;

	protected override void OnCreate()
	{
		_input = new Controls();
		_input.Enable();
		CheckedStateRef.RequireForUpdate<NetworkStreamInGame>();
	}

	protected override void OnUpdate()
	{
		foreach
		(
			(RefRW<PlayerInput> input, Entity player) in
			SystemAPI.Query<RefRW<PlayerInput>>().WithAll<GhostOwnerIsLocal>().WithEntityAccess()
		)
		{
			input.ValueRW.movementXZ = _input.Player.Move.ReadValue<UnityEngine.Vector2>();
			input.ValueRW.jump = _input.Player.Jump.ReadValue<float>() > 0;
		}
	}

	protected override void OnDestroy()
	{
		_input.Disable();
	}
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct PlayerMovementSystem : ISystem
{
	//A little bit smaller than the actuall radius in order to avoid counting side collisions
	//Gettining the data dynamically is far too complicated
	private bool GroundCheck(float3 position)
	{
		NativeList<ColliderCastHit> collisons = new NativeList<ColliderCastHit>(Allocator.Temp);
		SystemAPI.GetSingleton<PhysicsWorldSingleton>().SphereCastAll
		(
			position, PlayerData.GroundCheckRadius,
			new float3(0, -1, 0), PlayerData.HalfHeight-PlayerData.GroundCheckRadius,
			ref collisons, CollisionFilter.Default
		);
		bool result = collisons.Length > 1;
		collisons.Dispose();
		return result;
	}

	[Unity.Burst.BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		foreach
		(
			(PlayerInput input, Unity.Transforms.LocalTransform transform, RefRW<Unity.Physics.PhysicsVelocity> velocity, Entity player) in
			SystemAPI.Query<PlayerInput, Unity.Transforms.LocalTransform, RefRW<Unity.Physics.PhysicsVelocity>>().WithAll<Simulate>().WithEntityAccess()
		)
		{
			velocity.ValueRW.Linear.x = input.movementXZ.x*PlayerData.Speed;
			velocity.ValueRW.Linear.z = input.movementXZ.y*PlayerData.Speed;
			if (input.jump && GroundCheck(transform.Position))
			{
				velocity.ValueRW.Linear.y = PlayerData.JumpHeight;
			}
		}
	}
}

public struct KickRpcCommand : IRpcCommand{ public int playerId; }

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct PlayerServerSystem : ISystem
{
	[Unity.Burst.BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		EntityCommandBuffer buffer = new EntityCommandBuffer(Allocator.Temp);
		foreach
		(
			(RefRW<PlayerData> data, RefRW<PhysicsMass> mass, PlayerInput input, Unity.Transforms.LocalTransform transform) in
			SystemAPI.Query<RefRW<PlayerData>, RefRW<PhysicsMass>, PlayerInput, Unity.Transforms.LocalTransform>()
		)
		{
			//Lock rotation
			mass.ValueRW.InverseInertia = float3.zero;

			data.ValueRW.movement = input.movementXZ;
			if((input.movementXZ.x != 0 || input.movementXZ.y != 0))
			{
				float2 normalizedMovement = math.normalizesafe(input.movementXZ, float2.zero);
				float3 direction = new float3(normalizedMovement.x, 0, normalizedMovement.y);
				
				NativeList<DistanceHit> collisions = new NativeList<DistanceHit>(Allocator.Temp);
				//Player collider radius + sphere radius + margin = 0.6
				SystemAPI.GetSingleton<PhysicsWorldSingleton>().OverlapSphere(transform.Position+(direction*0.6f), 0.25f, ref collisions, PlayerData.s_collisonFilter);
				
				Entity entity = buffer.CreateEntity();
				
				if (!collisions.IsEmpty)
				{
					buffer.AddComponent(entity, new SendRpcCommandRequest());
					buffer.AddComponent(entity, new KickRpcCommand{playerId = data.ValueRO.id});

					foreach(DistanceHit hit in collisions)
					{
						PhysicsVelocity colliderVelocity = SystemAPI.GetComponent<PhysicsVelocity>(hit.Entity);
						colliderVelocity.Linear = direction*PlayerData.JumpHeight;
						colliderVelocity.Linear.y = PlayerData.JumpHeight;
						buffer.SetComponent(hit.Entity, colliderVelocity);

					}
				}

				collisions.Dispose();
			}
		}
		buffer.Playback(state.EntityManager);
		buffer.Dispose();
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct PlayerClientSystem : ISystem
{
	[Unity.Burst.BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		EntityCommandBuffer buffer = new EntityCommandBuffer(Allocator.Temp);
		foreach (var (request, command, entity) in SystemAPI.Query<ReceiveRpcCommandRequest, KickRpcCommand>().WithEntityAccess())
		{
			foreach (RefRW<PlayerData> playerData in SystemAPI.Query<RefRW<PlayerData>>())
			{
				if (playerData.ValueRO.id != command.playerId) { continue; }
				playerData.ValueRW.kick = true;
				break;
			}
			buffer.DestroyEntity(entity);
		}
		buffer.Playback(state.EntityManager);
		buffer.Dispose();
	}
}