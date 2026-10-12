using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TopdownPlayer : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.8f;
    [SerializeField] private float interactRange = 0.9f;
    [SerializeField] private Transform visual;
    [SerializeField] private Animator animator;

    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Camera viewCamera;
    private Collider2D body;
    private SpriteRenderer bodySprite;
    private Vector2 reservedExtents;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body = GetComponent<Collider2D>();

        if (visual == null && transform.childCount > 0)
        {
            visual = transform.GetChild(0);
        }

        if (animator == null && visual != null)
        {
            animator = visual.GetComponent<Animator>();
        }

        if (visual != null)
        {
            bodySprite = visual.GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        viewCamera = Camera.main;
    }

    private void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        if (IsMovementLocked())
        {
            input = Vector2.zero;
        }

        moveInput = input;
        UpdateFacing();
        UpdateAnimation();
        HandleInteraction();
    }

    private void FixedUpdate()
    {
        float step = Time.fixedDeltaTime;
        Vector2 next = rb.position + moveInput * moveSpeed * step;
        next = ClampToCamera(next);
        rb.velocity = step > 0f ? (next - rb.position) / step : Vector2.zero;
    }

    private Vector2 ClampToCamera(Vector2 position)
    {
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        if (viewCamera == null || !viewCamera.orthographic)
        {
            return position;
        }

        Vector2 centerOffset = GetBodyCenterOffset();
        float halfHeight = viewCamera.orthographicSize;
        float halfWidth = halfHeight * viewCamera.aspect;
        Vector2 cam = viewCamera.transform.position;
        float minX = cam.x - halfWidth + reservedExtents.x;
        float maxX = cam.x + halfWidth - reservedExtents.x;
        float minY = cam.y - halfHeight + reservedExtents.y;
        float maxY = cam.y + halfHeight - reservedExtents.y;

        Vector2 center = position + centerOffset;
        center.x = minX > maxX ? cam.x : Mathf.Clamp(center.x, minX, maxX);
        center.y = minY > maxY ? cam.y : Mathf.Clamp(center.y, minY, maxY);
        return center - centerOffset;
    }

    private Vector2 GetBodyCenterOffset()
    {
        bool hasBounds = false;
        Bounds bounds = default;

        if (bodySprite != null && bodySprite.sprite != null)
        {
            bounds = bodySprite.bounds;
            hasBounds = true;
        }
        else if (body != null)
        {
            bounds = body.bounds;
            hasBounds = true;
        }

        if (!hasBounds)
        {
            return Vector2.zero;
        }

        reservedExtents.x = Mathf.Max(reservedExtents.x, bounds.extents.x);
        reservedExtents.y = Mathf.Max(reservedExtents.y, bounds.extents.y);
        return (Vector2)bounds.center - (Vector2)transform.position;
    }

    private bool IsMovementLocked()
    {
        return DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
    }

    private void HandleInteraction()
    {
        DialogueManager dialogue = DialogueManager.Instance;
        bool locked = dialogue != null && dialogue.IsOpen;
        Interactable nearest = locked ? null : FindNearest();

        if (dialogue != null)
        {
            if (nearest != null)
            {
                dialogue.ShowPrompt(nearest.PromptPosition, nearest.Prompt);
            }
            else
            {
                dialogue.HidePrompt();
            }
        }

        if (nearest == null || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (dialogue != null && !dialogue.CanStartInteraction)
        {
            return;
        }

        nearest.Interact(transform);
    }

    private Interactable FindNearest()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.down * 0.3f;
        Interactable best = null;
        float bestDistance = interactRange;

        for (int i = 0; i < Interactable.Active.Count; i++)
        {
            Interactable candidate = Interactable.Active[i];
            if (candidate == null || !candidate.HasDialogue || candidate.IsCompleted)
            {
                continue;
            }

            float distance = candidate.DistanceTo(origin);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    private void UpdateFacing()
    {
        if (visual == null || Mathf.Abs(moveInput.x) < 0.01f)
        {
            return;
        }

        Vector3 scale = visual.localScale;
        scale.x = Mathf.Abs(scale.x) * (moveInput.x > 0f ? 1f : -1f);
        visual.localScale = scale;
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsMovingHash, moveInput.sqrMagnitude > 0.01f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.9f);
        Gizmos.DrawWireSphere((Vector2)transform.position + Vector2.down * 0.3f, interactRange);
    }
}
