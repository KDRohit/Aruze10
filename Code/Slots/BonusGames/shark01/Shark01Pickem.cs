using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This pickem class encapsulates both the bomb pick and the shark pick games for shark01.
public class Shark01Pickem : PickingGame<PickemOutcome>
{
	[Header("Bomb Screen Vars")]
	public GameObject[]	tornadoImages;
	public GameObject[] tornadoAnimations;
	public GameObject explosionReveal;
	public UILabel remainingTornadoes;	// To be removed when prefabs are updated.
	public LabelWrapperComponent remainingTornadoesWrapperComponent;

	public LabelWrapper remainingTornadoesWrapper
	{
		get
		{
			if (_remainingTornadoesWrapper == null)
			{
				if (remainingTornadoesWrapperComponent != null)
				{
					_remainingTornadoesWrapper = remainingTornadoesWrapperComponent.labelWrapper;
				}
				else
				{
					_remainingTornadoesWrapper = new LabelWrapper(remainingTornadoes);
				}
			}
			return _remainingTornadoesWrapper;
		}
	}
	private LabelWrapper _remainingTornadoesWrapper = null;
	

	// Game wide private vars
	private bool allowInputAfterPick = false;

	// Bomb private vars
	private string nextBonusGameName = "";
	private int tornadoCount;
	private float[] rotationAngles = new float[5];

	// Bomb Consts
	private const float PICK_ROTATE_TIME = 0.2f;
	private const float BOMB_PICK_DONE_WAIT = 1.0f;
	private const float BOMB_PICK_ROLLUP_WAIT = 0.25f;
	private const int TOTAL_TORNADOES = 3;
	private const string TORNADO_GROUP_ID = "tornado";
	private const string SHARK_GROUP_ID = "shark";

	[Header("Shark Screen Vars")]
	public GameObject[]	shadowObjects;
	public Animation[] sharkAnimations;
	public GameObject chainsawAnimation;
	public GameObject[] chainsawPositions;

	// Shark private vars
	private WheelOutcome sharkOutcome;
	private WheelPick currentWheelPick;
	private int winsIndex;
	private PlayingAudio chopperLoop;

	// Shark Consts
	private const float REVEAL_NOVA_CHAINSAW_TIME = 1.0f;
	private const float SHARK_PICKME_ANIMATION_FADE_TIME = 0.1f;
	private const float SHARK_PICKME_ANIMATION_WAIT_TIME = 0.5f;
	private const float SHARK_PICK_WAIT_FOR_SHARK_LANDINGS = 1.0f;
	private const float SHARK_PICK_PAUSE_BEFORE_FINAL_REVEAL = 0.5f;
	private const float SHARK_PICK_DONE_WAIT = 1.0f;
	private readonly string[] sharkPickmeAnimationNames = new string[]
	{
		"wiggleC",
		"wiggleA",
		"wiggleB",
	};

	// Stages/Rounds Constants
	private const int BOMB_STAGE = 0;
	private const int SHARK_STAGE = 1;

	// Sound names Constants
	private const string INTRO_VO = "DSIntroVO";
	private const string CHOPPER_LOOP_SOUND = "DSChopperLoop";
	private const string PICK_SOUND = "DSPickMeShark01";
	private const string PICK_ME_SOUND = "DSPickMeShark01";
	private const string REVEAL_TORNADO_SOUND = "DSRevealTornado";
	private const string REVEAL_SHARK_SOUND = "DSRevealShark";
	private const string REVEAL_CREDIT_SOUND = "DSRevealCredit";
	private const string LEVEL2_MUSIC = "DestroySharknadoLevel2";
	private const string LEVEL2_VO = "FNFallingSharks";
	private const string REVEAL_MULTIPLIER_SOUND = "DSRevealMultiplier";
	private const string REVEAL_NOVA_SOUND = "DSRevealNova";
	private const string REVEAL_NOVA_VO = "NVIReallyHateSharks";
	private const string SUMMARY_SCREEN_VO = "FNOKShouldBeSmoothSailingFromHere";
	private const string SHARKS_FALLING_SOUND = "DSSharksFall";

	// Initialization of Pickem, calls base.Init() which setsup the outcome
	public override void init()
	{
		base.init();

		// Sound startup
		chopperLoop = Audio.play(CHOPPER_LOOP_SOUND, 1.0f, 0.0f, 0.0f, float.PositiveInfinity);		
		Audio.play(INTRO_VO, 1.0f, 0.0f, 0.6f); // Standard 600 ms delay on VO.
		pickMeSoundName = PICK_ME_SOUND;

		revealWaitTime = 0.5f;

		// Let's populate the rotation angles 
		for (int i = 0; i < 5; i++)
		{
			rotationAngles[i] = 20 / (i + 1);
			if (i % 2 == 0)
			{
				rotationAngles[i] *= -1;
			}
		}

		tornadoCount = 0;
		UpdateRemainingTornadoesText(tornadoCount);
	}

	/// Called when a pick button is pressed (Bomb or Shark)
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		inputEnabled = false;

		switch (currentStage)
		{
			case BOMB_STAGE:
				yield return StartCoroutine(bombClicked(button));
				break;
			case SHARK_STAGE:
				yield return StartCoroutine(sharkClicked(button));
				break;
		}

		if (allowInputAfterPick)
		{	
			inputEnabled = true;
			allowInputAfterPick = false;
		}
	}
	
	/// Pick me animation player (Shark or Bomb)
	protected override IEnumerator pickMeAnimCallback()
	{
		if (inputEnabled)
		{		
			switch (currentStage)
			{
				case BOMB_STAGE:
					yield return StartCoroutine(bombPickMeAnim());
					break;
				case SHARK_STAGE:
					yield return StartCoroutine(sharkPickmeAnim());
					break;
			}
		}
	}

	// Animation of the Bomb Pick Me! shake animation (called by pickMeAnimCallback())
	// - We control our shaking bomb in code since the art was never delivered for it, and its simple to reproduce the effect.
	private IEnumerator bombPickMeAnim()
	{
		PickGameButtonData pickMe = getRandomPickMe();
			
		if (pickMe != null)
		{
			Audio.play(pickMeSoundName);

			foreach (float rotationAngle in rotationAngles)
			{
				iTween.RotateTo(pickMe.button, iTween.Hash("z", rotationAngle, "time", PICK_ROTATE_TIME, "easetype", iTween.EaseType.linear));
				yield return new TIWaitForSeconds(PICK_ROTATE_TIME);
			}
				
			iTween.RotateTo(pickMe.button, iTween.Hash("z", 0, "time", PICK_ROTATE_TIME, "easetype", iTween.EaseType.linear));
			yield return new TIWaitForSeconds(PICK_ROTATE_TIME);
		}
	}
	// Animation of the Shark Pick Me! fish flop animation (called by pickMeAnimCallback())
	// - We control our floping sharks partially in code since the are no animators and the animated sharks 
	//	 are in a different branch.
	private IEnumerator sharkPickmeAnim()
	{
		Audio.play(pickMeSoundName);

		int sharkPickmeIndex = Random.Range(0, sharkAnimations.Length);
		string pickmeAnimationName = sharkPickmeAnimationNames[sharkPickmeIndex];

		sharkAnimations[sharkPickmeIndex].Play(pickmeAnimationName);
			
		yield return new TIWaitForSeconds(SHARK_PICKME_ANIMATION_WAIT_TIME);
	}

	// Player clicking a bomb pick (called from pickemButtonPressedCoroutine())
	private IEnumerator bombClicked(GameObject bombSelected)
	{
		Audio.play(PICK_SOUND);		 

		// Let's set it to false then true, just in case it was already playing somewhere else
		explosionReveal.SetActive(false);
		explosionReveal.transform.position = bombSelected.transform.position;
		explosionReveal.SetActive(true);

		bombSelected.SetActive(false);

		PickemPick currentPick = outcome.getNextEntry();

		yield return StartCoroutine(revealBombPick(currentPick, getButtonIndex(bombSelected, BOMB_STAGE)));
	}

	// For Bomb Stage, Let's do the rollup celebration
	private IEnumerator beginBombRollup(PickemPick currentPick)
	{
		BonusGamePresenter.instance.currentPayout += currentPick.credits;
		
		yield return new TIWaitForSeconds(BOMB_PICK_ROLLUP_WAIT);
		yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout - currentPick.credits, 
		                                             BonusGamePresenter.instance.currentPayout, currentWinAmountTextsWrapper[BOMB_STAGE]));		
		yield return null;
	}

	// Our reveal bomb method that can be used during the reveals or the intial pick.
	private IEnumerator revealBombPick(PickemPick currentPick, int pickIndex)
	{
		PickGameButton pickGameButton = getPickGameButton(pickIndex, BOMB_STAGE);		
		pickGameButton.revealNumberLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
		pickGameButton.revealNumberLabel.gameObject.SetActive(true);

		if (currentPick.groupId == TORNADO_GROUP_ID)
		{
			tornadoAnimations[pickIndex].SetActive(true);

			Audio.play(REVEAL_TORNADO_SOUND);

			tornadoCount++;
			UpdateRemainingTornadoesText(tornadoCount);

			yield return StartCoroutine(beginBombRollup(currentPick));
				
			// If we get all the tornadoes, new bonus game is a-go.
			if (tornadoCount == TOTAL_TORNADOES)
			{
				nextBonusGameName = currentPick.bonusGame;
				yield return StartCoroutine(revealBombPickRemainingPicks(true));
			}
			else
			{
				allowInputAfterPick = true;
			}			
		}
		else if (currentPick.groupId == SHARK_GROUP_ID) // the resert shark, not the shark stage
		{
			pickGameButton.imageReveal.gameObject.SetActive(true);

			Audio.play(REVEAL_SHARK_SOUND);
				
			yield return StartCoroutine(beginBombRollup(currentPick));

			// Is may be confusing, but the Bomb pick has a Shark 
			// that ends the round, so really it's a bomb reveal.
			yield return StartCoroutine(revealBombPickRemainingPicks(false));
		}
		else
		{
			Audio.play(REVEAL_CREDIT_SOUND);
				
			yield return StartCoroutine(beginBombRollup(currentPick));

			allowInputAfterPick = true;
		}
	}

	// Our final/missed reveal bomb method
	private void revealBombPickRemaining(PickemPick currentPick, int pickIndex)
	{
		PickGameButton pickGameButton = getPickGameButton(pickIndex, BOMB_STAGE);		
		pickGameButton.revealNumberLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
		pickGameButton.revealNumberLabel.gameObject.SetActive(true);
		
		if (currentPick.groupId == TORNADO_GROUP_ID)
		{
			tornadoAnimations[pickIndex].SetActive(true);

			// Grey out the mesh.
			MeshRenderer tornadoMeshRenderer = tornadoAnimations[pickIndex].GetComponentInChildren<MeshRenderer>();
			if (tornadoMeshRenderer != null)
			{
				tornadoMeshRenderer.material.color = Color.gray;
			}
		}
		else if (currentPick.groupId == SHARK_GROUP_ID) // the resert shark, not the shark stage
		{
			pickGameButton.imageReveal.gameObject.SetActive(true);

			// We want to gray out the shark.
			pickGameButton.imageReveal.color = Color.gray;
		}
		
		// Let's gray out the revealed images.
		pickGameButton.revealNumberLabel.gameObject.GetComponent<UILabelMultipleEffectsStyler>().enabled = false;
		pickGameButton.revealNumberLabel.color = Color.gray;
		pickGameButton.revealNumberLabel.effectStyle = UILabel.Effect.None;
		pickGameButton.revealNumberLabel.colorMode = UILabel.ColorMode.Solid;

		if (!revealWait.isSkipping)
		{
			Audio.play (Audio.soundMap (DEFAULT_NOT_CHOSEN_SOUND_KEY));
		}
	}

	// After we've ended the bomb game, this reveal method is called.
	private IEnumerator revealBombPickRemainingPicks(bool shouldContinue)
	{
		GameObject[] bombObjects = newPickGameButtonRounds[BOMB_STAGE].pickGameObjects;		
		for (int i = 0; i < bombObjects.Length; i++)
		{
			GameObject bombButton = bombObjects[i].GetComponent<PickGameButton>().button;
			if (bombButton.activeSelf)
			{
				bombButton.SetActive(false);

				PickemPick currentPick = outcome.getNextReveal();

				revealBombPickRemaining(currentPick, i);
				yield return StartCoroutine(revealWait.wait(revealWaitTime));
			}
		}

		yield return new TIWaitForSeconds(BOMB_PICK_DONE_WAIT);

		Audio.stopSound(chopperLoop);

		// Sets up the shark game, or ends the current one.
		if (shouldContinue)
		{
			SlotOutcome challengeBonus = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, nextBonusGameName);
			sharkOutcome = new WheelOutcome(challengeBonus);

			Audio.switchMusicKeyImmediate(LEVEL2_MUSIC);
			Audio.play(LEVEL2_VO);
			chainsawAnimation.SetActive(false);
			currentWinAmountTextsWrapper[SHARK_STAGE].text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);

			// Activate the Shark Stage!
			continueToNextStage();
			
			Audio.play(SHARKS_FALLING_SOUND);

			// Adding in a slight delay to click the sharks so they can travel down first.
			yield return new TIWaitForSeconds(SHARK_PICK_WAIT_FOR_SHARK_LANDINGS);

			// The wait times might be stale
			pickMeController.reset();

			allowInputAfterPick = true;
		}
		else
		{
			endGame();
		}
	}

	// Player clicking a shark pick (called from pickemButtonPressedCoroutine())
	private IEnumerator sharkClicked(GameObject sharkSelected)
	{
		// The button (aka shark selected) is a child of the object in the NewPickGameButtonRounds
		GameObject sharkObject = sharkSelected.transform.parent.gameObject;
		GameObject[] sharkObjects = newPickGameButtonRounds[SHARK_STAGE].pickGameObjects;
		int sharkIndex = System.Array.IndexOf(sharkObjects, sharkObject);
		currentWheelPick = sharkOutcome.getNextEntry();

		yield return StartCoroutine(revealSharkPick(sharkIndex, false));
	}

	// Reveals the shark, on either the reveals or the one you clicked on.
	private IEnumerator revealSharkPick(int sharkIndex, bool afterGameReveal)
	{
		PickGameButton pickGameButton = getPickGameButton(sharkIndex, SHARK_STAGE);

		shadowObjects[sharkIndex].SetActive(false);

		long credits = 0;
		int multiplier = 0;
		long initialPayout = BonusGamePresenter.instance.currentPayout;

		if (afterGameReveal)
		{
			credits = currentWheelPick.wins[winsIndex].credits;
			multiplier = currentWheelPick.wins[winsIndex].multiplier;
		}
		else
		{
			credits = currentWheelPick.credits;
			multiplier = currentWheelPick.multiplier;
		}

		if (credits != 0)
		{
			sharkAnimations[sharkIndex].gameObject.SetActive(false);
			if (!afterGameReveal)
			{
				Audio.play(REVEAL_MULTIPLIER_SOUND);
			}		
			pickGameButton.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
			pickGameButton.revealNumberLabel.gameObject.SetActive(true);

			if (!afterGameReveal)
			{
				BonusGamePresenter.instance.currentPayout += credits;
			}
		}
		else
		{
			Vector3 sharkTextPosition = pickGameButton.revealNumberLabel.transform.localPosition;
			sharkTextPosition += new Vector3(160, -120, 0);
			
			pickGameButton.revealNumberLabel.gameObject.transform.localPosition = sharkTextPosition;

			if (afterGameReveal)
			{		
				pickGameButton.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
				pickGameButton.revealNumberLabel.gameObject.SetActive(true);
				pickGameButton.imageReveal.gameObject.SetActive(true);
				pickGameButton.imageReveal.MakePixelPerfect();
				pickGameButton.imageReveal.color = Color.gray;
			}
			else
			{
				// If there was no credits, and before the reveals, then this is the chainsaw sequence. Otherwise, just show nova grayed out.
				chainsawAnimation.transform.position = chainsawPositions[sharkIndex].gameObject.transform.position;
				chainsawAnimation.transform.rotation = chainsawPositions[sharkIndex].gameObject.transform.rotation;
				chainsawAnimation.SetActive(true);
				Audio.play(REVEAL_NOVA_SOUND);
				yield return new TIWaitForSeconds(REVEAL_NOVA_CHAINSAW_TIME);

				chainsawAnimation.SetActive(false);
				pickGameButton.imageReveal.gameObject.SetActive(true);
				pickGameButton.imageReveal.MakePixelPerfect();
				Audio.play(REVEAL_NOVA_VO, 1.0f, 0.0f, 1.0f);
				BonusGamePresenter.instance.currentPayout *= multiplier;
			}

			pickGameButton.revealNumberLabel.gameObject.SetActive(true);
			pickGameButton.revealNumberLabel.text = Localize.text("{0}X", CommonText.formatNumber(multiplier));
			sharkAnimations[sharkIndex].gameObject.SetActive(false);
		}

		if (afterGameReveal)
		{
			pickGameButton.revealNumberLabel.gameObject.GetComponent<UILabelMultipleEffectsStyler>().enabled = false;
			pickGameButton.revealNumberLabel.color = Color.gray;
			pickGameButton.revealNumberLabel.effectStyle = UILabel.Effect.None;
			pickGameButton.revealNumberLabel.colorMode = UILabel.ColorMode.Solid;
		}
		else
		{
			yield return null;
			yield return StartCoroutine(SlotUtils.rollup(initialPayout, BonusGamePresenter.instance.currentPayout, 
			                                             currentWinAmountTextsWrapper[SHARK_STAGE]));
			yield return StartCoroutine(revealSharkPickRemainingPicks());
		}
	}

	// After our shark selection, handles revealing the rest of the sharks.
	private IEnumerator revealSharkPickRemainingPicks()
	{
		yield return new TIWaitForSeconds(SHARK_PICK_PAUSE_BEFORE_FINAL_REVEAL);

		winsIndex = 0;

		if (currentWheelPick.winIndex == winsIndex)
		{
			winsIndex++;
		}
		
		revealWait.reset();
		for (int i = 0; i < sharkAnimations.Length; i++)
		{
			if (sharkAnimations[i].gameObject.activeSelf)
			{				
				StartCoroutine(revealSharkPick(i, true));
				yield return StartCoroutine(revealWait.wait(revealWaitTime));

				winsIndex++;
				if (currentWheelPick.winIndex == winsIndex)
				{
					winsIndex++;
				}
			}
		}

		yield return new TIWaitForSeconds(SHARK_PICK_DONE_WAIT);

		endGame();
	}

	// Ends the game and show the player the results
	private void endGame()
	{
		Audio.play(SUMMARY_SCREEN_VO);

		BonusGamePresenter.instance.gameEnded();
	}

	// Pull the Pick we need out of the base class's data
	// There is something similiar in the base class but it returns the PickGameButtonData
	private new PickGameButton getPickGameButton(int pickIndex, int stage)
	{
		PickGameButton pickGameButton = null;
		try
		{
			pickGameButton = newPickGameButtonRounds[stage].pickGameObjects[pickIndex].GetComponent<PickGameButton>();
		}
		catch (System.IndexOutOfRangeException)
		{
			Debug.LogError(string.Format("Shark01Pickem.getPickGameButton(pickIndex = {0}, stage = {1}) return null!", pickIndex, stage));
		}

		return pickGameButton;		
	}

	private void UpdateRemainingTornadoesText(int count)
	{		
		remainingTornadoesWrapper.text = Localize.text("find_{0}", CommonText.formatNumber(TOTAL_TORNADOES - count));
	}
}

