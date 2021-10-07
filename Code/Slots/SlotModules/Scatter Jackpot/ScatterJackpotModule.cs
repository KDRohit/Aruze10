using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScatterJackpotModule : SlotModule 
{
	[SerializeField] private Animator[] jackpotAnimators;
	[SerializeField] private GameObject[] celebrationObjects;
	[SerializeField] private LabelWrapper[] jpAmounts;	// The Labels that hold the "Any N" text.
	[SerializeField] private LabelWrapper[] jpLabels;	// The Labels that hold the "Any N" text.
	[SerializeField] private Vector3 jackpotScaleAmount = Vector3.one;
	[SerializeField] private bool isStaggeringAnticipationAnims = false;
	[SerializeField] private bool isStaggeringAnticipationsBottomUp = true;
	[SerializeField] private Animator shroudAnimator;
	[SerializeField] private string shroudInAnimationName;
	[SerializeField] private string shroudOutAnimationName;

	private bool firstSpin = true;
	private bool jackpotDoneMoving = true;
	private Vector3 winningJackpotStartingPosition = Vector3.zero;
	private int jpAniamtionsPlayed = 0;
	private int numberOfProgressivesHit = 0;

	// Constants
	[SerializeField] private float TIME_MOVE_JACKPOT = 1.0f;	// The first number that should be in the Any text.
	[SerializeField] private Vector3 JACKPOT_CENTER_POS = Vector3.zero;
	[SerializeField] private float PROGRESSIVE_2_SOUND_DELAY = 0.33f; // When you have 2 progressives on a reel
	[SerializeField] private float PROGRESSIVE_3_SOUND_DELAY = 0.33f; // When you have 3 progressives on a reel.
	[SerializeField] private string JACKPOT_ANIMATION_END_NAME;
	[SerializeField] private string JACKPOT_ANIMATION_PLAY_NAME;
	private const int minNumberOfScatterWinsForJackpot = 3;

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		numberOfProgressivesHit = 0;
		if (firstSpin)
		{
			firstSpin = false;
			string payTableKey = reelGame.engine.freeSpinsPaytableKey;
			PayTable paytable = PayTable.find(payTableKey);
			if (paytable != null)
			{
				List<long> scatterWinValues = new List<long>(paytable.scatterWins.Count);
				foreach (PayTable.ScatterWin scatterWin in paytable.scatterWins.Values)
				{
					scatterWinValues.Add(scatterWin.credits * reelGame.multiplier);
				}
				scatterWinValues.Sort(
					delegate (long a, long b) 
					{ 
						return (int) (b - a); // Sort highest to lowest
					}
				);

				// Put the values in the overlay.
				if (jpAmounts.Length > scatterWinValues.Count)
				{
					Debug.LogError("Trying to set more jackpot values than are in the paytable.");
				}
				else
				{
					for (int i = 0; i < jpAmounts.Length; i++)
					{
						jpAmounts[i].text = CreditsEconomy.convertCredits(scatterWinValues[jpAmounts.Length - i - 1]);
					}
				}
			}
			else
			{
				Debug.LogError("Couldn't find paytable, can't set scatter jackpot amounts.");
			}

			// Set strings at the top of the game
			for (int i = 0; i < jpLabels.Length; i++)
			{
				if (jpLabels[i] != null)
				{
					jpLabels[i].text = Localize.textTitle("any_{0}", CommonText.formatNumber(i + reelGame.engine.progressiveThreshold + 1));
				}
				else
				{
					Debug.LogWarning("Any label at " + i + " isn't set");
				}
			}

		}
		yield break;
	}


	public override bool needsToExecuteForSymbolAnticipation(SlotSymbol symbol)
	{
		if (symbol.name.Contains("JP"))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public override void executeForSymbolAnticipation(SlotSymbol symbol)
	{
		int numberJPHitOnThisReel = 0;

		int symbolIndex = -1;
		int symbolIndexGoal = symbol.reel.visibleSymbolsBottomUp.Count;
		int direction = 1;

		if (!isStaggeringAnticipationsBottomUp)
		{
			symbolIndex = symbol.reel.visibleSymbolsBottomUp.Count;
			symbolIndexGoal = 0;
			direction = -1;
		}

		do
		{
			symbolIndex += direction;

			SlotSymbol currentSymbol = symbol.reel.visibleSymbolsBottomUp[symbolIndex];
			if (currentSymbol.name.Contains("JP"))
			{
				numberOfProgressivesHit++;
				numberJPHitOnThisReel++;

				if (currentSymbol == symbol)
				{
					// this is the symbol we should be animating
					string bonusSoundKey = "";
					if (GameState.game.keyName.Contains("wow"))
					{
						// This needs to be uncommented when wow starts using this class.
						//bonusSoundKey = "WFS_prog_jackpot_hit_0" + hits;
					}
					else
					{
						bonusSoundKey = Audio.soundMap("jackpot_symbol_fanfare" + numberOfProgressivesHit);
					}

					if (bonusSoundKey != "")
					{
						// Calculate the delay, if we have 1 hit no delay, 2 or 3 hits do 0.33 delay offset
						float delay = 0;
						if (numberJPHitOnThisReel == 2)
						{
							delay = PROGRESSIVE_2_SOUND_DELAY;
						}
						else if (numberJPHitOnThisReel == 3)
						{
							delay = PROGRESSIVE_3_SOUND_DELAY;
						}
						else
						{
							delay = 0;
						}
						
						Audio.play(bonusSoundKey, 1f, 0f, delay);
						symbol.reel.incrementStartedAnticipationAnims();

						if (isStaggeringAnticipationAnims)
						{
							symbol.animateAnticipation(symbol.reel.onAnticipationAnimationDone, delay);
						}
						else
						{
							symbol.animateAnticipation(symbol.reel.onAnticipationAnimationDone);
						}
					}
					break;
				}
			}
		} while (symbolIndex != symbolIndexGoal);
	}

// executeOnReevaluationReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		numberOfProgressivesHit = 0;
		return reelGame.engine.progressivesHit >= minNumberOfScatterWinsForJackpot;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Go through every JP symbol and animate it.
		SlotReel[]reelArray = reelGame.engine.getReelArray();
		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
			{
				if (symbol.name == "JP")
				{
					jpAniamtionsPlayed++;
					symbol.animateOutcome(animationFinishedCallback);
					CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_REELS_OVERLAY);
				}
			}
		}
		if (jpAniamtionsPlayed > 0)
		{
			Audio.play(Audio.soundMap("jackpot_symbol_anim"));
		}
		// Wait for all of the animations to play.
		while (jpAniamtionsPlayed > 0)
		{
			yield return null;
		}
	}

	private void animationFinishedCallback(SlotSymbol symbol)
	{
		CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_REELS);
		jpAniamtionsPlayed--;
	}


// executeOnPaylines() section
// functions in this section are called when the paylines start to show.
	public override bool needsToExecuteAfterPaylines()
	{
		return reelGame.engine.progressivesHit > reelGame.engine.progressiveThreshold;
	}

	public override IEnumerator executeAfterPaylinesCallback(bool winsShown)
	{
		while (!jackpotDoneMoving)
		{
			yield return null;
		}
		if (shroudAnimator != null)
		{
			shroudAnimator.Play(shroudOutAnimationName);
		}
		// Move the jackpot back.
		int jackpotIndex = reelGame.engine.progressivesHit - reelGame.engine.progressiveThreshold - 1;
		if (!checkJackpotIndex(jackpotIndex))
		{
			yield break;
		}
		Animator jackpotAnimator = jackpotAnimators[jackpotIndex];
		Vector3 inverseScale = new Vector3(jackpotAnimator.transform.localScale.x / jackpotScaleAmount.x,
		                                   jackpotAnimator.transform.localScale.y / jackpotScaleAmount.y,
		                                   jackpotAnimator.transform.localScale.z / jackpotScaleAmount.z);
		iTween.ScaleTo(jackpotAnimator.gameObject, inverseScale, TIME_MOVE_JACKPOT);
		Audio.play(Audio.soundMap("jackpot_symbol_anim2"));
		yield return StartCoroutine(moveJackpotToPosition(jackpotIndex, winningJackpotStartingPosition));
		jackpotAnimator.Play(JACKPOT_ANIMATION_END_NAME);		
		

		// Turn off celebration objects
		if (celebrationObjects != null && celebrationObjects.Length > jackpotIndex)
		{
			celebrationObjects[jackpotIndex].SetActive(false);
		}
	}

// executeAfterPaylinesCallback() section
// functions in this section are accessed by SlotbaseGame/FreeSpinGame.doReelsStopped()
	public override bool needsToExecuteOnPaylinesPayoutRollup()
	{	
		return reelGame.engine.progressivesHit > reelGame.engine.progressiveThreshold;
	}


	public override void executeOnPaylinesPayoutRollup(bool winsShown, TICoroutine rollupRoutine)
	{
		int jackpotIndex = reelGame.engine.progressivesHit - reelGame.engine.progressiveThreshold - 1;

		StartCoroutine(moveJackpotToCenter(jackpotIndex));
		
		// Turn on celebration objects
		if (celebrationObjects != null && celebrationObjects.Length > jackpotIndex)
		{
			celebrationObjects[jackpotIndex].SetActive(true);
		}
	}

	private bool checkJackpotIndex(int jackpotIndex)
	{
		if (jackpotIndex < 0 || jackpotIndex > jackpotAnimators.Length)
		{
			Debug.LogError("Trying to display a jackpot that is out of range (" + jackpotIndex + ")");
			return false;
		}
		if (jackpotAnimators[jackpotIndex] == null)
		{
			Debug.LogError("Jackpot animator at " + jackpotIndex + " isn't defined.");
			return false;
		}
		return true;
	}

	protected IEnumerator moveJackpotToCenter(int jackpotIndex)
	{
		if (!checkJackpotIndex(jackpotIndex))
		{
			yield break;
		}
		if (shroudAnimator != null)
		{
			shroudAnimator.Play(shroudInAnimationName);
		}
		Audio.play(Audio.soundMap("jackpot_move_spinning_anim"));
		Animator jackpotAnimator = jackpotAnimators[jackpotIndex];
		winningJackpotStartingPosition = jackpotAnimator.transform.position;
		Vector3 newScale = new Vector3(jackpotAnimator.transform.localScale.x * jackpotScaleAmount.x,
		                                   jackpotAnimator.transform.localScale.y * jackpotScaleAmount.y,
		                                   jackpotAnimator.transform.localScale.z * jackpotScaleAmount.z);
		iTween.ScaleTo(jackpotAnimator.gameObject, newScale, TIME_MOVE_JACKPOT);
		yield return StartCoroutine(moveJackpotToPosition(jackpotIndex, JACKPOT_CENTER_POS));
		Debug.Log("play anim: " + JACKPOT_ANIMATION_PLAY_NAME);
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(jackpotAnimator, JACKPOT_ANIMATION_PLAY_NAME));
	}

	protected IEnumerator moveJackpotToPosition(int jackpotIndex, Vector3 position)
	{
		if (!checkJackpotIndex(jackpotIndex))
		{
			yield break;
		}
		jackpotDoneMoving = false;
		Animator jackpotAnimator = jackpotAnimators[jackpotIndex];
		iTween.MoveTo(jackpotAnimator.gameObject, position, TIME_MOVE_JACKPOT);
		yield return new TIWaitForSeconds(TIME_MOVE_JACKPOT);
		jackpotDoneMoving = true;

	}

}
