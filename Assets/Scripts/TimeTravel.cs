using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTravel : MonoBehaviour
{
    [SerializeField] private KeyCode timeTravelKey = KeyCode.F;
    [SerializeField] private float travelDistance = 30f;

    private bool isInPast;

    public bool IsInPast => isInPast;

    // Vertical world-space offset the character currently has purely because of time
    // travelling. Entering the past moves the character down by travelDistance, so the
    // offset is negative while in the past. Consumers (e.g. parallax) subtract this to
    // ignore the discrete teleport when reacting to the character's position.
    public float TimeTravelOffsetY => isInPast ? -travelDistance : 0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(timeTravelKey))
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
        float offsetY = isInPast ? travelDistance : -travelDistance;
        transform.position += new Vector3(0f, offsetY, 0f);
        isInPast = !isInPast;
    }
}
