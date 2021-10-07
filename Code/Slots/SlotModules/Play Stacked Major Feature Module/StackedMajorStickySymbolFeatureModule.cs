using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StackedMajorStickySymbolFeatureModule : BaseStackedMajorFeatureModule
{
	[SerializeField] private GameObject stickySymbolEffectPrefab = null;			// Prefab object to make sticky symbol effect clones from
	[SerializeField] private string stickyAnimationName = "";
	[SerializeField] private string loopingAnimationName = "";
	[SerializeField] private float TIME_BETWEEN_STICKY_EFFECTS = 0.0f;
	[SerializeField] private float PRE_REEVALUATION_FINISHED_WAIT = 0.0f;
	[SerializeField] private float STICKY_ANIM_LENGTH = 1.0f;
	[SerializeField] private float POPULATION_ANIM_LENGTH_OVERRIDE = -1.0f;
	[SerializeField] private bool playLoopingLockedAnimation = false;
	[SerializeField] private bool needsToPlaySpecialRespinMusic = true;
	private GameObjectCacher stickySymbolCacher = null;
	private List<GameObject> activeStickyEffects = new List<GameObject>();				// Need this so we can hide all the stick effects once the reels fully stop
	private List<Animator> activeStickyAnimators = new List<Animator>();		
	private bool loopingAnimators = false;
	private bool finishPopulationAnimation = false;

	private const string STICKY_SOUND_KEY = "sticky_wild_symbol";
	private const string RESPIN_MUSIC_KEY = "respin_music";

	public override void Awake()
	{
		base.Awake();
		stickySymbolCacher = new GameObjectCacher(this.gameObject, stickySymbolEffectPrefab);
	}

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
				needsToAnticipateFeature = true;
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

				// Wait before playing the next sound
				if (Audio.canSoundBeMapped(STACKED_MAJOR_FEATURE_MUSIC_KEY))
				{
					// need to play a loopped background music for the M1 respins feature
					Audio.switchMusicKeyImmediate(Audio.soundMap (STACKED_MAJOR_FEATURE_MUSIC_KEY), 0.0f);
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
		if (largeSymbolLeft.gameObject.activeSelf) //If at least the left symbol is on, then we need to do stuff when the reels stop
		{
			return true;
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// need to wait for the reveal animations to finish before moving on
		if (needsToPlayFeature)
		{
			yield return new TIWaitForSeconds(REVEAL_ANIMATION_LENGTH);
			yield return StartCoroutine(playFeatureTextAnimation());
			if (Audio.canSoundBeMapped(RESPIN_MUSIC_KEY) && needsToPlaySpecialRespinMusic)
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap(RESPIN_MUSIC_KEY));
			}
		}
		else
		{
			//If we got here then we know that the left large symbol prefab is active but no feature happened, and now we need to swap it to an actual symbol
			swapOverlaysForSymbolInstanceOnReel(0, TRIGGER_SYMBOL);
		}
		needsToAnticipateFeature = false;
	}

	public override bool needsToExecuteOnChangeSymbolToSticky()
	{
		return true;
	}

	public override IEnumerator executeOnChangeSymbolToSticky(SlotSymbol symbol, string name)
	{
		if(!loopingAnimators)
		{
			loopingAnimators = true;
		}
		finishPopulationAnimation = true;
		symbol.mutateTo(name);
		GameObject stickyEffectInstance = stickySymbolCacher.getInstance();

		// place the stickyEffect in the correct place
		stickyEffectInstance.transform.parent = symbol.reel.getReelGameObject().transform;
		stickyEffectInstance.transform.localScale = symbol.info.scaling;
		stickyEffectInstance.transform.localPosition = symbol.gameObject.transform.localPosition;

		activeStickyEffects.Add(stickyEffectInstance);
		stickyEffectInstance.SetActive(true);
		if (Audio.canSoundBeMapped (STICKY_SOUND_KEY))
		{
			Audio.play(Audio.soundMap(STICKY_SOUND_KEY));
		}
		Animator stickyAnimator = stickyEffectInstance.GetComponentInChildren<Animator>();
		if (stickyAnimator != null)
		{
			activeStickyAnimators.Add(stickyAnimator);
			if(POPULATION_ANIM_LENGTH_OVERRIDE > -1.0f)
			{
				stickyAnimator.Play(stickyAnimationName);
				yield return new TIWaitForSeconds(POPULATION_ANIM_LENGTH_OVERRIDE);
			}
			else
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(stickyAnimator, stickyAnimationName));
			}
		}
		yield return new TIWaitForSeconds(TIME_BETWEEN_STICKY_EFFECTS);
		if(playLoopingLockedAnimation)
		{
			StartCoroutine(loopAnimateAllStickySymbols());
		}

	}

	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		return needsToPlayFeature;
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		// using this to track if this is the last spin after the base class goes, since it might decrement how many spins remain

		if(finishPopulationAnimation)
		{
			yield return new TIWaitForSeconds(PRE_REEVALUATION_FINISHED_WAIT); //Wait for any remaining animations to finish before doing rollup stuff
		}

		if (!reelGame.hasReevaluationSpinsRemaining)
		{
			swapOverlaysForSymbolInstanceOnReel(0, TRIGGER_SYMBOL);
			swapOverlaysForSymbolInstanceOnReel(4, TRIGGER_SYMBOL);
			hideAllStickySymbolEffects();
			needsToPlayFeature = false;
		}

		finishPopulationAnimation = false;
		yield return StartCoroutine(base.executeOnReevaluationReelsStoppedCallback());
	}

	private void hideAllStickySymbolEffects()
	{
		foreach (GameObject stickyMask in activeStickyEffects)
		{
			stickySymbolCacher.releaseInstance(stickyMask);
		}
		loopingAnimators = false;
		activeStickyEffects.Clear();
		activeStickyAnimators.Clear();
	}

	private IEnumerator loopAnimateAllStickySymbols()
	{
		while (loopingAnimators)
		{
			yield return new TIWaitForSeconds(STICKY_ANIM_LENGTH);
			foreach (Animator stickyAnimator in activeStickyAnimators)
			{
				stickyAnimator.Play (loopingAnimationName);
			}
		}
	}
}
