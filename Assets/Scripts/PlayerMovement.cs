using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float ladderMoveSpeed = 4f;

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
    private float defaultGravityScale;
    private Vector3 spawnPosition;

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

        if (!isOnLadder && Input.GetButtonDown("Jump") && isGrounded)
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
                        (isOnLadder && Mathf.Abs(verticalInput) > 0.01f);
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
        isOnLadder = true;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;
    }

    public void ExitLadder()
    {
        isOnLadder = false;
        rb.gravityScale = defaultGravityScale;
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
