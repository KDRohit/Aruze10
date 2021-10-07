using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainLobbyBottomOverlayV4 : MainLobbyBottomOverlay , IResetGame
{
	private const string FIVE_BUTTON_LAYOUT_ANIM = "five button";
	private const string SIX_BUTTON_LAYOUT_ANIM = "six button";
	private const string FIVE_BUTTON_LAYOUT_STATE = "button_config_5";
	private const string SIX_BUTTON_LAYOUT_STATE = "button_config_6";
	public new static MainLobbyBottomOverlayV4 instance;

	[SerializeField] private Animator panelAnimator;

	[SerializeField] private GameObject gridParent;
	[SerializeField] private UICenteredGrid grid;
	[SerializeField] private ObjectSwapper swapper;
	[SerializeField] private ObjectSwapper panelSwapper;

	[SerializeField] private GameObject weeklyRaceButtonPrefab;
	[SerializeField] private GameObject vipButtonPrefab;
	[SerializeField] private GameObject friendsButtonPrefab;
	[SerializeField] private GameObject maxVoltageButtonPrefab;
	[SerializeField] private GameObject collectionsButtonPrefab;
	[SerializeField] private GameObject bonusButtonPrefab;
	[SerializeField] private GameObject richPassButtonPrefab;
	[SerializeField] private GameObject elitePassButtonPrefab;
	[SerializeField] private GameObject virtualPetButtonPrefab;

	private bool initialized;
	private bool animationUpdatePending;
	private bool collectionsShowing;
	private BottomOverlayCollectionsButton collectionsButtonInstance;
	private VIPRoomBottomButton vipButtonInstance;
	private GenericLobbyPortalV4 maxVoltageButtonInstance;
	private RichPassBottomButton richPassButtonInstance;

	// =============================
	// CONST
	// =============================
	private const float TWEEN_POSITION = -700;
	private const string BUTTON_CONFIG_6 = "button_config_6";
	private const string BUTTON_CONFIG_5 = "button_config_5";

	public bool shouldRepositionGrid { get; set; }

	protected override void init()
	{
		if (!initialized)
		{
			base.init();
			Collectables.registerCollectionEndHandler(onCollectionEnd);
			if (panelAnimator != null)
			{
				if (Collectables.isActive())
				{
					collectionsShowing = true;
					panelAnimator.Play(SIX_BUTTON_LAYOUT_ANIM);
				}
				else
				{
					collectionsShowing = false;
					panelAnimator.Play(FIVE_BUTTON_LAYOUT_ANIM);
				}
			}
			initialized = true;
		}

		if (EliteManager.hasActivePass && !EliteManager.showLobbyTransition)
		{
			enableElite();
		}

		if (EliteManager.isActive || ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
		{
			initializeElitePass();
		}
		
		if (Collectables.isActive() || ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
		{
			initializeCollections();
		}
	}

	public void enableElite()
	{
		if (panelSwapper != null)
		{
			panelSwapper.setState("elite");
		}
	}

	public void disableElite()
	{
		if (panelSwapper != null)
		{
			panelSwapper.setState("default");
		}
	}

	public void transitionOut(float time)
	{
		iTween.MoveTo(gameObject, iTween.Hash("y", TWEEN_POSITION, "time", time, "islocal", true, "delay", 0.5f));
	}

	public void transitionIn(float time)
	{
		iTween.MoveTo(gameObject, iTween.Hash("y", 0, "time", time, "islocal", true));
	}

	public Transform getCollectionsAnchorTransform()
	{
		
		return collectionsButtonInstance != null ? collectionsButtonInstance.transform : null;
	}
	
	private void Awake()
	{
		instance = this;
		init();
	}

	protected override void Update()
	{
		base.Update();

		if (shouldRepositionGrid)
		{
			repositionGrid();
			shouldRepositionGrid = false;
		}
	}

	public override void refreshUI()
	{
		base.refreshUI();
		if (vipButtonInstance != null)
		{
			vipButtonInstance.refreshNewTag();
		}

		if (maxVoltageButtonInstance != null)
		{
			maxVoltageButtonInstance.refreshNewTag();
		}

		if (richPassButtonInstance != null)
		{
			if ((CampaignDirector.richPass == null || !CampaignDirector.richPass.isActive) && !ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
			{
				Destroy(richPassButtonInstance.gameObject);
			}
			else
			{
				richPassButtonInstance.initNewRewardsAlert();
			}
		}

		repositionGrid();
	}

	public override void repositionGrid()
	{
		if (gameObject == null)
		{
			return;
		}
		
		grid.reposition();
		if (swapper != null)
		{
			swapper.setState(grid.transform.childCount <= 5 ? FIVE_BUTTON_LAYOUT_STATE : SIX_BUTTON_LAYOUT_STATE);
		}
	}

	private void updateCollectionsVisibility(bool collectionsActive)
	{
		if (animationUpdatePending)
		{
			return;
		}

		animationUpdatePending = true;
		StartCoroutine(waitAndUpdateLayout(collectionsActive));
	}

	private IEnumerator waitAndUpdateLayout(bool collectionsActive)
	{
		//wait one frame to ensure existing animation is finished
		yield return null;

		animationUpdatePending = false;

		if (panelAnimator == null || panelAnimator.gameObject == null)
		{
			yield break;
		}

		if (collectionsActive && !collectionsShowing)
		{
			panelAnimator.Play(SIX_BUTTON_LAYOUT_ANIM);
			collectionsShowing = true;
			if (collectionsButtonInstance == null)
			{
				initializeCollections();
			}
			else
			{
				initNewCardsAlert();
			}
		}
		else if (!collectionsActive && collectionsShowing)
		{
			panelAnimator.Play(FIVE_BUTTON_LAYOUT_ANIM);
			collectionsShowing = false;
		}
	}

	public override void initializeRaces()
	{
		base.initializeRaces();

		//create the weekly button
		GameObject obj = NGUITools.AddChild(gridParent, weeklyRaceButtonPrefab);
		repositionGrid();
	}

	protected override void initializeDailyBonus()
	{
		base.initializeDailyBonus();
		NGUITools.AddChild(gridParent, bonusButtonPrefab);
		repositionGrid();
	}

	protected override void initializeCollections()
	{
		GameObject go = NGUITools.AddChild(gridParent, collectionsButtonPrefab);
		collectionsButtonInstance = go.GetComponent<BottomOverlayCollectionsButton>();

		if (collectionsButtonInstance == null)
		{
			Destroy(go);
		}
		
		repositionGrid();
	}
	
	protected override void initializeRichPass()
	{
		base.initializeRichPass();
		if ((CampaignDirector.richPass != null && CampaignDirector.richPass.isActive) || ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
		{
			GameObject obj = NGUITools.AddChild(gridParent, richPassButtonPrefab);
			richPassButtonInstance = obj.GetComponent<RichPassBottomButton>();
			//offset z depth of button handler
			obj.transform.localPosition = new Vector3(0, 0, -20);	
		}
		repositionGrid();
	}

	protected void initializeElitePass()
	{
		GameObject obj = NGUITools.AddChild(gridParent, elitePassButtonPrefab);
		//offset z depth of button handler
		obj.transform.localPosition = new Vector3(0, 0, -20);

		repositionGrid();
	}

	public override void initNewCardsAlert()
	{
		base.initNewCardsAlert();
		if (collectionsButtonInstance != null)
		{
			collectionsButtonInstance.initNewCardsAlert();
		}
	}
	
	public override void initNewRichPassAlert()
	{
		base.initNewRichPassAlert();
		if (richPassButtonInstance != null)
		{
			richPassButtonInstance.initNewRewardsAlert();
		}
	}

	protected override void initializeVIPRoom()
	{
		GameObject button = NGUITools.AddChild(gridParent, vipButtonPrefab);
		if (button != null)
		{
			vipButtonInstance = button.GetComponent<VIPRoomBottomButton>();
		}
		repositionGrid();
	}

	protected override void initializeFriends()
    {
		base.initializeFriends();
		NGUITools.AddChild(gridParent, friendsButtonPrefab);
		repositionGrid();
    }

	protected override void initializeVirtualPet()
	{
		base.initializeVirtualPet();
		NGUITools.AddChild(gridParent, virtualPetButtonPrefab);
		repositionGrid();
	}

	protected override void initializeMaxVoltage()
	{
		if ((LoLaLobby.maxVoltage != null) || MaxVoltageLobbyHIR.isBeingLazilyLoaded)
		{
			GameObject button = NGUITools.AddChild(gridParent, maxVoltageButtonPrefab);

			GenericLobbyPortalV4 roomCard = button.GetComponent<GenericLobbyPortalV4>();

			if (roomCard != null)
			{
				roomCard.actionId = "max_voltage_lobby";
				roomCard.setup(SlotsPlayer.instance.socialMember.experienceLevel < Glb.MAX_VOLTAGE_MIN_LEVEL, false, false);

				if (roomCard.lockLevelLabel != null)
				{
					roomCard.lockLevelLabel.text = Glb.MAX_VOLTAGE_MIN_LEVEL.ToString();
				}
			}
			else
			{
				Debug.LogError("Could not initialize max voltage -- object not found");
			}
		}
		repositionGrid();
	}
	
	protected override bool shouldShowWeeklyRace()
	{
		return !ExperimentWrapper.EUEFeatureUnlocks.isInExperiment && !EliteManager.isActive && (CampaignDirector.richPass == null || !CampaignDirector.richPass.isActive);
	}

	public void onCollectionBundleFinished()
	{
		updateCollectionsVisibility(true);
	}

	private void onCollectionEnd(object sender, System.EventArgs e)
	{
		updateCollectionsVisibility(false);
	}

	public static void resetStaticClassData()
	{
		instance = null;
	}

	private void OnDestroy()
	{
		Collectables.unregisterCollectionEndHandler(onCollectionEnd);

		if (DailyBonusButton.instance != null)
		{
			DailyBonusButton.instance.cleanup();
		}
		
		BottomOverlayButton.globalList.Clear();
	}


	public static void cleanup()
	{
		if (instance != null)
		{
			Destroy(instance.gameObject);
			instance = null;
		}
	}
}
