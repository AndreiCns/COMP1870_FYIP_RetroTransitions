using UnityEngine;

public class TutorialPanelController : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPanel;

    private bool isOpen = true; // Start open

    private void Awake()
    {
        if (tutorialPanel == null)
        {
            Debug.LogWarning("TutorialPanelController: No panel assigned.");
            enabled = false;
            return;
        }

        // Start open
        tutorialPanel.SetActive(true);
        ApplyState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePanel();
        }
    }

    private void TogglePanel()
    {
        isOpen = !isOpen;
        tutorialPanel.SetActive(isOpen);
        ApplyState();
    }

    private void ApplyState()
    {
        Time.timeScale = isOpen ? 0f : 1f;

        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;
    }
}