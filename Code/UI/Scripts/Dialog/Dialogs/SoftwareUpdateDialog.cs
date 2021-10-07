using UnityEngine;
using TMPro;

public class SoftwareUpdateDialog : DialogBase
{
	public static string instructionUrl;
	public static string minVersion;

	public Renderer background;
	public TextMeshPro bodyMessage;

	private const string BACKGROUND_TEXTURE_URL = "software_update/Kindle_Update_DialogBG.jpg";
	
	public override void init()
	{
		downloadedTextureToRenderer(background, 0);
		bodyMessage.text = Localize.text("software_update_instruction_{0}", "", SlotsPlayer.instance.socialMember.firstName);
		StatsManager.Instance.LogCount("dialog", "software", "update", "", "view", "view");
		MOTDFramework.markMotdSeen(dialogArgs);
	}

	// Callback for the "See Instructions" button.
	public void clickOkay()
	{
		Application.OpenURL(instructionUrl);
		Dialog.close();
	}

	// Callback for the close button.
	public void clickClose()
	{
		Dialog.close();
		StatsManager.Instance.LogCount("dialog", "software", "update", "", "click", "close");
	}

	public override void close()
	{
		// Do Cleanup here.
	}
	
	// Static method to show the dialog.
	public static bool showDialog(string motdKey = "")
	{
		int viewCount = PlayerPrefsCache.GetInt(Prefs.SOFTWARE_UPDATE_VIEW_COUNT, 0);
		viewCount++;
		PlayerPrefsCache.SetInt(Prefs.SOFTWARE_UPDATE_VIEW_COUNT, viewCount);
		Dict args = Dict.create(D.MOTD_KEY, motdKey);
		Dialog.instance.showDialogAfterDownloadingTextures("software_update", BACKGROUND_TEXTURE_URL, args, true);
		return true;
	}
}
