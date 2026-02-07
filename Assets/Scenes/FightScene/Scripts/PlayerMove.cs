using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMove : NetworkBehaviour
{
	[Header("Movement Settings")]
	public float moveSpeed = 5f;
	public float acceleration = 60f;
	public float deceleration = 40f;
	public float velocityPower = 0.95f;
	public float jumpForce = 8f;
	public float dashSpeed = 12f;
	public float dashDuration = 0.2f;
	public float stopThreshold = 0.05f;

	[Header("Ground Check")]
	public LayerMask groundLayer;
	public Transform groundCheck;
	public float groundCheckRadius = 0.2f;

	private Rigidbody2D rb;
	private bool isGrounded;
	private bool isDashing;
	private float dashTimer;

	public bool isMovementBlocked;

	private GameManager gameManager;

	private Vector2 moveDirection = Vector2.zero;
	private SpriteRenderer spriteRenderer;

	private Animator animator;

	private NetworkVariable<bool> isFacingRight = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public override void OnNetworkSpawn()
	{
		isFacingRight.OnValueChanged += OnFacingChanged;
		OnFacingChanged(!isFacingRight.Value, isFacingRight.Value);
	}

	private void OnFacingChanged(bool oldValue, bool newValue)
	{
		Vector3 scale = transform.localScale;
		scale.x = Mathf.Abs(scale.x) * (newValue ? 1 : -1);
		transform.localScale = scale;
	}

	private void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		animator = GetComponent<Animator>();
		gameManager = FindFirstObjectByType<GameManager>();
	}

	private void Update()
	{
		if (gameManager.matchEnded) return;
		if (!IsOwner) return; // Prevent non-owners from moving
		if (isDashing) return;
		if (isMovementBlocked) return;

		// Handling a movement direction
		bool movingLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
		bool movingRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

		//  Update animation for movement
		bool isMoving = moveDirection != Vector2.zero;
		animator.SetBool("isMoving", isMoving);

		if (isMoving)
		{
			bool faceRight = moveDirection.x > 0;
			if (isFacingRight.Value != faceRight)
			{
				isFacingRight.Value = faceRight;
			}
		}

		if (movingLeft)
		{
			moveDirection = Vector2.left;
		}
		else if (movingRight)
		{
			moveDirection = Vector2.right;
		}
		else if (movingLeft && movingRight)
		{
			moveDirection = Vector2.zero; // Stop if both keys are pressed
		}
		else
		{
			moveDirection = Vector2.zero; // No movement if neither is pressed
		}

		// Jump (only if grounded)
		if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && isGrounded)
		{
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
		}

		// Dash (only if there is a movement direction)
		if (Input.GetKeyDown(KeyCode.LeftShift) && moveDirection != Vector2.zero)
		{
			StartDash();
		}

		// Flip player based on a movement direction
		if (movingLeft)
		{
			FlipPlayer(true);
		}
		else if (movingRight)
		{
			FlipPlayer(false);
		}
	}

	private void FixedUpdate()
	{
		if (!IsOwner) return; // Prevent non-owners from moving
		if (gameManager.matchEnded) return;
		if (isMovementBlocked) return;

		isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

		if (isDashing)
		{
			HandleDash();
			return;
		}

		HandleMovement();
	}

	public void BlockMovement(bool isMovementBlocked)
	{
		if (isMovementBlocked)
		{
			rb.linearVelocity = Vector2.zero;
		}
	}

	private void HandleMovement()
	{
		float targetSpeed = moveDirection.x * moveSpeed;
		float speedDiff = targetSpeed - rb.linearVelocity.x;

		float accelRate = moveDirection != Vector2.zero ? acceleration : deceleration;

		float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velocityPower) * Mathf.Sign(speedDiff);

		rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement * Time.fixedDeltaTime, rb.linearVelocity.y);

		if (Mathf.Abs(rb.linearVelocity.x) < stopThreshold && moveDirection == Vector2.zero)
		{
			rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
		}
	}

	private void StartDash()
	{
		isDashing = true;
		dashTimer = dashDuration;

		rb.linearVelocity = new Vector2(moveDirection.x * dashSpeed, 0f);
	}

	private void HandleDash()
	{
		dashTimer -= Time.fixedDeltaTime;
		if (dashTimer <= 0f)
		{
			isDashing = false;
		}
	}

	private void FlipPlayer(bool flipLeft)
	{
		Vector3 scale = transform.localScale;

		if (flipLeft)
		{
			scale.x = -Mathf.Abs(scale.x); // Flip to the left
		}
		else
		{
			scale.x = Mathf.Abs(scale.x); // Flip to the right
		}

		transform.localScale = scale;
	}

	private void OnDrawGizmosSelected()
	{
		if (groundCheck != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
		}
	}
}