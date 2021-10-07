using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Dog house bonus game logic for Gen04 game...which is basically the tanker game.

public class DogHouse : ChallengeGame 
{
	public UILabel winValue; 							// Final Win Label -  To be removed when prefabs are updated.
	public LabelWrapperComponent winValueWrapperComponent; 							// Final Win Label

	public LabelWrapper winValueWrapper
	{
		get
		{
			if (_winValueWrapper == null)
			{
				if (winValueWrapperComponent != null)
				{
					_winValueWrapper = winValueWrapperComponent.labelWrapper;
				}
				else
				{
					_winValueWrapper = new LabelWrapper(winValue);
				}
			}
			return _winValueWrapper;
		}
	}
	private LabelWrapper _winValueWrapper = null;
	
	public UILabel innerWinValue;						// Final Win Label for inside the doghouse itself. -  To be removed when prefabs are updated.
	public LabelWrapperComponent innerWinValueWrapperComponent;						// Final Win Label for inside the doghouse itself.

	public LabelWrapper innerWinValueWrapper
	{
		get
		{
			if (_innerWinValueWrapper == null)
			{
				if (innerWinValueWrapperComponent != null)
				{
					_innerWinValueWrapper = innerWinValueWrapperComponent.labelWrapper;
				}
				else
				{
					_innerWinValueWrapper = new LabelWrapper(innerWinValue);
				}
			}
			return _innerWinValueWrapper;
		}
	}
	private LabelWrapper _innerWinValueWrapper = null;
	
	public UILabel[] revealedValues;					// Revealed Values underneath the selections -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealedValuesWrapperComponent;					// Revealed Values underneath the selections

	public List<LabelWrapper> revealedValuesWrapper
	{
		get
		{
			if (_revealedValuesWrapper == null)
			{
				_revealedValuesWrapper = new List<LabelWrapper>();

				if (revealedValuesWrapperComponent != null && revealedValuesWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealedValuesWrapperComponent)
					{
						_revealedValuesWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in revealedValues)
					{
						_revealedValuesWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _revealedValuesWrapper;
		}
	}
	private List<LabelWrapper> _revealedValuesWrapper = null;	
	
	public UILabel[] winAllTexts;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] winAllTextsWrapperComponent;

	public List<LabelWrapper> winAllTextsWrapper
	{
		get
		{
			if (_winAllTextsWrapper == null)
			{
				_winAllTextsWrapper = new List<LabelWrapper>();

				if (winAllTextsWrapperComponent != null && winAllTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in winAllTextsWrapperComponent)
					{
						_winAllTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in winAllTexts)
					{
						_winAllTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _winAllTextsWrapper;
		}
	}
	private List<LabelWrapper> _winAllTextsWrapper = null;	
	
	public UILabel[] endsTexts;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] endsTextsWrapperComponent;

	public List<LabelWrapper> endsTextsWrapper
	{
		get
		{
			if (_endsTextsWrapper == null)
			{
				_endsTextsWrapper = new List<LabelWrapper>();

				if (endsTextsWrapperComponent != null && endsTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in endsTextsWrapperComponent)
					{
						_endsTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in endsTexts)
					{
						_endsTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _endsTextsWrapper;
		}
	}
	private List<LabelWrapper> _endsTextsWrapper = null;	
	

	public UISprite[] winAllIcons;
	public UISprite[] endsIcons;

	public UILabelMultipleEffectsStyle grayedOutTextStyle;
	public UILabelMultipleEffectsStyle grayedOutCreditTextStyle;

	private CoroutineRepeater pickMeController;

	public GameObject[] icons;							// Selectable icons
	public GameObject[] animParent;						// Animation gameobjects for the icons

	public GameObject outsideHolder; 					// holder for all of the outside elements
	public GameObject insideHolder; 					// holder for all of the inside elements

	private PickemOutcome pickemOutcome; 				// outcome for this game
	private PickemPick currentPick; 					// the current pick from the outcome
	
	private SkippableWait revealWait = new SkippableWait();

	private bool canTouch = true;
	private bool gameEnded = false;

	private const float TIME_BETWEEN_REVEALS = 0.25f;						// Time between each reveal
	private const float TIME_BETWEEN_PICK_ME = 1.0f;
	private const float MIN_TIME_PICKME = 3.0f;								// Minimum time pickme animation might take to play next
	private const float MAX_TIME_PICKME = 8.0f;								// Maximum time pickme animation might take to play next

	private const string DOGHOUSE_BONUS_LOOP = "doghouse_bonus_loop";
	private const string CREDITS_HIT = "doghouse_higher";
	private const string END_HIT = "doghouse_guess_wrong";
	private const string WIN_ALL_HIT = "doghouse_guess_right";
	private const string REVEAL_ICON = "doghouse_lower";
	private const string PUP_PARTY = "PupParty";
	private const string PUP_BARKS = "pup_barks";


	public override void init()
	{
		Audio.switchMusicKeyImmediate(DOGHOUSE_BONUS_LOOP);

		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeCallback);
	
		pickemOutcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		currentPick = pickemOutcome.getNextEntry();
	}

	private IEnumerator pickMeCallback()
	{
		if (icons.Length > 0)
		{
			int pickMeIndex = Random.Range(0, icons.Length);

			UISprite boneIcon = icons[pickMeIndex].GetComponent<UISprite>();
			if (boneIcon != null && boneIcon.alpha == 1)
			{
				Audio.play("rollover_sparkly");
				animParent[pickMeIndex].GetComponent<Animation>().Play("GEN04_Pawpalooza_DHB_Bone_Pickme");
				yield return new TIWaitForSeconds(animParent[pickMeIndex].GetComponent<Animation>()["GEN04_Pawpalooza_DHB_Bone_Pickme"].length);
			}

			yield return new TIWaitForSeconds(TIME_BETWEEN_PICK_ME);
		}
	}

	// Our callback from the clicked bone.
	public void iconClicked(GameObject icon)
	{
		if (canTouch)
		{
			canTouch = false;
			int index = System.Array.IndexOf(icons, icon);
			CommonGameObject.setObjectCollidersEnabled(icon, false);
			StartCoroutine(revealChosenIcon(index, icon));
		}
	}

	protected override void Update()
	{
		base.Update();
		// We only want to be able to play the pickme animations if we actually pick stuff.
		if (!gameEnded && canTouch && pickMeController != null)
		{
			pickMeController.update();
		}
	}

	// Coroutine that handles the selections and what is revealed.
	private IEnumerator revealChosenIcon(int index, GameObject icon)
	{
		if (!currentPick.isGameOver && !currentPick.isCollectAll)
		{
			// The picks #'s are in the picks themselves, so we multiply against the base wager as a result.
			long newCredits = System.Int32.Parse(currentPick.pick) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			revealedValuesWrapper[index].text = CreditsEconomy.convertCredits(newCredits);
			animParent[index].GetComponent<Animation>().Play("GEN04_Pawpalooza_DHB_Bone_reveal credit");
			BonusGamePresenter.instance.currentPayout += newCredits;
			currentPick = pickemOutcome.getNextEntry();

			Audio.play(CREDITS_HIT);
			// There's a little animation on selection, give it some time before rolling up.
			yield return new TIWaitForSeconds(0.1f);
			yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout - newCredits, BonusGamePresenter.instance.currentPayout, winValueWrapper));
			canTouch = true;
		}
		else
		{
			gameEnded = true;
			// It's a win or lose, let's disable all the icons now.
			for (int i = 0; i < icons.Length;i++)
			{
				CommonGameObject.setObjectCollidersEnabled(icons[i], false);
			}

			if (currentPick.isCollectAll)
			{
				Audio.play(WIN_ALL_HIT, 1, 0, 1);
				// Let's show the anim, reveals, show the inside, and end the game.

				animParent[index].GetComponent<Animation>().Play("GEN04_Pawpalooza_DHB_Bone_reveal win all");

				yield return StartCoroutine(revealAllIcons(true));

				yield return new TIWaitForSeconds(3.0f);

				insideHolder.SetActive(true);
				innerWinValueWrapper.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
				outsideHolder.SetActive(false);
				Audio.switchMusicKeyImmediate(PUP_PARTY);
				
				yield return new TIWaitForSeconds(3.0f);

				BonusGamePresenter.instance.gameEnded();
			}
			else
			{
				Audio.play(END_HIT, 1, 0, 1);
				// Let's just do the reveals
				animParent[index].GetComponent<Animation>().Play("GEN04_Pawpalooza_DHB_Bone_reveal Ends");

				yield return StartCoroutine(revealAllIcons(false));
			}
		}
	}

	// Handles the reveals. Slightly different from above as we cache the reveals in order to roll up once.
	private IEnumerator revealAllIcons(bool winAll)
	{
		yield return new TIWaitForSeconds(0.5f);
		List<PickemPick> remainingReveals = new List<PickemPick>();
		long totalRevealedCredits = 0;
		PickemPick pick;
		
		// The actual caching of the reveals and adding in the total credits.
		PickemPick revealedPick = null;
		do
		{
			revealedPick = pickemOutcome.getNextReveal();
			if (revealedPick != null)
			{
				remainingReveals.Add(revealedPick);
				if (!revealedPick.isGameOver && !revealedPick.isCollectAll)
				{
					totalRevealedCredits += (System.Int32.Parse(revealedPick.pick) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers);
				}
			}
		}
		while (revealedPick != null);
		
		// Only rollup and add it in if its a win all situation.
		if (winAll)
		{
			long initialCredits = BonusGamePresenter.instance.currentPayout;
			BonusGamePresenter.instance.currentPayout += totalRevealedCredits;
			StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, winValueWrapper));
		}

		// Now we do the standard reveals.
		int revealIndex = 0;
		for (int i = 0; i < icons.Length;i++)
		{
			UISprite boneIcon = icons[i].GetComponent<UISprite>();
			if (boneIcon.alpha == 1)
			{
				pick = remainingReveals[revealIndex];
				revealIndex++;
				if (pick != null)
				{
					if (!pick.isGameOver && !pick.isCollectAll)
					{
						long newCredits = System.Int32.Parse(pick.pick) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
						revealedValuesWrapper[i].text = CreditsEconomy.convertCredits(newCredits);
						UILabelMultipleEffectsStyler styler = revealedValuesWrapper[i].gameObject.GetComponent<UILabelMultipleEffectsStyler>();
						if (styler != null && !winAll)
						{
							styler.style = grayedOutCreditTextStyle;
							styler.updateStyle();
						}
						animParent[i].GetComponent<Animation>().Play("GEN04_Pawpalooza_DHB_Bone_reveal credit");
					}
					else
					{
						UILabelMultipleEffectsStyler styler;
						if (pick.isCollectAll)
						{
							styler = winAllTextsWrapper[i].gameObject.GetComponent<UILabelMultipleEffectsStyler>();
							animParent[i].GetComponent<Animation>().Play("GEN04_Pawpalooza_DHB_Bone_reveal win all");
							if (!winAll)
							{
								winAllIcons[i].color = Color.gray;
							}
						}
						else
						{
							styler = endsTextsWrapper[i].gameObject.GetComponent<UILabelMultipleEffectsStyler>();
							animParent[i].GetComponent<Animation>().Play("GEN04_Pawpalooza_DHB_Bone_reveal Ends");
							endsIcons[i].color = Color.gray;
						}

						if (styler != null && (!winAll || !pick.isCollectAll))
						{
							styler.style = grayedOutTextStyle;
							styler.updateStyle();
						}
					}
					if(!revealWait.isSkipping)
					{
						Audio.play(REVEAL_ICON);
					}
					yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
				}
			}
		}

		// If we didn't win, just end it already.
		if (!winAll)
		{
			yield return new TIWaitForSeconds(1.5f);
			BonusGamePresenter.instance.gameEnded();
		}
	}
}

