using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls display of progressive jackpot initial bet selection.
*/

public class MultiProgressiveMOTD : DialogBase
{
	public LobbyOptionButtonMultiProgressive multiprogressiveCabinet;
	public Renderer backgroundRenderer;
	//public TextMeshPro jackpotLabel;

	protected static LobbyGame gameInfo = null;

	private static string gameKey = "";
	
	// Initialization
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 1);
		StartCoroutine(LobbyOption.setupStandaloneCabinet(multiprogressiveCabinet, gameInfo));
		MOTDFramework.markMotdSeen(dialogArgs);
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	/// Used by UIButtonMessage
	public void closeClicked()
	{
		Dialog.close();
	}

	public static bool showDialog(string motdKey = "")
	{
		gameKey = Data.liveData.getString("CURRENT_MULTI_GAME", "");
		gameInfo = LobbyGame.find(gameKey);

		// We check in "shouldShowDialog" as well, but lets just make sure nothing crazy happened between then and now.
		if (gameInfo != null)
		{ 
			// Multiprogressive mode uses a 1X1 image, which is achieved by passing in "".
			string imageSize = (gameInfo.isMultiProgressive ? "" : "1X2");
			string filename = SlotResourceMap.getLobbyImagePath(gameInfo.groupInfo.keyName, gameInfo.keyName, imageSize);

			string[] files =  { filename };

			Dialog.instance.showDialogAfterDownloadingTextures("motd_multiprogressive_jackpot", args: Dict.create(D.MOTD_KEY, motdKey), nonMappedBundledTextures:files);
			return true;

		}
		else
		{
			Debug.LogError("ProgressiveSelectBetDialog::showDialog() - gameInfo is null and couldn't be found!  Dialog will not be shown!");
			return false;
		}
	}
}

