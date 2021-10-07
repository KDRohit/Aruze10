using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This class encompasses 3 games, all which behave exactly the same, save for the differences in the prefab itself.
They select one beard, which reveals a face and the amount they win. The rest are revealed as well. The starting faces can
either be 3, 4, or 5, depending on the amount of scatter wins they got.
*/
public class ScatterBonus : ChallengeGame
{
	private const float SCALE_TIME = 0.25f;
	private const float TIME_BETWEEN_REVEALS = 0.25f;
	
	public UILabel title;						// Game title label -  To be removed when prefabs are updated.
	public LabelWrapperComponent titleWrapperComponent;						// Game titleWrapperComponent label

	public LabelWrapper titleWrapper
	{
		get
		{
			if (_titleWrapper == null)
			{
				if (titleWrapperComponent != null)
				{
					_titleWrapper = titleWrapperComponent.labelWrapper;
				}
				else
				{
					_titleWrapper = new LabelWrapper(title);
				}
			}
			return _titleWrapper;
		}
	}
	private LabelWrapper _titleWrapper = null;
	
	public ScatterBonusPanel threeFacePanel;	// Panel for the three face variant of the game
	public ScatterBonusPanel fourFacePanel;		// Panel for the four face variant of the game
	public ScatterBonusPanel fiveFacePanel;		// Panel for the five face variant of the game
	public UILabelStyle disabledStyle;			// Test style
	
	private WheelOutcome wheelOutcome;			// Outcome information form the server
	private WheelPick wheelPick;				// Pick extracted from the outcome

	private List<ScatterBonusHead> currentBonusHeads;	// the current set of heads on the active face number variant panel
	
	private long[] revealedCredits;		// Revealable credit values
	private long wonCredits;			// The amount of credits won
	private string currentWonFace;		// The face the the user got
	private SkippableWait revealWait = new SkippableWait();
	
	private Dictionary<string, string> spriteNameDictionary = new Dictionary<string, string>(); // Mapping of character names to sprites
	
	/**
	Initialize data specific to this game
	*/
	public override void init() 
	{
		wheelOutcome = BonusGameManager.instance.outcomes[BonusGameType.SCATTER] as WheelOutcome;
		
		spriteNameDictionary.Add("Willie", "Willie_icon_m");
		spriteNameDictionary.Add("Jase", "Jase_icon_m");
		spriteNameDictionary.Add("Si", "Si_icon_m");
		spriteNameDictionary.Add("Phil", "Phil_icon_m");
	
		// We have 3 starting panels, depending on how the user enters the game. Let's key off
		// of the game name and make sure the right panel is enabled.
		if (BonusGameManager.instance.summaryScreenGameName.Contains('3'))
		{
			threeFacePanel.gameObject.SetActive(true);
			fourFacePanel.gameObject.SetActive(false);
			fiveFacePanel.gameObject.SetActive(false);

			currentBonusHeads = threeFacePanel.bonusHeads;
		}
		
		if (BonusGameManager.instance.summaryScreenGameName.Contains('4'))
		{
			threeFacePanel.gameObject.SetActive(false);
			fourFacePanel.gameObject.SetActive(true);
			fiveFacePanel.gameObject.SetActive(false);

			currentBonusHeads = fourFacePanel.bonusHeads;
		}
		
		if (BonusGameManager.instance.summaryScreenGameName.Contains('5'))
		{
			threeFacePanel.gameObject.SetActive(false);
			fourFacePanel.gameObject.SetActive(false);
			fiveFacePanel.gameObject.SetActive(true);

			currentBonusHeads = fiveFacePanel.bonusHeads;
		}
		
		// Let's store the text and beard references dynamically based on which panel we've just used.
		for (int i = 0; i < currentBonusHeads.Count; i++)
		{
			currentBonusHeads[i].winAmountTextStyler.labelWrapper.gameObject.SetActive(false);
		}
		
		// Let's get the wheel pick, and all the possible reveals.
		
		wheelPick = wheelOutcome.getNextEntry();
		wonCredits = wheelPick.credits * BonusGameManager.instance.currentMultiplier;
		currentWonFace = wheelPick.extraData;
		
		Debug.Log("Credits you are going to win is: " + wonCredits);
		
		revealedCredits = new long[wheelPick.wins.Count];
		
		for (int j = 0; j < wheelPick.wins.Count; j++)
		{
			if (j != wheelPick.winIndex)
			{
				currentBonusHeads[j].faceName = wheelPick.wins[j].extraData;
				revealedCredits[j] = wheelPick.wins[j].credits * BonusGameManager.instance.currentMultiplier;
				Debug.Log("Next revealed credit is: " + revealedCredits[j]);
			}
		}

		Audio.play("JRWhoHasTheLongestBeard", 1, 0, 1);
		Audio.switchMusicKey("PickABeardBg");
		Audio.stopMusic(0f);

		_didInit = true;
	}
	
	// NGUI button callback when a beard is clicked.
	public void beardClicked(GameObject beard)
	{
		StartCoroutine(showResults(beard));
	}
	
	// Show the results of clicking a beard.
	private IEnumerator showResults(GameObject beard)
	{
		if (BonusGamePresenter.HasBonusGameIdentifier())
			SlotAction.seenBonusSummaryScreen(BonusGamePresenter.NextBonusGameIdentifier());
		
		// Disable the colliders for all beards.
		for (int i = 0; i < currentBonusHeads.Count; i++)
		{
			currentBonusHeads[i].gameObject.GetComponent<Collider>().enabled = false;
		}

		Audio.play("PickABeardFlourish");

		yield return new TITweenYieldInstruction(iTween.ScaleTo(beard.transform.parent.gameObject, iTween.Hash("scale", Vector3.zero, "time", 0.25f, "easetype", iTween.EaseType.easeInSine)));

		Audio.play("BeardExplosion");
		
		int beardIndex = -1;
		
		for (int i = 0; i < currentBonusHeads.Count; i++)
		{
			if (currentBonusHeads[i].gameObject == beard)
			{
				beardIndex = i;
				break;
			}
		}
		
		ScatterBonusHead currentHead = currentBonusHeads[beardIndex];
		currentHead.winAmountTextStyler.labelWrapper.gameObject.SetActive(true);
		currentHead.winAmountTextStyler.labelWrapper.text = CreditsEconomy.convertCredits(wonCredits);
		currentHead.beardSprite.spriteName = spriteNameDictionary[currentWonFace];
		currentHead.beardSprite.MakePixelPerfect();

		titleWrapper.text = currentWonFace;

		yield return new TITweenYieldInstruction(iTween.ScaleTo(beard.transform.parent.gameObject, iTween.Hash("scale", Vector3.one, "time", 0.25f, "easetype", iTween.EaseType.easeInSine)));
		
		yield return new TIWaitForSeconds(0.75f);

		switch (currentWonFace)
		{
			case "Willie":
			{
				Audio.play("WRYouThinkIMeasureMyBeardInReeds");
				break;
			}
			case "Si":
			{
				Audio.play("SiMyBeardIsLongerThanAllYallsBeard");
				break;
			}
			case "Phil":
			case "Jase":
			{
				Audio.play("PRIllWinTheContestBeforeItsOver");
				break;
			}
		}

		// Reveal the remaining beards.
		int revealIndex = 0;

		for (int i = 0; i < currentBonusHeads.Count; i++)
		{
			if (i != beardIndex)
			{
				GameObject beardObject = currentBonusHeads[i].beardSprite.transform.parent.gameObject;	// shorthand
				
				if (!revealWait.isSkipping)
				{
					iTween.ScaleTo(beardObject, iTween.Hash("scale", Vector3.zero, "time", SCALE_TIME, "easetype", iTween.EaseType.easeInSine));
					yield return StartCoroutine(revealWait.wait(SCALE_TIME));
					iTween.Stop(beardObject);	// Just in case reveal skipping happened before the tween finished.
				}
				
				if (revealIndex == wheelPick.winIndex)
				{
					revealIndex++;
				}

				currentBonusHeads[i].winAmountTextStyler.labelWrapper.gameObject.SetActive(true);
				currentBonusHeads[i].winAmountTextStyler.labelWrapper.text = CreditsEconomy.convertCredits(revealedCredits[revealIndex]);
				currentBonusHeads[i].beardSprite.spriteName = spriteNameDictionary[currentBonusHeads[revealIndex].faceName];
				currentBonusHeads[i].beardSprite.MakePixelPerfect();
				currentBonusHeads[i].beardSprite.color = Color.gray;
				currentBonusHeads[i].winAmountTextStyler.updateStyle(disabledStyle);
				
				revealIndex++;

				if (!revealWait.isSkipping)
				{
					iTween.ScaleTo(beardObject, iTween.Hash("scale", Vector3.one, "time", SCALE_TIME, "easetype", iTween.EaseType.easeInSine));
					yield return StartCoroutine(revealWait.wait(SCALE_TIME));
					iTween.Stop(beardObject);	// Just in case reveal skipping happened before the tween finished.
				}
				// Make sure the beard is at full scale, since scaling up only happens if not already skipping.				
				beardObject.transform.localScale = Vector3.one;
				
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}
		
		yield return new TIWaitForSeconds(1.0f);

		Audio.play("BBEndBonus");
		
		BonusGamePresenter.instance.currentPayout = wonCredits;
		BonusGamePresenter.instance.gameEnded();		
	}

	// Basic data structure for use in inspector.
	[System.Serializable]
	public class ScatterBonusPanel
	{
		public GameObject gameObject;
		public List<ScatterBonusHead> bonusHeads;
	}

	// Basic data structure for use in inspector.
	[System.Serializable]
	public class ScatterBonusHead
	{
		public UILabelStyler winAmountTextStyler;
		public UISprite beardSprite;
		[HideInInspector] public string faceName;
		
		// Convenience getter to avoid having to link to the same game object as the beard on an additional property.
		public GameObject gameObject
		{
			get { return beardSprite.gameObject; }
		}
	}

}



