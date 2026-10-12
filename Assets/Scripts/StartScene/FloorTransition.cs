using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sends the player to the next floor after the required talks, then hides the floor they left.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FloorTransition : MonoBehaviour
{
    [SerializeField] private string[] requiredIds = { "parents", "painting", "brother" };
    [SerializeField] private GameObject[] firstFloor;
    [SerializeField] private GameObject secondFloor;
    [SerializeField] private string blockedLine = "Talk to your parents, the painting, and your brother before going upstairs.";

    private readonly HashSet<string> completed = new HashSet<string>();
    private Vector3 startPosition;
    private Collider2D playerInside;
    private bool transferred;

    private void Awake()
    {
        if (secondFloor != null)
        {
            secondFloor.SetActive(false);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            startPosition = player.transform.position;
        }
    }

    private void OnEnable()
    {
        Interactable.Completed += HandleInteractionCompleted;
    }

    private void OnDisable()
    {
        Interactable.Completed -= HandleInteractionCompleted;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (transferred || !other.CompareTag("Player"))
        {
            return;
        }

        playerInside = other;
        if (IsReady())
        {
            Transfer(other);
            return;
        }

        ShowBlocked();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (playerInside == other)
        {
            playerInside = null;
        }
    }

    private void HandleInteractionCompleted(Interactable source)
    {
        if (transferred || source == null || string.IsNullOrEmpty(source.InteractionId))
        {
            return;
        }

        completed.Add(source.InteractionId);
        if (playerInside != null && IsReady())
        {
            Transfer(playerInside);
        }
    }

    private bool IsReady()
    {
        if (requiredIds == null || requiredIds.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < requiredIds.Length; i++)
        {
            if (!completed.Contains(requiredIds[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void ShowBlocked()
    {
        DialogueManager dialogue = DialogueManager.Instance;
        if (dialogue == null || !dialogue.CanStartInteraction || string.IsNullOrWhiteSpace(blockedLine))
        {
            return;
        }

        dialogue.StartDialogue(string.Empty, new[] { blockedLine });
    }

    private void Transfer(Collider2D playerCollider)
    {
        if (transferred || playerCollider == null)
        {
            return;
        }

        transferred = true;
        playerInside = null;

        Rigidbody2D body = playerCollider.attachedRigidbody;
        if (body != null)
        {
            body.velocity = Vector2.zero;
            body.position = startPosition;
        }

        playerCollider.transform.position = startPosition;

        if (secondFloor != null)
        {
            secondFloor.SetActive(true);
        }

        if (firstFloor == null)
        {
            return;
        }

        for (int i = 0; i < firstFloor.Length; i++)
        {
            if (firstFloor[i] != null)
            {
                firstFloor[i].SetActive(false);
            }
        }
    }
}
