using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class GenericRewardGrantDialog : DialogBase
{
	public const string BACKGROUND_IMAGE_PATH = "generic_reward_grant/Generic_Reward_BG_Coin.png";
	public Renderer backgroundRenderer;
	public TextMeshPro bodyMessageLabel;
	public TextMeshPro buttonLabel;
	public UIButton collectButton;
	public TextMeshPro amountLabel;
	public Transform coinStartPos;
	public Transform coinEndPos;

	private string rewardType = string.Empty;
	private long amount = 0;
	private string kingdom = string.Empty;
	private string phylum = string.Empty;
	private string eventID = string.Empty;

	private const float ROLLUP_TIME = 2.0f;

	public static void registerEventDelegates()
	{
		Server.registerEventDelegate("reward_grant", processData, true);
	}

	private static void processData(JSON response)
	{
		showDialog(response);
	}

	public override void init()
	{
		JSON data = (JSON)dialogArgs[D.DATA];
		downloadedTextureToRenderer(backgroundRenderer, 0); 
		bodyMessageLabel.text = Localize.text(data.getString("dialog.body", ""));
		buttonLabel.text = Localize.textUpper(data.getString("dialog.button", ""));;

		JSON[] rewards = data.getJsonArray("reward");
		if (rewards != null)
		{
			for (int i = 0; i < rewards.Length; ++i)
			{
				rewardType = rewards[i].getString("type", "");
				amount = rewards[i].getLong("amount", 0);
				if (rewardType == "credits")
				{
					break;
				}
			}
		}

		kingdom = data.getString("stats.kingdom", "");
		phylum = data.getString("stats.phylum", "");
		eventID = data.getString("event", "");

		amountLabel.text = CreditsEconomy.convertCredits(amount);
		StatsManager.Instance.LogCount("dialog", kingdom, phylum, "", "", "view");
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();

		// Rollup animation.
		StartCoroutine(SlotUtils.rollup(0, amount, amountLabel, true, ROLLUP_TIME));
	}

	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(clickClose);
	}

	public virtual void clickClose()
	{
		RewardAction.claimGenericReward(eventID);
		StatsManager.Instance.LogCount("dialog", kingdom, phylum, "", "collect", "click");
		if (rewardType == "credits")
		{
			StartCoroutine(earnCredits(amount));
		}
		else
		{
			earnRewards(rewardType, amount.ToString());
			Dialog.close();
		}
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
	}

	private void earnRewards (string key, string value)
	{
		string reasonPrefix = "Generic Reward Grant: ";
		switch (key)
		{
			case "game_unlock":
				// Don't need to do anything. Unlock system handles the unlock action.
				break;
			case "xp":
				SlotsPlayer.instance.xp.add(long.Parse(value), reasonPrefix + key, playCreditsRollupSound:true, reportToGameCenterManager:false);
				break;
			case "vip_point":
				SlotsPlayer.instance.addVIPPoints(int.Parse(value));
				break;
			case "level":
				// Don't need to do anything. Level up system handles the level up action.
				break;
			default:
				Debug.LogError("Generic Reward Grant: " + key + " reward type not recognized.");
				break;
		}
	}

	/// Creates a flying coin that goes from the coin tab to the coin meter on the overlay.
	protected IEnumerator earnCredits(long amount)
	{
		collectButton.isEnabled = false;
		// Create the coin as a child of "sizer", at the position of "coinIconSmall",
		// with a local offset of (0, 0, -100) so it's in front of everything else with room to spin in 3D.
		CoinScript coin = CoinScript.create(
			sizer,
			coinStartPos.position,
			new Vector3(0, 0, -100)
		);

		// Calculate the local coordinates of the destination, which is where "coinIconLarge" is positioned relative to "sizer".
		Vector2 destination = NGUIExt.localPositionOfPosition(sizer, coinEndPos.position);
		yield return StartCoroutine(coin.flyTo(destination));
		coin.destroy();

		// Roll up after the coin reaches the destination.
		SlotsPlayer.addCredits(amount, "Generic Reward Grant: credits");

		// Close the dialog as soon as the coin reaches the destination,
		// so there is a clear view of the rollup without the dialog in the way.
		Dialog.close();
	}

	public static void showDialog(JSON response)
	{
		// Load the default background image if this is no themed background.
		string imagePath = response.getString("dialog.background_image", "");
		if (string.IsNullOrEmpty(imagePath))
		{
			imagePath = BACKGROUND_IMAGE_PATH;
		}

		Dict args = Dict.create
		(
			D.DATA, response
		);

		Dialog.instance.showDialogAfterDownloadingTextures("generic_reward_grant", imagePath, args, true, SchedulerPriority.PriorityType.IMMEDIATE);
	}
}
