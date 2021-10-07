using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Used to get the progressivePool and the winning value.


// This is the controler class for all of the Sunday Comic Bonus classes. It starts off each of the possible stages
// for the sunday comic bonus game, and expects to be told when they have finished so that it can move to the next stage
// or end the bonus.
//
// The flow of this game looks like this
// 1. Pre Pickem - Pick between some banners to reveal the amount of picks that you have.
// 2. Pickem - Go through the pickem game and reveal the possible values that you can have.
// if you pick a POW symbol, goto step 3 o.w. end game.
// 3. Post Pickem - Reveal a progressive bonus value to the player.
public class SundayComicBonus : ChallengeGame 
{
	public GameObject prePickemGame; 							// The game object that is activated when the game starts.
	public GameObject pickemGame; 								// The game the gets called after the prepickem game.
	public GameObject postPickemGame; 							// The game that gets called after the pickem game.
	public GameObject progressiveParent; 						// Parent with all the progressive information underneath it.
	public GameObject characterParent; 							// The parent that contains the comic character.
	public GameObject[] progressiveTexts; 						// Each of the progressive texts, used to populate the value.
	public UILabel winLabel;									// The label that shows the winnings. -  To be removed when prefabs are updated.
	public LabelWrapperComponent winLabelWrapperComponent;									// The label that shows the winnings.

	public LabelWrapper winLabelWrapper
	{
		get
		{
			if (_winLabelWrapper == null)
			{
				if (winLabelWrapperComponent != null)
				{
					_winLabelWrapper = winLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winLabelWrapper = new LabelWrapper(winLabel);
				}
			}
			return _winLabelWrapper;
		}
	}
	private LabelWrapper _winLabelWrapper = null;
	
	public UILabel picksRemainingLabel;							// The label that shows the number of picks remaining. -  To be removed when prefabs are updated.
	public LabelWrapperComponent picksRemainingLabelWrapperComponent;							// The label that shows the number of picks remaining.

	public LabelWrapper picksRemainingLabelWrapper
	{
		get
		{
			if (_picksRemainingLabelWrapper == null)
			{
				if (picksRemainingLabelWrapperComponent != null)
				{
					_picksRemainingLabelWrapper = picksRemainingLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_picksRemainingLabelWrapper = new LabelWrapper(picksRemainingLabel);
				}
			}
			return _picksRemainingLabelWrapper;
		}
	}
	private LabelWrapper _picksRemainingLabelWrapper = null;
	
	public GameObject priceInformationParent; 					// The banner at the top that shows $1.00<Space>Sunday Comic<Space>Since 2013
	public GameObject titleParent; 								// The title parent containing the POW symbol and the text.
	public GameObject coverTexture; 							// a texture to cover up the score box until we're ready to show it
	[HideInInspector] public PickemOutcome pickemOutcome;		// pickemOutcome for the Pre and Pickem game.
//	private WheelOutcome _portalOutcome; 						// This doesn't get used on web, but I'm getting the data because it is being passed.
	[HideInInspector] public WheelOutcome progressiveOutcome;	// wheelOutcome that stores the progressive win amount.
	
	public int numberOfPicks									// The number of picks that are remaining, updating this updates pickRemainingLabel.
	{
		get
		{
			return _numberOfPicks;
		}
		set
		{
			_numberOfPicks = value;
			updatePicksRemaining();
		}
	}
	protected int _numberOfPicks;

	private LabelWrapperComponent title; 									// The label in the title.
	private LabelWrapperComponent prePickTitle; 								// The label in the pre pick title. Second lable used for centering purposes
	private GameObject powSymbol; 								// The pow symbol in the title
	// Constant Variables
	// Localization Strings
	private const string PICK_SCREEN_TITLE = "comics_common_pick_screen_title";		// Loc key for the title of the pickem game.
	private const string POST_PICK_SCREEN_TITLE = "win_last_jackpot_remaining";		// Loc key for the title of the post-pickem game.
	private const string COM01_PAYTABLE_NAME = "com01_common_progressive_t1";
	private const string COM02_PAYTABLE_NAME = "com02_common_progressive_t1";

	public override void init() 
	{
		pickemOutcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		title = CommonGameObject.findDirectChild(titleParent,"Text").GetComponent<LabelWrapperComponent>();
		prePickTitle = CommonGameObject.findDirectChild(titleParent,"PrePickTitle").GetComponent<LabelWrapperComponent>();
		powSymbol = CommonGameObject.findDirectChild(titleParent,"Pow Symbol");
		setProgressiveAmounts();
		startPrepickem();

		_didInit = true;
	}

	public IEnumerator rollupNumberOfPicks(int numberOfPicks)
	{
		//int startPicks = _numberOfPicks;
		_numberOfPicks = numberOfPicks;
		picksRemainingLabelWrapper.text = numberOfPicks.ToString();
		yield break;
	}

	/// Keeps the number of picks non negitive.
	private void updatePicksRemaining()
	{
		picksRemainingLabelWrapper.text = (numberOfPicks < 0 ? 0 : numberOfPicks).ToString();
	}

	/// Goes though each of the progressive and sets them to the proper amounts collected from the base games progressive data.
	private void setProgressiveAmounts()
	{
		JSON[] progressivePools = SlotBaseGame.instance.slotGameData.progressivePools; // These pools are from highest to lowest.

		for (int k = 0; k < progressiveTexts.Length; k++)
		{
			UILabel label = progressiveTexts[k].GetComponent<UILabel>();
			if (label != null)
			{
				long credits = 0;
				if (progressivePools != null && progressivePools.Length > 0)
				{
					credits = SlotsPlayer.instance.progressivePools.getPoolCredits(progressivePools[k].getString("key_name", ""), SlotBaseGame.instance.multiplier, false);
				}
				else
				{
					if(GameState.game.keyName.Contains("com01"))
					{
						credits = BonusGamePaytable.getBasePayoutCreditsForPaytable("wheel", COM01_PAYTABLE_NAME, "-1", k) * SlotBaseGame.instance.multiplier * GameState.baseWagerMultiplier;
					}
					else if(GameState.game.keyName.Contains("com02"))
					{
						credits = BonusGamePaytable.getBasePayoutCreditsForPaytable("wheel", COM02_PAYTABLE_NAME, "-1", k) * SlotBaseGame.instance.multiplier * GameState.baseWagerMultiplier;
					}
				}
				label.text = CreditsEconomy.convertCredits(credits);
			}
		}

	}
	
	private void endGame()
	{
		BonusGamePresenter.instance.gameEnded();
	}

	/// The banners that get selected at the begining.
	/// Sets everything to the correct enabled and disabled state so that any changes made in the editor don't change that way that the game is played.
	private void startPrepickem()
	{
		title.gameObject.SetActive(false);
		prePickTitle.gameObject.SetActive(true);

		// Make the title oscilate between different colors.
		Color[] colorsToSwapBetween = new Color[] { Color.green, Color.magenta, Color.blue, Color.white};
		CommonEffects.addOscillateTextColorEffect(title.labelWrapper, colorsToSwapBetween, 0.01f);
		// Activate the right game
		prePickemGame.SetActive(true);
		priceInformationParent.SetActive(true);
		// Make sure everything else is disabled
		powSymbol.SetActive(false);
		pickemGame.SetActive(false);
		postPickemGame.SetActive(false);
		progressiveParent.SetActive(false);
		characterParent.SetActive(false);
	}

	public void endPrepickem()
	{
		// Set the previous game to inactive.
		prePickemGame.SetActive(false);
		priceInformationParent.SetActive(false);
		prePickTitle.gameObject.SetActive(false);
		title.color = Color.white;
		startPickem();
	}

	/// Pickem game
	private void startPickem()
	{
		title.gameObject.SetActive(true);
		prePickTitle.gameObject.SetActive(false);
		title.text = Localize.text(PICK_SCREEN_TITLE);
		powSymbol.SetActive(true);
		progressiveParent.SetActive(true);
		coverTexture.SetActive(false);
		winLabelWrapper.transform.parent.gameObject.SetActive(true);
		pickemGame.SetActive(true);
		characterParent.SetActive(true);
	}

	/// Checks to see if there should be a progressive bonus for this game.
	public void endPickem()
	{
		if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.CREDIT))
		{
			progressiveOutcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CREDIT];
		}
		
		if (progressiveOutcome != null)
		{
			BonusGamePresenter.instance.currentPayout *= BonusGameManager.instance.currentMultiplier; // This is the only part of the game that we want to multiply.
			BonusGamePresenter.instance.useMultiplier = false; // We don't want to use the multiplier anymore because of how progressives are calculated.
			winLabelWrapper.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
			pickemGame.SetActive(false);
			startPostpickem();
		}
		else
		{
			endGame();
		}
	}

	/// Progressive Bonus reveals
	private void startPostpickem()
	{
		title.text = Localize.text(POST_PICK_SCREEN_TITLE);
		postPickemGame.SetActive(true);
		SundayComicPostpickem postPickemGameScript = postPickemGame.GetComponent<SundayComicPostpickem>();
		if (postPickemGameScript != null)
		{
			StartCoroutine(postPickemGameScript.startGame());
		}
		else
		{
			Debug.LogWarning("There is no postPickemGame defined and there should be. Aborting.");
		}

	}
	public void endPostpickem(long progressiveAmount)
	{
		StartCoroutine(endPostpickemEnumerated(progressiveAmount));
	}

	private IEnumerator endPostpickemEnumerated(long progressiveAmount)
	{
		yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout - progressiveAmount , BonusGamePresenter.instance.currentPayout, winLabelWrapper));
		endGame();
	}

}

