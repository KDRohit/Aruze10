using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Module used to implement games with wild overlays that have a big animationt hat happens in the middle of the screen
(gen10 Howling Wilds and ani04 African Thunder are examples)

Original Author: Scott Lepthien
*/
public class WildOverlayTransformModule : SlotModule 
{
	// Generic Inspector Stuff
	[SerializeField] private Animator wildEffectAnimator = null;				// The animation component for the revealing of the wild overlay symbols
	[SerializeField] private GameObject shroud = null;							// Shroud that darkens the reels
	[SerializeField] private GameObject[] wildEffectSymbols = null; 			// List of moon symbols that need to be turned on, doesn't include the M1 symbol which is 2 high
	[SerializeField] private bool hasSpecialCaseForM1 = false;					// Controls if M1 symbols have special handling, like a different animation to play
	[SerializeField] private List<string> symbolOrderList;						// Ordering the index's of which should match up to wildEffectSymbols 
	[SerializeField] private string FEATURE_HIDDEN_ANIM_NAME = "";
	[SerializeField] private string FEATURE_EFFECT_START_ANIM_NAME = "";
	[SerializeField] private string FEATURE_EFFECT_M1_START_ANIM_NAME = "";
	[SerializeField] private string FEATURE_EFFECT_IDLE_ANIM_NAME = "";
	[SerializeField] private bool showFeatureEffectIdleAnim = true;				// Some games may have a different symbol effect loop that doesn't match up with this idle so it wouldn't want it played
	[SerializeField] private string FEATURE_EFFECT_M1_IDLE_ANIM_NAME = "";
	[SerializeField] private string REEL_STOP_SOUND_IF_HAS_WILD = "";
	[SerializeField] private string EFFECT_START_FANFARE_SOUND = "";
	[SerializeField] private string EFFECT_START_SFX_SOUND = "";
	[SerializeField] private string EFFECT_START_VO_SOUND = "";
	[SerializeField] private string WILD_OVERLAY_APPLIED_VO = "";				// VO played when the wild overlay is applied
	[SerializeField] private bool isWildOverlayAppliedVoPlayedOnce = true;	// Controls if the WILD_OVERLAY_APPLIED_VO plays only once per feature trigger
	[SerializeField] private string[] symbolFeatureEffectStartAnimOverrides = null;	// Some games, like ani04, have a set of animations for specific symbols like minors, this should match up to wildEffectSymbols
	[SerializeField] private float[] symbolFeatureEffectStartAnimOverridesLengths = null;	// Some games, like ani04, have a set of animations for specific symbols like minors, this should match up to wildEffectSymbols
	[SerializeField] private string[] symbolFeatureEffectIdleAnimOverrides = null; // Idle animaiton overrides, serves teh same purpose as symbolFeatureEffectStartAnimOverrides

	[SerializeField] private float FEATURE_EFFECT_START_ANIM_LENGTH;
	[SerializeField] private float SYMBOL_SHOW_IDLE_TIME;
	[SerializeField] private float EFFECT_START_VO_SOUND_DELAY;
	[SerializeField] private string SYMBOL_REVEAL_SOUND_NAME;				// Sound that triggers when the symbol is revealed
	[SerializeField] private float SYMBOL_REVEAL_SOUND_DELAY;				// Set this to control how far into the reveal animation the symbol actually shows up

	// Free Spin Inspector Stuff
	[SerializeField] private Transform[] flyTopPoints = null;			// The points at the top where the newly changed wild symbol will land
	[SerializeField] private GameObject symbolIndicatorPrefab = null;	// Prefab to create symbol indicators that show which symbols are currently wild
	[SerializeField] private Transform symbolIndicatorParent = null;	// Prefab to locate parent transform of wild indicator prefabs.
	[SerializeField] private string WILD_INDICATOR_TRAVEL_SOUND = "";
	[SerializeField] private string WILD_INDICATOR_ARRIVE_SOUND = "";

	[SerializeField] private float TIME_TO_FLY_TO_SYMBOL_AT_TOP;			// How long it takes for the symbol that appears in the moon animation to fly to the top
	[SerializeField] private float SYMBOL_INDICATOR_M1_SCALE;				// What the local scale should be of the indicator when it shrinks down
	[SerializeField] private float SYMBOL_INDICATOR_MINOR_SCALE;			// What the local scale should be of the indicator when it shrinks down
	[SerializeField] private float SYMBOL_INDICATOR_OTHER_MAJOR_SCALE;		// What the local scale should be of the indicator when it shrinks down
	[SerializeField] private float INDICATOR_START_Z_POS;					// The z-position where the indicator will first be displayed, needs to be over the grayed out icons at the top
	[SerializeField] private string[] SYMBOL_INDICATOR_ARRIVE_VOS;			// VO clips for the symbol indicator arriving at the target position along the top, matches up with the other ordered lists
	[SerializeField] private float SYMBOL_INDICATOR_ARRIVE_VO_DELAY;		// Delay on the VO for indicator arriving

	[SerializeField] private AudioListController.AudioInformationList revealSequenceAudio;	// audio list to facilitate syncing a number of foley sounds into the feature sequence
	[SerializeField] private string RESUME_MUSIC_KEY = null;				// Music to resume after feature completes

	// Public State Variables
	[HideInInspector] public bool doWildReplacement = false;
	[HideInInspector] public List<string> activeWilds = new List<string>();
	[HideInInspector] public string mutationTarget = "";

	private bool isWildOverlayAppliedVoPlayed = false;

// executePreReelsStopSpinning() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels stop spinning (after the outcome has been set)
	public override bool needsToExecutePreReelsStopSpinning()
	{
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
		{
			if (reelGame.mutationManager.mutations.Count > 0)
			{
				// we need to do wild overlays
				return true;
			}
		}

		return false;
	}
	
	public override IEnumerator executePreReelsStopSpinning()
	{
		// Find the symbol that is being made wild
		JSON mut = reelGame.outcome.getMutations()[0];
		mutationTarget = mut.getString("replace_symbol", "");

		// Freespins can contain mutation information about sticky mutations, but not new ones, only handle the new ones here
		if (mutationTarget != "")
		{
			if (mutationTarget == "M1")
			{
				mutationTarget = "M1-2A";
			}

			yield return StartCoroutine(playWildOverlayReveal(mutationTarget));

			// if FreeSpinGame we also need to fly the indicator up
			if (reelGame.isFreeSpinGame())
			{
				yield return StartCoroutine(flySymbolToIndicator());
			}
		}
	}

// executeOnPreSpin() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		isWildOverlayAppliedVoPlayed = false;

		if (!reelGame.isFreeSpinGame())
		{
			if (mutationTarget != "")
			{
				hideAllWildOverlays();
			}
		}

		mutationTarget = "";
		doWildReplacement = false;
		yield break;
	}

	/// Go thourgh all the symbols and hide all the wild overlays
	private void hideAllWildOverlays()
	{
		SlotReel[]reelArray = reelGame.engine.getReelArray();

		foreach (SlotReel reel in reelArray)
		{
			foreach (SlotSymbol symbol in reel.symbolList)
			{
				symbol.hideWild();
			}
		}
	}

	// allows the animaiton to be turned off, but leave the shroud up, used by free spins so it can replace the symbol with a moveable version
	public void hideFeatureAnimation()
	{
		wildEffectAnimator.Play(FEATURE_HIDDEN_ANIM_NAME);
	}

	// turns the feature off, in function form so the free spin game can turn it off when needed
	public void hideFeature()
	{
		if (shroud != null)
		{
			shroud.SetActive(false);
		}
		hideFeatureAnimation();
	}

	/// Play the wild overlay animation to reveal the symbol that is changing to wilds
	public IEnumerator playWildOverlayReveal(string symbolName)
	{
		ReelGame reelGame = ReelGame.activeGame;

		if (shroud != null)
		{
			shroud.SetActive(true);
		}

		// play a choreographed reveal audio sequence
		if (revealSequenceAudio != null)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(revealSequenceAudio));
		}

		if (EFFECT_START_FANFARE_SOUND != "")
		{
			Audio.play(EFFECT_START_FANFARE_SOUND);
		}

		if (EFFECT_START_SFX_SOUND != "")
		{
			Audio.play(EFFECT_START_SFX_SOUND);
		}

		if (EFFECT_START_VO_SOUND != "")
		{
			Audio.play(EFFECT_START_VO_SOUND, 1.0f, 0.0f, EFFECT_START_VO_SOUND_DELAY);
		}

		turnOnSymbolVisual(symbolName);

		string featureStartOverrideAnim = getSymbolFeatureEffectStartAnimOverride(symbolName);
		string idleAnimOverride = getSymbolFeatureEffectIdleAnimOverride(symbolName);
		
		if (hasSpecialCaseForM1 && symbolName.Contains("M1"))
		{
			if (SYMBOL_REVEAL_SOUND_NAME != "")
			{
				Audio.play(SYMBOL_REVEAL_SOUND_NAME, 1.0f, 0.0f, SYMBOL_REVEAL_SOUND_DELAY);
			}

			// play the wild overlay reveal animation
			if (featureStartOverrideAnim != "")
			{
				wildEffectAnimator.Play(featureStartOverrideAnim);
				float animOverrideLength = getSymbolFeatureEffectStartAnimOverridesLength(symbolName);

				if (animOverrideLength != 0)
				{
					yield return new TIWaitForSeconds(animOverrideLength);
				}
				else
				{
					yield return new TIWaitForSeconds(FEATURE_EFFECT_START_ANIM_LENGTH);
				}
			}
			else
			{
				wildEffectAnimator.Play(FEATURE_EFFECT_M1_START_ANIM_NAME);
				yield return new TIWaitForSeconds(FEATURE_EFFECT_START_ANIM_LENGTH);
			}

			if (!reelGame.isFreeSpinGame())
			{
				doWildReplacement = true;
			}

			if (showFeatureEffectIdleAnim)
			{
				// now show the idle for a second or two so the player sees what it is (assuming an idle is needed)
				if (idleAnimOverride != "")
				{
					wildEffectAnimator.Play(idleAnimOverride);
					yield return new TIWaitForSeconds(SYMBOL_SHOW_IDLE_TIME);
				}
				else
				{
					if (FEATURE_EFFECT_M1_IDLE_ANIM_NAME != "")
					{
						wildEffectAnimator.Play(FEATURE_EFFECT_M1_IDLE_ANIM_NAME);
						yield return new TIWaitForSeconds(SYMBOL_SHOW_IDLE_TIME);
					}
				}
			}

			if (!reelGame.isFreeSpinGame())
			{
				hideFeature();
			}
		}
		else
		{
			if (SYMBOL_REVEAL_SOUND_NAME != "")
			{
				Audio.play(SYMBOL_REVEAL_SOUND_NAME, 1.0f, 0.0f, SYMBOL_REVEAL_SOUND_DELAY);
			}

			// play the wild overlay reveal animation
			if (featureStartOverrideAnim != "")
			{
				wildEffectAnimator.Play(featureStartOverrideAnim);
				float animOverrideLength = getSymbolFeatureEffectStartAnimOverridesLength(symbolName);

				if (animOverrideLength != 0)
				{
					yield return new TIWaitForSeconds(animOverrideLength);
				}
				else
				{
					yield return new TIWaitForSeconds(FEATURE_EFFECT_START_ANIM_LENGTH);
				}
			}
			else
			{
				wildEffectAnimator.Play(FEATURE_EFFECT_START_ANIM_NAME);
				yield return new TIWaitForSeconds(FEATURE_EFFECT_START_ANIM_LENGTH);
			}

			if (!reelGame.isFreeSpinGame())
			{
				doWildReplacement = true;
				// Go through each reel and see if it's going to land with any of the symbols.
				foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
				{
					foreach (string name in reel.getFinalReelStopsSymbolNames())
					{
						if (name == symbolName)
						{
							if (REEL_STOP_SOUND_IF_HAS_WILD != "")
							{
								reel.reelStopSoundOverride = REEL_STOP_SOUND_IF_HAS_WILD;
							}

							if ((!isWildOverlayAppliedVoPlayedOnce || !isWildOverlayAppliedVoPlayed) && WILD_OVERLAY_APPLIED_VO != "")
							{
								reel.reelStopVOSound = WILD_OVERLAY_APPLIED_VO;
								isWildOverlayAppliedVoPlayed = true;
							}
						}
					}
				}
			}

			if (showFeatureEffectIdleAnim)
			{
				if (idleAnimOverride != "")
				{
					wildEffectAnimator.Play(idleAnimOverride);
					yield return new TIWaitForSeconds(SYMBOL_SHOW_IDLE_TIME);
				}
				else
				{
					// now show the idle for a second or two so the player sees what it is (assuming an idle is needed)
					if (FEATURE_EFFECT_IDLE_ANIM_NAME != "")
					{
						wildEffectAnimator.Play(FEATURE_EFFECT_IDLE_ANIM_NAME);
						yield return new TIWaitForSeconds(SYMBOL_SHOW_IDLE_TIME);
					}
				}
			}

			if (!reelGame.isFreeSpinGame())
			{
				hideFeature();
			}

			if (RESUME_MUSIC_KEY != null)
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap(RESUME_MUSIC_KEY));
			}
		}	
	}

	/// Play the free spin effect of a symbol flying up to the top of the reels, only triggered if this module is attached to a FreeSpinGame
	private IEnumerator flySymbolToIndicator()
	{
		// add the current wild to the list of active ones so they will start being changed as the spin
		activeWilds.Add(mutationTarget);

		// hide the feature animation as we replace it with the symbol indicator
		hideFeatureAnimation();

		// create the indicator instance
		GameObject indicatorObj = CommonGameObject.instantiate(symbolIndicatorPrefab) as GameObject;

		//Set indicator under reel scaler, make the indicator scaling properly.
		if (symbolIndicatorParent != null)
		{
			indicatorObj.transform.parent = symbolIndicatorParent;
		}
		//Set to under current transform if symbolIndicatorParent is null.
		else
		{
			indicatorObj.transform.parent = transform;
		}
		indicatorObj.transform.localPosition = new Vector3(0, 0, INDICATOR_START_Z_POS);
		WildOverlayTransformFreeSpinSymbolIndicator indicator = indicatorObj.GetComponent<WildOverlayTransformFreeSpinSymbolIndicator>();
		indicator.setParticleTrailVisible(true);

		bool doSpecialCaseForM1 = hasSpecialCaseForM1 && mutationTarget.Contains("M1");
		int targetSymbolIndex = getTargetSymbolIndex(mutationTarget);
		indicator.playSymbolAnimation(targetSymbolIndex, doSpecialCaseForM1);
		
		// Determine the destination position based on which symbol we have.
		string shortMutationTargetName = SlotSymbol.getShortNameFromName(mutationTarget);
		int iconIndex = int.Parse(shortMutationTargetName.Substring(1)) - 1;

		Vector3 targetScale;
		if (mutationTarget == "M1-2A")
		{
			targetScale = new Vector3(SYMBOL_INDICATOR_M1_SCALE, SYMBOL_INDICATOR_M1_SCALE, 1.0f);
		}
		else if (SlotSymbol.isMajorFromName(mutationTarget))
		{
			targetScale = new Vector3(SYMBOL_INDICATOR_OTHER_MAJOR_SCALE, SYMBOL_INDICATOR_OTHER_MAJOR_SCALE, 1.0f);
		}
		else
		{
			targetScale = new Vector3(SYMBOL_INDICATOR_MINOR_SCALE, SYMBOL_INDICATOR_MINOR_SCALE, 1.0f);
		}
		
		// Fly from the newly duplicated moon symbol to the small symbol up top. Also need to scale down to have it fit the top row symbol size.
		Vector3 targetPos = flyTopPoints[iconIndex].position;
		targetPos.z = indicatorObj.transform.position.z;

		if (WILD_INDICATOR_TRAVEL_SOUND != "")
		{
			Audio.play(WILD_INDICATOR_TRAVEL_SOUND);
		}

		iTween.MoveTo(indicatorObj, iTween.Hash("position", targetPos, "time", TIME_TO_FLY_TO_SYMBOL_AT_TOP, "easetype", iTween.EaseType.easeInOutQuad));
		iTween.ScaleTo(indicatorObj, iTween.Hash("scale", targetScale, "time", TIME_TO_FLY_TO_SYMBOL_AT_TOP, "easetype", iTween.EaseType.easeInOutQuad));
		
		// Wait for the flight to finish.
		yield return new TIWaitForSeconds(TIME_TO_FLY_TO_SYMBOL_AT_TOP);

		if (WILD_INDICATOR_ARRIVE_SOUND != "")
		{
			Audio.play(WILD_INDICATOR_ARRIVE_SOUND);
		}

		playIndicatorArriveVO(iconIndex);

		indicator.setParticleTrailVisible(false);
		if(indicator.shouldHideSymbolDurningTween)
		{
			indicator.turnOnSymbolVisual(targetSymbolIndex);
		}
		// turn off the shroud from the moon feature
		hideFeature();
	}

	// Check if the passed symbol has an override
	private string getSymbolFeatureEffectStartAnimOverride(string symbolName)
	{
		int targetIndex = getTargetSymbolIndex(symbolName);

		if (targetIndex != -1 && symbolFeatureEffectStartAnimOverrides != null && targetIndex >= 0 && targetIndex < symbolFeatureEffectStartAnimOverrides.Length)
		{
			return symbolFeatureEffectStartAnimOverrides[targetIndex];
		}
		else
		{
			return "";
		}
	}

	private float getSymbolFeatureEffectStartAnimOverridesLength(string symbolName)
	{
		int targetIndex = getTargetSymbolIndex(symbolName);

		if (targetIndex != -1 && symbolFeatureEffectStartAnimOverridesLengths != null && targetIndex >= 0 && targetIndex < symbolFeatureEffectStartAnimOverridesLengths.Length)
		{
			return symbolFeatureEffectStartAnimOverridesLengths[targetIndex];
		}
		else
		{
			return 0;
		}
	}

	private string getSymbolFeatureEffectIdleAnimOverride(string symbolName)
	{
		int targetIndex = getTargetSymbolIndex(symbolName);

		if (targetIndex != -1 && symbolFeatureEffectIdleAnimOverrides != null && targetIndex >= 0 && targetIndex < symbolFeatureEffectIdleAnimOverrides.Length)
		{
			return symbolFeatureEffectStartAnimOverrides[targetIndex];
		}
		else
		{
			return "";
		}
	}

	/// Get the index into the symbol list for the passed symbolName
	private int getTargetSymbolIndex(string symbolName)
	{
		int targetIndex = symbolOrderList.IndexOf(symbolName);

		if (targetIndex == -1)
		{
			Debug.LogError("WildOverlayTransformModule::getTargetSymbolIndex() - symbolName not found in symbolOrderList!");
		}

		return targetIndex;
	}

	/// Handle turning on a symbol visual which reveals in the moon animation
	private void turnOnSymbolVisual(string symbolName)
	{
		int targetIndex = getTargetSymbolIndex(symbolName);

		if (targetIndex != -1)
		{
			for (int i = 0; i < wildEffectSymbols.Length; i++)
			{
				if (targetIndex == i)
				{
					wildEffectSymbols[i].SetActive(true);
				}
				else
				{
					wildEffectSymbols[i].SetActive(false);
				}
			}
		}
		else
		{
			Debug.LogError("WildOverlayTransformModule::turnOnSymbolVisual() - symbolName not found in symbolOrderList!");
		}
	}

	/// Play the indicator arrive vo if this is a valid index, and there is a clip and not an empty string
	private void playIndicatorArriveVO(int voIndex)
	{
		if (voIndex < 0 || voIndex >= SYMBOL_INDICATOR_ARRIVE_VOS.Length)
		{
			return;
		}
		else
		{
			string voSoundName = SYMBOL_INDICATOR_ARRIVE_VOS[voIndex];
			if (voSoundName != "")
			{
				Audio.play(voSoundName, 1.0f, 0.0f, SYMBOL_INDICATOR_ARRIVE_VO_DELAY);
			}
		}
	}
}
