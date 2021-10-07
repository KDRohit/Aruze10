using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class AchievementsUpdateMOTD : DialogBase 
{
	[SerializeField] private ImageButtonHandler closeButton;
	[SerializeField] private ImageButtonHandler collectButton;
	[SerializeField] private TextMeshPro rewardLabel;
	[SerializeField] private Transform coinStartPos;
	[SerializeField] private Transform coinEndPos;
	public override void init()
	{
		
		closeButton.registerEventDelegate(closeClicked);
		collectButton.registerEventDelegate(collectClicked);
		rewardLabel.text = string.Format("{0:n0}", CreditsEconomy.convertCredits(NetworkAchievements.getBackfillAmount()));
		
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_trophy_motd_2",
			phylum: "",
			klass: "view",
			family: "",
			genus: "");

		//disable the close button
		closeButton.gameObject.SetActive(false);
		
		MOTDFramework.markMotdSeen(dialogArgs);
		Audio.play(AchievementsMOTD.OPEN_MOTD_AUDIO); //match the audio in teh original achievements motd
	}
	public override void close()
	{
		// This must be here because the base function is marked as abstract
	}

	private void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_trophy_motd_2",
			phylum: "",
			klass: "close",
			family: "",
			genus: "");		
		Dialog.close();
	}

	private void collectClicked(Dict args = null)
	{
		collectButton.enabled = false;
		NetworkAchievementAction.collectAchievementBackfill();

		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_trophy_motd_2",
			phylum: "",
			klass: "click",
			family: "",
			genus: "");		

		StartCoroutine(coinFly());

	}

	public static void showDialog(string motdKey)
	{
		Dialog.instance.showDialogAfterDownloadingTextures("achievements_update_motd",
			SlotsPlayer.instance.socialMember.getImageURL,
			Dict.create(D.MOTD_KEY, motdKey),
			priorityType: SchedulerPriority.PriorityType.IMMEDIATE,
			isPersistent: true);
	}

	/// Creates a flying coin that goes from the coin tab to the coin meter on the overlay.
	protected IEnumerator coinFly()
	{	
		// Create the coin as a child of "sizer", at the position of "coinIconSmall",
		// with a local offset of (0, 0, -100) so it's in front of everything else with room to spin in 3D.
		CoinScriptUpdated  coin = CoinScriptUpdated.create(
			sizer,
			coinStartPos.position,
			new Vector3(0, 0, -100)
		);

		Audio.play("initialbet0");
		// Calculate the local coordinates of the destination, which is where "coinIconLarge" is positioned relative to "sizer".
		Vector2 destination = NGUIExt.localPositionOfPosition(sizer, coinEndPos.position);
		yield return StartCoroutine(coin.flyTo(destination));
		coin.destroy();

		Dialog.close();
	}	
}
