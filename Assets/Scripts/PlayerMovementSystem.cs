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
	private bool GroundCheck(float3 position, float radius = 0.2f, float halfHeight = 1)
	{
		NativeList<ColliderCastHit> collisons = new NativeList<ColliderCastHit>(Allocator.Temp);
		SystemAPI.GetSingleton<PhysicsWorldSingleton>().SphereCastAll(position, radius, new float3(0, -1, 0), halfHeight-radius, ref collisons, CollisionFilter.Default);
		bool result = collisons.Length > 1;
		collisons.Dispose();
		return result;
	}

	//[Unity.Burst.BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		foreach
		(
			(PlayerInput input, Unity.Transforms.LocalTransform transform, RefRW<Unity.Physics.PhysicsVelocity> velocity, Entity player) in
			SystemAPI.Query<PlayerInput, Unity.Transforms.LocalTransform, RefRW<Unity.Physics.PhysicsVelocity>>().WithAll<Simulate>().WithEntityAccess()
		)
		{
			velocity.ValueRW.Linear.x = input.movementXZ.x*PlayerData.speed;
			velocity.ValueRW.Linear.z = input.movementXZ.y*PlayerData.speed;
			if (input.jump && GroundCheck(transform.Position))
			{
				velocity.ValueRW.Linear.y = PlayerData.jumpHeight;
			}
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct PlayerServerSystem : ISystem
{
	[Unity.Burst.BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		foreach ((RefRW<PlayerData> data, RefRW<PhysicsMass> mass, PlayerInput input) in SystemAPI.Query<RefRW<PlayerData>, RefRW<PhysicsMass>, PlayerInput>())
		{
			//Lock rotation
			mass.ValueRW.InverseInertia = float3.zero;

			data.ValueRW.movement = input.movementXZ;
		}
	}
}