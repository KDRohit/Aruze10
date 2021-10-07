using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;
using TMProExtensions;

public class VIPPhoneCollectRewardDialog : DialogBase
{
	public TextMeshPro rewardAmountMessage;
	public Renderer backgroundRenderer;
	public GameObject collectButton;
	public GameObject closeButton;
	public Transform flyingCoinStart;
	public Transform flyingCoinDestination;

	private const string BACKGROUND_PATH = "vip_phone_collect/Vip_Phone_Collect_BG.png";
	private const string AUDIO_COIN_PICK = "SparklyCoinPick";

	private int rewardCoin = 0;
	private string eventID = "";
	private bool isGetReward = false;

	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
		JSON data = dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as JSON;
		rewardCoin = data.getInt("coin_amt", 0);
		eventID = data.getString("event", "");

		StatsManager.Instance.LogCount("dialog", "vip_phone_number", "collect", "", "", "view"); 
		rewardAmountMessage.text = CreditsEconomy.convertCredits(rewardCoin);
	}

	void Update()
	{
		AndroidUtil.checkBackButton(getReward);
	}

	// Collect Reward
	private void getReward()
	{
		if (isGetReward)
		{ 
			return;
		}
		isGetReward = true;
		Audio.play(AUDIO_COIN_PICK);
		StartCoroutine(coinEffect());
	}

	/// Creates a flying coin that goes from the coin tab to the coin meter on the overlay.
	protected IEnumerator coinEffect()
	{
		// Make the button disappear as soon as it's touched, to prevent multiple touches while the coin is flying.
		collectButton.SetActive(false);
		closeButton.SetActive(false);
		// Create the coin as a child of "sizer", at the position of "coinIconSmall",
		// with a local offset of (0, 0, -100) so it's in front of everything else with room to spin in 3D.
		CoinScript coin = CoinScript.create(
			sizer,
			flyingCoinStart.position,
			new Vector3(0, 0, -100)
		);

		// Calculate the local coordinates of the destination, which is where "coinIconLarge" is positioned relative to "sizer".
		Vector2 destination = NGUIExt.localPositionOfPosition(sizer, flyingCoinDestination.position);

		yield return StartCoroutine(coin.flyTo(destination));

		coin.destroy();

		// Close the dialog as soon as the coin reaches the destination,
		// so there is a clear view of the rollup without the dialog in the way.
		Dialog.close();
		
		// Roll up after the coin reaches the destination.
		SlotsPlayer.addCredits(rewardCoin, "VIP Phone Number Reward");
		if (eventID != "")	// Check this because editor testing can pass in empty eventID.
		{
			VIPPhoneCollectAction.vipAcceptReward(eventID);
		}
		StatsManager.Instance.LogCount("dialog", "vip_phone_number", "collect", "", "collect", "click"); 
		StatsManager.Instance.LogCount("coins", "vip_phone_number", "", "", "", CreditsEconomy.convertCredits(rewardCoin));
		
	}

	private void OnPrivacyPolicyClicked()
	{
		Application.OpenURL(Glb.HELP_LINK_PRIVACY);
	}

	public override void close()
	{
		// Do special cleanup.
	}

	public static void showDialog(JSON data)
	{
		Dialog.instance.showDialogAfterDownloadingTextures("vip_phone_collect_reward_dialog", BACKGROUND_PATH, Dict.create(D.CUSTOM_INPUT, data), false, SchedulerPriority.PriorityType.IMMEDIATE);
	}

}
