namespace QuestForTheChest
{
	public class DoSomethingQFC : DoSomethingAction
	{
		public override void doAction(string parameter)
		{
			if (parameter == "video")
			{
				VideoDialog.showDialog
				(
					ExperimentWrapper.QuestForTheChest.videoUrl,
					"quest_for_the_chest:immediate",
					"Learn More",
					"quest_for_the_chest",
					0,
					0,
					"quest_for_the_chest_motd",
					ExperimentWrapper.QuestForTheChest.videoSummaryPath,
					true, 
					"", 
					""
				);
			}
			else if (parameter == "immediate")
			{
				QFCMapDialog.showDialog(true);
			}
			else
			{
				QFCMapDialog.showDialog();
			}
		}

		public override bool getIsValidToSurface(string parameter)
		{
			if (QuestForTheChestFeature.instance.isEnabled)
			{
				string zid = SlotsPlayer.instance.socialMember.zId;
				if (QuestForTheChestFeature.instance.isPlayerOnHomeTeam(zid))
				{
					return true;
				}
			}
			return false;
		}

		public override GameTimer getTimer(string parameter)
		{
			if (parameter == "video")
			{
				return null;
			}

			QuestForTheChestFeature feature = QuestForTheChestFeature.instance;
			if (feature != null && feature.featureTimer != null)
			{
				return feature.featureTimer.endTimer;
			}
			return null;
		}
	}
}
