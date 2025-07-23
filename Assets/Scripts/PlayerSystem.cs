using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;

public struct PlayerData : IComponentData
{
	public float speed;
	public float jumpHeight;
	public PlayerInput input;
}

public partial struct PlayerSystem : ISystem
{
	public void OnUpdate(ref SystemState state)
	{
		PlayerJob job = new PlayerJob{};
		job.ScheduleParallel();
	}
}

public partial struct PlayerJob : IJobEntity
{
	public void Execute
	(
		ref PlayerData data,
		ref LocalTransform transform,
		ref PhysicsVelocity velocity,
		ref PhysicsMass mass
	)
	{
		//Lock rotation
		mass.InverseInertia = float3.zero;
		//Handle movement on the XZ plane
		velocity.Linear.x = data.speed*data.input.move.x;
		velocity.Linear.z = data.speed*data.input.move.y;
		//Handle jumping	
		if (data.input.jump)
		{
			velocity.Linear.y += data.jumpHeight;
		}
	}
}