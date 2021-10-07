using System.Text;
using UnityEngine;
using System.Collections.Generic;
using Com.Rewardables;

public class DevGUIMenuRichPass : DevGUIMenu
{
    private const string TEST_DATA_DIRECTORY = "Test Data/RichPass/";
    private const string LOGIN_FILE = "login";
    private bool fakeOn = false;
    private int pointsToIncrement = 0;

    private static bool isRegisteredForPassTypeChange = false;
    
    public override void drawGuts()
    {
        if (!fakeOn && (CampaignDirector.richPass == null || !CampaignDirector.richPass.isActive))
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Fake Data"))
            {
                fakeOn = true;
                
                //send login data
                JSON data = getFakeLoginData();
                CampaignDirector.initCampaign(data, CampaignDirector.RICH_PASS);
            }
            GUILayout.EndHorizontal();
        }

        //don't draw the rest of the menu if we're not active
        if (!fakeOn && (CampaignDirector.richPass == null || !CampaignDirector.richPass.isActive))
        {
            return;
        }

        GUILayout.BeginHorizontal();

        if (!fakeOn && !isRegisteredForPassTypeChange)
        {
            isRegisteredForPassTypeChange = true;
            CampaignDirector.richPass.onPassTypeChanged += onPassTypeChanged;
        }
        
        if (GUILayout.Button("Open Main Dialog"))
        {
            RichPassFeatureDialog.showDialog(CampaignDirector.richPass);
        }
        
        if (GUILayout.Button("Open New Seasonal Challenges Dialog"))
        {
            RichPassNewSeasonalChallengesDialog.showDialog(-1);
        }
        
        if (GUILayout.Button("Open Pass Summary Dialog"))
        {
            RichPassSummaryDialog.showDialog();
        }
        
        if (GUILayout.Button("Open Piggy Bank Reward Dialog"))
        {
            generateFakeBankReward();
        }

        GUILayout.EndHorizontal();
        
                
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Reset Last Seen Seasonal Challenges Unlocked Date"))
        {
            CustomPlayerData.setValue(CustomPlayerData.LAST_SEEN_RICH_PASS_CHALLENGES_TIME, 0);
        }
        
        if (GUILayout.Button("Reset Last Seen New Pass Started Date"))
        {
            CustomPlayerData.setValue(CustomPlayerData.LAST_SEEN_RICH_PASS_START_TIME, 0);
        }

        if (GUILayout.Button("Expire Timer (until reload)"))
        {
            if (CampaignDirector.richPass != null)
            {
                CampaignDirector.richPass.timerRange = new GameTimerRange(GameTimer.currentTime - 100, GameTimer.currentTime - 10);
                CampaignDirector.richPass.onEventEnd();
            }
            
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Total Points: ", new GUILayoutOption[]{ GUILayout.Width(200) });
        GUILayout.Label(CampaignDirector.richPass.pointsAcquired.ToString());
        if (GUILayout.Button("Reset Player"))
        {
            if (!fakeOn)
            {
                RichPassAction.resetPlayer();
            }

            CampaignDirector.richPass.fullReset();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Pass Type: ", new GUILayoutOption[]{ GUILayout.Width(200) });
        GUILayout.Label(CampaignDirector.richPass.passType);
        switch (CampaignDirector.richPass.passType)
        {
            case "silver":
                if (GUILayout.Button("Upgrade To Gold"))
                {
                    if (!fakeOn)
                    {
                        RichPassAction.setPassToGold();
                    }
                    else
                    {
                        CampaignDirector.richPass.upgradePass("gold");
                    }
                }
                break;
            
            case "gold":
                if (GUILayout.Button("Revert to Silver"))
                {
                    if (!fakeOn)
                    {
                        RichPassAction.setPassToSilver();
                    }
                    else
                    {
                        CampaignDirector.richPass.expireGoldPass();
                    }
                }
                break;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        string tempText = GUILayout.TextField(pointsToIncrement.ToString(), 
            new GUILayoutOption[]{
            GUILayout.Height(50),
            GUILayout.Width(200)
        });
        if (!System.Int32.TryParse(tempText, out pointsToIncrement))
        {
            pointsToIncrement = 1;
        }
        if (GUILayout.Button("Increment Point Total"))
        {
            if (!fakeOn)
            {
                RichPassAction.addPoints(pointsToIncrement);
            }
            CampaignDirector.richPass.incrementPoints(pointsToIncrement);
        }
        GUILayout.EndHorizontal();
        drawRewards();
    }

    private void onPassTypeChanged(string type)
    {
        if (type != "silver" && Dialog.instance.currentDialog != null && Dialog.instance.currentDialog is RichPassFeatureDialog)
        {
            //Play the upgrade animations if the dialog is currently open
            (Dialog.instance.currentDialog as RichPassFeatureDialog).upgradeDialog();
            DevGUI.isActive = false;
        }
    }
    
    private JSON getFakeLoginData()
    {
        string testDataPath = TEST_DATA_DIRECTORY + LOGIN_FILE;
        TextAsset textAsset = (TextAsset)Resources.Load(testDataPath,typeof(TextAsset));
        string text = textAsset.text;
        //update start/end time to be current
        text = text.Replace("\"start_time\": 1574714218", "\"start_time\" : " +  GameTimer.currentTime);
        text = text.Replace("\"end_time\": 1574715218", "\"end_time\" : " +   (GameTimer.currentTime + (60 * 60)));
        return new JSON(text);
    }

    public static string buildRewardJSON(string passType, PassReward reward, long cumulativePoints)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("\t\t\"pass_type\" : \""+ passType + "\",");
        sb.AppendLine("\t\t\"cumulative_points\" : "+ cumulativePoints + ",");
        sb.AppendLine("\t\t\"reward_id\" : "+ 1 + ",");
        sb.AppendLine("\t\"type\" : \"rp_reward_granted\",");
        sb.AppendLine("\t\"grant_data\" : {");
        sb.AppendLine("\t\t\"reward_type\" : \"rewards_bundle\",");
        sb.AppendLine("\t\t\"feature_name\" : \"rich_pass\",");
        sb.AppendLine("\t\t\"rewardables\" : [");

        long oldCredits = SlotsPlayer.creditAmount;
        long newCredits = oldCredits;
      
        sb.AppendLine("\t\t\t{");
        sb.AppendLine("\t\t\t\"reward_type\" : \"" + reward.type + "\",");
        sb.AppendLine("\t\t\t\"feature_name\" : \"rich_pass\",");
        switch (reward.type)
        {
            case ChallengeReward.RewardType.CREDITS:
                sb.AppendLine("\t\t\t\"value\" : " + reward.amount + ",");
                newCredits += reward.amount;
                break;
        }

        sb.AppendLine("\t\t\t\"old_credits\" : " + oldCredits + ",");
        sb.AppendLine("\t\t\t\"new_credits\" : " + newCredits);
        sb.AppendLine("}");

        sb.AppendLine("\t]");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static void generateFakeBankReward()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("\t\"type\" : \"rp_bank_reward\",");
        
        sb.AppendLine("\t\t\"rewards\" : [{");
        sb.AppendLine("\t\"type\" : \"coin\",");
        sb.AppendLine("\t\"value\" : \"126000\"");
        sb.AppendLine("\t}]");
        sb.AppendLine("}");
        RichPassCampaign.onBankReward(new JSON(sb.ToString()));
    }

    private void drawRewards()
    {
        RichPassCampaign.RewardTrack silver = CampaignDirector.richPass.silverTrack;
        RichPassCampaign.RewardTrack gold = CampaignDirector.richPass.goldTrack;
        List<long> rewardKeys = CampaignDirector.richPass.allRewardKeys;
        for (int i = 0; i < rewardKeys.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Required Points: " + rewardKeys[i], 
                new GUILayoutOption[]{
                    GUILayout.Width(150)
                });
            GUILayout.BeginVertical();
            drawRewardTrack(silver, rewardKeys[i]);
            drawRewardTrack(gold, rewardKeys[i]);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }

    private void drawRewardTrack(RichPassCampaign.RewardTrack track, long pointValue)
    {
         List<PassReward> rewards = track.getSingleRewardsList(pointValue);
         if (rewards != null && rewards.Count > 0)
         {
            GUILayout.BeginHorizontal();
            GUILayout.Label(track.name + " - ", 
                new GUILayoutOption[]{
                GUILayout.Width(150)
            });
            GUILayout.BeginVertical();
            for (int j = 0; j < rewards.Count; j++)
            {
                if (rewards[j] == null)
                {
                    continue;
                }
                
                GUILayout.BeginHorizontal();
                ChallengeReward.RewardType type = rewards[j].type;
                GUILayout.Label(nameof(type), new GUILayoutOption[]{
                    GUILayout.Width(100)});

                if (track.name != CampaignDirector.richPass.passType && track.name != "silver")
                {
                    GUILayout.Label("Upgrade pass to unlock");
                }
                else if (rewards[j].unlocked)
                {
                    if (!rewards[j].claimed)
                    {
                        if (GUILayout.Button("Claim Reward"))
                        {
                            if (!fakeOn)
                            {
                                track.claimReward(rewards[j].id, pointValue);
                            }
                            else
                            {
                                string rewardJSON = buildRewardJSON("silver", rewards[j], pointValue);
                                RewardablesManager.onRPRewardGranted(new JSON(rewardJSON.ToString()));
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("Already Claimed");
                    }
                }
                else
                {
                    GUILayout.Label("Add points to unlock");
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal(); 
         }
    }
    
    public new static void resetStaticClassData()
    {
        isRegisteredForPassTypeChange = false;
    }
}
