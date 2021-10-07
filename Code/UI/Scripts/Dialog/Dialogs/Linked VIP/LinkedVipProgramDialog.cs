using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
/*
 * This Class is for the linked vip program dialog
 * */

public class LinkedVipProgramDialog: DialogBase
{

	public GameObject animationParent;
	public GameObject contentParent;

	// The connect button for the linked vip dialog
	[SerializeField] private ImageButtonHandler closeButton;
	[SerializeField] private ImageButtonHandler continueButton;
	
	public Renderer[] curtainRenderers;	// Left and right.

	// This is a test.
	//Initialization
	public override void init() 
	{
		closeButton.registerEventDelegate(closeClicked);
		continueButton.registerEventDelegate(continueClicked);
		downloadedTextureToRenderer(curtainRenderers[0], 1);
		downloadedTextureToRenderer(curtainRenderers[1], 1);
		
		// Setting this to run the animation first.
		contentParent.SetActive(false);
		animationParent.SetActive(false);

		MOTDFramework.markMotdSeen(dialogArgs);
		StatsManager.Instance.LogCount ("dialog", "linked_vip", "intro_linked_vip", "", "", "view");
		
		Audio.play("SpecialAlertFanfare");
	}
	
	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		
		// Wait until the loading screen isn't visible before starting the curtain animation.
		StartCoroutine(startAfterLoadingScreen());
	}
	
	private IEnumerator startAfterLoadingScreen()
	{
		while (Loading.isLoading)
		{
			yield return null;
		}
		
		// Show the curtains now.
		animationParent.SetActive(true);

		// Wait a couple seconds before showing stuff that is visible when the curtain opens again.
		yield return new WaitForSeconds(2f);
		showContentBehindCurtain();
	}
	
	protected virtual void showContentBehindCurtain()
	{
		// This should be overrided by subclasses to show whatever it wants to show first when the curtain opens.
	}

	private void continueClicked(Dict args = null)
	{
		Dialog.close();
		LinkedVipConnectDialog.showDialog(SchedulerPriority.PriorityType.IMMEDIATE);
	}
	
	// Callback for the close button.
	private void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount ("dialog", "linked_vip", "intro_linked_vip", "", "close", "click");
		Dialog.close();
		Audio.play("QuitFeatureLL");
		MainLobby.playLobbyMusic();
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	//Show the dialog
	public static bool showDialog(string motdKey = "") 
	{
		if (LinkedVipProgram.instance.isEligible)
		{
			LinkedVipProgram.instance.updateStatusAndOpenDialog("program_dialog", "", motdKey);
			// Should this stat call be here and not in the dialog itself?
			StatsManager.Instance.LogCount ("dialog", "linked_vip", "intro_linked_vip", "", "sign_up_now", "view");
			return true;
		}
		return false;
	}

	// Callback for the sign up button.
	private void signUpClicked()
	{
		Dialog.close();
		if (!LinkedVipProgram.instance.isConnected && !LinkedVipProgram.instance.isPending)
		{
			// Spawn the network connect dialog.
			LinkedVipConnectDialog.showDialog(SchedulerPriority.PriorityType.IMMEDIATE);
		}
		else
		{
			LinkedVipStatusDialog.checkNetworkStateAndShowDialog();
		}
		StatsManager.Instance.LogCount ("dialog", "linked_vip", "intro_linked_vip", "", "sign_up_now", "click");
	}

}



