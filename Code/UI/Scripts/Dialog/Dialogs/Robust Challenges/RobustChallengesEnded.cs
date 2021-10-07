using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class RobustChallengesEnded : DialogBase
{
	private const string BACKGROUND_IMAGE_PATH = "robust_challenges/Robust_Challenges_BG_Purple.png";

	public Renderer backgroundRenderer;
	
	// Set the ended event active then let MOTD surface the ended dialog.
	public static void processEndedData(JSON response)
	{
		if (response != null)
		{
			// Let the server know that the player has seen this dialog.
			// Wait until the dialog is actually shown before doing this.
			RobustChallengesAction.sendLostSeenResponse(response.getString("event", ""));
		}
		// no longer showing this dialog in accordance with HIR-73158
		//showDialog(response);
	}

	public override void init()
	{
		StatsManager.Instance.LogCount("dialog", "robust_challenges_ended",ExperimentWrapper.RobustChallengesEos.variantName, "", "ok", "view");
		downloadedTextureToRenderer(backgroundRenderer, 0);
		
		JSON data = dialogArgs.getWithDefault(D.DATA, null) as JSON;
		if (data != null)
		{
			// Let the server know that the player has seen this dialog.
			// Wait until the dialog is actually shown before doing this.
			RobustChallengesAction.sendLostSeenResponse(data.getString("event", ""));
		}
	}

	public void Update()
	{
		AndroidUtil.checkBackButton(clickClose);
	}

	public virtual void clickClose()
	{
		Audio.play("minimenuclose0");
		StatsManager.Instance.LogCount("dialog", "robust_challenges_ended", ExperimentWrapper.RobustChallengesEos.variantName, "", "ok", "close");
		Dialog.close();
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	public static void showDialog(JSON data)
	{
		Dialog.instance.showDialogAfterDownloadingTextures("robust_challenges_ended", BACKGROUND_IMAGE_PATH, Dict.create(D.DATA, data), shouldAbortOnFail:true); 
	}
}
