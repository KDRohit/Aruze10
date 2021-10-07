using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LottoBlastBall : TICoroutineMonoBehaviour
{
    [SerializeField] private LabelWrapperComponent multiplierText;
    [SerializeField] private LabelWrapperComponent jackpotMultiplierText;
    public int multiplierAmount = 20;
    public bool isJackpotBall = false;

    [SerializeField] private AnimationListController.AnimationInformationList[] freeColorAnimationLists;
    [SerializeField] private AnimationListController.AnimationInformationList[] premiumColorAnimationLists;
    [SerializeField] private AnimationListController.AnimationInformationList jackpotColorAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList hideBallAnimationList;

    private int previousColorIndex = -1;
    private bool isPremium;
    private bool isJackpot;

    public int getColorListSize()
    {
        return freeColorAnimationLists.Length;
    }

    public IEnumerator setColor(int index, bool premium, bool isJackpot)
    {
        if (this == null || gameObject == null)
        {
            yield break;
        }
        
        previousColorIndex = index;

        if (!gameObject.activeInHierarchy)
        {
            isPremium = premium;
            this.isJackpot = isJackpot;
            yield break;
        }
        
        if (isJackpot)
        {
            if (premium)
            {
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotColorAnimationList));
            }
            else
            {
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hideBallAnimationList));
            }
        }
        else if (premium)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(premiumColorAnimationLists[index]));
        }
        else
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(freeColorAnimationLists[index]));
        }
    }

    public IEnumerator toggle(bool on)
    {
        if (this == null || gameObject == null || !gameObject.activeInHierarchy)
        {
            yield break;
        }

        if (on)
        {
            //Restore the ball to the previous color when being toggled on
            yield return StartCoroutine(setColor(previousColorIndex, isPremium, isJackpot));
        }
        else
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hideBallAnimationList));
        }
    }

    public void setMultiplierText(bool shouldShow, int newMultiplierAmount)
    {
        multiplierAmount = newMultiplierAmount;

        if (shouldShow)
        {
            multiplierText.text = jackpotMultiplierText.text = "x" + CommonText.formatNumber(multiplierAmount);
        }
        else
        { 
            multiplierText.text = jackpotMultiplierText.text = " ";
        }
    }

    void OnEnable()
    {
        //Set ball color if we previously had the color set
        if (previousColorIndex >= 0)
        {
            StartCoroutine(setColor(previousColorIndex, isPremium, isJackpot));
        }
    }

}
