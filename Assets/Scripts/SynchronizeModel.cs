using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;

public class SynchronizeModel : MonoBehaviour
{
	public int playerId;
	public Vector3 position;
	public Vector3 rotation;

	private EntityManager _entityManager;
	private Entity _player = Entity.Null;
	private Animator _animator;

	private bool FindPlayer()
	{
		if (!_player.Equals(Entity.Null)) { return true; }
		NativeArray<Entity> entities = _entityManager.CreateEntityQuery(typeof(PlayerData)).ToEntityArray(Allocator.Temp);

		foreach (Entity entity in entities)
		{
			PlayerData data = _entityManager.GetComponentData<PlayerData>(entity);
			if (data.id == playerId)
			{
				_player = entity;
				GetComponent<Animator>().speed = 0.644f*data.speed;
			}
		}

		entities.Dispose();

		return !_player.Equals(Entity.Null);
	}

	void Start()
	{
		_entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
		_animator = GetComponent<Animator>();		
	}

    void FixedUpdate()
	{
		if (!FindPlayer()) { return; }
		LocalTransform entityTransform = _entityManager.GetComponentData<LocalTransform>(_player);
		transform.position = (Vector3)entityTransform.Position + position;
		transform.rotation = entityTransform.Rotation;
		transform.Rotate(rotation);
		//_animator.SetBool("Walking", _entityManager.GetComponentData<PlayerData>(_player).walking);
	}
}