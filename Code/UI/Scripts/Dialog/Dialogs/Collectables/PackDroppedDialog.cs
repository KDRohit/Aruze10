using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

public class PackDroppedDialog : DialogBase 
{
	[SerializeField] private ButtonHandler closeHandler;
	[SerializeField] private ButtonHandler starPackButton;
	[SerializeField] private ButtonHandler setCompleteButton;
	[SerializeField] private ButtonHandler viewAlbumButton;
	[SerializeField] private ButtonHandler okayButton;

	[SerializeField] private CollectAPack packPresentation;
	[SerializeField] private Transform packPresentationParent;
	[SerializeField] private LabelWrapperComponent headerLabel;
	[SerializeField] private LabelWrapperComponent starPackSubHeader;
	[SerializeField] private LabelWrapperComponent starPackRewardLabel;
	[SerializeField] private GameObject powerupsAnchor;
	[SerializeField] private GameObject powerupsPrefab;
	[SerializeField] private float bottomPanelAnimDelay = 0.0f;
	[SerializeField] private float powerupDropRoutineTime = 0.0f;
	[SerializeField] private GameObject[] packLogos;

	private CollectableAlbum currentAlbum;
	private PackDropType currentPackType = PackDropType.DEFAULT;
	private JSON starPackData = null;
	private string completedAlbum = "";
	private long starPackReward = -1;
	private long completeAlbumReward = 0;
	private Queue<string> completedSets;
	private bool introFinished = false;
	private string source = "";
	private int numberOfCardsInPack = 0;
	private string packId = "";
	private string eventId = "";
	private PowerupInGameUI powerupUI;

	private bool inRoyalRushGame = false;
	private RoyalRushCollectionModule rrMeter = null;
	private bool foundPowerupCard = false;

	private const string BOTTOM_CONTENT_INTRO_ANIM = "animation start";
	private const float ROLLUP_WAIT_TIME = 1.0f;
	private const string HEADER_LOCALIZATION = "collections_pack_drop_header_{0}";
	private const int PACK_PARENT_OFFSET = 300;
	
	public static bool completedPowerupDropRoutine = false;
	
	//The pack drop type determines what will be in the CTA button and what happens when that is clicked
	private enum PackDropType
	{
		DEFAULT, //Opens album on click
		STAR_PACK,
		PACK_WITH_STAR_PACK, //Opens pack on click
		SET_COMPLETE, //Opens set complete dialog on complete
		ALBUM_COMPLETE
	}

	public override void init()
	{
		completedPowerupDropRoutine = false;
		
		if (ExperimentWrapper.RoyalRush.isPausingInCollections && SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame && SlotBaseGame.instance.tokenBar != null)
		{
			inRoyalRushGame = true;
			rrMeter = SlotBaseGame.instance.tokenBar as RoyalRushCollectionModule;
			if (rrMeter != null)
			{
				rrMeter.pauseTimers();
			}
		}

		closeHandler.gameObject.SetActive(false); //Want to hide the close button when anpther pack is queued up
		eventId = (string)dialogArgs.getWithDefault(D.EVENT_ID, "");

		string currentAlbumName = (string)dialogArgs.getWithDefault(D.TITLE, "");
		currentAlbum = Collectables.Instance.getAlbumByKey(currentAlbumName);

		starPackData = (JSON)dialogArgs.getWithDefault(D.DATA, null);
		if (starPackData != null)
		{
			currentPackType = PackDropType.PACK_WITH_STAR_PACK;
		}

		List<CollectableCardData> collectedCards = (List<CollectableCardData>)dialogArgs.getWithDefault(D.COLLECTABLE_CARDS, null);
		source = (string)dialogArgs.getWithDefault(D.KEY, "spin");
		packId = (string)dialogArgs.getWithDefault(D.PACKAGE_KEY, "");
		numberOfCardsInPack = collectedCards.Count;

		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "pack_award",
			klass: source,
			family: packId,
			genus: "view",
			val: collectedCards.Count);
		
		if (source != "spin")
		{
			string cardPackLocalization = string.Format(HEADER_LOCALIZATION, source);
			if (Localize.keyExists(cardPackLocalization))
			{
				headerLabel.text = Localize.text(cardPackLocalization);
			}

			//Set the subheader for any pack that isn't a spin pack
			CollectablePackData purchasePackData = Collectables.Instance.findPack(packId);
			if (purchasePackData != null && purchasePackData.constraints != null && purchasePackData.constraints.Length > 0)
			{
				starPackSubHeader.text = Localize.text("collectables_pack_subheader", CommonText.digitToText(purchasePackData.constraints[0].guaranteedPicks.ToString()), purchasePackData.constraints[0].minRarity);
				starPackSubHeader.gameObject.SetActive(true);
			}
		}

		if (MainLobby.hirV3 != null && LobbyCarouselV3.instance != null)
		{
			MainLobby.hirV3.pageController.setScrollerActive(false);
			LobbyCarouselV3.instance.setCarousalScrollActive(false);
		}

		packPresentation.starMeter.init(currentAlbum, packId, starPackData != null, source);

		//Determine if we have any rewards information, which will go into determining the bottom button state, and the pack sprite
		JSON rewardsJson = (JSON)dialogArgs.getWithDefault(D.BASE_CREDITS, null);
		if (rewardsJson != null)
		{
			starPackReward = rewardsJson.getLong("jackpot", -1);
			completeAlbumReward = rewardsJson.getLong("album", 0);
			if (completeAlbumReward > 0)
			{
				Collectables.nextIterationData = rewardsJson.getJSON("next_iteration");
			}
			JSON completedSetsJson = rewardsJson.getJSON("sets");
			if (completedSetsJson != null)
			{
				List<string> completedSetsList = new List<string>();
				completedSetsList = completedSetsJson.getKeyList();
				completedSets = new Queue<string>(completedSetsList);
			}

			if (starPackReward >= 0)
			{
				StatsManager.Instance.LogCount(counterName:"dialog",
					kingdom: "hir_collection",
					phylum: "dupes_meter_award",
					klass: packId,
					genus: "view",
					val: starPackReward * CreditsEconomy.economyMultiplier);

				eventId = Collectables.Instance.starPackEventId;
				Collectables.Instance.starPackEventId = "";
				currentPackType = PackDropType.STAR_PACK;
				starPackRewardLabel.text = CreditsEconomy.convertCredits(starPackReward);
				CollectablePackData starPackConstraintData = Collectables.Instance.findPack(currentAlbum.starPackName);
				if (starPackConstraintData != null && starPackConstraintData.constraints != null && starPackConstraintData.constraints.Length > 0)
				{
					starPackSubHeader.text = Localize.text("collectables_star_pack_subheader", CommonText.digitToText(starPackConstraintData.constraints[0].guaranteedPicks.ToString()), starPackConstraintData.constraints[0].minRarity);
					starPackSubHeader.gameObject.SetActive(true);
				}
			}
			else if (completedSets != null && completedSets.Count > 0)
			{
				currentPackType = PackDropType.SET_COMPLETE;
				closeHandler.gameObject.SetActive(false); //Want to hide the close button when anpther pack is queued up
			}
		}

		//Only used when a player gets a star pack at the same time as completing the album becuase it needs special choreography
		JSON previousPackReward = (JSON)dialogArgs.getWithDefault(D.AMOUNT, null);
		if (previousPackReward != null)
		{
			JSON previousCompletedSetsJson = previousPackReward.getJSON("sets");
			completeAlbumReward = previousPackReward.getLong("album", 0);

			if (previousCompletedSetsJson != null)
			{
				List<string> previousCompletedSetsList = new List<string>();
				previousCompletedSetsList = previousCompletedSetsJson.getKeyList();
				completedSets = new Queue<string>(previousCompletedSetsList);
				currentPackType = PackDropType.STAR_PACK;
				closeHandler.gameObject.SetActive(false); //Want to hide the close button when anpther pack is queued up
			}
		}

		//Set the state of the bottom button
		switch (currentPackType)
		{
		case PackDropType.PACK_WITH_STAR_PACK:
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "dupes_meter_complete",
				klass: source,
				family: packId,
				genus: "view",
				val:collectedCards.Count);
			
			starPackButton.gameObject.SetActive(true);
			starPackButton.registerEventDelegate(ctaClicked);
			break;

		case PackDropType.STAR_PACK:
			if (starPackData != null) //Might have more than one star pack
			{
				starPackButton.gameObject.SetActive(true);
				starPackButton.registerEventDelegate(ctaClicked);
			}
			else if (completedSets != null && completedSets.Count > 0)
			{
				StatsManager.Instance.LogCount(counterName:"dialog",
					kingdom: "hir_collection",
					phylum: "set_complete",
					klass: source,
					family: packId,
					genus: "view");
				setCompleteButton.gameObject.SetActive(true);
				setCompleteButton.registerEventDelegate(ctaClicked);
			}
			else
			{
				viewAlbumButton.gameObject.SetActive(true);
				viewAlbumButton.registerEventDelegate(ctaClicked);
				okayButton.gameObject.SetActive(true);
				okayButton.registerEventDelegate(closeClicked, Dict.create(D.OPTION1, "okay"));
			}
			break;

		case PackDropType.DEFAULT:
			viewAlbumButton.gameObject.SetActive(true);
			viewAlbumButton.registerEventDelegate(ctaClicked);
			okayButton.gameObject.SetActive(true);
			okayButton.registerEventDelegate(closeClicked,  Dict.create(D.OPTION1, "okay"));
			break;

		case PackDropType.SET_COMPLETE:
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "set_complete",
				klass: source,
				family: packId,
				genus: "view");
			setCompleteButton.gameObject.SetActive(true);
			setCompleteButton.registerEventDelegate(ctaClicked);
			if (starPackData != null && completedSets != null && completeAlbumReward > 0)
			{
				//Special case where they get a star pack and complete the album in one pack
				//Need to show the star pack first since even though this pack happens after the album is completes we're still supposed to wipe out these cards
				//This actually needs to lead into the Star pack instead of showing the completed Sets
				Collectables.onPackDropStarPackAndAlbumComplete(starPackData, rewardsJson);
				currentPackType = PackDropType.PACK_WITH_STAR_PACK;
				starPackButton.gameObject.SetActive(true);
				starPackButton.registerEventDelegate(ctaClicked);
				setCompleteButton.gameObject.SetActive(false);
			}
			break;

		default:
			viewAlbumButton.gameObject.SetActive(true);
			viewAlbumButton.registerEventDelegate(ctaClicked);
			break;
		}

		cancelAutoClose();

		closeHandler.registerEventDelegate(closeClicked);

		packPresentation.onCardsReady += cardsLoaded;

		string packColor = CollectAPack.DEFAULT_PACK_COLOR; //Different sources might have different colors later
		CollectablePackData packData = Collectables.Instance.findPack(packId);
		if (packData != null && packData.constraints != null && packData.constraints.Length > 0)
		{
			packColor = CollectablePack.getPackColor(packData.constraints[0].minRarity, packId);
		}
		else if (CollectablePack.isWildCardPack(packId))
		{
			packColor = CollectAPack.WILD_CARD_PACK_COLOR;
			for (int i = 0; i < packLogos.Length; i++)
			{
				packLogos[i].SetActive(false);
			}
		}

		
		List<string> collectedPowerups = new List<string>();
		for (int i = 0; i < collectedCards.Count; i++)
		{
			if (PowerupBase.collectablesPowerupsMap.ContainsKey(collectedCards[i].keyName))
			{
				collectedPowerups.Add(PowerupBase.collectablesPowerupsMap[collectedCards[i].keyName]);
				foundPowerupCard = true;
			}
		}

		if (foundPowerupCard)
		{
			if (collectedCards.Count >= 5)
			{
				packPresentationParent.localPosition = new Vector3(packPresentationParent.localPosition.x + PACK_PARENT_OFFSET, packPresentationParent.localPosition.y, packPresentationParent.localPosition.z);
			}

			StartCoroutine(loadPowerupsUI(collectedPowerups));
		}

		packPresentation.preparePackSequence(collectedCards, packColor); //Handles generate the cards and setting all necessary things on them
		StartCoroutine(playIntroAnim());
		
		CollectablesAction.cardPackSeen(eventId);
	}

	private bool loadedCards = false;

	private void setupPowerups(Dict data = null)
	{
		loadedCards = true;
	}
	
	private IEnumerator loadPowerupsUI(List<string> collectedPowerups)
	{
		while (!loadedCards)
		{
			yield return null;
		}
		// attach to anchor
		GameObject instance = CommonGameObject.instantiate(powerupsPrefab, powerupsAnchor.transform) as GameObject;
		powerupUI = instance.GetComponent<PowerupInGameUI>();
		if (powerupUI != null)
		{
			powerupUI.init(Dict.create(D.MODE, PowerupInGameUI.PowerupsLocation.COLLECTIONS_DIALOG, D.DATA, collectedPowerups));
			StartCoroutine(completePowerupsDrop());
		}
	}
	
	private IEnumerator completePowerupsDrop()
	{
		yield return new WaitForSeconds(powerupDropRoutineTime);

		completedPowerupDropRoutine = true;
	}

	private IEnumerator playIntroAnim()
	{
		yield return new WaitForSeconds(Dialog.animInTime);
		if (starPackReward <= 0)
		{
			Audio.play("CardsPresentedCollections");
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(packPresentation.packAnimator, "Collection FTUE intro"));
		}
		else
		{
			Audio.play ("StarsCompletePresentedCollections");
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(packPresentation.packAnimator, "extra bonus intro"));
			SlotsPlayer.addFeatureCredits(starPackReward, "starPackDropDialog");
			Overlay.instance.top.updateCredits(false);
			yield return new WaitForSeconds(ROLLUP_WAIT_TIME); //Rollup the top overlay to the new player amount
		}
		introFinished = true;
	}

	private void cardsLoaded(Dict data = null)
	{
		StartCoroutine(openCardPack());
	}

	private IEnumerator openCardPack()
	{
		string closeAnimation = currentPackType == PackDropType.STAR_PACK && starPackReward > 0 ? CollectAPack.STAR_PACK_COLLECT_ANIM : CollectAPack.NORMAL_PACK_COLLECT_ANIM;
		while (!introFinished)
		{
			yield return null;
		}

		packPresentation.openAndRevealPack(closeAnimation);
		packPresentation.onCardAnimationsFinished += setupPowerups;
		packPresentation.onCardAnimationsFinished += animateBottomContent;
	}

	private void animateBottomContent(Dict data = null)
	{
		float delay = foundPowerupCard ? bottomPanelAnimDelay : 0.0f;
		StartCoroutine(delayAnimateBottom(delay));
	}

	private IEnumerator delayAnimateBottom(float delay = 0.0f)
	{
		yield return new WaitForSeconds(delay);
		
		if (currentPackType == PackDropType.DEFAULT || currentPackType == PackDropType.STAR_PACK)
		{
			closeHandler.gameObject.SetActive(true);
		}
		
		if (currentPackType == PackDropType.DEFAULT)
		{
			Audio.play("ViewCardsButtonAppearCollections");
		}
		else
		{
			Audio.play("CollectSetButtonAppearCollections");
		}
		packPresentation.bottomContentAnimator.Play(BOTTOM_CONTENT_INTRO_ANIM);
		if (SlotBaseGame.instance != null && SlotBaseGame.instance.hasAutoSpinsRemaining && currentPackType != PackDropType.STAR_PACK)
		{
			GameTimerRange.createWithTimeRemaining(15).registerFunction(autoCloseTimeout);
		}
	}

	private void autoCloseTimeout(Dict args = null, GameTimerRange sender = null)
	{
		if (this != null)
		{
			if (currentPackType == PackDropType.PACK_WITH_STAR_PACK || currentPackType == PackDropType.SET_COMPLETE)
			{
				ctaClicked(Dict.create(D.OPTION, "autoclose"));
			}
			else
			{
				closeClicked(Dict.create(D.OPTION, "autoclose"));
			}
		}
	}

	public void closeClicked(Dict args = null)
	{
		Audio.play("ClickXCollections");

		string statGenus = "click";
		string statFamily = "close";

		if (args != null)
		{
			statGenus = (string)args.getWithDefault(D.OPTION, "click");
			statFamily = (string)args.getWithDefault(D.OPTION1, "close"); 
		}

		packPresentation.updateDuplicateStarCount(currentAlbum);

		if (starPackReward <= 0)
		{
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "pack_award",
				klass: source,
				family: statFamily,
				genus: statGenus,
				val: numberOfCardsInPack);
		}
		else
		{
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "dupes_meter_award",
				klass: packId,
				family: statFamily,
				genus: statGenus,
				val: starPackReward * CreditsEconomy.economyMultiplier);
		}
		
		Dialog.close(this);
		completedPowerupDropRoutine = false;

	}

	private void Update()
	{
		AndroidUtil.checkBackButton(closeOnBack);
	}

	private void closeOnBack()
	{
		closeClicked();
	}
	
	public void ctaClicked(Dict args = null)
	{
		string statGenus = "click";
		if (args != null)
		{
			statGenus = (string)args.getWithDefault(D.OPTION, "click");
		}
		switch (currentPackType)
		{
		case PackDropType.PACK_WITH_STAR_PACK:
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "dupes_meter_complete",
				klass: source,
				family: "collect",
				genus: statGenus,
				val: numberOfCardsInPack);
			Audio.play("ButtonSubmitCollections");
			break;

		case PackDropType.STAR_PACK:
			//Do special coin rollup stuff
			if (starPackData != null)
			{
				StatsManager.Instance.LogCount(counterName:"dialog",
					kingdom: "hir_collection",
					phylum: "dupes_meter_complete",
					klass: source,
					family: "collect",
					genus: statGenus,
					val: numberOfCardsInPack);
				Audio.play("ButtonSubmitCollections");
			}
			else if (completedSets != null && completedSets.Count > 0)
			{
				StatsManager.Instance.LogCount(counterName:"dialog",
					kingdom: "hir_collection",
					phylum: "set_complete",
					klass: source,
					family: "collect",
					genus: statGenus);
				Audio.play("ButtonSubmitCollections");
			}
			else
			{
				StatsManager.Instance.LogCount(counterName:"dialog",
					kingdom: "hir_collection",
					phylum: "dupes_meter_award",
					klass: packId,
					family: "collection",
					genus: statGenus,
					val: starPackReward * CreditsEconomy.economyMultiplier);
				Audio.play("ButtonViewCardsCollections");
				CollectableAlbumDialog.showDialog(currentAlbum.keyName, packId, isTopOfList:true);
			}
			break;

		case PackDropType.SET_COMPLETE:
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "set_complete",
				klass: source,
				family: "collect",
				genus: statGenus);
			Audio.play("ButtonSubmitCollections");
			break;

		case PackDropType.DEFAULT:
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "pack_award",
				klass: source,
				family: "collection",
				genus: statGenus);
			
			Audio.play("ButtonViewCardsCollections");
			CollectableAlbumDialog.showDialog(currentAlbum.keyName, packId, isTopOfList:true);
			break;

		default:
			break;
		}

		packPresentation.updateDuplicateStarCount(currentAlbum);
		Dialog.close(this);
	}

	public override void close()
	{
		if (MainLobby.hirV3 != null && LobbyCarouselV3.instance != null)
		{
			MainLobby.hirV3.pageController.setScrollerActive(true);
			LobbyCarouselV3.instance.setCarousalScrollActive(true);
		}

		//Only marking the pack as seen here if we don't have any presentations to show once this pack is closed

		if ((completedSets == null || completedSets.Count == 0) && starPackData == null)
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

		if (powerupUI != null)
		{
			powerupUI.unregisterEvents();
		}

		//Show star pack once this dialog closes if we're not showing a set complete dialog
		//Set complete dialog will handle showing star pack when it closes
		if (starPackData != null && (completedSets == null || completedSets.Count <= 0))
		{
			Collectables.claimPackDropNow(starPackData, SchedulerPriority.PriorityType.IMMEDIATE);
		}
		else if (completedSets != null && completedSets.Count > 0)
		{
			CollectableSetCompleteDialog.showDialog(completedSets, eventId, completeAlbumReward, starPackData);
		}
	}

	// If we ever had multiple albums active at once, knowing what album we were going into would be nice. 
	public static void showDialog(List<CollectableCardData> collectedCards, string albumName, string eventId, string packId, string source = "", JSON starPackData = null, JSON rewardData = null, JSON previousRewardData = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW)
	{
		if (StreakSaleManager.attemptingPurchaseWithCardPack)
		{
			priority = SchedulerPriority.PriorityType.IMMEDIATE;
			StreakSaleManager.attemptingPurchaseWithCardPack = false;
		}

		bool isTopOfList = source == "weekly_race" || source == "star";
		Dict args = Dict.create(D.COLLECTABLE_CARDS, collectedCards, D.TITLE, albumName, D.EVENT_ID, eventId, D.PACKAGE_KEY, packId, D.KEY, source, D.DATA, starPackData, D.BASE_CREDITS, rewardData, D.AMOUNT, previousRewardData, D.IS_TOP_OF_LIST, isTopOfList);

		bool isShowingInGameUI = SpinPanelHIR.hir != null && SpinPanelHIR.hir.powerupsInGameUI != null;

		// hide the in game ui
		if (isShowingInGameUI)
		{
			Scheduler.addFunction(delegate(Dict dict)
			{
				//Recheck hir and powerupInGameUi for being null
				//This can take potentially take time and the panel might be destroyed
				if (SpinPanelHIR.hir != null && SpinPanelHIR.hir.powerupsInGameUI)
				{
					SpinPanelHIR.hir.powerupsInGameUI.transitionPanel(false);
				}
			}, null, SchedulerPriority.PriorityType.HIGH );
		}

		// show the dialog
		Scheduler.addDialog("collectables_pack_dropped", args, priority);

		// show the in game ui again
		if (isShowingInGameUI)
		{
			Scheduler.addFunction(delegate(Dict dict)
			{
				//Recheck hir and powerupInGameUi for being null
				//This can take potentially take time and the panel might be destroyed
				if (SpinPanelHIR.hir != null && SpinPanelHIR.hir.powerupsInGameUI)
				{
					SpinPanelHIR.hir.powerupsInGameUI.transitionPanel(true);
				}
			});
		}

	}
	
#if ZYNGA_TRAMP || UNITY_EDITOR
	public override IEnumerator automate()
	{
		while (this != null &&  Dialog.instance.currentDialog == this && !Dialog.instance.isClosing)
		{
			if (currentPackType == PackDropType.DEFAULT || currentPackType == PackDropType.STAR_PACK)
			{
				// Wait for the closeHandler object to become enabled so that it will be correctly grabbed by the standard base function
				while (!closeHandler.gameObject.activeInHierarchy)
				{
					yield return null;
				}

				yield return CommonAutomation.routineRunnerBehaviour.StartCoroutine(base.automate());
			}
			else
			{
				// These versions of the dialog might not close normally, and the auto close funcitonality
				// uses this instead, so we'll use this as well.
				ctaClicked(Dict.create(D.OPTION, "autoclose"));
			}
		}
	}
#endif
}
