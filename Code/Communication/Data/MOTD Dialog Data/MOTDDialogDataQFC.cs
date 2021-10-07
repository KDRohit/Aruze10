using Com.Scheduler;
using QuestForTheChest;

/*
Override for special behavior.
*/

public class MOTDDialogDataQFC : MOTDDialogData
{
	private const string MOTD_KEY = "quest_for_the_chest";
	public override bool shouldShow
	{
		get { return QuestForTheChestFeature.instance.isEnabled; }
	}

	public override string noShowReason
	{
		get { return QuestForTheChestFeature.instance.getInactiveReason(); }
	}

	public override bool show()
	{
		//If this is our first time seeing the map for this event then we're going to queue up the video then the map
		int lastSeenQFCId = CustomPlayerData.getInt(CustomPlayerData.LAST_SEEN_QFC_COMPETITION_ID, 0);
		if (lastSeenQFCId == QuestForTheChestFeature.instance.competitionId)
		{
			QFCMapDialog.showIntro = false;
			//if the map is already queued or is currently open do not show the dialog (rewards or users actions supercede the motd)
			if (!Scheduler.hasTaskWith("quest_for_the_chest_map") && !QFCMapDialog.hasBeenViewedThisSession())
			{
				QFCMapDialog.showDialog(false, MOTD_KEY);
			}
			else
			{
				return false;
			}
		}
		else
		{
			QFCMapDialog.showIntro = true;
			CustomPlayerData.setValue(CustomPlayerData.LAST_SEEN_QFC_COMPETITION_ID, QuestForTheChestFeature.instance.competitionId);
			if (!string.IsNullOrEmpty(ExperimentWrapper.QuestForTheChest.videoUrl))
			{
				StatsManager.Instance.LogCount("dialog", "ptr_v2", "intro_video", "", "", "view");
				VideoDialog.showDialog
				(
					ExperimentWrapper.QuestForTheChest.videoUrl,
					"quest_for_the_chest:immediate",
					"Learn More",
					"quest_for_the_chest",
					0,
					0,
					MOTD_KEY,
					ExperimentWrapper.QuestForTheChest.videoSummaryPath,
					true,
					"",
					"quest_for_the_chest"
				);
			}
			else if (!Scheduler.hasTaskWith("quest_for_the_chest_map") && !QFCMapDialog.hasBeenViewedThisSession())
			{
				QFCMapDialog.showDialog(false, MOTD_KEY);
			}
		}

		return true;
	}

	new public static void resetStaticClassData()
	{
	}

}
