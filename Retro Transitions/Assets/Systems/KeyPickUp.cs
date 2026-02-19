using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KeyPickup : MonoBehaviour
{
    [SerializeField] private KeyType keyType;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerCombatState state = other.GetComponent<PlayerCombatState>();
        if (state == null) return;

        switch (keyType)
        {
            case KeyType.Blue:
                state.GrantKeyBlue();
                break;
            case KeyType.Yellow:
                state.GrantKeyYellow();
                break;
            case KeyType.Red:
                state.GrantKeyRed();
                break;
        }

        Destroy(gameObject);
    }
}