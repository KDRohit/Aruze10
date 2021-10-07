using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Com.Rewardables
{
	public class RewardRichPass : Rewardable
	{
		public RichPassCampaign.RewardTrack track { get; private set; }

		public override void init(JSON data)
		{
			if (data == null || CampaignDirector.richPass == null)
			{
				return;
			}
			
			JSON grantedData = data.getJSON("grant_data");
			if (grantedData == null)
			{
				return;
			}

			base.init(grantedData);
			
			isRepeatable = false;
			if (data.getString("type","") == "rp_repeatable_reward_granted")
			{
				isRepeatable = true;
			}
			
			string rewardType = grantedData.getString("reward_type", "");
			int rewardId = data.getInt("reward_id", 0);
			string passType = data.getString("pass_type", "");
			long points = data.getLong("cumulative_points", 0);

			if (data.getString("type", "") == "rp_repeatable_reward_granted")
			{
				points += CampaignDirector.richPass.maximumPointsRequired;
			}
		
			long value = 0;
			if ("coin" == rewardType)
			{
				value = grantedData.getLong("value", 0);
				SlotsPlayer.addNonpendingFeatureCredits(value, "richPass");
			}
		
			//use negative value as default to check for invalid case
			long oldCredits = data.getLong("old_credits", -1);
			long newCredits = data.getLong("new_credits", -1);
		
			RichPassCampaign.RewardTrack track = null;
			switch (passType)
			{
				case "silver":
					track = CampaignDirector.richPass.silverTrack;
					break;
			
				case "gold":
					track = CampaignDirector.richPass.goldTrack;
					break;
			}

			if (track == null)
			{
				Debug.LogError("Invalid rich pass track");
				return;
			}
		
			List<PassReward> rewardsToClaim = track.getSingleRewardsList(points);
			if (rewardsToClaim != null && rewardsToClaim.Count > 0)
			{
				for (int i = 0; i < rewardsToClaim.Count; i++)
				{
					if (rewardsToClaim[i] == null)
					{
						continue;
					}
					if (rewardsToClaim[i].id == rewardId)
					{
						if (rewardsToClaim[i].type != ChallengeReward.RewardType.BASE_BANK &&
						    rewardsToClaim[i].type != ChallengeReward.RewardType.BANK_MULTIPLIER)
						{
							rewardsToClaim[i].Claim();
						}

						break;
					}
				}
			}

			/*
			 TODO: Where did this event come from:
			 
			if (onRichPassRewardableRecieved != null)
			{
				onRichPassRewardableRecieved(track, grantedData);
			}
			*/
			
		}

		public override void consume()
		{
			RewardablesManager.consumeReward(this);
		}

		/// <inheritdoc/>
		public override string type
		{
			get { return "rich_pass"; }
		}
		
		public bool isRepeatable { get; protected set; }
	}
}