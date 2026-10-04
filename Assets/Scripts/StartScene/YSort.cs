using UnityEngine;

/// <summary>
/// Draws lower objects in front of higher ones, so the flat map reads as a top-down space.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class YSort : MonoBehaviour
{
    [SerializeField] private int orderBias;

    private SpriteRenderer spriteRenderer;

    private void LateUpdate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sortingOrder = 500 + Mathf.RoundToInt(-transform.position.y * 20f) + orderBias;
    }
}
