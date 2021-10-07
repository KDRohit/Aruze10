using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//This class is used to make sure that we are always tracking the current reel height, and are able to ensure the reels play the proper height info on big win end 
//When the reel objects get turned off and back on their animators start at the entry state, therefor playing the 3 high animation.
//This is obviously not good since if you get a big win with the reels all at a height of 5, when the big win finishes the reels are all animating at a height of 3.
//Playing the appropriate hold animation on big win end will fix this.
[System.Serializable]
public class ReelHeightHoldInfo
{	
	public int height;
	public string holdAnimation;	
}

//This class holds the data needed to control each reel's specific expansion and preexpansion animations
[System.Serializable]
public class ExpandingReelData
{	
	//Expansion effect
	public Animator reelExpansionAnimator;
	public string reelStripKeyName = "";
	public List<ReelExpansionAnimation> reelExpansionAnimations = new List<ReelExpansionAnimation>();
}

//This class hold an animation name which is linked to a specific reel height value 
[System.Serializable]
public class ReelExpansionAnimation
{
	public int fromHeightValue;
	public int toHeightValue;
	//Pre expansion effect 
	public AnimationListController.AnimationInformationList preExpansionAnimationInfo;
	public AnimationListController.AnimationInformationList animationInfo;
}

public class SymbolRandomReelExpander : SlotModule
{
	[Header("Reel Defaults")]
	public int defaultReelHeight = 3;
	public int numberOfReels = 5;		
	public string defaultReelStripFormat = "bb01_reelstrip_bg_";
	[Header("Set this list to hold one anim per reel height.")]
	public List<ReelHeightHoldInfo> reelHeightHoldInfoList = new List<ReelHeightHoldInfo>();

	[Header("Reel Expansion Mutation")]
	//Mutation Keys
	//bb01 basegame -> contract_on_checkpoint
	//bb01 freespin -> symbol_random_reel_expander
	public string expansionMutationKey = "";
	public string FEATURE_SYMBOL = "";
	public string FEATURE_SOUND_KEY = ""; //if you want a sound to play on a symbol reveal that makes the reels expand
	//Until this is in the mutation we can't be sure it will match the anticipation reel ID will need to keep it here
	public int featureReel = 4;
	//This name sound funny but it is aptly named, set this to be the wait for after the pre expansion has played started 
	public float POST_PREEXPANSION_DELAY = 1.0f;
	public List<ExpandingReelData> expandingReelData = new List<ExpandingReelData>();
	public List<ExpandingReelData> shrinkingReelData = new List<ExpandingReelData>();
	[Header("Use these bool to control timing and sound options")]
	[Tooltip("If true reels will reset to default size before each spin.")]
	public bool shouldResetReelsPreSpin = false;

	public bool expandPreReelsStoppedSpinning = false;
	public bool expandOnReelsStoppedSpinning = false;

	//Adding this option due to the fact that the sound team didnt set the sounds up to play for multiple reels
	//So the playing the expansion sound once per expansion is "cluttered".
	//If your reels are expanding all at the same time you may want to check this box to only play one set of sounds
	public bool shouldOnlyPlayExpansionSoundOnce = false;

	//The names of the anticipations
	private List<string> anticipationNames = new List<string>();	

	//The current expansionMutation
	private StandardMutation expansionMutation;

	//Keeps track of the current heights of the reels
	private Dictionary<int, int> currentReelHeights = new Dictionary<int, int>();

	//A check to see if we want to continue looping the feature symbol;
	private bool stopFeatureSymbolLoop = false;

	//keeps track of defaultReel information in reelSetData in slot engine so we can put it back after we revert changes to reel height
	private Dictionary<int, ReelData> customReelSetData  = new Dictionary<int, ReelData>();

	public override void Awake()
	{
		base.Awake();

		anticipationNames.Add("1x3");
		anticipationNames.Add("1x4");
		anticipationNames.Add("1x5");

		for (int i = 0; i < numberOfReels; ++i)
		{
			currentReelHeights.Add(i, defaultReelHeight);			
		}

	}

	public override bool shouldUseCustomReelSetData()
	{
		return true;
	}

	public override Dictionary<int, ReelData> getCustomReelSetData()
	{
		return customReelSetData;
	}

	public override bool needsToExecuteOnBigWinEnd()
	{
		return true;
	}

	//make sure the reenabled reels are playing the appropriate hold animation
	public override void executeOnBigWinEnd()
	{
		//play the hold animation for the current reel heights	
		for (int i = 0; i < currentReelHeights.Count; i++)
		{
			foreach (ReelHeightHoldInfo reelHeightHoldinfo in reelHeightHoldInfoList)
			{
				if (reelHeightHoldinfo.height == currentReelHeights[i] && !string.IsNullOrEmpty(reelHeightHoldinfo.holdAnimation))
				{
					StartCoroutine(CommonAnimation.playAnimAndWait(expandingReelData[i].reelExpansionAnimator, reelHeightHoldinfo.holdAnimation));
				}
			}
		}
	}
	
	//Do we want to reset the reel size before we spin again
	public override bool needsToExecuteOnPreSpin()
	{
		stopFeatureSymbolLoop = true;
		return shouldResetReelsPreSpin;
	}

	//Reset the reels size
	public override IEnumerator executeOnPreSpin()
	{
		//Play the default reel animation to reset them to the smallest size
		int index = 1;
		foreach (ExpandingReelData reelToShrink in shrinkingReelData)
		{
			//Play shrinking animation
			foreach(ReelExpansionAnimation reelExpansionAnimation in reelToShrink.reelExpansionAnimations)
			{				
				if (reelExpansionAnimation.toHeightValue == currentReelHeights[index - 1])
				{
					yield return StartCoroutine(AnimationListController.playAnimationInformation(reelExpansionAnimation.animationInfo.animInfoList[0]));
				}
			}

			//reset the reel data
			SlotReel reel = reelGame.engine.getSlotReelAt(index - 1);
			reel.setReelDataWithoutRefresh(new ReelData(defaultReelStripFormat + index, defaultReelHeight), index);
			reel.refreshReelWithReelData();
			index++;
		}

		//Reset the current heights dict 
		for (int i = 0; i < numberOfReels; ++i)
		{
			currentReelHeights[i] = defaultReelHeight;
		}
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		expansionMutation = null; 

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null && reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				if (baseMutation.type == expansionMutationKey)
				{
					expansionMutation = baseMutation as StandardMutation;
				}
			}
		}
		//Only do this if we have the right mutations and want the reels to expand while the reels are spinning
		return expandPreReelsStoppedSpinning && (expansionMutation != null);
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		yield return StartCoroutine(doReelExpansions());
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		//Only do this if we have the right mutations and want the reels to expand after the reels have stopped
		return expandOnReelsStoppedSpinning && expansionMutation != null;
	}	

	public override IEnumerator executeOnReelsStoppedCallback()	
	{
		SlotSymbol featureSymbol = getFeatureSymbol();
		if (featureSymbol != null)
		{
			Audio.play(Audio.tryConvertSoundKeyToMappedValue(FEATURE_SOUND_KEY));
			StartCoroutine(loopFeatureSymbolAnim(featureSymbol));
		}
		yield return StartCoroutine(doReelExpansions());		
	}

	//This function handles expanding the reels from the mutation data
	protected virtual IEnumerator doReelExpansions()
	{
		//Used to make sure the expansion sounds only get played once
		bool isFirstExpansion = true;

		//Do Reel Expansions 
		if (expansionMutation != null)
		{
			foreach (IndependentReelExpansionMutation.IndependentReelExpansion expansionInfo in expansionMutation.symbolRandomReelExpanderData)
			{
				ExpandingReelData reelToExpand = expandingReelData[expansionInfo.reelID];
				foreach (ReelExpansionAnimation reelExpansionAnimation in reelToExpand.reelExpansionAnimations)
				{
					//If we have the corresponding animation and we arent already at that height
					if (reelExpansionAnimation.toHeightValue == expansionInfo.expandHeight && reelExpansionAnimation.fromHeightValue == currentReelHeights[expansionInfo.reelID])
					{
						if (shouldOnlyPlayExpansionSoundOnce)
						{
							yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(reelExpansionAnimation.preExpansionAnimationInfo, null, isFirstExpansion));
							isFirstExpansion = false;
						}
						else
						{
							yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(reelExpansionAnimation.preExpansionAnimationInfo));
						}

						//Update the reestrips with the correct number of visible symbols based on the mutation								
						SlotReel reel = reelGame.engine.getSlotReelAt(expansionInfo.reelID);
						reel.setReelDataWithoutRefresh(new ReelData(reelToExpand.reelStripKeyName, reelExpansionAnimation.toHeightValue), expansionInfo.reelID + 1);

						if(reelExpansionAnimation.toHeightValue != defaultReelHeight)
						{
							if(customReelSetData.ContainsKey(expansionInfo.reelID + 1)){
					            customReelSetData[expansionInfo.reelID + 1] = new ReelData(reelToExpand.reelStripKeyName, reelExpansionAnimation.toHeightValue);
					        }
					        else
					        {
					            customReelSetData.Add(expansionInfo.reelID + 1,new ReelData(reelToExpand.reelStripKeyName, reelExpansionAnimation.toHeightValue));
						    }
						}
						else
						{
							customReelSetData.Remove(expansionInfo.reelID + 1);
						}
						reel.refreshReelWithReelData();
					}
				}
			}

			yield return new TIWaitForSeconds(POST_PREEXPANSION_DELAY);
			
			//Used to make sure the expansion sounds only get played once
			isFirstExpansion = true;

			foreach (IndependentReelExpansionMutation.IndependentReelExpansion expansionInfo in expansionMutation.symbolRandomReelExpanderData)
			{
				ExpandingReelData reelToExpand = expandingReelData[expansionInfo.reelID];

				foreach (ReelExpansionAnimation reelExpansionAnimation in reelToExpand.reelExpansionAnimations)
				{
					//If we have the corresponding animation and we arent already at that height
					if (reelExpansionAnimation.toHeightValue == expansionInfo.expandHeight && reelExpansionAnimation.fromHeightValue == currentReelHeights[expansionInfo.reelID])
					{
						if (shouldOnlyPlayExpansionSoundOnce)
						{
							//Expand animation
							yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(reelExpansionAnimation.animationInfo, null, isFirstExpansion));
							isFirstExpansion = false;
						}
						else
						{
							//Expand animation
							yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(reelExpansionAnimation.animationInfo));
						}
						currentReelHeights[expansionInfo.reelID] = expansionInfo.expandHeight;
					}
				}
			}
		}
	}

	public override bool needsToGetFeatureAnicipationNameFromModule()
	{
		return (reelGame.outcome.getAnticipationTriggers() != null);
	}

	public override string getFeatureAnticipationNameFromModule()
	{
		string reelAnticipationName = "";
		foreach (Dictionary<string, int> triggerInfo in reelGame.outcome.getAnticipationTriggers().Values)
		{

			int reelToAnimate;
			if (triggerInfo.TryGetValue("reel", out reelToAnimate))
			{
				switch (currentReelHeights[reelToAnimate-1])
				{
					case 3:
						reelAnticipationName = "1x3";
						break;
					case 4:
						reelAnticipationName = "1x4";
						break;
					case 5:
						reelAnticipationName = "1x5";
						break;
				}
				return reelAnticipationName;
			}
		}
				
		//Default Anim 
		return "1x3";		
	}

	private SlotSymbol getFeatureSymbol()
	{
		SlotSymbol featureSymbol = null;
		reelGame.engine.getSlotReelAt(featureReel).refreshVisibleSymbols();
		SlotSymbol[] visibleSymbols = reelGame.engine.getSlotReelAt(featureReel).visibleSymbols;
		foreach (SlotSymbol symbol in visibleSymbols)
		{
			//If our Feature symbol is two high we want to make sure we only grab the first symbol			
			if (symbol.name.Contains(FEATURE_SYMBOL))
			{
				featureSymbol = symbol;
				break;
			}			
		}		
		return featureSymbol;
	}

	private IEnumerator loopFeatureSymbolAnim(SlotSymbol featureSymbol)
	{
		featureSymbol.animateAnticipation();
		while (featureSymbol.isAnimatorDoingSomething)
		{
			yield return null;
		}
		//This gets set to true in prespin ensure the symbol keeps animating like design asked
		if(!stopFeatureSymbolLoop)
		{
			StartCoroutine(loopFeatureSymbolAnim(featureSymbol));
		}		
	}
}
