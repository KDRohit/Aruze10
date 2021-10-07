using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
/*
HIR version of the linked vip program dialog
*/

public class LinkedVipProgramDialogHIR: LinkedVipProgramDialog
{
	// The connect button for the linked vip dialog
	public Renderer loyaltyLoungeRenderer;
	public GameObject introScreen;

	public const string LOGO_PATH = "misc_dialogs/linked_vip/large_loyalty_lounge_logo.png";	// In an asset bundle.

	//Initialization
	public override void init() 
	{
		base.init();
		
		// Make sure contents are off by default.
		introScreen.SetActive(false);
		contentParent.SetActive(false);

		downloadedTextureToRenderer(loyaltyLoungeRenderer, 2);
	}

	protected override void showContentBehindCurtain()
	{
		introScreen.SetActive(true);
	}

	// NGUI button callback
	private void showNext()
	{
		// Shows the next piece of our two part presentation
		introScreen.SetActive(false);
		contentParent.SetActive(true);
		Audio.playMusic("FeatureBgLL");
		Audio.switchMusicKey("FeatureBgLL");
	}
}



