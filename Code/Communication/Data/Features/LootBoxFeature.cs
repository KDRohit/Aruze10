using UnityEngine;
using System.Collections.Generic;
using Com.Rewardables;
using Com.Scheduler;

public class LootBoxFeature : FeatureBase
{
	public const string SOURCE_CHALLENGE = "challenges";
	public const string SOURC_RICH_PASS = "rich_pass";
	public const string SOURCE_SLOTVENTURES = "slotventures";
	
	public const string LOOT_BOX = "loot_box";
	
	private const string GRANT_DATA = "grant_data";
	
	private Dictionary<string, LootBoxRewardBundle> allLootBoxBundles = new Dictionary<string, LootBoxRewardBundle>();
	private Dictionary<string, JSON> allCardPackData = new Dictionary<string, JSON>();

	private bool needToRefreshRichPassFeatureDialog = false;
	
	public static LootBoxFeature instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<LootBoxFeature>(LOOT_BOX);
		}
	}
	
	public static void checkInstance()
	{
		if (instance == null)
		{
			Debug.LogError("LootBoxRewardBundle instance failed to create");
		}
	}

	public LootBoxRewardBundle findLootBoxformSource(string source)
	{
		if (string.IsNullOrEmpty(source))
		{
			return null;
		}
		
		LootBoxRewardBundle lootBoxRewardBundle = null;
		allLootBoxBundles.TryGetValue(source, out lootBoxRewardBundle);
		
		return lootBoxRewardBundle;
	}
	
	public void showLootBoxRewardDialog(string source)
	{
		LootBoxRewardBundle lootBoxBundle = findLootBoxformSource(source);
		
		if (lootBoxBundle == null || string.IsNullOrEmpty(lootBoxBundle.Source))
		{
			return;
		}

		needToRefreshRichPassFeatureDialog = (RichPassFeatureDialog.instance != null);
		if (needToRefreshRichPassFeatureDialog)
		{
			// we need to then close rich pass feature dialog here, and reopen it after loot box dialog closes.
			// why: because loot box might update rich pass points and we need to make sure the rich pass dialog get
			//      updated as well.
			Dialog.close(RichPassFeatureDialog.instance);
		}

		if (source.Equals(lootBoxBundle.Source, System.StringComparison.InvariantCultureIgnoreCase))
		{
			applyRewardsWhenOpeningLootBoxDialog(lootBoxBundle);
			LootBoxDialog.showDialog(Dict.create(D.OPTION, lootBoxBundle, D.CALLBACK,
				new DialogBase.AnswerDelegate((x)=> { onLootBoxDialogClosed(x, lootBoxBundle); })), SchedulerPriority.PriorityType.BLOCKING);

			// If we have other feature rewards included in loot box, we will process them and queue the back to back reward dialogs here
			queueBackToBackFeatureRewardDialog(lootBoxBundle);
			
			// It has been displayed, so remove it from dictionary.  so there is a second call with the same source, 
			// we do not display the same loot box twice.
			// NOTE: showLootBoxRewardDialog might get called twice when claiming loot box in SoltVentures lobby due to
			//       a race condition:
			//       1. onRewardBundleReceived invokes showLoobBoxRewardDialog when claiming loot box
			//       2. Then SlotventuresChallengeCampaign.showMissionComplete() might call showLoobBoxRewardDialog again
			allLootBoxBundles.Remove(source);
		}
	}

	public void processRewardsImmediatelyAfterReceivingIt(LootBoxRewardBundle lootBoxBundle)
	{
		if (lootBoxBundle == null || lootBoxBundle.LootBoxRewardItems == null)
		{
			return;
		}
	    
		foreach (LootBoxRewardItem rewardItem in lootBoxBundle.LootBoxRewardItems)
		{
			// Add into pending credits queue in so that we won't run into credit desync problem 
			// Others will wait until we open loot box dialog.
			if (rewardItem.rewardItemType == LootBoxRewardItem.LootRewardItemType.coin)
			{
				Server.handlePendingCreditsCreated(lootBoxBundle.Source, rewardItem.addedValue);
			}
		}
	}
	
    public void applyRewardsWhenOpeningLootBoxDialog(LootBoxRewardBundle lootBoxBundle)
    {
        if (lootBoxBundle == null || lootBoxBundle.LootBoxRewardItems == null)
        {
            return;
        }
	    
        foreach (LootBoxRewardItem rewardItem in lootBoxBundle.LootBoxRewardItems)
        {
            switch (rewardItem.rewardItemType)
            {
                case LootBoxRewardItem.LootRewardItemType.coin:
                    // It has been added into pending credits queue in processRewardsImmediatelyAfterReceivingIt,
	                // so that we won't run into credit desync problem 
                    break;
                
                case LootBoxRewardItem.LootRewardItemType.powerup:
                    // Apply powerup there, since the lootbox dialog will looking for the active powerup to display
                    string rewardJsonString = "{" + JSON.createJsonString(GRANT_DATA, rewardItem.jsonData) + "}";
                    RewardablesManager.onRewardGranted(new JSON(rewardJsonString));
                    break;
                case LootBoxRewardItem.LootRewardItemType.cardPack:
                    // Card Pack Drop will be handled outside this - a card pack drop dialog will be displayed after lootbox dialog
	                break;
                case LootBoxRewardItem.LootRewardItemType.elite:
                    // Elite reward will be deferred after the lootbox dialog, so that it won't not interrupt loot box flow here
	                break;
                case LootBoxRewardItem.LootRewardItemType.richPass:
                    // Rich pass points will be deferred after the lootbox dialog, so that it won't not interrupt loot box flow here
					break;
                case LootBoxRewardItem.LootRewardItemType.pets:
	                break;
                default:
                    break;
            }
        }
    }

	// The loot box will display loot box reward dialog A, after that, feature rewards in loot box - e.g. dialogs B, C)
	// so we need to guarantee them to be displayed back to back without any interruption.
	// Make them blocking dialogs and add them in scheduler task queue back to back is the easiest solution here. 
	public void queueBackToBackFeatureRewardDialog(LootBoxRewardBundle lootBoxBundle)
	{
		if (lootBoxBundle == null || lootBoxBundle.LootBoxRewardItems == null)
		{
			return;
		}

		foreach (LootBoxRewardItem rewardItem in lootBoxBundle.LootBoxRewardItems)
		{
			string rewardJsonString;
			switch (rewardItem.rewardItemType)
			{
				case LootBoxRewardItem.LootRewardItemType.coin:
					// It has been added into pending credits queue in processRewardsImmediatelyAfterReceivingIt,
					// so that we won't run into credit desync problem 
					break;

				case LootBoxRewardItem.LootRewardItemType.powerup:
					// Do nothing, powerup has been applied in applyRewardsWhenOpeningLootBoxDialog
					break;

				case LootBoxRewardItem.LootRewardItemType.cardPack:
					// Handle card pack dropped
					Dictionary<string, JSON> cardPacks = lootBoxBundle.getLookBoxCardPackDrops();
					if (cardPacks == null)
					{
						return;
					}

					// Handle card packs in this bundle
					foreach (KeyValuePair<string, JSON> pack in cardPacks)
					{
						Collectables.claimPackDropNow(pack.Value, SchedulerPriority.PriorityType.BLOCKING);
					}
					break;

				case LootBoxRewardItem.LootRewardItemType.elite:
				// Elite reward will be deferred after the lootbox dialog, so that it won't not interrupt loot box flow here
					break;
				case LootBoxRewardItem.LootRewardItemType.richPass:
				// Rich pass points will be deferred after the lootbox dialog, so that it won't not interrupt loot box flow here
					break;
				case LootBoxRewardItem.LootRewardItemType.pets:
					break;
				// V2
				default:
					break;
			}
		}
	}

	public void finalizeRewardsAfterLootBoxDialog(LootBoxRewardBundle lootBoxBundle)
    {
	    if (lootBoxBundle == null || lootBoxBundle.LootBoxRewardItems == null)
	    {
		    return;
	    }
	    
        foreach (LootBoxRewardItem rewardItem in lootBoxBundle.LootBoxRewardItems)
        {
            string rewardJsonString;
            switch (rewardItem.rewardItemType)
            {
                case LootBoxRewardItem.LootRewardItemType.coin:
                    SlotsPlayer.addFeatureCredits(rewardItem.addedValue, lootBoxBundle.Source);
                    break;
                
                case LootBoxRewardItem.LootRewardItemType.powerup:
                    // Do nothing, powerup has been applied in applyRewardsWhenOpeningLootBoxDialog
                    break;
                
                case LootBoxRewardItem.LootRewardItemType.cardPack:
                    // queueBackToBackFeatureRewardDialog did it
                    break;
                
                case LootBoxRewardItem.LootRewardItemType.elite:
                    rewardJsonString = "{" + JSON.createJsonString(GRANT_DATA, rewardItem.jsonData) + "}";
                    RewardablesManager.onRewardGranted(new JSON(rewardJsonString));
                    break;
                
                case LootBoxRewardItem.LootRewardItemType.richPass:
                    if (CampaignDirector.richPass != null)
                    {
                        CampaignDirector.richPass.incrementPoints(rewardItem.addedValue);	
                    }

                    break;
                case LootBoxRewardItem.LootRewardItemType.pets:
	                // V2
	                break;
                default:
                    break;
            }
        }
    }
	
#region Reward Event Handlers
	
	// This function is to process lootbox for rich pass reward
	private void onRichPassRewardReceived(Rewardable rewardable)
	{
		RewardRichPass richPassReward = rewardable as RewardRichPass;

		if (richPassReward == null || richPassReward.data ==null)
		{
			return;
		}
		
		string featureName = richPassReward.data.getString("feature_name", "");
		if (featureName.Equals(LOOT_BOX, System.StringComparison.InvariantCultureIgnoreCase))
		{
			LootBoxRewardBundle lootboxBundle = new LootBoxRewardBundle();
			lootboxBundle.ParseRichPassRewardBundle(richPassReward.data);
			processRewardsImmediatelyAfterReceivingIt(lootboxBundle);

			allLootBoxBundles[lootboxBundle.Source] = lootboxBundle;
			
			// Show loot box immediately.  This happens when we claim loot box in rich pass reward dialog
			showLootBoxRewardDialog(lootboxBundle.Source);
			
			StatsManager.Instance.LogMileStone("hir_collections_lootbox_pack", lootboxBundle.Rarity);
		}
	}

	// This function is process lootbox for campaign reward
	private void onRewardBundleReceived(JSON data)
	{
		if (data == null)
		{
			return;
		}

		string featureName = data.getString("feature_name", "");
		if (featureName.Equals(LOOT_BOX, System.StringComparison.InvariantCultureIgnoreCase))
		{
			LootBoxRewardBundle lootboxBundle = new LootBoxRewardBundle();
			lootboxBundle.ParseChallengeRewardBundle(data);
			processRewardsImmediatelyAfterReceivingIt(lootboxBundle);

			allLootBoxBundles[lootboxBundle.Source] = lootboxBundle;

			switch (lootboxBundle.Source)
			{
				case SOURCE_SLOTVENTURES:
					// Slot venture will display the loot box dialog at
					// SlotventuresMissionCompleteDialog.Close() for Mission complete
					// SlotventuresJackpotButton.onClaimLastReward
					break;
				case SOURC_RICH_PASS:
				// Rich pass was handled in a different route throug onRewardReceived
				case SOURCE_CHALLENGE:
					// Robust Challenges will display the loot box dialog at RobustChallengesObjectivesDialog.Close()
					break;
			}

			StatsManager.Instance.LogMileStone("hir_collections_lootbox_pack", lootboxBundle.Rarity);
		}
	}

	private void onPackDropRecieved(JSON data)
	{	
		// We just intercept the card Pack Drop for loot box here, and will then drop it.
		// Why: All the loot box card pack Drop reward has been embedded into loot box bundle, this is a duplicate one.
		//      It is difficult to remove this duplicate one on server side without significant code change.
		//      so we just let it come and simply drop it.
	}
	
#endregion


#region Call back functions

	private void onLootBoxDialogClosed(Dict args, LootBoxRewardBundle lootboxBundle)
	{
		// Apply all rewards once the dialog is closed, including pending credits
		finalizeRewardsAfterLootBoxDialog(lootboxBundle);

		if (needToRefreshRichPassFeatureDialog)
		{
			RichPassFeatureDialog.showDialog(CampaignDirector.richPass);
			needToRefreshRichPassFeatureDialog = false;
		}
	}

#endregion

#region feature_base_overrides

	protected override void registerEventDelegates()
	{
		RewardablesManager.addEventHandler(onRichPassRewardReceived);
		Collectables.Instance.registerForPackDrop(onPackDropRecieved, LOOT_BOX);
		Server.registerEventDelegate("rewards_bundle", onRewardBundleReceived, true);
	}

	protected override void clearEventDelegates()
	{
		RewardablesManager.removeEventHandler(onRichPassRewardReceived);
		Collectables.Instance.unRegisterForPackDrop(onPackDropRecieved, LOOT_BOX);
		Server.unregisterEventDelegate("rewards_bundle");
	}
	
#endregion
}
