using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using UnityEngine;

public class VirtualPet : TICoroutineMonoBehaviour
{
    [SerializeField] private AnimationListController.AnimationInformationList respinIntroAnimationsList;
    [SerializeField] private AnimationListController.AnimationInformationList respinBonusAnimationsList;
    [SerializeField] private AnimationListController.AnimationInformationList respinOutroAnimationsList;
    [SerializeField] private AnimationListController.AnimationInformationList noRespinIntroAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList noRespinBonusAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList noRespinOutroAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList pettingAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList feedAnimationsList;
    [SerializeField] private AnimationListController.AnimationInformationList treatPanelClickedAnimationsList;
    [SerializeField] private AnimationListController.AnimationInformationList trickPanelClickedAnimationsList;
    [SerializeField] private AnimationListController.AnimationInformationList energyRewardAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList rewardsCelebrationAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList nameChangeAnimationsList;
    [SerializeField] private AnimationListController.AnimationInformationList eatingSpecialTreatAnimationsList;
    [SerializeField] private AnimationListController.AnimationInformationList firstTreatIntroList;

    [SerializeField] private ReactionsAnimationList idleAnimations;
    [SerializeField] private ReactionsAnimationList nonRewardPettingReactions;
    [SerializeField] private ReactionsAnimationList eatingNonFinalTreatReactions;
    [SerializeField] private ReactionsAnimationList eatingFinalTreatReactions;

    private const float MAX_LOW_ENERGY = 0.5f;
    private const float MAX_MEDIUM_ENERGY = 0.99f;
    
    public bool isPlayingReaction { get; private set; }

    public IEnumerator playIdleAnimations()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(idleAnimations.getEnergyBasedReaction()));
    }
    
    public IEnumerator playRespinIntroAnimations()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(respinIntroAnimationsList));
    }

    public IEnumerator playNoRespinIntroAnimations()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(noRespinIntroAnimationList));
    }
    
    public IEnumerator playRespinBonusAnimations(bool didFakeSpin)
    {
        if (didFakeSpin)
        {
            yield return StartCoroutine(
                AnimationListController.playListOfAnimationInformation(respinBonusAnimationsList));
        }
        else
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(noRespinBonusAnimationList));
        }
    }
    
    public IEnumerator playRespinOutroAnimations(bool didFakeSpin)
    {
        if (didFakeSpin)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(respinOutroAnimationsList));
        }
        else
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(noRespinOutroAnimationList));
        }
        
    }
    
    public IEnumerator playPettingAnimation()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(pettingAnimationList));
    }

    public IEnumerator playFeedAnimation()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(feedAnimationsList));
    }
    
    public IEnumerator playFirstTreatIntro()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(firstTreatIntroList));
    }

    public IEnumerator playTreatPanelClickedAnimation()
    {
        isPlayingReaction = true;
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(treatPanelClickedAnimationsList));
        isPlayingReaction = false;
    }
    
    public IEnumerator playTricktPanelClickedAnimation()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(trickPanelClickedAnimationsList));
    }

    public IEnumerator playPettingEnergyRewardAnimation()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(energyRewardAnimationList));
    }

    public IEnumerator playRandomNonRewardReaction()
    {
        if (!isPlayingReaction)
        {
            isPlayingReaction = true;
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(nonRewardPettingReactions.getEnergyBasedReaction()));
            isPlayingReaction = false;
        }
    }
    
    public IEnumerator playRandomNonFinalTreatReaction()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(eatingNonFinalTreatReactions.getEnergyBasedReaction()));
    }
    
    public IEnumerator playRandomFinalTreatReaction()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(eatingFinalTreatReactions.getEnergyBasedReaction()));
    }

    public IEnumerator playSpecialTreatReaction()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(eatingSpecialTreatAnimationsList));
    }

    public IEnumerator playCelebration()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rewardsCelebrationAnimationList));
    }
    
    public IEnumerator playNameChangedAnimations()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(nameChangeAnimationsList));
    }

    [System.Serializable]
    private class WeightedPetAnimationList
    {
        public float weight;
        public AnimationListController.AnimationInformationList animList;
    }

    [System.Serializable]
    private class ReactionsAnimationList
    {
        private float[] lowEnergyAnimWeights;
        private float[] mediumEnergyAnimWeights;
        private float[] highEnergyAnimWeights;
        
        [SerializeField] private WeightedPetAnimationList[] lowEnergyAnimationList;
        [SerializeField] private WeightedPetAnimationList[] mediumEnergyAnimationList;
        [SerializeField] private WeightedPetAnimationList[] highEnergyAnimationList;
        
        private AnimationListController.AnimationInformationList getWeightedAnimList(float[] weights, WeightedPetAnimationList[] animationLists)
        {
            if (weights == null)
            {
                weights = new float[animationLists.Length];
                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] = animationLists[i].weight;
                }        
            }
        
            int chosenIndex = CommonMath.chooseRandomWeightedValue(weights);
            return animationLists[chosenIndex].animList;
        }
        
        public AnimationListController.AnimationInformationList getEnergyBasedReaction()
        {
            float energyPercent = (float)VirtualPetsFeature.instance.currentEnergy / VirtualPetsFeature.instance.maxEnergy;
            if (VirtualPetsFeature.instance.isHyper)
            {
                return getWeightedAnimList(highEnergyAnimWeights, highEnergyAnimationList);
            }
        
            if (energyPercent < MAX_LOW_ENERGY)
            {
                return getWeightedAnimList(lowEnergyAnimWeights, lowEnergyAnimationList);
            }

            return getWeightedAnimList(mediumEnergyAnimWeights, mediumEnergyAnimationList);
        }
    }
}
