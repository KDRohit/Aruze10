using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialMeter : MonoBehaviour
{
    [SerializeField] private RotationRange range;
    [SerializeField] private Transform pointer;
    
    

    private float maxAmount = 0;
    private float maxTweenTime = 1.1f;

    public float setPointerPosition(float currentValue, float maxValue, bool doAnimation)
    {
        maxAmount = maxValue;
        float newPointerAngle = getPointerRotationAngle(currentValue);
        Vector3 pointerRotation = new Vector3 (pointer.eulerAngles.x, pointer.eulerAngles.y,newPointerAngle);
        if (doAnimation)
        {
            iTween.RotateTo(pointer.gameObject, pointerRotation, maxTweenTime);
            return maxTweenTime;
        }
        else
        {
            pointer.eulerAngles = pointerRotation;
            return 0;
        }
    }

    private IEnumerator animateMeter(Vector3 newRotationVector)
    {
        yield return new TITweenYieldInstruction(iTween.RotateTo(pointer.gameObject, newRotationVector, maxTweenTime));
    }

    private float getPointerRotationAngle(float progressAmount)
    {
        float progressPercent = progressAmount / maxAmount;
        float absRotationAmount = Mathf.Abs(range.maxRotation - range.minRotation);
        return range.minRotation - progressPercent * absRotationAmount;
    }

    [System.Serializable]
    private class RotationRange
    {
        public float minRotation;
        public float maxRotation;
    }
}
