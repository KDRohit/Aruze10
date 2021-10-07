using UnityEngine;
using System.Collections;

public class StackedMajorLinkedReelsRespinFeatureModule : BaseStackedMajorFeatureModule
{
	[SerializeField] private GameObject middleReelsAnticipation = null;
	[SerializeField] private GameObject poplulatePrefab = null;
	[SerializeField] private GameObject maskToActivate = null;
	[SerializeField] private float PRE_RESPIN_WAIT = 0.0f;
	[SerializeField] private float POPULATE_WAIT = 0.0f;
	[SerializeField] private string MUTATED_SYMBOL_NAME = ""; //Just the name goes here. The prefix and size stuff will be constructed later on
	private const string RESPIN_MUSIC_KEY = "respin_music";
	private const string FEATURE_INTRO_SOUND_KEY = "basegame_feature_intro_3";

	public override IEnumerator checkAndPlayReelFeature(SlotReel stoppedReel)
	{
		int reelId = stoppedReel.reelID;
		int reelIndex = reelId - 1;
		bool triggeredFeature = false;
		// this game only has special features on the 1st and 5th reels
		if (reelId == 1)
		{
			triggeredFeature = doesReelContainAllFeatureSymbol(reelIndex, TRIGGER_SYMBOL);
			if (triggeredFeature)
			{
				needsToAnticipateFeature = true;
				if (largeSymbolLeftScaler != null)
				{
					scaleAndPositionLargeOverlay(reelIndex, stoppedReel.visibleSymbols.Length-1, largeSymbolLeftScaler); //scale and position our overlay based on the top symbol
				}
				if (Audio.canSoundBeMapped(STACKED_REEL1_EXPAND_SOUND))
				{
					Audio.play(Audio.soundMap(STACKED_REEL1_EXPAND_SOUND));
				}

				largeSymbolLeft.gameObject.SetActive(true);
				if (LARGE_SYMBOL_REVEAL_ANIMATION_NAME != "")
				{
					largeSymbolLeft.Play(LARGE_SYMBOL_REVEAL_ANIMATION_NAME);
				}
			}
		}
		else if (reelId == 5)
		{
			// need to make sure that the reel feature is the same as the triggered one
			bool reelOnetriggeredFeature = doesReelContainAllFeatureSymbol(0, TRIGGER_SYMBOL);
			triggeredFeature = doesReelContainAllFeatureSymbol(reelIndex, TRIGGER_SYMBOL);

			if (triggeredFeature && reelOnetriggeredFeature)
			{
				needsToPlayFeature = true;
				if (largeSymbolRightScaler != null)
				{
					scaleAndPositionLargeOverlay(reelIndex, stoppedReel.visibleSymbols.Length-1, largeSymbolRightScaler); //scale and position our overlay based on the top symbol
				}
				if (Audio.canSoundBeMapped(STACKED_REEL5_EXPAND_SOUND))
				{
					Audio.play(Audio.soundMap(STACKED_REEL5_EXPAND_SOUND));
				}

				if (largeSymbolRight != null)
				{
					largeSymbolRight.gameObject.SetActive(true);
					if (LARGE_SYMBOL_REVEAL_ANIMATION_NAME != "")
					{
						largeSymbolRight.Play(LARGE_SYMBOL_REVEAL_ANIMATION_NAME);
					}
				}

				if (Audio.canSoundBeMapped(STACKED_MAJOR_FEATURE_MUSIC_KEY))
				{
					Audio.play(Audio.soundMap(STACKED_MAJOR_FEATURE_MUSIC_KEY));
				}
			}
		}
		yield break;
	}

	public override string getFeatureAnticipationNameFromModule()
	{
		if (needsToAnticipateFeature)
		{
			return TRIGGER_SYMBOL;
		}
		return base.getFeatureAnticipationNameFromModule();
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		if (largeSymbolLeft.gameObject.activeSelf)
		{
			return true;
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		//For some reason this is being called before our checkAndPlayReelFeature on reel 5 is being called
		//so the needsToPlayFeature flag is being turned on appropriately. 
		//Need to explcicitly check the visible symbols to see if the feature is being activated
		bool reelFivetriggeredFeature = doesReelContainAllFeatureSymbol(4, TRIGGER_SYMBOL);
		if (reelFivetriggeredFeature)
		{
			// need to wait for the reveal animations to finish before moving on
			yield return new TIWaitForSeconds(REVEAL_ANIMATION_LENGTH);
		}
		else
		{
			//If we got here then we know that the left large symbol prefab is active but no feature happened, and now we need to swap it to an actual symbol
			swapOverlaysForSymbolInstanceOnReel(0, TRIGGER_SYMBOL);
		}

		needsToAnticipateFeature = false;
	}
	protected virtual IEnumerator doLinkedReelsAfterPaylines()
	{
		yield return StartCoroutine(playFeatureTextAnimation());

		// a special mask may be needed for the linked symbol display
		if (maskToActivate != null)
		{
			maskToActivate.SetActive(true);
		}

		yield return StartCoroutine(doLinkedReels());
		if (Audio.canSoundBeMapped(RESPIN_MUSIC_KEY))
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(RESPIN_MUSIC_KEY));
		}
	}
	public IEnumerator doLinkedReels()
	{
		// Change the middle reels into the W4
		string largeFeatureSymbolName = "";
		string FLATTENED_SYMBOL_POSTFIX = "";
		if (reelGame.isGameUsingOptimizedFlattenedSymbols)
		{
			FLATTENED_SYMBOL_POSTFIX = SlotSymbol.FLATTENED_SYMBOL_POSTFIX;
		}

		if (reelGame.isFreeSpinGame())
		{
			largeFeatureSymbolName = SlotSymbol.constructNameFromDimensions(MUTATED_SYMBOL_NAME + FLATTENED_SYMBOL_POSTFIX, 3, 4);
		}
		else
		{
			largeFeatureSymbolName = SlotSymbol.constructNameFromDimensions(MUTATED_SYMBOL_NAME + FLATTENED_SYMBOL_POSTFIX, 3, 3);
		}
		SlotSymbol mutatedSymbol = reelGame.engine.getVisibleSymbolsAt(1)[0];
		if(poplulatePrefab != null)
		{
			SymbolInfo wdInfo = reelGame.findSymbolInfo(largeFeatureSymbolName);
			poplulatePrefab.SetActive(true);
			poplulatePrefab.transform.localScale = wdInfo.scaling;
			yield return new TIWaitForSeconds(POPULATE_WAIT);
			poplulatePrefab.SetActive(false);
		}
		mutatedSymbol.mutateTo(largeFeatureSymbolName);
		if (Audio.canSoundBeMapped(FEATURE_INTRO_SOUND_KEY))
		{
			Audio.play(Audio.soundMap(FEATURE_INTRO_SOUND_KEY));
		}
		mutatedSymbol.animateOutcome();
		yield return new TIWaitForSeconds(PRE_RESPIN_WAIT);
	}

	public override bool needsToExecuteOnReevaluationReelsStoppedCallback ()
	{
		return needsToPlayFeature;
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback ()
	{
		// using this to track if this is the last spin after the base class goes, since it might decrement how many spins remain

		if (!reelGame.hasReevaluationSpinsRemaining)
		{
			swapOverlaysForSymbolInstanceOnReel(0, TRIGGER_SYMBOL);
			swapOverlaysForSymbolInstanceOnReel(4, TRIGGER_SYMBOL);
			needsToPlayFeature = false;
		}
		if (middleReelsAnticipation != null)
		{
			middleReelsAnticipation.SetActive(false);
		}
		if (maskToActivate != null)
		{
			maskToActivate.SetActive(false);
		}
		yield return StartCoroutine(base.executeOnReevaluationReelsStoppedCallback());
	}

	public override bool needsToExecuteOnReevaluationSpinStart()
	{
		return true;
	}

	public override IEnumerator executeOnReevaluationSpinStart()
	{
		if(needsToPlayFeature)
		{
			if (middleReelsAnticipation != null)
			{
				middleReelsAnticipation.SetActive(true);
			}
		}
		yield break;
	}

	public override bool needsToExecuteAfterPaylines ()
	{
		return true;
	}

	public override IEnumerator executeAfterPaylinesCallback (bool winsShown)
	{
		if(needsToPlayFeature)
		{
			yield return StartCoroutine(doLinkedReelsAfterPaylines());
		}
	}
		
}
