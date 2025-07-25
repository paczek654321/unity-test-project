using Unity.Entities;
using Unity.Mathematics;

public struct PlayerInput
{
	public float2 move;
	public bool jump;
}

public partial class PlayerInputSystem : SystemBase
{
	private Controls _input;

	protected override void OnCreate()
	{
		_input = new Controls();
		_input.Enable();
	}

	protected override void OnDestroy()
	{
		_input.Disable();
	}

	protected override void OnUpdate()
	{
		PlayerInput playerInput = new PlayerInput{};

		playerInput.move = _input.Player.Move.ReadValue<UnityEngine.Vector2>();
		playerInput.jump = _input.Player.Jump.ReadValue<float>() > 0;
		//TODO: Extract this into a seperate player input component
		foreach(RefRW<PlayerData> data in SystemAPI.Query<RefRW<PlayerData>>())
		{
			data.ValueRW.input = playerInput;
		}
	}
}