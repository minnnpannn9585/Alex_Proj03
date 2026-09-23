using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Horizontal + vertical parallax background driven by the character's position.
///
/// Two independent background sets are maintained: one for the present and one for
/// the past. Both are scrolled every frame so they stay in sync, while only the set
/// matching the current <see cref="TimeTravel"/> state is shown.
///
/// The parallax is computed from the character's ABSOLUTE position relative to a home
/// baseline captured on start:
///     layer.position = layerHome + (characterPos - characterHome) * parallaxFactor
/// Because it is absolute (not accumulated), any teleport self-corrects: when the
/// character respawns back at spawn the backgrounds snap back to their home layout, and
/// the discrete time-travel teleport is removed via <see cref="TimeTravel.TimeTravelOffsetY"/>
/// so it never produces a vertical parallax jump.
/// </summary>
public class ParallaxScroll : MonoBehaviour
{
    [Serializable]
    public class ParallaxLayer
    {
        [Tooltip("Transform of the background layer to move (usually a SpriteRenderer).")]
        public Transform layer;

        [Tooltip("How much this layer follows the character. 0 = static, 1 = moves exactly with the character. Lower = further away.")]
        public Vector2 parallaxFactor = new Vector2(0.5f, 0.5f);

        [Tooltip("Optional: seamlessly repeat the layer horizontally (requires a wide/tiled sprite).")]
        public bool infiniteHorizontal;

        [HideInInspector] public Vector3 homePosition;
        [HideInInspector] public float spriteWidth;
    }

    [Header("References")]
    [Tooltip("Character/target the parallax is calculated from. Defaults to the object tagged 'Player'.")]
    [SerializeField] private Transform target;

    [Tooltip("Time travel source used to pick the visible set and to ignore the time-travel teleport.")]
    [SerializeField] private TimeTravel timeTravel;

    [Header("Backgrounds")]
    [Tooltip("Layers that make up the present-day background.")]
    [SerializeField] private List<ParallaxLayer> presentLayers = new List<ParallaxLayer>();

    [Tooltip("Layers that make up the past background.")]
    [SerializeField] private List<ParallaxLayer> pastLayers = new List<ParallaxLayer>();

    private Vector3 targetHomePosition;
    private bool initialized;

    private void Start()
    {
        ResolveReferences();

        if (target != null)
        {
            targetHomePosition = GetAdjustedTargetPosition();
        }

        CacheLayers(presentLayers);
        CacheLayers(pastLayers);

        initialized = target != null;

        ApplyVisibility(timeTravel != null && timeTravel.IsInPast);
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            return;
        }

        Vector3 travel = GetAdjustedTargetPosition() - targetHomePosition;

        ApplyParallax(presentLayers, travel);
        ApplyParallax(pastLayers, travel);

        ApplyVisibility(timeTravel != null && timeTravel.IsInPast);
    }

    // Character position with the discrete time-travel teleport removed so vertical
    // parallax reacts only to real movement, not to switching eras.
    private Vector3 GetAdjustedTargetPosition()
    {
        Vector3 position = target.position;
        if (timeTravel != null)
        {
            position.y -= timeTravel.TimeTravelOffsetY;
        }
        return position;
    }

    private void ApplyParallax(List<ParallaxLayer> layers, Vector3 travel)
    {
        foreach (ParallaxLayer entry in layers)
        {
            if (entry == null || entry.layer == null)
            {
                continue;
            }

            float offsetX = travel.x * entry.parallaxFactor.x;
            float offsetY = travel.y * entry.parallaxFactor.y;

            if (entry.infiniteHorizontal && entry.spriteWidth > 0f)
            {
                float half = entry.spriteWidth * 0.5f;
                offsetX = Mathf.Repeat(offsetX + half, entry.spriteWidth) - half;
            }

            Vector3 position = entry.homePosition;
            position.x += offsetX;
            position.y += offsetY;
            entry.layer.position = position;
        }
    }

    private void ApplyVisibility(bool inPast)
    {
        SetLayersActive(presentLayers, !inPast);
        SetLayersActive(pastLayers, inPast);
    }

    private void ResolveReferences()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (timeTravel == null && target != null)
        {
            timeTravel = target.GetComponent<TimeTravel>();
        }

        // Fallback for projects where the character is not tagged "Player":
        // locate the character through its TimeTravel component.
        if (timeTravel == null)
        {
            timeTravel = FindObjectOfType<TimeTravel>();
        }

        if (target == null && timeTravel != null)
        {
            target = timeTravel.transform;
        }
    }

    private static void SetLayersActive(List<ParallaxLayer> layers, bool active)
    {
        foreach (ParallaxLayer entry in layers)
        {
            if (entry == null || entry.layer == null)
            {
                continue;
            }

            if (entry.layer.gameObject.activeSelf != active)
            {
                entry.layer.gameObject.SetActive(active);
            }
        }
    }

    private static void CacheLayers(List<ParallaxLayer> layers)
    {
        foreach (ParallaxLayer entry in layers)
        {
            if (entry == null || entry.layer == null)
            {
                continue;
            }

            entry.homePosition = entry.layer.position;

            SpriteRenderer renderer = entry.layer.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                entry.spriteWidth = renderer.bounds.size.x;
            }
        }
    }
}
