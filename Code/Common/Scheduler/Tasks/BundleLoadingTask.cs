using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.LobbyTransitions;
using Com.Scheduler;
using Com.States;

//Needs a loadingTooltip controller
//Accepts a dialog key and will block until dialog bundle is finished downloading
//Meant to be used to play loading animation while assets for dialog are being loaded
//Finishes its execution once the bundle for the given dialog has finished downloading

public class BundleLoadingTask : SchedulerTask
{
	private BottomOverlayButtonToolTipController unlockedToolTipController;
	private string dialogKey = "";
	
	public BundleLoadingTask(Dict args = null) : base(args)
	{
		if (args != null)
		{
			unlockedToolTipController = args.getWithDefault(D.OBJECT, null) as BottomOverlayButtonToolTipController;
			dialogKey = args.getWithDefault(D.KEY, "") as string;
		}
	}

	public override void execute()
	{
		base.execute();
		if (unlockedToolTipController == null || string.IsNullOrEmpty(dialogKey))
		{
			bundleReady();
		}
		else
		{
			unlockedToolTipController.playLoadingTooltip();
			DialogType dialogType = DialogType.find(dialogKey);
			if (dialogType == null)
			{
				bundleReady();
				return;
			}

			string bundle = AssetBundleManager.getBundleNameForResource(dialogType.dialogPrefabPath);
			if (!string.IsNullOrEmpty(bundle))
			{
				if (AssetBundleManager.isBundleCached(bundle))
				{
					bundleReady();
				}
				else
				{
					unlockedToolTipController.setLockedText("downloading");
					unlockedToolTipController.StartCoroutine(unlockedToolTipController.playLockedTooltip());
					AssetBundleManager.load(this, dialogType.dialogPrefabPath, onBundleDownloaded, onBundleFailed, isSkippingMapping:dialogType.isSkippingBundleMap, fileExtension:".prefab");
				}
			}
		}
	}

	private void bundleReady()
	{
		if (unlockedToolTipController != null)
		{
			unlockedToolTipController.stopLoadingTooltip();
		}

		Scheduler.removeTask(this);
	}

	private void onBundleDownloaded(string assetPath, Object obj, Dict data = null)
	{
		bundleReady();
	}

	private void onBundleFailed(string assetPath, Dict data = null)
	{
		bundleReady();
	}
}