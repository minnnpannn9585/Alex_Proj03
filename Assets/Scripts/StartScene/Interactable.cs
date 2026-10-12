using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on a character or prop, then fill in the speaker name and lines.
/// The player talks to it with E when standing next to its collider.
/// </summary>
public class Interactable : MonoBehaviour
{
    public static readonly List<Interactable> Active = new List<Interactable>();
    public static event Action<Interactable> Completed;

    private static readonly HashSet<string> completedIds = new HashSet<string>();

    [Tooltip("相同 id 只计一次。爸和妈都填 parents，这样跟其中一位说话就算和爸妈交互过。")]
    [SerializeField] private string interactionId;

    [Tooltip("对话框左上角显示的名字。")]
    [SerializeField] private string speakerName;

    [Tooltip("靠近时提示里的动作，例如 交谈、阅读、查看。")]
    [SerializeField] private string prompt = "Interact";

    [TextArea(2, 4)]
    [SerializeField] private string[] lines;

    [Tooltip("对话时让侧视角色转向玩家。告示、石狮这类物体保持关闭。")]
    [SerializeField] private bool facePlayer = true;

    [Tooltip("按 E 提示相对物体中心的高度。")]
    [SerializeField] private float promptHeight = 1.2f;

    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private bool completedSelf;

    public string InteractionId => interactionId;
    public string Prompt => prompt;
    public Vector3 PromptPosition => transform.position + Vector3.up * promptHeight;
    public bool HasDialogue => lines != null && lines.Length > 0;
    public bool IsCompleted => string.IsNullOrEmpty(interactionId) ? completedSelf : completedIds.Contains(interactionId);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        completedIds.Clear();
        Active.Clear();
        Completed = null;
    }

    private void Awake()
    {
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (!Active.Contains(this))
        {
            Active.Add(this);
        }
    }

    private void OnDisable()
    {
        Active.Remove(this);
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.Closed -= HandleDialogueClosed;
        }
    }

    public float DistanceTo(Vector2 point)
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (bodyCollider != null)
        {
            return Vector2.Distance(point, bodyCollider.ClosestPoint(point));
        }

        return Vector2.Distance(point, transform.position);
    }

    public void Interact(Transform interactor)
    {
        if (facePlayer && interactor != null)
        {
            Face(interactor.position);
        }

        OnInteracted(interactor);

        if (IsCompleted)
        {
            return;
        }

        if (!HasDialogue || DialogueManager.Instance == null)
        {
            Complete();
            return;
        }

        DialogueManager.Instance.Closed -= HandleDialogueClosed;
        DialogueManager.Instance.Closed += HandleDialogueClosed;
        DialogueManager.Instance.StartDialogue(speakerName, lines);
    }

    private void HandleDialogueClosed()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.Closed -= HandleDialogueClosed;
        }

        Complete();
    }

    private void Complete()
    {
        if (IsCompleted)
        {
            return;
        }

        if (string.IsNullOrEmpty(interactionId))
        {
            completedSelf = true;
        }
        else
        {
            completedIds.Add(interactionId);
        }

        Completed?.Invoke(this);
    }

    protected virtual void OnInteracted(Transform interactor)
    {
    }

    private void Face(Vector3 targetPosition)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        float direction = targetPosition.x - transform.position.x;
        if (Mathf.Abs(direction) < 0.05f)
        {
            return;
        }

        spriteRenderer.flipX = direction < 0f;
    }
}
