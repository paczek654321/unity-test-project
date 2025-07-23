using UnityEngine;
using Unity.Entities;

public class PlayerAuthoring : MonoBehaviour
{
    public float speed;
	public float jumpHeight;
}

public class PlayerBaker : Baker<PlayerAuthoring>
{
	public override void Bake(PlayerAuthoring authoring)
	{
		AddComponent(GetEntity(TransformUsageFlags.Dynamic), new PlayerData
		{
			speed = authoring.speed,
			jumpHeight = authoring.jumpHeight
		});
	}
}
