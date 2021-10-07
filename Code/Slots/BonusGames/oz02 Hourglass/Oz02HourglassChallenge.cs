using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Oz02HourglassChallenge : ChallengeGame
{

	public UILabel winLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winLabelWrapperComponent;

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
	
	public UILabel bottomPanelLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent bottomPanelLabelWrapperComponent;

	public LabelWrapper bottomPanelLabelWrapper
	{
		get
		{
			if (_bottomPanelLabelWrapper == null)
			{
				if (bottomPanelLabelWrapperComponent != null)
				{
					_bottomPanelLabelWrapper = bottomPanelLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_bottomPanelLabelWrapper = new LabelWrapper(bottomPanelLabel);
				}
			}
			return _bottomPanelLabelWrapper;
		}
	}
	private LabelWrapper _bottomPanelLabelWrapper = null;
	
	public GameObject winBoxRollup;
	public List<Oz02Hourglass> hourglasses;
	public Oz02Multiplier[] multipliers;

	private PickemOutcome outcome;
	private int pickCount = 0;
	private bool isPicking = false;
	private float timeForNextPickme = 0;
	private SkippableWait revealWait = new SkippableWait();
	
	// Constant Variables
	private static int[] MULTIPLIERS = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 15 };  
	private const float HOURGLASS_PICKME_TIMER_MIN = 2.0f;
	private const float HOURGLASS_PICKME_TIMER_MAX = 5.0f;
	private const string PICK_AN_HOURGLASS = "pick_an_hourglass";				// The name of the localized text telling the player to make a choice between hourglasses.
	private const string GAME_OVER = "game_over";								// Text localized when the game is over.
	private const float TIME_AFTER_HG_UPDATE = 0.25f;							// Time to wait after the HG value has been multiplied.
	private const string MULTIPLICATION_STRING = "{0}_x_{1}_equals_{2}";		// Formated localized string that displays the multiplcation information.
	private const float TIME_BETWEEN_REVEALS = 0.25f;							// Time between each reveal at the end of the game.
	private const float TIME_GAME_END = 1.0f;									// That famous sink in time.
	private const float TIME_TO_MOVE_MULTIPLIER = 1.0f;							// How long it takes to get the multiplier from the top to the hourglass.


	public override void init() 
	{
		outcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		winLabelWrapper.text = CreditsEconomy.convertCredits(0);
		
		// Make sure the hourglasses are in a state ready to be played.
		foreach (Oz02Hourglass hg in hourglasses)
		{
			hg.setup();
		}

		// Set up the multipliers are initialized with the correct value.
		for (int i = 0; i < multipliers.Length; i++)
		{
			multipliers[i].init(MULTIPLIERS[i]);
		}
		bottomPanelLabelWrapper.text = Localize.text(PICK_AN_HOURGLASS);
		// Activate the first multiplier.
		multipliers[0].setActive();

		_didInit = true;
		timeForNextPickme = Time.time + Random.Range(HOURGLASS_PICKME_TIMER_MIN, HOURGLASS_PICKME_TIMER_MAX);

	}

	protected override void Update()
	{
		base.Update();
		if (Time.time > timeForNextPickme)
		{
			timeForNextPickme = Time.time + Random.Range(HOURGLASS_PICKME_TIMER_MIN, HOURGLASS_PICKME_TIMER_MAX);
			int randomIndex = Random.Range(0, hourglasses.Count);
			StartCoroutine(hourglasses[randomIndex].playPickMeAnimation());
		}

	}

	public void onPickSelected(GameObject button)
	{
		if (!isPicking)
		{
			isPicking = true;
			// Let's find which button index was clicked on.	
			Oz02Hourglass hgComponent = button.GetComponent<Oz02Hourglass>();
			int index = hourglasses.IndexOf(hgComponent);
			PickemPick pick = outcome.getNextEntry();

			if (pick == null)
			{
				StartCoroutine(revealRemainingPicks());
			}
			else if (pick.isGameOver)
			{
				StartCoroutine(revealLosingChoice(index, pick, hgComponent));
			}
			else if ((pickCount + 1) >= multipliers.Length)
			{
				//special end case for when we run out of picks
				StartCoroutine(revealFinalPick(index, pick, hgComponent));
			}
			else
			{
				StartCoroutine(revealWinningChoice(index, pick, hgComponent));
			}
		}
	}

	private IEnumerator revealFinalPick(int index, PickemPick pick, Oz02Hourglass hgComponent)
	{
		yield return StartCoroutine(revealWinningChoice(index, pick, hgComponent));
		StartCoroutine(revealRemainingPicks());
	}

	private IEnumerator revealLosingChoice(int index, PickemPick pick, Oz02Hourglass hgComponent)
	{
		int myPickCount = pickCount;
		//Hide the "Pick an Hourglass" label
		bottomPanelLabelWrapper.gameObject.SetActive(false);

		//disable hourglass glows
		foreach (Oz02Hourglass hg in this.hourglasses)
		{
			hg.setGlowActive(false);
		}
		// reveal loss sequence on the hourglass 
		yield return StartCoroutine(hgComponent.revealLoss(pick.credits));

		//Show the "Pick an Hourglass" label, switched to Game Over
		bottomPanelLabelWrapper.text = Localize.textUpper(GAME_OVER);
		bottomPanelLabelWrapper.gameObject.SetActive(true);

		//Fizzle the multiplier fire
		StartCoroutine(this.multipliers[myPickCount].fizzle());		

		//Do credit roll
		long initialCredits = BonusGamePresenter.instance.currentPayout;
		BonusGamePresenter.instance.currentPayout += pick.credits;
		winBoxRollup.SetActive(true);
		yield return StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, winLabelWrapper));

		//Show distractors
		yield return StartCoroutine(revealRemainingPicks());	
	}

	//This handles the reveal sequence for a winning pick
	private IEnumerator revealWinningChoice(int index, PickemPick pick, Oz02Hourglass hgComponent)
	{
		int myPickCount = pickCount;
		pickCount++;
		//Hide the "Pick an Hourglass" label
		bottomPanelLabelWrapper.gameObject.SetActive(false);

		//turn of glows
		foreach (Oz02Hourglass hg in hourglasses)
		{
			hg.setGlowActive(false);
		}
		hourglasses.RemoveAt(index);
		//do reveal win sequence
		yield return StartCoroutine(hgComponent.revealWin(pick.credits));
		// THe pick has been revealed, you can pick again.
		isPicking = false;

		if (pickCount < multipliers.Length)
		{
			// Light up the next one because you can pick again.
			multipliers[pickCount].setActive();

			// Enable the glows again so players know they can pick
			foreach (Oz02Hourglass hg in hourglasses)
			{
				hg.setGlowActive(true);
			}
		}

		bottomPanelLabelWrapper.gameObject.SetActive(true);
		long winAmount = MULTIPLIERS[myPickCount] * pick.credits;
		long initialCredits = BonusGamePresenter.instance.currentPayout;
		BonusGamePresenter.instance.currentPayout += winAmount;
		bottomPanelLabelWrapper.text = Localize.text(MULTIPLICATION_STRING, CreditsEconomy.convertCredits(pick.credits), MULTIPLIERS[myPickCount], CreditsEconomy.convertCredits(winAmount));

		//activate the flame trail
		multipliers[myPickCount].turnOnTrail();

		//Tween the multiplier object
		iTween.MoveTo(multipliers[myPickCount].gameObject, new Vector3(hgComponent.transform.position.x, hgComponent.transform.position.y, multipliers[myPickCount].transform.position.z), TIME_TO_MOVE_MULTIPLIER);


		yield return new TIWaitForSeconds(TIME_TO_MOVE_MULTIPLIER);

		//Multiplier explosion 
		StartCoroutine(multipliers[myPickCount].explode());

		//Rollup effect
		winBoxRollup.SetActive(true);
		//Do credit roll up
		hgComponent.updateWinLabel(winAmount);
		yield return StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, winLabelWrapper));		
		winBoxRollup.SetActive(false);
		yield return new TIWaitForSeconds(TIME_AFTER_HG_UPDATE);	 
		// We don't need to keep track of that hourglass anymore.
	   	hourglasses.Remove(hgComponent);
		//Hide the used multiplier
		multipliers[myPickCount].hide();
		Destroy(multipliers[myPickCount].gameObject);

		bottomPanelLabelWrapper.text = Localize.text(PICK_AN_HOURGLASS);

	}

	private IEnumerator revealRemainingPicks()
	{
		PickemPick pick;
		foreach (Oz02Hourglass hg in hourglasses)
		{
			yield return null;
						
			//If this is an unchosen hourglass
			if (!hg.alreadyChosen)
			{
				pick = outcome.getNextReveal();
				if (pick != null)
				{
					hg.doDistractor(pick.isGameOver, pick.credits);
					yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
				}
			}
		}
		StartCoroutine(endGame());   
	}

	private IEnumerator endGame()
	{
		yield return new WaitForSeconds(TIME_GAME_END);
		BonusGamePresenter.instance.gameEnded();
	}
}

