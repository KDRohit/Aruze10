using UnityEngine;
using TMPro;
using Com.HitItRich.Feature.TimedBonus;
using Com.HitItRich.Feature.VirtualPets;

/**
Controls the display of the daily bonus button in the lobby.
*/

public class DailyBonusButton : BottomOverlayButton
{
	public const string FB_EVENT_BONUS_COINS = "fb_mobile_daily_bonus";
	
	public GameObject collectNowParent;
	public GameObject readyInParent;
	public TextMeshPro timerLabel;
	
	protected string bonusString = "bonus";
	
	public VirtualPetsCollectButtonOverlay petOverlay { get; protected set; }
	
	public virtual bool isCollectNow
	{
		get { return collectNowParent.activeSelf; }
	}
		
	public static DailyBonusButton instance = null;
	
	protected virtual void Awake()
	{
		base.Awake();

		if (instance == null)
		{
			// If we clone this to use in a FTUE or anywhere else, dont overwrite the static reference.
			instance = this;
		}

		sortIndex = 3;

		if (!ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
		{
			if (CampaignDirector.richPass != null && CampaignDirector.richPass.isActive)
			{
				sortIndex++;
			}

			if (EliteManager.isActive)
			{
				sortIndex++;
			}
		}

		// Set it to "ready in" mode by default.
		setActive(false);
	}
	
	/// Resets the button to the non-ready state.
	public virtual void setActive(bool isActive)
	{
		if (readyInParent != null)
		{
			readyInParent.SetActive(!isActive);
		}
		
		if (collectNowParent != null)
		{
			collectNowParent.SetActive(isActive);
		}
		
	}

	public virtual void cleanup()
	{
		instance = null;
	}
	
	public void resetTimer()
	{
		setActive(false);
	}
	
	protected virtual void Update()
	{
	}

	public virtual void onAutoCollect(bool isActive)
	{
	}
	
	/// NGUI button callback.
	public void collectNowClicked()
	{
		//ignore click when pet overlay is up
		if (petOverlay != null)
		{
			petOverlay.onClick(null);
			return;
		}
		
		NGUIExt.disableAllMouseInput();
		
		//log click
		if (ExperimentWrapper.NewDailyBonus.isInExperiment)
		{
			bonusString = ExperimentWrapper.NewDailyBonus.bonusKeyName;
			Audio.play("menuselect0");
			StatsManager.Instance.LogCount(
            	counterName:"bottom_nav",
            	kingdom:	"daily_bonus",
            	phylum:		SlotsPlayer.isFacebookUser ? "fb_connected" : "anonymous",
            	genus:		"click"
            );
		}
		else
		{
			StatsManager.Instance.LogCount("bottom_nav", "daily_bonus", "", "", string.Format("day_{0}", SlotsPlayer.instance.dailyBonusTimer.day), "click");
		}

		// Start the daily bonus game.
		TimedBonusFeature.userCollectBonus();
		
	}
}
