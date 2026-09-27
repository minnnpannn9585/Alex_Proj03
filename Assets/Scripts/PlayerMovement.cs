using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float ladderMoveSpeed = 4f;
    [Tooltip("How quickly left/right can steer after letting go of a rope. Does not slow a swing that is already faster than move speed.")]
    [SerializeField] private float swingAirControl = 8f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Visual")]
    [Tooltip("Animator on the 'visual' child. Auto-found in children when left empty.")]
    [SerializeField] private Animator animator;

    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");

    private Rigidbody2D rb;
    private TimeTravel timeTravel;
    private float moveInput;
    private float verticalInput;
    private bool isGrounded;
    private bool facingRight = true;
    private bool isOnLadder;
    private bool isSwinging;
    private bool preserveSwingVelocity;
    private bool swingChangedCollision;
    private CollisionDetectionMode2D collisionModeBeforeSwing;
    private float defaultGravityScale;
    private Vector3 spawnPosition;

    public bool IsSwinging => isSwinging;

    public event Action SwingCancelled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        timeTravel = GetComponent<TimeTravel>();
        spawnPosition = transform.position;
        defaultGravityScale = rb.gravityScale;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        isGrounded = CheckGrounded();

        if (!isOnLadder && !isSwinging && Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        HandleFlip();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (isOnLadder)
        {
            rb.velocity = new Vector2(moveInput * ladderMoveSpeed, verticalInput * ladderMoveSpeed);
            return;
        }

        if (isSwinging)
        {
            return;
        }

        if (preserveSwingVelocity)
        {
            if (isGrounded)
            {
                preserveSwingVelocity = false;
                RestoreCollisionMode();
            }
            else
            {
                ApplySwingAirControl();
                return;
            }
        }

        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    private bool CheckGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void HandleFlip()
    {
        if (moveInput > 0f && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0f && facingRight)
        {
            Flip();
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        bool isMoving = Mathf.Abs(moveInput) > 0.01f ||
                        (isOnLadder && Mathf.Abs(verticalInput) > 0.01f) ||
                        (isSwinging && rb.velocity.sqrMagnitude > 0.25f);
        animator.SetBool(IsMovingHash, isMoving);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    public void EnterLadder()
    {
        if (isSwinging)
        {
            return;
        }

        preserveSwingVelocity = false;
        RestoreCollisionMode();
        isOnLadder = true;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;
    }

    public void ExitLadder()
    {
        isOnLadder = false;
        rb.gravityScale = defaultGravityScale;
    }

    public bool TryBeginSwing()
    {
        if (isSwinging || isOnLadder)
        {
            return false;
        }

        isSwinging = true;
        preserveSwingVelocity = false;
        if (!swingChangedCollision)
        {
            collisionModeBeforeSwing = rb.collisionDetectionMode;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            swingChangedCollision = true;
        }

        return true;
    }

    public void CompleteSwing(bool keepVelocity)
    {
        isSwinging = false;
        preserveSwingVelocity = keepVelocity;
        if (!keepVelocity)
        {
            RestoreCollisionMode();
        }
    }

    public void CancelSwing()
    {
        if (!isSwinging)
        {
            return;
        }

        SwingCancelled?.Invoke();
        if (isSwinging)
        {
            isSwinging = false;
            preserveSwingVelocity = true;
        }
    }

    private void ApplySwingAirControl()
    {
        if (Mathf.Abs(moveInput) < 0.01f)
        {
            return;
        }

        float velocityX = rb.velocity.x;
        bool alreadyFaster = Mathf.Abs(velocityX) > moveSpeed && Mathf.Sign(velocityX) == moveInput;
        if (alreadyFaster)
        {
            return;
        }

        float maxDelta = swingAirControl * Time.fixedDeltaTime;
        float newX = Mathf.MoveTowards(velocityX, moveInput * moveSpeed, maxDelta);
        rb.velocity = new Vector2(newX, rb.velocity.y);
    }

    private void RestoreCollisionMode()
    {
        if (!swingChangedCollision)
        {
            return;
        }

        swingChangedCollision = false;
        rb.collisionDetectionMode = collisionModeBeforeSwing;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Deathzone"))
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        CancelSwing();
        preserveSwingVelocity = false;
        RestoreCollisionMode();
        ExitLadder();

        if (timeTravel != null)
        {
            timeTravel.ReturnToModernTime();
        }

        transform.position = spawnPosition;
        rb.velocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
