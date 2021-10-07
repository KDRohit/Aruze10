using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class gwtw01Pickem : PickingGame<NewBaseBonusGameOutcome>
{
	public Animator winboxAnimator;
	public Animator scarlettAnimator;
	public Animator rhettAnimator;

	public UILabel[] multiplierLabels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] multiplierLabelsWrapperComponent;

	public List<LabelWrapper> multiplierLabelsWrapper
	{
		get
		{
			if (_multiplierLabelsWrapper == null)
			{
				_multiplierLabelsWrapper = new List<LabelWrapper>();

				if (multiplierLabelsWrapperComponent != null && multiplierLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in multiplierLabelsWrapperComponent)
					{
						_multiplierLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in multiplierLabels)
					{
						_multiplierLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _multiplierLabelsWrapper;
		}
	}
	private List<LabelWrapper> _multiplierLabelsWrapper = null;	
	
	public UILabel[] multiplierShadowLabels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] multiplierShadowLabelsWrapperComponent;

	public List<LabelWrapper> multiplierShadowLabelsWrapper
	{
		get
		{
			if (_multiplierShadowLabelsWrapper == null)
			{
				_multiplierShadowLabelsWrapper = new List<LabelWrapper>();

				if (multiplierShadowLabelsWrapperComponent != null && multiplierShadowLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in multiplierShadowLabelsWrapperComponent)
					{
						_multiplierShadowLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in multiplierShadowLabels)
					{
						_multiplierShadowLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _multiplierShadowLabelsWrapper;
		}
	}
	private List<LabelWrapper> _multiplierShadowLabelsWrapper = null;	
	
	public UILabel[] multiplierBlueShadowLabels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] multiplierBlueShadowLabelsWrapperComponent;

	public List<LabelWrapper> multiplierBlueShadowLabelsWrapper
	{
		get
		{
			if (_multiplierBlueShadowLabelsWrapper == null)
			{
				_multiplierBlueShadowLabelsWrapper = new List<LabelWrapper>();

				if (multiplierBlueShadowLabelsWrapperComponent != null && multiplierBlueShadowLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in multiplierBlueShadowLabelsWrapperComponent)
					{
						_multiplierBlueShadowLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in multiplierBlueShadowLabels)
					{
						_multiplierBlueShadowLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _multiplierBlueShadowLabelsWrapper;
		}
	}
	private List<LabelWrapper> _multiplierBlueShadowLabelsWrapper = null;	
	
	public UILabel[] multiplierWhiteShadowLabels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] multiplierWhiteShadowLabelsWrapperComponent;

	public List<LabelWrapper> multiplierWhiteShadowLabelsWrapper
	{
		get
		{
			if (_multiplierWhiteShadowLabelsWrapper == null)
			{
				_multiplierWhiteShadowLabelsWrapper = new List<LabelWrapper>();

				if (multiplierWhiteShadowLabelsWrapperComponent != null && multiplierWhiteShadowLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in multiplierWhiteShadowLabelsWrapperComponent)
					{
						_multiplierWhiteShadowLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in multiplierWhiteShadowLabels)
					{
						_multiplierWhiteShadowLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _multiplierWhiteShadowLabelsWrapper;
		}
	}
	private List<LabelWrapper> _multiplierWhiteShadowLabelsWrapper = null;	
	
	public Animator[] multiplierAnimators;
	public Animator[] multiplierWinBoxAnimators;

	private int verticalMultiplierIndex = 0;
	private int horizontalMultiplierIndex = 1;
	private RoundPicks currentRoundPick;
	private BasePick currentPick;
	private Dictionary<int, List<int>> paytableMultipliers;

	private const string MULTIPLIER_ON = "multiplier container on";
	private const string MULTIPLIER_OFF = "multiplier container off start";
	private const string PICKME_ON = "picking object_pick me";
	private const string PICKME_OFF = "picking object_still";
	private const string WINBOX_ON = "winbox celebration ani";
	private const string WINBOX_OFF = "winbox celebration still";
	private const string HORIZONTAL_REVEAL = "picking object_reveal rhett";
	private const string VERTICAL_REVEAL = "picking object_reveal scarlett";
	private const string NUMBER_REVEAL = "picking object_reveal number";
	private const string HORIZONTAL_REVEAL_GRAY = "picking object_reveal not selected rhett";
	private const string VERTICAL_REVEAL_GRAY = "picking object_reveal not selected scarlett";
	private const string NUMBER_REVEAL_GRAY = "picking object_reveal not selected number";
	private const string INCREASE_MULTIPLIER_ON = "increase multiplier ani";
	private const string INCREASE_MULTIPLIER_OFF = "increase multiplier still";
	private const string RHETT_ANIMATION_ON = "rhett_celebration ani";
	private const string RHETT_ANIMATION_OFF = "rhett_celebration still";
	private const string SCARLETT_ANIMATION_ON = "scarlett_celebration ani";
	private const string SCARLETT_ANIMATION_OFF = "scarlett_celebration still";

	private const string INTRO_VO = "BonusIntroVOGWTW";
	private const string CREDIT_SFX = "HPRevealCredit";
	private const string CREDIT_VO = "HPRevealCreditVO";
	private const string SCARLETT_SFX = "HPRevealScarlett";
	private const string SCARLETT_VO = "HPRevealScarlettVO";
	private const string RHETT_SFX = "HPRevealRhett";
	private const string RHETT_VO = "HPRevealRhettVO";
	private const string REVEAL_ALL_SFX = "HPIncreaseAllX";
	private const string VERTICAL_ADVANCE_SFX = "HPAdvanceX";
	private const string PICKME_SFX = "HPPickMeGWTW";

	private const float CREDIT_REVEAL_DELAY = 1.0f;
	private const float FINAL_ALL_REVEAL_DELAY = 0.1f;
	private const float VERTICAL_REVEAL_DELAY = 0.5f;
	private const float HORIZONTAL_REVEAL_DELAY = 0.5f;
	private const float PICKME_DELAY = 1.0f;
	private const float RHETT_INCREASE_DELAY = 0.4f;
	private const float RHETT_VO_DELAY = 0.8f;
	private const float SCARLETT_VO_DELAY = 0.8f;
	
	public override void init()
	{		
		base.init();

		Dictionary<string, JSON> paytablePools = outcome.paytablePools;
		
		// Grab all the items in our paytable pool so we can populate later.
		JSON gwtwPool = paytablePools["GWTW_POOL"];
		JSON[] items = gwtwPool.getJsonArray("items");

		paytableMultipliers = new Dictionary<int, List<int>>();

		// We know there's only 3 indexes, so we hard code that here to make it easier.
		paytableMultipliers.Add(1, new List<int>());
		paytableMultipliers.Add(2, new List<int>());
		paytableMultipliers.Add(3, new List<int>());

		// index on the sort index, and add in the multipliers
		foreach (JSON item in items)
		{
			paytableMultipliers[item.getInt("horizontal_sort_index", 0)].Add(item.getInt("multiplier", 0));
		}

		// Now sort them so we are certain they're in order.
		paytableMultipliers[1].Sort();
		paytableMultipliers[2].Sort();
		paytableMultipliers[3].Sort();

		// Let's now populate the visible indexes according to what we got here.
		for (int i = 0; i < multiplierLabelsWrapper.Count; i++)
		{
			multiplierLabelsWrapper[i].text = Localize.text("{0}X",paytableMultipliers[horizontalMultiplierIndex][i]);
			multiplierShadowLabelsWrapper[i].text = Localize.text("{0}X",paytableMultipliers[horizontalMultiplierIndex][i]);
			multiplierBlueShadowLabelsWrapper[i].text = Localize.text("{0}X",paytableMultipliers[horizontalMultiplierIndex][i]);
			multiplierWhiteShadowLabelsWrapper[i].text = Localize.text("{0}X",paytableMultipliers[horizontalMultiplierIndex][i]);
		}
		
		currentRoundPick = outcome.roundPicks[currentStage];
		currentPick = currentRoundPick.getNextEntry();
		multiplierAnimators[0].Play(MULTIPLIER_ON);
		Audio.play(INTRO_VO);
	}

	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		inputEnabled = false;
		removeButtonFromSelectableList(button);
		int pickIndex = getButtonIndex(button);
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);

		// If we find a credit, its it also means the game is done. Reveal the numbers, add up what's needed, reveal, and end.
		if (currentPick.credits != 0)
		{
			Audio.play(CREDIT_SFX);
			Audio.play(CREDIT_VO);
			pickButtonData.animator.Play(NUMBER_REVEAL);
			pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
			pickButtonData.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
			pickButtonData.multiplierLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
			yield return new TIWaitForSeconds(CREDIT_REVEAL_DELAY);
			winboxAnimator.Play(WINBOX_ON);
			yield return StartCoroutine(animateScore(0, currentPick.credits));
			winboxAnimator.Play(WINBOX_OFF);
			long firstCreditWin = currentPick.credits;
			BonusGamePresenter.instance.currentPayout += (currentPick.credits * paytableMultipliers[horizontalMultiplierIndex][verticalMultiplierIndex]);
			currentPick = currentRoundPick.getNextReveal();

			// Let's start the reveal process.
			while (currentPick != null)
			{
				GameObject revealButton = grabNextButtonAndRemoveIt(currentStage);
				int revealIndex = getButtonIndex(revealButton);
				PickGameButtonData revealButtonData = getPickGameButton(revealIndex);

				if (currentPick.credits != 0)
				{
					revealButtonData.animator.Play(NUMBER_REVEAL_GRAY);
					revealButtonData.extraLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
					revealButtonData.extraOutlineLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
				}
				else if (currentPick.poolKeyName != "" && currentPick.verticalShift != 0)
				{
					revealButtonData.animator.Play(VERTICAL_REVEAL_GRAY);
				}
				else
				{
					revealButtonData.animator.Play(HORIZONTAL_REVEAL_GRAY);
				}

				yield return StartCoroutine(revealWait.wait(revealWaitTime));
				currentPick = currentRoundPick.getNextReveal();
			}

			bonusSparkleTrail.transform.parent = multiplierWinBoxAnimators[horizontalMultiplierIndex].gameObject.transform;
 			bonusSparkleTrail.transform.position = multiplierWinBoxAnimators[horizontalMultiplierIndex].gameObject.transform.position;
			bonusSparkleTrail.SetActive(true);
			iTween.MoveTo(bonusSparkleTrail, winboxAnimator.gameObject.transform.position, 1.0f);
			yield return new TIWaitForSeconds(1.0f);
			bonusSparkleTrail.SetActive(false);

			//yield return new TIWaitForSeconds(CREDIT_REVEAL_DELAY);
			winboxAnimator.Play(WINBOX_ON);
			yield return StartCoroutine(animateScore(firstCreditWin, BonusGamePresenter.instance.currentPayout));
			winboxAnimator.Play(WINBOX_OFF);
			BonusGameManager.instance.multiBonusGamePayout += (BonusGamePresenter.instance.currentPayout * BonusGameManager.instance.currentMultiplier);

			BonusGamePresenter.instance.gameEnded();
		}
		else if (currentPick.poolKeyName != "")
		{
			// Vertical shift basically means the mult index on the screen goes up to the next notch.
			if (currentPick.verticalShift != 0)
			{
				scarlettAnimator.Play(SCARLETT_ANIMATION_ON);
				verticalMultiplierIndex++;
				Audio.play(SCARLETT_SFX);
				Audio.play(SCARLETT_VO, 1, 0, SCARLETT_VO_DELAY);
				pickButtonData.animator.Play(VERTICAL_REVEAL);
				yield return new TIWaitForSeconds(VERTICAL_REVEAL_DELAY);
				bonusSparkleTrail.transform.parent = pickButtonData.animator.gameObject.transform;
 				bonusSparkleTrail.transform.position = pickButtonData.animator.gameObject.transform.position;
				bonusSparkleTrail.SetActive(true);
				Vector3 finalTargetPosition = multiplierWinBoxAnimators[verticalMultiplierIndex].gameObject.transform.position + new Vector3(0,0,-1);
				iTween.MoveTo(bonusSparkleTrail, finalTargetPosition, 1.0f);
				yield return new TIWaitForSeconds(0.5f);
				for (int i = 0; i < multiplierAnimators.Length;i++)
				{
					if (i == verticalMultiplierIndex)
					{
						Audio.play(VERTICAL_ADVANCE_SFX);
						multiplierAnimators[i].Play(MULTIPLIER_ON);
					}
					else
					{
						multiplierAnimators[i].Play(MULTIPLIER_OFF);
					}
				}
				yield return new TIWaitForSeconds(0.5f);
				bonusSparkleTrail.SetActive(false);
				scarlettAnimator.Play(SCARLETT_ANIMATION_OFF);
			}
			else
			{
				rhettAnimator.Play(RHETT_ANIMATION_ON);
				// Horizontal shift means that all multipliers increase. The currently selected mutliplier location remains the same though.
				horizontalMultiplierIndex++;
				Audio.play(RHETT_SFX);
				Audio.play(RHETT_VO, 1, 0, RHETT_VO_DELAY);
				pickButtonData.animator.Play(HORIZONTAL_REVEAL);
				yield return new TIWaitForSeconds(HORIZONTAL_REVEAL_DELAY);
				for (int i = 0; i < multiplierLabelsWrapper.Count; i++)
				{
					Audio.play(REVEAL_ALL_SFX);
					multiplierWinBoxAnimators[i].Play(INCREASE_MULTIPLIER_ON);
					multiplierLabelsWrapper[i].text = Localize.text("{0}X",paytableMultipliers[horizontalMultiplierIndex][i]);
					multiplierShadowLabelsWrapper[i].text = Localize.text("{0}X",paytableMultipliers[horizontalMultiplierIndex][i]);
					multiplierBlueShadowLabelsWrapper[i].text = Localize.text("{0}X",paytableMultipliers[horizontalMultiplierIndex][i]);
					multiplierWhiteShadowLabelsWrapper[i].text = Localize.text("{0}X",paytableMultipliers[horizontalMultiplierIndex][i]);
					yield return new TIWaitForSeconds(RHETT_INCREASE_DELAY);
					multiplierWinBoxAnimators[i].Play(INCREASE_MULTIPLIER_OFF);
				}
				rhettAnimator.Play(RHETT_ANIMATION_OFF);
			}

			currentPick = currentRoundPick.getNextEntry();
			inputEnabled = true;
		}

		yield return null;
	}

	
	protected override IEnumerator pickMeAnimCallback()
	{
		PickGameButtonData pickButton = getRandomPickMe();

		if (pickButton != null)
		{		
			pickButton.animator.Play(PICKME_ON);
			Audio.play(PICKME_SFX);
			
			yield return new TIWaitForSeconds(PICKME_DELAY);
				
			if (isButtonAvailableToSelect(pickButton))
			{
				pickButton.animator.Play(PICKME_OFF);
			}
		}
		yield return new WaitForSeconds(PICKME_DELAY);
	}
}

