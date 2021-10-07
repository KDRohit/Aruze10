using UnityEngine;
using TMPro;

public class LobbyOptionButtonSlotventurePortal : MonoBehaviour
{
	public TextMeshPro endsInLabel;

	public Renderer themedBackground;

	public GameObject jackpotMode;
	public GameObject completeMode;
	public GameObject overMode;
	
	[SerializeField] private BottomOverlayButtonToolTipController tooltipController;
	[SerializeField] private GameObject[] unlockedElements;
	
	private Material clonedMaterial;
	SlotventuresChallengeCampaign slotventureCampaign;

	[SerializeField] rewardBar jackpotBar;

	private const string PORTAL_IMAGE_PATH = "Features/Slotventures/{0}/Textures/SlotVentures_{0}";

	private void Awake()
	{
		slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as SlotventuresChallengeCampaign;

		if (slotventureCampaign != null && slotventureCampaign.lastMission != null && 
		    slotventureCampaign.lastMission.rewards != null && slotventureCampaign.lastMission.rewards.Count > 0)
		{
			// Display the jack pot. It could be a loot box, or could be coins
			if (jackpotBar != null)
			{
				jackpotBar.showJackPot(slotventureCampaign.lastMission.rewards[0]);
			}
		}
		else
		{
			for (int i = 0; i < unlockedElements.Length; i++)
			{
				if (unlockedElements[i] != null)
				{
					unlockedElements[i].SetActive(false);
				}
			}

			setupLevelLock();
		}
		AssetBundleManager.load(string.Format(PORTAL_IMAGE_PATH, SlotventuresLobby.assetData.themeName), onLoadTexture, onLoadTextureFail);

	}

	private void setupLevelLock()
	{
		EueFeatureUnlockData svUnlockData = EueFeatureUnlocks.getUnlockData("sv_challenges");
		if (svUnlockData != null)
		{
			if (svUnlockData.unlockLevel > SlotsPlayer.instance.socialMember.experienceLevel)
			{
				tooltipController.initLevelLock(svUnlockData.unlockLevel, "sv_challenges_unlock_level", svUnlockData.unlockLevel);
			}
			else
			{
				tooltipController.setLockedText(BottomOverlayButtonToolTipController.COMING_SOON_LOC_KEY);
			}
		}
	}

	private void onLoadTexture(string assetPath, Object obj, Dict data = null)
	{
		//if user has left the lobby don't do anything with this
		if (this == null || this.gameObject == null)
		{
			return;
		}
		
		//Save the material so we don't make duplicate clones
		if (clonedMaterial != null)
		{
			clonedMaterial.mainTexture = obj as Texture2D;
		}
		else if (themedBackground != null && themedBackground.material != null)
		{
			themedBackground.material.mainTexture = obj as Texture2D;
			clonedMaterial = themedBackground.material;
		}
		
	}

	private void onLoadTextureFail(string assetPath, Dict data = null)
	{
		Debug.LogError("LobbyOptionButtonSlotventurePortal::onLoadTextureFail - Could not load " + assetPath);
	}

	void OnDisable()
	{
		if (slotventureCampaign != null && slotventureCampaign.timerRange != null)
		{
			slotventureCampaign.timerRange.removeLabel(endsInLabel);
		}
	}

	private void OnDestroy()
	{
		//remove cloned material so we don't leak memory
		if (clonedMaterial != null)
		{
			Destroy(clonedMaterial);
		}
	}

	void OnEnable()
	{
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
	}
}