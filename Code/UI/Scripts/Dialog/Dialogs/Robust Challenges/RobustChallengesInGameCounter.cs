using Com.Scheduler;
using UnityEngine;

/*
Controls display of the Robust Challenges in-game counter UI.
*/

public class RobustChallengesInGameCounter : MonoBehaviour
{
	public LabelWrapperComponent robustChallengesTimer;
	[SerializeField] private LabelWrapperComponent robustChallengesProgressLabel;
	[SerializeField] private GameObject checkMark;
	public Animator counterAnimator;

	public ImageButtonHandler robustChallengesButton;
	private bool updatesEnabled = true;

	private void Start()
	{
		if (SpinPanel.instance == null)
		{
			Destroy(gameObject);
		}
		else
		{
			robustChallengesButton.registerEventDelegate(counterClicked);
			SpinPanel.instance.activateFeatureButton(robustChallengesButton.imageButton);
		}
	}

	public void enableUpdates(bool enabled)
	{
		updatesEnabled = enabled;
	}

	private Mission getActiveMission(RobustCampaign robustCampaign)
	{
		Mission mission = robustCampaign.currentMission;
		if (mission == null && robustCampaign.missions != null && robustCampaign.missions.Count > 0)
		{
			//get first uncompleted mission or use the last mission if all are complete
			for (int i = 0; i < robustCampaign.missions.Count; i++)
			{
				mission = robustCampaign.missions[i];
				if (!robustCampaign.missions[i].isComplete)
				{
					break;
				}
			}
		}
		return mission;
	}
	
	private Mission getMissionWithLastCompletedObjective(RobustCampaign robustCampaign)
	{
		Mission mission = null;
		if (robustCampaign.missions != null && robustCampaign.missions.Count > 0)
		{
			//get first uncompleted mission or use the last mission if all are complete
			Mission prev = null;
			for (int i = 0; i < robustCampaign.missions.Count; i++)
			{
				prev = mission;
				mission = robustCampaign.missions[i];
				if (!robustCampaign.missions[i].isComplete)
				{
					if (robustCampaign.missions[i].numObjectivesCompleted > 0)
					{
						return mission;
					}
					else
					{
						return prev;
					}
				}
			}
		}
		return mission;
	}

	public void updateChallengesProgress(RobustCampaign robustCampaign, bool useMissionWithLastCompleteObjective = false)
	{
		if (!updatesEnabled)
		{
			return;
		}
		
		if (robustCampaign != null)
		{
			Mission mission = useMissionWithLastCompleteObjective ? getMissionWithLastCompletedObjective(robustCampaign) : getActiveMission(robustCampaign);
			if (mission == null)
			{
				return;
			}
			
			robustChallengesProgressLabel.text = string.Format
			(
				"{0}/{1}"
				, mission.numObjectivesCompleted.ToString()
				, mission.objectives.Count.ToString()
			);

			if (mission.isComplete)
			{
				checkMark.gameObject.SetActive(true);
				robustChallengesProgressLabel.gameObject.SetActive(false);
			}
			else
			{
				robustChallengesProgressLabel.gameObject.SetActive(true);
			}
		}
		else
		{
			robustChallengesProgressLabel.text = "";
		}
	}

	public void showTimeRemaining(RobustCampaign robustCampaign)
	{
		string timerString = robustCampaign.timerRange.timeRemainingFormatted;
		if (timerString[0] == '0')
		{
			timerString = timerString.Substring(1); // Show 3:11 rather than 03:11.
		}

		robustChallengesTimer.gameObject.SetActive(true);
		robustChallengesTimer.text = timerString;
	}

	public void toggleCheckmark(bool enabled)
	{
		checkMark.SetActive(enabled);
		robustChallengesProgressLabel.gameObject.SetActive(!enabled);
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
}
