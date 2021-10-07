using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

public class SevenDayWelcomeDialog :  DialogBase
{
	private const string DIALOG_ANIM_1_SOUND = "Dialog01Anim017DayWelcome";
	private const string DIALOG_ANIM_3_SOUND = "Dialog01Anim037DayWelcome";
	private const string DIALOG_ANIM_4_SOUND = "Dialog01Anim047DayWelcome";
	private const string DIALOG_ANIM_5_SOUND = "Dialog01Anim057DayWelcome";
	private const string DIALOG_ANIM_6_SOUND = "Dialog01Anim067DayWelcome";
	private const string DIALOG_OPEN_SOUND = "Dialog01Open7DayWelcome";
	private const string DIALOG_OPEN_FROM_CAROUSEL_SOUND = "CarouselDialogOpen7DayWelcome";


	[SerializeField] private GameObject panelPrefab;
	[SerializeField] private List<GameObject> coinPrefabs;
	[SerializeField] private GameObject nextRewardRoot;
	[SerializeField] private GameObject currentRewardRoot;
	[SerializeField] private TextMeshPro nextRewardText;
	[SerializeField] private TextMeshPro claimAmountText;
	[SerializeField] private TextMeshPro headerText;
	[SerializeField] private TextMeshPro day7Label;
	[SerializeField] private TextMeshPro day7ClaimAmountText;
	[SerializeField] private ClickHandler day7ClickHandler;
	[SerializeField] private List<GameObject> panelAnchors;
	[SerializeField] private Animator panelAnimator;
	[SerializeField] private List<string> collectAnimationNames;
	[SerializeField] private List<string> showAnimationNames;
	[SerializeField] private List<string> doneAnimationNames;
	[SerializeField] private ButtonHandler collectButton;
	[SerializeField] private GameObject collectButtonSheen;
	[SerializeField] private ButtonHandler closeButton;
	[SerializeField] private Color timerTextColour;
	[SerializeField] private Color defaultTextColour;


	private const float DIALOG_OUTRO_LENGTH = 4.33f;
	private const float DIALOG_DAY_7_OUTRO_LENGTH = 6.65f;

	private bool isClosing = false;
	private GameTimer rewardTimer = null;
	private bool isCollectable = false;
	private bool shouldClose = false;

	private string statsKindgom;

	public override void init()
	{
		statsKindgom = ExperimentWrapper.WelcomeJourney.isLapsedPlayer ? "welcome_back_journey" : "welcome_journey";
		isCollectable = WelcomeJourney.shouldShow();
		int activeDay = getActiveDayIndex();

		playOpenDialogAudio(isCollectable);
		constructPanels(activeDay);

		string dayFormat = Localize.text("welcome_journey_day");
		day7Label.text = string.Format(dayFormat, "7");
		if (WelcomeJourney.instance.rewardsList != null && WelcomeJourney.instance.rewardsList.Length >= 7)
		{
			day7ClaimAmountText.text = CreditsEconomy.convertCredits(WelcomeJourney.instance.rewardsList[6]);
		}

		rewardTimer = constructRewardTimer();


		//test if we can collect
		if (isCollectable)
		{
			configureForCollect(activeDay);
		}
		else
		{
			configureForView(activeDay);
		}

		StatsManager.Instance.LogCount(counterName: "dialog",
			kingdom: statsKindgom,
			genus: "view");
	}

	private void configureForView(int activeDay)
	{
		nextRewardRoot.SetActive(true);
		currentRewardRoot.SetActive(false);

		//play the animation
		if (panelAnimator != null && panelAnimator.gameObject != null && doneAnimationNames !=null && doneAnimationNames.Count > (activeDay -1))
		{
			panelAnimator.Play(doneAnimationNames[activeDay-1]);
		}

		//show the previous days claim amount
		claimAmountText.text = CreditsEconomy.convertCredits(WelcomeJourney.instance.previousClaimAmount);
		nextRewardText.text =Localize.text("welcome_journey_come_back", rewardTimer.timeRemainingFormatted);
		nextRewardText.color = timerTextColour;
		collectButton.enabled = false;
		collectButton.gameObject.SetActive(false);
		collectButtonSheen.SetActive(false);
		closeButton.registerEventDelegate(closeClicked);
	}

	private void configureForCollect(int activeDay)
	{
		nextRewardRoot.SetActive(false);
		currentRewardRoot.SetActive(true);

		//play the animation
		if (panelAnimator != null && panelAnimator.gameObject != null)
		{
			panelAnimator.Play(showAnimationNames[activeDay]);
		}

		//show current claim amount
		claimAmountText.text = CreditsEconomy.convertCredits(WelcomeJourney.instance.claimAmount);

		//Setting up all our summary sequence labels and wheel data
		collectButton.registerEventDelegate(collectClicked);
		closeButton.registerEventDelegate(collectClicked);

		if (activeDay == 6)
		{
			headerText.text = Localize.text("welcome_journey_day_7_header");
			day7ClickHandler.registerEventDelegate(collectClicked);
		}
		else
		{
			headerText.text = Localize.text("welcome_journey_day_header");
		}

		headerText.color = defaultTextColour;
	}

	private void playOpenDialogAudio(bool isCollectable)
	{
		if (isCollectable)
		{
			Audio.play(DIALOG_OPEN_SOUND);
		}
		else
		{
			Audio.play(DIALOG_OPEN_FROM_CAROUSEL_SOUND);
		}
	}

	private void constructPanels(int activeDay)
	{
		for (int i = 0; i < panelAnchors.Count; ++i)
		{
			//instantiate panel
			GameObject panelObj = CommonGameObject.instantiate(panelPrefab, panelAnchors[i].transform) as GameObject;

			if (panelObj != null)
			{
				//rename so animation lines up
				int cloneIndex = panelObj.name.IndexOf("(Clone)");
				if (cloneIndex >= 0)
				{
					panelObj.name = panelObj.name.Substring(0,cloneIndex);
				}

				WelcomeJourneyPanel panel = panelObj.GetComponent<WelcomeJourneyPanel>();
				if (panel != null)
				{
					GameObject coinDisplay = coinPrefabs != null && coinPrefabs.Count > i ? coinPrefabs[i] : null;
					int reward =
						WelcomeJourney.instance.rewardsList != null &&
						WelcomeJourney.instance.rewardsList.Length > i ?
							WelcomeJourney.instance.rewardsList[i] :
							0;

					if (activeDay == i && isCollectable)
					{
						panel.init(coinDisplay, i + 1, reward, i< WelcomeJourney.instance.claimDay, collectClicked);
					}
					else
					{
						panel.init(coinDisplay, i + 1, reward, i< WelcomeJourney.instance.claimDay, null);
					}

				}
			}
			else
			{
				Bugsnag.LeaveBreadcrumb("SevenDayWelcomeDialog::init -- Can't instantiate panel object");
				shouldClose = true;
				break;
			}
		}

		//animations need to be bound again
		if (panelAnimator != null && panelAnimator.gameObject != null)
		{
			//we've renamed and added objects, lets fix the animator
			panelAnimator.Rebind();
		}
	}

	private void Update()
	{
		if (shouldClose)
		{
			shouldClose = false;
			closeClicked();
			return;
		}

		if (!isCollectable)
		{
			nextRewardText.text = Localize.text("welcome_journey_come_back", rewardTimer.timeRemainingFormatted);
		}
	}

	private static GameTimer constructRewardTimer()
	{
		long nextClaimTime = WelcomeJourney.instance.getNextClaimTime();
		int diff = System.Convert.ToInt32(nextClaimTime - GameTimer.currentTime);
		return new GameTimer(diff);
	}


	private int getActiveDayIndex()
	{
		int activeDay = WelcomeJourney.instance.claimDay -1;
		if (activeDay < 0)
		{
			Bugsnag.LeaveBreadcrumb("evenDayWelcomeDialog::getActiveDayIndex -- welcome journey not active");
			shouldClose = true;
			activeDay = 0;

		}

		if (activeDay >= showAnimationNames.Count)
		{
			Bugsnag.LeaveBreadcrumb("SevenDayWelcomeDialog::getActiveDayIndex -- last reward already claimed");
			activeDay = showAnimationNames.Count - 1;
			shouldClose = true;
		}
		return activeDay;

	}

	private void closeClicked(Dict args = null)
	{
		Dialog.close();
	}



	private void collectClicked(Dict args = null)
	{
		if (!isClosing)
		{
			isClosing = true;
			cancelAutoClose();

			closeButton.enabled = false;
			collectButton.enabled = false;
			collectButtonSheen.SetActive(false);

			int activeDay = getActiveDayIndex();
			if (activeDay != 6)
			{
				playCollectAudio();
			}
			else
			{
				playCollectDay7Audio();
			}

			//delay dialog close for anim
			StartCoroutine(coinOutro(WelcomeJourney.instance.claimAmount));

			//claim reward
			WelcomeJourney.instance.claimReward();

			StatsManager.Instance.LogCount(counterName: "dialog",
				kingdom: statsKindgom,
				genus: "click");
		}
	}

	private void playCollectAudio()
	{
		Audio.play(DIALOG_ANIM_1_SOUND);

	}

	private void playCollectDay7Audio()
	{
		Audio.play(DIALOG_ANIM_3_SOUND);
		Audio.playWithDelay(DIALOG_ANIM_4_SOUND, 1.7f);
		Audio.playWithDelay(DIALOG_ANIM_5_SOUND, 2.6f);
		Audio.playWithDelay(DIALOG_ANIM_6_SOUND, 4.9f);
	}

	private IEnumerator coinOutro(long totalCredits)
	{
		//add the credits to the top bar
		SlotsPlayer.addCredits(totalCredits, "daily bonus", false, true, true);

		int activeDay = getActiveDayIndex();

		//animate
		if (panelAnimator != null)
		{
			string animState = collectAnimationNames[activeDay];
			panelAnimator.Play(animState);
		}


		//wait for coin trail to finish
		if (activeDay == 6)
		{
			yield return new WaitForSeconds(DIALOG_DAY_7_OUTRO_LENGTH);
		}
		else
		{

			yield return new WaitForSeconds(DIALOG_OUTRO_LENGTH);
		}


		//close
		Dialog.close();
	}




	public static bool showDialog(string motdKey = "")
	{
		// This is the path to the current background.
		Dict args = null;
		if (!string.IsNullOrEmpty(motdKey))
		{
			args = Dict.create(D.MOTD_KEY, motdKey);
		}

		if (ExperimentWrapper.WelcomeJourney.isLapsedPlayer)
		{
			CustomPlayerData.setValue("was_welcome_back_lapsed", true);
			Scheduler.addDialog("welcome_back_journey", args); //Dialogs are almost identical with just a different top banner
		}
		else
		{
			Scheduler.addDialog("welcome_journey", args);
		}
		return true;
	}



	public override void close()
	{
		//remove event handlers
		collectButton.unregisterEventDelegate(collectClicked);
		closeButton.unregisterEventDelegate(collectClicked);
		closeButton.unregisterEventDelegate(closeClicked);
		day7ClickHandler.unregisterEventDelegate(collectClicked);

		//stat log
		StatsManager.Instance.LogCount(counterName: "dialog",
			kingdom: statsKindgom,
			genus: "close");
	}
}
