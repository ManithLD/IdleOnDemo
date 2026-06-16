using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int IsJumpingHash = Animator.StringToHash("isJumping");

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 14.5f;
    [SerializeField] private float jumpReleaseDamping = 0.5f;
    [SerializeField] private float coyoteTime = 0.15f;

    [Header("Collision Checks")]
    [SerializeField] private float groundCheckDistance = 0.08f;
    [SerializeField] private float wallCheckDistance = 0.08f;
    [SerializeField] private float minGroundNormalY = 0.65f;
    [SerializeField] private LayerMask groundLayer;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PhysicsMaterial2D movementPhysicsMaterial;

    private readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[4];
    private ContactFilter2D groundFilter;

    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private InputAction moveAction;
    private InputAction jumpAction;

    private float horizontalInput;
    private float simulatedHorizontalInput;
    private float lastGroundedTime = float.NegativeInfinity;

    private bool jumpQueued;
    private bool stopJump;
    private bool isGrounded;
    private bool hasSimulatedInput;
    private bool hasJumpedSinceGrounded;

    public bool IsAttacking { get; set; }

    public bool IsGrounded => isGrounded;

    public int FacingDirection { get; private set; } = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();

        animator ??= GetComponentInChildren<Animator>();
        spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();

        // Cache physics material and filter
        capsuleCollider.sharedMaterial = movementPhysicsMaterial != null
            ? movementPhysicsMaterial
            : new PhysicsMaterial2D("Runtime_Player_NoFriction") { friction = 0f, bounciness = 0f };

        groundFilter = new ContactFilter2D { layerMask = groundLayer, useTriggers = false };

        InitializeInputActions();
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
    }

    private void Update()
    {
        if (IsAttacking)
        {
            ApplyAttackLock();
            return;
        }

        ReadInput();
        UpdateFacingDirection();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        isGrounded = CheckGrounded();

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            hasJumpedSinceGrounded = false;
        }

        if (IsAttacking)
        {
            ApplyAttackLock();
            return;
        }

        // Only allow movement if grounded or not running into a wall in the air
        float effectiveInput = (isGrounded || !IsBlockedInMoveDirection(horizontalInput)) ? horizontalInput : 0f;
        Vector2 velocity = rb.linearVelocity;
        velocity.x = effectiveInput * moveSpeed;

        // Process Jump Queue & Coyote Time
        bool canJump = !hasJumpedSinceGrounded && (isGrounded || (Time.time - lastGroundedTime <= coyoteTime));
        if (jumpQueued && canJump)
        {
            velocity.y = jumpForce;
            hasJumpedSinceGrounded = true;
            stopJump = false;
        }
        else if (stopJump && velocity.y > 0f)
        {
            velocity.y *= jumpReleaseDamping;
            stopJump = false;
        }

        rb.linearVelocity = velocity;
        jumpQueued = false;
    }

    // Input Handling
    private void InitializeInputActions()
    {
        InputActionAsset actions = InputSystem.actions;
        if (actions == null) return;

        moveAction = actions.FindAction("Player/Move", false);
        jumpAction = actions.FindAction("Player/Jump", false);
    }

    public void SetSimulatedInput(float horizontal)
    {
        simulatedHorizontalInput = Mathf.Clamp(horizontal, -1f, 1f);
        hasSimulatedInput = !Mathf.Approximately(simulatedHorizontalInput, 0f);

        if (hasSimulatedInput)
        {
            SetFacingDirection(simulatedHorizontalInput);
        }
    }

    public void FaceDirection(float horizontal)
    {
        SetFacingDirection(horizontal);
    }

    private void ReadInput()
    {
        float requestedHorizontalInput = 0f;

        // Horizontal Input
        if (moveAction != null && moveAction.enabled)
        {
            requestedHorizontalInput = Mathf.Clamp(moveAction.ReadValue<Vector2>().x, -1f, 1f);
        }
        else if (Keyboard.current != null)
        {
            Keyboard kb = Keyboard.current;
            requestedHorizontalInput = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) -
                                       (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        }

        horizontalInput = hasSimulatedInput ? simulatedHorizontalInput : requestedHorizontalInput;

        // Jump Input
        if ((jumpAction != null && jumpAction.WasPressedThisFrame()) ||
            (Keyboard.current != null && (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)))
        {
            jumpQueued = true;
        }

        // Variable Jump Release
        if ((jumpAction != null && jumpAction.WasReleasedThisFrame()) ||
            (Keyboard.current != null && (Keyboard.current.wKey.wasReleasedThisFrame || Keyboard.current.upArrowKey.wasReleasedThisFrame)))
        {
            stopJump = true;
        }
    }

    // Physics Checks
    private bool CheckGrounded()
    {
        int hitCount = capsuleCollider.Cast(Vector2.down, groundFilter, hitBuffer, groundCheckDistance);
        for (int i = 0; i < hitCount; i++)
        {
            if (hitBuffer[i].normal.y >= minGroundNormalY) return true;
        }
        return false;
    }

    private bool IsBlockedInMoveDirection(float moveDirection)
    {
        if (Mathf.Approximately(moveDirection, 0f)) return false;

        float direction = Mathf.Sign(moveDirection);
        int hitCount = capsuleCollider.Cast(Vector2.right * direction, groundFilter, hitBuffer, wallCheckDistance);

        for (int i = 0; i < hitCount; i++)
        {
            if (Vector2.Dot(hitBuffer[i].normal, Vector2.left * direction) > 0.5f) return true;
        }
        return false;
    }

    // Visuals
    private void UpdateFacingDirection()
    {
        SetFacingDirection(horizontalInput);
    }

    private void SetFacingDirection(float horizontal)
    {
        if (Mathf.Approximately(horizontal, 0f))
        {
            return;
        }

        FacingDirection = horizontal < 0f ? -1 : 1;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = FacingDirection < 0;
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool isJumping = !isGrounded || jumpQueued || hasJumpedSinceGrounded;
        bool isMoving = !isJumping && isGrounded && Mathf.Abs(horizontalInput) > 0.01f;

        animator.SetBool(IsMovingHash, isMoving);
        animator.SetBool(IsJumpingHash, isJumping);
    }

    private void ApplyAttackLock()
    {
        horizontalInput = 0f;
        jumpQueued = false;
        stopJump = false;
        SetHorizontalVelocity(0f);

        if (animator == null) return;

        animator.SetBool(IsMovingHash, false);
        animator.SetBool(IsJumpingHash, false);
    }

    private void SetHorizontalVelocity(float xVelocity)
    {
        if (rb == null) return;

        Vector2 velocity = rb.linearVelocity;
        velocity.x = xVelocity;
        rb.linearVelocity = velocity;
    }

    private void OnDrawGizmosSelected()
    {
        CapsuleCollider2D editorCollider = GetComponent<CapsuleCollider2D>();
        if (editorCollider == null) return;

        Bounds bounds = editorCollider.bounds;

        Gizmos.color = Color.yellow;
        Vector3 groundStart = new Vector3(bounds.center.x, bounds.min.y, transform.position.z);
        Gizmos.DrawLine(groundStart, groundStart + Vector3.down * groundCheckDistance);

        Gizmos.color = Color.cyan;
        Vector3 leftStart = new Vector3(bounds.min.x, bounds.center.y, transform.position.z);
        Vector3 rightStart = new Vector3(bounds.max.x, bounds.center.y, transform.position.z);
        Gizmos.DrawLine(leftStart, leftStart + Vector3.left * wallCheckDistance);
        Gizmos.DrawLine(rightStart, rightStart + Vector3.right * wallCheckDistance);
    }
}
