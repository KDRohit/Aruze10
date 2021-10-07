using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.HitItRich.EUE
{
	public class EueCampaign : ChallengeCampaign
	{
		public override void init(JSON data = null)
		{
			campaignID = CampaignDirector.EUE_FTUE;
			base.init(data);			
		}

#if !ZYNGA_PRODUCTION
		public void showCurrentMissionComplete()
		{
			if (!Debug.isDebugBuild)
			{
				return;
			}
			string jsonString = "{ \"event_index\":" + currentEventIndex + " }";
			JSON data = new JSON(jsonString);
			List<JSON> eventList = new List<JSON>() {data};
			showMissionComplete(eventList);
		}
#endif

		protected override void showMissionComplete(List<JSON> completionJSON)
		{
			base.showMissionComplete(completionJSON);

			if (completionJSON != null)
			{
				for (int i = 0; i < completionJSON.Count; i++)
				{
					if (completionJSON[i] == null)
					{
						continue;
					}
					int completedEventIndex = completionJSON[i].getInt("event_index", 0) - 1;

					// It's probably on replay and we just rolled over
					if (completedEventIndex == -1 && shouldRepeat)
					{
						completedEventIndex = missions.Count - 1;
					}

					long totalCredits = getTotalCreditRewardForMission(completedEventIndex);
					if (totalCredits > 0)
					{
						SlotsPlayer.addNonpendingFeatureCredits(totalCredits, "eueFtueCampaign");
					}
				}	
			}

			EUEManager.onChallengeComplete();
		}

		protected override void showTypeComplete(List<JSON> completionJSON)
		{
			base.showTypeComplete(completionJSON);
			EUEManager.onChallengeComplete();
		}

		protected override void showCampaignComplete(List<JSON> completionJSON)
		{
			base.showCampaignComplete(completionJSON);
			int completedEventIndex = missions.Count - 1;
			long totalCredits = getTotalCreditRewardForMission(completedEventIndex);
			if (totalCredits > 0)
			{
				SlotsPlayer.addNonpendingFeatureCredits(totalCredits, "eueFtueCampaign");	
			}

			//show objective complete on the in game counter
			EUEManager.onChallengeComplete();
		}
	}
}
