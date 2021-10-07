using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TokenCollectionModule : TICoroutineMonoBehaviour, IResetGame
{
	[System.NonSerialized] public bool tokenWon = false;
	private bool initialSetUpComplete = false;

	[SerializeField] public string tokenServerEventName = "";
	[SerializeField] public bool hasDialogCelebration = false;
	[SerializeField] protected Animator tokenMeterAnimator;
	[SerializeField] protected Animator tooltipAnimator;
	[SerializeField] protected Animator coinsControllerAnimator;

	[SerializeField] protected AnimationListController.AnimationInformationList tokenGrantAnimations;
	[SerializeField] protected AnimationListController.AnimationInformationList miniGameWonTransitionAnimations;
	[SerializeField] private string bonusGameKeyName = "";

	[SerializeField] protected string addCoinAnimationName = "AddCoin";
	[SerializeField] private string holdCoinsAnimationPrefix = "Have";
	[SerializeField] private string holdCoinsAnimationPostfix = "Coins";
	[SerializeField] protected string transitionSoundName = "";
	[SerializeField] private BoxCollider2D reelsBoundsLimit;

	protected static bool needsToRollupBonusGame = false;
	protected string eventId;
	protected string bonusType;
	protected string bonusGameName;
	protected JSON bonusGameOutcomeJson;

	protected bool toolTipShowing = false;
	protected bool showingFullBar = true;
	protected bool barIsAnimating = false;
	protected bool waitingToAddToken = false;

	protected int currentTokens = 0;

	protected JSON tokenJSON = null;

	void OnEnable()
	{
		// if meter gets disabled and reenabled, want to ensure correct animation state
		if (currentTokens > 0 && coinsControllerAnimator != null)
		{
			coinsControllerAnimator.Play(holdCoinsAnimationPrefix + currentTokens + holdCoinsAnimationPostfix);
		}
	}

	public virtual void startTokenCelebration()
	{

	}

	public virtual void spinPressed()
	{

	}

	public virtual void spinHeld()
	{
		
	}

	public virtual void showToolTip()
	{

	}

	public virtual IEnumerator handleWinnings()
	{
		yield break;
	}

	public virtual IEnumerator slotStarted()
	{
		yield break;
	}

	public virtual IEnumerator addTokenAfterCelebration()
	{
		if (Overlay.instance != null)
		{
			Overlay.instance.setButtons(false);
		}

		if (tokenGrantAnimations != null && tokenGrantAnimations.animInfoList != null)
		{
			tokenGrantAnimations.animInfoList[0].ANIMATION_NAME = addCoinAnimationName + currentTokens;
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(tokenGrantAnimations)); 
		}

		if (currentTokens == 5 && miniGameWonTransitionAnimations != null)
		{
			Audio.switchMusicKeyImmediate(transitionSoundName);
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(miniGameWonTransitionAnimations)); 
		}
		if (tokenJSON != null && tokenJSON.hasKey("mini_game_outcome"))
		{
			needsToRollupBonusGame = true;
			eventId = tokenJSON.getString("event", "");
			bonusGameOutcomeJson = tokenJSON.getJSON("mini_game_outcome");
			if (bonusGameOutcomeJson != null)
			{
				// Cancel autospins if the game is doing those. This prevents
				// additional spins from triggering while we wait for the bonus
				// to load.
				if (SlotBaseGame.instance != null)
				{
					SlotBaseGame.instance.stopAutoSpin();
				}

				startGameAfterLoadingScreen();
			}
		}

		waitingToAddToken = false;
		tokenJSON = null;
		tokenWon = false;
	}


	protected virtual void startGameAfterLoadingScreen()
	{
		bonusGameName = bonusGameOutcomeJson.getString("bonus_game", "");
		bonusType = "";
		if (bonusGameName.Contains("freespin"))
		{
			bonusType = "gifting";
		}
		else
		{
			bonusType = "challenge";
		}
	}

	public virtual IEnumerator setTokenState()
	{
		yield return null;
		if (SlotsPlayer.instance.vipTokensCollected > 0)
		{
			if (tokenWon)
			{
				coinsControllerAnimator.Play(holdCoinsAnimationPrefix + (SlotsPlayer.instance.vipTokensCollected-1) + holdCoinsAnimationPostfix);
			}
			else
			{
				coinsControllerAnimator.Play(holdCoinsAnimationPrefix + (SlotsPlayer.instance.vipTokensCollected) + holdCoinsAnimationPostfix);
			}
		}
		tokenMeterAnimator.Play("HoldTokensOnly");
		showingFullBar = false;
	}

	public virtual IEnumerator waitThenHoldToken()
	{
		yield break;
	}

	public virtual void setupBar()
	{
		Server.registerEventDelegate (tokenServerEventName, tokenWonEvent, true);
		if (currentTokens > 0 && coinsControllerAnimator != null)
		{
			coinsControllerAnimator.Play(holdCoinsAnimationPrefix + currentTokens + holdCoinsAnimationPostfix);
		}
		if (needsToRollupBonusGame)
		{
			StartCoroutine(handleWinnings());
		}
	}

	public virtual void betChanged(bool isIncreasingBet)
	{
	
	}

	protected virtual void tokenWonEvent(JSON data)
	{
		eventId = "";
		bonusType = "";
		bonusGameName = "";
		bonusGameOutcomeJson = null;
		tokenJSON = data;
	}

	public virtual void startBonusGame()
	{
		// Make sure we end any spin transactions before transitioning away from the game currently being played
		// otherwise they will think they are still happening when we come back
		if (Glb.spinTransactionInProgress)
		{
			Glb.endSpinTransaction();
		}

		GameState.pushBonusGame(bonusGameKeyName, "", bonusType, eventId, bonusGameOutcomeJson);
		Glb.loadGame();
#if ZYNGA_TRAMP
		if (AutomatedPlayerCompanion.instance.activeGame.getActionAt(0).Key == AutomatedTestAction.FEATURE_MINI_GAME_ACTION)
		{
		AutomatedPlayerCompanion.instance.activeGame.removeActionAt(0); //Get rid of our test for the feature action
		}

		AutomatedPlayerCompanion.instance.addCurrentGameToTopOfTestStack(); //Add the current game to the games to test list

		AutomatedGameIteration newTest = new AutomatedGameIteration(new AutomatedGame(GameState.game.keyName));
		AutomatedPlayerCompanion.instance.activeGame = newTest;
		AutomatedPlayerCompanion.instance.activeGame.start();
		AutomatedPlayerCompanion.instance.activeGame.recieved(bonusGameOutcomeJson.ToString(), true); //Make sure any error/warnings in the mini game lead back to the mini game outcome
#endif
	}

	public virtual void hideBar()
	{
	
	}

	public class TokenInformation
	{
		public long threshold = 0L;
		public long minimumWager = 0; //Base * Inflation Factor
		public int picks = 0;
		public long absMinWager = 0;
		public float startingMinWager = 0; //Base Non-Modified Value

		public TokenInformation(long _threshold, long _minimumWager, int _picks, float _startingMinWager, long _absMinWager)
		{
			threshold = _threshold;
			minimumWager = _minimumWager;
			picks = _picks;
			startingMinWager = _startingMinWager;
			absMinWager = _absMinWager;
		}

		public void adjustInflationValue(float oldInflation, float newInflation)
		{
			float startingThreshold = (float)threshold/oldInflation;
			threshold = (long)(startingThreshold * newInflation);

			long newInflatedWager = (long) (startingMinWager * newInflation);
			minimumWager = newInflatedWager > absMinWager ? newInflatedWager : absMinWager;
		}
	}

	public static void resetStaticClassData()
	{
		needsToRollupBonusGame = false;
	}
	
	public Bounds getBounds()
	{
		if (reelsBoundsLimit != null)
		{
			return reelsBoundsLimit.bounds;
		}
		else
		{
			return new Bounds(Vector3.zero, Vector3.zero);
		}
	}
}
