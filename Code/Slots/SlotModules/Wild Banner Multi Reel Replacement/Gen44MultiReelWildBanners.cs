using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

//Helper class to tidy up the module inspector and give us an easy way to access mulitple animations for each feature banner
[Serializable]
public class Gen44WildBanner
{
	[HideInInspector] public bool isActive;
	//These house the Animation Info for each state of the banners, this includes the banner and the frame animations  
	//Using these classes allow us to link a sound(s) to an animation
	public AnimationListController.AnimationInformationList teaseAnimations;
	public AnimationListController.AnimationInformationList idleAnimations;
	public AnimationListController.AnimationInformationList expandAnimations;
	public AnimationListController.AnimationInformationList paylineAnimations;
}

//This code is specifically for Gen44 which needs simplfied version of the MultiReelWildBannerReplacementModule
//We want to handle the idles of the banners differently and the placement and reveal effects dont work the same as the other games
public class Gen44MultiReelWildBanners : SlotModule
{
	//Intervals for the tease animations
	[SerializeField] protected float minInterval = 0.5f;
	[SerializeField] protected float maxInterval = 1.5f;

	//Since we don't want the VO to play with ever banner we need to play it outside of the banner expand AnimationInfo
	[SerializeField] protected string bannerVO = "freespin_vertical_wild_reveal_vo";
	[SerializeField] protected float bannerVODelay = 3f;

	//Public class used to house all the banner animation data
	[SerializeField] protected List<Gen44WildBanner> wildBanners = new List<Gen44WildBanner>();

	//Tall symbol override to swap to under the banners
	[SerializeField] protected string tallBannerSymbolName = ""; // allows for a tall symbol to be placed under the banner to make the pay box be the full size of the banner
	[SerializeField] protected bool isSwappingBackTo1x1WDSymbolsBeforeNextSpin = true;

	//Used to map the reel IDs the banner are placed on for the payline animations
	private Dictionary<int, Gen44WildBanner> wildBannerInfoByReelID = new Dictionary<int, Gen44WildBanner>();

	//This will keep track of the current mutation
	private StandardMutation featureMutation;

	//This will try to play a tease animation everyonce and a while on an inactive reel
	protected CoroutineRepeater teaseAnimationController;
	private TICoroutine teaseAnimationCoroutine;

	//Used to only play VO on the first banner
	private bool isFirstBanner = true;

	public override void Awake()
	{	
		//Gen44 is only going to have banners on Reels 1, 2, and/or 3.
		//More straight forward to be able to grab these by reel id for the payline animations and turning them on base off the mutation reels
		wildBannerInfoByReelID.Add(1, wildBanners[0]);
		wildBannerInfoByReelID.Add(2, wildBanners[1]);
		wildBannerInfoByReelID.Add(3, wildBanners[2]);

		teaseAnimationController = new CoroutineRepeater(minInterval, maxInterval, doTeaseAnimation);

		base.Awake();
	}

	private void Update()
	{
		teaseAnimationController.update();
	}

	//Reset the banners 	
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	//Set banners and reel effects back to idle
	public override IEnumerator executeOnPreSpin()
	{
		if (featureMutation != null)
		{
			for (int i = 0; i < featureMutation.reels.Length; i++)
			{
				int reelID = featureMutation.reels[i];

				// if we need to swap back to 1x1 WD's before the next spin do it now, mostly useful if a fake mega WD symbolw as being used
				if (isSwappingBackTo1x1WDSymbolsBeforeNextSpin)
				{
					foreach (SlotSymbol slotSymbol in reelGame.engine.getVisibleSymbolsAt(reelID))
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

				if (wildBannerInfoByReelID.ContainsKey(reelID) && wildBannerInfoByReelID[reelID] != null)
				{					
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(wildBannerInfoByReelID[reelID].idleAnimations));
					wildBannerInfoByReelID[reelID].isActive = false;
				}
			}
			isFirstBanner = true;
		}
	}

	//If we find our mutation do banners
	public override bool needsToExecutePreReelsStopSpinning()
	{
		featureMutation = null;
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mutation = baseMutation as StandardMutation;
			if (mutation.type == "multi_reel_replacement")
			{
				featureMutation = mutation;
				Array.Sort(featureMutation.reels);
			}
		}
		return (featureMutation != null);
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		if (featureMutation != null)
		{
			//Shuffle this so it appears random
			CommonDataStructures.shuffleList(featureMutation.reels);

			//Expand the banners
			for (int i = 0; i < featureMutation.reels.Length; i++)
			{
				int reelID = featureMutation.reels[i];
				if (wildBannerInfoByReelID.ContainsKey(reelID) && wildBannerInfoByReelID[reelID] != null)
				{
					if(isFirstBanner)
					{
						Audio.play(Audio.tryConvertSoundKeyToMappedValue(bannerVO), 1, 0, bannerVODelay);
						isFirstBanner = false;
					}
					wildBannerInfoByReelID[reelID].isActive = true;
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(wildBannerInfoByReelID[reelID].expandAnimations));
				}
			}
		}
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return (featureMutation != null);
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Mutate the symbols under the banner to WD's
		for (int i = 0; i < featureMutation.reels.Length; i++)
		{
			int reelID = featureMutation.reels[i];

			// Mutate the symbols under the banner to WD's
			if (!string.IsNullOrEmpty(tallBannerSymbolName))
			{
				// going to use the set tall symbol which will cover the whole reel
				reelGame.engine.getVisibleSymbolsAt(reelID)[0].mutateTo(tallBannerSymbolName);
			}
			else
			{
				// doing 1x1 WD symbols
				foreach (SlotSymbol slotSymbol in reelGame.engine.getVisibleSymbolsAt(reelID))
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

		yield break;
	}

	//Make the Banners animate with the paylines
	public override bool needsToExecuteOnPaylineDisplay()
	{
		return true;
	}

	public override IEnumerator executeOnPaylineDisplay(SlotOutcome outcome, PayTable.LineWin lineWin, Color paylineColor)
	{
		for (int i = 0; i < lineWin.symbolMatchCount; i++)
		{
			//Do the wild banner animations themselves 
			if (wildBannerInfoByReelID.ContainsKey(i) && wildBannerInfoByReelID[i] != null && wildBannerInfoByReelID[i].isActive)
			{				
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(wildBannerInfoByReelID[i].paylineAnimations));
			}			
		}
		yield break;
	}	

	private IEnumerator doTeaseAnimation()
	{
		//Only play is if the reels are spinning
		if (reelGame != null && reelGame.engine != null && !reelGame.engine.isStopped)
		{
			//Tease on of the reels
			for (int i = 0; i < wildBannerInfoByReelID.Keys.Count; i++)
			{
				if (wildBannerInfoByReelID.ContainsKey(i) && wildBannerInfoByReelID[i] != null && !wildBannerInfoByReelID[i].isActive)
				{
					teaseAnimationCoroutine = StartCoroutine(AnimationListController.playListOfAnimationInformation(wildBannerInfoByReelID[i].teaseAnimations));
					yield return teaseAnimationCoroutine;
					yield break;
				}
			}
		}	
	}

	protected override void OnDestroy()
	{
		if(teaseAnimationCoroutine != null)
		{
			StopCoroutine(teaseAnimationCoroutine);
		}

		base.OnDestroy();
	}
}
