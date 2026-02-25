using UnityEngine;

public class KeyPickup : PickupBase
{
    [SerializeField] private KeyType keyType;

    protected override bool TryApply(Collider player)
    {
        PlayerCombatState state = player.GetComponent<PlayerCombatState>();
        if (state == null) return false;

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

        return true;
    }
}