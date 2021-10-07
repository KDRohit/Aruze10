using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class bbh01BuckBonus : PickemGameStagesUsingWheelPicks {
	public Animator buckCard;
	public Animator cougarCard;
	public Animator wolfCard;
	public Animator foxCard;

	public GameObject[] animalTopLevelObjects;

	private List<Animator> animalCards = new List<Animator>();
	private Vector3[] originalPositions = new Vector3[5];
	private int[][] animalPositions = new int[4][];

	private int finalCardIndex;
	private int moveCount = 0;

	private string TRAIL_MOVE_SOUND = "SparklyWhooshDown1";
	private string TRAIL_LAND_SOUND = "value_land";
	private string HUNTING_BG_MUSIC = "BBHBg";
	private string SUMMARY_VO_SOUND = "BBHSummaryVO";

	public override void init()
	{
		animalPositions[0] = new int[5];
		animalPositions[0][0] = -1;
		animalPositions[0][1] = 1;
		animalPositions[0][2] = 1;
		animalPositions[0][3] = -1;
		animalPositions[0][4] = 1;

		animalPositions[1] = new int[5];
		animalPositions[1][0] = -1;
		animalPositions[1][1] = 1;
		animalPositions[1][2] = -1;
		animalPositions[1][3] = -1;
		animalPositions[1][4] = 1;

		animalPositions[2] = new int[5];
		animalPositions[2][0] = 1;
		animalPositions[2][1] = 1;
		animalPositions[2][2] = -1;
		animalPositions[2][3] = -1;
		animalPositions[2][4] = 1;

		animalPositions[3] = new int[5];
		animalPositions[3][0] = 1;
		animalPositions[3][1] = 1;
		animalPositions[3][2] = -1;
		animalPositions[3][3] = -1;
		animalPositions[3][4] = 1;

		animalCards.Add(buckCard);
		animalCards.Add(cougarCard);
		animalCards.Add(wolfCard);
		animalCards.Add(foxCard);
		base.init();

		Audio.switchMusicKeyImmediate(HUNTING_BG_MUSIC);
	}

	// override this function to stop the music before ending the game, not sure
	// why we have to do this manually in this game, it's a bit hacky
	protected override void endGame()
	{
		Audio.switchMusicKeyImmediate(""); // force the music off so the summary music starts right away
		rollupSoundOverride = "";
		Audio.play(SUMMARY_VO_SOUND);
		BonusGamePresenter.instance.gameEnded();
	}

	protected override IEnumerator beginPrePickAnimationSequence()
	{
		foreach (LabelWrapper instructionLabel in instructionTextWrapper)
		{
			instructionLabel.gameObject.SetActive(false);
		}

		foreach (UILabel buttonLabel in roundButtonList[currentStage].revealNumberList)
		{
			buttonLabel.gameObject.SetActive(false);
		}

		for (int i = 0; i < animalTopLevelObjects.Length;i++)
		{
			animalTopLevelObjects[i].SetActive(false);
		}

		// Begin shuffle card sequence.
		moveCount = 0;

		//float cycleTime = 2.0f;

		foreach (Animator animalAnimator in animalCards)
		{
			animalAnimator.Play("card_deactive");
		}

		yield return new TIWaitForSeconds(0.125f);

		for (int i = 0; i < 15; i++)
		{
			Audio.play("BBHSelectAnimalBell");
			int random = Random.Range(0, animalCards.Count);
			for (int j = 0; j < animalCards.Count; j++)
			{
				if (j == random)
				{
					animalCards[j].Play("card_active");
				}
				else
				{
					animalCards[j].Play("card_deactive");
				}
			}
			yield return new TIWaitForSeconds(0.125f);
		}

		// We assume its the deer one first...
		currentStage = 0;

		if (wheelPick.paytableName.Contains("lion"))
		{
			currentStage = 1;
		}
		else if (wheelPick.paytableName.Contains("wolf"))
		{
			currentStage = 2;
		}
		else if (wheelPick.paytableName.Contains("fox"))
		{
			currentStage = 3;
		}

		for (int i = 0; i < animalCards.Count;i++)
		{
			if (i == currentStage)
			{
				animalCards[i].Play("card_active");
			}
			else
			{
				animalCards[i].Play("card_deactive");
			}
		}

		for (int i = 0; i < animalTopLevelObjects.Length;i++)
		{
			if (currentStage == i)
			{
				animalTopLevelObjects[i].SetActive(true);
				instructionTextWrapper[i].gameObject.SetActive(true);
			}
			else
			{
				animalTopLevelObjects[i].SetActive(false);
			}
		}

		PickGameButtonDataList pickGameButtonList = roundButtonList[currentStage];
		Audio.play("BBHAnimalsRunOn");
		for (int i = 0; i < pickGameButtonList.buttonList.Length;i++)
		{
			originalPositions[i] = pickGameButtonList.animationList[i].gameObject.transform.localPosition;
			roundButtonList[currentStage].materialList[i].color = Color.white;
			pickGameButtonList.animationList[i].Play("run");
			float movementDirection = 1500.0f;
			movementDirection *= animalPositions[currentStage][i];
			pickGameButtonList.animationList[i].gameObject.transform.localPosition = pickGameButtonList.animationList[i].gameObject.transform.localPosition + new Vector3(movementDirection, 0, 0);
			StartCoroutine(moveAnimalToFinalSpot(i));
		}
	}

	protected override void prePickAudioCall()
	{
		Audio.play("RifleShot");
		Audio.play("BBHHitAnimal", 1.0f, 0.0f, .25f	);
	}

	protected override IEnumerator beginPostPickSequence()
	{
		yield return StartCoroutine(revealSinglePickemObject());
		if (wheelPick.extraRound > 0)
		{
			GameObject instancedSparkleTrail = CommonGameObject.instantiate(bonusSparkleTrail) as GameObject;
			// Now we turn on the sparkle, reposition, and fly it to the gem.
			PickGameButtonData pick = getPickGameButton(pickButtonIndex);
			instancedSparkleTrail.transform.parent = pick.revealNumberLabel.gameObject.transform;
			instancedSparkleTrail.transform.position = pick.revealNumberLabel.gameObject.transform.position;
			instancedSparkleTrail.transform.localScale = Vector3.one * 0.1f;

			Audio.play(TRAIL_MOVE_SOUND);
			iTween.MoveTo(instancedSparkleTrail, pickCountLabelWrapper.gameObject.transform.position, 1.5f);
			yield return new WaitForSeconds(1.5f);
			picksRemaining = picksRemaining + wheelPick.extraRound;
			pickCountLabelWrapper.text = picksRemaining.ToString();
			Audio.play(TRAIL_LAND_SOUND);
			Destroy(instancedSparkleTrail);
		}
		yield return StartCoroutine(base.beginPostPickSequence());
	}

	protected override IEnumerator revealSinglePickemObject()
	{
		PickGameButtonData pick = getPickGameButton(pickButtonIndex);
		pick.revealNumberLabel.transform.position = pick.button.transform.parent.Find("TextPos").position;
		pick.revealNumberLabel.gameObject.SetActive(true);
		pick.revealNumberLabel.color = Color.white;
		if (wheelPick.extraRound > 0)
		{
			pick.revealNumberLabel.text = CreditsEconomy.convertCredits(wheelPick.credits) + "\n" + Localize.text("plus_extra_ammo");
		}
		else
		{
			pick.revealNumberLabel.text = CreditsEconomy.convertCredits(wheelPick.credits);
		}
		yield return StartCoroutine(revealRemainingObjects());
	}

	private IEnumerator revealRemainingObjects()
	{
		revealWait.reset ();
		int winsIndex = 0;
		for (int i = 0; i < roundButtonList[currentStage].revealNumberList.Length; i++)
		{
			if (wheelPick.wins[winsIndex].winIndex == wheelPick.winIndex)
			{
				winsIndex++;
			}

			if (i != pickButtonIndex)
			{
				PickGameButtonData pick = getPickGameButton(i);
				pick.revealNumberLabel.transform.position = pick.button.transform.parent.Find("TextPos").position;

				if (wheelPick.wins[winsIndex].extraRound > 0)
				{
					pick.revealNumberLabel.text = CreditsEconomy.convertCredits(wheelPick.wins[winsIndex].credits) + "\n" + Localize.text("plus_extra_ammo");
				}
				else
				{
					pick.revealNumberLabel.text = CreditsEconomy.convertCredits(wheelPick.wins[winsIndex].credits);
				}
				pick.revealNumberLabel.color = Color.gray;
				pick.material.color = Color.gray;
				pick.revealNumberLabel.gameObject.SetActive(true);
				if(!revealWait.isSkipping)
				{
					Audio.play (Audio.soundMap("reveal_not_chosen"));
				}
			}
			yield return StartCoroutine(revealWait.wait(revealWaitTime));
		}
		yield return new TIWaitForSeconds(0.5f);
	}

	private IEnumerator moveAnimalToFinalSpot(int index)
	{
		yield return new TIWaitForSeconds(Random.Range(0.0f,0.6f));
		PickGameButtonDataList pickGameButtonList = roundButtonList[currentStage];
		Hashtable tween = iTween.Hash("position", originalPositions[index], "isLocal", true, "time", 1.5f, "easetype", iTween.EaseType.linear);
   		iTween.MoveTo(pickGameButtonList.animationList[index].gameObject, tween);
		yield return new TIWaitForSeconds(1.4f);
		pickGameButtonList.animationList[index].CrossFade("run2stop", .05f);
   		//pickGameButtonList.animationList[index].Play("run2stop");
		yield return new TIWaitForSeconds(.167f); // length of run2stop animation
   		moveCount++;
   		if (moveCount >=5)
   		{
   			Audio.play("RifleCock1", 1, 0, 0.5f);
   			inputEnabled = true;
   		}
	}
}
