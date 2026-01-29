using UnityEngine;

public interface IEnemyAttack
{
    bool CanAttack(Transform target);
    void TickAttack(Transform target); // called by brain when in combat
}
