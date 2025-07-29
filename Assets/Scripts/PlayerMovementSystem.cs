using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;

public struct PlayerInput : IInputComponentData
{
	public Unity.Mathematics.float2 movementXZ;
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
	[Unity.Burst.BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		foreach
		(
			(PlayerInput input, RefRW<Unity.Physics.PhysicsVelocity> velocity, Entity player) in
			SystemAPI.Query<PlayerInput, RefRW<Unity.Physics.PhysicsVelocity>>().WithAll<Simulate>().WithEntityAccess()
		)
		{
			velocity.ValueRW.Linear.x = input.movementXZ.x*PlayerData.speed;
			velocity.ValueRW.Linear.z = input.movementXZ.y*PlayerData.speed;
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
			mass.ValueRW.InverseInertia = Unity.Mathematics.float3.zero;

			data.ValueRW.movement = input.movementXZ;
		}
	}
}