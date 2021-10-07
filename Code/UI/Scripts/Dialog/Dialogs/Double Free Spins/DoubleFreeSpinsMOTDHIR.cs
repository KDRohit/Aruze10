using UnityEngine;
using System.Collections;
using TMPro;

/*
	Dialog for showing the Double Free Spins MOTD
*/
public class DoubleFreeSpinsMOTDHIR : DoubleFreeSpinsMOTD
{
	public Renderer backgroundRenderer;

	public override void init()
	{
		base.init();

		downloadedTextureToRenderer(backgroundRenderer, 0);

		Audio.play("minimenuopen0");
		MOTDFramework.markMotdSeen(dialogArgs);

		// Stats track for server
		StatsManager.Instance.LogCount("dialog", DIALOG_KEY, "", "", "", "view");
	}
		
	public override void closeClicked(Dict args = null)
	{
		// Stats track for server
		StatsManager.Instance.LogCount("dialog", DIALOG_KEY, "", "", "close", "click");
		base.closeClicked(args);
	}
	
	public override void visitVIP(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", DIALOG_KEY, "", "", "visit_vip", "click");
		base.visitVIP(args);
	}
			
}