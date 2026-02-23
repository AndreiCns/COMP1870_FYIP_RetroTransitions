using System.Collections.Generic;
using UnityEngine;

public class GameplayPauseManager : MonoBehaviour
{
    private readonly List<IGameplayPausable> pausable = new List<IGameplayPausable>();

    public void Register(IGameplayPausable target)
    {
        if (target == null)
            return;

        if (!pausable.Contains(target))
            pausable.Add(target);
    }

    public void Unregister(IGameplayPausable target)
    {
        if (target == null)
            return;

        pausable.Remove(target);
    }

    public void SetPaused(bool paused)
    {
        for (int i = 0; i < pausable.Count; i++)
            pausable[i].SetPaused(paused);
    }
}