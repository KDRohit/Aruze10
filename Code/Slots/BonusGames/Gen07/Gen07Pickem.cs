using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Gen07Pickem : ChallengeGame {

	public GameObject[] butterflies;						// The selectable coffins
	public Animator[] butterflyAnimators;					// The animators that control the pickem and reveals for the coffins.

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

	private SkippableWait revealWait = new SkippableWait();			//Handles skippable reveals
		
	// Animation name consts
	private const string BUTTERFLY_PICKME_ANIM_NAME = "Gen07_PickingBonus_pickObject_PickMe";
	private const string BUTTERFLY_PICK_IDLE_ANIM_NAME = "Gen07_PickingBonus_pickObject_Idle";
	private const string BUTTERFLY_REVEAL_ANIM_PREFIX = "Gen07_PickingBonus_pickObject_Reveal ";
	private const string GEM_ANIM_PREFIX = "Gen07_PickingBonus_Icon_";
	private const string GEM_REVEAL_ANIM_POSTFIX = "ProgressiveGem_Reveal";
	private const string GEM_IDLE_ANIM_POSTFIX = "ProgressiveGem_Idle";
	private const string GEM_GLOW_ANIM_POSTFIX = "_Glow";

	// Sound names
	private const string INTRO_VO = "RHPureThoughtYouFindUnicorn";
	private const string PICKME_SOUND = "rollover_sparkly";
	private const string COFFIN_PICKED = "FTPickAFairy";
	private const string REVEAL_UNICORN_VO_SOUND = "FTRevealUnicornVO";
	private const string REVEAL_UNICORN_SOUND = "FTRevealUnicorn";
	private const string REVEAL_NORMAL_SOUND = "FTRevealNormal";
	private const string FINAL_REVEAL_JACKPOT_VO = "FTRevealJackpotVO";
	private const string FINAL_REVEAL_JACKPOT_SOUND = "FTJackpotUnicorn";
	private const string SUMMARY_VO = "SummaryVOUnicorn";
	private const string REVEAL_NOT_CHOSEN_SOUND = "reveal_not_chosen";
	private const string TRAIL_MOVE_SOUND = "value_move";
	private const string TRAIL_LAND_SOUND = "value_land";

	// Time consts
	private const float REVEAL_ALL_WAIT_TIME = 1.0f;
	private const float GEM_IDLE_WAIT_TIME = 1.0f;
	private const float TRAIL_MOVE_TIME = 1.0f;
	private const float REVEAL_WAIT_AFTER_COLLIDER_DISABLE = 0.5f;
	private const float TIME_BETWEEN_REVEALS = 0.15f;
	private const float WAIT_BEFORE_SUMMARY_TIME = 1.0f;
	private const float WAIT_AFTER_PICKME_ANIM = 1.0f;

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

		foreach (Animator animator in butterflyAnimators)
		{
			animator.Play(BUTTERFLY_PICK_IDLE_ANIM_NAME);
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

	// Straightforward repeater that plays the pick me animation
	private IEnumerator pickeMeCallback()
	{
		if (!gameEnded)
		{
			Audio.play(PICKME_SOUND);
			int pickemIndex = Random.Range(0, butterflyAnimators.Length);
			if (!coffinRevealed[pickemIndex])
			{
				butterflyAnimators[pickemIndex].Play(BUTTERFLY_PICKME_ANIM_NAME);
			}
			yield return new TIWaitForSeconds(WAIT_AFTER_PICKME_ANIM);
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
	public void butterflyClicked(GameObject butterfly)
	{
		foreach (GameObject butterflyObj in butterflies)
		{
			CommonGameObject.setObjectCollidersEnabled(butterflyObj, false);
		}

		Audio.play(COFFIN_PICKED);
		int index = System.Array.IndexOf(butterflies, butterfly);

		// We basically parse out the proper gem from the pick itself.
		string selectedGem = currentPick.pick.Substring(currentPick.pick.Length-2);

		// Mark the appropriate coffin as reveals, and play the opening animation using the newly parsed out gem name.
		coffinRevealed[index] = true;
		butterflyAnimators[index].Play(BUTTERFLY_REVEAL_ANIM_PREFIX + selectedGem);

		StartCoroutine(flyToGem(selectedGem, index, butterfly));
	}

	private IEnumerator flyToGem(string selectedGem, int index, GameObject butterfly)
	{		
		GameObject targetGem = null;

		// Finds the gem we want to turn on by its icon designator.
		switch (selectedGem)
		{
			case "M1":
				targetGem = m1Gems[gemCount[selectedGem]];
				Audio.play(REVEAL_UNICORN_VO_SOUND);
				Audio.play(REVEAL_UNICORN_SOUND, 1, 0, 0.5f);
				break;
			case "M2":
				targetGem = m2Gems[gemCount[selectedGem]];
				Audio.play(REVEAL_NORMAL_SOUND, 1, 0, 0.5f);
				break;
			case "M3":
				targetGem = m3Gems[gemCount[selectedGem]];
				Audio.play(REVEAL_NORMAL_SOUND, 1, 0, 0.5f);
				break;
			case "M4":
				targetGem = m4Gems[gemCount[selectedGem]];
				Audio.play(REVEAL_NORMAL_SOUND, 1, 0, 0.5f);
				break;
			case "M5":
				targetGem = m5Gems[gemCount[selectedGem]];
				Audio.play(REVEAL_NORMAL_SOUND, 1, 0, 0.5f);
				break;
		}

		// Now we turn on the sparkle, reposition, and fly it to the gem.
		sparkleTrail.transform.parent = butterfly.transform;
		sparkleTrail.transform.position = butterfly.transform.position;
		sparkleTrail.SetActive(true);
		Audio.play(TRAIL_MOVE_SOUND);
		iTween.MoveTo(sparkleTrail, targetGem.transform.position, TRAIL_MOVE_TIME);
		yield return new TIWaitForSeconds(TRAIL_MOVE_TIME);
		Audio.play(TRAIL_LAND_SOUND);
		sparkleTrail.SetActive(false);

		// Once the sparkle has landed, play the gem reveal.
		targetGem.GetComponent<Animation>().Play(GEM_ANIM_PREFIX + selectedGem + GEM_REVEAL_ANIM_POSTFIX);

		// Then, give it some time and let the gem idle begin.
		StartCoroutine(beginIdleGemAnimation(targetGem, selectedGem));

		// Increase the final count for the gem of this type.
		gemCount[selectedGem] += 1;

		// Game's done under these conditions, end the damn game! (M1 has 3 gems, the rest have 2, hence the || here)
		if ((selectedGem == "M1" && gemCount[selectedGem] == 3) || (selectedGem != "M1" && gemCount[selectedGem] == 2))
		{
			gameEnded = true;

			if (selectedGem == "M1")
			{
				Animator unicornAnimator = allIcons[selectedGem].GetComponent<Animator>();
				if (unicornAnimator != null)
				{
					unicornAnimator.Play(GEM_ANIM_PREFIX + selectedGem + GEM_GLOW_ANIM_POSTFIX);
				}
			}
			else
			{
				allIcons[selectedGem].GetComponent<Animation>().Play(GEM_ANIM_PREFIX + selectedGem + GEM_GLOW_ANIM_POSTFIX);
			}

			Audio.play(FINAL_REVEAL_JACKPOT_SOUND);

			// delay before we start all the reveals so the result sound isn't overlapped by reveal pops
			yield return new TIWaitForSeconds(REVEAL_ALL_WAIT_TIME);
			
			BonusGamePresenter.instance.currentPayout = currentPick.credits;
			StartCoroutine(revealAllCoffins());
		}
		else
		{
			// If we're not ending, get the next pick, and ensure all colldiers are on except for those that have been revealed.
			currentPick = pickemOutcome.getNextEntry();
			for (int i = 0; i < butterflies.Length;i++)
			{
				if (!coffinRevealed[i])
				{
					CommonGameObject.setObjectCollidersEnabled(butterflies[i], true);
				}
			}
		}
	}

	// Merely triggers the idle gem animation on a newly revealed gem.
	private IEnumerator beginIdleGemAnimation(GameObject targetGem, string selectedGem)
	{
		yield return new TIWaitForSeconds(GEM_IDLE_WAIT_TIME);

		if (selectedGem == "M1" && gemCount[selectedGem] == 3)
		{
			targetGem.GetComponent<Animation>().Play(GEM_ANIM_PREFIX + selectedGem + GEM_IDLE_ANIM_POSTFIX);
		}
		else
		{
			targetGem.GetComponent<Animation>().Play(GEM_ANIM_PREFIX + selectedGem + GEM_IDLE_ANIM_POSTFIX);
		}
	}

	private IEnumerator revealAllCoffins()
	{
		// Let's disable all the icons now
		foreach (GameObject butterfly in butterflies)
		{
			CommonGameObject.setObjectCollidersEnabled(butterfly, false);
		}

		yield return new TIWaitForSeconds(REVEAL_WAIT_AFTER_COLLIDER_DISABLE); 

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
			GameObject spriteToReveal = CommonGameObject.findChild(butterflyAnimators[index].gameObject, revealedGem + "_Revealed");
			if (spriteToReveal != null)
			{
				if(!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap(REVEAL_NOT_CHOSEN_SOUND));
				}
				UISprite revealSprite = spriteToReveal.GetComponent<UISprite>();
				if (revealSprite != null)
				{
					revealSprite.color = Color.gray;
				}
			}

			// And let's ensure the reveal animation for the coffin plays.
			butterflyAnimators[index].Play(BUTTERFLY_REVEAL_ANIM_PREFIX + revealedGem);

			// Then get the next one, and let's do this again.
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			currentPick = pickemOutcome.getNextReveal();
		}

		yield return new TIWaitForSeconds(WAIT_BEFORE_SUMMARY_TIME);
		Audio.play(SUMMARY_VO);
		BonusGamePresenter.instance.gameEnded();
	}
}

