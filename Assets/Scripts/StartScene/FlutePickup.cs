using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The second-floor flute. Closing its dialogue grants time travel and enters the gameplay level.
/// </summary>
public class FlutePickup : MonoBehaviour
{
    [SerializeField] private string gameplayScene = "GameplayScene";
    [SerializeField] private Interactable interactable;

    private bool loading;

    private void Awake()
    {
        if (interactable == null)
        {
            interactable = GetComponent<Interactable>();
        }
    }

    private void OnEnable()
    {
        Interactable.Completed += HandleCompleted;
    }

    private void OnDisable()
    {
        Interactable.Completed -= HandleCompleted;
    }

    private void HandleCompleted(Interactable source)
    {
        if (loading || source != interactable || string.IsNullOrEmpty(gameplayScene))
        {
            return;
        }

        loading = true;
        TimeTravel.Grant();
        SceneManager.LoadScene(gameplayScene);
    }
}
