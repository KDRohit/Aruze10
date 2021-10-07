using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

public class VIPTokenCollectionModule : TokenCollectionModule
{
	public const int MAX_TOKENS = 5;

	[SerializeField] private float waitToAddCoin = 4.5f;
	[SerializeField] private float coinShimmerStaggerTime = 0.25f;
	[SerializeField] private float coinShimmerMinIntervalTime = 5.0f;
	[SerializeField] private float coinShimmerMaxIntervalTime = 7.0f;

	[SerializeField] private LabelWrapperComponent miniJackpotLabel;
	[SerializeField] private LabelWrapperComponent majorJackpotLabel;
	[SerializeField] private LabelWrapperComponent vipJackpotLabel;

	[SerializeField] private ClickHandler legendButtonHandler;

	[SerializeField] private Animator meterSheenAnimator;
	[SerializeField] private List<Animator> tokenAnimators;

	private CoroutineRepeater playSheensCoroutineRepeater;

	private List<string> tokenEvents = new List<string>();

	static public long miniJpValue = 0L;
	static public long majorJpValue = 0L;
	static public long grandJpValue = 0L;
	static public long scatterWinnings = 0L;

	private int oldTokens = 0;

	private void Update()
	{
		if (playSheensCoroutineRepeater != null)
		{
			playSheensCoroutineRepeater.update();
		}
	}

	protected override void tokenWonEvent(JSON data)
	{
		string eventId = data.getString("event", "");

		if (!string.IsNullOrEmpty(eventId) && !tokenEvents.Contains(eventId))
		{
			tokenEvents.Add(eventId);

			base.tokenWonEvent(data);

			tokenWon = true;
			oldTokens = currentTokens;

			currentTokens = data.getInt("tokens_collected", 0);

			if (oldTokens > currentTokens)
			{
				oldTokens = 0;
			}

			SlotsPlayer.instance.vipTokensCollected = currentTokens;
			StatsManager.Instance.LogCount("vip_token", "", GameState.game.keyName, "", "play_now", currentTokens.ToString());
		}
	}

	public override void startTokenCelebration()
	{
		if(!Scheduler.hasTaskWith("vip_celebration_dialog"))
		{
			VIPTokenCelebrationDialog.showDialog(Dict.create(D.PRIORITY, SchedulerPriority.PriorityType.IMMEDIATE));
		}
	}

	public override IEnumerator waitThenHoldToken()
	{
		Overlay.instance.setButtons(false);
		yield return new TIWaitForSeconds(waitToAddCoin);
		coinsControllerAnimator.Play("AddCoinHold");
		VIPTokenCelebrationDialog.closeDialog();
	}

	private void onMeterClicked(Dict args = null)
	{
		if (!barIsAnimating) //Prevent spamming the meter from opening and closing
		{
			StartCoroutine(slideMeter());
		}
	}

	protected override void startGameAfterLoadingScreen ()
	{
		base.startGameAfterLoadingScreen ();
		VIPLoadingDialog.showDialog();
	}

	private IEnumerator slideMeter()
	{
		barIsAnimating = true;
		if (showingFullBar) //Close the full bar and show coins only
		{
			if (tokenMeterAnimator.HasState(0, Animator.StringToHash("FulltoTokensOnly")))
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(tokenMeterAnimator, "FulltoTokensOnly"));
				showingFullBar = false;
			}
		}
		else //Expand the bar to show everything
		{
			if (tokenMeterAnimator.HasState(0, Animator.StringToHash("TokensToFull")))
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(tokenMeterAnimator, "TokensToFull"));
				showingFullBar = true;
			}
		}
		barIsAnimating = false;
	}

	public override void showToolTip()
	{
		if (!toolTipShowing)
		{
			toolTipShowing = true;
			tooltipAnimator.Play("ToolTipIn");
		}
	}

	public override void spinPressed()
	{
		if (toolTipShowing)
		{
			tooltipAnimator.Play("ToolTipOut");
			toolTipShowing = false;
		}
		if (showingFullBar)
		{
			StartCoroutine(slideMeter());
		}
	}

	public override IEnumerator addTokenAfterCelebration()
	{
		if (Overlay.instance != null)
		{
			Overlay.instance.setButtons(false);
		}

		if (tokenGrantAnimations != null && tokenGrantAnimations.animInfoList != null)
		{
			int delta = currentTokens - oldTokens;
			if (delta > 0)
			{
				for (int i = oldTokens + 1; i <= currentTokens; i++)
				{
					tokenGrantAnimations.animInfoList[0].ANIMATION_NAME = "AddCoin" + i;
					yield return StartCoroutine(
						AnimationListController.playListOfAnimationInformation(tokenGrantAnimations));
				}
			}
		}

		if (tokenJSON != null && tokenJSON.hasKey("mini_game_outcome"))
		{
			if (miniGameWonTransitionAnimations != null)
			{
				Audio.switchMusicKeyImmediate("VIPLastTokenWelcomeMiniGame");
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(miniGameWonTransitionAnimations));
			}

			oldTokens = 0;
			needsToRollupBonusGame = true;
			eventId = tokenJSON.getString("event", "");
			bonusGameOutcomeJson = tokenJSON.getJSON("mini_game_outcome");
			
			// Cancel autospins if the game is doing those. This prevents
			// additional spins from triggering while we wait for the bonus
			// to load.
			if (SlotBaseGame.instance != null)
			{
				SlotBaseGame.instance.stopAutoSpin();
			}
			
			startGameAfterLoadingScreen();
		}

		waitingToAddToken = false;
		tokenJSON = null;
		tokenWon = false;
	}

	private IEnumerator playTokenSheens()
	{
		GameObject tokenBarAnchor = null;
		if (Overlay.instance != null && Overlay.instance.jackpotMysteryHIR != null)
		{
			tokenBarAnchor = Overlay.instance.jackpotMysteryHIR.tokenAnchor;
		}

		if (tokenBarAnchor == null)
		{
			yield break;
		}

		int numberOfPossibleTokens = tokenAnimators.Count;

		if (meterSheenAnimator.isActiveAndEnabled)
		{
			meterSheenAnimator.Play("topMeter");
		}

		for (int i = 0; i < currentTokens; i++)
		{
			if (numberOfPossibleTokens >= i && tokenAnimators != null && tokenBarAnchor.activeSelf) //Need to keep checking the anchor in case the user left the game in the middle of this coroutine 
			{
				if (tokenAnimators[i].isActiveAndEnabled)
				{
					tokenAnimators[i].Play("coinGlint");
					yield return new TIWaitForSeconds(coinShimmerStaggerTime);
				}
			}
		}
	}


	public override IEnumerator handleWinnings()
	{
		long totalWinnings = miniJpValue + majorJpValue + grandJpValue + scatterWinnings;
		SlotsPlayer.addCredits(totalWinnings, "bonus game payout", false); //Add the scatter credits to the player
		Overlay.instance.top.updateCredits(false); //Rollup the top overlay to the new player amount

		// user is never going to see this rollup happen
		if (GameState.game.isBuiltInProgressive)
		{
			ReelGame.activeGame.onPayoutRollup(totalWinnings);
		}
		else
		{
			RoutineRunner.instance.StartCoroutine(SlotUtils.rollup(0, totalWinnings, ReelGame.activeGame.onPayoutRollup, true));
		}
		needsToRollupBonusGame = false;
		miniJpValue = 0L;
		majorJpValue = 0L;
		grandJpValue = 0L;
		scatterWinnings = 0L;
		yield break;
	}

	public override void betChanged (bool isIncreasingBet)
	{
		showToolTip();
	}

	public override void setupBar()
	{
		currentTokens = SlotsPlayer.instance.vipTokensCollected;
		base.setupBar();
		legendButtonHandler.registerEventDelegate(onMeterClicked);

		// MCC -- adding a nullcheck to these since we shouldn't NRE the whole game if the jackpots aren't working.
		if (ProgressiveJackpot.vipRevampMini != null)
		{
			ProgressiveJackpot.vipRevampMini.registerLabel(miniJackpotLabel.labelWrapper);
		}

		if (ProgressiveJackpot.vipRevampMajor != null)
		{
			ProgressiveJackpot.vipRevampMajor.registerLabel(majorJackpotLabel.labelWrapper);
		}

		if (ProgressiveJackpot.vipRevampGrand != null)
		{
			ProgressiveJackpot.vipRevampGrand.registerLabel(vipJackpotLabel.labelWrapper);
		}
		
		currentTokens = SlotsPlayer.instance.vipTokensCollected;
		showingFullBar = true;
		playSheensCoroutineRepeater = new CoroutineRepeater(coinShimmerMinIntervalTime, coinShimmerMaxIntervalTime, playTokenSheens);
	}

	// Cleanup
    new	public static void resetStaticClassData()
	{
		miniJpValue = 0L;
		majorJpValue = 0L;
		grandJpValue = 0L;
		scatterWinnings = 0L;
		needsToRollupBonusGame = false;
	}
}
