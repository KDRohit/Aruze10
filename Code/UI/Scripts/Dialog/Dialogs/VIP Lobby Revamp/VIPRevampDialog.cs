using UnityEngine;
using System.Collections;

public class VIPRevampDialog : DialogBase
{
	public MeshRenderer gameImage;

	public override void init()
	{
		downloadedTextureToRenderer(gameImage, 0);
		MOTDFramework.markMotdSeen(dialogArgs);
		StatsManager.Instance.LogCount("dialog", "vip_revamp_motd", "", "", "", "view");
	}
	
	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	private void playClicked()
	{
		Dialog.close();
		DoSomething.now("vip_lobby");
		StatsManager.Instance.LogCount("dialog", "vip_revamp_motd", "", "", "play_now", "click");
	}

	private void closeClicked()
	{
		Dialog.close();
	}
	
	public static bool showDialog(LobbyOption option, string motdKey = "")
	{
		if (option != null)
		{
			string imagePath = SlotResourceMap.getLobbyImagePath(option.game.groupInfo.keyName, option.game.keyName);
			Dialog.instance.showDialogAfterDownloadingTextures( "vip_new_lobby", nonMappedBundledTextures:new string[]{imagePath}, args:Dict.create(D.MOTD_KEY, motdKey) );
			return true;
		}
		return false;
	}

	// Do NOT call -- called by Dialog.close()
	public override void close()
	{
		// Clean up here.
	}
}
