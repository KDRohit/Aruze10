using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class TicketTumblerDialog : DialogBase 
{
	public enum DialogState
	{
		INTRO = 0,
		COLLECT = 1,
		TICKETING = 2,
	};

#region static_variables
	// Public Static Variables
	public static TicketTumblerDialog instance;
	public static string errMessage;
	public static bool hasBeenViewed = false;
#endregion

	public TextMeshPro ticketCreditsLabel;
	public TextMeshPro ticketsPerPackLabelEntry;
	public TextMeshPro collectButtonText;
	public GameObject activeTimerText;
	public GameObject expiredTimerText;
	public GameObject collectAwardObject;

	public GameObject drawingParent;
	public GameObject skipButton;

	public List<TicketTumblerLabelHelper> stateLabels = new List<TicketTumblerLabelHelper>();		// indexed by state

	// Completed Winner Variables
	private List<SocialMember> winners;		
	private long[] winnersServerVipIndexes;		
	private long[] winnersServerVipPrizes;	
	public List<FacebookFriendInfo> completedWinners;

	public AnimationListController.AnimationInformationList introAnimationList;
	public AnimationListController.AnimationInformationList winnerAnimations;
	public AnimationListController.AnimationInformationList loserAnimations;
	public AnimationListController.AnimationInformationList creditsRecievedAnims;
	public AnimationListController.AnimationInformationList normalTicketAnimations;
	public AnimationListController.AnimationInformationList finalTicketAnimations;
	public AnimationListController.AnimationInformationList coinAnimation;
	public AnimationListController.AnimationInformationList collectionActiveAnimations;
	public AnimationListController.AnimationInformationList collectionInactiveAnimations;

	public List<GameObject> collectionsUI;		
	public List<GameObject> nonCollectionsUI;		
	public List<GameObject> collectionsUIDrawing;		
	public Renderer[] packLogos;

	public ButtonHandler spinButton;
	public ButtonHandler collectButton;
	public ButtonHandler collectCloseButton;
	public ButtonHandler newFeaturesButton;
	public ButtonHandler newFeaturesButtonNoCollectables;

	public UIAnchor closeButtonAnchor;

	public List<GameObject> ticketStack = new List<GameObject>();

	public JSON instanceEventData;
	public int instanceTicketCount;
	public int instanceTicketKey;
	public long instanceTicketPrize;
	public bool instanceHasCollectables;
	public long instancePrizeAmount;
	public int vipPrizeTier;

	private SkippableWait skipWait = new SkippableWait();
	private SkippableWait skipWaitCollect = new SkippableWait();

	public static bool showDialogOnInfoData;
	private const float FINAL_TICKET_ANIMATION_DURATION = 2.25f;
	private const float FINAL_TICKET_ANIMATION_LABEL_POP_DURATION = .75f;
	private const float MIN_TICKET_SCENE_HOLD = 3.1f;

	private string backgroundMusicRestoreKey;
	private bool isMissedWinnerEvent = false;
	private bool isLogoLoaded = false;
	private bool isClosing = false;
	private DialogState state;
	private  bool isWinner;
	private  bool waitingForResults;

	[SerializeField] private GameObject prevWinnerPrefab;
	[SerializeField] private GameObject prevWinnerParent;
	private List<FacebookFriendInfo> prevWinnerInfos = new List<FacebookFriendInfo>();
	private List<VIPIconHandler> prevWinnerVIPIconHandlers = new List<VIPIconHandler>();
	private List<TicketTumblerPreviousWinner> previousWinnerHandles = new List<TicketTumblerPreviousWinner>();
	private int currentWinnerIndex = 0;

	public override void init()
	{
		backgroundMusicRestoreKey = Audio.defaultMusicKey;

		instance = this;

		markAsSeen();

		showDialogOnInfoData = false;

		registerButtonHandlers();

		processDialogArgs();

		setState(state);

		showCollectionUI(instanceHasCollectables);

		switch (state)
		{
			case DialogState.INTRO:
				StartCoroutine(manageIntroSequence());
				break;
			case DialogState.COLLECT:
				StartCoroutine(manageCollectSequence());
				break;
			case DialogState.TICKETING:
				StartCoroutine(manageTicketSequence());
				break;
		}
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		if (closeButtonAnchor != null)
		{
			closeButtonAnchor.reposition();
		}
	}

	private IEnumerator manageIntroSequence()
	{
		// setup gem icons, icon loader gets setup in awake now fixing need for race condition hacks
		for (int i = 0; i < TicketTumblerFeature.instance.previousWinners.Count; i++)
		{
			//SocialMember prevWinnerInfo = TicketTumblerFeature.instance.previousWinners[i];
			GameObject prevWinnerObject = NGUITools.AddChild(prevWinnerParent, prevWinnerPrefab);
			if (i > 0)
			{
				CommonGameObject.alphaGameObject(prevWinnerObject, 0.0f);
			}
			TicketTumblerPreviousWinner prevWinnerHandle = prevWinnerObject.GetComponent<TicketTumblerPreviousWinner>();
			prevWinnerInfos.Add(prevWinnerHandle.prevWinnerInfo);
			prevWinnerVIPIconHandlers.Add(prevWinnerHandle.prevWinnerVIPIconHandler);
			previousWinnerHandles.Add(prevWinnerHandle);
		}
		
		membersToUI(
			TicketTumblerFeature.instance.previousWinners,
			TicketTumblerFeature.instance.previousWinnersVipIndexes,
			prevWinnerInfos,
			prevWinnerVIPIconHandlers,
			SocialMember.ScoreType.TICKET_TUMBLER_PREVIOUS_WIN
		);
		foreach (FacebookFriendInfo info in prevWinnerInfos)
		{
			SafeSet.componentGameObjectActive(info, false);
		}

		currentWinnerIndex = 0;
		SafeSet.componentGameObjectActive(prevWinnerInfos[0], true);

		// yield so that touch input gets a chance to clear its taps, such as from the carousel card, so we don't skip the spin
		// yes we have to wait 2 frames worth
		yield return null;
		yield return null;

		stateLabels[(int)state].setVipTier(VIPLevel.find(vipPrizeTier));

		Audio.switchMusicKeyImmediate("Dialog01OpenTicketTumbler");

		if (TicketTumblerFeature.instance.previousWinners.Count > 1)
		{
			cycleWinners();
		}
	}

	private void cycleWinners()
	{
		StartCoroutine(waitThenSwapWinners());
	}

	private IEnumerator waitThenSwapWinners()
	{
		int nextWinnerIndex = currentWinnerIndex + 1;
		if (nextWinnerIndex >= previousWinnerHandles.Count)
		{
			nextWinnerIndex = 0;
		}
		
		yield return new WaitForSeconds(2.0f);

		if (this == null || this.gameObject == null)
		{
			yield break; //Break early in case the dialog was closed while we we're waiting to transition to the next winner
		}
		
		SafeSet.componentGameObjectActive(prevWinnerInfos[nextWinnerIndex], true); //Turn on next winner which should be faded out
		StartCoroutine(CommonGameObject.fadeGameObjectTo(previousWinnerHandles[currentWinnerIndex].gameObject, 1.0f, 0.0f, 1f, false)); //Fade out current winner
		yield return  StartCoroutine(CommonGameObject.fadeGameObjectTo(previousWinnerHandles[nextWinnerIndex].gameObject, 0.0f, 1.0f, 1f, false)); //Fade in next winner
		SafeSet.componentGameObjectActive(prevWinnerInfos[currentWinnerIndex], false); //Turn off current winner, current not showing since its faded out
		currentWinnerIndex = nextWinnerIndex;
		cycleWinners();
	}
	
	

	// this is the part where tickets animate into the barrel, one by one
	private IEnumerator manageTicketSequence()
	{
		showCollectionUI(instanceHasCollectables);

		ticketsPerPackLabelEntry.text = ticketsPerPackLabelEntry.text.Replace("10", TicketTumblerFeature.instance.numTicketsForCollectablesPack.ToString());		

		if (instanceTicketCount == 0 || TicketTumblerFeature.instance.lastTicketingKey == instanceTicketKey)
		{
			setState(DialogState.COLLECT);
			StartCoroutine(manageCollectSequence());
			yield break;
		}

		TicketTumblerFeature.instance.lastTicketingKey = instanceTicketKey;

		Audio.switchMusicKeyImmediate("Dialog02OpenTicketTumbler");

		if (TicketTumblerFeature.instance.numTicketsForCollectablesPack >= 0)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(collectionInactiveAnimations));
		}

		int startTickets = instanceTicketCount;
		stateLabels[(int)state].setTicketCountLabelsForRollup(startTickets, instanceTicketCount);
		TicketTumblerFeature.instance.stackTickets(ticketStack, startTickets);

		// wait  before we let them skip out of getting results
		// yes we have to wait 2 frames worth
		yield return null;
		yield return null;
		skipWaitCollect.reset();
		yield return StartCoroutine(skipWaitCollect.wait(1.0f));

		bool doSkip = false;
		int ticketsEnteredInBarrel = 0;
		while (!doSkip && state == DialogState.TICKETING)
		{
			if (startTickets > 0)
			{
				startTickets--;
				if (startTickets == 0)
				{
					StartCoroutine(AnimationListController.playListOfAnimationInformation(finalTicketAnimations));
					yield return StartCoroutine(skipWaitCollect.wait(FINAL_TICKET_ANIMATION_DURATION));
					stateLabels[(int)state].setTicketCountLabelsForRollup(startTickets, instanceTicketCount);
					yield return StartCoroutine(skipWaitCollect.wait(FINAL_TICKET_ANIMATION_LABEL_POP_DURATION));
				}
				else
				{
					StartCoroutine(AnimationListController.playListOfAnimationInformation(normalTicketAnimations));
					yield return StartCoroutine(skipWaitCollect.wait(.5f));
					stateLabels[(int)state].setTicketCountLabelsForRollup(startTickets, instanceTicketCount);
				}

				TicketTumblerFeature.instance.stackTickets(ticketStack, startTickets);

				yield return StartCoroutine(skipWaitCollect.wait(.2f));

				if (ticketsEnteredInBarrel == 0)
				{
					StartCoroutine(SlotUtils.rollup(0, instanceTicketPrize, ticketCreditsLabel,
						 true, (float)(startTickets+1) * 1.05f, true, false,
						"Dialog02CollectLoopTicketTumbler", "Dialog02CollectEndTicketTumbler"));
				}

				ticketsEnteredInBarrel++;

				if (ticketsEnteredInBarrel == TicketTumblerFeature.instance.numTicketsForCollectablesPack)
				{
					StartCoroutine(AnimationListController.playListOfAnimationInformation(collectionActiveAnimations));
				}

				// hide the collection pack if they didn't get enough tickets
				if (startTickets == 0 && ticketsEnteredInBarrel < TicketTumblerFeature.instance.numTicketsForCollectablesPack)
				{
					showCollectionUI(false);
				}
			}
			else if (!waitingForResults)
			{
				yield return StartCoroutine(skipWaitCollect.wait(MIN_TICKET_SCENE_HOLD));
				doSkip = true;
			}

			doSkip = (TouchInput.didTap || doSkip);

			if (doSkip)
			{
				skipButton.SetActive(false);
			}

			yield return null;
		}

		setState(DialogState.COLLECT);
		StartCoroutine(manageCollectSequence());
	}

	// This is where the drum spins and the winners are drawn, called for both losers and winners since losers collect credits for tickets.
	private IEnumerator manageCollectSequence()
	{
		if (!isWinner)
		{
			showCollectionUI(false);
		}

		if (instanceTicketPrize == 0)
		{
			collectButtonText.text = Localize.textUpper("ok");
		}

		// wait  before we let them skip out of getting results
		// yes we have to wait 2 frames worth
		yield return null;
		yield return null;

		bool doSkip = false;

		while (waitingForResults && !doSkip)
		{
			doSkip = (TouchInput.didTap || doSkip);
			yield return null;
		}

		TicketTumblerLabelHelper helper = stateLabels[(int)state];

		if (!doSkip)
		{
			if (isWinner)
			{
				StatsManager.Instance.LogCount(counterName:"dialog", kingdom:"lottery_day_results", phylum:"winner", genus:"view");
				StartCoroutine(AnimationListController.playListOfAnimationInformation(winnerAnimations));
			}
			else
			{
				StatsManager.Instance.LogCount(counterName:"dialog", kingdom:"lottery_day_results", phylum:"loser", genus:"view");
				winners = TicketTumblerFeature.instance.makeWinnerList(instanceEventData, out winnersServerVipIndexes, "winners", SocialMember.ScoreType.TICKET_TUMBLER_PREVIOUS_WIN);
				winnersServerVipPrizes = TicketTumblerFeature.instance.makeTierPrizeList(instanceEventData, "winners");
				membersToUI(winners, winnersServerVipIndexes, completedWinners, null, SocialMember.ScoreType.TICKET_TUMBLER_PREVIOUS_WIN, true);
				StartCoroutine(AnimationListController.playListOfAnimationInformation(loserAnimations));
			}
			skipWaitCollect.reset();
			yield return StartCoroutine(skipWaitCollect.wait(5.0f));
			Audio.switchMusicKeyImmediate("Dialog01OpenTicketTumbler");
		}
		else
		{
			Dialog.close();
		}
	}

	public void setInstanceEventData(JSON data)
	{
		if (data != null)
		{
			instanceEventData = data;
			instanceTicketCount = instanceEventData.getInt("ticket_count", 0);
			instanceTicketKey = instanceEventData.getInt("key", 0);
			instanceTicketPrize = instanceEventData.getLong("total_ticket_prize", 0L);
			instanceHasCollectables = instanceEventData.getBool("collectibles_eligibility", false);
			instancePrizeAmount = instanceEventData.getLong("prize", 0L);
			vipPrizeTier = instanceEventData.getInt("vip", 0);
		}
		else
		{
			Debug.LogError("null event data recieved for Ticket Tumbler dialog instance.");
		}
	}

	private void processDialogArgs()
	{
		state = dialogArgs != null && dialogArgs.containsKey(D.DATA) ? (DialogState)dialogArgs[D.DATA] : state = DialogState.INTRO;
		setInstanceEventData(dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as JSON);

		if (state == DialogState.COLLECT)
		{
			isMissedWinnerEvent = (bool)dialogArgs.getWithDefault(D.VALUE, false);
			isWinner = true;
			state = DialogState.TICKETING;
		}
	}

	public void transitionToTicketingFromIntroState()
	{
		if (state == TicketTumblerDialog.DialogState.INTRO)
		{
			setState(TicketTumblerDialog.DialogState.TICKETING);
			StartCoroutine(manageTicketSequence());
		}
	}



	public void transitionToCollectionStateAsWinner(JSON data)
	{
		if (state != DialogState.COLLECT)
		{
			waitingForResults = false;
			isWinner = true;
		    setInstanceEventData(data);

		
		    transitionToTicketingFromIntroState();
		}
		else
		{
			showWinnerDialog(data, false);
		}	
	}

	public void updateLabelsForExpiredTimer()
	{
		activeTimerText.SetActive(instanceTicketCount == 0);
		expiredTimerText.SetActive(instanceTicketCount != 0);	
	}

	private void setState(DialogState newState)
	{
		state = newState;

		foreach(TicketTumblerLabelHelper helper in stateLabels)
		{
			helper.gameObject.SetActive(false);
		}

		if (state == DialogState.COLLECT)
		{
			// move so it can be part of the animation transition
			stateLabels[(int)DialogState.TICKETING].gameObject.SetActive(true);
			stateLabels[(int)DialogState.TICKETING].gameObject.transform.parent = drawingParent.transform;
		}

		stateLabels[(int)state].gameObject.SetActive(true);

		initLabels();
	}

	private void showCollectionUI(bool show)
	{
		if (show && !isLogoLoaded)
		{
			SkuResources.loadFromMegaBundleWithCallbacks(this, CollectablePack.GENERIC_LOGO_PATH, logoLoadedSuccess, bundleLoadFail);
		}

		SafeSet.gameObjectListActive(collectionsUI, show);
		SafeSet.gameObjectListActive(nonCollectionsUI, !show);

		if (state == DialogState.INTRO)
		{
			SafeSet.gameObjectListActive(collectionsUIDrawing, false);	// this never shows in intro state
		}
		else
		{
			SafeSet.gameObjectListActive(collectionsUIDrawing, show);
		}
	}

	private void bundleLoadFail(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load set image at " + assetPath);
	}

	private void logoLoadedSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this == null || this.gameObject == null)
		{
			return; //Return early if the bundle load callback is happening after we've been destroyed
		}
		
		isLogoLoaded = true;
		if (packLogos.Length > 0)
		{
			for (int i = 0; i < packLogos.Length; i++)
			{
				Material material = new Material(packLogos[i].material.shader);
				material.mainTexture = obj as Texture2D;
				packLogos[i].material = material;
				packLogos[i].gameObject.SetActive(true);
			}
		}	
	}

	private void initLabels()
	{
		TicketTumblerLabelHelper helper = stateLabels[(int)state];

		helper.setCredits(instanceTicketPrize + instancePrizeAmount);

		int numTix = instanceTicketCount;

		if (state == DialogState.COLLECT && instanceEventData != null)
		{
			numTix = instanceEventData.getInt("ticket_count", 0);
		}

		helper.setNumTickets(numTix);
		helper.setRoundLength(TicketTumblerFeature.instance.roundLength);

		helper.setVipTier(VIPLevel.find(vipPrizeTier));
		helper.setEventTimeLeft(TicketTumblerFeature.instance.featureTimer);
		helper.setRoundTimeLeft(TicketTumblerFeature.instance.roundEventTimer);

		TicketTumblerFeature.instance.stackTickets(ticketStack, numTix);
	}

	private void registerButtonHandlers()
	{
		spinButton.registerEventDelegate(onCloseButtonClicked);
		collectButton.registerEventDelegate(onCloseButtonClicked);
		collectCloseButton.registerEventDelegate(onClickCloseWithCoinAnimation);
		newFeaturesButton.registerEventDelegate(onClickInfo);
		newFeaturesButtonNoCollectables.registerEventDelegate(onClickInfo);
	}

	private void onClickInfo(Dict args = null)
	{
		Dialog.close();
		TicketTumblerMOTD.showDialogFromFeature(SchedulerPriority.PriorityType.IMMEDIATE);
	}

	private void membersToUI(List<SocialMember> members, long[] vipIndexes, List<FacebookFriendInfo> facebookInfos, List<VIPIconHandler> iconHandlers, SocialMember.ScoreType scoreType, bool sortByVipLevel = false)
	{
		if (members == null)
		{
			Debug.LogError("Null members list passed to membersToUI");
			return;
		}
		int i = 0;
		int winnerIndex = members.Count - 1;
		
		for (i = 0; i < members.Count; i++)
		{
			if (winnerIndex >= 0 && winnerIndex < members.Count)
			{
				int sortIndex = i;
				if (sortByVipLevel)
				{
					// this is used for UI gameObjects that are arranged by vip level in arrays
					if (vipIndexes != null)
					{
						sortIndex = (int)vipIndexes[winnerIndex];
					}
				}
				
				if (iconHandlers != null && sortIndex < iconHandlers.Count && iconHandlers[sortIndex] != null)
				{
					iconHandlers[sortIndex].setLevel(vipPrizeTier);
				}
				
				if (members[winnerIndex] != null && facebookInfos != null && sortIndex < facebookInfos.Count && facebookInfos[sortIndex] != null)
				{
					facebookInfos[sortIndex].member = members[winnerIndex];
				}
				winnerIndex--;
				}
		}

		int prizeIndex = 0;
		foreach (FacebookFriendInfo fInfo in facebookInfos)
		{
			if (fInfo.member == null)
			{
				if (winnersServerVipPrizes != null && prizeIndex < winnersServerVipPrizes.Length)
				{
					fInfo.scoreTMPro.text = CreditsEconomy.multiplyAndFormatNumberTextSuffix(winnersServerVipPrizes[prizeIndex], 2, true,  false);
					fInfo.nameTMPro.text = "No Participant";
				}
			}
			else
			{
				fInfo.scoreTMPro.text = CreditsEconomy.multiplyAndFormatNumberTextSuffix(fInfo.member.getScore(scoreType), 2, true,  false);
				fInfo.scoreTMPro = null;
			}

			prizeIndex++;

		}
	}

	private void addCredits(long amount)
	{
		SlotsPlayer.addFeatureCredits(amount, "ticketTumblerDialog");
	}

	private void startCollectBanner()
	{
		StartCoroutine(AnimationListController.playListOfAnimationInformation(creditsRecievedAnims));
	}

	private void onClickCloseWithCoinAnimation(Dict args = null)
	{
		if (!isClosing)
		{
			isClosing = true;
			StartCoroutine(coinOutro());
		}
	}

	private IEnumerator coinOutro()
	{
		if (instanceTicketPrize > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(coinAnimation));
		}
		Dialog.close();
		yield return null;
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		if (!isClosing)
		{
			Dialog.close();
		}
	}

	public override void close()
	{
		switch (state)
		{
			case DialogState.INTRO:
				StatsManager.Instance.LogCount(counterName:"dialog", kingdom:"lottery_day_motd", family:"play_now", genus:"click");
				Audio.play("Dialog01Close01TicketTumbler");
				break;
			case DialogState.TICKETING:
			case DialogState.COLLECT:
				long credits = 0;

				credits += instanceTicketPrize;

				if (isWinner)
				{
					StatsManager.Instance.LogCount(counterName:"dialog", kingdom:"lottery_day_results", phylum:"winner", family:"collect", genus:"click");
					if (instanceEventData != null)
					{
						credits += instanceEventData.getLong("prize", 0L);
					}
					isWinner = false;
				}
				else
				{
					StatsManager.Instance.LogCount(counterName:"dialog", kingdom:"lottery_day_results", phylum:"loser", family:"okay", genus:"click");
				}
				// remove our desync protection fudge credits which protects us from desyncs until we collected
				if (credits > 0)
				{
					addCredits(credits);
				}

				break;
		}
		
		TicketTumblerFeature.instance.dropPackCheck();

		instance = null;

		TicketTumblerFeature.instance.statusButtonCheck();

		Audio.switchMusicKeyImmediate(backgroundMusicRestoreKey);
	}

	void Update()
	{
		AndroidUtil.checkBackButton(onCloseButtonClicked);
	}

	public static void showTicketingDialog(JSON data, int key)
	{
		Dict args = Dict.create(
				D.IS_LOBBY_ONLY_DIALOG, false,
				D.DATA, DialogState.TICKETING,
				D.EVENT_ID, key,
				D.CUSTOM_INPUT, data
			);
		long prizeAmount = data.getLong("total_ticket_prize", 0L);

		Scheduler.addDialog("ticket_tumbler", args);
	}

	public static void showWinnerDialog(JSON data, bool isOldWin)
	{
		// while this dialog is getting created auto spin will get a few frames and may spin one more time causing a desync
		// adjustFudgeCredits which will be subtracted from shouldHaveCredits so it can't happen and we will add it back in when they collect
		long prizeAmount = data.getLong("prize", 0L);

		prizeAmount += data.getLong("total_ticket_prize", 0L);


		Dict args = Dict.create(
				D.IS_LOBBY_ONLY_DIALOG, false,
				D.DATA, DialogState.COLLECT,
				D.CUSTOM_INPUT, data,
				D.VALUE, isOldWin
			);

		Scheduler.addDialog("ticket_tumbler", args);
	}

	// this showDialog should only be called from MOTDDialogTicketTumbler
	public static bool showDialog(string motdKey = "", Dict args = null, bool mustShow = false)
	{
		// if called from motdframework mustShow will be false, check if we already saw it this session don't show again this way
		if ((!mustShow && hasBeenViewed) || TicketTumblerFeature.instance.eventData == null)
		{
			// this only gets called from motdframework, so if we already saw it this session don't show again this way
			return false;
		}
		hasBeenViewed = true;

		if (args == null)
		{
			{
				args = Dict.create(
					D.CUSTOM_INPUT, adjustDataTicketCounts(),
					D.IS_LOBBY_ONLY_DIALOG, false,
					D.MOTD_KEY, motdKey,
					D.DATA, DialogState.INTRO
				);
			}
		}
		Scheduler.addDialog("ticket_tumbler", args);
		return true;
	}

	private static JSON adjustDataTicketCounts()
	{
		JSON data = null;
		if (TicketTumblerFeature.instance.eventData != null)
		{
			data = TicketTumblerFeature.instance.eventData;
			data.jsonDict["ticket_count"] = TicketTumblerFeature.instance.ticketCount.ToString();
			data.jsonDict["total_ticket_prize"] = TicketTumblerFeature.instance.ticketPrizeAmount.ToString();	
		}

		return data;
	}

	public static void markAsSeen()
	{
		hasBeenViewed = true;
		if (MOTDFramework.seenThisSession != null)
		{
			MOTDFramework.seenThisSession.Add("ticket_tumbler");	// so it doesn't get added to sorted motd list
		}
		PlayerAction.markMotdSeen("ticket_tumbler", true);
	}
}
