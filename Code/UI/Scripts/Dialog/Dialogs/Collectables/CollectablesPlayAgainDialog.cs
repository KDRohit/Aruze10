using System.Collections;
using Com.Scheduler;
using UnityEngine;

public class CollectablesPlayAgainDialog : DialogBase 
{
	[SerializeField] private ButtonHandler closeHandler;
	[SerializeField] private ButtonHandler reviewCollectionHandler;
	[SerializeField] private MultiLabelWrapperComponent albumAwardLabel;
	[SerializeField] private Renderer logoImage;

	private const string SET_COMPLETE_ANIM_NAME = "intro";

	public override void init()
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "collection_repeat",
			klass: Collectables.currentAlbum,
			genus: "view");
		
		Audio.play("ResetDialogueCollections");
		CollectableAlbum currentAlbum = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum);
		AssetBundleManager.load(currentAlbum.logoTexturePath, logoLoadedSuccess, bundleLoadFail);

		closeHandler.registerEventDelegate(closeClicked);
		reviewCollectionHandler.registerEventDelegate(reviewCollectionClicked);
		//Assuming we've updated the static album in our dictionary
		long newRewardAmount = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum).rewardAmount;

		if (Collectables.nextIterationData != null)
		{
			JSON allAlbumsData = Collectables.nextIterationData.getJSON("albums");
			if (allAlbumsData != null)
			{
				JSON currentAlbumsData = allAlbumsData.getJSON(currentAlbum.keyName);
				if (currentAlbumsData != null)
				{
					newRewardAmount = currentAlbumsData.getLong("reward", 0);
				}
			}
		}
		albumAwardLabel.text = CreditsEconomy.convertCredits(newRewardAmount);
	}

	public void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "collection_repeat",
			klass: Collectables.currentAlbum,
			family: "continue",
			genus: "click");
		
		Collectables.Instance.resetAlbum(Collectables.currentAlbum);
		Audio.play("ResetConfirmCollections");
		Dialog.close();
	}

	public void reviewCollectionClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "collection_repeat",
			klass: Collectables.currentAlbum,
			family: "collection",
			genus: "click");
		
		CollectableAlbumDialog.showDialog(Collectables.currentAlbum, "collection_complete", false, 0, true, isTopOfList:true);
		Audio.play("ResetConfirmCollections");
		Dialog.close();
	}

	private void bundleLoadFail(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load set image at " + assetPath);
	}

	private void logoLoadedSuccess(string assetPath, Object obj, Dict data = null)
	{
		Material material = new Material(logoImage.material.shader);
		material.mainTexture = obj as Texture2D;
		logoImage.material = material;
	}

	public override void close()
	{
		if (SlotventuresLobby.instance != null)
		{
			SlotventuresLobby svLobby = SlotventuresLobby.instance as SlotventuresLobby;
			if (svLobby != null && svLobby.waitingForCardPackToFinish)
			{
				RoutineRunner.instance.StartCoroutine((SlotventuresLobby.instance as SlotventuresLobby).scrollToNextGame());
			}
		}
	}

	// If we ever had multiple albums active at once, knowing what album we were going into would be nice. 
	public static void showDialog(string eventId)
	{
		Dict args = Dict.create(D.EVENT_ID, eventId, D.IS_TOP_OF_LIST, true); //This should always end up at the top so it can directly follow the Collectable Album dialog that triggered it
		Scheduler.addDialog("collectables_play_again", args);
	}
}
