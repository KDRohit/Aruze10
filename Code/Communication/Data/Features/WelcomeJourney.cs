using System;
using UnityEngine;
using Zynga.Core.Util;

public class WelcomeJourney : FeatureBase , IResetGame
{

	private const string CLAIM_TIME_PREF = "welcome_journey_last_claim_time";
	private const string CLAIM_DAY_PREF = "welcome_journey_last_claim_day";

	public delegate void WelcomeJourneyEventDelegate(int day, int amount);

	public event WelcomeJourneyEventDelegate onClaimReward;

	private int currentClaimDay;
	private int currentClaimAmount;
	private int lastClaimAmount;
	private int lastClaimTime;
	public bool isFirstLaunch;
	public int[] rewardsList { get; private set; }


	public static WelcomeJourney instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<WelcomeJourney>("welcome_journey");
		}
	}

	public int claimDay
	{
		get { return currentClaimDay; }
	}

	public int claimAmount
	{
		get { return currentClaimAmount; }
	}

	public int previousClaimAmount
	{
		get { return lastClaimAmount; }
	}

	public static int maxRewardDays
	{
		get
		{
			if (instance.rewardsList == null)
			{
				return 0;
			}

			return instance.rewardsList.Length;
		}
	}

	public static bool shouldShow()
	{
		if (instance == null)
		{
			return false;
		}

		bool isValidData = instance.currentClaimDay > 0 && instance.currentClaimAmount > 0;
		bool hasClaimedAllPrizes = instance.currentClaimDay > maxRewardDays;
		bool is24HoursAfterLastClaim = (instance.lastClaimTime + (60 /*seconds*/ * 60 /*minutes*/ * 24 /*hours*/)) < GameTimer.currentTime;
		return !hasClaimedAllPrizes && isValidData && is24HoursAfterLastClaim;

	}


	public bool isInCooldown
	{
		get
		{
			return instance.currentClaimDay < maxRewardDays && 
			       (instance.lastClaimTime + Common.SECONDS_PER_DAY) >= GameTimer.currentTime;
		}
	}

	public string noShowReason
	{
		get
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			bool isValidData = instance.currentClaimDay > 0 && instance.currentClaimAmount > 0;
			bool hasClaimedAllPrizes = instance.currentClaimDay > maxRewardDays;
			bool is24HoursAfterLastClaim = (instance.lastClaimTime + Common.SECONDS_PER_DAY) < GameTimer.currentTime;

			if (!isValidData)
			{
				builder.AppendLine("Data is invalid.");
			}

			if (hasClaimedAllPrizes)
			{
				builder.AppendLine("All prizes have been claimed.");
			}

			if (!is24HoursAfterLastClaim)
			{
				builder.AppendLine("Last claim was less than 24 hours ago.");
			}
			return builder.ToString();
		}
	}

	protected override void initializeWithData(JSON data)
	{
		//parse data
		JSON welcomeJourneyData = data.getJSON("welcome_journey");
		if (welcomeJourneyData != null)
		{
			currentClaimDay = welcomeJourneyData.getInt("claim_day", 0);
			if (currentClaimDay <= 0)
			{
				//data was not in json, fallback to last claim day
				currentClaimDay = welcomeJourneyData.getInt("last_claim_day", 0);
				++currentClaimDay;
			}
			currentClaimAmount = welcomeJourneyData.getInt("claim_amount", 0);
			
			rewardsList = welcomeJourneyData.getIntArray("rewards_list");
			if (rewardsList.Length == 0 && ExperimentWrapper.WelcomeJourney.rewards != null)
			{
				rewardsList = ExperimentWrapper.WelcomeJourney.rewards.ToArray(); //Fallback to EOS rewards if they're not in the feature data
			}
			
			if (currentClaimAmount == 0)  //doesn't get sent down if we're not eligible to claim
			{
				if (currentClaimDay > 0 && rewardsList != null && currentClaimDay < rewardsList.Length)
				{
					currentClaimAmount = rewardsList[currentClaimDay-1];
				}
				else
				{
					currentClaimAmount = 0;
				}
			}
			lastClaimTime = welcomeJourneyData.getInt("last_claim_timestamp", 0);
			//save claim data (in case user has cleared their app data, or we're using the admin tool)
			CustomPlayerData.setValue(CLAIM_DAY_PREF, currentClaimDay);
			CustomPlayerData.setValue(CLAIM_TIME_PREF, lastClaimTime);
		}
		else
		{
			//no days to claim
			try
			{
				currentClaimDay = CustomPlayerData.getInt(CLAIM_DAY_PREF, -1);
				rewardsList = ExperimentWrapper.WelcomeJourney.rewards.ToArray();
				if (currentClaimDay > 0 && rewardsList != null && currentClaimDay < rewardsList.Length)
				{
					currentClaimAmount = rewardsList[currentClaimDay-1];
				}
				else
				{
					currentClaimAmount = 0;
				}
				lastClaimTime = CustomPlayerData.getInt(CLAIM_TIME_PREF, 0);
			}
			catch (Exception e)
			{
				currentClaimDay = -1;
				currentClaimAmount = 0;
				lastClaimTime = 0;
			}
		}

		//set previous claim amount based on current claim day
		if (currentClaimDay > 1 && rewardsList != null && (currentClaimDay-2) < rewardsList.Length)
		{
			lastClaimAmount = rewardsList[currentClaimDay - 2];
		}
		else
		{
			lastClaimAmount = 0;
		}
	}

	public long getNextClaimTime()
	{
		return lastClaimTime + (60 /*seconds*/ * 60 /*seconds/hour*/ * 24 /*hours/day*/);
	}

	public long getLastClaimTime()
	{
		return lastClaimTime;
	}

	public bool isActive()
	{
		if (currentClaimDay < 0 || rewardsList == null)
		{
			return false;
		}

		return currentClaimDay >= 0 && currentClaimDay <= rewardsList.Length;
	}

	public void claimReward()
	{
		Server.registerEventDelegate("welcome_journey_claimed", welcomeRewardClaimed, false);
		WelcomeJourneyAction.claimReward();
	}

	private void welcomeRewardClaimed(JSON data)
	{
		if (null == data)
		{
			Debug.LogWarning("invalid json data for welcome journey action");
			return;
		}
		
		//record the claim time
		lastClaimTime = data.getInt("creation_time", GameTimer.currentTime);

		lastClaimAmount = currentClaimAmount;

		//increment claim day
		++currentClaimDay;

		//get next amount
		if (currentClaimDay <= maxRewardDays)
		{
			currentClaimAmount = rewardsList[currentClaimDay - 1];
		}
		else
		{
			currentClaimAmount = 0;
		}

		//save data
		CustomPlayerData.setValue(CLAIM_DAY_PREF, currentClaimDay);
		CustomPlayerData.setValue(CLAIM_TIME_PREF, lastClaimTime);

		//send event for claimed day/amount
		int claimedDay = data.getInt("claim_day", 0);
		int claimedAmount = data.getInt("claim_amount", 0);

		//invoke handler
		WelcomeJourneyEventDelegate handler = onClaimReward;
		if (handler != null)
		{
			handler(claimedDay, claimedAmount);
		}

		CarouselData carouselData;
		string carouselKey = ExperimentWrapper.WelcomeJourney.isLapsedPlayer ? "welcome_back_journey" : "welcome_journey";
		carouselData = CarouselData.findInactiveByAction(carouselKey);
		if (carouselData != null)
		{
			if (carouselData.getIsValid())
			{
				carouselData.activate();
			}
		}
		else
		{
			carouselData = CarouselData.findActiveByAction(carouselKey);
			if (carouselData != null && LobbyCarouselV3.instance != null)
			{
				LobbyCarouselV3.instance.refreshTimer(carouselData);
			}
		}
	}

}
