using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.HitItRich.Feature.VirtualPets;

public class VirtualPetEnergyMeter : TICoroutineMonoBehaviour
{
    [SerializeField] private DialMeter energyMeter;
    [SerializeField] private LabelWrapperComponent hyperLabel;
    [SerializeField] private AnimationListController.AnimationInformationList hyperOnAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList hyperOffAnimationList;
    [SerializeField] private GradientTintController meterGradientTintController;
    [SerializeField] private AnimationListController.AnimationInformationList meterUpdateAnimationList;


    private const float MAX_NON_HYPER_AMOUNT = 0.8f; //Max amount the meter will show when not in hyper mode. Its possible to have full energy but not be in hyper mode so we don't want the energy meter to actually look full
    private float previousEnergy = -1;
    private bool isShowingHyper = false;
    private GameTimerRange hyperTimer = null; //we have to clone this to show updates, we can't register to the virutal pets timer directly
    private bool hasAnimatedHyperMeter = false;
    public void init(float currentEnergy, int hyperEndTime)
    {
        previousEnergy = currentEnergy;
        energyMeter.setPointerPosition(getPointerEnergyAmount(currentEnergy), VirtualPetsFeature.instance.maxEnergy, false);
        meterGradientTintController.updateColor(currentEnergy/(float)VirtualPetsFeature.instance.maxEnergy, true);
        if (hyperEndTime > GameTimer.currentTime)
        {
            isShowingHyper = true;
            if (hyperLabel != null)
            {
                hyperTimer = GameTimerRange.createWithTimeRemaining(hyperEndTime - GameTimer.currentTime);
                hyperTimer.registerLabel(hyperLabel.tmProLabel);
                animateHyperMeterTimer();
            }
        }
    }

    private void animateHyperMeterTimer()
    {
        if (isShowingHyper && !hasAnimatedHyperMeter)
        {
            hasAnimatedHyperMeter = true;
            StartCoroutine(AnimationListController.playListOfAnimationInformation(hyperOnAnimationList));
        }
    }

    private void OnEnable()
    {
        animateHyperMeterTimer();
    }

    public IEnumerator updateEnergy(float newEnergyAmount, int hyperEndTime)
    {
        float maxTweenTime = energyMeter.setPointerPosition(getPointerEnergyAmount(newEnergyAmount), VirtualPetsFeature.instance.maxEnergy, true);
        if (maxTweenTime > 0)
        {
            yield return new TIWaitForSeconds(maxTweenTime);
        }
        
        meterGradientTintController.updateColor(newEnergyAmount / (float) VirtualPetsFeature.instance.maxEnergy, true);

        bool didUpdateEnergy = false;
        if (newEnergyAmount > previousEnergy)
        {
            didUpdateEnergy = true;
            previousEnergy = newEnergyAmount;
        }
        if (!isShowingHyper && hyperEndTime > GameTimer.currentTime)
        {
            if (didUpdateEnergy)
            {
                yield return StartCoroutine(playUpdateAnimations(hyperEndTime));
            }
            isShowingHyper = true;
            if (hyperLabel != null)
            {
                hyperTimer = GameTimerRange.createWithTimeRemaining(hyperEndTime - GameTimer.currentTime);
                hyperTimer.registerLabel(hyperLabel.tmProLabel);
            }
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hyperOnAnimationList));
        }
        else if (isShowingHyper && hyperTimer != null && hyperEndTime > GameTimer.currentTime)
        {
            if (didUpdateEnergy)
            {
                yield return StartCoroutine(playUpdateAnimations(hyperEndTime));
            }
            //update the timer end time
            hyperTimer.clearLabels();
            hyperTimer = GameTimerRange.createWithTimeRemaining(hyperEndTime - GameTimer.currentTime);
            hyperTimer.registerLabel(hyperLabel.tmProLabel);
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hyperOnAnimationList));
        }
        else
        {
            //always play update animation
            yield return StartCoroutine(playUpdateAnimations(hyperEndTime));
        }
    }

    private IEnumerator playUpdateAnimations(int hyperEndTime)
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(meterUpdateAnimationList));
        if (hyperEndTime > GameTimer.currentTime)
        {
            if (!isShowingHyper)
            {
                isShowingHyper = true;
                if (hyperLabel != null)
                {
                    hyperTimer = GameTimerRange.createWithTimeRemaining(hyperEndTime - GameTimer.currentTime);
                    hyperTimer.registerLabel(hyperLabel.tmProLabel);
                }
                
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hyperOnAnimationList));
            }
            else if (isShowingHyper && hyperTimer != null && hyperEndTime > hyperTimer.endTimestamp)
            {
                //update the timer end time
                hyperTimer.clearLabels();
                hyperTimer = GameTimerRange.createWithTimeRemaining(hyperEndTime - GameTimer.currentTime);
                hyperTimer.registerLabel(hyperLabel.tmProLabel);
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hyperOnAnimationList));
            }
        }
    }

    public IEnumerator turnOffHyperMode()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hyperOffAnimationList));
    }

    //Visual cap to energy meter.
    //Don't want to show the meter above a specified value if we're not in hyper mode
    private float getPointerEnergyAmount(float energyAmount)
    {
        if (!VirtualPetsFeature.instance.isHyper)
        {
            if ((float) energyAmount / VirtualPetsFeature.instance.maxEnergy > MAX_NON_HYPER_AMOUNT)
            {
                return MAX_NON_HYPER_AMOUNT * VirtualPetsFeature.instance.maxEnergy;
            }
        }

        return energyAmount;
    }
}
