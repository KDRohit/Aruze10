using UnityEngine;
using System.Collections.Generic;
using Com.Scheduler;

public class ZisReloadSupportDialog : DialogBase
{
	public static List<string> downloadingLazyBundles = new List<string>();
	public static bool hasLoadedBundles = false;
	
	/// Initialization
	public override void init()
	{
	}
	
	private void OnReloadClicked(GameObject ignored)
	{
		Glb.resetGame("FB login popup closed without doing anything");
	}
	
	private void OnSupportClicked(GameObject ignored)
	{
		Common.openSupportUrl(Glb.HELP_LINK_SUPPORT);
	}
	
	/// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		// Do special cleanup.
	}
	
	public static void showDialog()
	{
		Scheduler.addDialog("zis_reload_or_support_dialog");
	}
}
