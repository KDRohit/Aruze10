using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class elvira02Pickem : ChallengeGame {

	public GameObject[] coffins;						// The selectable coffins
	public Animator[] coffinsAnimators;					// The animators that control the pickem and reveals for the coffins.

	public GameObject[] icons;							// The icons that highlight after a match has been found.

	public GameObject[] m1Gems;							// Each individual gem array so we can reveal and idle the gems after being found.
	public GameObject[] m2Gems;
	public GameObject[] m3Gems;
	public GameObject[] m4Gems;
	public GameObject[] m5Gems;

	public UILabel[] winLabels;							// The labels under the icons that we populate with progressive amounts. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] winLabelsWrapperComponent;							// The labels under the icons that we populate with progressive amounts.

	public List<LabelWrapper> winLabelsWrapper
	{
		get
		{
			if (_winLabelsWrapper == null)
			{
				_winLabelsWrapper = new List<LabelWrapper>();

				if (winLabelsWrapperComponent != null && winLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in winLabelsWrapperComponent)
					{
						_winLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in winLabels)
					{
						_winLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _winLabelsWrapper;
		}
	}
	private List<LabelWrapper> _winLabelsWrapper = null;	
	

	public GameObject sparkleTrail;						// The trail from the coffin to the gem.

	private bool gameEnded = false;						// For pickemrepeater use

	private Dictionary<string, int> gemCount = new Dictionary<string, int>();					// Keeps count of the selections locally
	private Dictionary<int, bool> coffinRevealed = new Dictionary<int, bool>();					// Bool dict that says if we've selected a coffin
	private Dictionary<string, GameObject> allIcons = new Dictionary<string, GameObject>();		// Reference to the gem

	private PickemOutcome pickemOutcome; 				// Outcome for this game
	private PickemPick currentPick; 					// the current pick from the outcome

	private CoroutineRepeater pickemRepeater;			// Repeat that pickem!

	private const float MIN_TIME = 2.0f;				// Minimum time an animation might take to play next
	private const float MAX_TIME = 7.0f;				// Maximum time an animation might take to play next

	private const float TIME_BETWEEN_REVEALS = 0.25f;
	private SkippableWait revealWait = new SkippableWait();			//Handles skippable reveals

	// Sound names
	private const string INTRO_VO = "GYIntroVO";
	private const string PICKME_SOUND = "CoffinPickMeEL02";
	private const string COFFIN_PICKED = "GYPickACoffin";
	private const string REVEAL_VO = "GYRevealVO";
	private const string REVEAL_M1 = "GYRevealElvira";
	private const string REVEAL_M2 = "GYRevealCredit";
	private const string REVEAL_M3 = "GYRevealCredit";
	private const string REVEAL_M4 = "GYRevealCredit";
	private const string REVEAL_M5 = "GYRevealGravestone";
	private const string FINAL_REVEAL = "GYJackpotEL02";
	private const string SUMMARY_VO = "SummaryVOEL02";

	public override void init()
	{
		Audio.play(INTRO_VO);
		pickemOutcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		currentPick = pickemOutcome.getNextEntry();

		// Initializing the above dictionaries as needed.
		for (int i = 1; i < 6; i++)
		{
			gemCount.Add("M"+i, 0);
			allIcons.Add("M"+i, icons[i-1]);
		}
		for (int i = 0; i < 16; i++)
		{
			coffinRevealed.Add(i, false);
		}

		// Now let's populate the labels under the icons with their respective amounts.
		foreach(JSON paytableGroup in pickemOutcome.paytableGroups)
		{
			long credits = paytableGroup.getLong("credits", 0L) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			switch (paytableGroup.getString("group_code", ""))
			{
				case "group_M1":
					winLabelsWrapper[0].text = CreditsEconomy.convertCredits(credits);
					break;
				case "group_M2":
					winLabelsWrapper[1].text = CreditsEconomy.convertCredits(credits);
					break;
				case "group_M3":
					winLabelsWrapper[2].text = CreditsEconomy.convertCredits(credits);
					break;
				case "group_M4":
					winLabelsWrapper[3].text = CreditsEconomy.convertCredits(credits);
					break;
				case "group_M5":
					winLabelsWrapper[4].text = CreditsEconomy.convertCredits(credits);
					break;
			}
		}

		pickemRepeater = new CoroutineRepeater(MIN_TIME, MAX_TIME, pickeMeCallback);
		_didInit = true;
	}

	// Straightforward repeater that opens the coffin and plays a sound.
	private IEnumerator pickeMeCallback()
	{
		if (!gameEnded)
		{
			Audio.play(PICKME_SOUND);
			int pickemIndex = Random.Range(0, coffinsAnimators.Length);
			if (!coffinRevealed[pickemIndex])
			{
				coffinsAnimators[pickemIndex].Play("EV02_PickingBonus_PickObject_PickMe");
			}
			yield return new TIWaitForSeconds(1.0f);
		}
	}

	protected override void Update()
	{
		base.Update();
		if (!gameEnded && _didInit)
		{
			pickemRepeater.update();
		}
	}

	// Callback from clicking a coffin.
	public void coffinSelected(GameObject coffin)
	{
		foreach (GameObject coffinObj in coffins)
		{
			CommonGameObject.setObjectCollidersEnabled(coffinObj, false);
		}

		Audio.play(COFFIN_PICKED);
		int index = System.Array.IndexOf(coffins, coffin);

		// We basically parse out the proper gem from the pick itself.
		string selectedGem = currentPick.pick.Substring(currentPick.pick.Length-2);

		// Mark the appropriate coffin as reveals, and play the opening animation using the newly parsed out gem name.
		coffinRevealed[index] = true;
		coffinsAnimators[index].Play("EV02_PickingBonus_PickObject_Revealed_" + selectedGem);

		StartCoroutine(flyToGem(selectedGem, index, coffin));
	}

	private IEnumerator flyToGem(string selectedGem, int index, GameObject coffin)
	{		
		GameObject targetGem = null;

		// Finds the gem we want to turn on by its icon designator.
		switch (selectedGem)
		{
			case "M1":
				targetGem = m1Gems[gemCount[selectedGem]];
				Audio.play(REVEAL_M1, 1, 0, 0.5f);
				break;
			case "M2":
				targetGem = m2Gems[gemCount[selectedGem]];
				Audio.play(REVEAL_M2, 1, 0, 0.5f);
				break;
			case "M3":
				targetGem = m3Gems[gemCount[selectedGem]];
				Audio.play(REVEAL_M3, 1, 0, 0.5f);
				break;
			case "M4":
				targetGem = m4Gems[gemCount[selectedGem]];
				Audio.play(REVEAL_M4, 1, 0, 0.5f);
				break;
			case "M5":
				targetGem = m5Gems[gemCount[selectedGem]];
				Audio.play(REVEAL_M5, 1, 0, 0.5f);
				break;
		}

		// Now we turn on the sparkle, reposition, and fly it to the gem.
		sparkleTrail.transform.parent = coffin.transform;
		sparkleTrail.transform.position = coffin.transform.position;
		sparkleTrail.SetActive(true);
		Audio.play("value_move");
		iTween.MoveTo(sparkleTrail, targetGem.transform.position, 1.0f);
		yield return new TIWaitForSeconds(1.0f);
		Audio.play("value_land");
		sparkleTrail.SetActive(false);

		Audio.play(REVEAL_VO);

		// Once the sparkle has landed, play the gem reveal.
		if (selectedGem == "M1" && gemCount[selectedGem] == 1)
		{
			targetGem.GetComponent<Animation>().Play("EV02_PickingBonus_" + selectedGem +"_LargeJem_Reveal");
		}
		else
		{
			targetGem.GetComponent<Animation>().Play("EV02_PickingBonus_" + selectedGem +"_Jem_Reveal");
		}

		// Then, give it some time and let the gem idle begin.
		StartCoroutine(beginIdleGemAnimation(targetGem, selectedGem));

		// Increase the final count for the gem of this type.
		gemCount[selectedGem] += 1;

		// Game's done under these conditions, end the damn game! (M1 has 3 gems, the rest have 2, hence the || here)
		if ((selectedGem == "M1" && gemCount[selectedGem] == 3) || (selectedGem != "M1" && gemCount[selectedGem] == 2))
		{
			gameEnded = true;
			allIcons[selectedGem].GetComponent<Animation>().Play("EV02_PickingBonus_IconGlow_"+ selectedGem +"_on");
			Audio.play(FINAL_REVEAL);
			BonusGamePresenter.instance.currentPayout = currentPick.credits;
			StartCoroutine(revealAllCoffins());
		}
		else
		{
			// Play the sound.
			switch (selectedGem)
			{
				case "M1":
					Audio.play(REVEAL_M1, 1, 0, 0.5f);
					break;
				case "M2":
					Audio.play(REVEAL_M2, 1, 0, 0.5f);
					break;
				case "M3":
					Audio.play(REVEAL_M3, 1, 0, 0.5f);
					break;
				case "M4":
					Audio.play(REVEAL_M4, 1, 0, 0.5f);
					break;
				case "M5":
					Audio.play(REVEAL_M5, 1, 0, 0.5f);
					break;
			}
			// If we're not ending, get the next pick, and ensure all colldiers are on except for those that have been revealed.
			currentPick = pickemOutcome.getNextEntry();
			for (int i = 0; i < coffins.Length;i++)
			{
				if (!coffinRevealed[i])
				{
					CommonGameObject.setObjectCollidersEnabled(coffins[i], true);
				}
			}
		}
	}

	// Merely triggers the idle gem animation on a newly revealed gem.
	private IEnumerator beginIdleGemAnimation(GameObject targetGem, string selectedGem)
	{
		yield return new TIWaitForSeconds(1.0f);

		if (selectedGem == "M1" && gemCount[selectedGem] == 3)
		{
			targetGem.GetComponent<Animation>().Play("EV02_PickingBonus_" + selectedGem +"_LargeJem_Idle");
		}
		else
		{
			targetGem.GetComponent<Animation>().Play("EV02_PickingBonus_" + selectedGem +"_Jem_Idle");
		}
	}

	private IEnumerator revealAllCoffins()
	{
		// Let's disable all the icons now
		foreach (GameObject coffin in coffins)
		{
			CommonGameObject.setObjectCollidersEnabled(coffin, false);
		}

		yield return new TIWaitForSeconds(0.5f); 

		currentPick = pickemOutcome.getNextReveal();
		while (currentPick != null)
		{
			// Let's pull the gem from the current pick.
			string revealedGem = currentPick.pick.Substring(currentPick.pick.Length-2);
			int index = 0;
			foreach (KeyValuePair<int, bool> pair in coffinRevealed)
			{
				if (!pair.Value)
				{
					// Then, the next one that hasn't been revealed, let's pull the index and mark it true.
					index = pair.Key;
					coffinRevealed[index] = true;
					break;
				}
			}

			// Let's then mark the reveals as grayed out.
			GameObject spriteToReveal = CommonGameObject.findChild(coffinsAnimators[index].gameObject, "RO_" + revealedGem + "_Sprite");
			if (spriteToReveal != null)
			{
				if(!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap("reveal_not_chosen"));
				}
				UISprite revealSprite = spriteToReveal.GetComponent<UISprite>();
				if (revealSprite != null)
				{
					revealSprite.color = Color.gray;
				}
			}

			// And let's ensure the reveal animation for the coffin plays.
			coffinsAnimators[index].Play("EV02_PickingBonus_PickObject_Revealed_" + revealedGem);

			// Then get the next one, and let's do this again.
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			currentPick = pickemOutcome.getNextReveal();
			//yield return new TIWaitForSeconds(0.15f); 
		}

		yield return new TIWaitForSeconds(1.0f);
		Audio.play(SUMMARY_VO);
		BonusGamePresenter.instance.gameEnded();
	}
}

