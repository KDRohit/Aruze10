using System.Text;
using UnityEngine;

namespace Com.HitItRich.Feature.OOCRebound
{
	public class OOCReboundFeature : FeatureBase, IResetGame
	{
		public const string LOGIN_DATA_KEY = "ooc";

		private static OOCReboundFeature instance = null;
		private static bool isWaitingForActionResponse = false;

		private int spinCount = 0;
		private int oocCount = 0;
		private int initTime = 0;

		protected override void registerEventDelegates()
		{
			base.registerEventDelegates();
			Server.registerEventDelegate("ooc_reward_grant", handleOOCDialogEvent, true);
			Server.registerEventDelegate("ooc_inbox_messages_sent", handleOOCInboxEvent, true);
		}

		protected override void clearEventDelegates()
		{
			base.clearEventDelegates();
			Server.unregisterEventDelegate("ooc_reward_grant", handleOOCDialogEvent, true);
			Server.unregisterEventDelegate("ooc_inbox_messages_sent", handleOOCInboxEvent, true);
		}

		public override void initFeature(JSON data)
		{
			base.initFeature(data);

			spinCount = data.getInt("spin_count", 0);
			oocCount = data.getInt("ooc_count", 0);
			initTime = data.getInt("initialization_time", 0);
		}

		private static int timeLimit
		{
			get
			{
				//experiment data is in hours
				return ExperimentWrapper.SpecialOutOfCoins.experimentData.timeLimit * Common.SECONDS_PER_HOUR;
			}
		}

		public static bool isAvailableForCollect
		{
			get
			{
				if (ExperimentWrapper.SpecialOutOfCoins.experimentData.isInExperiment  &&
						instance != null && 
						instance.spinCount < ExperimentWrapper.SpecialOutOfCoins.experimentData.spinLimit &&  
						instance.oocCount < ExperimentWrapper.SpecialOutOfCoins.experimentData.oocLimit)
				{

					int timeElapsed = GameTimer.currentTime - instance.initTime;
					return timeElapsed < timeLimit;
				}

				return false;
			}
		}


		public static void triggerSpecialOOCEvent()
		{
			if (!isWaitingForActionResponse && !Scheduler.Scheduler.hasTaskWith("ooc_rebound") && OutOfCoinsReboundDialog.instance == null)
			{
				isWaitingForActionResponse = true;
				OOCAction.triggerSpecialOOCEvent();	
			}
		}
		public static string notAvailableReason
		{
			get
			{
				System.Text.StringBuilder sb = new StringBuilder();
				if (!ExperimentWrapper.SpecialOutOfCoins.experimentData.isInExperiment)
				{
					sb.AppendLine("Not in experiment");
				}

				if (instance != null)
				{
					if (instance.spinCount >= ExperimentWrapper.SpecialOutOfCoins.experimentData.spinLimit)
					{
						sb.AppendLine("Over spin limit, user spins: " + instance.spinCount + ", maximum: " + ExperimentWrapper.SpecialOutOfCoins.experimentData.spinLimit);
					}

					if (instance.oocCount >= ExperimentWrapper.SpecialOutOfCoins.experimentData.oocLimit)
					{
						sb.AppendLine("Over collect limit, ooc count: " + instance.oocCount + ", maximum: " + ExperimentWrapper.SpecialOutOfCoins.experimentData.oocLimit);
					}

					int timeElapsed = GameTimer.currentTime - instance.initTime;

					if (timeElapsed >= timeLimit)
					{
						sb.AppendLine("Over time limit, time Elapsed: " + timeElapsed + ", maximum: " + timeLimit);
					}
				}
				else
				{
					sb.AppendLine("Feature class not instantiated");
				}

				return sb.ToString();
			}
		}

		public static void incrementSpinCount()
		{
			if (instance == null)
			{
				return;
			}

			instance.spinCount++;
		}

		public static void instantiateFeature(JSON data)
		{
			if (instance != null)
			{
				instance.clearEventDelegates();
			}

			instance = new OOCReboundFeature();
			instance.initFeature(data);
		}

		private void handleOOCDialogEvent(JSON data)
		{
			//increment trigger count
			++oocCount;

			//tell user we are not waiting on any pending events
			isWaitingForActionResponse = false;

			if (data == null)
			{
				Debug.LogError("Invalid ooc data");
				return;
			}

			JSON[] rewards = data.getJsonArray("rewards");
			if (rewards != null && rewards.Length == 1)
			{
				long credits = rewards[0].getLong("value", 0);
				//show special ooc dialog with a reward grant handler
				OutOfCoinsReboundDialog.showDialog(data, (args) =>
				{
					string eventId = data.getString("event", "");
					if (!string.IsNullOrEmpty(eventId))
					{
						OOCAction.acceptRewardGrant(eventId);
						SlotsPlayer.addNonpendingFeatureCredits(credits, "OOC Rebound");
					}
				});
			}
			else
			{
				Debug.LogError("Invalid reward data");
			}

		}

		private void handleOOCInboxEvent(JSON data)
		{
			//increment trigger count
			++oocCount;
			
			//tell user we are not waiting on any pending events
			isWaitingForActionResponse = false;

			//show special ooc dialog with an inbox handler
			OutOfCoinsReboundDialog.showDialog(data, (args)=>
			{
				//open the inbox afer an inventory update
				InboxAction.getInboxItems((e) =>
				{
					InboxInventory.onInboxUpdate(e);
					InboxDialog.showDialog(InboxDialog.MESSAGES_STATE);	
				});
				
			});
		}
		
		public static void resetStaticClassData()
		{
			instance = null;
			isWaitingForActionResponse = false;
		}
	}
}


