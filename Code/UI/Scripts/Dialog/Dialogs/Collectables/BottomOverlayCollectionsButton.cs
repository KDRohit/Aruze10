using System;
using UnityEngine;
using System.Collections;

public class BottomOverlayCollectionsButton : BottomOverlayButton
{
	[SerializeField] private ButtonHandler collectionsButtonHandler;
	[SerializeField] private GameObject newCardsBadge;
	[SerializeField] private LabelWrapperComponent newCardsLabel;

	private bool hasEventEnding = false;

	protected override void Awake()
	{
		base.Awake();
		if (ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
		{
			sortIndex = 1;
		}
		else
		{
			sortIndex = 2;
		}
		if (CampaignDirector.richPass != null && CampaignDirector.richPass.isActive)
		{
			sortIndex++;
		}

		if (EliteManager.isActive)
		{
			sortIndex++;
		}
		collectionsButtonHandler.registerEventDelegate(collectionsClicked);
		hasViewedFeature = EueFeatureUnlocks.hasFeatureBeenSeen(featureKey);

		if (Collectables.isLevelLocked())
		{
			initLevelLock(false);
		}
		else
		{
			
			if (Collectables.isActive())
			{
				Collectables.registerCollectionEndHandler(onCollectionsEnd);
				initNewCardsAlert();
			}
			else
			{
				hideAlert();
				toolTipController.setLockedText(BottomOverlayButtonToolTipController.COMING_SOON_LOC_KEY);	
			}
			
			if (needsToShowUnlockAnimation())
			{
				showUnlockAnimation();
			}
			else if (needsToForceShowFeature())
			{
				collectionsClicked(Dict.create(D.OPTION, true));
			}
		}
	}

	private void onCollectionsEnd(object sender, System.EventArgs e)
	{
		//Swap to "Coming Soon" version of the button if the feature ends while we're still in the lobby
		hideAlert();
		toolTipController.setLockedText(BottomOverlayButtonToolTipController.COMING_SOON_LOC_KEY);
	}
	
	public void collectionsClicked(Dict args = null)
	{
		if (Collectables.isLevelLocked())
		{
			logLockedClick();
			StartCoroutine(toolTipController.playLockedTooltip());
		}
		else if (!Collectables.isEventTimerActive() || !ExperimentWrapper.Collections.isInExperiment ||!Collectables.hasValidBundles())
		{
			logComingSoonClick();
			StartCoroutine(toolTipController.playLockedTooltip());
		}
		else
		{
			showLoadingTooltip("collectables_album");
			if (!hasViewedFeature)
			{
				logFirstTimeFeatureEntry(args);
				Collectables.showVideo();
				markFeatureSeen();
				initNewCardsAlert();
			}
			//get first pack or show album here.
			if (!Collectables.Instance.hasCards)
			{
				Collectables.Instance.startFeature();
			}
			else
			{
				Audio.play("ButtonViewCardsCollections");
				CollectableAlbumDialog.showDialog(Collectables.currentAlbum, "bottom_nav");

				StatsManager.Instance.LogCount(
					counterName:"bottom_nav",
					kingdom:	"collections",
					phylum:		SlotsPlayer.isFacebookUser ? "fb_connected" : "anonymous",
					genus:		"click"
				);

				if (Dialog.instance != null && Dialog.instance.currentDialog != null && Dialog.instance.currentDialog.type.keyName == "start_collecting_dialog")
				{
					Dialog.close();
				}	
			}
		}
	}

	void Update()
	{
		if (!hasEventEnding)
		{
			updateEventEnds();
		}
	}

	public void initNewCardsAlert()
	{
		if (!hasViewedFeature)
		{
			toolTipController.toggleNewBadge(!needsToShowUnlockAnimation());
			newCardsBadge.SetActive(false);
			return;
		}
		
		int currentNewCards = 0;
		if (null != Collectables.currentAlbum && null != Collectables.Instance.getAlbumByKey(Collectables.currentAlbum))
		{
			currentNewCards = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum).currentNewCards;
		}

		if (Collectables.isActive() && Collectables.endTimer.timeRemaining > Common.SECONDS_PER_DAY * 3)
		{
			newCardsBadge.SetActive(currentNewCards > 0);
			newCardsLabel.text = currentNewCards.ToString();
		}
		else
		{
			updateEventEnds();
		}
	}

	protected override void initLevelLock(bool isBeingUnlocked)
	{
		base.initLevelLock(isBeingUnlocked);
		hideAlert();
	}

	public void hideAlert()
	{
		newCardsBadge.SetActive(false);
	}

	public void showAlert()
	{
		newCardsBadge.SetActive(true);
	}

	public void updateEventEnds()
	{
		if (Collectables.isActive() && Collectables.endTimer.timeRemaining <= Common.SECONDS_PER_DAY * 3 && hasViewedFeature)
		{
			if (!hasEventEnding)
			{
				newCardsLabel.text = Localize.text("ends_soon");
				newCardsBadge.SetActive(true);
				hasEventEnding = true;
			}
		}
	}

	protected override bool needsToForceShowFeature()
	{
		return base.needsToForceShowFeature() && Collectables.isEventTimerActive() && ExperimentWrapper.Collections.isInExperiment;
	}

	void OnDestroy()
	{
		Collectables.unregisterCollectionEndHandler(onCollectionsEnd);
	}
}
