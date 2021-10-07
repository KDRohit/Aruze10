using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EUECharacterItem : MonoBehaviour
{
    [SerializeField] private MultiLabelWrapperComponent bubbleLabel;
    [SerializeField] private UIStretch bubbleStretch;
    public Animator animator;

    public void setText(string locKey)
    {
        if (bubbleLabel != null)
        {
            bubbleLabel.text = Localize.text(locKey);

            //animation prevents stretch from working on first frame, delay a bit
            StartCoroutine(updateStretch(1.0f));
        }
    }

    private IEnumerator updateStretch(float time)
    {
        if (bubbleStretch != null)
        {
            yield return new WaitForSeconds(time);
            bubbleStretch.enabled = true;
        }
    }
}
