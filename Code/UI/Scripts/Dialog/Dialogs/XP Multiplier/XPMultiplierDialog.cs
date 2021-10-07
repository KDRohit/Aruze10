using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class XPMultiplierDialog : DialogBase
{
	public TextMeshPro timeLabel;
	private const string S3UrlForDouble = "dynamic_dialogs/doublexp_windowed.png";
	private const string S3UrlForTriple = "dynamic_dialogs/triplexp_windowed.png";
	public UITexture backgroundTexture;

	public ClickHandler okButtonHandler;

	private static string[] multiplierStrings = new string[]
	{
		"",
		"xp",
		"double_xp",
		"triple_xp",
		"quadruple_xp"
	};
	
	//Used by outside classes
	public static string getMultiplierString(int multiplier, bool toUpper = false)
	{
		if ((multiplier < 1) || (multiplier >= multiplierStrings.Length))
		{
			multiplier = 1;
		}

		string multiplierString = Localize.text(multiplierStrings[multiplier]);
						
		if (toUpper)
		{
			multiplierString = Localize.toUpper(multiplierString);
		}
		
		return multiplierString;
	}
	
	public override void init()
	{
		downloadedTextureToUITexture(backgroundTexture, 0);

		XPMultiplierEvent.instance.featureTimer.combinedActiveTimeRange.registerLabel(timeLabel, GameTimerRange.TimeFormat.REMAINING);
				
		StatsManager.Instance.LogCount("dialog", "need_credits", "view", StatsManager.getGameTheme(), StatsManager.getGameName());
		Audio.play("WoW_spin_wheel");
		
		StatsManager.Instance.LogCount("dialog", "double_xp", "view");
		
		// Register this to close the dialog if it runs out while it is open.
		XPMultiplierEvent.instance.onDisabledEvent += autocloseDialog;
		
		if (okButtonHandler != null)
		{
			okButtonHandler.registerEventDelegate(onCloseButtonClicked);
		}
	}

	protected virtual void Update()
	{	
		AndroidUtil.checkBackButton(clickClose, "dialog", "xp_multiplier", "back", StatsManager.getGameTheme(), StatsManager.getGameName(), "back");
	}

	public virtual void clickClose()
	{
		Dialog.close();
		StatsManager.Instance.LogCount("dialog", "double_xp", "click");
	}

	protected void autocloseDialog()
	{
		// Close without stat.
		Dialog.close();
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		okButtonHandler.unregisterEventDelegate(onCloseButtonClicked);
		StatsManager.Instance.LogCount("dialog", "double_xp", "", "close");
		PlayerAction.seeXPMultiplierDialog();
	}
	
	public static bool showDialog()
	{
		int xpMultiplier = XPMultiplierEvent.instance != null ? XPMultiplierEvent.instance.xpMultiplier : 0;
		bool returnValue = false;
		switch (xpMultiplier)
		{
			case 2:
				Dialog.instance.showDialogAfterDownloadingTextures("xp_multiplier", S3UrlForDouble, null, true);
				returnValue = true;
				break;
			case 3:
				Dialog.instance.showDialogAfterDownloadingTextures("xp_multiplier", S3UrlForTriple, null, true);
				returnValue = true;
				break;

		}
		return returnValue;
	}
}
