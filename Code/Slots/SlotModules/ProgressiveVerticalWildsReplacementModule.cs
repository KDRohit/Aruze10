using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Gets the wild triggers to fill up a meter for each reel.
 * When the meter is full, the whole reel is replaced by a wild symbol overlay
 * Used in wonka01
 */
public class ProgressiveVerticalWildsReplacementModule : SlotModule 
{
	[SerializeField] private Inspector2DGameObjectArray reelTriggerEndPositions;	//Stores the game objects below each reel that show how many triggers have been found in each reel
	[SerializeField] private GameObject sparkleTrail;								//Effect that goes from the trigger found in the reel to the appropriate trigger end position below the reel
	[SerializeField] private Inspector2DGameObjectArray wildExpands;
	[SerializeField] private string[] wildExpandsSoundKeys;
	[SerializeField] private string TRAIL_ANIM_NAME;
	[SerializeField] private string TRIGGER_ANIM_NAME;
	[SerializeField] private string TRIGGER_CELEBRATE_ANIM_NAME;
	[SerializeField] private string TRIGGER_CELEBRATE_AUDIO = "trigger_symbol_effect_final";
	[SerializeField] private float TRIGGER_CELEBRATE_AUDIO_DELAY = 0.0f;

	[SerializeField] private string WILD_OVERLAY_ANIM_NAME;
	[SerializeField] private string TRIGGER_SYMBOL_ID = "TR";   // symbol must contain this for trigger_symbol audio to be played
	[SerializeField] private string MUTATED_SYMBOL_ID = "WD";	//Symbol that we want to mutate to underneath the overlay. These should not have animators to prevent animating symbols under the overlay. 

	[SerializeField] private List<AnimationListController.AnimationInformationList> prePlaceEffectAnimationInfo;

	[SerializeField] private float TRAIL_MOVE_TIME = 0.5f;
	[SerializeField] private float TW_ANIM_DURATION = 1.0f;
	[SerializeField] private float TRIGGER_CELEBRATE_ANIM_DURATION = 1.0f;
	[SerializeField] private float FS_COUNTER_ANIM_DURATION = 0.7f;
	[SerializeField] private float REVEAL_VERTICAL_WILD_VO_DELAY = 0.0f;
	[SerializeField] private bool mutateSymbolsUnderExpandedReel = false;
	[SerializeField] private bool onlyPlayAnticipationSoundOnce = false;

	protected const string TRIGGER_SYMBOL_SOUND_KEY = "trigger_symbol";
	protected const string TRIGGER_SYMBOL_REEL_EFFECT_SOUND_KEY = "trigger_symbol_effect";
	[SerializeField] private string TRIGGER_SYMBOL_VO_SOUND_KEY = "trigger_symbol_vo";
	[SerializeField] private float TRIGGER_SYMBOL_VO_DELAY = 0.0f;
	[SerializeField] private string TRIGGER_SYMBOL_REEL_EFFECT_FINAL_SOUND_KEY = "trigger_symbol_effect_final";
	[SerializeField] private string REVEAL_VERTICAL_WILD_VO_KEY = "freespin_vertical_wild_reveal_vo";
	[SerializeField] private bool isPlayingOneTriggerEffectSoundPerSpin = false;

	protected StandardMutation featureMutation = null;
	private int[] reelTriggerCount;													//Keeps track of the number of triggers found in each reel
	private bool[] isTransforming;
	private bool[] reelHasTransformed;
	private int wildOverlayCount;
	private bool needsToPlayAnticipationSound = true;
	private bool hasPlayedTriggerSoundThisSpin = false;

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		reelTriggerCount = new int[reelArray.Length];
		isTransforming  = new bool[reelArray.Length];
		reelHasTransformed  = new bool[reelArray.Length];
	}

	public override bool needsToExecuteOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		return stoppedReel > 0 && needsToPlayAnticipationSound;
	}
	
	public override IEnumerator executeOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{

		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(stoppedReel))
		{
			if (symbol != null && symbol.serverName.Contains(TRIGGER_SYMBOL_ID))
			{
				Audio.play(Audio.soundMap(TRIGGER_SYMBOL_SOUND_KEY));

				if (Audio.canSoundBeMapped(TRIGGER_SYMBOL_VO_SOUND_KEY))
				{
					Audio.playWithDelay(Audio.soundMap(TRIGGER_SYMBOL_VO_SOUND_KEY), TRIGGER_SYMBOL_VO_DELAY);
				}

				if (onlyPlayAnticipationSoundOnce)
				{
					needsToPlayAnticipationSound = false;
				}
				break;
			}
		}

		yield return null;
	}		

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}
	
	public override IEnumerator executeOnPreSpin()
	{
		featureMutation = null;
		needsToPlayAnticipationSound = true;
		hasPlayedTriggerSoundThisSpin = false;
		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		foreach (MutationBase mutation in reelGame.mutationManager.mutations)
		{
			if (mutation.type == "progressive_vertical_wilds")
			{
				featureMutation = mutation as StandardMutation;
				return true;
			}
		}
		
		return featureMutation != null;
	}

	// For the case where we only want to play a single trigger sound per spin
	// determine which sound to play, perfering the final sound if one of the 
	// meters will finish this spin
	private string getSoundKeyForSingleTriggerSoundPerSpin()
	{
		string singleTriggerSoundKey = "";
		if (isPlayingOneTriggerEffectSoundPerSpin)
		{
			for (int i = 0; i < featureMutation.featureReels.Length; i++)
			{
				foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(featureMutation.featureReels[i]))
				{	
					if (symbol.serverName == featureMutation.mutatedSymbols[i])
					{
						int reelID = featureMutation.featureReels[i];
						if (reelTriggerCount[reelID] == 2)
						{
							// one is about to complete so we should override and just play the final sound
							return TRIGGER_SYMBOL_REEL_EFFECT_FINAL_SOUND_KEY;
						}
						else
						{
							singleTriggerSoundKey = TRIGGER_SYMBOL_REEL_EFFECT_SOUND_KEY;
						}
					}
				}
			}
		}

		return singleTriggerSoundKey;
	}

	// Determine which sound key to use when revealing the trigger on this reel
	private string getTriggerSoundKeyForReel(int reelID, string singleTriggerSoundKey)
	{
		string triggerSoundKey = "";
		if (isPlayingOneTriggerEffectSoundPerSpin)
		{
			if (hasPlayedTriggerSoundThisSpin)
			{
				// already played the trigger sound so just play nothing
				triggerSoundKey = "";
			}
			else
			{
				triggerSoundKey = singleTriggerSoundKey;
			}
		}
		else
		{
			if (reelTriggerCount[reelID] == 2)
			{
				// one is about to complete so we should override and just play the final sound
				return TRIGGER_SYMBOL_REEL_EFFECT_FINAL_SOUND_KEY;
			}
			else
			{
				singleTriggerSoundKey = TRIGGER_SYMBOL_REEL_EFFECT_SOUND_KEY;
			}
		}

		return triggerSoundKey;
	}
	
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (featureMutation == null)
		{
			Debug.LogError("Trying to execute module on invalid data.");
			yield break;
		}

		if (featureMutation.featureReels != null)
		{
			// if we are only playing one trigger sound we want to play
			// the most important one, which will be the finish one
			// so go through and figure out which one we will play with the effects
			// @note: (Scott) Putting this here since it requires looping through
			// the mutation info, so that we don't have to loop through it every time
			// we call getTriggerSoundKeyForReel()
			string singleTriggerSoundKey = getSoundKeyForSingleTriggerSoundPerSpin();

			for (int i = 0; i < featureMutation.featureReels.Length; i++)
			{
				foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(featureMutation.featureReels[i]))
				{	
					if (symbol.serverName == featureMutation.mutatedSymbols[i])
					{
						//do animations from symbol position to the appropriate target position
						if (sparkleTrail != null)
						{
							int reelID = featureMutation.featureReels[i];
							StartCoroutine(playAcquiredEffects(reelID, symbol, getTriggerSoundKeyForReel(reelID, singleTriggerSoundKey)));
							hasPlayedTriggerSoundThisSpin = true;
						}
					}
				}
			}
			yield return new TIWaitForSeconds(TW_ANIM_DURATION + TRAIL_MOVE_TIME + FS_COUNTER_ANIM_DURATION);
		}
		if(featureMutation.reelsToMutate != null && featureMutation.reelsToMutate.Length > 0)
		{	
			if (featureMutation.mutateToSymbol == "")
			{
				Debug.LogError("Cannot convert reel to a wild overlay. Check if the mutation data for type: \"progressive_vertical_wilds\" contains the key \"to_symbol\"");

			}
			else
			{
				for (int i = 0; i < featureMutation.reelsToMutate.Length; i++)
				{

					if(wildExpands[featureMutation.reelsToMutate[i]][0].activeSelf == false && wildExpands[featureMutation.reelsToMutate[i]][1].activeSelf == false)
					{
						isTransforming[featureMutation.reelsToMutate[i]] = true;
						StartCoroutine(expandWildOn(featureMutation.reelsToMutate[i]));
					}
					continue;
				}

				bool shouldwait = true;
				while (shouldwait)
				{
					shouldwait = false;
					foreach (bool reelTransforming in isTransforming)
					{
						if (reelTransforming)
						{
							shouldwait = true;
						}
					}
					yield return null;
				}
			}
		}

		if (mutateSymbolsUnderExpandedReel)
		{
			for (int i = 0; i < reelHasTransformed.Length; i++)
			{
				if (reelHasTransformed[i])
				{
					SlotReel reelToMutate = reelGame.engine.getSlotReelAt(i);
					string expandedSymbolName = SlotSymbol.constructNameFromDimensions(MUTATED_SYMBOL_ID, 1, reelToMutate.visibleSymbols.Length);
					if (reelGame.findSymbolInfo(expandedSymbolName) != null)
					{
						reelToMutate.visibleSymbols[0].mutateTo(expandedSymbolName);
					}
					else
					{
						Debug.LogError("ProgressiveVerticalWildsReplacementModule has no " + expandedSymbolName + " sybols in its symbol templates.");
					}
				}
			}
		}
	}

	private IEnumerator playAcquiredEffects(int reelID, SlotSymbol symbol, string triggerSoundKey)
	{
		// put symbol on the overlay layer so it plays above the reel dividers
		symbol.animateOutcome(twSymbolAnimationDone);
		CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_REELS_OVERLAY);

		GameObject currentReelTrigger = reelTriggerEndPositions[reelID][reelTriggerCount[reelID]];

		if (!string.IsNullOrEmpty(triggerSoundKey))
		{
			Audio.play(Audio.soundMap(triggerSoundKey));
		}

		currentReelTrigger.SetActive(true);
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(currentReelTrigger.GetComponent<Animator>(), TRIGGER_ANIM_NAME));
		reelTriggerCount[reelID]++;
	}

	/// Put the symbol back on the reels layer before continuing to spin
	private void twSymbolAnimationDone(SlotSymbol sender)
	{
		if (sender.gameObject != null)
		{
			CommonGameObject.setLayerRecursively(sender.gameObject, Layers.ID_SLOT_REELS);
		}
	}
	
	private IEnumerator expandWildOn(int reelID)
	{
		if(TRIGGER_CELEBRATE_ANIM_NAME != "")
		{
			foreach (GameObject trigger in reelTriggerEndPositions[reelID])
			{
				trigger.GetComponent<Animator>().Play(TRIGGER_CELEBRATE_ANIM_NAME);
			}

			Audio.playSoundMapOrSoundKeyWithDelay(TRIGGER_CELEBRATE_AUDIO, TRIGGER_CELEBRATE_AUDIO_DELAY);
			yield return new TIWaitForSeconds(TRIGGER_CELEBRATE_ANIM_DURATION);
		}

		if (reelID < prePlaceEffectAnimationInfo.Count && prePlaceEffectAnimationInfo[reelID].animInfoList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(prePlaceEffectAnimationInfo[reelID]));
		}

		if(wildExpandsSoundKeys != null && wildExpandsSoundKeys.Length > wildOverlayCount%2 && wildExpandsSoundKeys[wildOverlayCount%2] != null && !Audio.isPlaying(Audio.soundMap(wildExpandsSoundKeys[wildOverlayCount%2])))
		{
			Audio.play(Audio.soundMap(wildExpandsSoundKeys[wildOverlayCount%2]));
		}
		Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_VERTICAL_WILD_VO_KEY, REVEAL_VERTICAL_WILD_VO_DELAY);
		wildExpands[reelID][wildOverlayCount%2].SetActive(true);
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(wildExpands[reelID][wildOverlayCount%2].GetComponent<Animator>(), WILD_OVERLAY_ANIM_NAME));
		isTransforming[reelID] = false;
		reelHasTransformed[reelID] = true;
		wildOverlayCount++;

	}
}