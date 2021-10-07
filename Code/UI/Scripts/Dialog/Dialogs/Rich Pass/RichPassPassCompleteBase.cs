using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RichPassPassCompleteBase : MonoBehaviour
{
    [SerializeField] private AnimationListController.AnimationInformationList introAnimationInformationList;

    public virtual void init(SlideController parentSlider, RichPassRewardTrackSegmentEnd segmentEnd)
    {
        StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimationInformationList));
    }

    public virtual void unlock()
    {
        
    }
}
