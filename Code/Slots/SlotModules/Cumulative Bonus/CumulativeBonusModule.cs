using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * CumulativeBonusModule.cs
 * author: Leo Schnee
 * CumulativeBonusModule is designed to keep track of the number of bonus symbols that have been recieved,
 * and once a bonus game is reached clear out that list.
 *
 * In order for this to work we need to get information from 2 different places.
 * When a SlotBaseGame is started up we need to look in the started event to see which bonus symbols we had from
 * the last play.
 * Then on each spin we need to look though the reevaluation information for the next spin because that's where the anticipation
 * and the cumulative information is at.
 * 
 */ 
public class CumulativeBonusModule : SlotModule 
{
	[SerializeField] protected List<Animator> cumulativeSymbolAnimators; // The animators that get played when one of the cumulative symbols is found.
	[SerializeField] private float TIME_MOVE_BN_SYMBOL = 1.0f;
	[SerializeField] private string CUMULATIVE_SYMBOL_ON_ANIM_NAME = "on";
	[SerializeField] private float CUMULATIVE_SYMBOL_ON_ANIM_LENGTH;		// need this so we can correctly hide the symbol inbetween some animations
	[SerializeField] protected string CUMULATIVE_SYMBOL_OFF_ANIM_NAME = "off";
	[SerializeField] private string CUMULATIVE_SYMBOL_ON_STILL_ANIM_NAME = "on still";
	[SerializeField] private bool waitForSymbolOnStillCompletion = true; // false speeds up presentation when more than one symbol lands
	[SerializeField] private Vector3 symbolScaleDownSize = Vector3.one;
	[SerializeField] private Vector3 symbolPositionOffset = Vector3.zero;
	[SerializeField] private float TIME_SCALE_BN_SYMBOL = 1.0f;
	[SerializeField] private Animator bonusAcquiredCelebrationAnimator;
	[SerializeField] private string BONUS_ACQUIRED_CELEBRATION_ON_ANIM_NAME = "on";
	[SerializeField] private string BONUS_ACQUIRED_CELEBRATION_OFF_ANIM_NAME = "off";
	[SerializeField] private float BONUS_ACQUIRED_CELEBRATION_SHOW_TIME = 1.5f;
	[SerializeField] private float DELAY_BETWEEN_MOVING_BONUS_SYMBOLS_TO_ACUM_AREA = 0.3f;
	[SerializeField] private Layers.LayerID LAYER_SYMBOLS_MOVING = Layers.LayerID.ID_SLOT_OVERLAY;
	[SerializeField] private Layers.LayerID LAYER_SYMBOLS_MOVE_COMPLETE = Layers.LayerID.ID_SLOT_REELS;

	[SerializeField] private bool shouldMutateToFlyingSymbol = false;
	[SerializeField] private string flyingSymbolPostFix = "_Fly";
	[SerializeField] private bool flySymbolHideOnLand = false;

	private HashSet<string> collectedBonusSymbols = new HashSet<string>();
	private int anticipationAnimationsPlaying = 0;
	protected int numBonusSymbolsAcquired = 0;
	private SlotOutcome bonusSymbolAccumulationOutcome;						// Stores the reevaluation outcome
	private bool isCumulativeDataLoaded = false;							// Ensure we don't refresh the active bonus symbols before the data loads for it

	private const string BONUS_SYMBOLS_DATA_KEY = "bonus_symbols";
	private const string ANTICIPATION_INFO_DATA_KEY = "anticipation_info";
	private const string BONUS_SYMBOL_ACCUMULATION_TYPE_KEY = "bonus_symbol_accumulation";
	private const string SYMBOLS_DATA_KEY = "symbols";

	private const string BONUS_SYMBOL_FLY_TO_COLLECT_SOUND_KEY = "bonus_symbol_fly_to_collect";
	private const string BONUS_SYMBOL_COLLECT_SOUND_PREFIX_KEY = "bonus_symbol_collect";
	private const string BONUS_SYMBOL_INIT_SOUND_PREFIX_KEY = "bonus_symbol_fanfare";

	[SerializeField] private AudioListController.AudioInformationList allBonusRevealedAudioList; // audio list to play on collecting all symbols

	protected override void OnEnable()
	{
		base.OnEnable();

		if (isCumulativeDataLoaded)
		{
			updateCurrentlyAcquiredBonusSymbols();
		}
	}

// executeOnSlotGameStartedNoCoroutine() section
// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return reelSetDataJson != null;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		foreach (string symbol in reelSetDataJson.getStringArray(BONUS_SYMBOLS_DATA_KEY))
		{
			collectedBonusSymbols.Add(symbol);
		}

		updateCurrentlyAcquiredBonusSymbols();

		numBonusSymbolsAcquired = collectedBonusSymbols.Count;

		isCumulativeDataLoaded = true;
	}

	/// Force a refresh of the bonus symbols that are currently acquired (needs to be handled when the game is first entered and any time it is toggled on and off)
	private void updateCurrentlyAcquiredBonusSymbols()
	{
		foreach (string collectedSymbol in collectedBonusSymbols)
		{
			string nameOfBonusSymbol = collectedSymbol;
			char number = nameOfBonusSymbol[nameOfBonusSymbol.Length - 1];
			int bonusIndex = (int)char.GetNumericValue(number) - 1; // One less because BN symbols start with 1. i.e. BN1, BN2, BN3
			if (cumulativeSymbolAnimators.Count > bonusIndex)
			{
				// Set the animation to their on state.
				cumulativeSymbolAnimators[bonusIndex].Play(CUMULATIVE_SYMBOL_ON_STILL_ANIM_NAME);
			}
		}
	}

	public override bool needsToExecuteOnReelEndRollback(SlotReel stoppingReel)
	{
		bonusSymbolAccumulationOutcome = getBonusSymbolAccumulationOutcome();

		if (getCumulativeBonusSymbolInformationThisSpin().ContainsKey(stoppingReel.reelID))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public override IEnumerator executeOnReelEndRollback(SlotReel stoppingReel)
	{
		// Play the anticipation on the BN symbol that's stopping.
		foreach (SlotSymbol symbol in stoppingReel.visibleSymbols)
		{
			string cumulativeSymbolName = stoppingReel.getReplacedSymbolName(getCumulativeBonusSymbolInformationThisSpin()[stoppingReel.reelID]);
			if (symbol.serverName == cumulativeSymbolName)
			{
				// We want to play the anticipation on the subsymbol.
				anticipationAnimationsPlaying++;
				symbol.animateAnticipation(onAnticipationAnimationDone);

				collectedBonusSymbols.Add(symbol.serverName);
				Audio.play(Audio.soundMap(BONUS_SYMBOL_INIT_SOUND_PREFIX_KEY + collectedBonusSymbols.Count));
			}
			else
			{
				// check sub symbols to see if they match
				SlotSymbol subSymbol = symbol.subsymbol;

				while (subSymbol != null)
				{
					if (subSymbol.serverName == cumulativeSymbolName)
					{
						// found a match
						anticipationAnimationsPlaying++;
						subSymbol.animateAnticipation(onAnticipationAnimationDone);

						collectedBonusSymbols.Add(subSymbol.serverName);
						Audio.play(Audio.soundMap(BONUS_SYMBOL_INIT_SOUND_PREFIX_KEY + collectedBonusSymbols.Count));
						break;
					}

					subSymbol = subSymbol.subsymbol;
				}
			}
		}
		yield break;
	}

	public override bool needsToExecuteOnBonusGameEnded()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGameEnded()
	{
		collectedBonusSymbols.Clear();
		yield break;
	}

// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return true;
	}
	
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		yield break;
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		while (anticipationAnimationsPlaying != 0)
		{
			// Wait for all the anticipations to stop playing
			yield return null;
		}
		// Now we want to go through the symbols and grab the BN symbols to move them to to their cumulative symbol animators.
		Dictionary<int,string> cumulativeBonusSymbolInformation = getCumulativeBonusSymbolInformationThisSpin();
		foreach (int reelID in cumulativeBonusSymbolInformation.Keys)
		{
			bool found = false;
			SlotReel reel = reelGame.engine.getSlotReelAt(reelID - 1);
			string cumulativeSymbolName = reel.getReplacedSymbolName(getCumulativeBonusSymbolInformationThisSpin()[reel.reelID]);

			// One at a time we want to move the bonus symbols from their symbol into the cumulative location.
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				//string symbolName = symbol.serverName;

				if (symbol.serverName == cumulativeSymbolName)
				{
					found = true;
					yield return StartCoroutine(moveSymbolToAccumulationArea(symbol, cumulativeSymbolName));

					// now, make the symbol normal again for outcome display purposes
					symbol.mutateTo(SlotSymbol.getNameWithoutSubsymbolFromName(symbol.name));
				}
				else
				{
					// check sub symbols to see if they match
					SlotSymbol subSymbol = symbol.subsymbol;

					while (subSymbol != null)
					{
						if (subSymbol.serverName == cumulativeSymbolName)
						{
							// found a match
							found = true;
							yield return StartCoroutine(moveSymbolToAccumulationArea(subSymbol, cumulativeSymbolName));

							// now, make the symbol normal again for outcome display purposes
							symbol.mutateTo(SlotSymbol.getNameWithoutSubsymbolFromName(symbol.name));
							break;
						}

						subSymbol = subSymbol.subsymbol;
					}
				}
			}

			if (!found)
			{
				Debug.LogWarning("Couldn't find " + cumulativeSymbolName + " in reel " + reelID);
			}
		}

		if (reelGame.outcome.isBonus)
		{
			if (reelGame is SlotBaseGame)
			{
				// tell the base game we're already going to play the bonus outcome stuff in this module, so not to handle it itself
				((SlotBaseGame)reelGame).isBonusOutcomePlayed = true;
			}

			if (bonusSymbolAccumulationOutcome != null)
			{
				BonusGameManager.instance.betMultiplierOverride = bonusSymbolAccumulationOutcome.getBetMultiplierOverride();
			}

			if (bonusAcquiredCelebrationAnimator != null)
			{
				yield return StartCoroutine(playBonusAcquiredAnimations());
			}

			numBonusSymbolsAcquired = 0;
		}
	}

	//Make this overrideable incase children need different execution
	protected virtual IEnumerator playBonusAcquiredAnimations()
	{
		bonusAcquiredCelebrationAnimator.Play(BONUS_ACQUIRED_CELEBRATION_ON_ANIM_NAME);

		yield return StartCoroutine(AudioListController.playListOfAudioInformation(allBonusRevealedAudioList));

		yield return new TIWaitForSeconds(BONUS_ACQUIRED_CELEBRATION_SHOW_TIME);
		bonusAcquiredCelebrationAnimator.Play(BONUS_ACQUIRED_CELEBRATION_OFF_ANIM_NAME);
	}


	private IEnumerator moveSymbolToAccumulationArea(SlotSymbol symbol, string nameOfBonusSymbol)
	{
		// Get the index that we need to move the symbol to.
		// Move the symbol to the accumlation area.
		char number = nameOfBonusSymbol[nameOfBonusSymbol.Length - 1];
		int bonusIndex = (int)char.GetNumericValue(number) - 1; // One less because BN symbols start with 1. i.e. BN1, BN2, BN3
		if (cumulativeSymbolAnimators.Count > bonusIndex)
		{
			Vector3 startingPosition = symbol.gameObject.transform.position;
			
			//This is incase we want to fly a different symbol than what lands on the reels, Ex. LIS01
			if (shouldMutateToFlyingSymbol)
			{
				symbol.mutateTo(symbol.name + flyingSymbolPostFix);
			}

			// Change the layer so it's on top of stuff.
			CommonGameObject.setLayerRecursively(symbol.gameObject, (int)LAYER_SYMBOLS_MOVING);

			Vector3 symbolPosition = cumulativeSymbolAnimators[bonusIndex].transform.position;
			symbolPosition.z -= 1;

			// modify by original symbol offsets at this scale
			Vector3 symbolAdjustment = new Vector3(symbol.info.positioning.x * symbolScaleDownSize.x,
													symbol.info.positioning.y * symbolScaleDownSize.y,
													symbol.info.positioning.y * symbolScaleDownSize.z);
			symbolPosition += symbolAdjustment;

			startingPosition.z = symbolPosition.z;
			symbol.gameObject.transform.position = startingPosition;

			Audio.play(Audio.soundMap(BONUS_SYMBOL_FLY_TO_COLLECT_SOUND_KEY));

			iTween.ScaleTo(symbol.gameObject, iTween.Hash("scale", symbolScaleDownSize, "time", TIME_SCALE_BN_SYMBOL, "islocal", true, "easetype", iTween.EaseType.easeOutExpo));
			yield return new TITweenYieldInstruction(
				iTween.MoveTo(symbol.gameObject, symbolPosition, TIME_MOVE_BN_SYMBOL)
			);

			yield return StartCoroutine(playCumulativeSymbolAcquiredAnim(cumulativeSymbolAnimators[bonusIndex], symbol));

			yield return new TIWaitForSeconds(DELAY_BETWEEN_MOVING_BONUS_SYMBOLS_TO_ACUM_AREA);
		}
		else
		{
			Debug.LogWarning("Not enough cumulativeSymbolAnimators defined for " + symbol.name);
		}
	}

	/// Play the acquired anim on the right hand side UI, then release the symbol we moved over
	public virtual IEnumerator playCumulativeSymbolAcquiredAnim(Animator cumulativeSymbolAnimator, SlotSymbol symbol)
	{
		if (flySymbolHideOnLand)
		{
			symbol.gameObject.SetActive(false);
		}

		numBonusSymbolsAcquired++;
		Audio.play(Audio.soundMap(BONUS_SYMBOL_COLLECT_SOUND_PREFIX_KEY + numBonusSymbolsAcquired));

		// Once we make it there we should clean up the symbol.
		// And now that we've released the symbol instance of it, we should play the aniamtion for this BN symbol.
		if (CUMULATIVE_SYMBOL_ON_ANIM_LENGTH == 0)
		{
			Debug.LogWarning("CUMULATIVE_SYMBOL_ON_ANIM_LENGTH was 0!");

			yield return StartCoroutine(CommonAnimation.playAnimAndWait(cumulativeSymbolAnimator, CUMULATIVE_SYMBOL_ON_ANIM_NAME));

			Vector3 currentPos = symbol.gameObject.transform.localPosition;
			symbol.gameObject.transform.localPosition = new Vector3(currentPos.x, currentPos.y, 5);
		}
		else
		{
			cumulativeSymbolAnimator.Play(CUMULATIVE_SYMBOL_ON_ANIM_NAME);

			yield return new TIWaitForSeconds(0.1f);

			Vector3 currentPos = symbol.gameObject.transform.localPosition;
			symbol.gameObject.transform.localPosition = new Vector3(currentPos.x, currentPos.y, 5);

			yield return new TIWaitForSeconds(CUMULATIVE_SYMBOL_ON_ANIM_LENGTH - 0.1f);
		}

		if (waitForSymbolOnStillCompletion)
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(cumulativeSymbolAnimator, CUMULATIVE_SYMBOL_ON_STILL_ANIM_NAME));
		}
		else
		{
			// help speed up presentation of multiple symbols landing
			StartCoroutine(CommonAnimation.playAnimAndWait(cumulativeSymbolAnimator, CUMULATIVE_SYMBOL_ON_STILL_ANIM_NAME));
		}

		CommonGameObject.setLayerRecursively(symbol.gameObject, (int)LAYER_SYMBOLS_MOVE_COMPLETE);
		symbol.cleanUp();
	}

	/// Only handle this for subsymbols because maybe regular symbols that are cumulative would work different
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{ 
		if (symbol.subsymbol != null && collectedBonusSymbols.Contains(symbol.subsymbol.serverName))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		// We want to hide the subsymbol, since it shouldn't show up on the reels anymore.
		string debugName = symbol.debugName;
		string debug = "Mutated " + debugName;
		symbol.mutateTo(SlotSymbol.getNameWithoutSubsymbolFromName(symbol.name));
		symbol.debugName = debugName;
		symbol.debug = debug;
	}

	/// Check if we have a bonus_symbol_accumulation reevaluation
	private SlotOutcome getBonusSymbolAccumulationOutcome()
	{
		List<SlotOutcome> reevaluationOutcomes = reelGame.outcome.getReevaluationsAsSlotOutcomes();

		foreach (SlotOutcome reevaluation in reevaluationOutcomes)
		{
			if (reevaluation.getType() == BONUS_SYMBOL_ACCUMULATION_TYPE_KEY)
			{
				return reevaluation;
			}
		}

		return null;
	} 

	// Looks through the reevaluation information for the next spin, if it exists and checks to see if 
	private Dictionary<int,string> getCumulativeBonusSymbolInformationThisSpin()
	{
		Dictionary<int,string> symbols = new Dictionary<int, string>();

		if (bonusSymbolAccumulationOutcome != null)
		{
			symbols = bonusSymbolAccumulationOutcome.getAnticipationSymbols();
		}

		return symbols;
	}

	// Keeps track of how many animations are being played so we don't skip into the next spin too quickly.
	private void onAnticipationAnimationDone(SlotSymbol sender)
	{
		anticipationAnimationsPlaying--;
	}

	public override bool needsToExecuteOnBigWinEnd()
	{
		return true;
	}

	public override void executeOnBigWinEnd()
	{
		//Need to do this after a big win so our won symbols turn back on
		updateCurrentlyAcquiredBonusSymbols();

	}
}
