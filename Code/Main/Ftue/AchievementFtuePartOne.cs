using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;

public class AchievementFtuePartOne : FtueBase
{
	public GameObject FtueTab;
	public TextMeshPro TabLabel;
	public UISprite TabSprite;
	public GameObject TabCounter;
	private const string TROPHIES_TAB = "trophies_tab";
	private const string TROPHIES_BUTTON = "trophies_button";
	private const string TROPHIES_FRAME_TEXT = "trophies_frame_text";

	public AchievementFtuePartOne()
	{
	}

	public override void Awake()
	{
		base.Awake();
		TabLabel.text = Localize.text(TROPHIES_TAB);
		buttonHandler.text = Localize.text(TROPHIES_BUTTON);
		ftueText.text = Localize.text (TROPHIES_FRAME_TEXT);
		StatsManager.Instance.LogCount("dialog", "ll_trophy_ftue", "new_trophy_tab", "view", "", SlotsPlayer.instance.networkID, 1);

		//need to wait for initialization to finish
		StartCoroutine(waitAndPosition());
	}

	public override void Initialize ()
	{
		base.Initialize ();
	}

	private IEnumerator  waitAndPosition()	
	{
		//wait two frames for init code to position and activate the scripts (otherwise transform functions don't actually do anything)
		yield return null;
		yield return null;
		yield return null;

		NetworkProfileDialog dialog = NetworkProfileDialog.instance;
		
		if (dialog != null)
		{

			matchPosition(TabSprite.gameObject, dialog.trophyTabSpriteTransform);
			
			matchPosition(TabLabel.gameObject, dialog.trophyTabLabelTransform);
			TabLabel.transform.localPosition = new Vector3(TabLabel.transform.localPosition.x, TabLabel.transform.localPosition.y, -1);
			
			matchPosition(TabCounter, dialog.trophyTabCounterTransform);
			TabCounter.transform.localPosition = new Vector3(TabCounter.transform.localPosition.x, TabCounter.transform.localPosition.y, -2);

			dialog.cleanForFtue();
			
		}
		else
		{
			Debug.LogError("Can't find network profile dialog");
		}
	}

	private void matchPosition(GameObject obj, Transform trans)
	{
		Transform originalParent = obj.transform.parent;

		obj.transform.localPosition = Vector3.zero;
		obj.transform.localRotation = Quaternion.identity;
		obj.transform.localScale = Vector3.one;

		obj.transform.SetParent(trans, false);
		obj.transform.SetParent(originalParent, true);
		obj.transform.localPosition = new Vector3(obj.transform.localPosition.x, trans.localPosition.y, obj.transform.localPosition.z);
	}

	public override void ButtonClick(Dict args)
	{
		StatsManager.Instance.LogCount("dialog", "ll_trophy_ftue", "new_trophy_tab", "skip", "", SlotsPlayer.instance.networkID, 1);
		Destroy (FTUEManager.Instance.Go);
	}

	public override void TabClick(Dict args)
	{
		StatsManager.Instance.LogCount ("dialog", "ll_trophy_ftue", "new_trophy_tab", "click", "", SlotsPlayer.instance.networkID, 1);
		Destroy (FTUEManager.Instance.Go);
		NetworkProfileDialog networkProfileDialog = (NetworkProfileDialog)Dialog.instance.currentDialog;
		networkProfileDialog.changeTab (NetworkProfileDialog.PageTabTypes.ACHIEVEMENTS);
		networkProfileDialog.switchState (NetworkProfileDialog.ProfileDialogState.ACHIEVEMENTS);
	}

}


