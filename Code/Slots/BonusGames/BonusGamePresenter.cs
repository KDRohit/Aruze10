using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

/*
 * Implementation of challenge games used within slot machines
 */
public class BonusGamePresenter : ChallengeGamePresenter
{
	
	public bool isHandlingReelOutcomesAfter = true; // Most bonus games trigger before other outcomes are shown, so we have to show them after the bonus ends, but sometimes a bonus might trigger after the other outcomes if so make this false
	[Tooltip("Controls if the BaseGame will hide when the BonusGamePresenter inits(), BonusGameManager may force this off if it goes through createBonusWithEntireGameTransition().")]
	public bool isHidingBaseGame = true;
	public bool manuallyRollingUpInBaseGame = false;
	
	[Tooltip("Set this flag in Editor or code if the BonusGameManager.instance.summaryScreenGameName should not be cleared when this BonusGamePresenter is cleaned up.  Needed when the name is set before or during a bonus that isn't going to show a summary screen.")]
	public bool isKeepingSummaryScreenGameName = false;
	protected int _autoSpins;
	
	// Used for Rome style games, where 2 payouts need to be combined since there are 2 bonus games.
	[HideInInspector] public static long portalPayout = 0;
	[HideInInspector] public static long secondBonusGamePayout = 0;

	// Used for London wheel payouts, where raising wheel slices, then entering a game, should increase values.
	[HideInInspector] public static long carryoverMultiplier = 0;

	[System.NonSerialized] public bool isReturningToBaseGameWhenDone = true;

	public string FREESPIN_SUMMARY_FANFARE = "freespin_summary_fanfare";

		
	public static BonusGamePresenter instance = null;

	/// Implements IResetGame
	new public static void resetStaticClassData()
	{
		
	}

	void Awake()
	{
		instance = this;

		// Set the name for a ChallengeGame if it was initialized before the BonusGamePresenter
		if (ChallengeGame.instance != null)
		{
			ChallengeGame.instance.setBonusGameName();
		}
	}

	void OnDestroy()
	{
		// TOFIX: can't enable this until addressing danging reference to BonusGamePresenter.instance at ReelGame.onPayoutRollup:
		// BonusGamePresenter.instance.currentPayout = (payoutValue + runningPayoutRollupValue);
		// instance = null;
	}
			
	public override void init(bool isCheckingReelGameCarryOverValue)
	{
		base.init(isCheckingReelGameCarryOverValue);
		
		// Very specific games have progressive payouts. Here, we're tagging ourself is we're a specific one that can have such a payout.
		if ((BonusGameManager.instance.currentGameType == BonusGameType.GIFTING && BonusGameManager.instance.currentGameKey.Contains("wow")) ||
			(BonusGameManager.instance.currentGameType == BonusGameType.CREDIT && BonusGameManager.instance.currentGameKey.Contains("oz")) ||
			BonusGameManager.instance.currentGameType == BonusGameType.CREDIT && BonusGameManager.instance.currentGameKey == "com01" ||
			BonusGameManager.instance.currentGameType == BonusGameType.CREDIT && BonusGameManager.instance.currentGameKey == "com02") 
		{
			if (BonusGameManager.instance.currentGameKey != "wow05")
			{
				isProgressive = true;
			}
		}
		
		// Cheeck for progressive
		if (BonusGamePresenter.instance.isProgressive)
		{
			if (isAutoPlayingInitMusic)
			{
				Audio.switchMusicKey(Audio.soundMap("progressive_idle"));
				Audio.stopMusic();
			}
		}
		
		// Due to the order that BonusGamePresenter.init() gets called before FreeSpinGame.init(),
		// FreeSpinGame.instance.bonusGamePresenter could not be set. So we need some additional logic
		// here to determine if this is actually a BonusGamePresenter for freespins
		bool isFreeSpinsBonus = false;
		if (FreeSpinGame.instance != null)
		{
			if (FreeSpinGame.instance.bonusGamePresenter == null)
			{
				// If the FreeSpinGame.instance is set but hasn't been init
				// yet we will assume that this is the presenter for that freespins.
				isFreeSpinsBonus = true;
			}
			else
			{
				// If it has been set then verify that it matches this presenter
				isFreeSpinsBonus = FreeSpinGame.instance.bonusGamePresenter == this;
			}
		}
		
		if (ChallengeGame.instance != null && BonusGameManager.instance.currentGameKey.Contains("lls"))
		{
			Audio.switchMusicKeyImmediate("");
		}
		else if (isFreeSpinsBonus)
		{
			setGameScreenForFreeSpins();
			
			if (isAutoPlayingInitMusic)
			{
				// Set freespin idle music - freespin mapped call will play when wheels actually spin in FreeSpinGame.cs
				string freespinIdleKey = Audio.soundMap("freespin_idle");
				if (freespinIdleKey != "waiting_bg")
				{
					Audio.switchMusicKey(freespinIdleKey); 
					if (Audio.currentMusicPlayer != null && 
						Audio.currentMusicPlayer.audioInfo != null && 
						freespinIdleKey != Audio.currentMusicPlayer.audioInfo.keyName)
					{
						Audio.stopMusic();
					}
				}
			}
		}
		else if (ChallengeGame.instance != null &&
		         ChallengeGame.instance.shouldAutoPlayBgMusic &&
		         !BonusGameManager.instance.currentGameKey.Contains("lls"))
		{
			if (isAutoPlayingInitMusic)
			{
				// Set bonus music
				Audio.switchMusicKey(Audio.soundMap("bonus_bg"));
				Audio.stopMusic();
			}
		}
		
		if (FreeSpinGame.instance == null)
		{
			// Turn off the ways/lines side info for non-free spins bonus games.
			if (SpinPanel.instance != null)
			{
				SpinPanel.instance.showSideInfo(false);
			}
		}

		// Some bonus games, like ones from features, will not want to check the reel game for a carry over value
		if (isCheckingReelGameCarryOverValue)
		{
			if (BonusGameManager.currentBaseGame != null)
			{
				if (isHidingBaseGame)
				{
					BonusGameManager.currentBaseGame.gameObject.SetActive(false);
				}

				foreach (SlotModule module in BonusGameManager.currentBaseGame.cachedAttachedSlotModules)
				{
					if (module.needsToGetCarryoverWinnings())
					{
						currentPayout = module.executeGetCarryoverWinnings();
					}
				}
			}
		}
	}
	/// set game screen position and world scale so paylines display properly during free spins
	public void setGameScreenForFreeSpins()
	{
		// For reels, set the bonus game's world position and scale at 0,0,0 and 1,1,1
		// so it works with paylines when in the NGUI hierarchy,
		// without messing up things that rely on NGUI.
		// The prefab should probably already be set up like this, but do it here just in case.
		gameScreen.transform.position = Vector3.zero;
		CommonTransform.setWorldScale(gameScreen.transform, Vector3.one);
		FreeSpinGame.instance.updateVerticalSpacingWorld();

		//Updating the background in case setting the position affected our centering
		if (FreeSpinGame.instance != null && FreeSpinGame.instance.reelGameBackground != null)
		{
			FreeSpinGame.instance.reelGameBackground.forceUpdate();
		}
	}


	protected override bool hasLightSet()
	{
		return (GameState.game != null && GameState.game.keyName == "bbh01") || base.hasLightSet();
	}

	/// The summary dialog has been closed.
	public override void summaryClosed()
	{
		// if (true)
		// {
		// 	// Show fake challenge results for testing/troubleshooting.
		// 	int i = Random.Range(0, FacebookMember.allFacebookFriends.Count);
		// 	ChallengeResultsDialog.showDialog(
		// 		Dict.create(
		// 			D.SCORE, Random.Range(300, 1000),
		// 			D.SCORE2, Random.Range(300, 1000),
		// 			D.BONUS_GAME, "oz00_challenge",
		// 			D.PLAYER, FacebookMember.allFacebookFriends[i],
		// 			D.CALLBACK, new DialogBase.AnswerDelegate((noArgs) => { executeModulesThenFinalCleanup(); })
		// 		)
		// 	);
		// }
		// else
		
		// Do not allow a progressive or high limit or VIP or sneak preview game to be gifted, head back to base game.
		if (BonusGamePresenter.instance.isProgressive ||
			GameState.game.isHighLimit ||
			GameState.game.isSneakPreview ||
			LobbyOption.activeGameOption(BonusGameManager.instance.currentGameKey) == null ||
			GameState.game.vipLevel != null)
		{
			executeModulesThenFinalCleanup();
		}
		else if (SlotsPlayer.isSocialFriendsEnabled 
			&& GameState.giftedBonus == null 
			&& BonusGameManager.instance.isGiftable 
			&& !string.IsNullOrEmpty(BonusGameManager.instance.summaryScreenGameName)
			&& MFSDialog.shouldSurfaceSendSpins())
		{
			// This bonus game was from a base game spin,
			// so offer the player the opportunity to gift it
			// or to challenge friends.
			if (BonusGameManager.instance.currentGameType == BonusGameType.GIFTING && !DevGUIMenuTools.disableFeatures && currentPayout > 0)
			{
				MFSDialog.showDialog(
					Dict.create(
						D.GAME_KEY, GameState.game.keyName,
						D.BONUS_GAME, BonusGameManager.instance.summaryScreenGameName,
						D.TYPE, MFSDialog.Mode.SPINS,
						D.CALLBACK, new DialogBase.AnswerDelegate((noArgs) => { executeModulesThenFinalCleanup(); })
					)
				);
			}
			else
			{
				executeModulesThenFinalCleanup();
			}
		}
		else
		{
			// This bonus game was not a challenge played from the inbox, or player is anonymous,
			// or no paytableSetId provided (maybe gifting is not allowed from this bonus game),
			// so just clean up. There is no gifting step.
			executeModulesThenFinalCleanup();
		}
	}
	
	/// Callback for the send challenge dialog.
	private void challengeDialogClosed(Dict args)
	{
		if ((string)args.getWithDefault(D.ANSWER, "") == "yes")
		{
			// GAME_KEY, BONUS_GAME, TYPE and SCORE are already in the args from the previous dialog.
			args.merge(D.CALLBACK, new DialogBase.AnswerDelegate((noArgs) => { executeModulesThenFinalCleanup(); }));
			MFSDialog.showDialog(args);
		}
		else
		{
			// Declined to challenge friends. What a putz.
			executeModulesThenFinalCleanup();
		}
	}

	private void fbConnectDialogClosed(Dict args)
	{
		executeModulesThenFinalCleanup();
	}
	
	// Made so we can end an intermediate game and not trigger outcome evaluation.
	public void endBonusGameImmediately()
	{
		// make sure portal games also turn the Dialog light back on
		if (Dialog.instance != null && hasLightSet())
		{
			Dialog.instance.keyLight.SetActive(true);
		}

		secondBonusGamePayout = 0;

		// Hide before we destroy to avoid NGUI ghost panel frames being rendered
		gameObject.SetActive(false);
		GameObject.Destroy(gameObject);

		// tell the bonus game manager we're done
		BonusGameManager.instance.bonusGameEnded();
	}

	// Execute modules either for ChallengeGame or ReelGame and then call the finalCleanup.
	protected override IEnumerator executeModulesThenFinalCleanupCoroutine()
	{
        if (ChallengeGame.instance != null)
        {
            yield return StartCoroutine(ChallengeGame.instance.handleNeedsToExecuteOnBonusGamePresenterFinalCleanupModules());
        }
        else
        {
            // assume this is a reel game
            if (FreeSpinGame.instance != null)
            {
                yield return StartCoroutine(FreeSpinGame.instance.handleNeedsToExecuteOnBonusGamePresenterFinalCleanupModules());
            }
            else
            {
                yield return StartCoroutine(ReelGame.activeGame.handleNeedsToExecuteOnBonusGamePresenterFinalCleanupModules());
            }
        }

        // now that the bonuses have had a chance to do stuff before we kill them, perform finalCleanup!
        finalCleanup();
	}

	public override void finalCleanup()
	{
		BonusGamePresenter.secondBonusGamePayout = 0;
		BonusGamePresenter.carryoverMultiplier = 0;
		BonusGameManager.instance.currentGameFinalPayout = 0;

		// Clear this variable which is used for sending gifts so that the next game can explicitly set it
		// ensuring we don't hold onto a previous game accidentally.
		// Double check if we should actualy clear it, some games like gen97 Cash Tower that have multi-part bonuses
		// don't actually want it cleared in some cases until a final summary screen is shown.
		if (!isKeepingSummaryScreenGameName)
		{
			BonusGameManager.instance.summaryScreenGameName = "";
		}

		// Make sure the overlay is visible.
		// but only if it should be, some modules like transitions might
		// want to enable it later, if this is a gift we need to enable it now
		if (GameState.giftedBonus != null || (ReelGame.activeGame != null && ReelGame.activeGame.isEnablingOverlayWhenBonusGameEnds()))
		{
			Overlay.instance.top.show(true);
		}
		
		if (FreeSpinGame.instance != null && FreeSpinGame.instance.bonusGamePresenter == this)
		{
			FreeSpinGame.instance.clearOutcomeDisplay();
			FreeSpinGame.instance.hasFreespinGameStarted = false;
		}
		
		if (BonusGameManager.instance.wings != null)
		{
			BonusGameManager.instance.wings.hide();
		}
		
		//Do base functions
		base.finalCleanup();
		
		if (!destroyOnEnd)
		{
			// check if we should null out the instance (only do this if the instance is still this game)
			if (BonusGamePresenter.instance != null && BonusGamePresenter.instance == this)
			{
				BonusGamePresenter.instance = null;
			}
		}
		
		if (SlotBaseGame.instance != null)
		{
            //Added challenge game condition as it is possible that we triggered a challenge game during free spins in base game.
			if (SlotBaseGame.instance.isDoingFreespinsInBasegame() && !BonusGameManager.instance.hasStackedBonusGames() && !SlotBaseGame.instance.outcome.isChallenge)
			{
				// if we were in freespins in base and are returning to the base game (i.e. not returning from a stacked bonus to the freespins) then restore that outcome before checking the queued bonus games
				SlotBaseGame.instance.restoreOutcomeFromBeforeFreespinsInBase();
				// I think this is a non-standard way of returning for freespins in base since usually it launches it's own summary,
				// so adding this here to make sure we correctly handle this variable in case this code is used
				SlotBaseGame.instance.hasFreespinGameStarted = false;
			}

			// if bonuses are queued up, then remove the current one we just finished, and check if we have another to load in to be played next
			if (SlotBaseGame.instance.outcome != null && SlotBaseGame.instance.outcome.hasQueuedBonuses)
			{
				SlotBaseGame.instance.outcome.removeBonusFromQueue();
				// check if we have another bonus after the one we just finished
				if (SlotBaseGame.instance.outcome.hasQueuedBonuses)
				{
					SlotBaseGame.instance.outcome.processNextBonusInQueue();
				}
			}
		}

		// tell the bonus game manager we're done
		BonusGameManager.instance.bonusGameEnded();
		
		if (isReturningToBaseGameWhenDone)
		{
			bool isDoingFreespinsInBase = SlotBaseGame.instance != null && SlotBaseGame.instance.isDoingFreespinsInBasegame();

			if (GameState.giftedBonus == null || isDoingFreespinsInBase)
			{
				// This wasn't a gifted bonus game, or it is, but we finished a bonus game triggered by free spins 
				// in base from a gift and aren't done with the gifted spins yet 
				// (NOTE: gifted spins from freespins in base doesn't use this code path, which is why it is safe to assume that this was a nested bonus in the freespins)
				
				// Check the base game to see if it wants the spin panel restored now, 
				// or if something like a transition module will restore it later.
				if (ReelGame.activeGame.isEnablingSpinPanelWhenBonusGameEnds())
				{
					// Show the normal spin panel
					// Need to make sure the spin panel is correctly restored to freespins if doing freespins in base
					if (isDoingFreespinsInBase)
					{
						SpinPanel.instance.gameObject.SetActive(true);
						SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
						SpinPanel.instance.showSideInfo(true);
					}
					else
					{
						
						SpinPanel.instance.gameObject.SetActive(true);
						SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
						SpinPanel.instance.showSideInfo(true);
					}

					// Need to handle the case where a Freespin uses one way of scoring (lines for instance) and the main slot base 
					// game uses a different one (clusters for instance), which occurs in the Splendor of Rome game.
					// So reset the ways to win when going back to the SlotBaseGame.
					if (FreeSpinGame.instance != null)
					{
						SlotBaseGame.instance.resetSpinPanelWaysToWin();
					}

					SpinPanel.instance.resetAutoSpinUI();
				}

				SlotBaseGame.instance.gameObject.SetActive(true);

				// call this before we show outcomes so we stop animations already going but don't screw up outcome animations
				if (SlotBaseGame.instance != null)
				{
					SlotBaseGame.instance.doSpecialOnBonusGameEnd();
				}

				// Do checks for queued bonus games which triggers the next bonus game instead of showing the base game outcomes, exclude freespins in base
				if (SlotBaseGame.instance != null)
				{
					bool isNextBonusFreespinsInBase = SlotBaseGame.instance.outcome.hasQueuedBonuses && SlotBaseGame.instance.playFreespinsInBasegame && SlotBaseGame.instance.outcome.peekAtNextQueuedBonusGame().isGifting;

					if (SlotBaseGame.instance.outcome.hasQueuedBonuses && !isNextBonusFreespinsInBase)
					{
						RoutineRunner.instance.StartCoroutine(SlotBaseGame.instance.rollupBonusWinBeforeStartingNextQueuedBonus());
						isHandlingReelOutcomesAfter = false;
					}
				}

				if (isHandlingReelOutcomesAfter)
				{
					// If there are other outcomes from the main spin, show them now.
					// This re-enables the UI buttons immediately if there are no other outcomes.

					bool letModuleShowOutcome = false;

					foreach (SlotModule module in ReelGame.activeGame.cachedAttachedSlotModules)
					{
						if (module.needsToLetModuleTransitionBeforePaylines())
						{
							letModuleShowOutcome = true;
						}
					}
					if (!letModuleShowOutcome)
					{
						SlotBaseGame.instance.doShowNonBonusOutcomes();
					}
				}
				else
				{
					// we need to tell any animated transition type modules to not handle the paylines afterwards, since they might still try to do it
					foreach (SlotModule module in ReelGame.activeGame.cachedAttachedSlotModules)
					{
						BonusGameAnimatedTransition transitionModule = module as BonusGameAnimatedTransition;
						if (transitionModule != null)
						{
							transitionModule.isSkippingShowNonBonusOutcomes = true;
						}
					}
				}
			}
			else
			{
				string giftedBonus = "";

				// Since we aren't going through rollup via showNonBonusOutcome() add the win value directly
				if (!manuallyRollingUpInBaseGame && GameState.giftedBonus == null)
				{
					if (SlotBaseGame.instance != null)
					{
						SlotBaseGame.instance.addCreditsToSlotsPlayer(BonusGameManager.instance.finalPayout, "bonus game payout", shouldPlayCreditsRollupSound: false);
					}
				}
				else if (GameState.giftedBonus != null)
				{
					giftedBonus = GameState.giftedBonus.slotsGameKey;
					SlotsPlayer.addCredits(BonusGameManager.instance.finalPayout, "gifted bonus game payout", false);
				}

				BonusGameManager.instance.finalPayout = 0;

				// This was a gifted bonus game, so no base game or spin panel exists to go back to.
				GameState.pop();
				
				if (GameState.game != null)
				{
					// If the player was in a game when launching this free bonus game,
					// re-load the game that the player was in before. It's still on the top of the stack.
					Glb.loadGame();
					Loading.show(Loading.LoadingTransactionTarget.GAME);
				}
				else
				{
					// Go back to the lobby.
					Glb.loadLobby();
					Loading.show(Loading.LoadingTransactionTarget.LOBBY);
				}

				// Re-open the inbox automatically after returning to wherever we're going. vip01 comes in as a gifted bonus, so don't reopen
				// the inbox for that. Note that vip01 will be removed in the new vip lobby VIPER in Q2 2020
				// TODO: Remove in VIPER

				bool inboxStillHasEliteFreeSpins = InboxInventory.findItemByCommand<InboxEliteFreeSpinsCommand>() != null;

				if
				(
					!string.IsNullOrEmpty(giftedBonus) &&
					giftedBonus != "vip01" &&
					giftedBonus != "max01" &&
					SlotsPlayer.instance.giftBonusAcceptLimit.amountRemaining > 0 &&
					(
						InboxInventory.getAmountOfType(InboxItem.InboxType.FREE_SPINS) > 0 ||
						inboxStillHasEliteFreeSpins
					)
				)
				{
					Scheduler.addTask(new InboxTask(Dict.create(D.KEY, inboxStillHasEliteFreeSpins ? InboxDialog.MESSAGES_STATE : InboxDialog.SPINS_STATE)));
				}

				//make sure symbols are stopped when you come back from playing a gift game if you were in a game
				if (SlotBaseGame.instance != null)
				{
					SlotBaseGame.instance.doSpecialOnBonusGameEnd();
				}

				Audio.stopMusic();
				Audio.switchMusicKey(Audio.soundMap("prespin_idle_loop"));
			}

			Bugsnag.LeaveBreadcrumb("Leaving a bonus game");
			
			//Tracking time in bonus game
			StatsManager.Instance.LogCount("timing", "BonusGameComplete", "", "", "", "", StatsManager.getTime(false));
		}
	}

	public override bool gameEnded()
	{
		return base.gameEnded();
	}
}
