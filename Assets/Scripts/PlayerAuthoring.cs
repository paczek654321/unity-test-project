using UnityEngine;
using Unity.Entities;

public class PlayerAuthoring : MonoBehaviour{ public int id; }

public struct PlayerData : IComponentData
{
	[Unity.NetCode.GhostField]public int id;
	//Stored here because the client predicted one wont synchronize with other clients
	[Unity.NetCode.GhostField]public Vector2 movement;
	public const float speed = 3;
	public const float jumpHeight = 0.5f;
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