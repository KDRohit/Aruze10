using UnityEngine;
using System.Collections;

public class StackedMajorMutatingWildFeatureModule : BaseStackedMajorFeatureModule
{
	[SerializeField] private GameObject mutateSymbolPrefab = null;
	[SerializeField] private GameObject poplulatePrefab = null;
	
	[SerializeField] private float TIME_BETWEEN_MUTATE_EFFECTS = 0.0f;
	[SerializeField] private float POPULATE_WAIT_LENGTH = 0.0f;
	[SerializeField] private float POPULATE_ANIMATION_LENGTH = 0.0f;
	[SerializeField] private string MUTATING_SYMBOL_NAME = "";

	private const string FEATURE_ANIMATION_SOUND_KEY = "basegame_feature_intro_2";
	private const string WD_TRANSFORM_SOUND = "wild_transform_general";
	private const string FEATURE_VO_KEY = "basegame_feature_vo";
	private GameObjectCacher mutatingSymbolCacher = null;


	public override void Awake()
	{
		base.Awake();
		mutatingSymbolCacher = new GameObjectCacher(this.gameObject, mutateSymbolPrefab);
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
				needsToAnticipateFeature = true;
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
				if (largeSymbolRightScaler != null)
				{
					scaleAndPositionLargeOverlay(reelIndex, stoppedReel.visibleSymbols.Length-1, largeSymbolRightScaler);
				}
				needsToPlayFeature = true;
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
		if (needsToPlayFeature)
		{
			// need to wait for the reveal animations to finish before moving on
			yield return new TIWaitForSeconds(REVEAL_ANIMATION_LENGTH);
			yield return StartCoroutine(playFeatureTextAnimation());
			if (Audio.canSoundBeMapped(FEATURE_ANIMATION_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(FEATURE_ANIMATION_SOUND_KEY));
			}

			//If we have an animation that needs to fly across the reels for example see billted01 (M2 mutation)
			if (poplulatePrefab != null)
			{
				StartCoroutine(doPopulateAnimation());
			}

			yield return StartCoroutine(doMutatingWilds());
			needsToPlayFeature = false;

			// execute the post-spin checks to restore music after the feature
			StartCoroutine(base.executeOnReevaluationReelsStoppedCallback());

		}
		else
		{
			//If we got here then we know that the left large symbol prefab is active but no feature happened, and now we need to swap it to an actual symbol
			swapOverlaysForSymbolInstanceOnReel(0, TRIGGER_SYMBOL);
		}

		needsToAnticipateFeature = false;
	}

	private IEnumerator doPopulateAnimation()
	{
		SymbolInfo wdInfo = reelGame.findSymbolInfo(MUTATING_SYMBOL_NAME);
		poplulatePrefab.SetActive(true);
		poplulatePrefab.transform.localScale = wdInfo.scaling;
		yield return new TIWaitForSeconds(POPULATE_ANIMATION_LENGTH);
		poplulatePrefab.SetActive(false);
	}

	public IEnumerator doMutatingWilds()
	{
		GameObject mutatingSymbol = null;
		// make sure actual mutations are going to occur due to the witch fireballs just in case
		if (reelGame.mutationManager.mutations.Count != 0)
		{
			StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;
			SlotReel[] reelArray = reelGame.engine.getReelArray();

			for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
			{
				for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
				{
					if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
					{
						yield return new TIWaitForSeconds(TIME_BETWEEN_MUTATE_EFFECTS);
						SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
						// replace the symbol
						if (Audio.canSoundBeMapped(WD_TRANSFORM_SOUND))
						{
							Audio.play(Audio.soundMap(WD_TRANSFORM_SOUND));
						}
						if (mutateSymbolPrefab != null)
						{
							SymbolInfo mutateInfo = reelGame.findSymbolInfo(MUTATING_SYMBOL_NAME);
							mutatingSymbol = mutatingSymbolCacher.getInstance();
							mutatingSymbol.transform.parent = symbol.reel.getReelGameObject().transform;
							mutatingSymbol.transform.localScale = mutateInfo.scaling;
							mutatingSymbol.transform.localPosition = new Vector3 (0.0f, mutateInfo.positioning.y + (j * reelGame.symbolVerticalSpacingLocal), 0.0f);
							mutatingSymbol.SetActive(true);
						}					
						StartCoroutine(mutateSymbolAfterDelay(symbol, POPULATE_WAIT_LENGTH, mutatingSymbol));
					}
				}
			}
		}
		swapOverlaysForSymbolInstanceOnReel(0, TRIGGER_SYMBOL);
		swapOverlaysForSymbolInstanceOnReel(4, TRIGGER_SYMBOL);
		turnOffLargeSymbols();
		yield break;
	}
	
	private IEnumerator mutateSymbolAfterDelay(SlotSymbol symbolToMutate, float delay, GameObject objectToCache = null)
	{
		yield return new TIWaitForSeconds(POPULATE_ANIMATION_LENGTH);
		symbolToMutate.mutateTo(MUTATING_SYMBOL_NAME);
		if(objectToCache != null)
		{
			mutatingSymbolCacher.releaseInstance(objectToCache);
		}
	}
}
