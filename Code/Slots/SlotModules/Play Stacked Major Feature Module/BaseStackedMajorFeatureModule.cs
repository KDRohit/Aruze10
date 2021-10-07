using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BaseStackedMajorFeatureModule : SlotModule
{
	[SerializeField] protected string TRIGGER_SYMBOL = "";
	[SerializeField] private List<int> featureReels = new List<int>(); //Used to determine which specific reels are being checked in executeOnSpecificReelStop 

	[SerializeField] protected Animator largeSymbolLeft = null;
	[SerializeField] protected Animator largeSymbolRight = null;

	[SerializeField] protected GameObject largeSymbolLeftScaler = null;
	[SerializeField] protected GameObject largeSymbolRightScaler = null;

	[SerializeField] protected string LARGE_SYMBOL_REVEAL_ANIMATION_NAME = "";
	[SerializeField] protected Animator featureTextAnimator;
	[SerializeField] protected GameObject featureTextObject;
	[SerializeField] protected string FEATURE_TEXT_ANIMATION_NAME = "";
	[SerializeField] protected string FEATURE_TEXT_SOUND_KEY = "";
	[SerializeField] protected float FEATURE_TEXT_SOUND_DELAY = 0.0f;
	[SerializeField] protected float REVEAL_ANIMATION_LENGTH = 0.0f;

	[SerializeField] protected string STACKED_MAJOR_FEATURE_MUSIC_KEY = "";

	private string reelAnticipationName = "BN"; //Default to anticipation for a bonus game and it will be changed if a feature is triggered
	protected const string BG_BACKGROUND_MUSIC = "reelspin_base";
	protected const string FS_BACKGROUND_MUSIC = "freespin";
	protected const string STACKED_REEL1_EXPAND_SOUND = "stacked_reel_expand_1";
	protected const string STACKED_REEL5_EXPAND_SOUND = "stacked_reel_expand_5";
	protected const string STACKED_REEL1_EXPAND_SOUND_FS = "stacked_reel_freespin_expand_1";
	protected const string STACKED_REEL5_EXPAND_SOUND_FS = "stacked_reel_freespin_expand_5";
	protected const string FEATURE_REEL_ANTICIPATION_SOUND = "bonus_anticipate_alternate";

	protected bool needsToPlayFeature = false;
	protected bool needsToAnticipateFeature = false;

	private List<BaseStackedMajorFeatureModule> mods = new List<BaseStackedMajorFeatureModule>();


	void Start()
	{
		mods.Add(this);
		foreach(BaseStackedMajorFeatureModule mod in reelGame.GetComponents<BaseStackedMajorFeatureModule>())
		{
			mods.Add(mod);
		}
	}

	public override bool needsToExecuteOnPreSpinNoCoroutine()
	{
		return true;
	}

	public override void executeOnPreSpinNoCoroutine()
	{
		turnOffLargeSymbols();
		splitAnyLargeSideSymbols();
	}

	public void turnOffLargeSymbols()
	{
		if (largeSymbolLeft != null)
		{
			largeSymbolLeft.gameObject.SetActive(false);
		}

		if (largeSymbolRight != null)
		{
			largeSymbolRight.gameObject.SetActive(false);
		}
			
	}

	public void splitAnyLargeSideSymbols()
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		SlotSymbol topLeftSymbol = reelArray[0].visibleSymbols[0];
		if (topLeftSymbol.canBeSplit())
		{
			topLeftSymbol.splitSymbol();
		}

		SlotSymbol topRightSymbol = reelArray[4].visibleSymbols[0];
		if (topRightSymbol.canBeSplit())
		{
			topRightSymbol.splitSymbol();
		}
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		foreach (int featureReelID in featureReels)
		{
			if (stoppedReel.reelID == featureReelID)
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		if (reelGame.currentReevaluationSpin == null)
		{
			yield return StartCoroutine(checkAndPlayReelFeature(stoppedReel));
		}
	}

	public virtual IEnumerator checkAndPlayReelFeature(SlotReel stoppedReel)
	{
		yield break;
	}

	public bool doesReelContainAllFeatureSymbol(int reelNum, string triggerSymbolName)
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		foreach (SlotSymbol slotSymbol in reelArray[reelNum].visibleSymbols)
		{
			if (!slotSymbol.name.Contains(triggerSymbolName))
			{
				// a symbol in this reel doesn't match the feature trigger, so NOT triggered
				return false;
			}
		}
		return true;
	}

	public override bool needsToGetFeatureAnicipationNameFromModule()
	{
		return true;
	}

	public override string getFeatureAnticipationNameFromModule()
	{
		bool returnBonus = true;
		foreach(BaseStackedMajorFeatureModule mod in mods)
		{
			if(mod.needsToAnticipateFeature)
			{
				returnBonus = false;
				reelAnticipationName = mod.TRIGGER_SYMBOL;
			}
		}
		if(returnBonus)
		{
			reelAnticipationName = "BN";
		}
		return reelAnticipationName;
	}

	public IEnumerator playFeatureTextAnimation()
	{
		if (featureTextObject != null)
		{
			featureTextObject.SetActive (true);
			if (FEATURE_TEXT_SOUND_KEY != "" && Audio.canSoundBeMapped(FEATURE_TEXT_SOUND_KEY))
			{
				Audio.playWithDelay(Audio.soundMap(FEATURE_TEXT_SOUND_KEY), FEATURE_TEXT_SOUND_DELAY);
			}
			if (featureTextAnimator != null && FEATURE_TEXT_ANIMATION_NAME != "")
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait (featureTextAnimator, FEATURE_TEXT_ANIMATION_NAME));
			}

			featureTextObject.SetActive(false);
		}
	}

	//Mutate symbols into the larger version while the overlay is still active so we don't actually see this happen
	public void swapOverlaysForSymbolInstanceOnReel(int reelIndex, string featureSymbolName)
	{
		string largeFeatureSymbolName = "";
		string FLATTENED_SYMBOL_POSTFIX = "";
		if (reelGame.isGameUsingOptimizedFlattenedSymbols)
		{
			FLATTENED_SYMBOL_POSTFIX = SlotSymbol.FLATTENED_SYMBOL_POSTFIX;
		}

		if (reelGame.isFreeSpinGame())
		{
			largeFeatureSymbolName = SlotSymbol.constructNameFromDimensions(featureSymbolName + FLATTENED_SYMBOL_POSTFIX, 1, 3);
		}
		else
		{
			largeFeatureSymbolName = SlotSymbol.constructNameFromDimensions(featureSymbolName + FLATTENED_SYMBOL_POSTFIX, 1, 4);
		}
		reelGame.engine.getVisibleSymbolsAt(reelIndex)[0].mutateTo(largeFeatureSymbolName);
		turnOffLargeSymbols();
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		if (!reelGame.hasReevaluationSpinsRemaining)
		{
			if (reelGame.isFreeSpinGame())
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap(FS_BACKGROUND_MUSIC), 0.0f);
			}
			else
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap(BG_BACKGROUND_MUSIC), 0.0f);
			}
		}

		yield break;
	}

	public override bool needsToPlayReelAnticipationSoundFromModule()
	{
		
		for (int i = 0; i < featureReels.Count-1; i++) //Make sure all the reels contain the trigger symbol except for the reel we want to anticipate on
		{
			int reelIndex = featureReels[i]-1;
			if (!doesReelContainAllFeatureSymbol(reelIndex, TRIGGER_SYMBOL) ||
				!Audio.canSoundBeMapped(FEATURE_REEL_ANTICIPATION_SOUND)) //Make sure the sound is mapped before we try to play it
			{
				return false;
			}
		}

		return true;
	}

	public override void playReelAnticipationSoundFromModule ()
	{
		Audio.play(Audio.soundMap(FEATURE_REEL_ANTICIPATION_SOUND));
	}

	protected void scaleAndPositionLargeOverlay(int reelIndex, int row, GameObject scaler)
	{
		string targetSymbolName = "";
		if (reelGame.isFreeSpinGame())
		{
			targetSymbolName = SlotSymbol.constructNameFromDimensions(TRIGGER_SYMBOL, 1, 3);
		}
		else
		{
			targetSymbolName = SlotSymbol.constructNameFromDimensions(TRIGGER_SYMBOL, 1, 4);
		}

		SymbolInfo targetSymbolInfo = reelGame.findSymbolInfo(targetSymbolName);

		Vector3 symbolOffset = targetSymbolInfo.positioning;
		Vector3 targetSymbolPosition = reelGame.engine.getReelRootsAt (reelIndex, row).transform.position;
		targetSymbolPosition = new Vector3(targetSymbolPosition.x, targetSymbolPosition.y + (row * reelGame.symbolVerticalSpacingWorld), 0);
		scaler.transform.position = targetSymbolPosition;

		// include scale value in offset positioning to account for extra bars
		float scaleValue = ReelGame.activeGame.reelGameBackground.getVerticalSpacingModifier();
		scaler.transform.position += (symbolOffset * scaleValue);

		scaler.transform.localScale = targetSymbolInfo.scaling;
	}
}
