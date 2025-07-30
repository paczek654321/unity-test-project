using UnityEngine;
using Unity.Entities;

public class PlayerAuthoring : MonoBehaviour{ public int id; }

public struct PlayerData : IComponentData
{
	[Unity.NetCode.GhostField]public int id;
	//Stored here because the client predicted one wont synchronize with other clients
	[Unity.NetCode.GhostField]public Vector2 movement;

	public bool kick;

	public const float Speed = 3;
	public const float JumpHeight = 5;
	public const float HalfHeight = 1;
	//Player collider radius -0.1
	public const float GroundCheckRadius = 0.2f;
	public static readonly Unity.Physics.CollisionFilter s_collisonFilter = new Unity.Physics.CollisionFilter{BelongsTo = ~0u, CollidesWith = 1u<<3};
}

public class PlayerBaker : Baker<PlayerAuthoring>
{
	public override void Bake(PlayerAuthoring authoring)
	{
		Entity player = GetEntity(TransformUsageFlags.Dynamic);
		AddComponent(player, new PlayerData{ id = authoring.id });
		AddComponent(player, new PlayerInput());
	}
}