using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTravel : MonoBehaviour
{
    public static bool HasAbility { get; private set; }

    [SerializeField] private KeyCode timeTravelKey = KeyCode.F;
    [SerializeField] private float travelDistance = 30f;

    private bool isInPast;

    public bool IsInPast => isInPast;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAbility()
    {
        HasAbility = false;
    }

    public static void Grant()
    {
        HasAbility = true;
    }

    // Vertical world-space offset the character currently has purely because of time
    // travelling. Entering the past moves the character down by travelDistance, so the
    // offset is negative while in the past. Consumers (e.g. parallax) subtract this to
    // ignore the discrete teleport when reacting to the character's position.
    public float TimeTravelOffsetY => isInPast ? -travelDistance : 0f;

    private void Awake()
    {
        if (!HasAbility)
        {
            Grant();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (HasAbility && Input.GetKeyDown(timeTravelKey))
        {
            ToggleTimeTravel();
        }
    }

    public void ReturnToModernTime()
    {
        if (!isInPast)
        {
            return;
        }

        ToggleTimeTravel();
    }

    private void ToggleTimeTravel()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.CancelSwing();
        }

        float offsetY = isInPast ? travelDistance : -travelDistance;
        transform.position += new Vector3(0f, offsetY, 0f);
        isInPast = !isInPast;
    }
}
