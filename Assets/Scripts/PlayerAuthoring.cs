using UnityEngine;
using Unity.Entities;

public class PlayerAuthoring : MonoBehaviour
{
	public string playerName;
    public float speed;
	public float jumpHeight;
}

public class PlayerBaker : Baker<PlayerAuthoring>
{
	public override void Bake(PlayerAuthoring authoring)
	{
		AddComponent(GetEntity(TransformUsageFlags.Dynamic), new PlayerData
		{
			playerName = authoring.playerName,
			speed = authoring.speed,
			jumpHeight = authoring.jumpHeight
		});
	}
}
