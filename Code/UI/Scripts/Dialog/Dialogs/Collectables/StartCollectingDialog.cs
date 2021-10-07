using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

public class StartCollectingDialog : DialogBase {

	private const float GOAL_ASPECT_RATIO = 2.1f;

	[SerializeField] private ButtonHandler closeHandler;
	[SerializeField] private ButtonHandler okButtonHandler;
	[SerializeField] private Transform collectionButtonAnchor;
	[SerializeField] private UIAnchor bubbleAnchor;
	[SerializeField] private UIAnchor collectionsButtonAnchor;
	
	private Transform formerParent;
	private Transform objectToMove;
	private Vector3 originalLocalPos;
	private bool needsToResetOverlayButton = false;

	private const string BUTTON_PREFAB_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Lobby Prefabs/Instanced Prefabs/Collections Button Item.prefab";

	public override void init()
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "ftue_collection_conclusion",
			klass: Collectables.currentAlbum,
			family: (bool)dialogArgs.getWithDefault(D.OPTION, false) ? "help" : "ftue",
			genus: "view");
		
		closeHandler.registerEventDelegate(clickOk);
		okButtonHandler.registerEventDelegate(clickOk);
	}

	protected override void onFadeInComplete ()
	{
		highlightTab(); //Need to grab the icon from the 
		Audio.play("Alert1FTUE");
		base.onFadeInComplete();
	}

	public void highlightTab()
	{
		if (MainLobby.instance != null)
		{
			Transform collectionsAnchor = MainLobbyBottomOverlayV4.instance.getCollectionsAnchorTransform();

			if (MainLobby.hirV3 != null)
			{
				collectionButtonAnchor.localScale = Vector3.one * MainLobby.hirV3.getScaleFactor();
			}

			GameObject collectionsButtonObj = NGUITools.AddChild(collectionButtonAnchor, SkuResources.getObjectFromMegaBundle<GameObject>(BUTTON_PREFAB_PATH));
			CommonGameObject.setLayerRecursively(collectionsButtonObj, Layers.ID_NGUI);
			BottomOverlayCollectionsButton collectionsButton = collectionsButtonObj.GetComponent<BottomOverlayCollectionsButton>();

			if (collectionsButton != null)
			{
				collectionsButton.hideAlert();
			}

			if (collectionsAnchor != null)
			{
				CommonTransform.matchScreenPosition(collectionButtonAnchor, collectionsAnchor);	
			}
		}
		else
		{
			bubbleAnchor.gameObject.SetActive(false); //Disable this object that usually highlights the lobby button
		}
	}

	public override void close()
	{
		if (needsToResetOverlayButton)
		{
			resetOverlayButton();
		}
	}

	private void resetOverlayButton()
	{
		if (objectToMove && formerParent != null)
		{
			objectToMove.SetParent(formerParent);
			objectToMove.localPosition = originalLocalPos;
			CommonGameObject.setLayerRecursively(objectToMove.gameObject, Layers.ID_NGUI_OVERLAY);
			objectToMove.gameObject.SetActive(false);
			objectToMove.gameObject.SetActive(true);
		}
		needsToResetOverlayButton = false;
	}

	private void clickOk(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "ftue_collection_conclusion",
			klass: Collectables.currentAlbum,
			family: "continue",
			genus: "click");

		if (needsToResetOverlayButton)
		{
			resetOverlayButton();
		}

		//get the powerups pack
		Collectables.getPowerupsPack();
		Dialog.close();
	}

	public static void showDialog(bool manualStart = false)
	{
		Dict args = Dict.create(D.OPTION, manualStart, D.IS_TOP_OF_LIST, true); //Always forcing this to the top since it should always follow after the FTUE flow from the album dialog
		Scheduler.addDialog
		(
			"start_collecting_dialog",
			args,
			SchedulerPriority.PriorityType.BLOCKING  //change to blocking
		);
	}
}
