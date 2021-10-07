using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls the top part of the Overlay for HIR SKU.
*/

public class OverlayTopHIR : OverlayTop
{
	public static OverlayTopHIR instance = null;

	// =============================
	// PUBLIC
	// =============================
	public TextMeshPro inboxCountTopTMPro;
	public UIImageButton giftsButton;
	public GameObject inboxButton;
	public GameObject maxVoltageRecentWinnerParent;
	public GameObject featuresParent;
	public Animator levelUpAnimator;
	public Animator spinAnimator;
	public GameObjectCycler objectCycler;

	[HideInInspector] public NetworkProfileOverlayButton profileButton;
	[System.NonSerialized] public LevelUpOverlay levelUpSequence = null;
	[System.NonSerialized] public Animator levelUpSequenceAnticipation = null;

	// =============================
	// PRIVATE
	// =============================
	private FacebookFriendInfo maxVoltageRecentWinnerInfo;
	private Vector3 inboxViewposition = new Vector3(0.9f, 1.0f);
	private bool playLevelUpOnLoad = false;
	private List<GameObject> featuresAddToOverlay = new List<GameObject>();
	[SerializeField] private ClickHandler creditMeterHandler;

	// =============================
	// CONST
	// =============================
	private const float PROFILE_BUTTON_WIDTH = 204;

	protected override void Awake()
	{
		base.Awake();
		instance = this;

		adjustForResolution();
		setButtonsVisibility();

		if (creditMeterHandler != null)
		{
			creditMeterHandler.registerEventDelegate(onCreditMeterClick);
		}
	}

	private void onCreditMeterClick(Dict args)
	{
		clickBuyCredits();
	}

	public void showMaxVoltageWinner(SocialMember recentWinnerMember, long credits = 0L)
	{
		if (maxVoltageRecentWinnerInfo == null)
		{
			Dict args = Dict.create(D.PLAYER, recentWinnerMember, D.AMOUNT, credits);
			// If we haven't loaded this from the bundle yet do that now.
			AssetBundleManager.load(this, "Features/Max Voltage/Prefabs/Max Voltage Recent Winner", maxVoltageWinnerLoadSuccess, maxVoltageWinnerLoadFailure, args);
		}
		else
		{
			// Otherwise just update the info.
			// If we have a game object existing, turn it on and show the winner.
			maxVoltageRecentWinnerParent.SetActive(true);
			if (recentWinnerMember != null)
			{
				maxVoltageRecentWinnerInfo.member = recentWinnerMember;
			}
			else
			{
				maxVoltageRecentWinnerInfo.nameTMPro.text = Localize.textUpper("slots_lover", "");
				maxVoltageRecentWinnerInfo.scoreTMPro.text = credits.ToString();
			}			
		}
	}

	private void maxVoltageWinnerLoadSuccess(string path, Object obj, Dict args)
	{
		GameObject prefab = obj as GameObject;
		if (prefab != null)
		{
			GameObject newWinnerBox = CommonGameObject.instantiate(prefab) as GameObject;
			if (newWinnerBox != null)
			{
				newWinnerBox.transform.parent = maxVoltageRecentWinnerParent.transform;
				newWinnerBox.transform.localPosition = Vector3.zero;
				newWinnerBox.transform.localScale = Vector3.one;
				maxVoltageRecentWinnerInfo = newWinnerBox.GetComponent<FacebookFriendInfo>();
				FacebookMember member = args.getWithDefault(D.PLAYER, null) as FacebookMember;
				long credits = (long)args.getWithDefault(D.AMOUNT, 0L);
				if (member == null)
				{
					Debug.LogErrorFormat("OverlayTopHIR.cs -- maxVoltageWinnerLoadSucces -- was not able to get a FacebookMember from the arguments, so not turning this on.");
					maxVoltageRecentWinnerParent.SetActive(false);
					return;
				}
				else
				{
					maxVoltageRecentWinnerInfo.member = member;
					maxVoltageRecentWinnerParent.SetActive(true);
				}

			}
			else
			{
				Debug.LogErrorFormat("OverlayTopHIR.cs -- maxVoltageWinnerLoadSuccess -- failed to create an object from the prefab created from path: {0}", path);
			}
		}
		else
		{
			Debug.LogErrorFormat("OverlayTopHIR.cs -- maxVoltageWinnerLoadSuccess -- returned success but failed to have an prefab at path: {0}", path);
		}
	}

	private void maxVoltageWinnerLoadFailure(string path, Dict args = null)
	{
		maxVoltageRecentWinnerParent.SetActive(false);
		Debug.LogErrorFormat("OverlayTopHIR.cs -- maxVoltageWinnerLoadFailure -- failed to load object from path: {0}", path);
	}
		   
	public void hideMaxVoltageWinner()
	{
		maxVoltageRecentWinnerParent.SetActive(false);
	}

	public void onClickPartnerPowerup(Dict args = null)
	{
		PartnerPowerupIntroDialog.showDialog();
	}

	public virtual void setButtonsVisibility()
	{
		StartCoroutine(setProfileButtonVisibility()); //Check to see if we still need to set up the profile button in case the player's profile was parsed before the overlay was loaded
	}

	public IEnumerator setProfileButtonVisibility()
	{
		if (profileButton != null && profileButton.isSetup)
		{
			// We have already initialized it so do nothing.
			yield break;
		}
		
		
		bool shouldTurnOn = NetworkProfileFeature.instance.isEnabled && SlotsPlayer.instance.socialMember.networkProfile != null;
		if (shouldTurnOn)
		{
			string path = NetworkAchievements.isEnabled ? NetworkAchievements.OVERLAY_BUTTON_PREFAB_PATH : NetworkProfileFeature.OVERLAY_BUTTON_PREFAB_PATH_V2;
			SkuResources.loadFromMegaBundleWithCallbacks(this, path, profileButtonLoadSuccess, profileButtonLoadFailure);
		}
	}

	private void profileButtonLoadSuccess(string path, Object obj, Dict args)
	{
		if (profileButton != null)
		{
			Debug.LogErrorFormat("OverlayTopHIR.cs -- profileButtonLoadSuccess -- profile button prefab loaded callback when we already have a profile button. This is being double initialized for some reason.");
			return;
		}
		
		GameObject prefab = obj as GameObject;
		if (prefab != null)
		{
			GameObject newButton = CommonGameObject.instantiate(prefab) as GameObject;

			if (newButton != null)
			{
				if (XPUI.instance != null)
				{
					newButton.transform.parent = featuresParent.transform;
					newButton.transform.localScale = Vector3.one;
					newButton.transform.localPosition = Vector3.zero;
					profileButton = newButton.GetComponent<NetworkProfileOverlayButton>();
					//We always want the player button to be the rightmost button
					//Fixes HIR-88276 sometimes the weekly race icon appears to the right
					addFeatureDisplay(newButton, PROFILE_BUTTON_WIDTH, featuresAddToOverlay.Count);
				}

				if (profileButton != null)
				{
					profileButton.init();
				}
				else
				{
					Debug.LogErrorFormat("OverlayTopHIR.cs -- profileButtonLoadSuccess -- failed to get the NetworkProfileOverlayButton script from the object, turning it off.");
					newButton.SetActive(false);
				}
			}
			else
			{
				Debug.LogErrorFormat("OverlayTopHIR.cs -- profileButtonLoadSuccess -- failed to instantiate prefab.");
			}
		}
		else
		{
			Debug.LogErrorFormat("OverlayTopHIR.cs -- profileButtonLoadSuccess -- failed to get the prefab from the Object at path: {0}", path);
		}
	}
	//The positionIndex controls the index of the featureButton in the featuresAddToOverlay List
	//This allows certain buttons like the player profile button to be the rightmost (end of the list)
	public void addFeatureDisplay(GameObject featureButton, float size, int positionIndex)
	{
		if (!featuresAddToOverlay.Contains(featureButton))
		{
			//Make sure positionIndex isnt higher than the count which would cause a IndexOutOfRange exception
			positionIndex = Mathf.Clamp(positionIndex, 0, featuresAddToOverlay.Count);
			featuresAddToOverlay.Insert( positionIndex, featureButton);
			XPUI.instance.onItemAddedToOverlay(size);

			for (int i = 1; i < featuresAddToOverlay.Count; ++i)
			{
				Vector3 localPos = featuresAddToOverlay[i].transform.localPosition;
				featuresAddToOverlay[i].transform.localPosition = new Vector3(localPos.x + size, localPos.y, localPos.z);
			}
		}
	}

	public void removeFeatureDisplay(GameObject featureButton, float size)
	{
		if (featuresAddToOverlay.Contains(featureButton))
		{
			int removedIndex = featuresAddToOverlay.IndexOf(featureButton);
			featuresAddToOverlay.Remove(featureButton);
			XPUI.instance.onItemRemovedFromOverlay(size);
			//We want to shift everything that was to the right of the removed element
			for (int i = removedIndex; i < featuresAddToOverlay.Count; ++i)
			{
				Vector3 localPos = featuresAddToOverlay[i].transform.localPosition;
				featuresAddToOverlay[i].transform.localPosition = new Vector3(localPos.x - size, localPos.y, localPos.z);
			}
		}
	}
	
	private void profileButtonLoadFailure(string path, Dict args = null)
	{
		Debug.LogErrorFormat("OverlayTopHIR.cs -- profileButtonLoadFailure -- failed to load the overlay button from prefab path: {0}", path);
	}

	public override void adjustForResolution()
	{
		base.adjustForResolution();
		xpUI.updateXP();	// Make sure the xp meter is the correct width, since it gets messed up if changing resolution while the meter is tweening.
		//overlayOrganizer.organizeButtons();
	}

	public override void setVIPInfo()
	{
		base.setVIPInfo();

		if (profileButton != null)
		{
			profileButton.refreshMember(SlotsPlayer.instance.socialMember);
		}
	}

	public Vector3 getInboxScreenPostion()
	{
		Camera overlayCamera = NGUITools.FindCameraForLayer(gameObject.layer);
		if (overlayCamera == null)
		{
			return inboxViewposition;
		}
		return overlayCamera.WorldToViewportPoint(inboxButton.transform.position);
	}

	public void showLevelUpAnimation(long amount, int vipPoints, int levelOverride = -1)
	{
#if !ZYNGA_PRODUCTION
		if (DevGUIMenuTools.disableFeatures)
		{
			return;
		}
#endif

		int levelToShow = SlotsPlayer.instance.socialMember.experienceLevel;
#if !ZYNGA_PRODUCTION		
		if (Debug.isDebugBuild)
		{
			if (levelOverride > 0)
			{
				//ensure fake data exists
				string levelData = "{\"level\":\"" + levelOverride + "\",\"required_xp\":\"25000\",\"bonus_amount\":\"1000\",\"bonus_vip_points\":\"1\",\"max_bet\":\"500\"}";
				JSON levelJSON = new JSON(levelData);
				JSON[] levelArray = new JSON[] { levelJSON };
				ExperienceLevelData.populateAll(levelArray);
				levelToShow = levelOverride;
			}	
		}
#endif		
		
		ExperienceLevelData currentLevel = ExperienceLevelData.find(levelToShow);
		if (!ExperimentWrapper.RepriceLevelUpSequence.isInExperiment)
		{
			if (levelUpAnimator != null && xpUI.currentState != null && xpUI.currentState.levelLabel != null)
			{
				if (currentLevel != null)
				{
					StartCoroutine(CommonAnimation.playAnimAndWait(levelUpAnimator, "LevelUp"));
				}
			}
		}
		else
		{
			AssetBundleManager.downloadAndCacheBundle("main_snd_motd"); //Need to have this bundle ready for seperate sounds
			if (levelUpSequence != null && (SlotBaseGame.instance == null || !SlotBaseGame.instance.isGameBusy))
			{
				levelUpSequence.startLevelUp();
			}
			else if (levelUpSequence == null)
			{
				playLevelUpOnLoad = true;
			}
		}

		//Show the inflation dialog if this level up triggered an increase, or show the teaser if the next level will increase it
		if (SlotsPlayer.instance.currentBuyPageInflationPercentIncrease > 0)
		{
			LevelUpInflationDialog.showDialog(SlotsPlayer.instance.socialMember.experienceLevel, true);
		}
		else if (SlotsPlayer.instance.nextBuyPageInflationPercentIncrease > 0)
		{
			LevelUpInflationDialog.showDialog(SlotsPlayer.instance.socialMember.experienceLevel, false);
		}
	}

	public void playLevelUpSequence(bool isIncreasingInflation, int newLevel)
	{
		if (levelUpSequence == null)
		{
			Dict args = Dict.create(D.NEW_LEVEL, newLevel, D.MODE, isIncreasingInflation);
			GameObject levelUpObj = SkuResources.loadFromMegaBundleOrResource(LevelUpOverlay.LEVEL_UP_OVERLAY_PATH);
			if (levelUpObj != null)
			{
				levelUpLoadSuccess(LevelUpOverlay.LEVEL_UP_OVERLAY_PATH, levelUpObj, args);
			}
			else
			{
				ExperienceLevelData curLevel = ExperienceLevelData.find(newLevel);
				long creditsAwarded = curLevel.bonusAmt + curLevel.levelUpBonusAmount;
				SlotsPlayer.addCredits(creditsAwarded, "level up", false);
			}
		}
		else
		{
			levelUpSequence.init(isIncreasingInflation, newLevel);
		}
	}

	private void levelUpLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (assetPath == LevelUpOverlay.LEVEL_UP_OVERLAY_PATH)
		{
			int newLevel = (int) data.getWithDefault(D.NEW_LEVEL, 0);
			bool isIncreasingInflation = (bool) data.getWithDefault(D.MODE, false);
			GameObject lvlUpObj = NGUITools.AddChild(this.gameObject, obj as GameObject);
			levelUpSequence = lvlUpObj.GetComponent<LevelUpOverlay>();
			levelUpSequence.init(isIncreasingInflation, newLevel);

			if (SlotBaseGame.instance != null && SlotBaseGame.instance.isGameBusy) //Only load the anticipation if we're not instantly showing the level up
			{
				levelUpLoadSuccess(LevelUpOverlay.LEVEL_UP_ANTICIPATION_PATH, SkuResources.loadFromMegaBundleOrResource(LevelUpOverlay.LEVEL_UP_ANTICIPATION_PATH));
			}

			if (playLevelUpOnLoad && (SlotBaseGame.instance == null || !SlotBaseGame.instance.isGameBusy))
			{
				levelUpSequence.startLevelUp();
				playLevelUpOnLoad = false;
			}
		}
		else if (assetPath == LevelUpOverlay.LEVEL_UP_ANTICIPATION_PATH)
		{
			GameObject levelUpAnticipation = NGUITools.AddChild(levelUpSequence.gameObject, obj as GameObject);
			levelUpSequenceAnticipation = levelUpAnticipation.GetComponent<Animator>();
			levelUpAnticipation.transform.position = xpUI.starObject.transform.position;
		}

	}

	private void levelUpLoadFailed(string assetPath, Dict data = null)
	{
		
	}

	private IEnumerator setInBoxEffect(bool isRainyDayJarActive)
	{

		// Update Ballcount



		// Fade Effect
		iTween.ValueTo(gameObject,
			iTween.Hash(
				"from", 0f,
				"to", 0.5f,
				"time", 1.5f,
				"easetype", iTween.EaseType.linear,
				"onupdate", "setFadeinPanel"
			)
		);

		yield return null; 
	}

	public override int updateInboxCount()
	{
		int inboxCount = base.updateInboxCount();

		// we are getting null exceptions here, so be paranoid about null-checks
		if (inboxCountTopTMPro != null && inboxCountTopTMPro.transform.parent != null)
		{
			if (inboxCount <= 0)
			{
				inboxCountTopTMPro.transform.parent.gameObject.SetActive(false);
				if (objectCycler != null && objectCycler.isRunning)
				{
					objectCycler.stopCycling();
				}
			}
			else
			{
				inboxCountTopTMPro.transform.parent.gameObject.SetActive(true);
				string countText = CommonText.formatNumber(inboxCount);
				inboxCountTopTMPro.text = countText;

				if (objectCycler != null && InboxInventory.findItemByCommand<InboxEliteCommand>() != null)
				{
					if (!objectCycler.isRunning)
					{
						objectCycler.startCycling(false);
					}
				}
				else if (objectCycler != null && objectCycler.isRunning)
				{
					objectCycler.stopCycling();
					base.updateInboxCount();
					
				}
			}
		}

		return inboxCount;
	}

//	public void setFadeinPanel(float alpha)
//	{
//		if (fadePanel != null && fadePanel.GetComponent<MeshRenderer>().material.HasProperty ("color")) 
//		{
//			fadePanel.GetComponent<MeshRenderer>().material.color = new Color (0.5f, 0.5f, 0.5f, alpha); 
//		}
//	}

	public override void showLobbyButton()
	{
		lobbyButton.gameObject.SetActive(true);
		//overlayOrganizer.organizeButtons();
	}

	public override void hideLobbyButton()
	{
		// Null checking because of Crittercism report.
		// https://app.crittercism.com/developers/crash-details/525d7eb9d0d8f716a9000006/93b0ef17765b2e69fedbfceb5102e3ebd2a385af911237192987515f
		if (lobbyButton.transform != null && lobbyButton.transform.parent != null &&
			lobbyButton.transform.parent.gameObject != null)
		{
			lobbyButton.gameObject.SetActive(false);
		}
		//overlayOrganizer.organizeButtons();
	}

	// NGUI button callback.
	private void vipButtonClicked()
	{
		VIPDialog.showDialog();
	}
}
