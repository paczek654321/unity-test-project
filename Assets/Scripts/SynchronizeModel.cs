using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;

public class SynchronizeModel : MonoBehaviour
{
	public int playerId;
	public Vector3 position;
	public Vector3 rotation;

	private EntityManager _entityManager;
	private Entity _player = Entity.Null;
	private Animator _animator;

	private bool _local;

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
				GetComponent<Animator>().speed = 0.644f*PlayerData.speed;
				_local = _entityManager.IsComponentEnabled<Unity.NetCode.GhostOwnerIsLocal>(_player);
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
		//Prefer predicted inputs if possible
		Vector2 movement = _local ? _entityManager.GetComponentData<PlayerInput>(_player).movementXZ :
			_entityManager.GetComponentData<PlayerData>(_player).movement;

		LocalTransform entityTransform = _entityManager.GetComponentData<LocalTransform>(_player);
		transform.position = (Vector3)entityTransform.Position + position;
		bool walking = movement.x != 0 || movement.y != 0;
		if (walking)
		{
			transform.rotation = quaternion.Euler(0, math.atan2(movement.x, movement.y), 0);
			transform.Rotate(rotation);
		}
		_animator.SetBool("Walking", walking);
	}
}