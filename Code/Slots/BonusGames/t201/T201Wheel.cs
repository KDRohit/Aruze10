using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls the T201 wheel bonus game.
*/
public class T201Wheel : ChallengeGame
{
	/* Wheel Game Items */
	public GameObject carousel;
	public GameObject carouselValues;
	public LabelWrapperComponent[] wheelTextsWrapperComponent;
	public GameObject spinButton;
	public LabelWrapperComponent[] progressivePoolsWrapperComponent;
	public GameObject wheelStartAnimationPrefab;
	public GameObject wheelWinBoxAnimation;
	public GameObject spinTitleText;
	
	/*Pickem Game Items*/
	public GameObject pickemParent;
	public GameObject wheelParent;
	public UISprite progressiveIcon;
	public string[] progressiveIconArray;
	public string[] characterIcons;
	public string[] progressiveItemArray;
	public UISprite[] progressiveItemIcons;
	public LabelWrapperComponent progressiveWinLabelWrapperComponent;
	public LabelWrapperComponent progressiveAmountLabelWrapperComponent;
	public LabelWrapperComponent picksRemainingLabelWrapperComponent;
	public GameObject[] picks;
	public LabelWrapperComponent[] revealsWrapperComponent;
	public GameObject[] explosionEffects;
	public GameObject[] sheenEffects;
	public Animation topPanel;
	public GameObject barExplosion;
	
	private WheelOutcome wheelOutcome;
	private PickemOutcome pickemOutcome = null;
	private WheelSpinner3d wheelSpinner;
	private WheelPick wheelPick;
	private long[] progPool = new long[4];
	private string selectedCharacter;
	private int lastRnd = -1;
	private int numTries = 0;
	private int correctArrayIndex = -1;
	private int stopAngle = 0;
	private List<GameObject> unpickedPicks = new List<GameObject>();
	private bool isDoingPickMeAnims = true;

	private static readonly string[] BASE_PAYOUT_PAYTABLES = {
		"t2_common_pickem_1",
		"t2_common_pickem_2",
		"t2_common_pickem_3",
		"t2_common_pickem_4" };

	public override void init() 
	{
		wheelOutcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] as WheelOutcome;
		wheelPick = wheelOutcome.getNextEntry();
		
		int wheelIdx = 0;
		for (int j = 0; j < wheelPick.wins.Count; j++)
		{
			long credits = wheelPick.wins[j].credits;
			if (credits == 0)
			{
				continue;
			}
			wheelTextsWrapperComponent[wheelIdx].text = CommonText.makeVertical(CreditsEconomy.convertCredits(credits, false));
			wheelIdx++;
		}
		
		JSON[] progressivePoolsJSON = SlotBaseGame.instance.slotGameData.progressivePools; // These pools are from highest to lowest.

		if(progressivePoolsJSON != null && progressivePoolsJSON.Length > 0)
		{
			for (int i = 0; i < progPool.Length; i++)
			{
				progPool[i] = SlotsPlayer.instance.progressivePools.getPoolCredits(progressivePoolsJSON[i].getString("key_name", ""), SlotBaseGame.instance.multiplier, false);
				progressivePoolsWrapperComponent[i].text = CreditsEconomy.convertCredits(progPool[i]);
			}
		}
		else
		{
			for(int i = 0; i < BASE_PAYOUT_PAYTABLES.Length; i++)
			{
				progPool[i] = BonusGamePaytable.getBasePayoutCreditsForPaytable("pickem", BASE_PAYOUT_PAYTABLES[i], "1") * SlotBaseGame.instance.multiplier * GameState.baseWagerMultiplier;
				progressivePoolsWrapperComponent[i].text = CreditsEconomy.convertCredits(progPool[i]);
			}
		}

		for (int i = 0; i < revealsWrapperComponent.Length; i++)
		{
			revealsWrapperComponent[i].alpha = 0;
		}

		int winIndex = wheelPick.winIndex;
		switch (winIndex)
		{
			case 0: //blue
				stopAngle = -45;
			break;
			case 1 : //t1000
				stopAngle = 0;
			break;
			case 2: //white
				stopAngle = 45;
			break;
			case 3 : //arnold
				stopAngle = 90;
			break;
			case 4: //yellow
				stopAngle = 135;
			break;
			case 5 : //sarah
				stopAngle = 180;
			break;
			case 6: // green
				stopAngle = 225;
			break;
			case 7 : //john
				stopAngle = 270;
			break;
		}
		
		Audio.switchMusicKey(Audio.soundMap("wheel_click_to_spin"));
		Audio.play(Audio.soundMap("wheel_click_to_spin"));

		_didInit = true;
	}

	protected override void startGame()
	{
		base.startGame();
		showSpinButton(); // Needs to go in the Start function for Swipe to spin to work b/c camera may not be set in awake.
		Audio.stopMusic();
	}
	
	public void showSpinButton()
	{
#if !UNITY_WEBGL
		gameObject.AddComponent<SwipeableWheel3d>().init(carousel, stopAngle, onSwipeStarted, onWheelSpinComplete);
#endif // UNITY_WEBGL		
		spinButton.SetActive(true);
	}

	private void disableSwipeToSpin()
	{
#if !UNITY_WEBGL
		SwipeableWheel3d swipeableWheel = gameObject.GetComponent<SwipeableWheel3d>();
		if (swipeableWheel != null)
		{
			Destroy(swipeableWheel);
		}
#endif
	}

	// Meshes need to be put together after their positions have been updated.
	protected override void LateUpdate()
	{
		// Makes sure that the two mesh renderes are both locked into the same position.
		carouselValues.GetComponent<Renderer>().materials[0].mainTextureOffset = carousel.GetComponent<Renderer>().materials[0].mainTextureOffset;
		base.LateUpdate();
	}

	protected override void Update()
	{
		base.Update();
		
		if (!_didInit)
		{
			return;
		}
		
		if (wheelSpinner != null)
		{
			wheelSpinner.updateWheel();
		}
	}
	
	public void onWheelSpinComplete()
	{
		StartCoroutine(rollupAndEnd());
		Audio.switchMusicKeyImmediate("");
		disableSwipeToSpin();
	}
	
	// When rollup ends if the outcome is payout show payout
	// Otherwise start the pickem game
	private IEnumerator rollupAndEnd()
	{
		Audio.play("WheelStopsT2");
		wheelWinBoxAnimation.SetActive(true);
		yield return new WaitForSeconds(2.0f);
		long payout = wheelPick.wins[wheelPick.winIndex].credits;
		if (payout > 0)
		{
			BonusGamePresenter.instance.currentPayout = payout;
			endGame();
		}
		else
		{
			SlotOutcome pickemGame = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, wheelPick.bonusGame, false);
			pickemOutcome = new PickemOutcome(pickemGame);
		
			switch (wheelPick.winIndex)
			{
				case 1:
					correctArrayIndex = 0;
					Audio.play("tmYouHavePhotoOfJohn");
					break;
				case 3:
					correctArrayIndex = 3;
					Audio.play("atOfCourseImATerminator");
					break;
				case 5:
					correctArrayIndex = 2;
					Audio.play("scILoveYouJohnIAlwaysHave ");
				break;
				case 7:
					correctArrayIndex = 1;
					Audio.play("jcGotSkynetByTheBallsNow");
					break;
			}
			CommonGameObject.parentsFirstSetActive(progressivePoolsWrapperComponent[correctArrayIndex].gameObject, true);
			yield return new WaitForSeconds(1.0f);
			showPickemGame();
		}
	}
	
	private void showPickemGame()
	{
		Audio.switchMusicKeyImmediate("T2CommonPostWheelBg");
		BonusGamePresenter.instance.useMultiplier = false;
		pickemParent.SetActive(true);
		wheelParent.SetActive(false);

		for (int i = 0; i < picks.Length; i++)
		{
			unpickedPicks.Add(picks[i]);
		}
		
		foreach (Transform t in progressivePoolsWrapperComponent[correctArrayIndex].gameObject.transform)
		{
			t.gameObject.SetActive(false);
		}
		
		progressiveIcon.spriteName = progressiveIconArray[correctArrayIndex];

		for (int i = 0; i < progressiveItemIcons.Length; i++)
		{
			progressiveItemIcons[i].spriteName = progressiveItemArray[correctArrayIndex];
			progressiveItemIcons[i].MakePixelPerfect();
		}
		
		progressiveAmountLabelWrapperComponent.text = progressivePoolsWrapperComponent[correctArrayIndex].text;
		picksRemainingLabelWrapperComponent.text = Localize.textUpper("{0}_picks_remaining", pickemOutcome.entryCount);		
		StartCoroutine(shakeItems());
	}
	
	public IEnumerator revealItem(GameObject button)
	{
		long initialCredits = BonusGamePresenter.instance.currentPayout;
		NGUITools.SetActive(button, false);
		button.GetComponent<Collider>().enabled = false;
		int index = System.Array.IndexOf(picks, button);

		PickemPick pick = pickemOutcome.getNextEntry();

		if (pick.groupId.Length > 0)
		{
			switch (wheelPick.winIndex)
			{
			case 1:
				Audio.play("tmYouHavePhotoOfJohn");
				break;
			case 3:
				Audio.play("atOfCourseImATerminator");
				break;
			case 5:
				Audio.play("scILoveYouJohnIAlwaysHave");
				break;
			case 7:
				Audio.play("jcGotSkynetByTheBallsNow");
				break;
			}
			topPanel.Play("wheel_pick_progressive_win_t2");

			yield return new WaitForSeconds(0.5f);
			NGUITools.SetActive(barExplosion, true);
			NGUITools.SetActive(button, true);
			revealsWrapperComponent[index].alpha = 1;
			revealsWrapperComponent[index].text = "";
			BonusGamePresenter.instance.currentPayout += progPool[correctArrayIndex];
			
			UISprite uiSprite = button.GetComponent<UISprite>();
			if (uiSprite)
			{
				uiSprite.spriteName = characterIcons[correctArrayIndex];
				uiSprite.MarkAsChanged();
				uiSprite.MakePixelPerfect();
				selectedCharacter = uiSprite.spriteName;
			}
			
			StartCoroutine(SlotUtils.rollup(
				initialCredits,
				BonusGamePresenter.instance.currentPayout,
				progressiveWinLabelWrapperComponent.labelWrapper,
				new RollupDelegate(ProgressiveRollUpCallback)
			));
			
			picksRemainingLabelWrapperComponent.text = Localize.textUpper("{0}_picks_remaining", pickemOutcome.entryCount);
		}
		else
		{
			revealsWrapperComponent[index].text = CreditsEconomy.convertCredits(pick.credits * BonusGameManager.instance.currentMultiplier);
			revealsWrapperComponent[index].alpha = 1;
			BonusGamePresenter.instance.currentPayout  += pick.credits * BonusGameManager.instance.currentMultiplier;
			StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, progressiveWinLabelWrapperComponent.labelWrapper));
	
			picksRemainingLabelWrapperComponent.text = Localize.textUpper("{0}_picks_remaining", pickemOutcome.entryCount);
	
			if (numTries >= 3)
			{
				Invoke("revealRemainingPicks", 2.5f);
			}
		}

		for (int i = 0; i < picks.Length; i++)
		{
			// Button must both be active, and have visible reveal text. Even if that visible reveal text is an empty string.
			if ((picks[i].activeSelf && revealsWrapperComponent[i].alpha != 1) && numTries < 3)
			{
				CommonGameObject.setObjectCollidersEnabled(picks[i], true);
			}
		}
	}

	private IEnumerator playReturnAnimation()
	{
		yield return new WaitForSeconds(1.0f);

		topPanel.Play("wheel_pick_progressive_win_return_t2");
		if (numTries >= 3)
		{
			for (int i = 0; i < progressiveItemIcons.Length; i++)
			{
				progressiveItemIcons[i].gameObject.GetComponent<Collider>().enabled = false;
			}
			Invoke("revealRemainingPicks", 2.5f);
		}
		
	}
	
	private void ProgressiveRollUpCallback(long rollupValue)
	{
		if (rollupValue >= BonusGamePresenter.instance.currentPayout)
		{
			StartCoroutine(playReturnAnimation());
		}
	}
	
	public IEnumerator OnPickSelected(GameObject button)
	{
		Audio.play("RollupTermWheelT2");
		numTries ++;
		
		if (numTries >= 3)
		{
			for (int i = 0; i < progressiveItemIcons.Length; i++)
			{
				progressiveItemIcons[i].gameObject.GetComponent<Collider>().enabled = false;
			}

			isDoingPickMeAnims = false;
		}

		NGUITools.SetActive(button, false);
		int index = System.Array.IndexOf(picks, button);

		unpickedPicks.Remove(picks[index]);
		
		if (explosionEffects[index] != null)
		{
			NGUITools.SetActive(explosionEffects[index], true);
		}
		foreach (GameObject icon in picks)
		{
			CommonGameObject.setObjectCollidersEnabled(icon, false);
		}
		yield return new WaitForSeconds(1.0f);
		StartCoroutine(revealItem(button));
	}
	
	private void revealRemainingPicks()
	{
		PickemPick reveal = pickemOutcome.getNextReveal();
		if (reveal == null)
		{
			Invoke("endGame", 1.0f);
			return;
		}
		
		for (int i = 0; i < revealsWrapperComponent.Length; i++)
		{
			if (revealsWrapperComponent[i].alpha == 0)
			{
				UISprite uiSprite = revealsWrapperComponent[i].transform.parent.GetComponentInChildren<UISprite>();
				revealsWrapperComponent[i].color = Color.gray;
				revealsWrapperComponent[i].alpha = 1;
				
				if (reveal.groupId.Length > 0)
				{
					NGUITools.SetActive(revealsWrapperComponent[i].transform.parent.gameObject,true);
					revealsWrapperComponent[i].text = "";
					if (uiSprite)
					{
						uiSprite.spriteName = characterIcons[correctArrayIndex];
						uiSprite.color = Color.gray;
						uiSprite.MarkAsChanged();
						uiSprite.MakePixelPerfect();
						selectedCharacter = uiSprite.spriteName;
					}
				}
				else
				{
					if (uiSprite)
					{
						uiSprite.enabled = false;
					}
					revealsWrapperComponent[i].text = CreditsEconomy.convertCredits(reveal.credits * BonusGameManager.instance.currentMultiplier);
				}
				
				Audio.play("RifleShot01");
				
				Invoke("revealRemainingPicks", 0.5f);
				return; 
			}
		}
	}
			
	private void endGame()
	{
		BonusGamePresenter.instance.gameEnded();
	}

	// The setup that is supposed to happen before any spin.
	private IEnumerator preSpin()
	{
		Audio.stopMusic(0.0f);
		Audio.switchMusicKeyImmediate("WheelSlowsMusicT2");
		spinTitleText.SetActive(false);
		spinButton.SetActive(false);
		wheelStartAnimationPrefab.SetActive(true);
		yield return new WaitForSeconds(2.0f);
	}

	private void onSwipeStarted()
	{
		StartCoroutine(preSpin());
	}

	public IEnumerator spinClicked()
	{
		yield return StartCoroutine(preSpin());
		wheelSpinner = new WheelSpinner3d(carousel, ( stopAngle ), onWheelSpinComplete);
		disableSwipeToSpin();
	}
	
	private IEnumerator shakeItems()
	{
		do
		{
			int rnd = Random.Range(0, unpickedPicks.Count - 1);

			if (rnd == lastRnd)
			{
				// just increase the random index by one if we were going
				// to shake the same object again
				rnd = (rnd + 1) % unpickedPicks.Count;
			}

			GameObject pickToShake = unpickedPicks[rnd];

			VisualEffectComponent vfx = VisualEffectComponent.Create(sheenEffects[correctArrayIndex], pickToShake);
			vfx.transform.parent = pickToShake.transform;
			vfx.transform.localScale = new Vector3(1.5f, 2f, 0);
			vfx.transform.localPosition = Vector3.zero;
			iTween.ShakeRotation(pickToShake,
								iTween.Hash("amount", new Vector3(0, 0, 5),
											"time", 0.5f));
			yield return new WaitForSeconds(1.0f);
			
			Destroy(vfx);
			lastRnd = rnd;

			if (isDoingPickMeAnims)
			{
				// Add a delay before playing the next pick me animation
				yield return new WaitForSeconds(2.0f);
			}
		} 
		while (isDoingPickMeAnims);
	}
}
