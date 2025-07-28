using UnityEngine;
using Unity.Entities;

public class PlayerAuthoring : MonoBehaviour
{
	public int id;
    public float speed;
	public float jumpHeight;
}

public struct PlayerData : IComponentData
{
	[Unity.NetCode.GhostField]public int id;
	[Unity.NetCode.GhostField]public float speed;
	[Unity.NetCode.GhostField]public float jumpHeight;
}

public class PlayerBaker : Baker<PlayerAuthoring>
{
	public override void Bake(PlayerAuthoring authoring)
	{
		AddComponent(GetEntity(TransformUsageFlags.Dynamic), new PlayerData
		{
			id = authoring.id,
			speed = authoring.speed,
			jumpHeight = authoring.jumpHeight
		});
	}
}