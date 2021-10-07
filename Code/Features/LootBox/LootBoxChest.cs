using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootBoxChest : MonoBehaviour
{
    [SerializeField] private AnimationListController.AnimationInformationList introAnimList;

    public void startIntroAnimation()
    {
        StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimList));
    }
}
