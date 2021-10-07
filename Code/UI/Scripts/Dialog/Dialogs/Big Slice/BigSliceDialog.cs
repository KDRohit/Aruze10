using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
using TMProExtensions;

/*
Controls the dialog for showing mystery gift game and sharing the news about it.
*/

public class BigSliceDialog : DialogBase, IResetGame
{
	[SerializeField] private ImageButtonHandler closeHandler;
	[SerializeField] private ImageButtonHandler collectClickHandler;
	public static bool isShowing = false;	// Is this dialog currently open anywhere on the dialog stack?

	public Animator transitionAnimator;
	public UIPanel gamePanelMain;
	public UIPanel gamePanelForeground;
	public Renderer backgroundRenderer;
	public Renderer summaryBackgroundRenderer;
	public GameObject headerLabelSizer;
	public TextMeshPro headerLabel;
	public BigSlicePickemChoice pickemChoiceTemplate;
	public UIGrid pickemGrid;
	public GameObject wheelParent;
	public GameObject summaryParent;
	public GameObject closeButton;
	public GameObject collectButton;
	public TextMeshPro collectButtonLabel;
	public ParticleSystem fireball;
	public ParticleSystem sparkleExplosion;
	public GameObject bigSliceUpgradePrefab;
	public PickemButtonShakerMaster pickemShaker;
	
	// The labels at the bottom, not on the summary screen.
	public TextMeshPro bottomTotalBetLabel;
	public TextMeshPro bottomMultiplierLabel;
	public TextMeshPro bottomWinAmountLabel;
	// The labels on the summary screen.
	public TextMeshPro summaryTotalBetLabel;
	public TextMeshPro summaryMultiplierLabel;
	public TextMeshPro summaryWinAmountLabel;

	public Renderer wheelRenderer;
	public Collider wheelButtonCollider;
	public TextMeshPro[] wheelWedgeLabels;
	public GameObject wheelSpin;
	public Animator plusOneAnimation;
	public GameObject wheelSpinParticles;

	public  long BIG_SLICE_DEFAULT_MULTIPLIER = 50L;
	public  float SUMMARY_DELAY = 0.5f;

	
	private const float DELAY_BEFORE_ROLLUP = 1.0f;
	protected  float TOTAL_AMOUNT_ROLLUP_TIME = 2.0f;
	private const float TIME_BETWEEN_CHOICE_REVEALS = 0.25f;
	private const float MULTIPLIER_FLY_TIME = 0.5f;
	private const float DELAY_BETWEEN_PLUS_1_ANIM_AND_BOOST = 1.0f;
	protected  float DELAY_BEFORE_SUMMARY = 0.5f;
	private const float FIREBALL_EXPLOSION_DELAY = 2.0f;
	protected  float GAME_FADE_IN_TIME = 0.5f;
	private const float GAME_FADE_OVERLAP = 1.0f;

	// Sounds  TBD use sound mapping
	protected const string AUDIO_BACKGROUND_MUSIC = "BonusBgSlice";
	protected const string AUDIO_TRANSITION_START = "TransitionToSliceIn";
	protected const string AUDIO_TRANSITION_END = "TransitionToSliceOut";
	protected const string AUDIO_COIN_PICK = "SparklyCoinPick";
	protected const string AUDIO_REVEAL_DOUBLE_BET = "PickCoinRevealDoubleBetSlice";
	protected const string AUDIO_REVEAL_SPIN = "PickCoinRevealSpinSlice";
	protected const string AUDIO_PRE_SPIN = "WheelPrespinSlice";
	protected const string AUDIO_REVEAL_UNCHOSEN = "RevealSparklyCmajSlice";
	protected const string AUDIO_PLUS_ONE_ALL_WEDGES = "PickCoinRevealAdvxSlice";
	protected const string AUDIO_REVEAL_BIG_SLICE = "PickCoinRevealBigSlice";
	protected const string AUDIO_WHEEL_MUSIC = "WheelMusicSlice";
	protected const string AUDIO_WHEEL_DECELERATE = "wheel_decelerate";
	protected const string AUDIO_WHEEL_STOP = "WheelStopSlice";
	protected const string AUDIO_WHEEL_STOP_BIG_SLICE = "WheelStopBigSlice";
	protected const string AUDIO_SUMMARY = "SummaryWheelSlice";
	protected const string AUDIO_SUMMARY_REVEAL = "bonus_summary_reveal_value";
	protected const string AUDIO_FIREBALL_START = "BetTravelsSparklyWhooshSlice";
	protected const string AUDIO_FIREBALL_LAND = "BetArrivesSparklySplashSlice";
	protected const string AUDIO_ROLLUP = "RollupWheelSlice";
	protected const string AUDIO_ROLLUP_END = "RollupTermWheelSlice";
	
	protected bool isWaitingForTouch = false;
	private long baseBetAmount = 0L;	// Can be from the last spin's bet, or from a quest reference wager if triggered by gettin a quest collectible.
	private long betMultiplier = 1L;	// This can increase, usually only by 1.
	protected long multiplier = 1L;		// The awarded multiplier.
	protected long totalWin = 0L;			// This comes from betAmount * betMultiplier * multiplier. Multiplier is taken from the bonus game winnings instead of credits.

	protected List<string> wheelWedgeIds = new List<string>();

	// The first element in this list is the template gift.
	protected List<BigSlicePickemChoice> pickemChoices = new List<BigSlicePickemChoice>();
	private List<BigSlicePickemChoice> unrevealedChoices = null;
	protected Dictionary<int, Animator> bigSliceAnimators = new Dictionary<int, Animator>();
	
	protected JSON[] pickemsSelected = null;
	protected JSON[] pickemsUnselected = null;
	private int currentPickemIndex = 0;
	protected string wheelWinId = "";
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Start of setup functions.

	public override void init()
	{
		closeHandler.registerEventDelegate(closeClicked);
		collectClickHandler.registerEventDelegate(shareClicked);
		isShowing = true;

		trackStat("secret_wheel_game", "", "view");		

		baseBetAmount = SlotBaseGame.instance.betAmount;
		
		setBetLabels();

		bottomMultiplierLabel.text = "";
		bottomWinAmountLabel.text = "";

		if (SlotsPlayer.isFacebookUser || Sharing.isAvailable)
		{
			collectButtonLabel.text = Localize.textUpper("collect_amp_share");
		}
		else
		{
			collectButtonLabel.text = Localize.textUpper("collect");
		}

		mapDownloadedTextures();

		// Make sure everything is hidden by default.
		transitionAnimator.gameObject.SetActive(false);
		gamePanelMain.gameObject.SetActive(false);
		summaryParent.SetActive(false);
		wheelButtonCollider.enabled = false;
		
		// Parse the outcome data.
		JSON outcome = dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as JSON;
		JSON[] rounds = outcome.getJsonArray("rounds");
		if (rounds.Length < 2)
		{
			// Should never happen on production, or shit's really fucked up.
			Debug.LogError("Didn't find 2 rounds in the big slice mystery gift outcome.");
			return;
		}
		
		// First round is the pickem.
		JSON pickemJSON = rounds[0];
		pickemsSelected = pickemJSON.getJsonArray("selected");
		pickemsUnselected = pickemJSON.getJsonArray("unselected");

		pickemChoices.Add(pickemChoiceTemplate);
	
		// Create all the pickem choice objects from the template one.
		for (int i = 1; i < pickemsSelected.Length + pickemsUnselected.Length; i++)
		{
			GameObject go = NGUITools.AddChild(pickemChoiceTemplate.transform.parent.gameObject, pickemChoiceTemplate.gameObject);			
			pickemChoices.Add(go.GetComponent<BigSlicePickemChoice>());
			pickemShaker.addAnimator(go.GetComponent<Animator>());
		}
		
		// Position the grid of all the new pickem options.
		if (pickemGrid != null)
		{
			pickemGrid.repositionNow = true;
		}
		
		// Make a copy of the list to know what hasn't been revealed so far.
		unrevealedChoices = new List<BigSlicePickemChoice>(pickemChoices);

		// Second round is the wheel. Find the wheel's win wedge.
		JSON wheelJSON = rounds[1];
		JSON[] wheelSelected = wheelJSON.getJsonArray("selected");
		if (wheelSelected == null)
		{
			// This should never happen.
			Debug.LogError("No \"selected\" array found in round 2 of big slice outcome data.");
			return;
		}

		assignWinningSlices(wheelSelected);
	
		// Set up the wedge labels with the initial values.
		// Round 2 has the wheel data.
		string paytableKey = outcome.getString("bonus_game_pay_table", "");
		JSON wheelPaytable = BonusGamePaytable.findPaytable("base_bonus", paytableKey);
		rounds = wheelPaytable.getJsonArray("rounds");
		if (rounds.Length < 2)
		{
			// Should never happen on production, or shit's really fucked up.
			Debug.LogError("Didn't find 2 rounds in the " + paytableKey + " bonus game paytable.");
			return;
		}
		JSON[] wheelValues = rounds[1].getJsonArray("wins");
		if (wheelValues.Length != wheelWedgeLabels.Length)
		{
			Debug.LogError("The number of wheel wedges doesn't match the number of wins in the " + paytableKey + " paytable for big slice.");
			return;
		}

		for (int i = 0; i < wheelWedgeLabels.Length; i++)
		{
			string wedgeId = wheelValues[i].getString("id", "");
			long wedgeValue = wheelValues[i].getLong("credits", 0L);
			
			wheelWedgeIds.Add(wedgeId);

			initWedge(wedgeId, wedgeValue, i);
		}

		startTransitionAudio();
	}

	protected virtual void startTransitionAudio()
	{
		Audio.play(AUDIO_TRANSITION_START);
	}	

	protected virtual void assignWinningSlices(JSON[] wheelSelected)
	{
		// Big slice only has 1 winning slice
		wheelWinId  = wheelSelected[0].getString("win_id", "");    // winning slice at 12 o'clock
		multiplier = wheelSelected[0].getLong("credits", 1L);
	}

	protected virtual void trackStat(string kingdom, string family, string genus)
	{
		// big slice has no tracking
	}

	protected virtual void mapDownloadedTextures()
	{
		// Apply the downloaded textures.
		downloadedTextureToRenderer(wheelRenderer, 0);
		downloadedTextureToRenderer(backgroundRenderer, 1);
		downloadedTextureToRenderer(summaryBackgroundRenderer, 2);
	}

	protected virtual void initWedge(string wedgeId, long wedgeValue, int index)
	{
			if (wedgeValue == BIG_SLICE_DEFAULT_MULTIPLIER)
			{
				StartCoroutine(addBigSlice(
					wedgeValue,
					wedgeId,
					false
				));
			}
			else
			{
				setWedgeMultiplier(index, wedgeValue);
			}
	}
	
	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		
		StartCoroutine(fadeInAfterTransition());
	}

	// Wait for the transition animation to finish, then fade in the pickem game.
	protected IEnumerator playTransition()
	{
		Debug.LogWarning("starting transition at " + Time.realtimeSinceStartup);
		transitionAnimator.gameObject.SetActive(true);
		
		// Wait a frame to get the animation length to wait.
		yield return null;
		yield return null;
		
		Debug.LogWarning("transition anim: " + transitionAnimator.GetCurrentAnimatorStateInfo(0).length + ", overlap: " + GAME_FADE_OVERLAP);

		float waitTime = transitionAnimator.GetCurrentAnimatorStateInfo(0).length - GAME_FADE_OVERLAP;

		yield return new WaitForSeconds(waitTime);
	}		
	
	// Wait for the transition animation to finish, then fade in the pickem game.
	protected virtual IEnumerator fadeInAfterTransition()
	{
		yield return StartCoroutine(playTransition());

		startFadeIn();
		
		yield return new WaitForSeconds(GAME_FADE_OVERLAP);
		
		isWaitingForTouch = true;
	}

	protected new void startFadeIn()
	{
		// Start fading in the game.
		Audio.play(AUDIO_BACKGROUND_MUSIC);
		Audio.play(AUDIO_TRANSITION_END);
		updateGameFade(0.0f);
		gamePanelMain.gameObject.SetActive(true);

		iTween.ValueTo(gameObject, iTween.Hash(
			"from", 0.0f,
			"to", 1.0f,
			"time", GAME_FADE_IN_TIME,
			"onupdate", "updateGameFade"
		));
	}
	
	private void updateGameFade(float alpha)
	{
		gamePanelMain.alpha = alpha;
		gamePanelForeground.alpha = alpha;
		CommonRenderer.alphaRenderer(backgroundRenderer, alpha);
		foreach (BigSlicePickemChoice choice in pickemChoices)
		{
			CommonRenderer.alphaRenderer(choice.coin.GetComponent<Renderer>(), alpha);			
		}
	}
	
	// End of setup functions.
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Start of gift pickem functions

	// Clicked a choice in the pick game.
	private void choiceClicked(GameObject go)
	{
		if (!isWaitingForTouch)
		{
			return;
		}

		if (go == null)
		{
			Debug.LogError("BigSliceDialog: FATAL invalid gameobject selected for pickem game!");
			close();
			return;
		}
		
		StartCoroutine(revealChoicePick(go));
	}
	
	// Reveal a gift pick, then do whatever comes next based on the pick.
	private IEnumerator revealChoicePick(GameObject go)
	{
		isWaitingForTouch = false;
		
		Audio.play(AUDIO_COIN_PICK);

		// Temporarily turn off the pickme shaker for all gifts now.
		pickemShaker.disableShaking = true;

		go.GetComponent<Collider>().enabled = false;

		BigSlicePickemChoice choice = go.GetComponent<BigSlicePickemChoice>();
		unrevealedChoices.Remove(choice);
		pickemShaker.removeAnimator(choice.animator);

		JSON pick = pickemsSelected[currentPickemIndex];
		currentPickemIndex++;
		
		int multiplierChange = pick.getInt("multiplier", 0);

		if (multiplierChange > 0)
		{
			betMultiplier = multiplierChange;
		}
				
		// Show the reveal animation for the picked coin.
		yield return StartCoroutine(choice.pick(pick));
		
		bool shouldPickAgain = true;
		
		if (multiplierChange > 0)
		{
			// Got the "double bet" pick.
			Audio.play(AUDIO_REVEAL_DOUBLE_BET);
			yield return StartCoroutine(flyFireball(fireball, sparkleExplosion, choice.transform, bottomTotalBetLabel.transform, setBetLabels));
			setHeaderText(Localize.textUpper("wager_doubled"));
		}
		else
		{
			JSON[] modifiers = pick.getJsonArray("modifiers");
			
			if (modifiers == null || modifiers.Length == 0)
			{
				// This is the final pick/
				shouldPickAgain = false;
				
				Audio.play(AUDIO_REVEAL_SPIN);

				// Reveal the remaining unpicked gifts as desaturated without animation.
				yield return StartCoroutine(revealAllChoices());

				Audio.play(AUDIO_PRE_SPIN);
				wheelButtonCollider.enabled = true;
				setHeaderText(Localize.textUpper("good_luck"));
				
				// Spin the wheel automatically instead of waiting for a touch.
				StartCoroutine(spinWheel());
			}
			else
			{
				// Modify the wheel's values.
				foreach (JSON modJSON in modifiers)
				{
					yield return StartCoroutine(modifyWheel(modJSON));
				}
			}
		}
		
		isWaitingForTouch = shouldPickAgain;
		
		if (shouldPickAgain)
		{
			pickemShaker.disableShaking = false;
		}
	}
		
	private IEnumerator revealAllChoices()
	{
		pickemShaker.disableShaking = true;
		
		SkippableWait revealWait = new SkippableWait();
		
		for (int i = 0; i < pickemsUnselected.Length; i++)
		{
			if (unrevealedChoices.Count == 0)
			{
				Debug.LogWarning("Found more mystery gift reveals than gifts to reveal them with.");
				break;
			}
		
			unrevealedChoices[0].GetComponent<Collider>().enabled = false;
			unrevealedChoices[0].reveal(pickemsUnselected[i]);
			unrevealedChoices.RemoveAt(0);
			if (!revealWait.isSkipping)
			{
				Audio.play(AUDIO_REVEAL_UNCHOSEN);
			}
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_CHOICE_REVEALS));
		}
	}
	
	// Modifies the values on the wheel based on the modifier data.
	private IEnumerator modifyWheel(JSON modJSON)
	{
		JSON[] modifiers = modJSON.getJsonArray("win_modifiers");

		switch (modJSON.getString("key_name", ""))
		{
			case "big_slice":
			case "big_slice_new_one":
				yield return StartCoroutine(addBigSlice(
					modifiers[0].getLong("credits_after_replace", 0L),
					modifiers[0].getString("win_id", "")
				));
				break;
			case "plus_1_all":
			case "plus_1_even_new_one":
			case "plus_1_odd_new_one":
			case "double_up":
				yield return StartCoroutine(upgradeAllSlices(modifiers));
				break;
		}

		// set the final header text beyond what upgradeAllSlices may have done
		switch (modJSON.getString("key_name", ""))
		{
			case "plus_1_even_new_one":
				setHeaderText(Localize.textUpper("secret_wheel_gold_slices_plus_{0}", CommonText.formatNumber(1)));
				break;
			case "plus_1_odd_new_one":
				setHeaderText(Localize.textUpper("secret_wheel_copper_slices_plus_{0}", CommonText.formatNumber(1)));
				break;
			case "double_up":
				setHeaderText(Localize.textUpper("secret_wheel_double_up_slices"));
				break;				
		}		
	}
		
	protected virtual void setWedgeMultiplier(int wedgeIndex, long multValue)
	{
		wheelWedgeLabels[wedgeIndex].text = CommonText.makeVertical(Localize.text("{0}X", multValue));
	}

	protected virtual void playWedgeMultiplierAudio()
	{
	}
	
	private IEnumerator upgradeAllSlices(JSON[] modifiers)
	{
		plusOneAnimation.Play("Plus 1");

		Audio.play(AUDIO_PLUS_ONE_ALL_WEDGES);
		
		// Wait a frame to get the animation length to wait.
		// I don't know why, but this one takes more than one frame,
		// so loop until the length exists.
		float animLength = 0.0f;
		while (animLength == 0.0f)
		{
			yield return null;
			animLength = plusOneAnimation.GetCurrentAnimatorStateInfo(0).length;
		}
		float eachDelay = animLength / modifiers.Length;
		
		// Let the particles spew a bit before we start boosting the wheel values,
		// so it looks like the particles are on top of the values when they're boosted.
		yield return new WaitForSeconds(DELAY_BETWEEN_PLUS_1_ANIM_AND_BOOST);
		
		foreach (JSON modifier in modifiers)
		{
			yield return new WaitForSeconds(eachDelay);
			int wedgeIndex = wheelWedgeIds.IndexOf(modifier.getString("win_id", ""));

			long multVal = modifier.getLong("credits_after_add", 0L);
			
			setWedgeMultiplier(wedgeIndex, multVal);

			playWedgeMultiplierAudio();			
		}

		setHeaderText(Localize.textUpper("all_slices_plus_{0}", CommonText.formatNumber(1)));
	}

	
	// Adds a big slice wedge to the wheel.
	protected virtual IEnumerator addBigSlice(long wedgeValue, string wedgeId, bool isUpgrade = true)
	{
		Audio.play(AUDIO_REVEAL_BIG_SLICE);

		int wedgeIndex = wheelWedgeIds.IndexOf(wedgeId);
				
		GameObject go = NGUITools.AddChild(wheelWedgeLabels[wedgeIndex].transform.parent.gameObject, bigSliceUpgradePrefab);
		
		// Since AddChild sets the local position to 0,0,0 we have to reset the default z.
		CommonTransform.setZ(go.transform, bigSliceUpgradePrefab.transform.localPosition.z);
		
		yield return null;
		BigSliceWedge wedgeScript = go.GetComponent<BigSliceWedge>();
		
		if (wedgeScript == null)
		{
			Debug.LogError("big slice wedgeScript is null", go);
		}
		
		// This wedge's label is now the one we just added with the big slice.
		wheelWedgeLabels[wedgeIndex] = wedgeScript.label;
		setWedgeMultiplier(wedgeIndex, wedgeValue);
		
		Animator wedgeAnimator = go.GetComponent<Animator>();
		bigSliceAnimators.Add(wedgeIndex, wedgeAnimator);
		
		if (isUpgrade)
		{
			wedgeAnimator.Play("Big Slice Upgrade");
			
			// The "Big Slice Upgrade" animation should play automatically when instantiated.
			// Wait a frame to get the animation length to wait.
			yield return null;
			yield return new WaitForSeconds(wedgeAnimator.GetCurrentAnimatorStateInfo(0).length);

			setHeaderText(Localize.textUpper("big_slice_added"));
		}
	}
	
	protected void setHeaderText(string text)
	{
		headerLabel.text = text;
		
		// Throb the text whenever it changes.
		StartCoroutine(CommonEffects.throb(headerLabelSizer, 1.5f, 0.5f));
	}

	// End of pickem functions
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Start of wheel functions

	protected virtual IEnumerator playPreSpinWheelFx()
	{
		yield return null;
	}

	protected virtual IEnumerator playBigSliceWinFx(int winIndex)
	{
		if (multiplier >= BIG_SLICE_DEFAULT_MULTIPLIER)
		{
			Audio.play(AUDIO_WHEEL_STOP_BIG_SLICE);	

			// The wheel landed on one of the big slices, so animate it in a special way before rolling up.
			bigSliceAnimators[winIndex].Play("Big Slice Widen");
			// Wait a frame to get the animation length to wait.
			yield return null;
			yield return new WaitForSeconds(bigSliceAnimators[winIndex].GetCurrentAnimatorStateInfo(0).length);
		}
		else
		{
			Audio.play(AUDIO_WHEEL_STOP);	
		}		
	}
			
	private IEnumerator spinWheel()
	{
		isWaitingForTouch = false;

		wheelSpinParticles.SetActive(true);

		Audio.playMusic(AUDIO_WHEEL_MUSIC);

		int winIndex = wheelWedgeIds.IndexOf(wheelWinId);

		float degreesPerSlice = 360.0f / wheelWedgeLabels.Length;

		float finalDegrees = winIndex * degreesPerSlice;
		WheelSpinner wheelSpinner = new WheelSpinner(wheelSpin, finalDegrees, null);
		
		yield return StartCoroutine(wheelSpinner.waitToStop());
		
		// Make sure that the sound stops.
		Audio.stopSound(Audio.findPlayingAudio(AUDIO_WHEEL_DECELERATE));
		
		// The wheel stopped spinning. Do some visual stuff before wrapping up.
		yield return StartCoroutine(playBigSliceWinFx(winIndex));

		
		yield return StartCoroutine(playParticleFx(winIndex));

		yield return new WaitForSeconds(TOTAL_AMOUNT_ROLLUP_TIME + DELAY_BEFORE_SUMMARY);
		yield return StartCoroutine(showSummary());
	}

	protected virtual IEnumerator playParticleFx(int srcIndex)
	{
		yield return StartCoroutine(flyFireball(fireball, sparkleExplosion, wheelWedgeLabels[srcIndex].transform, bottomMultiplierLabel.transform, setMultiplierLabel));
	}

	// End of wheel functions.		
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Start of summary functions.

	protected virtual IEnumerator showSummary()
	{
		Audio.play(AUDIO_SUMMARY);

		// Tell the server the player saw the bonus summary screen.
		if (BonusGamePresenter.HasBonusGameIdentifier())
			SlotAction.seenBonusSummaryScreen(BonusGamePresenter.NextBonusGameIdentifier());
		else
		{
			Debug.LogWarning("Not sure how we got to a big slice summary screen without an eventID. Maybe the server didn't send it down earlier?");
		}

		// Make sure the bonus game is hidden now.
		gamePanelMain.gameObject.SetActive(false);
			
		collectButton.SetActive(false);
		closeButton.SetActive(false);
		
		summaryParent.SetActive(true);

		if (summaryTotalBetLabel != null)	// Big Slice uses this, Secret Wheel does not.
		{
			// Hide the summary labels by default, so they can be shown with some delay between each.
			summaryTotalBetLabel.gameObject.SetActive(false);
			summaryMultiplierLabel.gameObject.SetActive(false);
			summaryWinAmountLabel.gameObject.SetActive(false);

			
			yield return new WaitForSeconds(SUMMARY_DELAY);
			
			Audio.play(Audio.soundMap(AUDIO_SUMMARY_REVEAL));
			summaryTotalBetLabel.gameObject.SetActive(true);
			
			yield return new WaitForSeconds(SUMMARY_DELAY);

			Audio.play(Audio.soundMap(AUDIO_SUMMARY_REVEAL));
			summaryMultiplierLabel.gameObject.SetActive(true);

			yield return new WaitForSeconds(SUMMARY_DELAY);
			
			Audio.play(Audio.soundMap(AUDIO_SUMMARY_REVEAL));
			summaryWinAmountLabel.gameObject.SetActive(true);
		}
		
		collectButton.SetActive(!Sharing.isAvailable);		// if native sharing active don't show button
		closeButton.SetActive(true);
		
		// Flash the final win amount forever until the dialog is closed.
		while (true && summaryTotalBetLabel != null)
		{
			summaryWinAmountLabel.alpha = Mathf.Floor(CommonEffects.pulsateBetween(0.5f, 1.5f, 8));
			yield return null;
		}
	}

	// NGUI button callback.
	protected virtual void shareClicked(Dict args = null)
	{
		creditAndClose();
	}
	
	// NGUI button callback.
	private void closeClicked(Dict args = null)
	{
		trackStat("share_secret_wheel_win", "close", "click");		

		creditAndClose();
	}
	
	protected void creditAndClose()
	{
		SlotsPlayer.addCredits(totalWin, "big slice", false);
		Dialog.close();
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.playBgMusic();
		}
	}
	
	// End of summary functions.
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	// General functions.

	protected IEnumerator flyFireball(ParticleSystem fireball, ParticleSystem sparkleExplosion, Transform from, Transform to, GenericDelegate explodeCallback = null)
	{
		Audio.play(AUDIO_FIREBALL_START);

		Vector3 fromPos = from.position;
		Vector3 toPos = to.position;
		fromPos.z = fireball.transform.position.z;
		toPos.z = fireball.transform.position.z;
		
		fireball.transform.position = fromPos;

		// Workaround for Unity bug.
		// If you don't deactivate and reactivate the emitter object before emitting again,
		// there will be some stray particles emitted between the last emitted position and the new one.
		fireball.gameObject.SetActive(false);
		fireball.gameObject.SetActive(true);
		
		fireball.Play();

		iTween.MoveTo(fireball.gameObject, iTween.Hash("x", toPos.x, "y", toPos.y, "time", MULTIPLIER_FLY_TIME, "easetype", iTween.EaseType.linear));
		
		yield return new WaitForSeconds(MULTIPLIER_FLY_TIME);
	
		fireball.Stop();
		
		if (explodeCallback != null)
		{
			explodeCallback();
		}
		
		sparkleExplosion.transform.position = toPos;
		sparkleExplosion.Play();
		
		Audio.play(AUDIO_FIREBALL_LAND);
		
		// Wait for the sparkles to be seen.
		yield return new WaitForSeconds(FIREBALL_EXPLOSION_DELAY);
	}

	// Set both of the bet labels to the current bet amount, with possible multiplier factored in.
	private void setBetLabels()
	{
		if (summaryTotalBetLabel != null)
		{
			summaryTotalBetLabel.text = CreditsEconomy.convertCredits(baseBetAmount * betMultiplier);
		}
		if (bottomTotalBetLabel != null)
		{
			bottomTotalBetLabel.text = CreditsEconomy.convertCredits(baseBetAmount * betMultiplier);
		}		
	}

	// Sets both of the multiplier labels to the current multiplier.
	private void setMultiplierLabel()
	{
		StartCoroutine(setMultiplierLabelCoroutine());
	}
	
	private IEnumerator setMultiplierLabelCoroutine()
	{
		if (summaryMultiplierLabel != null)
		{
			summaryMultiplierLabel.text = Localize.text("{0}X", CommonText.formatNumber(multiplier));
		}
		bottomMultiplierLabel.text = CommonText.formatNumber(multiplier);
		
		totalWin = baseBetAmount * betMultiplier * multiplier;
		if (summaryWinAmountLabel != null)
		{
			summaryWinAmountLabel.text = CreditsEconomy.convertCredits(totalWin);
		}

		yield return new WaitForSeconds(DELAY_BEFORE_ROLLUP);

		Audio.play(AUDIO_ROLLUP);

		yield return StartCoroutine(SlotUtils.rollup(
			0L,
			totalWin,
			bottomWinAmountLabel,
			false,
			TOTAL_AMOUNT_ROLLUP_TIME
		));

		Audio.play(AUDIO_ROLLUP_END);
	}
	
	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		isShowing = false;
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		isShowing = false;
	}

	public static void showDialog(JSON outcomeJSON)
	{
		Dict args = Dict.create(
			D.CUSTOM_INPUT, outcomeJSON,
			D.PRIORITY, SchedulerPriority.PriorityType.IMMEDIATE
		);

		string[] texturePaths = new string[]
		{
			"misc_dialogs/Big_Slice_Wheel.jpg",
			"misc_dialogs/Big_Slice_Game_BG.jpg",
			"misc_dialogs/Big_Slice_Summary_BG.jpg"			
		};
				
		Dialog.instance.showDialogAfterDownloadingTextures("big_slice", texturePaths, args);

	}
}
