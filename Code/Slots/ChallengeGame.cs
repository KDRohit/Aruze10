using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Base class for all challenge games (bonus games that aren't free spins).
Since init() is called AFTER the GameObject is downloaded and instantiated,
don't use Awake() or Start() to start things automatically.
Use init() to initialize the game, and use startGame() if necessary to start it after initialization.
*/

public abstract class ChallengeGame : TICoroutineMonoBehaviour
{
	public static ChallengeGame instance = null;
	
	protected bool _didInit = false;
	protected bool _didStart = false;

	public bool wingsInForeground = false;
	public bool wingsIncludedInBackground = false;

	public FanfareEnum fanfareType = FanfareEnum.BonusSummary;
	public string customBonusSummaryFanfareSound = "";

	[SerializeField] private bool isUsingReelGameRollupSounds = false; // use this if you want to use base game rollup sounds instead of challenge game ones

	public enum FanfareEnum
	{
		BonusSummary = 0,
		WheelSummary,
		FreeSpinSummary	// on the off chance only this is defined, probably avoid using this though
	}

	public string rollupSoundOverride;
	public string rollupTermOverride;
	
	public bool hasItsOwnLightSet = false;
	public bool shouldAutoPlayBgMusic = true;
	
	public float delayBonusSummaryVo = 0.0f; // If the summary has a VO, then wait this many seconds to play it.
	
	protected virtual void Awake()
	{
		instance = this;

		if (BonusGamePresenter.instance != null)
		{
			setBonusGameName();
		}
	}

	// Sets the bonusGameName used by BonusGameManager and BonusGamePresenter
	// made public so that BonusGamePresenter can call it if the ChallengeGame
	// gets initialized before it
	public void setBonusGameName()
	{
		bool isBonusGameNameSet = false;

		// try to auto grab the bonusGameName form the challenge outcome
		if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.CHALLENGE))
		{
			BaseBonusGameOutcome challengeOutcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
			if (challengeOutcome != null)
			{
				BonusGameManager.instance.bonusGameName = challengeOutcome.bonusGameName;
				BonusGamePresenter.instance.bonusGameName = challengeOutcome.bonusGameName;
				BonusGameManager.instance.paytableSetId = "";
				BonusGamePresenter.instance.paytableSetId = "";
				isBonusGameNameSet = true;
			}
		}

		if (!isBonusGameNameSet)
		{
			// Couldn't determine a name, but we will
			// just clear them out so we don't send something incorrectly
			BonusGameManager.instance.bonusGameName = "";
			BonusGamePresenter.instance.bonusGameName = "";
			BonusGameManager.instance.paytableSetId = "";
			BonusGamePresenter.instance.paytableSetId = "";
		}
	}

	// Allows manual reseting of the flags which control the game
	public void reset()
	{
		_didInit = false;
		_didStart = false;
	}
	
	protected virtual void Update()
	{
		if (_didInit && !_didStart)
		{
			startGame();
		}
	}
	
	/// Starts the game after initialization is finished.
	protected virtual void startGame()
	{
		_didStart = true;
	}

	private const string WHEEL_ROLLUP = "wheel_rollup";
	private const string WHEEL_ROLLUP_END = "wheel_rollup_term";
	private const string ROLLUP_BONUS = "rollup_bonus_loop";
	private const string ROLLUP_BONUS_END = "rollup_bonus_end";


	public virtual string getRollupSound(long payout)
	{
		if (isUsingReelGameRollupSounds && ReelGame.activeGame != null)
		{
			return ReelGame.activeGame.getRollupSound(payout, shouldBigWin: false);
		}
		else
		{
			if (rollupSoundOverride != "")
			{
				return rollupSoundOverride;
			}

			if (this is WheelGame && Audio.canSoundBeMapped(WHEEL_ROLLUP) && Audio.canSoundBeMapped(WHEEL_ROLLUP_END))
			{
				return Audio.soundMap(WHEEL_ROLLUP);
			}

			return Audio.soundMap(ROLLUP_BONUS);
		}
	}

	public virtual string getRollupTermSound(long payout)
	{
		if (isUsingReelGameRollupSounds && ReelGame.activeGame != null)
		{
			return ReelGame.activeGame.getRollupTermSound(payout, shouldBigWin: false);
		}
		else
		{
			if (rollupTermOverride != "")
			{
				return rollupTermOverride;
			}

			if (this is WheelGame && Audio.canSoundBeMapped(WHEEL_ROLLUP) && Audio.canSoundBeMapped(WHEEL_ROLLUP_END))
			{
				return Audio.soundMap(WHEEL_ROLLUP_END);
			}

			return Audio.soundMap(ROLLUP_BONUS_END);
		}
	}

	// Function to handle anything that needs to happen before BonusGamePresenter calls finalCleanup
	public virtual IEnumerator handleNeedsToExecuteOnBonusGamePresenterFinalCleanupModules()
	{
		// default is to not do anything
		// override this to handle custom stuff
		yield break;
	} 
	
	/// Required initialization function.
	public abstract void init();
}

