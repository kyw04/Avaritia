using System;
using System.Collections;
using UnityEngine;

[System.Serializable]
public class PeriodicTrigger : IAbilityTrigger
{
    public float interval;
    private Coroutine routine;

    public void Bind(Entity owner, Action<AbilityContext> fire)
    {
        if (routine != null) return;
        routine = owner.StartCoroutine(Loop(owner, fire));
    }

    public void Unbind(Entity owner)
    {
        if (routine != null) owner.StopCoroutine(routine);
        routine = null;
    }

    private IEnumerator Loop(Entity owner, Action<AbilityContext> fire)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            fire(new AbilityContext { Caster = owner, Target = owner.transform });
        }
    }
}
