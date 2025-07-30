using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;

public class CoinAuthoring : MonoBehaviour{}

public struct CoinTag : IComponentData{}

public class CoinBaker : Baker<CoinAuthoring>
{
	public override void Bake(CoinAuthoring authoring)
	{
		AddComponent(GetEntity(TransformUsageFlags.Dynamic), new CoinTag());
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct CoinSystem : ISystem
{
	public static readonly CollisionFilter s_collisonFilter = new CollisionFilter{BelongsTo = ~0u, CollidesWith = PlayerData.collisionMask};

	[Unity.Burst.BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<PrefabManager>();
		state.RequireForUpdate<CoinTag>();
	}

	[Unity.Burst.BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		//Caching would require disabling burst (Converting to a SystemBase)
		Entity coin = SystemAPI.GetSingletonEntity<CoinTag>();

		RefRW<LocalTransform> transform = SystemAPI.GetComponentRW<LocalTransform>(coin);
		transform.ValueRW = transform.ValueRO.RotateZ(math.TORADIANS*90*SystemAPI.Time.DeltaTime);

		NativeList<DistanceHit> collisions = new NativeList<DistanceHit>(Allocator.Temp);
		SystemAPI.GetSingleton<PhysicsWorldSingleton>().OverlapSphere(transform.ValueRO.Position, 0.5f, ref collisions, s_collisonFilter);

		if (collisions.IsEmpty) { return; }

		float maxDistance = 1;
		Entity player = Entity.Null;
		foreach (DistanceHit hit in collisions)
		{
			if (hit.Distance < maxDistance || player.Equals(Entity.Null))
			{
				player = hit.Entity;
				maxDistance = hit.Distance;
			}

		}
		collisions.Dispose();

		transform.ValueRW.Position = new float3(UnityEngine.Random.Range(-4.2f, 4.2f), 1, UnityEngine.Random.Range(-2.2f, 1.2f));
	}
}