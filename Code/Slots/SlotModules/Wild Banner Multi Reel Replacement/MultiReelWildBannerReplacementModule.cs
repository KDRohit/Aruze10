using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//This class is designed to do the same thing as the now DeprecatedWildBannerMultiReelReplacementModule
//However this will use the new AnimationInfo classes to couple alot of logic and animation and hopefully
//make the process of setting these features up a lot less painful
public class MultiReelWildBannerReplacementModule : SlotModule
{
	//Helper Enum lets use determine when to do the banner presentation
	public enum PresentationType
	{
		DuringReelSpin = 0,
		OnReelsStop = 1
	}
	[Header("Module Timing")]
	[SerializeField] protected PresentationType presentationType = PresentationType.DuringReelSpin;

	[Header("Intro Data")]
	//The background music key
	[SerializeField] protected string INTRO_SOUND_BG = "basegame_vertical_wild_bg";
	//This is a list incase we need a sequence of animations 
	[SerializeField] protected AnimationListController.AnimationInformationList introAnimationInfo;

	[Header("Preplace Effects Data")]
	//These are Lists of AnimationInformation so we can play an animation for reel index n
	[SerializeField] protected List<AnimationListController.AnimationInformationList> prePlaceEffectAnimationInfo;
	[SerializeField] private float DELAY_BETWEEN_BANNERS = 0.0f;
	[SerializeField] private float DELAY_AFTER_BANNERS = 0.0f; // delay after revealing banners prior to stopping reels

	[Header("Wild Banner Data")]
	[SerializeField] private bool shouldAnimateWithPaylines = true;
	[SerializeField] private bool shouldEnableDisableWithAnimation = false; // if true, use animations instead of activating / deactivated gameobjects
	private bool[] bannersActivated; // store banner states prior to deactivation from a big win for restoration
	[SerializeField] private float bannerRevealLength = 0.0f; //Used when the feature is during the reel spin. Lets us know when to start stopping the reels. 

	//These are the expanded banners
	[SerializeField] protected List<GameObject> bannerObjects = new List<GameObject>();
	[SerializeField] protected string BANNER_SOUND_KEY = "";
	[SerializeField] protected string FINAL_BANNER_SOUND_KEY = "";
	
	//The Banners animation info
	[SerializeField] private List<AnimationListController.AnimationInformation> bannerPaylineAnimationInfo;
	[SerializeField] private List<AnimationListController.AnimationInformation> bannerDisableAnimations;
	[SerializeField] private float POST_BANNERS_REVEAL_WAIT = 0.0f; //Let our banners finish any effects before starting paylines. 


	//This will keep track of the current mutation for this module
	protected StandardMutation featureMutation;

	[SerializeField] protected string VERTICAL_WILD_REVEAL_VO = "basegame_vertical_wild_reveal_vo";
	[SerializeField] protected float WILD_REVEAL_VO_DELAY = 0.0f;
	[SerializeField] private string VERTICAL_WILD_END_SOUND = "basegame_vertical_wild_bg_end";
	[SerializeField] private float featureEndSoundDelay = 0.0f;
	[SerializeField] private bool mutateToTallSymbolOnNextSpin = false;
	[SerializeField] private bool mutateToSmallSymbolsOnNextSpin = false;

	protected bool isFinalBanner = false;
	
	//We will get the mutations here even though we might not handle them until the reels have stopped
	public override bool needsToExecutePreReelsStopSpinning()
	{
		featureMutation = null;
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mutation = baseMutation as StandardMutation;
			if (mutation.type == "multi_reel_replacement" || mutation.type == SlotOutcome.REEVALUATION_TYPE_SPOTLIGHT)
			{
				featureMutation = mutation;
				System.Array.Sort(featureMutation.reels);				
			}
			else if (mutation.type == "multi_reel_advanced_replacement")
			{
				featureMutation = mutation;				
			}
		}
		return (presentationType == PresentationType.DuringReelSpin) && (featureMutation != null);
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		yield return StartCoroutine(handleBannerMutations());
		Audio.playSoundMapOrSoundKeyWithDelay(VERTICAL_WILD_END_SOUND, featureEndSoundDelay);
		yield return new TIWaitForSeconds(bannerRevealLength);
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return featureMutation != null;
	}
		
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (presentationType == PresentationType.OnReelsStop)
		{
			yield return StartCoroutine(handleBannerMutations());
		}

		mutateVisibleSymbols();
		yield return new TIWaitForSeconds(POST_BANNERS_REVEAL_WAIT);
	}

	private void mutateVisibleSymbols()
	{
		if (featureMutation.type == "multi_reel_advanced_replacement" && mutateToTallSymbolOnNextSpin)
		{
			for (int i = 0; i < featureMutation.mutatedReels.Length; i++)
			{
				for (int j = 0; j < featureMutation.mutatedReels[i].Length; j++)
				{

					SlotReel reelToMutate = reelGame.engine.getSlotReelAt(featureMutation.mutatedReels[i][j]);
					string expandedSymbolName = SlotSymbol.constructNameFromDimensions(featureMutation.mutatedSymbols[i], 1, reelToMutate.visibleSymbols.Length); //get the name of our tall feature symbol
					reelToMutate.visibleSymbols[i].mutateTo(expandedSymbolName); //mutate to the tall version so it will be visible when the next spin happens
				}
			}	
		
		}
		else if (featureMutation.type == "multi_reel_advanced_replacement" && mutateToSmallSymbolsOnNextSpin)
		{
			// loop through all potential types of mutated reels, then loop through each reel individually
			for (int i = 0; i < featureMutation.mutatedReels.Length; i++)
			{
				for (int j = 0; j < featureMutation.mutatedReels[i].Length; j++)
				{
					// Mutate the symbols under the banners to blanks so there aren't any depth sorting problems.
					// For example, in cinderella01, the symbol heads are supposed to appear on top of the paylines,
					// but you don't want the heads to appear on top of the wild banners, so blank the symbols.
					// We'll mutate the blanks to wilds when you spin again.
				
					SlotReel reelToMutate = reelGame.engine.getSlotReelAt(featureMutation.mutatedReels[i][j]);
				
					for (int iVisibleSymbol = 0; iVisibleSymbol < reelToMutate.visibleSymbols.Length; iVisibleSymbol++)
					{
						SlotSymbol slotSymbol = reelToMutate.visibleSymbols[iVisibleSymbol];
						slotSymbol.mutateTo("BL");
					}
				}
			}
		}
	
		for (int i = 0; i < featureMutation.reels.Length; i++)
		{
			if (mutateToTallSymbolOnNextSpin)
			{
				SlotReel reelToMutate = reelGame.engine.getSlotReelAt(featureMutation.reels[i]);
				string expandedSymbolName = SlotSymbol.constructNameFromDimensions(featureMutation.symbol, 1, reelToMutate.visibleSymbols.Length); //get the name of our tall feature symbol
				reelToMutate.visibleSymbols[0].mutateTo(expandedSymbolName); //mutate to the tall version so it will be visible when the next spin happens
			
			}
			else
			{
				foreach (SlotSymbol slotSymbol in reelGame.engine.getVisibleSymbolsAt(featureMutation.reels[i]))
				{
					if (slotSymbol.name != featureMutation.symbol)
					{
						if (featureMutation.symbol != "")
						{
							slotSymbol.mutateTo(featureMutation.symbol);
						}
					}
				}
			}
		}
	}

	//Since we want to do the same thing on both module hooks so why write the code twice
	protected virtual IEnumerator handleBannerMutations()
	{
		if (!INTRO_SOUND_BG.IsNullOrWhiteSpace())
		{
			Audio.playSoundMapOrSoundKey(INTRO_SOUND_BG);
		}

		//Do the intro animations 
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimationInfo));
		Audio.playSoundMapOrSoundKeyWithDelay(VERTICAL_WILD_REVEAL_VO, WILD_REVEAL_VO_DELAY);
		if (featureMutation.type == "multi_reel_advanced_replacement")
		{
			for (int i = 0; i < featureMutation.mutatedReels.Length; i++)
			{
				for (int j = 0; j < featureMutation.mutatedReels[i].Length; j++)
				{
					int reelID = featureMutation.mutatedReels[i][j];
					
					isFinalBanner =
						(i == featureMutation.mutatedReels.Length - 1) &&
						(j == featureMutation.mutatedReels[i].Length - 1);
					
					//string symbol = featureMutation.mutatedSymbols[i];
					//TODO-HANS -- set this up so we can have specific banners keyed to specific symbols? 
					StartCoroutine(doBannerAnimationSequences(reelID));
					yield return new TIWaitForSeconds(DELAY_BETWEEN_BANNERS);
				}
			}
		}
		else
		{
			for (int i = 0; i < featureMutation.reels.Length; i++)
			{
				int reelID = featureMutation.reels[i];
				isFinalBanner = (i == featureMutation.reels.Length - 1);
 
				StartCoroutine(doBannerAnimationSequences(reelID));
				yield return new TIWaitForSeconds(DELAY_BETWEEN_BANNERS);
			}
		}

		if (DELAY_AFTER_BANNERS > 0.0f)
		{
			yield return new TIWaitForSeconds(DELAY_AFTER_BANNERS);
		}
	}

	//Making this virtual incase any future module need to override this for any game specific flow
	protected virtual IEnumerator doBannerAnimationSequences(int reelID)
	{
		//Do the pre place effect if we have one
		if (reelID < prePlaceEffectAnimationInfo.Count)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(prePlaceEffectAnimationInfo[reelID]));
		}
		else
		{
			Debug.LogWarning("You tried you play a pre place effect at reel index [" + reelID + "] but your prePlaceEffectAnimationInfo list count is only [" + prePlaceEffectAnimationInfo.Count + "]");
		}

		//Turn the banner on
		if (reelID < bannerObjects.Count)
		{
			if (isFinalBanner && !string.IsNullOrEmpty(FINAL_BANNER_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(FINAL_BANNER_SOUND_KEY));
			}
			else
			if (!string.IsNullOrEmpty(BANNER_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(BANNER_SOUND_KEY));
			}
			
			// if enabling / disabling banners directly, do so
			if (!shouldEnableDisableWithAnimation)
			{
				bannerObjects[reelID].SetActive(true);
			}

			Animator bannerObjectAnimator = bannerObjects[reelID].GetComponent<Animator>();
			// This forces the default animation to play from the beginning instead of playing a frame where it was left off on the last spin
			if (bannerObjectAnimator != null)
			{
				bannerObjectAnimator.Update(0.0f);
			}

			bannersActivated[reelID] = true;
		}
	}


	//Make the Banners animate with the paylines
	public override bool needsToExecuteOnPaylineDisplay()
	{
		return shouldAnimateWithPaylines;
	}

	public override IEnumerator executeOnPaylineDisplay(SlotOutcome outcome, PayTable.LineWin lineWin, Color paylineColor)
	{
		for (int i = reelGame.spotlightReelStartIndex; i < lineWin.symbolMatchCount + reelGame.spotlightReelStartIndex; i++)
		{
			//Do the wild banner animations themselves 
			if (i < bannerPaylineAnimationInfo.Count)
			{
				//only play animation on active banners
				bool isBannerActive = bannerObjects[i].activeInHierarchy;

				// if using animations to enable & disable, determine if the banner should count as disabled.
				if (shouldEnableDisableWithAnimation)
				{
					if (!bannersActivated[i])
					{
						isBannerActive = false;
					}
				}

				if (isBannerActive)
				{
					yield return StartCoroutine(AnimationListController.playAnimationInformation(bannerPaylineAnimationInfo[i]));
				}
			}
			else
			{
				Debug.LogWarning("You tried you play a wild banner at reel index [" + i + "] but your bannerAnimationInfo list count is only [" + bannerPaylineAnimationInfo.Count + "]");
			}
		}
		yield break;
	}

	//Reset the banners 	
	public override bool needsToExecuteOnPreSpin()
	{
		return true; 
	}

	public override IEnumerator executeOnPreSpin()
	{
		if (bannersActivated == null)
		{
			bannersActivated = new bool[bannerObjects.Count];
		}

		for (int i = 0; i < bannerObjects.Count; i++)
		{
			if (shouldEnableDisableWithAnimation)
			{
				// no yield here, don't want to introduce additional pre-spin delay for swipes (HIR-46205)
				StartCoroutine(AnimationListController.playAnimationInformation(bannerDisableAnimations[i]));
			}
			else
			{
				bannerObjects[i].SetActive(false);
			}

			bannersActivated[i] = false;
		}
		
		if (featureMutation != null)
		{
			if (featureMutation.type == "multi_reel_advanced_replacement" && mutateToSmallSymbolsOnNextSpin)
			{
				// Mutate the blanks to wilds.
				
				for (int i = 0; i < featureMutation.mutatedReels[0].Length; i++)
				{
					SlotReel reelToMutate = reelGame.engine.getSlotReelAt(featureMutation.mutatedReels[0][i]);
					
					for (int iVisibleSymbol = 0; iVisibleSymbol < reelToMutate.visibleSymbols.Length; iVisibleSymbol++)
					{
						SlotSymbol slotSymbol = reelToMutate.visibleSymbols[iVisibleSymbol];
						slotSymbol.mutateTo("WD");
					}
				}
			}
		}
		
		isFinalBanner = false;
		yield break;
	}
}
