using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using FeatureOrchestrator;
using UnityEngine;

using TMPro;

public class OverlayTopHIRv2 : OverlayTopHIR
{
	public GameObject homeButtonParent;
	public BuyCoinButtonManager buyButtonManager;

	public ImageButtonHandler homeButtonHandler;
	public ImageButtonHandler settingsButtonHandler;
	public ImageButtonHandler backButtonHandler;
	public ImageButtonHandler inboxButtonHandler;
	
	//Elite stuff
	public GameObject lobbyButtonElite;
	public ImageButtonHandler eliteHomeButtonHandler;
	public ImageButtonHandler eliteSettingsButtonHandler;
	public ImageButtonHandler eliteBackButtonHandler;
	public ImageButtonHandler eliteInboxButtonHandler;

	public GameObject inboxButtonSpecial;
	public Animator inboxCoinEffect;
	public ImageButtonHandler inboxButtonHandlerSpecial;

	[HideInInspector] public WeeklyRaceOverlayObject weeklyRaceOverlay;
	[HideInInspector] public ButtonHandler weeklyRaceButton;
	public Transform coinAnchor;
	
    [SerializeField] private GameObject loyaltyLoungeAlertParent;

    [SerializeField] private UIAnchor genericProgressMeterAnchor;
    private GameObject genericProgressMeterObject;

    [SerializeField] private GameObject levelLottoMeterFXParent;
    private GameObject levelLottoMeterFX;
    
	private const float WEEKLY_RACE_BUTTON_WIDTH = 200f;
	private bool prefabLoading = false;

	protected override void Awake()
	{
		base.Awake();

		buyButtonManager.setButtonType();

		homeButtonHandler.registerEventDelegate(homeButtonClick);
		settingsButtonHandler.registerEventDelegate(settingButtonClick);
		inboxButtonHandler.registerEventDelegate(inboxButtonClick);
		eliteInboxButtonHandler.registerEventDelegate(inboxButtonClick);
		inboxButtonHandlerSpecial.registerEventDelegate(inboxButtonClick);
		backButtonHandler.registerEventDelegate(backButtonClick);
		loyaltyLoungeAlertParent.SetActive(false); // Default this to off.

		if (eliteBackButtonHandler != null)
		{
			eliteBackButtonHandler.registerEventDelegate(backButtonClick);
		}

		if (eliteHomeButtonHandler != null)
		{
			eliteHomeButtonHandler.registerEventDelegate(homeButtonClick);
		}

		if (eliteSettingsButtonHandler != null)
		{
			eliteSettingsButtonHandler.registerEventDelegate(settingButtonClick);
		}

		if (PowerupsManager.isPowerupsEnabled)
		{
			PowerupsManager.addEventHandler(onPowerupActivated);

			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_FREE_SPINS_KEY))
			{
				onPowerupActivated(PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_FREE_SPINS_KEY));
			}
		}
	}

	private void onPowerupActivated(PowerupBase powerup)
	{
		if (powerup.name == PowerupBase.POWER_UP_FREE_SPINS_KEY)
		{
			inboxButton.SetActive(false);
			inboxButtonSpecial.SetActive(true);
			powerup.runningTimer.registerFunction(onPowerupExpired);
		}
	}

	private void onPowerupExpired(Dict args = null, GameTimerRange originalTimer = null)
	{
		inboxButton.SetActive(true);
		inboxButtonSpecial.SetActive(false);
	}

	public override void setupSaleNotification()
	{
		buyButtonManager.setButtonType();
	}

	public void onEliteRebate()
	{
		if (inboxCoinEffect != null)
		{
			inboxCoinEffect.Play("anim");
		}
	}
	
#if !ZYNGA_PRODUCTION
	public void devForceWeeklyRace()
	{
		if (weeklyRaceOverlay == null)
		{
			if (!prefabLoading)
			{
				prefabLoading = true;
				//This will implicitly load the weekly_race bundle and ALL of its contents since
				//isSkippingMapping is false by default.
				string buttonPath = "Assets/Data/HIR/Bundles/Initialization/Features/Weekly Race/Player Rank - Top Overlay Button.prefab";
				SkuResources.loadFromMegaBundleWithCallbacks(this, buttonPath, onWeeklyRaceButtonLoadSuccess, weeklyRaceLoadFailure, Dict.create(D.ACTIVE, true));	
			}
		}
		else if (!weeklyRaceOverlay.isSetup)
		{
			showWeeklyRaceButton();
		}
		else
		{
			weeklyRaceOverlay.refresh();
		}
	}
#endif

	public void setupWeeklyRace()
	{
		if (ExperimentWrapper.WeeklyRace.isInExperiment && WeeklyRaceDirector.hasRace)
		{
			if (weeklyRaceOverlay == null)
			{
				if (!prefabLoading)
				{
					prefabLoading = true;
					//This will implicitly load the weekly_race bundle and ALL of its contents since
					//isSkippingMapping is false by default.
					string buttonPath = "Assets/Data/HIR/Bundles/Initialization/Features/Weekly Race/Player Rank - Top Overlay Button.prefab";
					SkuResources.loadFromMegaBundleWithCallbacks(this, buttonPath, onWeeklyRaceButtonLoadSuccess, weeklyRaceLoadFailure);	
				}
			}
			else if (!weeklyRaceOverlay.isSetup)
			{
				if (shouldShowWeeklyRaceButton())
				{
					showWeeklyRaceButton();
				}
				else
				{
					hideWeeklyRaceButton();	
				}
			}
		}
		else
		{
			hideWeeklyRaceButton();
		}
	}

	private void onWeeklyRace(Dict args = null)
	{
		weeklyRaceOverlay.onClick();
		if (WeeklyRaceDirector.currentRace != null)
		{
			StatsWeeklyRace.logOverlayClick(WeeklyRaceDirector.currentRace.division, WeeklyRaceDirector.currentRace.competitionRank);
		}
	}

	private void onWeeklyRaceButtonLoadSuccess(string path, Object obj, Dict args)
	{
		prefabLoading = false;
		
		GameObject prefab = obj as GameObject;
		GameObject assetObject = CommonGameObject.instantiate(prefab) as GameObject;

		weeklyRaceOverlay = assetObject.GetComponent<WeeklyRaceOverlayObject>();
		weeklyRaceOverlay.transform.parent = featuresParent.transform;
		weeklyRaceOverlay.transform.localScale = Vector3.one;
		weeklyRaceOverlay.transform.localPosition = Vector3.zero;

		// grab the button from there
		weeklyRaceButton = assetObject.GetComponent<ButtonHandler>();
		weeklyRaceButton.registerEventDelegate(onWeeklyRace);

		string playerRankPath = "Assets/Data/HIR/Bundles/Initialization/Features/Weekly Race/Player Rank - Top Overlay.prefab";
		string badgePath = "Assets/Data/HIR/Bundles/Initialization/Features/Weekly Race/Weekly Race Rank Badge Item.prefab";
		SkuResources.loadFromMegaBundleWithCallbacks(this, badgePath, weeklyRaceLoadSuccess, weeklyRaceLoadFailure);
		SkuResources.loadFromMegaBundleWithCallbacks(this, playerRankPath, weeklyRaceLoadSuccess, weeklyRaceLoadFailure);

		bool shouldShow = shouldShowWeeklyRaceButton() || (args != null && (bool)args.getWithDefault(D.ACTIVE, false));
		if (shouldShow)
		{
			showWeeklyRaceButton();
		}
		else
		{
			hideWeeklyRaceButton();	
		}
	}

	public void weeklyRaceLoadSuccess(string path, Object obj, Dict args)
	{
		GameObject prefab = obj as GameObject;
		GameObject assetObject = CommonGameObject.instantiate(prefab) as GameObject;
		
		if (path.Contains("Badge"))
		{			
			assetObject.transform.parent = weeklyRaceOverlay.divisionBadgeParent.transform;
			assetObject.transform.localPosition = Vector3.zero;
			assetObject.transform.localScale = Vector3.one;

			weeklyRaceOverlay.divisionBadge = CommonGameObject.findChild(assetObject, "Rank Badge Sprite").GetComponent<UISprite>();
			weeklyRaceOverlay.divisionLabel = CommonGameObject.findChild(assetObject, "Rank Badge Numeral Sprite").GetComponent<UISprite>();
		}
		else
		{
			assetObject.transform.parent = weeklyRaceOverlay.rankParent.transform;
			assetObject.transform.localPosition = Vector3.zero;
			assetObject.transform.localScale = Vector3.one;

			weeklyRaceOverlay.playerRank = CommonGameObject.findChild(assetObject, "Rank Label").GetComponent<TextMeshPro>();
			weeklyRaceOverlay.rankArrow = CommonGameObject.findChild(assetObject, "Zone Arrow Sprite").GetComponent<UISprite>();
		}

		Scheduler.addFunction(weeklyRaceOverlay.refresh);
	}

	public void weeklyRaceLoadFailure(string path, Dict args = null)
	{
		prefabLoading = false;
		Debug.LogErrorFormat("OverlayTopHIR.cs -- weeklyRaceLoadFailure -- failed to load the overlay button from prefab path: {0}", path);
	}	

	public override void adjustForResolution()
	{
		base.adjustForResolution();
		//overlayOrganizerRightSide.organizeButtons();
	}

	public override void setButtons(bool isEnabled)
	{
		if (backButtonHandler != null)
		{
			backButtonHandler.enabled = isEnabled;
		}

		if (eliteBackButtonHandler != null)
		{
			eliteBackButtonHandler.enabled = isEnabled;
		}
	}	

	public override void showLobbyButton()
	{
		if (gameObject == null)
		{
			Bugsnag.LeaveBreadcrumb("GameObject is null when setting the lobby button");
			return;
		}

		homeButtonParent.SetActive(false);

		if (EliteManager.hasActivePass && lobbyButtonElite != null)
		{
			lobbyButtonElite.SetActive(true);
		}

		base.showLobbyButton();
		//overlayOrganizerRightSide.organizeButtons();
	}

	public override void hideLobbyButton()
	{
		homeButtonParent.SetActive(true);

		if (EliteManager.hasActivePass && lobbyButtonElite != null)
		{
			lobbyButtonElite.SetActive(false);
		}

		base.hideLobbyButton();
		//overlayOrganizerRightSide.organizeButtons();
	}

	public void showWeeklyRaceButton()
	{
		if (weeklyRaceOverlay != null)
		{
			weeklyRaceOverlay.gameObject.SetActive(true);
			addFeatureDisplay(weeklyRaceOverlay.gameObject, WEEKLY_RACE_BUTTON_WIDTH, 0);
		}
	}

	public void hideWeeklyRaceButton()
	{
		if (weeklyRaceOverlay != null)
		{
			weeklyRaceOverlay.gameObject.SetActive(false);
			removeFeatureDisplay(weeklyRaceOverlay.gameObject, WEEKLY_RACE_BUTTON_WIDTH);
		}
	}

	public void showLoyaltyLoungeBadge()
	{
		loyaltyLoungeAlertParent.SetActive(true);
	}

	
	public GameObject addGenericXPProgressBar(GameObject meterObject)
	{
		genericProgressMeterObject = NGUITools.AddChild(genericProgressMeterAnchor.gameObject, meterObject);
		genericProgressMeterAnchor.reposition();
		return genericProgressMeterObject;
	}

	public GameObject addLevelLottoFXObject(GameObject fxObject)
	{
		levelLottoMeterFX = NGUITools.AddChild(levelLottoMeterFXParent, fxObject);
		return levelLottoMeterFX;
	}

	public void removeGenericXPProgressBar(bool playAnim = false)
	{
		if (levelLottoMeterFX != null)
		{
			Destroy(levelLottoMeterFX);
			levelLottoMeterFX = null;
		}

		if (genericProgressMeterObject != null)
		{
			LottoBlastProgressComponentView progressComponentView = genericProgressMeterObject.GetComponent<LottoBlastProgressComponentView>();
			progressComponentView.complete(playAnim);
		}
	}

	private void homeButtonClick(Dict args)
	{
		if (MainLobby.hirV3 != null && MainLobby.hirV3.pageController != null)
		{
			StatsManager.Instance.LogCount("top_nav", "home", "", "", "", "click");			
			MainLobby.hirV3.resetToFirstPage();
		}
		else
		{
			clickLobbyButton();
		}

		if (!shouldShowWeeklyRaceButton())
		{
			hideWeeklyRaceButton();	
		}
	}	

	private void settingButtonClick(Dict args)
	{
		loyaltyLoungeAlertParent.SetActive(false);
		settingsClicked();
	}

	public void onLobbyAwake()
	{
		if (shouldShowWeeklyRaceButton())
		{
			showWeeklyRaceButton();
		}
		else
		{
			hideWeeklyRaceButton();
		}
	}

	//If Rich Pass or Elite are active we cannot show the weekly race button in the bottom overlay
	//So we want to show it in the top overlay
	private bool shouldShowWeeklyRaceButton()
	{
		return ExperimentWrapper.EUEFeatureUnlocks.isInExperiment || (CampaignDirector.richPass != null && CampaignDirector.richPass.isActive || EliteManager.isActive);
	}

	private void backButtonClick(Dict args)
	{
		onClickLobbyButton();
		if (!shouldShowWeeklyRaceButton())
		{
			hideWeeklyRaceButton();	
		}
	}

	private void inboxButtonClick(Dict args)
	{
		inboxTopClicked();
	}

	public override void unregisterEvents()
	{
		base.unregisterEvents();
		PowerupsManager.removeEventHandler(onPowerupActivated);
	}
}
