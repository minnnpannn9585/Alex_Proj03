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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (visual == null && transform.childCount > 0)
        {
            visual = transform.GetChild(0);
        }

        if (animator == null && visual != null)
        {
            animator = visual.GetComponent<Animator>();
        }
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
        rb.velocity = moveInput * moveSpeed;
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
            if (candidate == null || !candidate.HasDialogue)
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
