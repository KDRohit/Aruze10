using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MultiReelWildBannerReplacementModuleWith2xBanner : MultiReelWildBannerReplacementModule
{
    private bool firstBanner = true;

    [Header("Preplace Effects Data")]
    [SerializeField]
    private List<AnimationListController.AnimationInformationList> bonusPrePlaceEffectAnimationInfo;

    [Header("Wild Banner Data")]
    [SerializeField] protected bool usingDoubleBanners = true;
    //2x Banners list
    [SerializeField] private List<GameObject> doubleBannerObjects = new List<GameObject>();
    //The Banners animation info
    [SerializeField] private float WAIT_BETWEEN_PREANIMATIONS = 0.0f;
    [SerializeField] private float BANNER_ENABLE_DELAY = 0.0f;

	[SerializeField] private bool playTeaserPrePlaceEffects = true; //Plays the pre place effect on reels even if the banner isn't expanding on it

    //Since we want to do the same thing on both module hooks so why write the code twice
    protected override IEnumerator handleBannerMutations()
    {
        if(INTRO_SOUND_BG != "")
        {
			Audio.playSoundMapOrSoundKey(INTRO_SOUND_BG);
        }
        List<int> reelIDs = new List<int>();
        //List for allowing the preanimations to play randomly
        List<int> randomOrder = new List<int>(new int[] { 0,1,2,3,4 });
        //Do the intro animations 
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimationInfo));
        Audio.playSoundMapOrSoundKeyWithDelay(VERTICAL_WILD_REVEAL_VO, WILD_REVEAL_VO_DELAY);
        if (featureMutation.type == "multi_reel_advanced_replacement")
        {
            for (int i = 0; i < featureMutation.mutatedReels.Length; i++)
            {
                for (int j = 0; j < featureMutation.mutatedReels[i].Length; j++)
                {
                    reelIDs.Add(featureMutation.mutatedReels[i][j]);
                    //string symbol = featureMutation.mutatedSymbols[i];
                    //TODO-HANS -- set this up so we can have specific banners keyed to specific symbols? 
                }
            }
        }
        else
        {
            for (int i = 0; i < featureMutation.reels.Length; i++)
            {
                int reelID = featureMutation.reels[i];
                StartCoroutine(doBannerAnimationSequences(reelID));
            }
        }
        CommonDataStructures.shuffleList(randomOrder);
        for (int i = 0; i < randomOrder.Count; i++)
        {
            yield return new TIWaitForSeconds(WAIT_BETWEEN_PREANIMATIONS);
            if (reelIDs.Contains(randomOrder[i]) && firstBanner && usingDoubleBanners)
            {
                StartCoroutine(AnimationListController.playListOfAnimationInformation(bonusPrePlaceEffectAnimationInfo[randomOrder[i]]));
                yield return new TIWaitForSeconds(WAIT_BETWEEN_PREANIMATIONS);
                StartCoroutine(doBannerAnimationSequences(randomOrder[i]));
            }
            else if(reelIDs.Contains(randomOrder[i]) && (!firstBanner || !usingDoubleBanners))
            {
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(prePlaceEffectAnimationInfo[randomOrder[i]]));
                yield return new TIWaitForSeconds(WAIT_BETWEEN_PREANIMATIONS);
                StartCoroutine(doBannerAnimationSequences(randomOrder[i]));
            }
			else if (playTeaserPrePlaceEffects)
            {
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(prePlaceEffectAnimationInfo[randomOrder[i]]));
            }

        }
    }

    //Making this virtual incase any future module need to override this for any game specific flow
    protected override IEnumerator doBannerAnimationSequences(int reelID)
    {
        //Turn the banner on
        if (reelID < bannerObjects.Count)
        {          
            if (firstBanner && usingDoubleBanners)
            {
                firstBanner = false;
                yield return new TIWaitForSeconds(BANNER_ENABLE_DELAY);
                doubleBannerObjects[reelID].SetActive(true);
                Animator doubleBannerObjectAnimator = bannerObjects[reelID].GetComponent<Animator>();
                firstBanner = false;
                // This forces the default animation to play from the beginning instead of playing a frame where it was left off on the last spin
                if (doubleBannerObjectAnimator != null)
                {
                    doubleBannerObjectAnimator.Update(0.0f);
                }
            }
            else
            {
                yield return new TIWaitForSeconds(BANNER_ENABLE_DELAY);
                bannerObjects[reelID].SetActive(true);
                Animator bannerObjectAnimator = bannerObjects[reelID].GetComponent<Animator>();
                if (bannerObjectAnimator != null)
                {
                    bannerObjectAnimator.Update(0.0f);
                }
            }
        }
        yield break;
    }

    public override IEnumerator executeOnPreSpin()
    {
        foreach (GameObject banner in bannerObjects)
        {
            banner.SetActive(false);
        }
        foreach (GameObject banner in doubleBannerObjects)
        {
            banner.SetActive(false);
        }
        firstBanner = true;
        yield break;
    }
}

