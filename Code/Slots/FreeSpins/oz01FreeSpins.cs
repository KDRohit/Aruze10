using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Oz01 free spins has unique fuctionality that requires the normal FreeSpinGame class to be overridden.
*/

public class oz01FreeSpins : FreeSpinGame
{
	//parent objects
	public Transform buttonParent;
	public Oz01BasketButton[] basketButtons;
	public Transform explosianPrefab;
	public Transform sparkleTrail;
	
	//title pieces
	public UITexture titleBackground;
	public TweenPosition titleLabel;
	public UISprite titleShine;
	public GameObject titleOverlay;
	public UILabel winningLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winningLabelWrapperComponent;

	public LabelWrapper winningLabelWrapper
	{
		get
		{
			if (_winningLabelWrapper == null)
			{
				if (winningLabelWrapperComponent != null)
				{
					_winningLabelWrapper = winningLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winningLabelWrapper = new LabelWrapper(winningLabel);
				}
			}
			return _winningLabelWrapper;
		}
	}
	private LabelWrapper _winningLabelWrapper = null;
	
	
	private string lastWinFullInfo;
	private string lastWinInfo;
	private int indexChosen;
	private JSON[] bonusCards;
	private List<JSON> unpickedBonusCards = null;	///< Tracks which bonuses weren't pick, so we can use them in the end reveal.

	private long lastCreditWin = 0;
	private int lastFreeSpinWin = 0;

	private enum winPosition 
	{
		none,
		left,
		center,
		right
	}
	private winPosition currentWinPos;
	private long displayedMultiplier = 1; 	// the displayed multiplier value that the user sees and can increase by picking baskets
	private long startingMultiplier;		// tracks the starting multiplier which needs to be multiplied by the displayMultiplier to get the value we should be scaling by

	//used to control spinning in all object upon show
	private bool isShining;
	// Constant Variables
	private const float OPENING_VOICEOVER_DELAY = 1.0f;								// How long to wait after the start of the game to play the voiceover.
	private const float TIME_SHOW_WINNING_TEXT = 2.0f;								// The amount of time to show the winning text for.
	private const float TIME_BETWEEN_BASKET_SETUPS = 0.1f;							// The amount of time to wait between each basket is revealed in the pickem game.
	private const float TIME_TO_SHOW_BASKET_AMOUNT = 1.0f;							// The amount of time to wait after showing what the basket reveal amount is.
	private const float TIME_TO_UPDATE_LABEL = 0.1f;								// How long to wait before updating the label after a basket has been clicked.
	private const float TIME_BETWEEN_BASKET_REVEALS = 0.5f;							// How much time to wait between each reveal after the freespins are over.
	private const float TIME_AFTER_GAME_ENDS = 1.0f;								// How long to wait after the game is over to let the player see the board before the summary dialog.
	private const float TIME_TO_MOVE_IN_PICKEM = 0.5f;								// How long it should take to roll in the pickem game from the bottom of the screen.
	private const float TIME_TO_MOVE_OUT_PICKEM = 0.5f;								// How long it should take to roll out the pickem game from the bottom of the screen.
	private const float TIME_FOR_SPARKLE_TO_MOVE = 1.0f;							// The time it takes for the sparkle to move from the basket to the win position.
	private const float TIME_FOR_EXPLOSION = 1.5f;									// How long the fireworks animate for.
	private const float TIME_BASKET_SCALE_IN = 0.5f;								// How much time it should take for the basket to scale in on a normal device, slow devices don't scale
	private const float TIME_TO_MOVE_IN_WINNING_AMOUNT = 0.5f;						// How long it takes to move the wining amount up from the bottom of the screen.
	private const float TIME_TO_MOVE_OUT_WINNING_AMOUNT = 0.5f;						// How long to move the winning amount off the screen.


	// Sound names
	private const string OPENING_VO = "dogoodluck";									// The name of the voiceover that plays on the start of the bonus game.
	private const string BACKGROUND_MUSIC = "mayhemrainbowbg0";						// The name of the music that should be playing in the background.
	private const string PICKEM_START_VO = "totobasket0";							// The voice over that plays at the start of the pickem stage of the bonus game.
	private const string PICKEM_START = "showbaskets0";								// The name of the sound effect that plays at the start of the pickem stage.
	private const string SHOW_BASKET_AWARD = "showbasketaward0";					// Name of sound played when a basket is clicked on.
	private const string VALUE_MOVE = "xferbasketaward0";							// Name of sound played while the value is moving from the basket to the correct label.
	private const string VALUE_LAND = "clickheelsparkly0";							// Name of sound played when the value lands in the correct label.
	private const string SPARKLE_MOVING_SOUND = "xferbasketaward0";					// Name of sound while the sparkle effect is moving to the win box.
	private const string EXPLOSION_SOUND = "clickheelsparkly0";						// Sound name played when the fireworks go off.

	public override void initFreespins()
	{
		base.initFreespins();
		Audio.play(OPENING_VO, 1f, 0f, OPENING_VOICEOVER_DELAY);

		// Cache this data so it doesn't need to get looked up every time a basket is picked.
		bonusCards = _freeSpinsOutcomes.paytable.getJsonArray("bonus_cards");
		
		unpickedBonusCards = new List<JSON>(bonusCards);

 		for (int i = 0; i < basketButtons.Length; i++)
		{
			basketButtons[i].toggleButton(false);
		}

		showTitleElements(false);
		
		// Since this is treated as a challenge game, we don't use the normal multiplier for each spin's win.
		// This may increase as additional multipliers are found in picked baskets.
		displayedMultiplier = 1;

		if (GameState.giftedBonus != null)
		{
			startingMultiplier = multiplier = 1;
		}
		else
		{
			// in new wager system we need to apply the partial multiplier 
			// in addition to the displayed one so that the final math works 
			// out and doesn't cause a desync
			startingMultiplier = multiplier = GameState.bonusGameMultiplierForLockedWagers;
		}
	}

 	protected override void Update()
	{
		base.Update();
		if (!_didInit)
		{
			return;
		}

		if (isShining) 
		{
			for (int i = 0; i < basketButtons.Length; i++)
			{
				basketButtons[i].shine.transform.Rotate(-Vector3.forward, 50 * Time.deltaTime);
			}
			titleShine.transform.Rotate(-Vector3.forward, 50 * Time.deltaTime);
		}
   	}
   	
   	protected override void reelsStoppedCallback()
   	{
   		bool haveAllBeenPicked = true;
		foreach (Oz01BasketButton basket in basketButtons)
		{
			if (!basket.beenPicked)
			{
				haveAllBeenPicked = false;
			}
		}
		
		if (engine.getSymbolCount("TR") > 0 && !haveAllBeenPicked)
		{
			StartCoroutine(startPickEmGame());
		}
		else
		{
			base.reelsStoppedCallback();
		}
	}

	protected override IEnumerator prespin()
	{
		Audio.switchMusicKeyImmediate(BACKGROUND_MUSIC);
		yield return StartCoroutine(base.prespin());
	}

	private IEnumerator startPickEmGame()
	{
		Audio.play(PICKEM_START_VO);
		Audio.play(PICKEM_START);
		
		yield return StartCoroutine(showPickEmGame());

		showTitleElements(true);
		for (int i = 0; i < basketButtons.Length; i++)
		{
			basketButtons[i].doBasketShine(true);
			yield return new WaitForSeconds(TIME_BETWEEN_BASKET_SETUPS);
		}

		for (int i = 0; i < basketButtons.Length; i++)
		{
			basketButtons[i].toggleButton(true);
		}

		isShining = true;
	}

	/// Move the pickem game into view from the bottom.
	private IEnumerator showPickEmGame()
	{
		TweenPosition.Begin(buttonParent.gameObject, TIME_TO_MOVE_IN_PICKEM, new Vector3(buttonParent.localPosition.x, -605, buttonParent.localPosition.z));
		yield return new WaitForSeconds(TIME_TO_MOVE_IN_PICKEM);
	}

	private void showTitleElements(bool isEnabled) 
	{
		titleOverlay.SetActive(isEnabled);
		titleLabel.gameObject.SetActive(isEnabled);
		titleLabel.enabled = isEnabled;

		titleBackground.gameObject.SetActive(isEnabled);
		titleShine.gameObject.SetActive(isEnabled);

		if (isEnabled)
		{
			// We want to make these both pixel perfect.
			titleBackground.MakePixelPerfect();
			// We cheat with the title shine and make it double it's size because it doesn't need to be very high res.
			titleShine.MakePixelPerfect();
			titleShine.transform.localScale *= 2;
			// Don't do the scale in on a slow devices because it may look jumpy
			if (!MobileUIUtil.isSlowDevice)
			{
				iTween.ScaleFrom(titleBackground.gameObject, Vector3.one, TIME_BASKET_SCALE_IN);
				iTween.ScaleFrom(titleShine.gameObject, Vector3.one, TIME_BASKET_SCALE_IN);
			}
		}
		else
		{
			titleBackground.transform.localScale = Vector3.one;
			titleShine.transform.localScale = Vector3.one;
			// TweenScale.Begin(titleBackground.gameObject, 0.5f, Vector3.one);
			// TweenScale.Begin(titleShine.gameObject, 0.5f, Vector3.one);
		}
	}

	public void basketPicked(int index)
	{
		Audio.play("menuselect0");
		indexChosen = index;
		// TODO: Joey Sound < sound >

		for (int i = 0; i < basketButtons.Length; i++)
		{
			//toggles colliders to stop OnClicks
			basketButtons[i].toggleButton(false);

			//hide all put one chosen. Visual effect
			if (basketButtons[i].id != indexChosen) 
			{
				basketButtons[i].hideButton();
			}
		}

		StartCoroutine(basketPickedSequence());
	}

	private IEnumerator basketPickedSequence()
	{
		//hide the button the was chosen. Visual effect
		storeButtonInformation();

		Audio.play(SHOW_BASKET_AWARD);
		yield return new WaitForSeconds(TIME_TO_SHOW_BASKET_AMOUNT);

		basketButtons[indexChosen].hideButton();

		if (lastWinInfo != "")
		{
			Audio.play(VALUE_LAND);
			yield return StartCoroutine(showWinningText());
		}

		yield return StartCoroutine(updateMessageLabel());

		StartCoroutine(resetGame());
	}

	/// Do some info processing for the outcome.
	private void storeButtonInformation()
	{
		foreach (SlotOutcome subOutcomes in _outcome.getSubOutcomesReadOnly()) 
		{
			foreach (JSON bonusOutcome in bonusCards)
			{
				if (subOutcomes.getWinId() == bonusOutcome.getInt("id", 0) && subOutcomes.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.BONUS_SYMBOL) 
				{
					// Remove this pick from the unpicked list, so it won't be revealed at the end.
					unpickedBonusCards.Remove(bonusOutcome);
					
					defineWinInfo(bonusOutcome);
					return;
				}
			}
		}
	}
	
	/// Defines variables with info about the given bonus outcome.
	private void defineWinInfo(JSON bonusOutcome)
	{
		long creditsReceived = bonusOutcome.getLong("credits", 0) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
		int freeSpinsReceived = bonusOutcome.getInt("free_spins", 0);
		int multiplierReceived = bonusOutcome.getInt("multiplier", 0);

		if (creditsReceived > 0)
		{
			lastWinInfo = string.Format("{0} {1}", creditsReceived, Localize.text("credits"));
			lastWinFullInfo = lastWinInfo;
			lastCreditWin = creditsReceived;
			currentWinPos = winPosition.right;
		}
		else if (freeSpinsReceived > 0)
		{
			lastWinInfo = Localize.text("{0}_free_spins", freeSpinsReceived);
			lastWinFullInfo = Localize.text("you_won_free_spins_{0}", freeSpinsReceived);
			lastFreeSpinWin = freeSpinsReceived;
			currentWinPos = winPosition.left;
		}
		else if (multiplierReceived > 0)
		{
			lastWinInfo = "+" + Localize.text("{0}X", multiplierReceived);
			lastWinFullInfo = Localize.text("plus_{0}_multiplier", multiplierReceived);
			currentWinPos = winPosition.center;
			displayedMultiplier += multiplierReceived;

			// calculate the new multiplier based on the starting multiplier and the one being displayed
			multiplier = startingMultiplier * displayedMultiplier;
		}
	}

	private IEnumerator showWinningText() 
	{
		winningLabelWrapper.text = lastWinFullInfo;

		TweenPosition.Begin(winningLabelWrapper.gameObject, TIME_TO_MOVE_IN_WINNING_AMOUNT, new Vector3(winningLabelWrapper.transform.localPosition.x, 0, 0));

		yield return new WaitForSeconds(TIME_SHOW_WINNING_TEXT);

		winningLabelWrapper.text = "";
		TweenPosition.Begin(winningLabelWrapper.gameObject, TIME_TO_MOVE_OUT_WINNING_AMOUNT, new Vector3(winningLabelWrapper.transform.localPosition.x, -300, 0));

		for (int i = 0; i < basketButtons.Length; i++)
		{
			basketButtons[i].showButton();
		}

		basketButtons[indexChosen].labelWrapper.text = lastWinInfo;
	}

	private IEnumerator updateMessageLabel()
	{
		yield return new WaitForSeconds(TIME_TO_UPDATE_LABEL);

		if (displayedMultiplier > 1)
		{
			additionalInfo = Localize.text("{0}X", displayedMultiplier) + " " + Localize.text("multiplier_text");
		}
	}

	private IEnumerator doWinParticleAnimation() 
	{
		sparkleTrail.transform.position = basketButtons[indexChosen].labelWrapper.transform.position;

		if (currentWinPos == winPosition.right)
		{
			explosianPrefab.transform.parent = BonusSpinPanel.instance.winningsAmountLabel.transform.parent.transform;
			explosianPrefab.transform.localPosition = Vector3.zero;
			sparkleTrail.transform.parent = BonusSpinPanel.instance.winningsAmountLabel.transform.parent.transform;
		}
		else if (currentWinPos == winPosition.left)
		{
			explosianPrefab.transform.parent = BonusSpinPanel.instance.spinCountLabel.transform.parent.transform;
			explosianPrefab.transform.localPosition = Vector3.zero;
			sparkleTrail.transform.parent = BonusSpinPanel.instance.spinCountLabel.transform.parent.transform;
		}
		
		sparkleTrail.gameObject.SetActive(true);
		Audio.play(SPARKLE_MOVING_SOUND);
		TweenPosition.Begin(sparkleTrail.gameObject, TIME_FOR_SPARKLE_TO_MOVE, Vector3.zero);
		yield return new WaitForSeconds(TIME_FOR_SPARKLE_TO_MOVE);

		if (currentWinPos == winPosition.right)
		{
            runningPayoutRollupValue += lastCreditWin;
            setWinningsDisplay(runningPayoutRollupValue);
		}
		else if (currentWinPos == winPosition.left)
		{
			FreeSpinGame.instance.numberOfFreespinsRemaining += lastFreeSpinWin;
		}

		sparkleTrail.gameObject.SetActive(false);
		explosianPrefab.gameObject.SetActive(true);
		Audio.play(EXPLOSION_SOUND);
		yield return new WaitForSeconds(TIME_FOR_EXPLOSION);
		explosianPrefab.gameObject.SetActive(false);
		sparkleTrail.transform.parent = transform;

		//reset of string info and winPos
		lastWinInfo = "";
		lastWinFullInfo = "";
		currentWinPos = winPosition.none;
	}

	private IEnumerator resetGame() 
	{
		//turn off shine elements
		for (int i = 0; i < basketButtons.Length; i++)
		{
			basketButtons[i].doBasketShine(false);
		}
		isShining = false;

		//turn off title elements
		showTitleElements(false);

		//this is for the bottom bar the shows and hides. Its now in hide position
		TweenPosition.Begin(buttonParent.gameObject, TIME_TO_MOVE_OUT_PICKEM, new Vector3(buttonParent.localPosition.x, -950, buttonParent.localPosition.z));

		if (currentWinPos != winPosition.center)
		{
			yield return StartCoroutine(doWinParticleAnimation());
		}
		else
		{
			// Play the value land sound for the center postion, but no animation happens.
			Audio.play(VALUE_LAND);
		}
		//empty the winning information
		base.reelsStoppedCallback();
	}

	/// The free spins game ended.
	protected override void gameEnded()
	{
		multiplier = GameState.giftedBonus != null ? 1 : SlotBaseGame.instance.multiplier;
		
		StartCoroutine(showAllBasketsAtEnd());
	}

	/// Shows the remaining unpicked baskets at the end of the game, before showing the final score.
	private IEnumerator showAllBasketsAtEnd()
	{
		yield return StartCoroutine(showPickEmGame());
		
		for (int i = 0; i < basketButtons.Length; i++)
		{
			if (!basketButtons[i].beenPicked)
			{
				// Define win info variable for a random bonus option that hasn't been picked.
				int index = Random.Range(0, unpickedBonusCards.Count);
				if (unpickedBonusCards.Count != 0 && index < unpickedBonusCards.Count)
				{
					defineWinInfo(unpickedBonusCards[index]);
					unpickedBonusCards.RemoveAt(index);
				}
				
				// Reveal it!
				basketButtons[i].reveal(lastWinInfo);
				// Wait!
				yield return new WaitForSeconds(TIME_BETWEEN_BASKET_REVEALS);
			}
		}
		
		yield return new WaitForSeconds(TIME_AFTER_GAME_ENDS);
		
		yield return StartCoroutine(waitForModulesThenEndGame());
	}
}

