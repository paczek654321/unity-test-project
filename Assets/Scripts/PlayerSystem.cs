using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;

public struct PlayerData : IComponentData
{
	public FixedString64Bytes playerName;
	public bool walking;
	public float speed;
	public float jumpHeight;
	public PlayerInput input;
}

public partial struct PlayerSystem : ISystem
{	
	public void OnUpdate(ref SystemState state)
	{
		PlayerJob job = new PlayerJob{physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>()};
		//Using PhysicsWorldSingelton forces synchronous execution
		//TODO: Figure out how to make this async
		job.Schedule();
	}
}

public partial struct PlayerJob : IJobEntity
{
	public PhysicsWorldSingleton physicsWorld;

	private bool GroundCheck(float3 position, float radius, float halfHeight)
	{
		NativeList<ColliderCastHit> collisons = new NativeList<ColliderCastHit>(Allocator.Temp);
		physicsWorld.SphereCastAll(position, radius, new float3(0, -1, 0), halfHeight-radius, ref collisons, CollisionFilter.Default);
		return collisons.Length > 1;
	}

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
		
		//Animate movement
		if (velocity.Linear.x != 0 || velocity.Linear.z != 0)
		{
			data.walking = true;
			transform.Rotation = quaternion.Euler(0, math.atan2(velocity.Linear.x, velocity.Linear.z), 0);
		}
		else
		{
			data.walking = false;
		}
		
		//Handle jumping
		//Hardocded radius and height, Unity disables unsafe access by default | alternative: float radius; unsafe { radius = ((SphereCollider*)collider.ColliderPtr)->Radius - 0.1f; }
		if (data.input.jump && GroundCheck(transform.Position, 0.4f, 1))
		{
			velocity.Linear.y = data.jumpHeight;
		}
	}
}