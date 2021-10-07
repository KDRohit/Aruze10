using UnityEngine;
using System.Collections;
using TMPro;

public class MaxVoltageDialog : DialogBase
{
	[SerializeField] private UITexture image;
	[SerializeField] private ButtonHandler playButton;

	protected const string IMAGE_NAME = "maxvoltagezone_info_00_windowed";
	
	public override void init()
	{
		if (playButton != null)
		{
			playButton.gameObject.SetActive(SlotsPlayer.instance.socialMember.experienceLevel >= Glb.MAX_VOLTAGE_MIN_LEVEL);
			playButton.registerEventDelegate(playClicked);
		}
		
		if (dialogArgs != null && dialogArgs.ContainsKey(D.MOTD_KEY))
		{
			string motdKey = (string)dialogArgs[D.MOTD_KEY];
			if (motdKey.Contains("max_voltage_unlock"))
			{
				Audio.play("MVLobbyOpen");
			}
		}

		StatsManager.Instance.LogCount(
			"dialog",
			"max_voltage",
			"motd",
			"",
			"",
			"view");

		downloadedTextureToUITexture(image, 0);
		
		MOTDFramework.markMotdSeen(dialogArgs);
	}
	
	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	private void playClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			"dialog",
			"max_voltage",
			"motd",
			"",
			"okay",
			"click");
		
		Dialog.close();

		if (LobbyLoader.lastLobby != LobbyInfo.Type.MAX_VOLTAGE)
		{
			LobbyLoader.returnToNewLobbyFromDialog(false, LobbyInfo.Type.MAX_VOLTAGE);
		}
	}

	private void closeClicked()
	{
		StatsManager.Instance.LogCount(
			"dialog",
			"max_voltage",
			"motd",
			"",
			"close",
			"click");
		
		Dialog.close();
	}
	
	public static bool showDialog(string motdKey = "")
	{
		Dialog.instance.showDialogAfterDownloadingTextures(
			"max_voltage_motd",
			string.Format("motd/{0}.png", IMAGE_NAME),
			Dict.create(D.MOTD_KEY, motdKey)
		);
		return true;
	}

	// Do NOT call -- called by Dialog.close()
	public override void close()
	{
		// Clean up here.
	}

}
