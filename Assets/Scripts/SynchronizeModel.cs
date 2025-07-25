using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

public class SynchronizeModel : MonoBehaviour
{
	public string playerName;
	public Vector3 position;
	public Vector3 rotation;

	private EntityManager _entityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
	private Entity _player;

	void Start()
	{
		foreach (var entity in _entityManager.GetAllEntities())
		{
			if(_entityManager.HasComponent<PlayerData>(entity))
			{
				PlayerData playerData = _entityManager.GetComponentData<PlayerData>(entity);
				if (playerData.playerName == playerName)
				{
					_player = entity;
					GetComponent<Animator>().speed = 0.644f*playerData.speed;
					break;
				}
			}
		}
	}

    void FixedUpdate()
	{
		LocalTransform entityTransform = _entityManager.GetComponentData<LocalTransform>(_player);
		transform.position = (Vector3)entityTransform.Position + position;
		transform.rotation = entityTransform.Rotation;
		transform.Rotate(rotation);
		//TODO: Find out if its better to get this once and store it in a variable
		GetComponent<Animator>().SetBool("Walking", _entityManager.GetComponentData<PlayerData>(_player).walking);
	}
}
