using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;
public class ChallengeLobbyOptionButtonSlotventure : LobbyOptionButton
{
	public TextMeshPro endsInLabel;

	public Animator animator;
	public GameObject particleEffects;
	public UITexture themedBackground;

	public GameObject jackpotMode;
	public GameObject completeMode;
	public GameObject overMode;
	SlotventuresChallengeCampaign slotventureCampaign;

	[SerializeField] private BottomOverlayButtonToolTipController tooltipController;
	[SerializeField] private GameObject[] unlockedElements;
	[SerializeField] private rewardBar jackpotBar;

	private bool levelLocked;
	private bool firstTimeView = false;
	private const string PORTAL_IMAGE_PATH = "Features/Slotventures/Slotventures Main Lobby/Textures/SlotVentures_lobbycard_{0}";
	private const string EUE_ANIMATION = "Animated";
	
	public static int pageIndex = -1;

	private void Awake()
	{
		slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as SlotventuresChallengeCampaign;
		if (themedBackground != null)
		{
			SafeSet.gameObjectActive(themedBackground.gameObject, false);
		}

		if (slotventureCampaign != null && slotventureCampaign.lastMission != null && 
		    slotventureCampaign.lastMission.rewards != null && slotventureCampaign.lastMission.rewards.Count > 0)
		{
			// Display the jack pot. It could be a loot box, or could be coins
			if (jackpotBar != null)
			{
				jackpotBar.showJackPot(slotventureCampaign.lastMission.rewards[0]);
			}
		}
		else if (unlockedElements != null)
		{
			for (int i = 0; i < unlockedElements.Length; i++)
			{
				SafeSet.gameObjectActive(unlockedElements[i], false);
			}
		}
		
		AssetBundleManager.load(this, string.Format(PORTAL_IMAGE_PATH, SlotventuresLobby.assetData.themeName), onLoadTexture, onLoadTextureFail);

		if (animator != null && ExperimentWrapper.Slotventures.isEUE)
		{
			SafeSet.gameObjectActive(particleEffects, true);
			animator.Play(EUE_ANIMATION);
		}

		EueFeatureUnlockData svUnlockData = EueFeatureUnlocks.getUnlockData("sv_challenges");
		if (svUnlockData != null)
		{
			if (svUnlockData.unlockLevel > SlotsPlayer.instance.socialMember.experienceLevel)
			{
				levelLocked = true;
				initLevelLock();
			}
			else if (svUnlockData.unlockedThisSession && !svUnlockData.unlockAnimationSeen)
			{
				firstTimeView = true;
				initLevelLock();

				FeatureUnlockTask unlockTask = new FeatureUnlockTask(Dict.create(D.OBJECT, tooltipController, D.INDEX, pageIndex, D.KEY, "sv_challenges", D.TITLE, "EUE Unlock Slotventues task"));
				pageIndex = -1;
				Scheduler.addTask(unlockTask, SchedulerPriority.PriorityType.BLOCKING);
			}
			else if (slotventureCampaign == null)
			{
				if (tooltipController != null)
				{
					tooltipController.setLockedText(BottomOverlayButtonToolTipController.COMING_SOON_LOC_KEY);	
				}
			}
		}
	}

	private void initLevelLock()
	{
		int unlockLevel = EueFeatureUnlocks.getFeatureUnlockLevel("sv_challenges");
		tooltipController.initLevelLock(unlockLevel, "sv_challenges_unlock_level", unlockLevel);
	}

	private void onLoadTexture(string assetPath, Object obj, Dict data = null)
	{
		Material themedMat = new Material(themedBackground.material.shader);
		themedMat.mainTexture = obj as Texture2D;

		themedBackground.material = themedMat;
		themedBackground.gameObject.SetActive(true);
	}

	private void onLoadTextureFail(string assetPath, Dict data = null)
	{
		Debug.LogError("LobbyOptionButtonSlotventurePortal::onLoadTextureFail - Could not load " + assetPath);
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (slotventureCampaign != null)
		{
			slotventureCampaign.timerRange.removeLabel(endsInLabel);
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();

		if (slotventureCampaign != null)
		{
			jackpotMode.SetActive(slotventureCampaign.state == ChallengeCampaign.IN_PROGRESS);
			if (slotventureCampaign.state == ChallengeCampaign.IN_PROGRESS)
			{
				jackpotMode.SetActive(true);
				endsInLabel.text = string.Format("{0} ", Localize.text("ends_in"));
				slotventureCampaign.timerRange.registerLabel(endsInLabel, keepCurrentText: true);
			}

			if (slotventureCampaign.state == ChallengeCampaign.COMPLETE)
			{
				completeMode.SetActive(true);
			}

			// Failed condition
			if (slotventureCampaign.state == ChallengeCampaign.INCOMPLETE)
			{
				overMode.SetActive(true);
			}
		}
		else if (gameObject != null && !EueFeatureUnlocks.hasFeatureUnlockData("sv_challenges"))
		{
			gameObject.SetActive(false);
		}
	}

	protected override void OnClick()
	{
		if (slotventureCampaign != null && !levelLocked)
		{
			if (firstTimeView)
			{
				firstTimeView = false;
			}
			DoSomething.now("slotventure");
		}
		else
		{
			StartCoroutine(tooltipController.playLockedTooltip());
		}
	}
}