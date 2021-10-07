using UnityEngine;
using System.Collections;
using TMPro;

public class SlotventuresChallengeTab : MonoBehaviour
{
	public TextMeshPro description;
	public TextMeshPro challengeProgressText;
	public TextMeshPro simpleDescription;

	// On the flyout card when you complete it. Static?
	public TextMeshPro challengeUpdateText;

	public Animator tabAnimator;
	public UISprite greenFillSprite;
	public long currentProgress = 0;
	public long currentSpinCount = 0;
	public bool wasComplete = false;

	[SerializeField] private AnimationListController.AnimationInformationList progressAnimation;
	[SerializeField] private AnimationListController.AnimationInformationList completeAnimation;
	[SerializeField] private AnimationListController.AnimationInformationList resetAnimation;
	[SerializeField] private AnimationListController.AnimationInformationList closedCompleteAnimation;
	[SerializeField] private AnimationListController.AnimationInformationList completeIdleAnimation;

	private bool usesPercentage = false;


	// This all gets setup by the objective panel. 
	public void init(Objective objective)
	{
		if (objective.shouldRetryGettingSymbol)
		{
			objective.formatSymbol();
			objective.buildLocString();
		}

		description.text = objective.description;
		challengeUpdateText.text = objective.description;
		simpleDescription.text = objective.localizedChallengeType();

		if (objective.currentAmount >= objective.amountNeeded)
		{
			// This is so we don't play the complete animation every time
			wasComplete = true;
			challengeUpdateText.text = objective.description;
			challengeProgressText.text = objective.getCompletedProgressText();
			playCompleteIdleAnimation();
		}
		else
		{
			challengeProgressText.text = objective.getProgressText();
			currentProgress = objective.currentAmount;
			if (objective.type == XinYObjective.X_COINS_IN_Y)
			{
				XinYObjective xInY = objective as XinYObjective;
				if (xInY != null && xInY.constraints != null && xInY.constraints.Count > 0)
				{
					currentSpinCount = xInY.constraints[0].amount;
				}
			}
			greenFillSprite.fillAmount = (float)objective.currentAmount / (float)objective.amountNeeded;
		}
	}

	public void updateCounts(Objective objective, bool isSlidOut)
	{
		XinYObjective xInY = null;
		if (objective.type == XinYObjective.X_COINS_IN_Y)
		{
			xInY = objective as XinYObjective;
		}

		// If we haven't finished
		if (objective.currentAmount < objective.amountNeeded)
		{
			if (currentProgress < objective.currentAmount)
			{
				playProgressionAnimation();
			}
			else if (currentProgress > objective.currentAmount ||
			         (xInY != null && xInY.constraints != null && xInY.constraints.Count > 0 && currentSpinCount > xInY.constraints[0].amount))
			{
				challengeUpdateText.text = Localize.text("slotventure_progress_reset");
				playResetAnimation();
			}

			if (xInY != null && xInY.constraints != null && xInY.constraints.Count > 0)
			{
				currentSpinCount = xInY.constraints[0].amount;
			}
			
			currentProgress = objective.currentAmount;
		}
		else if (!wasComplete)
		{
			challengeUpdateText.text = objective.description;
			if (isSlidOut)
			{
				playCompleteAnimation();
			}
			else
			{
				playCloseAnimation();
			}
			wasComplete = true;
		}

		challengeProgressText.text = objective.getProgressText();
		description.text = objective.description;
		greenFillSprite.fillAmount = (float)objective.currentAmount / (float)objective.amountNeeded;
	}

	public void playProgressionAnimation()
	{
		if (gameObject != null && gameObject.activeInHierarchy)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(progressAnimation));
		}
	}

	public void playResetAnimation()
	{
		if (gameObject != null && gameObject.activeInHierarchy)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(resetAnimation));
		}
	}

	public void playCompleteAnimation()
	{
		if (gameObject != null && gameObject.activeInHierarchy)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(completeAnimation));
		}
	}

	public void playCloseAnimation()
	{
		if (gameObject != null && gameObject.activeSelf)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(closedCompleteAnimation));
		}
	}

	public void playCompleteIdleAnimation()
	{
		if (gameObject != null && gameObject.activeSelf)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(completeIdleAnimation));
		}
	}

	public void forceFinishedState(Objective objective, bool isSlidOut)
	{
		challengeProgressText.text = objective.getCompletedProgressText();
		challengeUpdateText.text = objective.description;
		greenFillSprite.fillAmount = 1f;
		if (isSlidOut)
		{
			// If we have and we're not in the collapsed state
			StartCoroutine(AnimationListController.playListOfAnimationInformation(completeAnimation));
		}
		else
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(closedCompleteAnimation));
		}
	}

}
