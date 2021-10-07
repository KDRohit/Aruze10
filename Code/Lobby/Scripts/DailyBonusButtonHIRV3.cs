using UnityEngine;
using Com.HitItRich.Feature.VirtualPets;
using TMPro;

/*
HIR subclass for the daily bonus button.
*/

public class DailyBonusButtonHIRV3 : DailyBonusButton
{
	// Hyperspeed objects.
	public Animator animator;

	public GameObject hyperSpeedParent;
	public ButtonHandler dealButtonClickHandler;
	public UIMeterNGUI normalMeter;
	public bool invertMeter;

	[SerializeField] private UISprite meterSprite;
	[SerializeField] private GameObject watchToEarnParent;
	[SerializeField] private TextMeshPro watchToEarnCoinText;
	[SerializeField] private GameObject petOverlayParent;

	private bool isHyperSpeedMode = false;
	private bool hasBeenSetup = false;

	private const string METER_HYPERSPEED_SPRITE = "Daily Bonus Meter Blue Stretchy";
	private const string METER_READY_SPRITE = "Daily Bonus Meter Green Stretchy";
	private const string METER_COOLDOWN_SPRITE = "Daily Bonus Meter Purple Stretchy";
	private const string ON_ANIMATION = "on";
	private const string ON_WITH_PET_ANIMATION = "on-pet";
	private const string OFF_ANIMATION = "inactive";

	protected override void Awake()
	{
		base.Awake();

		//WatchToEarn.init(WatchToEarn.REWARD_VIDEO);
		// Default this to off in case it was left on in the prefab.
		hyperSpeedParent.SetActive(false); 

		if (dealButtonClickHandler != null)
		{
			dealButtonClickHandler.registerEventDelegate(dealMeterClick);
		}

		if (SlotsPlayer.instance.dailyBonusTimer != null)
		{
			if (DailyBonusReducedTimeEvent.isActive)
			{
				normalMeter.maximumValue = Data.liveData.getInt("REDUCED_DAILY_BONUS_TIME_LENGTH", 0) * 60;
			}
			else
			{
				normalMeter.maximumValue = SlotsPlayer.instance.dailyBonusDuration * 60;
			}
		}

		if (watchToEarnParent != null)
		{
			if (WatchToEarn.isEnabled)
			{
				// Turn on watch to earn.
				watchToEarnParent.SetActive(true);
				watchToEarnCoinText.text = CreditsEconomy.convertCredits(WatchToEarn.rewardAmount);
			}
			else
			{
				//Turn it off.
				watchToEarnParent.SetActive(false);
			}
		}

		checkForPetCollect();

		if (VirtualPetsFeature.instance != null)
		{
			VirtualPetsFeature.instance.registerForHyperStatusChange(checkForPetCollect);
		}

		if (UserActivityManager.instance != null)
		{
			UserActivityManager.instance.registerForIdleEvent(checkForPetCollect);
		}
	}

	public void checkForPetCollect()
	{
		checkForPetCollect(VirtualPetsFeature.instance != null && VirtualPetsFeature.instance.isHyper);
	}
	private void checkForPetCollect(bool isHyper)
	{
		//isHyper is ignored, only needed to match callback delegate
		if (petOverlay != null && !VirtualPetsFeature.canPetCollectBonus)
		{
			Destroy(petOverlay.gameObject);
			petOverlay = null;
		}
		else if (petOverlay == null && VirtualPetsFeature.canPetCollectBonus)
		{
			loadPetCollectOverlay();
		}
	}

	/// Resets the button to the non-ready state.
	public override void setActive(bool isActive)
	{
		base.setActive(isActive);
	}
	
	
	private void loadPetCollectOverlay()
	{
		AssetBundleManager.load(VirtualPetsFeature.PET_COLLECT_PREFAB_PATH, onLoadPetUI, onLoadAssetFailure, isSkippingMapping:true, fileExtension:".prefab");
	}

	private void onLoadPetUI(string assetPath, object loadedObj, Dict data = null)
	{
		GameObject collectOverlayObj = NGUITools.AddChild(petOverlayParent != null ? petOverlayParent.transform : this.gameObject.transform, loadedObj as GameObject);
		if (collectOverlayObj == null || VirtualPetsFeature.instance == null || !VirtualPetsFeature.instance.isEnabled)
		{
			Debug.LogError("Virtual Pet not active or prefab broken");
			if (collectOverlayObj != null)
			{
				Destroy(collectOverlayObj);    
			}
		}
		else
		{
			Vector3 oldPos = collectOverlayObj.transform.localPosition;
			collectOverlayObj.transform.localPosition = new Vector3(oldPos.x, oldPos.y, oldPos.z -10);
			petOverlay = collectOverlayObj.GetComponent<VirtualPetsCollectButtonOverlay>();
			petOverlay.onAnimationCompleteEvent.AddListener(onPetCollectFinished);
			SafeSet.gameObjectActive(petOverlayParent, true);
			animator.Play(ON_WITH_PET_ANIMATION);
		}
	}

	public void onPetCollectFinished()
	{
		//remove listener
		petOverlay.onAnimationCompleteEvent.RemoveListener(onPetCollectFinished);
		
		//destroy the pet overlay
		if (petOverlay.gameObject != null)
		{
			Destroy(petOverlay.gameObject);	
		}
		
		//reset animation
		// If we are ready to collect, show the collect now meter
		if (SlotsPlayer.instance.dailyBonusTimer.isExpired)
		{
			animator.Play(ON_ANIMATION);
		}
		else
		{
			animator.Play(OFF_ANIMATION);
		}
	}

	private static void onLoadAssetFailure(string assetPath, Dict data = null)
	{
		Debug.LogError(string.Format("Failed to load asset at {0}", assetPath));
	}

	private void dealMeterClick(Dict args = null)
	{
		//check watch to earn
		if (watchToEarnParent != null && watchToEarnParent.activeSelf)
		{
			StatsManager.Instance.LogCount(
			counterName: "lobby",
			kingdom: "watch_to_earn",
			phylum: "",
			klass: "",
			family: "",
			genus: "click");
			WatchToEarn.watchVideo("lobby", true);
		}
		else if (petOverlay != null)
		{
			petOverlay.onClick(null);
		}
		else if (SlotsPlayer.instance != null && SlotsPlayer.instance.dailyBonusTimer != null && SlotsPlayer.instance.dailyBonusTimer.isExpired)
		{
			collectNowClicked();
		}
		else
		{
			InboxDialog.showDialog();
		}
	}

	protected override void Update ()
	{
		//don't run the update if we're marked for destruction
		if (this.gameObject == null)
		{
			return;
		}
		
		base.Update();
		if (SlotsPlayer.instance.dailyBonusTimer == null)
		{
			// If somehow the timer is null bounce outta here.
			return;
		}
		
		// Do HyperSpeed checks here.
		if (DailyBonusReducedTimeEvent.isActive && (!isHyperSpeedMode || !hasBeenSetup))
		{
			// If it is not on and should be, enable hyperspeed mode.
			isHyperSpeedMode = true;
			meterSprite.spriteName = METER_HYPERSPEED_SPRITE;
			hyperSpeedParent.SetActive(true);
		}
		else if (!DailyBonusReducedTimeEvent.isActive && isHyperSpeedMode)
		{
			// Or if it is on, and the event is over, turn it off.
			isHyperSpeedMode = false;
			hyperSpeedParent.SetActive(false);
		}

		// If we are ready to collect, show the collect now meter
		if (SlotsPlayer.instance.dailyBonusTimer.isExpired && (!collectNowParent.activeSelf || !hasBeenSetup))
		{
			// If we are ready to collect, and the object is not currently active,
			// then turn on collect now mode.
			readyInParent.SetActive(false);
			collectNowParent.SetActive(true);
			timerLabel.text = Localize.textUpper("collect_now");

			if (MainLobbyBottomOverlay.instance != null)
			{
				MainLobbyBottomOverlay.instance.refreshUI();
			}

			if (petOverlay != null)
			{
				animator.Play(ON_WITH_PET_ANIMATION);
			}
			else
			{
				animator.Play(ON_ANIMATION);	
			}
			
			if (!isHyperSpeedMode)
			{
				meterSprite.spriteName = METER_READY_SPRITE;
			}
		}
		else if (!SlotsPlayer.instance.dailyBonusTimer.isExpired && (!readyInParent.activeSelf || !hasBeenSetup))
		{
			readyInParent.SetActive(true);
			collectNowParent.SetActive(false);
			if (MainLobbyBottomOverlay.instance != null)
			{
				MainLobbyBottomOverlay.instance.refreshUI();
			}

			animator.Play(OFF_ANIMATION);
			
			if (isHyperSpeedMode)
			{
				if (!hyperSpeedParent.activeSelf || readyInParent.activeSelf)
				{
					readyInParent.SetActive(true);
					collectNowParent.SetActive(false);
					hyperSpeedParent.SetActive(true);
					if (MainLobbyBottomOverlay.instance != null)
					{
						MainLobbyBottomOverlay.instance.refreshUI();
					}
				}
			}
			else
			{
				if (!readyInParent.activeSelf || hyperSpeedParent.activeSelf)
				{
					readyInParent.SetActive(true);
					collectNowParent.SetActive(false);
					hyperSpeedParent.SetActive(false);
					if (MainLobbyBottomOverlay.instance != null)
					{
						MainLobbyBottomOverlay.instance.refreshUI();
					}
				}
			}
			timerLabel.text = getTimeRemainingText();
			meterSprite.spriteName = METER_COOLDOWN_SPRITE;
		}
		timerLabel.text = getTimeRemainingText();		

		long currentMeterValue = normalMeter.maximumValue - SlotsPlayer.instance.dailyBonusTimer.timeRemaining;
		if (invertMeter)
		{
			normalMeter.currentValue = normalMeter.maximumValue - currentMeterValue;
		}
		else
		{
			normalMeter.currentValue = currentMeterValue;	
		}
		

		if (watchToEarnParent != null)
		{
			// MCC -- We really should change this to be an event that gets fired.
			bool isDailyBonusActive = SlotsPlayer.instance.dailyBonusTimer != null && SlotsPlayer.instance.dailyBonusTimer.isExpired;
			if (WatchToEarn.isEnabled && !isDailyBonusActive)
			{
				if (!watchToEarnParent.activeSelf)
				{
					// Turn on watch to earn if its not already active.
					watchToEarnParent.SetActive(true);
				}
				if (watchToEarnCoinText.text != CreditsEconomy.convertCredits(WatchToEarn.rewardAmount))
				{
					watchToEarnCoinText.text = CreditsEconomy.convertCredits(WatchToEarn.rewardAmount);
				}

			}
			else if (watchToEarnParent.activeSelf)
			{
				// If we shouldn't be showing it and the gameobject is on, then turn it off.
				watchToEarnParent.SetActive(false);
			}
		}
		

		// Otheriwse show the cooldown meter.
		hasBeenSetup = true;
	}

	private void OnDestroy()
	{
		if (VirtualPetsFeature.instance != null)
		{
			VirtualPetsFeature.instance.deregisterForHyperStatusChange(checkForPetCollect);	
		}

		if (UserActivityManager.hasInstance())
		{
			UserActivityManager.instance.unregisterForIdleEvent(checkForPetCollect);
		}
	}

	private string getTimeRemainingText()
	{
		return Localize.textUpper("more_in_{0}", SlotsPlayer.instance.dailyBonusTimer.timeRemainingFormatted);
	}

	public void dailyBonusReady()
	{
	}

	public void dailyBonusOnCooldown()
	{
	}
}
