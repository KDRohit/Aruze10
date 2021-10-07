using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchasePerksIcon : MonoBehaviour
{
    [SerializeField] private AnimationListController.AnimationInformationList idleAmbientAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList activeAmbientAnimationList;

    public AdjustObjectColorsByFactor iconDimmer;
    public virtual void init(CreditPackage package, int index, bool isPurchased, RewardPurchaseOffer offer)
    {
    }

    public void setToDimmedVisuals()
    {
        iconDimmer.multiplyColors();
        StartCoroutine(AnimationListController.playListOfAnimationInformation(idleAmbientAnimationList));

    }

    public void setToNormalVisuals()
    {
        iconDimmer.restoreColors();
        StartCoroutine(AnimationListController.playListOfAnimationInformation(activeAmbientAnimationList));
    }
}
