using UnityEngine;
using System.Collections;
using Com.Scheduler;

/*
Controls display and functionality of VIP introduction animations.
*/

public class VIPLevelUpDialogHIR : VIPLevelUpDialog
{
	public GameObject awardAnimationPrefab = null;

	// Initialization
	public override void init()
	{
		base.init();
		StatsManager.Instance.LogCount("dialog", "vip_level_up", "", VIPLevel.find(SlotsPlayer.instance.vipNewLevel).name, "", "view");
	}
	
	public void animContinueClicked()
	{
		Dialog.close();

		if (isFirstTime)
		{
			if (VIPLobbyHIRRevamp.instance != null)
			{
				VIPLobbyHIRRevamp.instance.showBenefits();
			}
			else
			{
				// BY 05-21-2018: deprecated (at least until art replaces it with new lobby v3 versions)
				//VIPDialog.showDialog(false, true);
			}
		}
		else if (GameState.isMainLobby)
		{
			// If in the any lobby when leveling up, refresh the lobby when closing this dialog.
			Scheduler.addFunction(MainLobby.refresh);
		}
	} 

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		  
		// Instantiate the award animation from a prefab,
		// since this animation is also used by other dialogs.
		GameObject go = CommonGameObject.instantiate(awardAnimationPrefab) as GameObject;
		go.transform.parent = sizer.transform;
		go.transform.localScale = Vector3.one;
		go.transform.localPosition = Vector3.zero;
		
		VIPAwardAnimation awardAnim = go.GetComponent<VIPAwardAnimation>();
		
		StartCoroutine(awardAnim.init(level, false, isFirstTime, animContinueClicked));
	}
	
	public override void close()
	{
		if (!isFirstTime)	// Don't play any music if we are transitioning to a dialog with the same key.
		{
			playMusicWhenClosing();
		}
			
		if (VIPStatusBoostEvent.isEnabled() && !VIPStatusBoostEvent.isEnabledByPowerup())
		{
			VIPStatusBoostMOTD.showDialog();
		}
	}
}
