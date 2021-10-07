using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ReelGameBonusPoolsModule : SlotModule {

	[SerializeField] private GameObject bonusParent;							// The parent of all the objects that are part of what flies down to the win meter after winning a multiplier in the bonus feature
	[SerializeField] private GameObject bonusCharacter;							// The game object for the item that flies to the win meter after winning a multiplier in the bonus feature
	[SerializeField] private UISprite bonusSprite;								// Sprite object, if used, for the sprite used for the object that flies to the win meter after winning a multiplier in the bonus feature
	[SerializeField] private GameObject[] bonusMultiplierSprites;				// Numeric objects shown as part of the bonusSprite object which flies to the win meter after the bonus feature
	[SerializeField] private GameObject meterPopObject;							// Effect to trigger when the bonusSprite arrives at the meter, and plays while rolling up, fairly sure this isn't working at the moment
	[SerializeField] private float meterPopDuration;
	[SerializeField] private Animator meterPopAnimator;
	[SerializeField] private string meterPopAnimationName;

	public BaseBonusPoolsComponent bonusPoolComponent;		// Component on bonus pools object.
	public Camera bonusPoolsCamera;							// As of right now, this is only used for TRAMP.

	private int buttonChoice = 0;

	// Sound keys for the multiplier travel to amount box.
	private const string SCATTER_MULTIPLIER_TRAVEL_SOUND_KEY = "scatter_multiplier_travel";
	private const string SCATTER_MULTIPLIER_ARRIVE_SOUND_KEY = "scatter_multiplier_arrive";
	private const string SCATTER_MULTIPLIER_ARRIVE_VO_SOUND_KEY = "scatter_multiplier_arrive_vo";
	[SerializeField] private float SCATTER_MULTIPLIER_ARRIVE_VO_SOUND_DELAY = 0.0f;

	[SerializeField] private bool hasWildMutation = false;
	[SerializeField] private GameObject wildMutationGameObject = null;
	[SerializeField] private float wildMutationWaitTime = 0.4f;
	[SerializeField] private float wildMutationDestroyTime = 1.0f;
	[SerializeField] private string wildMutationSoundKey = "basegame_vertical_wild_reveal";

	private bool hasInitialRollupCompleted;
	private Dictionary<int,Dictionary<int,bool>> wildSymbols;

	public override void Awake()
	{
		base.Awake();
		reelGame.outcomeDisplayController.setBonusPoolCoroutine(bonusAfterRollup);
		wildSymbols = new Dictionary<int, Dictionary<int,bool>>();
	}

	public override bool needsToExecuteOnReelsSpinning()
	{
		return true;
	}

	public override IEnumerator executeOnReelsSpinning()
	{

		hasInitialRollupCompleted = false;
		wildSymbols.Clear();
		yield break;
	}

	/// So some other stuff after the normal rollup finishes, but before telling the game that the rollup is done.
	private IEnumerator bonusAfterRollup(JSON bonusPoolsJson, long basePayout, long bonusPayout, RollupDelegate rollupDelegate, bool doBigWin, BigWinDelegate bigWinDelegate)
	{
		hasInitialRollupCompleted = true;
		
		// First need to find out what the key name of the bonus_pool is.
		string bonusPoolKey = bonusPoolsJson.getKeyList()[0];	// Only expecting one bonus pool, so use index 0.

		// Now that we know the key name, get the JSON object and the BonusPool global data object.
		JSON bonusPoolJson = bonusPoolsJson.getJSON(bonusPoolKey);
		BonusPool bonusPool = BonusPool.find(bonusPoolKey);

		if (bonusPool != null)
		{
			// Show the choices.

			// This is so bad... Even though there's only one pick, it's in an array of convoluted formatting.
			// Actual example data, where "4" is the key name we're after.
			// "picks": [
			// 	{
			// 		"4": "bonus_multiplier"
			// 	}
			// ],
			BonusPoolItem pick = bonusPool.findItem(bonusPoolJson.getJsonArray("picks")[0].getKeyList()[0]);

			// There are multiple reveals, but ironically not set up in an array.
			// Actual example data, where "2" and "elvira_symbol_replace" are the key names we're after.
			// The "empty" entry is ignored, and shouldn't be in there but is. Don't ask me.
			// "reveals": {
			// 	"2": "bonus_multiplier",
			// 	"elvira_symbol_replace": "reevaluation",
			// 	"empty": "empty"
			// }
			List<string> revealKeys = bonusPoolJson.getJSON("reveals").getKeyList();
			revealKeys.Remove("empty");	// Don't use empty.

			List<BonusPoolItem> reveals = new List<BonusPoolItem>();

			foreach (string key in revealKeys)
			{
				reveals.Add(bonusPool.findItem(key));
			}

			JSON reevalJson = null;
			JSON cellReeval = null;
			string fromSymbol = "";

			if (pick.reevaluations != null)
			{
				// Need to change some symbols to wilds and reevaluate the payout.
				reevalJson = reelGame.outcome.getReevaluations();
				cellReeval = reevalJson.getJSON("cell_reevaluation");
				fromSymbol = reevalJson.getString("from_symbol", "");
			}
			else
			{

				// If not reevaluating wilds, we need to pick a random symbol as a fake wild.
				BonusPoolItem reevalItem = bonusPool.reevaluationItem;

				List<string> possible = new List<string>();

				foreach (SlotReel reel in reelGame.engine.getReelArray())
				{
					foreach (SlotSymbol symbol in reel.visibleSymbols)
					{
						// We want to use the short server name for exclusion checks since the excludeSymbols values come from server and only include short names
						if (!possible.Contains(symbol.name) && !reevalItem.reevaluations.excludeSymbols.Contains(symbol.shortServerName))
						{
							// Only use symbol names that aren't extra rows in a multi-row symbol.
							bool shouldUse = true;
							if (symbol.isTallSymbolPart)
							{
								int symbolRow = symbol.getRow();

								if (symbolRow != 1)
								{
									shouldUse = false;
								}
							}

							if (shouldUse)
							{
								possible.Add(symbol.name);
							}
						}
					}
				}

				fromSymbol = possible[Random.Range(0, possible.Count)];
			}

			reelGame.outcomeDisplayController.clearOutcome();

			if (bonusPoolComponent != null)
			{
				bonusPoolComponent.gameObject.SetActive(true);
				yield return StartCoroutine(bonusPoolComponent.playBonus(pick, reveals, fromSymbol));
				buttonChoice = bonusPoolComponent.userChoice;
			}

			if (reevalJson != null)
			{
				// setup the rollup to continue from where it was at before the bonus pool alters the symbols on the reels to turn some wild
				long amountAlreadyAwarded = basePayout + bonusPayout;
				reelGame.setRunningPayoutRollupValue(amountAlreadyAwarded);

				bool wildMutationAudioPlayed = false;

				// Overlay the wild graphic on the specified symbols.
				SlotReel[] reelArray = reelGame.engine.getReelArray();

				for (int i = 0; i < reelArray.Length; i++)
				{
					int reelId = i + 1;
					JSON reelJson = cellReeval.getJSON(reelId.ToString());
					if (reelJson != null)
					{
						// There is at least one symbol to change in this reel.
						SlotReel reel = reelArray[i];
						List<SlotSymbol> symbols = reel.visibleSymbolsBottomUp;
						// If we have a symbol that is multi-row, but the top of the symbol isn't visible then we need to make sure we make it WD.
						string tallSymbolToMakeWild = "";
						for (int j = 0; j < symbols.Count; j++)
						{
							int symbolIndex = j + 1;
							string cellSymbol = reelJson.getString(symbolIndex.ToString(), "");

							if (cellSymbol != "")
							{
								// track the wild symbols
								addToWildSymbols(reelId, symbolIndex);
								
								if (symbols[j].animator != null)
								{
									if (hasWildMutation)
									{
										// We should only play this audio if there's a delay between wild mutations.
										if (wildMutationWaitTime > 0)
										{
											Audio.tryToPlaySoundMap(wildMutationSoundKey);
										}
										// If there is no delay, only play the audio once and never again. 
										else if (!wildMutationAudioPlayed)
										{
											Audio.tryToPlaySoundMap(wildMutationSoundKey);
											wildMutationAudioPlayed = true;
										}

										StartCoroutine(attachWDMutationEffectThenDestroy(symbols[j]));
										yield return new TIWaitForSeconds(wildMutationWaitTime);
									}
									else
									{									
										// Secondary rows on a multi-row symbol will not have an animator, which is ok.
										// We just need to keep track of it so we can make it get the wd overlay.
										symbols[j].showWild();
									}
									tallSymbolToMakeWild = "";
								}
								else
								{
									tallSymbolToMakeWild = symbols[j].name;
								}
							}
						}
						// Go outside of the visible symbols so that tall symbols can show the wilds that they are supposed to.
						if (tallSymbolToMakeWild != "")
						{
							tallSymbolToMakeWild = tallSymbolToMakeWild.Substring(0, tallSymbolToMakeWild.Length - 1) + "A";
							symbols = reel.symbolList;
							foreach (SlotSymbol symbol in reel.symbolList)
							{
								if (symbol.name == tallSymbolToMakeWild && symbol.animator != null)
								{
									if (hasWildMutation)
									{
										Audio.tryToPlaySoundMap(wildMutationSoundKey);
										StartCoroutine(attachWDMutationEffectThenDestroy(symbol));
										yield return new TIWaitForSeconds(wildMutationWaitTime);
									}
									else
									{
										symbol.showWild();
									}
								}
							}
						}
					}
				}

				yield return new TIWaitForSeconds(1.0f);

				// Show new paylines and roll up the new winnings.
				// The new winnings are in addition to the base payout,
				// even though some reevaluated paylines are the same as the original payout.
				reelGame.setOutcomeNoExtraProcessing(new SlotOutcome(reevalJson));
				bool autoSpinMode = reelGame.hasAutoSpinsRemaining;
				reelGame.outcomeDisplayController.clearOutcome();
				long secondaryPayout = reelGame.outcomeDisplayController.displayOutcome(reelGame.outcome, autoSpinMode);

				// Wait for the reevaluation rollup to start.
				// This is necessary because previous outcome paylines need
				// to fade and the new paylines need to appear
				// before the new rollup starts, and we need to wait
				// until the rollup starts before we show the big win,
				// otherwise it sits at 0 until the rollup starts (a couple of seconds).
				while (reelGame.outcomeDisplayController.rollupsRunning.Count == 1)
				{
					yield return null;
				}

				// Wait for the reevaluation rollup to finish, which is rollup 2 (index 1).
				while (reelGame.outcomeDisplayController.rollupsRunning[1])
				{
					yield return null;
				}

				// We're finally done with this whole outcome.
				yield return StartCoroutine(reelGame.outcomeDisplayController.finalizeRollup());
			}
			else
			{
				// Need to multiply the current payout by a certain amount.
				// If we had free spins, we don't include the free spins amount in the multiplied payout - only the base payout is multiplied.
				// Make a multiplier character float to the current payout amount before rolling it up more.
				float meterPopStartTime = 0;

				bonusParent.transform.parent = SpinPanel.instance.transform;
				bonusParent.transform.localScale = Vector3.one;
				bonusCharacter.transform.localScale = Vector3.one;
				bonusParent.transform.localPosition = new Vector3((buttonChoice - 1) * 574 + 209, 0, -20);	// It has its own UIPanel, so position it in front of the spin panel.
				bonusCharacter.SetActive(true);

				if (meterPopObject != null)
				{
					// Make sure this is hidden until ready to show.
					meterPopObject.SetActive(false);
				}

				if (bonusSprite != null)
				{
					// if this game is using NGUI sprites we are just going to swap the sprite to show the right multiplier number
					bonusSprite.spriteName = string.Format("{0}x_m", pick.multiplier);
					bonusSprite.MakePixelPerfect();
				}
				else if (bonusMultiplierSprites != null)
				{
					// this game isn't using NGUI sprites, so instead we are going to turn on an object which displays the right multiplier number
					if (pick.multiplier < 4)
					{
						bonusMultiplierSprites[0].SetActive(true);
						bonusMultiplierSprites[1].SetActive(false);
					}
					else
					{
						bonusMultiplierSprites[0].SetActive(false);
						bonusMultiplierSprites[1].SetActive(true);
					}
				}

				// Calculate the local coordinates of the destination, which is where "winningsAmountLabel" is positioned relative to the spin panel.
				Vector2 destination = NGUIExt.localPositionOfPosition(SpinPanel.instance.transform, SpinPanel.instance.winningsAmountLabel.transform.position);

				float duration = 1f;

				iTween.MoveTo(bonusParent, iTween.Hash("x", destination.x, "y", destination.y, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear));
				iTween.ScaleTo(bonusCharacter, iTween.Hash("scale", Vector3.one * .5f, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear));

				// Play the correct audio for multiplier travel.
				Audio.tryToPlaySoundMap(SCATTER_MULTIPLIER_TRAVEL_SOUND_KEY);

				yield return new WaitForSeconds(duration);

				yield return null;	// Yield one more frame to make sure iTweens are done before deactivating the ghost.
				bonusCharacter.SetActive(false);

				Audio.tryToPlaySoundMap(SCATTER_MULTIPLIER_ARRIVE_SOUND_KEY);
				Audio.tryToPlaySoundMapWithDelay(SCATTER_MULTIPLIER_ARRIVE_VO_SOUND_KEY, SCATTER_MULTIPLIER_ARRIVE_VO_SOUND_DELAY);

				if (meterPopObject != null)
				{
					// Show the meter pop, and start all of its emitters and animations.
					ParticleSystem[] emitters = meterPopObject.GetComponentsInChildren<ParticleSystem>(true);
					Animation[] animations = meterPopObject.GetComponentsInChildren<Animation>(true);

					meterPopObject.SetActive(true);

					foreach (ParticleSystem emitter in emitters)
					{
						emitter.Play();
					}

					foreach (Animation anim in animations)
					{
						anim.Play(anim.clip.name);
					}
						
					if (meterPopAnimator != null)
					{
						
						if (!string.IsNullOrEmpty(meterPopAnimationName))
						{

							meterPopAnimator.Play(meterPopAnimationName);

						}
					}

					meterPopStartTime = Time.realtimeSinceStartup;
				}

				// Do the extra rollup.
				long previousPayout = basePayout + bonusPayout;
				long newPayout = basePayout * pick.multiplier + bonusPayout;

				if (doBigWin || (newPayout > Glb.BIG_WIN_THRESHOLD * SpinPanel.instance.betAmount))
				{
					// We need to handle the big win calls ourselves.
					bigWinDelegate(newPayout, false);
				}

				reelGame.addCreditsToSlotsPlayer(newPayout - previousPayout, "bonus pools payout");
				yield return StartCoroutine(reelGame.rollupCredits(previousPayout, 
					newPayout, 
					rollupDelegate, 
					isPlayingRollupSounds: true,
					specificRollupTime: 0.0f, 
					shouldSkipOnTouch:true, 
					allowBigWin:false));

				if (meterPopObject != null)
				{
					// Make sure enough time has elapsed since showing the meterpop before hiding it.
					float elapsedTime = Time.realtimeSinceStartup - meterPopStartTime;
					float waitTime = meterPopDuration - elapsedTime;
					if (waitTime > 0.0f)
					{
						yield return new WaitForSeconds(waitTime);
					}

					meterPopObject.SetActive(false);
				}

				bonusParent.transform.parent = transform;
			}
		}


		if (bonusPoolComponent != null)
		{
			bonusPoolComponent.gameObject.SetActive(false);
		}
	}

	// Keep a Dictionary of the symbols that are wild from getting
	// a cell_reevaluation in the outcome so we can add effects
	// to them if a large symbol gets split.
	private void addToWildSymbols(int reelId, int symbolIndex)
	{
		if (!wildSymbols.ContainsKey(reelId))
		{
			wildSymbols.Add(reelId, new Dictionary<int, bool>() { { symbolIndex, true } });
		}
		else if (!wildSymbols[reelId].ContainsKey(symbolIndex))
		{
			wildSymbols[reelId].Add(symbolIndex, true);
		}
	}

	// For some bonus pools games we have a game object to attach for a bit before enabling the wild overlay
	private IEnumerator attachWDMutationEffectThenDestroy(SlotSymbol symbol)
	{
		if (wildMutationGameObject != null)
		{
			GameObject mutationObject = CommonGameObject.instantiate(wildMutationGameObject) as GameObject;
			mutationObject.transform.position = symbol.transform.position;
			mutationObject.SetActive(true);
			yield return new TIWaitForSeconds(wildMutationDestroyTime);
			Destroy(mutationObject);
		}
		else if (wildMutationDestroyTime > 0)
		{
			// still we can wait
			yield return new TIWaitForSeconds(wildMutationDestroyTime);
		}
		
		symbol.showWild();
	}

	public override bool needsToExecuteAfterSymbolSplit()
	{
		return wildSymbols != null;
	}

	// make sure this symbol that was just split was one of the wilds that needs
	// the wild overlay effect on it.
	public override void executeAfterSymbolSplit(SlotSymbol splittableSymbol)
	{

		if (
			wildSymbols != null &&
			wildSymbols.ContainsKey(splittableSymbol.reel.reelID) &&
			wildSymbols[splittableSymbol.reel.reelID].ContainsKey(splittableSymbol.visibleSymbolIndexBottomUp) &&
			wildSymbols[splittableSymbol.reel.reelID][splittableSymbol.visibleSymbolIndexBottomUp]
			)
		{
			StartCoroutine(attachWDMutationEffectThenDestroy(splittableSymbol));
		}
	}

	// controls if the big win should be delayed
	public override bool isModuleHandlingBigWin()
	{
		bool hasBonusPools = reelGame.outcome.getBonusPools() != null;
		return hasBonusPools && !hasInitialRollupCompleted;
	}
}
