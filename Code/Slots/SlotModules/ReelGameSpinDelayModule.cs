using UnityEngine;
using System.Collections;

public class ReelGameSpinDelayModule : SlotModule 
{
    [SerializeField] private float spinDelay = 0.5f;

    public override bool needsToExecuteOnPreSpin()
    {
        return true;
    }

    public override IEnumerator executeOnPreSpin()
    {
        yield return new TIWaitForSeconds(spinDelay);
    }
}
