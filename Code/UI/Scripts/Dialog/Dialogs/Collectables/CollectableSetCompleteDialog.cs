using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
using UnityEngine;

public class CollectableSetCompleteDialog : DialogBase 
{
	[SerializeField] private ButtonHandler viewAlbumButtonHandler;
	[SerializeField] private ButtonHandler ctaButtonHandler;
	[SerializeField] private ButtonHandler okayButtonHandler;
	[SerializeField] private LabelWrapperComponent setAwardLabel;
	[SerializeField] private LabelWrapperComponent setNameLabel;
	[SerializeField] private LabelWrapperComponent setCardAmountLabel;
	[SerializeField] private LabelWrapperComponent ctaButtonLabel;
	[SerializeField] private LabelWrapperComponent ctaTextLabel;
	[SerializeField] private Animator setCompleteAnimator;
	[SerializeField] private Renderer setImage;
	[SerializeField] private UITexture setContainerTexture;
	[SerializeField] private TextMeshPro subheaderLabel; // So we can turn off the keep spinning surfacing when the final set is done
	[SerializeField] private GameObject powerupsContainer;
	[SerializeField] private GameObject setImageContainer;
	[SerializeField] private GameObject setBackgroundContainer;
	[SerializeField] private GameObject setBackgroundGlowContainer;
	[SerializeField] private GameObject inProgressContainer;

	private CollectableSetData completedSet = null;
	private Queue<string> completedSets = null;
	private long albumCompleteReward = 0;
	private JSON starPackData = null;
	private bool creditsAdded = false;
	private string eventId = "";

	private const float ROLLUP_DELAY = 2.7f;
	private const float BUTTON_SOUND_DELAY = 1.3f;

	private const string SET_COMPLETE_ANIM_NAME = "intro";

	private const string COLLECT_BUTTON_LOC = "collect";
	private const string SET_COMPLETE_LOC = "set_complete";
	private const string CONTINUE_BUTTON_LOC = "continue";
	private const string ALBUM_COMPLETE_LOC = "final_set_complete";
	private const string POWERUPS_COMPLETE_LOC = "powerups_set_complete";

	public override void init()
	{
		viewAlbumButtonHandler.registerEventDelegate(viewAlbumClicked);
		ctaButtonHandler.registerEventDelegate(ctaClicked);
		okayButtonHandler.registerEventDelegate(onCloseButtonClicked);
		completedSets = (Queue<string>) dialogArgs.getWithDefault(D.DATA, null);
		starPackData = (JSON) dialogArgs.getWithDefault(D.OPTION, null);
		eventId = (string)dialogArgs.getWithDefault(D.EVENT_ID, "");

		if (completedSets != null && completedSets.Count > 0)
		{
			string completedSetName = completedSets.Dequeue();
			completedSet = Collectables.Instance.findSet(completedSetName);
			completedSet.isComplete = true;
		}

		if (completedSet != null)
		{
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "set_award",
				klass: completedSet.keyName,
				genus: "view",
				val: completedSet.rewardAmount * CreditsEconomy.economyMultiplier);

			SlotsPlayer.addFeatureCredits(completedSet.rewardAmount, "completedSetDialog");
			
			if (completedSet.isPowerupsSet)
			{
				AssetBundleManager.load(PowerupBase.POWERUPS_SET_ASSET, powerupSetLoadSuccess, powerupSetLoadFailure);
				setImageContainer.SetActive(false);
				setBackgroundContainer.SetActive(false);
				setBackgroundGlowContainer.SetActive(false);
				setCardAmountLabel.gameObject.SetActive(false);
				Destroy(inProgressContainer); //destroy this so the animtion does not turn it on
				subheaderLabel.text = Localize.text(POWERUPS_COMPLETE_LOC);
			}
			else
			{
				StartCoroutine(DisplayAsset.loadTextureFromBundle(completedSet.texturePath, imageTextureLoaded, isExplicitPath:true));
				StartCoroutine(DisplayAsset.loadTextureFromBundle(Collectables.Instance.getAlbumByKey(Collectables.currentAlbum).setContainerTexturePath, containerTextureLoaded, isExplicitPath:true));
				
			}
			long awardAmount = completedSet.rewardAmount;
			int cardInSet = completedSet.cardsInSet.Count;
			setCardAmountLabel.text = string.Format("{0}/{1}", cardInSet, cardInSet);
			string completedSetName = Localize.text(completedSet.keyName + "_title");
			setNameLabel.text = string.Format("You completed the {0} set", completedSetName);
			//Look for a completed album reward
			albumCompleteReward = (long)dialogArgs.getWithDefault(D.AMOUNT, 0L);

			//Now see if we have more completed sets to show

			if (completedSets != null && completedSets.Count > 0)
			{
				viewAlbumButtonHandler.gameObject.SetActive(false);
				closeButtonHandler.gameObject.SetActive(false);
				okayButtonHandler.gameObject.SetActive(false);
				ctaButtonHandler.gameObject.SetActive(true);
				ctaButtonLabel.text = Localize.textOr(COLLECT_BUTTON_LOC, "Collect!");
				ctaTextLabel.text = Localize.textOr(SET_COMPLETE_LOC, "Set Complete!");
			}
			else if (albumCompleteReward > 0)
			{
				viewAlbumButtonHandler.gameObject.SetActive(false);
				closeButtonHandler.gameObject.SetActive(false);
				okayButtonHandler.gameObject.SetActive(false);
				ctaButtonHandler.gameObject.SetActive(true);
				subheaderLabel.gameObject.SetActive(false);
				ctaButtonLabel.text = Localize.textOr(CONTINUE_BUTTON_LOC, "Continue");
				ctaTextLabel.text = Localize.textOr(ALBUM_COMPLETE_LOC, "Final Set Complete!");
			}
			else if (starPackData != null)
			{
				viewAlbumButtonHandler.gameObject.SetActive(false);
				closeButtonHandler.gameObject.SetActive(false);
				okayButtonHandler.gameObject.SetActive(false);
				ctaButtonHandler.gameObject.SetActive(true);
				ctaButtonLabel.text = Localize.textOr(COLLECT_BUTTON_LOC, "Collect");
				ctaTextLabel.text = Localize.textOr(ALBUM_COMPLETE_LOC, "You've earned enough Stars\nfor the <#77ff41>Extras Bonus!</color>");
			}
			else
			{
				viewAlbumButtonHandler.gameObject.SetActive(true);
				closeButtonHandler.gameObject.SetActive(true);
				okayButtonHandler.gameObject.SetActive(true);
				ctaButtonHandler.gameObject.SetActive(false);
			}

			setAwardLabel.text = CreditsEconomy.convertCredits(completedSet.rewardAmount);
		}

		cancelAutoClose();
	}

	private void imageTextureLoaded(Texture2D tex, Dict data = null)
	{
		if (setImage != null && tex != null)
		{
			setImage.material.mainTexture = tex;
			setImage.material.color = Color.white;
		}
		else if (setImage == null)
		{
			Debug.LogError("Set Container renderer is null");
		}
		else
		{
			Debug.LogError("CollectableSetCompleteDialog::imageTextureLoaded - downloaded texture was null!");
		}
		StartCoroutine(startAnimationAndRollup());

	}

	private void containerTextureLoaded(Texture2D tex, Dict data = null)
	{
		if (setContainerTexture != null && tex != null)
		{
			Material setContainerMaterial = new Material(setContainerTexture.material.shader);
			setContainerMaterial.mainTexture = tex;
			setContainerTexture.material = setContainerMaterial;
			setContainerTexture.gameObject.SetActive(true);
		}
		else
		{
			Debug.LogError("CollectableSetCompleteDialog::imageTextureLoaded - downloaded texture was null!");
		}
	}

	private void powerupSetLoadSuccess(string assetPath, object loadedObj, Dict data = null)
	{
		GameObject setContainer = loadedObj as GameObject;
		if (setContainer != null)
		{
			CommonGameObject.instantiate(setContainer, powerupsContainer.transform);
		}
		
		StartCoroutine(startAnimationAndRollup());
	}
	
	public void powerupSetLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load asset at path " + assetPath);
	}

	private IEnumerator startAnimationAndRollup()
	{
		Audio.play("SetCompletePresentedCollections");
		setCompleteAnimator.Play(SET_COMPLETE_ANIM_NAME);
		yield return new WaitForSeconds(ROLLUP_DELAY);
		Overlay.instance.top.updateCredits(false); //Rollup the top overlay to the new player amount
		creditsAdded = true;
		yield return new WaitForSeconds(BUTTON_SOUND_DELAY);
		Audio.play("CollectSetButtonAppearCollections");
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		if (completedSet != null)
		{
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "set_award",
				klass: completedSet.keyName,
				family: "close",
				genus: "click",
				val: completedSet.rewardAmount * CreditsEconomy.economyMultiplier);
		}
		
		Dialog.close();
	}

	public void viewAlbumClicked(Dict args = null)
	{	
		if (completedSet != null)
		{
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "set_award",
				klass: completedSet.keyName,
				family: "collection",
				genus: "click",
				val: completedSet.rewardAmount * CreditsEconomy.economyMultiplier);
		}

		CollectableAlbumDialog.showDialog(Collectables.currentAlbum, "set_complete", isTopOfList:true);
		Dialog.close();
	}

	public void ctaClicked(Dict args = null)
	{
		Audio.play("ButtonSubmitCollections");
		Dialog.close();
	}

	public override void close()
	{
		if (completedSets != null && completedSets.Count > 0)
		{
			CollectableSetCompleteDialog.showDialog(completedSets, eventId, albumCompleteReward, starPackData);
		}
		else if (albumCompleteReward > 0)
		{
			Audio.play("ButtonViewCardsCollections");
			CollectableAlbumDialog.showDialog(Collectables.currentAlbum, "collection_complete", false, albumCompleteReward, eventId:eventId, isTopOfList:true); //Add the flag to show it with the special album complete stuff
		}
		else if (starPackData != null)
		{
			Collectables.claimPackDropNow(starPackData); //Queue up the star pack if we have one (Might only happen on CTA clicked)
		}
		
		if (!creditsAdded)
		{
			Overlay.instance.top.updateCredits(false); //Rollup the top overlay to the new player amount
		}

		//Only mark the pack as seen and unpause RR if we don't have more sets to show, a star pack to show, or an album complete to show
		if ((completedSets == null || completedSets.Count == 0) && starPackData == null && albumCompleteReward <= 0)
		{
			string gameKey = "";
			if (SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame && GameState.game != null)
			{
				gameKey = GameState.game.keyName;
			}
			
			if (SlotventuresLobby.instance != null)
			{
				SlotventuresLobby svLobby = SlotventuresLobby.instance as SlotventuresLobby;
				if (svLobby != null && svLobby.waitingForCardPackToFinish)
				{
					RoutineRunner.instance.StartCoroutine((SlotventuresLobby.instance as SlotventuresLobby).scrollToNextGame());
				}
			}
		}

		if (completedSet.isPowerupsSet)
		{
			Collectables.Instance.resetSet(completedSet.albumName, completedSet.keyName);
		}
	}

	// If we ever had multiple albums active at once, knowing what album we were going into would be nice. 
	public static void showDialog(Queue<string> completedSets, string eventId, long albumCompleteAmount = 0, JSON starPackdata = null)
	{
		Dict args = Dict.create
		(
			D.DATA, completedSets,
			D.EVENT_ID, eventId,
			D.AMOUNT, (long)albumCompleteAmount,
			D.OPTION, starPackdata,
			D.IS_TOP_OF_LIST, true
		);
		Scheduler.addDialog("collectables_set_complete", args, SchedulerPriority.PriorityType.IMMEDIATE);
	}
}
