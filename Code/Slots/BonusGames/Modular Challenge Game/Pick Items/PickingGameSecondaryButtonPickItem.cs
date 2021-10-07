using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickingGameSecondaryButtonPickItem : PickingGameBasePickItemAccessor
{
    [SerializeField] private Collider buttonCollider;
    [SerializeField] private AnimationListController.AnimationInformationList buttonEnabledAnims;
    [SerializeField] private AnimationListController.AnimationInformationList buttonDisabledAnims;

    public IEnumerator setClickableCoroutine(bool enabled)
    {
        buttonCollider.enabled = enabled;
        if (enabled)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(buttonEnabledAnims));
        }
        else
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(buttonDisabledAnims));
        }
    }
}
