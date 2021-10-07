using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Used when you want to have a TW feature which will generate fake 
// wild objects as well as real ones which will actually stick
// and change the symbols on the reels.
public class TriggerReplaceMultiWildFeature : SlotModule
{
	//Allows us to make a list of "real" vs "fake" wild replacements
	private class WildReplacementObject
	{
		public GameObject teaserObject;
		public Animator animator { private set; get; }
		public SlotSymbol slotSymbol;

		public WildReplacementObject(GameObject teaserEffect, SlotSymbol symbol)
		{
			teaserObject = teaserEffect;
			slotSymbol = symbol;
			animator = teaserObject.GetComponentInChildren<Animator>();
		}
	}

	[Header("Teaser")]
	[SerializeField] private GameObject teaserEffect = null;	
	[SerializeField] private Vector3 defaultTeaserPostion = Vector3.zero;
	[SerializeField] private string teaseAnimation = "fall";
	[SerializeField] private float teaseAnimationLength = 1.0f;
	[SerializeField] private int minNumberOfTeasers = 2;
	[SerializeField] private int maxNumberOfTeasers = 3;

	private GameObjectCacher teaserEffectCacher = null;
	private List<WildReplacementObject> currentTeasersPlaying = new List<WildReplacementObject>();

	[Header("Sounds")]
	[SerializeField] private string TW_FEATURE_BG_SOUND;
	[SerializeField] private bool shouldPlayAsMusic = false;
	[SerializeField] private string TW_FEATURE_START_VO;
	[SerializeField] private string TW_LANDING_SOUND;
	[SerializeField] private string PRE_WD_MUTATE_SOUND;
	[SerializeField] private string WD_MUTATE_SOUND;
	[SerializeField] private string POST_WD_MUTATE_SOUND;

	[Header("Delays")]
	[SerializeField] private float pauseBeforeWildMutates;	
	[SerializeField] private float pauseBetweenEachWildMutate;	
	[SerializeField] private float pauseAfterWildMutates;
	[SerializeField] private float pauseOnFeatureEnd;

	[Header("TW Symbol Versions")]
	[SerializeField] private string twActivateSymbolName = "";
	[SerializeField] private string twOutcomeSymbolName = "";

	private StandardMutation featureMutation;

	public override void Awake()
	{
		base.Awake();
		if (teaserEffect != null)
		{
			teaserEffectCacher = new GameObjectCacher(this.gameObject, teaserEffect);
		}
	}

	//Before the reels stop check for the trigger_replace_multi mutation
	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	//Check for the trigger_replace_multi mutation
	public override IEnumerator executePreReelsStopSpinning()
	{		
		featureMutation = null;
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mutation = baseMutation as StandardMutation;
			if (mutation.type == "trigger_replace_multi")
			{
				featureMutation = mutation;
			}
		}
				
		if (teaserEffectCacher != null && featureMutation != null)
		{
			int numTeasers = Random.Range(minNumberOfTeasers, maxNumberOfTeasers);
			for (int i = 0; i < numTeasers; i++)
			{
				//Get an instance of the teaser effect and position it
				GameObject teaser = teaserEffectCacher.getInstance();				
				GameObject reel = reelGame.getReelRootsAt(Random.Range(0, reelGame.getReelRootsLength()));
				teaser.transform.parent = reel.transform;
				teaser.transform.localPosition = defaultTeaserPostion;								
				currentTeasersPlaying.Add(new WildReplacementObject(teaser, null));
			}
		}

		yield return null;
	}

	//Only look for the TW symbol if we found the trigger_replace_multi mutation
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppingReel)
	{
		return (featureMutation != null);
	}
	
	//Check each reels visible symbols, and play the TW landing sound if we find the symbol on the reel
	//We want to play the sound on the specific reel stop, not after all the reels stop, the sound and anticipation animation line up 
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppingReel)
	{
		List<SlotSymbol> symbols = stoppingReel.visibleSymbolsBottomUp;

		for (int j = 0; j < symbols.Count; j++)
		{
			if (symbols[j].animator != null && symbols[j].animator.symbolInfoName == "TW")
			{
				//Play landing sound
				Audio.play(Audio.tryConvertSoundKeyToMappedValue(TW_LANDING_SOUND));
				if (shouldPlayAsMusic)
				{
					//Swap to custom BG music for the feature
					Audio.switchMusicKeyImmediate(Audio.tryConvertSoundKeyToMappedValue(TW_FEATURE_BG_SOUND));
				}
				else
				{					
					Audio.play(Audio.tryConvertSoundKeyToMappedValue(TW_FEATURE_BG_SOUND));
				}
			}
		}
		yield return null;
	}

	//Only handle the replace wilds if we found the trigger_replace_multi mutation
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return (featureMutation != null);
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Find the TW symbols
		List<SlotSymbol> allSymbols = reelGame.engine.getAllVisibleSymbols();
		
		List<SlotSymbol> twSymbols = new List<SlotSymbol>();
		for (int i = 0; i < allSymbols.Count; i++)
		{
			if (allSymbols[i].shortServerName == "TW" && !twSymbols.Contains(allSymbols[i]))
			{
				twSymbols.Add(allSymbols[i]);
			}
		}

		// Trigger the TW activate animation and then swap it out with the outcome animation
		if (!string.IsNullOrEmpty(twActivateSymbolName))
		{
			List<TICoroutine> symbolAnimationCoroutineList = new List<TICoroutine>();

			for (int i = 0; i < twSymbols.Count; i++)
			{
				twSymbols[i].mutateTo(twActivateSymbolName, null, playVfx: true, skipAnimation: true);
				symbolAnimationCoroutineList.Add(StartCoroutine(twSymbols[i].playAndWaitForAnimateOutcome()));
			}
			
			// wait for all of the animations to finish
			if (symbolAnimationCoroutineList.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(symbolAnimationCoroutineList));
			}

			// now swap out all of the symbols with a final outcome animation version, 
			// if one isn't defined we will swap back to just a 'TW' symbol
			for (int i = 0; i < twSymbols.Count; i++)
			{
				if (!string.IsNullOrEmpty(twOutcomeSymbolName))
				{
					twSymbols[i].mutateTo(twOutcomeSymbolName, null, playVfx: true, skipAnimation: true);
				}
				else
				{
					twSymbols[i].mutateTo("TW", null, playVfx: true, skipAnimation: true);
				}
			}
		}

		//Play the feature Start VO
		Audio.play(Audio.tryConvertSoundKeyToMappedValue(TW_FEATURE_START_VO), 1, 0, 0.5f);

		//Wait before starting all the WD1 Mutates
		yield return new WaitForSeconds(pauseBeforeWildMutates);

		//Play the pre mutate sound
		Audio.play(Audio.tryConvertSoundKeyToMappedValue(PRE_WD_MUTATE_SOUND));		

		//Go throught the mutations
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		for (int i = 0; i < featureMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < featureMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (featureMutation.triggerSymbolNames[i, j] != null && featureMutation.triggerSymbolNames[i, j] != "")
				{					
					if (teaserEffectCacher != null)
					{
						//create the WildReplacementObject for the actual symbols we want to mutate					
						GameObject teaser = teaserEffectCacher.getInstance();
						GameObject reel = reelArray[i].getReelGameObject();
						teaser.transform.parent = reel.transform;
						teaser.transform.localPosition = reelArray[i].visibleSymbolsBottomUp[j].transform.localPosition;
						currentTeasersPlaying.Add(new WildReplacementObject(teaser, reelArray[i].visibleSymbolsBottomUp[j]));
					}
					else
					{
						//just do the symbol mutate
						StartCoroutine(doWildMutate(reelArray[i].visibleSymbolsBottomUp[j]));
						// introduce a bit of a delay to stagger the wild mutations
						yield return new TIWaitForSeconds(pauseBetweenEachWildMutate);
					}
				}
			}
		}

		//Shuffle 
		CommonDataStructures.shuffleList(currentTeasersPlaying);
		//Do the drop animations
		foreach (WildReplacementObject replacementObject in currentTeasersPlaying)
		{
			StartCoroutine(doWildDrop(replacementObject));
			yield return new TIWaitForSeconds(pauseBetweenEachWildMutate);
		}

		//Wait for all of the mutations to finish
		yield return new TIWaitForSeconds(pauseAfterWildMutates);

		Audio.play(Audio.tryConvertSoundKeyToMappedValue(POST_WD_MUTATE_SOUND));

		//If we want to pause at the end of the feature
		yield return new TIWaitForSeconds(pauseOnFeatureEnd);

		if (shouldPlayAsMusic)
		{
			//Once we are finished swap back to the normal base music
			Audio.switchMusicKeyImmediate(Audio.tryConvertSoundKeyToMappedValue("reelspin_base"));
		}
		
		//Clear the the list of this mutations teasers
		currentTeasersPlaying.Clear();
	}

	//Handles dropping the wilds all wild replacements act like teasers and 
	//If the dropping wil has a symbol will will mutate at the end of its fall
	private IEnumerator doWildDrop(WildReplacementObject replacementObject)
	{
		//make sure the object is on
		replacementObject.teaserObject.SetActive(true);

		//play the fall animation
		replacementObject.animator.Play(teaseAnimation);		
		//wait for the fall animation
		yield return new TIWaitForSeconds(teaseAnimationLength);
		//if there is a slotSymbol assign this replacement object handle the reel symbol mutation
		if (replacementObject.slotSymbol != null)
		{
			//Mutate to our drop symbol 
			replacementObject.slotSymbol.mutateTo("TW-WD");
			replacementObject.slotSymbol.animateAnticipation();
			//play our mutation sound
			Audio.play(Audio.tryConvertSoundKeyToMappedValue(WD_MUTATE_SOUND));
			yield return new TIWaitForSeconds(replacementObject.slotSymbol.info.customAnimationDurationOverride);
			//Mutate the symbol to the WD1 which is our present wild with the proper payline outcome animation on it
			replacementObject.slotSymbol.mutateTo("WD1");
		}		
		//release the fall effect
		teaserEffectCacher.releaseInstance(replacementObject.teaserObject);
	}

	//No Teaser effect just mutate the wild symbol
	private IEnumerator doWildMutate(SlotSymbol symbol)
	{
		//Mutate to our drop symbol 
		symbol.mutateTo("TW-WD");
		symbol.animateAnticipation();
		//play our mutation sound
		Audio.play(Audio.tryConvertSoundKeyToMappedValue(WD_MUTATE_SOUND));
		yield return new TIWaitForSeconds(symbol.info.customAnimationDurationOverride);
		//Mutate the symbol to the WD1 which is our present wild with the proper payline outcome animation on it
		symbol.mutateTo("WD1");
	}
}
