using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class AchievementsRewardDialog : DialogBase 
{

	[SerializeField] private GameObject trophyView;
	[SerializeField] private ImageButtonHandler closeButton;
	
	private Achievement achievement = null;
	private bool shouldAbort = false;
	
	public override void init()
	{
		//spawn the trophy view.
		GameObject obj = CommonGameObject.instantiate(trophyView, sizer) as GameObject;
		obj.transform.localPosition = new Vector3(0, 0, 0);
		
		//get the achievement
		achievement = dialogArgs.getWithDefault(D.OPTION, null) as Achievement;
		if (achievement == null)
		{
			Debug.LogError("AchievementsRewardDialog.cs -- init -- trying to init an info panel with a null achievement");
			shouldAbort = true;
			return;
		}

		if (achievement.hasCollectedReward(SlotsPlayer.instance.socialMember))
		{
			Debug.LogError("AchievementsRewardDialog.cs -- init -- Already collected reward!");
			shouldAbort = true;
			return;
		}

		// Turn off the close button unless this is a Network trophy.
		closeButton.gameObject.SetActive(achievement.sku == NetworkAchievements.Sku.NETWORK);

		closeButton.registerEventDelegate(onCloseClicked);
		//display panel
		AchievementsDisplayPanel displayPanel = obj.GetComponent<AchievementsDisplayPanel>();
		if (displayPanel != null)
		{
			displayPanel.init(achievement, SlotsPlayer.instance.socialMember, true, onRewardCollected);
			StartCoroutine(playAnimationRoutine(displayPanel));
		}
		else
		{
			Debug.LogError("AchievementsRewardDialog.cs -- init -- Can't instantiate display panel");
			shouldAbort = true;
			return;
		}

		//stats
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_trophy",
			phylum: "trophy_unlock",
			klass: "view",
			family: achievement == null ? "" : achievement.id,
			genus: "");
	}

	private IEnumerator playAnimationRoutine(AchievementsDisplayPanel displayPanel)
	{
		yield return StartCoroutine(displayPanel.showTrophyAndWait());
		if (achievement.sku == NetworkAchievements.Sku.NETWORK)
		{
			yield return StartCoroutine(displayPanel.playSkuIconAnimations());
		}		
	}

	private void onRewardCollected(Dict args)
	{
		//animate the reward
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_trophy",
			phylum: "trophy_unlock",
			klass: "click",
			family: achievement == null ? "" : achievement.id,
			genus: "");

		Dialog.close();
	}

	private void onCloseClicked(Dict args = null)
	{
		if (achievement.sku == NetworkAchievements.Sku.NETWORK)
		{
			Dialog.close();
		}
		else
		{
			Debug.LogErrorFormat("AchievementsRewardDialog.cs -- onCloseClicked() -- close was clicked from a trophy that is NOT a network trophy, this should not be possible.");
		}
	}

	public override void close()
	{
		// Do special cleanup.
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_trophy",
			phylum: "trophy_unlock",
			klass: "close",
			family: achievement == null ? "" : achievement.id,
			genus: "");
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		if (shouldAbort)
		{
			Dialog.close();
		}
	}
	
	public static void showDialog(Achievement achievement, bool popNow)
	{
		NetworkAchievements.shownAchievementsList.Add(achievement);
		Scheduler.addDialog("achievement_reward",
			Dict.create(D.OPTION, achievement,
				D.PAYOUT_CREDITS, achievement.reward
			), 
			popNow ? SchedulerPriority.PriorityType.IMMEDIATE : SchedulerPriority.PriorityType.HIGH
		);
	}
}
