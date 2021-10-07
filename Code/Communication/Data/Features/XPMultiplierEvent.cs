using UnityEngine;
using System.Collections;

public class XPMultiplierEvent : MultiTimeRangeEventFeature
{
	public static XPMultiplierEvent instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<XPMultiplierEvent>("xp_multiplier");
		}
	}

	public int xpMultiplier
	{
		get
		{
			if (isEnabled)
			{
				return _xpMultiplier;
			}
			else
			{
				return 1;
			}
		}
		private set
		{
			_xpMultiplier = value;
		}
	}

	private int _xpMultiplier = 1;
	public int minLevel = 0;
	public bool isLiveDataEnabled = false;
	private bool wasEnabledOnStartup = false;
	private int cachedXpEventMultipler = 0;
	private bool hasPowerupXp = false;
	
	private const string CAROUSEL_SLIDE_FORMAT = "xp_multiplier:{0}";

	public GameTimerRange getTimer()
	{
		return featureTimer.combinedActiveTimeRange;
	}

	public void startEventFromCharm(int duration, int multiplier)
	{
		xpMultiplier = multiplier;
		startWithTimeRemaining(duration,"charm");
	}

	public void endEventFromCharm()
	{
		// Just reget the live data values so that we go back to whatever the public xp Multiplier amounts are.
		getLiveDataValues();
	}

	private void getLiveDataValues()
	{
		isLiveDataEnabled = Data.liveData.getBool("XP_MULTIPLIER_ON_OFF", false);
		minLevel = Data.liveData.getInt("XP_MULTIPLIER_MIN_LEVEL", 0);
		xpMultiplier = Data.liveData.getInt("XP_MULTIPLIER", 1);

		int beginTime = Data.liveData.getInt("XP_MULTIPLIER_START_TIMESTAMP", 0);
		int endTime = Data.liveData.getInt("XP_MULTIPLIER_END_TIMESTAMP", 0);
		setTimestamps(beginTime, endTime);
	}

	protected override void registerEventDelegates()
	{
		// Setup these validate calls to run on enable/disable so we don't have to call them manually
		onEnabledEvent += validateSpinXpMultiplier;
		onDisabledEvent += validateSpinXpMultiplier;
		Server.registerEventDelegate("timed_xp_multiplier_on", xpMultiplierOnEvent, true);
		Server.registerEventDelegate("timed_xp_multiplier_off", xpMultiplierOffEvent, true);
	}

	protected override void clearEventDelegates()
	{
		onEnabledEvent -= validateSpinXpMultiplier;
		onDisabledEvent -= validateSpinXpMultiplier;
	}

	private void toggleCarousel(bool isActive)
	{
		string carouselSlideName = string.Format(CAROUSEL_SLIDE_FORMAT, xpMultiplier);
		// See if there is a carousel slide to show now.
		CarouselData slide = CarouselData.findInactiveByAction(carouselSlideName);
		if (slide != null)
		{
			if (isActive)
			{
				slide.activate();
			}
			else
			{
				slide.deactivate();
			}

		}
		else
		{
			CustomLog.Log.log("xpMultiplierOnEvent CarouselData slide is null. Multiplier : " + instance.xpMultiplier, Color.green);
		}
	}

	public void onPowerupEnabled(int multiplierAmount, int timeRemaining, string source = "xpPowerUpEvent")
	{
		if (!hasPowerupXp)
		{
			if (multiplierAmount > xpMultiplier)
			{
				xpMultiplier += multiplierAmount;
				hasPowerupXp = true;
			}
		}
		startWithTimeRemaining(timeRemaining,source);
	}

	public void onPowerupDisabled(int multiplierAmount)
	{
		hasPowerupXp = false;
		xpMultiplier -= multiplierAmount;

		// Null check the fuck out of this, especially with multiple Overlays from different skus.
		if (Overlay.instance != null && Overlay.instance.top != null && Overlay.instance.top.xpUI != null)
		{
			// Tell the overlay xpui to recheck its state (and swap to whatever is the valid state).
			Overlay.instance.top.xpUI.checkEventStates();
		}
	}
	
	private void xpMultiplierOnEvent(JSON xpMultiplierData)
	{
		CustomLog.Log.log("xpMultiplierOnEvent: " + xpMultiplierData.ToString(), Color.green);

		cachedXpEventMultipler = xpMultiplierData.getInt("multiplier", 1);
		instance.xpMultiplier += cachedXpEventMultipler;

		if (!isEnabled)
		{
			// If the event is not already enabled, then enable the event.
			bool shouldShowDialog = xpMultiplierData.getBool("show_dialog", false);
			
			// Two different ways to provide the timer info, remaining and absolute end time.
			int timeRemaining = xpMultiplierData.getInt("time_remaining", 0);
			
			if (timeRemaining == 0)
			{
				int timeNow = GameTimer.currentTime;
				int timeEnd = xpMultiplierData.getInt("end_time", 0);
				timeRemaining = timeEnd - timeNow;
				
				CustomLog.Log.log("xpMultiplierOnEvent using end_time. timeNow: " + timeNow + ", remaining: " + timeRemaining, Color.green);
			}

			toggleCarousel(true);
			startWithTimeRemaining(timeRemaining,"xpMultiplier_Event");
			
			if (Overlay.instance != null)
			{
				Overlay.instance.top.xpUI.checkEventStates();
			}

			if (!wasEnabledOnStartup && isEnabled && !isEnabledByPowerup)
			{
				// If we recieve this event and the feature was not enabled at startup, then show the dialog.
				XPMultiplierDialog.showDialog();
			}
			/*
			// Don't show the dialog if the feature is about to expire.
			if (IntroDialog.shouldShowStartupDialogs &&
				instance.shouldShowXPMultiplierDialog &&
				timeRemaining >= Common.SECONDS_PER_MINUTE &&
				// Also only show it if we're not in the middle of autolaunching a game at startup.
				LobbyLoader.autoLaunchGameResult == LobbyGame.LaunchResult.NO_LAUNCH
				)
			{

			}
			*/
		}
	}

	private void xpMultiplierOffEvent(JSON xpMultiplierData)
	{
		if (instance == null)
		{
			// According to Crittercism, this might be happening. 
			return;
		}
		
		CustomLog.Log.log("xpMultiplierOffEvent: " + (null == xpMultiplierData ? instance.xpMultiplier.ToString() : xpMultiplierData.ToString()), Color.red);
		
		// See if there is a carousel slide to remove now.
		toggleCarousel(false);

		xpMultiplier = 1;
		instance.xpMultiplier -= cachedXpEventMultipler;

		// Null check the fuck out of this, especially with multiple Overlays from different skus.
		if (Overlay.instance != null && Overlay.instance.top != null && Overlay.instance.top.xpUI != null)
		{
			// Tell the overlay xpui to recheck its state (and swap to whatever is the valid state).
			Overlay.instance.top.xpUI.checkEventStates();
		}
	}

	// If the last spin was done at a different multiplier than the one in the event,
	// then we need to adjust the client XP after the spin. This should be a very rare
	// thing that only happens if a spin started with one multiplier, but the backend
	// changed the multiplier between the spin starting and getting processed on the backend.
	// This must be called AFTER setting the xpMultiplier variable to the correct amount from the event.
	private void validateSpinXpMultiplier()
	{
		if (SlotBaseGame.instance == null)
		{
			return;
		}
		
		int lastSpinMultiplier = SlotBaseGame.instance.lastSpinXPMultiplier;	// shorthand
		
		if (lastSpinMultiplier != 0 && lastSpinMultiplier != instance.xpMultiplier)
		{
			long badAddedXP = lastSpinMultiplier * SlotBaseGame.instance.betAmount;
			long goodXP = instance.xpMultiplier * SlotBaseGame.instance.betAmount;
			long difference = goodXP - badAddedXP;
			
			Debug.LogWarning("Spin XP multiplier discrepency detected, spun with " + lastSpinMultiplier +
				", server says: " + instance.xpMultiplier + ", adjusting xp by " + difference);
			
			if (difference > 0)
			{
				SlotsPlayer.instance.xp.add(difference, "xp multiplier fix", false, false);
			}
			else
			{
				// The difference is negative, but we always pass in a positive value to subtract().
				SlotsPlayer.instance.xp.subtract(-difference, "xp multiplier fix");
			}
		}
	}

	public string disabledReason
	{
		get
		{
			string reason = "";
			if (!isEnabled)
			{
				if (!isLiveDataEnabled)
				{
					reason += "Live data key is disabled.\n";
				}
				else if (!featureTimer.isActive)
				{
					reason += "Feature timer has not started.\n";
				}				
				else if (!featureTimer.isEnabled)
				{
					reason += "Feature timer is expired.\n";
				}
			}
			return reason;
		}
	}
	
	#region feature_base_overrides
	protected override void initializeWithData(JSON data)
	{	
		// Check for xp multiplier in player data after populating GameExperience, since it relies on the player's total spin count.
		// and after recieving buff defintions from server
		JSON xpMultiplierData = data.getJSON("player.timed_xp_multiplier");
		if (xpMultiplierData != null)
		{
			xpMultiplierOnEvent(xpMultiplierData);
		}
		
		// This feature doesnt use any data from player data.
		getLiveDataValues();
		wasEnabledOnStartup = isEnabled && !isEnabledByPowerup;
	}
	
	public override bool isEnabled
	{
		get
		{
			return (isLiveDataEnabled || isEnabledByPowerup) &&
				SlotsPlayer.instance != null &&
				SlotsPlayer.instance.socialMember != null &&
				(isEnabledByPurchasePowerup || SlotsPlayer.instance.socialMember.experienceLevel > minLevel) &&
				base.isEnabled;
		}
	}

	public bool isEnabledByPurchasePowerup
	{
		get
		{
			return PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_TRIPLE_XP_KEY);
		}
	}
	

	public bool isEnabledByPowerup
	{
		get
		{
			return PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_TRIPLE_XP_KEY) ||
			       PowerupsManager.hasActivePowerupByName(PowerupBase.LEVEL_LOTTO_TRIPLE_XP_KEY);
		}
	}
	#endregion	
}
