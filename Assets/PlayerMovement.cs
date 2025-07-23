using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public float speed;
	
	public float jumpHeight;
	
	public float mouseSensitivity;
	
	private Rigidbody _body;
	private Transform _camera;
	private float _rotationX = 0;
	
	private CapsuleCollider _shape;

	private Animator _animation;

	void Start()
	{
		_body = GetComponent<Rigidbody>();
		_camera = transform.GetChild(0);

		_shape = GetComponent<CapsuleCollider>();

		_animation = transform.GetChild(1).GetComponent<Animator>();

		Cursor.lockState = CursorLockMode.Locked;
	}

    void FixedUpdate()	
    {
		//Move on the XZ plane
		float velocityY = _body.linearVelocity.y;
		_body.linearVelocity =
			Input.GetAxis("Vertical") * transform.forward.normalized +
			Input.GetAxis("Horizontal") * transform.right.normalized;
		_body.linearVelocity = _body.linearVelocity.normalized * speed;
		_body.linearVelocity += Vector3.up * velocityY;
		//Manage walking/idle animations
		_animation.SetBool("Walking", _body.linearVelocity.x != 0 || _body.linearVelocity.z != 0);

		//Handle jumping
		if (Input.GetAxis("Jump") != 0 && Physics.SphereCast(new Ray(transform.position, Vector3.down), _shape.radius-0.1f, _shape.height/2))
		{
			_body.AddForce(Vector3.up*jumpHeight, ForceMode.Impulse);
		}

		//Handle mouse movement
		Vector2 mouse = new Vector2
		(
			Input.GetAxis("Mouse X"),
			Input.GetAxis("Mouse Y")
		) * mouseSensitivity;

		_rotationX = Mathf.Clamp(_rotationX - mouse.y, -30, 70);
		_camera.localRotation = Quaternion.Euler(_rotationX, 0, 0);
		
		transform.Rotate(Vector3.up * mouse.x);
    }		
}
	