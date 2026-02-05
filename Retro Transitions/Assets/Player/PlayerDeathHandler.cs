using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Health health;
    [SerializeField] private Animator animator; // optional (camera/arms animator)
    [SerializeField] private MonoBehaviour[] disableOnDeath; // controller, weapon scripts, input, etc.

    [Header("Animation")]
    [SerializeField] private string dieTrigger = "Die";

    [Header("Testing Options")]
    [SerializeField] private bool freezeTimeOnDeath = true;
    [SerializeField] private bool unlockCursorOnDeath = true;
    [SerializeField] private bool reloadSceneAfterDelay = true;
    [SerializeField] private float reloadDelay = 2.0f;

    private bool dead;

    private void Awake()
    {
        if (health == null) health = GetComponent<Health>();

        // If your animator is on weapon/camera, assign it in inspector.
        // This fallback is safe but may find the wrong animator if you have multiple.
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (health != null)
            health.OnDied.AddListener(HandleDeath);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDied.RemoveListener(HandleDeath);
    }

    private void HandleDeath()
    {
        if (dead) return;
        dead = true;

        // Disable gameplay scripts
        if (disableOnDeath != null)
        {
            foreach (var b in disableOnDeath)
                if (b != null) b.enabled = false;
        }

        // Play death animation (optional)
        if (animator != null && !string.IsNullOrEmpty(dieTrigger))
            animator.SetTrigger(dieTrigger);

        // Testing convenience
        if (unlockCursorOnDeath)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (freezeTimeOnDeath)
            Time.timeScale = 0f;

        if (reloadSceneAfterDelay)
            Invoke(nameof(ReloadScene), reloadDelay);
    }

    private void ReloadScene()
    {
        // If timeScale is 0, Invoke won’t tick. So unfreeze before reload.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
