using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ladder : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.EnterLadder();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.ExitLadder();
        }
    }
}
