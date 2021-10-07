using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Controls the dialog for showing mystery gift game and sharing the news about it.
*/

public abstract class MysteryGiftBaseDialog : DialogBase, IResetGame
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private ClickHandler wheelClickHandler;
	// =============================
	// PROTECTED
	// =============================
	protected int numWheelSlices = 12;
	protected float wheelDegreesPerSlice { get { return 360.0f / numWheelSlices; } }

	protected List<MysteryGiftBasePickem> pickemGifts = new List<MysteryGiftBasePickem>();	// The first element in this list is the template gift.
	protected List<MysteryGiftBaseMatch> scratchStars = new List<MysteryGiftBaseMatch>();	// The first element in this list is the template star.
	protected bool isWaitingForTouch = false;
	protected int questTier = 0;
	protected long baseBetAmount = 0L;	// Can be from the last spin's bet, or from a quest reference wager if triggered by gettin a quest collectible.
	protected long betMultiplier = 1L;	// This can increase, usually only by 1.
	protected long multiplier = 1L;		// The awarded multiplier.
	protected long totalWin = 0L;			// This comes from betAmount * betMultiplier * multiplier. Multiplier is taken from the bonus game winnings instead of credits.

	protected WheelOutcome giftPickOutcome = null;
	protected WheelOutcome wheelOutcome = null;
	protected WheelPick wheelPick = null;
	protected PickemOutcome scratchOutcome = null;
	protected Dictionary<long, bool> scratchPicks = new Dictionary<long, bool>();
	protected List<MysteryGiftBasePickem> unrevealedGifts = null;
	protected List<MysteryGiftBaseMatch> unrevealedStars = null;

	// Audio Keys to be overwritten in subclasses. Defaults to HIR
	protected string initSound = "MGinit";
	protected string pickAPresentBG = "MGPickAPresentBg";
	protected string revealDobuleRePick = "MGRevealDoubleRePick";
	protected string revealPremium = "MGRevealPremium";
	protected string pickPresent = "MGPickPresent";
	protected string wheelPreSpin = "MGWheelPrespin";
	protected string wheelSlowDown = "MGWheelSlowDown";
	protected string wheelStops = "MGWheelStops";
	protected string pickAStarBG = "MGPickAStarBg";
	protected string pickAStar = "MGPickStar";
	protected string summaryFanfare = "MGSummaryFanfare";
	protected string multiplierMove = "MGMultiplierMove";
	protected string revealMultiplier = "MGRevealMultiplier";
	protected string multiplierLandEnding = "MGMultiplierLandEnding";

	// =============================
	// PUBLIC
	// =============================
	public static bool isShowing = false;	// Is this dialog currently open anywhere on the dialog stack?

	public Animator pickTransition;
	public UIPanel dialogPanel;	// Used for fading in the dialog after the transition animation finishes.
	public GameObject gameCommonParent;
	public GameObject giftPickParent;
	public GameObject wheelParent;
	public GameObject scratchParent;
	public GameObject summaryParent;
	
	// The labels at the bottom, not on the summary screen.
	public TextMeshPro bottomTotalBetLabel;
	public TextMeshPro bottomMultiplierLabel;
	public TextMeshPro bottomWinAmountLabel;
	// The labels on the summary screen.
	public TextMeshPro summaryTotalBetLabel;
	public TextMeshPro summaryMultiplierLabel;
	public TextMeshPro summaryWinAmountLabel;
	
	public GameObject collectButton;
	public TextMeshPro collectButtonLabel;
	public ParticleSystem fireball;
	public ParticleSystem sparkleExplosion;

	public MysteryGiftBasePickem giftTemplate;
	public Transform[] pickemGiftPositions;
	public PickemButtonShakerMaster pickemShakerMaster;
	
	public TextMeshPro[] wheelWedgeLabels;
	public Animator wheelHubAnimator;
	public GameObject wheelSpin;
	
	public MysteryGiftBaseMatch starTemplate;
	public Transform[] scratchStarPositions;
	public PickemButtonShakerMaster starShakerMaster;

	// =============================
	// CONST
	// =============================
	protected const float TIME_BETWEEN_GIFT_REVEALS = 0.25f;
	protected const float TIME_BETWEEN_STAR_REVEALS = 0.25f;
	protected const float TIME_AFTER_REVEALS = 1.0f;
	protected const float MULTIPLIER_FLY_TIME = 0.5f;
	protected const float SUMMARY_DELAY = 0.5f;
	protected const string SCRATCH_BONUS_GAME = "mystery_gift_scratch_card";
	protected const string WHEEL_BONUS_GAME = "mystery_gift_wheel";

	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Start of setup functions.

	public override void init()
	{
		// Override to do special loading stuff.
		isShowing = true;

		if (wheelClickHandler != null)
		{
			wheelClickHandler.registerEventDelegate(wheelClicked);
		}

		long initialBet = (long) dialogArgs.getWithDefault(D.BET_CREDITS, 0L);
		if (initialBet != 0)
		{
			// this is for SIR LikelyToLapse feature, which sends a bet amount not related to current slot game
			// (ask Murali for details)
			baseBetAmount = initialBet;
		}
		else
		{
			// normal flow
			baseBetAmount = SlotBaseGame.instance.betAmount;
		}
		
		setBetLabels();
		bottomMultiplierLabel.text = "";
		bottomWinAmountLabel.text = "";
		
		pickemGifts.Add(giftTemplate);
		pickemShakerMaster.addAnimator(giftTemplate.animator);
		// Create all the gift objects from the template one.
		for (int i = 1; i < pickemGiftPositions.Length; i++)
		{
			GameObject go = CommonGameObject.instantiate(giftTemplate.gameObject) as GameObject;
			MysteryGiftBasePickem gift = go.GetComponent<MysteryGiftBasePickem>();
			go.transform.parent = giftTemplate.transform.parent;
			go.transform.localScale = Vector3.one;
			go.transform.position = pickemGiftPositions[i].position;
			gift.setup(i);
			pickemGifts.Add(gift);
			pickemShakerMaster.addAnimator(gift.animator);
		}
		
		// Make a copy of the list to know what hasn't been revealed so far.
		unrevealedGifts = new List<MysteryGiftBasePickem>(pickemGifts);

		giftPickOutcome = dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as WheelOutcome;

		if (SlotsPlayer.isFacebookUser || Sharing.isAvailable)
		{
			collectButtonLabel.text = Localize.textUpper("collect_amp_share");
		}
		else
		{
			collectButtonLabel.text = Localize.textUpper("collect");
		}

		// Make sure everything is hidden by default.
		pickTransition.gameObject.SetActive(false);
		gameCommonParent.SetActive(false);
		giftPickParent.SetActive(false);
		wheelParent.SetActive(false);
		scratchParent.SetActive(false);
		summaryParent.SetActive(false);
		
		Audio.play(initSound);
	}
	
	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		
		StartCoroutine(fadeInAfterTransition());
	}
	
	// Wait for the transition animation to finish, then fade in the pickem game.
	protected IEnumerator fadeInAfterTransition()
	{
		const float FADE_TIME = 0.25f;	// The amount of time for fading out the gift in the animation, and fading in the pickem game.
		
		pickTransition.gameObject.SetActive(true);
		
		// Need to wait one frame for the animation to start before we can get the length of it in the next line.
		yield return null;
		yield return new WaitForSeconds(pickTransition.GetCurrentAnimatorStateInfo(0).length - FADE_TIME);

		dialogPanel.alpha = 0.0f;
		gameCommonParent.SetActive(true);
		giftPickParent.SetActive(true);

		Audio.playMusic(pickAPresentBG, shouldLoop:true);
	
		// Start fading in the pickem game.
		float elapsedTime = 0.0f;
		do
		{
			dialogPanel.alpha = elapsedTime / FADE_TIME;
			elapsedTime += Time.deltaTime;
			yield return null;
		} while (elapsedTime < FADE_TIME);

		dialogPanel.alpha = 1.0f;
				
		pickTransition.gameObject.SetActive(false);
		
		isWaitingForTouch = true;
	}
	
	// End of setup functions.
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Start of gift pickem functions

	// Clicked a gift in the gift pick game.
	protected virtual void giftClicked(GameObject go)
	{
		if (!isWaitingForTouch)
		{
			return;
		}
				
		StartCoroutine(revealGiftPick(go));
	}
	
	// Reveal a gift pick, then do whatever comes next based on the pick.
	protected virtual IEnumerator revealGiftPick(GameObject go)
	{
		isWaitingForTouch = false;

		// Turn off the pickme shaker for all gifts now.
		pickemShakerMaster.disableShaking = true;

		go.GetComponent<Collider>().enabled = false;

		MysteryGiftBasePickem gift = go.GetComponent<MysteryGiftBasePickem>();
		unrevealedGifts.Remove(gift);

		WheelPick pick = giftPickOutcome.getNextEntry();
		betMultiplier += pick.multiplier;
		
		// Play the pick sound before starting the pick animation.
		if (pick.canContinue)
		{
			Audio.play(revealDobuleRePick);
		}
		else if (pick.bonusGame != "")
		{
			Audio.play(revealPremium);
		}
		else
		{
			Audio.play(pickPresent);
		}
		
		// Show the reveal animation for the picked gift.
		yield return StartCoroutine(gift.pick(pick));
		
		if (pick.canContinue)
		{
			// Got the "double bet" pick.
			yield return StartCoroutine(flyFireball(gift.transform, bottomTotalBetLabel.transform, setBetLabels));
		}
		else
		{
			// This is the final pick, so reveal the remaining unpicked gifts as desaturated without animation.
			yield return StartCoroutine(revealAllGifts(pick));

			multiplier = pick.baseCredits;
			
			switch (pick.bonusGame)
			{
				case WHEEL_BONUS_GAME:
					// Go to the wheel game after the reveal.
					goToWheelGame();
					break;
				case SCRATCH_BONUS_GAME:
					// Go to the scratch card game after the reveal.
					goToScratchGame();
					break;
				default:
					// No bonus game, so just give the multiplier that was picked.
					yield return StartCoroutine(flyFireball(gift.transform, bottomMultiplierLabel.transform, setMultiplierLabel));
					yield return StartCoroutine(showSummary());
					break;
			}
		}
				
		isWaitingForTouch = true;
	}
	
	protected virtual IEnumerator revealAllGifts(WheelPick pick)
	{
		SkippableWait revealWait = new SkippableWait();
				
		for (int i = 0; i < pick.wins.Count; i++)
		{
			if (pick.winIndex != i)
			{
				if (unrevealedGifts.Count == 0)
				{
					Debug.LogWarning("Found more mystery gift reveals than gifts to reveal them with.");
					break;
				}
			
				unrevealedGifts[0].reveal(pick.wins[i]);
				unrevealedGifts.RemoveAt(0);
				if (!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap("reveal_not_chosen"));
				}
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_GIFT_REVEALS));
			}
		}
		
		// Add slight pause after all the reveals, even if skipped.
		yield return new WaitForSeconds(TIME_AFTER_REVEALS);
	}
	
	// End of gift pickem functions
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Start of wheel functions
	
	protected virtual void goToWheelGame()
	{
		Audio.playMusic(wheelPreSpin, shouldLoop:true);

		SlotOutcome baseOutcome = (Quest.rewardOutcome == null) ? null : 
			SlotOutcome.getBonusGameOutcome(new SlotOutcome(Quest.rewardOutcome), WHEEL_BONUS_GAME, false, true);
		
		if (baseOutcome == null)
		{
			baseOutcome = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, WHEEL_BONUS_GAME, false, true);
		}
		wheelOutcome = (baseOutcome == null) ? null : new WheelOutcome(baseOutcome);

		if (wheelOutcome == null)
		{
			// with some outcomes that have multiple bonus games fail trying to find the wheel game from currentBonusGameOutcome so try to find it this way
			baseOutcome = SlotOutcome.getBonusGameOutcome(SlotBaseGame.instance.outcome, WHEEL_BONUS_GAME, false, true);
			if (baseOutcome == null)
			{
				Debug.LogError("MysteryGiftBaseDialog.goToWheelGame() : base bonus game outcome is null.");
				return;
			}
			else
			{
				wheelOutcome = new WheelOutcome(baseOutcome);
				if (wheelOutcome == null) 
				{
					Debug.LogError("MysteryGiftBaseDialog.goToWheelGame() : wheel outcome is null.");
					return;
				}
			}
		}

		wheelPick = wheelOutcome.getNextEntry();
		multiplier = wheelPick.baseCredits;

		// Set up the slices with possible win values.
		for (int i = 0; i < wheelPick.wins.Count; i++)
		{
			wheelWedgeLabels[i].text = CommonText.makeVertical(Localize.text("{0}X", wheelPick.wins[i].baseCredits));
		}

		giftPickParent.SetActive(false);
		wheelParent.SetActive(true);
		
		wheelHubAnimator.Play("wheel_hub_pickme");
	}
	
	// Start spinning the wheel.
	protected virtual void wheelClicked(Dict args = null)
	{
		if (!isWaitingForTouch)
		{
			return;
		}
		
		StartCoroutine(spinWheel());
	}

	protected virtual IEnumerator spinWheel()
	{
		isWaitingForTouch = false;

		wheelHubAnimator.Play("wheel_hub_q_mark");

		Audio.playMusic(wheelSlowDown);

		float finalDegrees = wheelPick.winIndex * wheelDegreesPerSlice;
		WheelSpinner wheelSpinner = new WheelSpinner(wheelSpin, finalDegrees, null);

		yield return StartCoroutine(wheelSpinner.waitToStop());
		Audio.play(wheelStops);

		// Make sure that the sound stops.
		Audio.stopSound(Audio.findPlayingAudio("wheel_decelerate"));
		
		// The wheel stopped spinning. Do some visual stuff before wrapping up.
		yield return StartCoroutine(flyFireball(wheelWedgeLabels[wheelPick.winIndex].transform, bottomMultiplierLabel.transform, setMultiplierLabel));
		yield return StartCoroutine(showSummary());
	}
	
	// End of wheel functions.		
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Start of scratch functions.
	
	protected virtual void goToScratchGame()
	{
		Audio.playMusic(pickAStarBG, shouldLoop:true);
		
		/*
		  MRCC -- Adding in a check for whether this is a MultiSlot game because the way
		  those outcomes come in doesn't put it in the same place as we originally were reading it from (its not from the specific game that got the bonus, but from the parent game.
		  This was a fix for SIR-2900
		*/
			
		if (ReelGame.activeGame is MultiSlotBaseGame)
		{
			scratchOutcome = new PickemOutcome(SlotOutcome.getBonusGameOutcome(SlotBaseGame.instance.outcome, SCRATCH_BONUS_GAME, false, true));
		}
		else
		{			
			scratchOutcome = new PickemOutcome(SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, SCRATCH_BONUS_GAME, false, true));			
			if (scratchOutcome == null)
			{
				// with some outcomes that have multiple bonus games fail trying to find the scratch game from currentBonusGameOutcome so try to find it this way
				scratchOutcome = new PickemOutcome(SlotOutcome.getBonusGameOutcome(SlotBaseGame.instance.outcome, SCRATCH_BONUS_GAME, false, true));
			}
		}			

		if (scratchOutcome == null)
		{
			Debug.LogError("Null scratch outcome!");
		}

		scratchStars.Add(starTemplate);
		starShakerMaster.addAnimator(starTemplate.animator);
		// Create all the star objects from the template one.
		for (int i = 1; i < scratchStarPositions.Length; i++)
		{
			GameObject go = CommonGameObject.instantiate(starTemplate.gameObject) as GameObject;
			MysteryGiftBaseMatch scratch = go.GetComponent<MysteryGiftBaseMatch>();
			scratch.setup(i);
			go.transform.parent = starTemplate.transform.parent;
			go.transform.localScale = Vector3.one;
			go.transform.position = scratchStarPositions[i].position;			
			scratchStars.Add(scratch);
			starShakerMaster.addAnimator(scratch.animator);
		}
		
		// Make a copy of the list to know what hasn't been revealed so far.
		unrevealedStars = new List<MysteryGiftBaseMatch>(scratchStars);

		giftPickParent.SetActive(false);
		scratchParent.SetActive(true);
	}
	
	// Clicked a gift in the gift pick game.
	protected virtual void starClicked(GameObject go)
	{
		if (!isWaitingForTouch)
		{
			return;
		}
		StartCoroutine(revealStarPick(go));
	}
	
	// Reveal a star pick, then do whatever comes next based on the pick.
	protected virtual IEnumerator revealStarPick(GameObject go)
	{
		isWaitingForTouch = false;

		Audio.play(pickAStar);
	
		go.GetComponent<Collider>().enabled = false;

		MysteryGiftBaseMatch star = go.GetComponent<MysteryGiftBaseMatch>();
		unrevealedStars.Remove(star);
		starShakerMaster.removeAnimator(star.animator);
		
		PickemPick pick = scratchOutcome.getNextEntry();
		
		yield return StartCoroutine(star.pick(pick));

		if (scratchPicks.ContainsKey(pick.baseCredits))
		{
			// This one was already picked once before, so this is going to be the winner.
			multiplier = pick.baseCredits;

			// Turn off the pickme shaker for all gifts now.
			starShakerMaster.disableShaking = true;
			
			// Reveal the remaining star objects.
			yield return StartCoroutine(revealAllStars());
			yield return StartCoroutine(flyFireball(star.transform, bottomMultiplierLabel.transform, setMultiplierLabel));
			yield return StartCoroutine(showSummary());
		}
		else
		{
			// First time picking this multiplier, so another pick is still needed.
			scratchPicks.Add(pick.baseCredits, true);
			isWaitingForTouch = true;
		}
	}
	
	protected virtual IEnumerator revealAllStars()
	{
		SkippableWait revealWait = new SkippableWait();
				
		PickemPick reveal = scratchOutcome.getNextReveal();
		while (reveal != null)
		{
			if (unrevealedStars.Count == 0)
			{
				Debug.LogWarning("Found more mystery gift star reveals than stars to reveal them with.");
				break;
			}
		
			unrevealedStars[0].reveal(reveal);
			unrevealedStars.RemoveAt(0);
			if (!revealWait.isSkipping)
			{
				Audio.play(Audio.soundMap("reveal_not_chosen"));
			}
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_STAR_REVEALS));

			reveal = scratchOutcome.getNextReveal();
		}
		
		// Add slight pause after all the reveals, even if skipped.
		yield return new WaitForSeconds(TIME_AFTER_REVEALS);
	}

	// End of scratch functions.
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Start of summary functions.

	protected IEnumerator showSummary()
	{
		Audio.play(summaryFanfare);

		// Tell the server the player saw the bonus summary screen.
		if (BonusGamePresenter.HasBonusGameIdentifier())
			SlotAction.seenBonusSummaryScreen(BonusGamePresenter.NextBonusGameIdentifier());
		else
		{
			Debug.LogWarning("Not sure how we got to a mystery gift summary screen without an eventID. Maybe the server didn't send it down earlier?");
		}

		// Make sure all the bonus games are hidden now.
		gameCommonParent.SetActive(false);
		giftPickParent.SetActive(false);
		wheelParent.SetActive(false);
		scratchParent.SetActive(false);
			
		// Hide the summary labels by default, so they can be shown with some delay between each.
		summaryTotalBetLabel.gameObject.SetActive(false);
		summaryMultiplierLabel.gameObject.SetActive(false);
		summaryWinAmountLabel.gameObject.SetActive(false);
		collectButton.SetActive(false);
		
		summaryParent.SetActive(true);
		
		yield return new WaitForSeconds(SUMMARY_DELAY);
		
		Audio.play(Audio.soundMap("bonus_summary_reveal_value"));
		summaryTotalBetLabel.gameObject.SetActive(true);
		
		yield return new WaitForSeconds(SUMMARY_DELAY);

		Audio.play(Audio.soundMap("bonus_summary_reveal_value"));
		summaryMultiplierLabel.gameObject.SetActive(true);

		yield return new WaitForSeconds(SUMMARY_DELAY);
		
		Audio.play(Audio.soundMap("bonus_summary_reveal_value"));
		summaryWinAmountLabel.gameObject.SetActive(true);
		
		collectButton.SetActive(!Sharing.isAvailable);
		
		// Flash the final win amount forever until the dialog is closed.
		while (true)
		{
			summaryWinAmountLabel.alpha = (int)CommonEffects.pulsateBetween(0.5f, 1.5f, 8);
			yield return null;
		}
	}

	// NGUI button callback.
	protected void shareClicked()
	{
		creditAndClose();
	}
	
	// NGUI button callback.
	protected void closeClicked()
	{
		creditAndClose();
	}
	
	protected void creditAndClose()
	{
		SlotsPlayer.addCredits(totalWin, "mystery gift");
		Dialog.close();
	}
	
	// End of summary functions.
	///////////////////////////////////////////////////////////////////////////////////////////////////////////

	///////////////////////////////////////////////////////////////////////////////////////////////////////////
	// General functions.

	protected IEnumerator flyFireball(Transform from, Transform to, GenericDelegate explodeCallback = null)
	{
		Audio.play(multiplierMove);
		Vector3 fromPos = from.position;
		Vector3 toPos = to.position;
		if (fireball != null)
		{
			fromPos.z = fireball.transform.position.z;
			toPos.z = fireball.transform.position.z;
		
			fireball.transform.position = fromPos;
		
			// Workaround for Unity bug.
			// If you don't deactivate and reactivate the emitter object before emitting again,
			// there will be some stray particles emitted between the last emitted position and the new one.
			fireball.gameObject.SetActive(false);
			fireball.gameObject.SetActive(true);
		
			fireball.Play();

		}

		iTween.MoveTo(fireball.gameObject, iTween.Hash("x", toPos.x, "y", toPos.y, "time", MULTIPLIER_FLY_TIME, "easetype", iTween.EaseType.linear));
		
		yield return new WaitForSeconds(MULTIPLIER_FLY_TIME);
	
		fireball.Stop();
		
		if (explodeCallback != null)
		{
			explodeCallback();
		}
		sparkleExplosion.gameObject.SetActive(true);
		sparkleExplosion.transform.position = toPos;
		sparkleExplosion.Play();
		
		// Wait for the sparkles to be seen.
		yield return new WaitForSeconds(2.0f);
		sparkleExplosion.gameObject.SetActive(false);
	}

	// Set both of the bet labels to the current bet amount, with possible multiplier factored in.
	protected void setBetLabels()
	{
		summaryTotalBetLabel.text = CreditsEconomy.convertCredits(baseBetAmount * betMultiplier);
		bottomTotalBetLabel.text = CreditsEconomy.convertCredits(baseBetAmount * betMultiplier);
	}

	// Sets both of the multiplier labels to the current multiplier.
	protected void setMultiplierLabel()
	{
		float delay = Audio.getAudioClipLength(revealMultiplier);
		Audio.play(revealMultiplier);
		Audio.playWithDelay(multiplierLandEnding, delay + 1f);

		summaryMultiplierLabel.text = Localize.text("{0}X", CommonText.formatNumber(multiplier));
		bottomMultiplierLabel.text = CommonText.formatNumber(multiplier);
		
		setTotalWin();
	}
	
	protected void setTotalWin()
	{
		totalWin = baseBetAmount * betMultiplier * multiplier;
		bottomWinAmountLabel.text = CreditsEconomy.convertCredits(totalWin);
		summaryWinAmountLabel.text = CreditsEconomy.convertCredits(totalWin);
		winAmount = totalWin;
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		Audio.stopMusic();
		isShowing = false;
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		isShowing = false;
	}
		
	// Note: initialBet is only used by LikelyToLapse
	public static void showDialog(JSON outcome, long initialBet=0, bool wasLaunchedFromRTR = false)
	{
		Scheduler.addDialog(
			"mystery_gift",
			Dict.create(
				D.CUSTOM_INPUT, new WheelOutcome(new SlotOutcome(outcome)),
				D.DATA, wasLaunchedFromRTR
			),
			// We must force this dialog so that it can be shown before processing normal spin outcomes:
			SchedulerPriority.PriorityType.IMMEDIATE
		);
	}
}
