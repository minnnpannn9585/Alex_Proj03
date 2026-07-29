using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTravel : MonoBehaviour
{
    [SerializeField] private KeyCode timeTravelKey = KeyCode.F;
    [SerializeField] private float travelDistance = 30f;

    private bool isInPast;

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

    private void ToggleTimeTravel()
    {
        float offsetY = isInPast ? travelDistance : -travelDistance;
        transform.position += new Vector3(0f, offsetY, 0f);
        isInPast = !isInPast;
    }
}
