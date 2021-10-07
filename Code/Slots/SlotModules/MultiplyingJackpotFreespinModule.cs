using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Module for handling a game where you have a jackpot that can be multiplied based on hitting symbols on the reels (First used in twilight01)
Similar to WordGameFreespinsModule but made to use a mutation type that is not "word_freespin".
Original Author: Stephen Arredondo
*/
public class MultiplyingJackpotFreespinModule : WordGameFreespinsModule
{
	private bool isFirstSpin = true;
	private StandardMutation mutationToCheck = null;

	//Wait for the banner to be masked by jackpot animation before swapping banners
	[SerializeField] private float bannerSwapDelay = 0.0f;
	//Incrementing Banners on Jackpot Win
	[SerializeField] private List<GameObject> jackpotBanners = new List<GameObject>();
	private int bannerIndex = 0;

	public override bool needsToExecutePreReelsStopSpinning()
	{
		// only need to handle pre spin for the first spin to setup the first word and letter score data
		return isFirstSpin;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		// Check for the initial WordMutation so we can get the letter score values and setup the first word
		foreach (MutationBase mutation in reelGame.mutationManager.mutations)
		{
			if (mutation.type == alternateMutationType)
			{
				isFirstSpin = false;
				StandardMutation startMutation = mutation as StandardMutation;
				Audio.play(SHOW_JACKPOT_VALUE_SOUND);
				jackpotLabelText.text = CreditsEconomy.convertCredits(startMutation.initialJackpotValue * gameMultiplier);
			}
		}
		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		mutationToCheck = null; //Null this out before the next spin starts
		foreach (MutationBase mutation in reelGame.mutationManager.mutations)
		{
			if (mutation.type == alternateMutationType)
			{
				mutationToCheck = mutation as StandardMutation;
				//Only return true if we need to payout or multiply the jackpot
				return true;
			}
		}

		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
		{
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				if (symbol.serverName == DOUBLE_WORD_SYMBOL_NAME || symbol.serverName == TRIPLE_WORD_SYMBOL_NAME)
				{
					if (mutationToCheck.creditsMultiplier > 0)
					{
						yield return StartCoroutine(doWordMultiplierEffect(reel, symbol, null, mutationToCheck));
					}
					else
					{
						bool mutationDataFound = false;
						foreach (JSON mutation in reelGame.outcome.getMutations())
						{
							if (mutation.getString("type", "") == alternateMutationType)
							{
								string multiplier = mutation.getString("jackpot_multiplier", "");
								if (multiplier != "")
								{
									mutationDataFound = true;
									yield return StartCoroutine(doWordMultiplierEffect(reel, symbol, null, mutationToCheck));
								}
							}
						}
						if (!mutationDataFound)
						{
							Debug.LogWarning("Couldn't find matching WordMultiplier mutation data for symbol: " + symbol.serverName);
						}
					}
				}
				else if (symbol.serverName == PLAY_WORD_SYMBOL_NAME)
				{
					if (mutationToCheck.creditsAwarded > 0)
					{
						StartCoroutine(swapBanner());
						yield return StartCoroutine(doWordPayoutEffect(reel, symbol, null, mutationToCheck, customSCFlyToTransform));						
					}
				}
			}
		}
	}

	private IEnumerator swapBanner()
	{
		yield return new TIWaitForSeconds(bannerSwapDelay);
		//Do we have banners to swap?
		if (jackpotBanners.Count > 0)
		{
			//Turn off current banner
			jackpotBanners[bannerIndex].SetActive(false);
			//Next banner
			bannerIndex++;
			//if we reached the last banner start at the beginning
			if (bannerIndex == jackpotBanners.Count)
			{
				bannerIndex = 0;
			}
			//Activate the next banner
			jackpotBanners[bannerIndex].SetActive(true);
		}
	}

	protected override long calculateCurrentJackpotValue ()
	{
		return mutationToCheck.currentJackpotValue * gameMultiplier;
	}

}
