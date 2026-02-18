using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Health health;
    [SerializeField] private Animator animator; // optional
    [SerializeField] private MonoBehaviour[] disableOnDeath;

    [Header("Animation")]
    [SerializeField] private string dieTrigger = "Die";

    [Header("Testing Options")]
    [SerializeField] private bool freezeTimeOnDeath = true;
    [SerializeField] private bool unlockCursorOnDeath = true;
    [SerializeField] private bool reloadSceneAfterDelay = true;
    [SerializeField] private float reloadDelay = 2.0f;

    private bool dead;
    private Coroutine reloadRoutine;

    private void Awake()
    {
        if (health == null) health = GetComponent<Health>();

        // Inspector is best, this is just a fallback
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

        // Stop gameplay scripts
        if (disableOnDeath != null)
        {
            foreach (var b in disableOnDeath)
                if (b != null) b.enabled = false;
        }

        // Optional death trigger
        if (animator != null && !string.IsNullOrEmpty(dieTrigger))
            animator.SetTrigger(dieTrigger);

        if (unlockCursorOnDeath)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Use realtime wait so reload still happens even if timescale is 0
        if (reloadSceneAfterDelay)
        {
            if (reloadRoutine != null) StopCoroutine(reloadRoutine);
            reloadRoutine = StartCoroutine(ReloadAfterDelayRealtime(reloadDelay));
        }

        if (freezeTimeOnDeath)
            Time.timeScale = 0f;
    }

    private IEnumerator ReloadAfterDelayRealtime(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
