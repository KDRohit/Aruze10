using UnityEngine;
using TMPro;

// TODO: this class/file should be renamed DailyChallengeGrantDialog, because it is no longer just Credits awarded
public class DailyChallengeCreditGrant : DialogBase
{
	public Renderer backgroundRenderer;
	public TextMeshPro awardTypeLabel;
	public TextMeshPro awardAmountLabel;
	public TextMeshPro bodyMessageLabel;

	private long winCredits;
	private long xpPoints;
	private int vipPoints;

	// When user pressed OK, changed it to "okay".
	private string action = "close";

	public override void init()
	{
		winCredits = 0;
		xpPoints = 0;
		vipPoints = 0;

		JSON awardJSON = dialogArgs.getWithDefault(D.DATA, null) as JSON;
		if (awardJSON != null)
		{
			/* awardJSON looks like this:
			 * 
			 *  'reward_type'        => ("xp"/"vip"/"credits")
	            'credits'            => $credits,
	            'event_type_id'	     => $eventType->getId(),
	            'xp_points'          => $xpPoints,
	            'vip_points'         => $vipPoints
	            'big_win'			 => $credits
			 */
			string rewardType = awardJSON.getString("reward_type","missing reward_type");
			switch(rewardType)
			{
				// For SIR
				case "credits":
					winCredits = awardJSON.getLong("credits", 0L);
					awardAmountLabel.text = CreditsEconomy.convertCredits(winCredits);
					SafeSet.labelText(awardTypeLabel, Localize.text("coins_awarded"));
					break;
				case "xp":
					xpPoints = awardJSON.getLong("xp_points", 0L);
					awardAmountLabel.text = xpPoints.ToString();
					SafeSet.labelText(awardTypeLabel, Localize.text("xp_awarded"));
					break;
				case "vip":
					vipPoints = awardJSON.getInt("vip_points", 0);
					awardAmountLabel.text = vipPoints.ToString();
					SafeSet.labelText(awardTypeLabel, Localize.text("vip_points_awarded"));
					break;
				case "big_win":
					winCredits = awardJSON.getLong("credits", 0L);
					awardAmountLabel.text = CreditsEconomy.convertCredits(winCredits);
					SafeSet.labelText(awardTypeLabel, Localize.text("coins_awarded"));
					break;
					
				// For HIR
				case "coin_reward": 
					winCredits = awardJSON.getLong("credits", 0L);
					awardAmountLabel.text = CreditsEconomy.convertCredits(winCredits);
					break;
				default:
					Debug.LogErrorFormat("DailyChallengeCreditGrant dialog: unknown reward type: '{0}'", rewardType);
					awardAmountLabel.text = string.Format("unknown reward type: '{0}'", rewardType);
					break;
			}
		}
			
		StatsManager.Instance.LogCount("dialog", "daily_challenge_complete", "daily_challenge", DailyChallenge.gameKey, "", "view");
		downloadedTextureToRenderer(backgroundRenderer, 0);

		Audio.play("ChallengeComplete");
	}

	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	public void okClicked()
	{
		action = "okay";
		closeClicked();
	}

	public void closeClicked()
	{
		StatsManager.Instance.LogCount("dialog", "daily_challenge_complete", "daily_challenge", DailyChallenge.gameKey, action, "click");
		Audio.play("minimenuclose0");
		if(winCredits!=0)
			SlotsPlayer.addCredits(winCredits, "daily challenge win", playCreditsRollupSound:true, reportToGameCenterManager:false);
		if(xpPoints!=0)
			SlotsPlayer.instance.xp.add(xpPoints, "daily challenge win", playCreditsRollupSound:true, reportToGameCenterManager:false);
		if(vipPoints!=0)
			SlotsPlayer.instance.addVIPPoints(vipPoints);
		Dialog.close();
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	private static string backgroundImagePath
	{
		get
		{
			return DailyChallengeCreditGrantHIR.BACKGROUND_PATH;
		}
	}

	public static void showDialog(JSON awardJson)
	{
		Dialog.instance.showDialogAfterDownloadingTextures("daily_challenge_credit_grant", backgroundImagePath, Dict.create(D.DATA, awardJson));
	}
}
