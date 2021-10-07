using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Zynga.Core.Util;

namespace Com.HitItRich.Feature.TimedBonus
{
	/// <summary>
	/// Class used to control a feature that runs on a cooldown.  This can be collected by the user or their pet.
	/// </summary>
	public class TimedBonusFeature
	{
		public int bonusCollectInterval
		{
			get
			{
				if (lastCollectedBonus == null)
				{
					return -1;
				}

				return lastCollectedBonus.nextCollectTime - lastCollectedBonus.claimTime;
			}
		}

		public int nextCollectDateTime
		{
			get
			{
				if (lastCollectedBonus == null)
				{
					return -1;
				}

				return lastCollectedBonus.nextCollectTime;
			}
		}
		public TimedBonusData lastCollectedBonus { get; private set; }

		public TimedBonusFeature(JSON data)
		{
			initDailyBonusOutcome(data);
		}
		
		protected void initDailyBonusOutcome(JSON outcome)
		{
			if (outcome == null)
			{
				return;
			}

			lastCollectedBonus = new DailyBonusData(outcome);
		}
		
		public static void userCollectBonus()
		{
			// This is the timestamp of 24 hours into the future when the super streak will expire if not renewed.
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			prefs.SetInt(Prefs.SUPER_STREAK_EXPIRATION_TIME, GameTimer.currentTime + Common.SECONDS_PER_DAY);
			prefs.Save();
			
			sendCreditClaimAction(false);
		}

		public static void petCollectBonus(int timestamp)
		{
			// This is the timestamp of 24 hours into the future from when the pet collected the bonus
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			prefs.SetInt(Prefs.SUPER_STREAK_EXPIRATION_TIME, timestamp + Common.SECONDS_PER_DAY);
			prefs.Save();
			
			sendCreditClaimAction(true);
		}

		private static void sendCreditClaimAction(bool isPetCollect)
		{
			string bonusString = "bonus";
			if (ExperimentWrapper.NewDailyBonus.isInExperiment)
			{
				bonusString = ExperimentWrapper.NewDailyBonus.bonusKeyName;
			}

			if (SlotsPlayer.instance.dailyBonusTimer.day > 7)
			{
				// We now just randomly pick a day for the user to pick.
				CreditAction.claimTimerCredits(Random.Range(1,7), bonusString, false, isPetCollect);// * (7-1) + 1
			}
			else
			{
				CreditAction.claimTimerCredits(-1, bonusString, false, isPetCollect);
			}
		}
	}
}

