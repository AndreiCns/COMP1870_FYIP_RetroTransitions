using UnityEngine;

public class HealthPickup : PickupBase
{
    [SerializeField] private float healAmount = 25f;
    [SerializeField] private bool consumeIfFullHealth = false;

    protected override bool TryApply(Collider player)
    {
        Health h = player.GetComponent<Health>();
        if (h == null) return false;

        if (!consumeIfFullHealth && h.Current >= h.Max)
            return false;

        h.Heal(healAmount);
        return true;
    }
}