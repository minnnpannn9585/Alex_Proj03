using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeactivateBtn : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Linked Buttons")]
    [SerializeField] private DeactivateBtn otherButton;

    [Header("Objects To Disable")]
    [SerializeField] private GameObject[] poweredObjects;

    private bool playerInRange;
    private bool isActivated;

    public bool IsActivated => isActivated;

    private void Update()
    {
        if (!playerInRange || isActivated)
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            ActivateButton();
        }
    }

    private void ActivateButton()
    {
        isActivated = true;

        if (otherButton != null && otherButton.IsActivated)
        {
            DisablePoweredObjects();
            otherButton.DisablePoweredObjects();
        }
    }

    private void DisablePoweredObjects()
    {
        if (poweredObjects == null)
        {
            return;
        }

        for (int i = 0; i < poweredObjects.Length; i++)
        {
            if (poweredObjects[i] != null)
            {
                poweredObjects[i].SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerMovement>() != null)
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerMovement>() != null)
        {
            playerInRange = false;
        }
    }
}
