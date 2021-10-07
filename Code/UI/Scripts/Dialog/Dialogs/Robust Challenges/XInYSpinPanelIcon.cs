using Com.Scheduler;
using UnityEngine;

public class XInYSpinPanelIcon : MonoBehaviour
{
	[SerializeField] private UIMeterNGUI keyMeter;
	[SerializeField] private LabelWrapper meterLabel;
	[SerializeField] private ButtonHandler button;
	[SerializeField] private AnimationListController.AnimationInformationList idleAnimation;
	[SerializeField] private AnimationListController.AnimationInformationList resetAnimation;

	private bool clickHandlerRegistered = false;

	public void init(long currentCoins, long requiredCoins, long numSpins, long maxSpins)
	{
		if (requiredCoins> 0 && numSpins >= 0)
		{
			//valid date
			long remainingSpins = maxSpins - numSpins;
			long remainingCoins = requiredCoins - currentCoins;
			keyMeter.setState(currentCoins, requiredCoins, true, 1.0f);
			meterLabel.text = Localize.text(remainingSpins > 1 ? "{0}_coins_in_{1}_spins" : "{0}_coins_in_{1}_spin", CreditsEconomy.multiplyAndFormatNumberAbbreviated(remainingCoins), remainingSpins);
		}
		else
		{
			//invalid data
			keyMeter.currentValue = 0;
			keyMeter.maximumValue = 1;
			meterLabel.text = Localize.text("{0}_coins_in_{1}_spins", 0, 1);
		}
		StartCoroutine(AnimationListController.playListOfAnimationInformation(idleAnimation));
		if (!clickHandlerRegistered)
		{
			button.registerEventDelegate(counterClicked);
			clickHandlerRegistered = true;
		}
		
	}

	public void init(string text, long currentValue, long requiredValue)
	{
		//valid date
		keyMeter.setState(currentValue, requiredValue, true, 1.0f);
		meterLabel.text = Localize.text(text);
		StartCoroutine(AnimationListController.playListOfAnimationInformation(idleAnimation));
		if (!clickHandlerRegistered)
		{
			button.registerEventDelegate(counterClicked);
			clickHandlerRegistered = true;
		}
	}
	
	private void counterClicked(Dict args = null)
	{
		// already about to display the dialog?
		if (Scheduler.hasTaskWith("robust_challenges_motd"))
		{
			return;
		}

		if (SpinPanelLeftIconHandler.robustCampaign != null)
		{
			StatsManager.Instance.LogCount("dialog", "robust_challenges_motd", CampaignDirector.robust.variant, "in_game_icon", (SpinPanelLeftIconHandler.robustCampaign.currentEventIndex + 1).ToString(), "view");
		}
		RobustChallengesObjectivesDialog.showDialog();
	}

	private void OnDestroy()
	{
		button.unregisterEventDelegate(counterClicked);
	}


	public void playResetAnimation()
	{
		StartCoroutine(AnimationListController.playListOfAnimationInformation(resetAnimation));
	}

	public void updateChallengesProgress(ChallengeCampaign campaign)
	{
		if (campaign != null)
		{
			Objective objective = null;
			Mission mission = campaign.currentMission;
			if (mission == null && campaign.missions != null && campaign.missions.Count > 0)
			{
				//get first uncompleted mission or use the last mission if all are complete
				for (int i = 0; i < campaign.missions.Count; i++)
				{
					mission = campaign.missions[i];
					if (!campaign.missions[i].isComplete)
					{
						break;
					}
				}
			}

			if (mission != null)
			{
				for (int i = 0; i < mission.objectives.Count; i++)
				{
					Objective obj = mission.objectives[i];
					if ((!obj.isComplete || i == mission.objectives.Count -1) &&
					    (string.IsNullOrEmpty(obj.game) || obj.game == GameState.game.keyName))
					{
						objective = obj;
						break;
					}
				}
			}
			
			//exit if we have invalid data
			if (objective == null)
			{
				keyMeter.currentValue = 0;
				return;
			}

			long currentValue = objective.currentAmount;
			long requiredValue = objective.amountNeeded;
			
			XinYObjective xInY = objective as XinYObjective;
			if (xInY != null && xInY.constraints != null && xInY.constraints.Count > 0)
			{
				Objective.Constraint activeConstraint = xInY.constraints[0];
				init(currentValue, requiredValue, activeConstraint.amount, activeConstraint.limit);
			}
			else
			{
				init(objective.getShortDescriptionLocalization(), currentValue, requiredValue);
			}
		}
		else
		{
			//use first objective
			keyMeter.currentValue = 0;
		}
	}
}
