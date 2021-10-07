using System.Collections.Generic;
using System.Collections;
using Com.Scheduler;
using UnityEngine;
using TMPro;

public class CollectBonusDialog : DialogBase, IResetGame
{
	private const string DEFAULT_THEME = "default";
	private const string IOS_VERS_START_STRING = "iPhone_OS_";

	//Top screen assets
	public Animator anim;
	public Animator topBarAnim;
	public Animator superStreakAnim;
	public Transform slider;
	public GameObject fillObject;
	public CollectBonusDayBox[] dayBoxes;

	//mid screen assets
	public CollectBonusScoreBox levelBox;
	public CollectBonusScoreBox vipBox;
	public CollectBonusScoreBox friendBox;
	public CollectBonusScoreBox streakBox;
	public VIPNewIcon vipIcon;
	public GameObject friendIcon;
	public GameObject facebookConnectButton;
	public TextMeshPro connectButtonLabel;
	public TextMeshPro noVIPText;
	public TextMeshPro noFriendsText;
	public TextMeshPro streakMultiplierText;
	public TextMeshPro streakMultiplierTextActive;
	public TextMeshPro streakMultiplierTextGlow;

	//Bottom screen assets
	public TextMeshPro finalScore;
	public GameObject collectButton;

	public TextMeshPro watchToEarnLabel;
	public GameObject watchToEarnButton;
	public GameObject sevenDaysPlusFX;		// shows fx if player is streaking beyond 7 days


	// This could probably be private, but it's just so much easier to get to it from the bonus box classes..
	private static JSON bonusData;
	public static JSON baseAmountData;

	//Just so we can change up the limits if needed. Also makes code more readble
	private const int DAY_LIMIT = 7;
	private const float FILL_OFFSET = 0.95f;
	private const float PERCENT_MODIFIER = 0.01f;
	private const float COLLECT_AREA_Y_COORD = -268f;
	// This is the path to the current background.

	private const string BACKGROUND_TEXTURE_PATH = "misc_dialogs/daily_bonus/Collect_Bonus_BG.png";
	private const string BACKGROUND_TEXTURE_PATH_V2 = "misc_dialogs/daily_bonus/Collect_Bonus_BG_v2.png";


	private long totalCredts = 0;
	private long baseAmount = 0;
	private long streakCredits = 0; 
	private long vipCredits = 0;
	private long friendsCredits = 0;
	
	private bool didTapToSkip = false;	// Impatient fucks.
	private bool isSecondTime = false;

	private string w2eFamilyStr = "";

	public static string theme = DEFAULT_THEME;
	
	private static string dialogTypeKey
	{
		get { return string.Format("collect_bonus_{0}", theme); }
	}

	private const float SLIDE_ANIMATION_DELAY = 3.0f;
	private const float DIAL_ANIMATION_DELAY = 4.0f;
	private const float METER_FIRE_LAND_DELAY = 3.7f;
	private const float DIAL_FIRE_LAND_DELAY = 4.4f;

	private string statFamily = "free_bonus";

	public static void setTheme(string liveDataTheme)
	{
		theme = liveDataTheme;
		
		if (theme == "")
		{
			theme = DEFAULT_THEME;
		}
		else
		{
			// Validate that a dialog type exists for the given theme.
			DialogType dialogType = DialogType.find(dialogTypeKey);
			if (dialogType == null)
			{
				// Theme dialog not found, so use the default theme.
				theme = DEFAULT_THEME;
			}
		}
	}

	public override void init()
	{  
		//WatchToEarn.init(WatchToEarn.REWARD_VIDEO);
		// Start playing the background loop
		Audio.play("DBLoop");

		vipIcon.setLevel(SlotsPlayer.instance.vipNewLevel);
		
		JSON data = dialogArgs.getWithDefault(D.DAILY_BONUS_DATA, null) as JSON;
		
		SlotsPlayer.instance.dailyBonusTimer.startTimer(data.getInt("next_collect", 0));

		finalScore.text = "";

		// Init all our data.
		baseAmount                = data.getInt("base_amount", 0);
		totalCredts               = data.getLong("credits", 0);

		// Used to calculate streak values for friend, VIP, and streak boxes.
		float streakBonusPercent  = data.getFloat("streak_bonus_pct", 0) * PERCENT_MODIFIER;
		float friendBonusPercent  = data.getFloat("friend_bonus_pct", 0) * PERCENT_MODIFIER;
		float vipBonusPercent     = data.getFloat("vip_bonus_pct", 0) * PERCENT_MODIFIER;

		vipCredits          = CommonMath.roundToLong(baseAmount * vipBonusPercent);
		friendsCredits      = CommonMath.roundToLong(baseAmount * friendBonusPercent);
		streakCredits       = CommonMath.roundToLong(baseAmount * streakBonusPercent);

		// init the superstreak multiplier text
		// use the multiplier value from eos
		string multiplierStr = Localize.text("{0}X",CommonText.formatNumber(CommonMath.round(ExperimentWrapper.SuperStreak.multiplier, 1)));
		SafeSet.labelText(streakMultiplierText, multiplierStr, false);
		SafeSet.labelText(streakMultiplierTextActive, multiplierStr, false);
		SafeSet.labelText(streakMultiplierTextGlow, multiplierStr, false);

		// Init the array of day boxes.
		for (int i = 0; i < DAY_LIMIT; i++)
		{
			float percentOfBonus = bonusData.getFloat((i+1).ToString(), 0) * PERCENT_MODIFIER;
			
			CollectBonusDayBox dayBox = dayBoxes[i];
			if (!ExperimentWrapper.SuperStreak.isInExperiment)	// animator handles his in v2 version
			{
				dayBox.init(i);  
			}

			dayBox.score.text = CreditsEconomy.convertCredits(Mathf.RoundToInt(percentOfBonus * baseAmount),true); 
			SafeSet.labelText(dayBox.scoreInactive, dayBox.score.text, false);
		}

		int dayIndex = Mathf.Min(SlotsPlayer.instance.dailyBonusTimer.day - 1, 6);

		SafeSet.gameObjectActive(sevenDaysPlusFX, SlotsPlayer.instance.dailyBonusTimer.day > DAY_LIMIT);

		if (ExperimentWrapper.SuperStreak.isEnabled && topBarAnim != null)
		{
			string animName = "day " + (dayIndex+1) +  " ani";

			StartCoroutine(CommonAnimation.playAnimAndWait(topBarAnim, animName, SLIDE_ANIMATION_DELAY));

			if (dayIndex + 1 == DAY_LIMIT && superStreakAnim != null)		// super streak is active!
			{
				Audio.playWithDelay("DBBoosterMeterAnimation", SLIDE_ANIMATION_DELAY);
				Audio.playWithDelay("DBBoosterMeterLands", METER_FIRE_LAND_DELAY);

				StartCoroutine(CommonAnimation.playAnimAndWait(superStreakAnim, "ani", DIAL_ANIMATION_DELAY));
				Audio.playWithDelay("DBBoosterDayStreakCollectFoley", DIAL_FIRE_LAND_DELAY);
			}
		}
		else
		{
			// Basically just sets the x of the slider to the posistion of one of our day boxes. 
			CommonTransform.setX(
				slider,
				dayBoxes[dayIndex].transform.localPosition.x
			);
		}
		
		// Set all the score data that we'll be using
		float dayValue = Mathf.Min(SlotsPlayer.instance.dailyBonusTimer.day, DAY_LIMIT);
		float scaleValue = dayValue * ((fillObject.transform.localScale.x)/DAY_LIMIT);
		scaleValue *= FILL_OFFSET;

		// Make sure to delete the UI stretch. We only need it for editor work really.
		Destroy(fillObject.GetComponent<UIStretch>());
		CommonTransform.setWidth(fillObject.transform, scaleValue);

		// Set all the bottom info on each box
		levelBox.bottomInfo.text  =  CommonText.formatNumber(SlotsPlayer.instance.socialMember.experienceLevel);
		friendBox.bottomInfo.text =  CommonText.formatNumber(SocialMember.friendPlayers.Count);
		streakBox.bottomInfo.text =  CommonText.formatNumber(SlotsPlayer.instance.dailyBonusTimer.day);

		// Show this text only if you have no friends or VIP credits.
		noVIPText.gameObject.SetActive(vipCredits == 0);
		noFriendsText.gameObject.SetActive(friendsCredits == 0);
		facebookConnectButton.SetActive(friendsCredits == 0);	// If actually connected but has no friends, CONNECT changes to INVITE
		friendIcon.SetActive(friendsCredits > 0);
		
		// Set the VIP box background.
		SafeSet.gameObjectActive(vipBox.boxNormal, vipCredits > 0);
		SafeSet.gameObjectActive(vipBox.boxRed, vipCredits == 0);
		SafeSet.gameObjectActive(vipBox.middleContents, vipCredits > 0);

		// Set the friends box background.
		SafeSet.gameObjectActive(friendBox.boxNormal, friendsCredits > 0);
		SafeSet.gameObjectActive(friendBox.boxRed, friendsCredits == 0);
		SafeSet.gameObjectActive(friendBox.middleContents, friendsCredits > 0);

		SafeSet.gameObjectActive(levelBox.middleContents, baseAmount > 0);
		SafeSet.gameObjectActive(streakBox.middleContents, streakCredits > 0);

		if (SlotsPlayer.isFacebookUser)
		{
			connectButtonLabel.text = Localize.textUpper("invite");

			// Friends credits at 0 means no friends.
			if (friendsCredits == 0)
			{               
				noFriendsText.text = Localize.text("invite_to_receive_bonus");
			}	   
		}
		else
		{
			connectButtonLabel.text = Localize.textUpper("connect");
			noFriendsText.text = Localize.textUpper("facebook_login_credits_message_{0}", CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus));
		}
	
		if (friendsCredits == 0)
		{
			friendBox.bottomInfo.text = "";
		}

		initWatchToEarn();

		// If the player never collected (see DailyBonusGameTimer.cs).
		if (SlotsPlayer.instance.dailyBonusTimer.dateLastCollected == "none")
		{
			collectButton.SetActive(false);
			SafeSet.gameObjectActive(watchToEarnButton, false);	
			StartCoroutine(waitToClose());
		}

		// Mark the daily bonus as being seen.
		CustomPlayerData.setValue(CustomPlayerData.FTUE_COLLECTED_DAILY_BONUS, true);

		StatsManager.Instance.LogCount("dialog", statFamily, "lobby", string.Format("day_{0}", SlotsPlayer.instance.dailyBonusTimer.day), w2eFamilyStr, "view");
	}

	public void initWatchToEarn()
	{
		if (WatchToEarn.initWatchToEarnUI(watchToEarnButton, collectButton, watchToEarnLabel, ExperimentWrapper.WatchToEarn.shouldShowDailyBonusCollect, UnityAdsManager.PlacementId.VIDEO))
		{
			w2eFamilyStr = "w2e_offer";  // this is used when we log the stat at the end of init()
		}
	}	

	protected override void onFadeInComplete()
	{
		// When the dialog drops in, we play this sound.
		Audio.play("DBPaneIn");
		StartCoroutine(playAnimations());
		base.onFadeInComplete();
	}
	
	void Update()
	{
		if (TouchInput.didTap)
		{
			didTapToSkip = true;
		}
	}
	
	public static void showDialog(JSON data)
	{
		Audio.play("SelectPremiumAction");
		Dict args = Dict.create(
			D.DAILY_BONUS_DATA, data,
			D.PRIORITY, SchedulerPriority.PriorityType.IMMEDIATE
		);
		
		string backgroundPath = "";
		
		if (theme == "default")
		{
			if (ExperimentWrapper.SuperStreak.isInExperiment)
			{
				backgroundPath = BACKGROUND_TEXTURE_PATH_V2;
			}
			else
			{
				backgroundPath = BACKGROUND_TEXTURE_PATH;
			}
		}

		if (backgroundPath != "")
		{
			Dialog.instance.showDialogAfterDownloadingTextures(dialogTypeKey, backgroundPath, args);
		}
		else
		{
			Scheduler.addDialog(dialogTypeKey, args);
		}
	}

	public static void preloadDialogTexture()
	{
		DisplayAsset.preloadTexture(BACKGROUND_TEXTURE_PATH);
	}

		// NGUI button callback
	private void watchToEarnClick()
	{
		StatsManager.Instance.LogCount("dialog", statFamily, "lobby", string.Format("day_{0}", SlotsPlayer.instance.dailyBonusTimer.day), "w2e_offer", "click", WatchToEarn.rewardAmount);
		
		addCreditsAndClose();

		WatchToEarn.watchVideo("daily_bonus", true);
	}
	
	// NGUI button callback
	private void clickClose()
	{
		Audio.play("DBCollectButton");
		StatsManager.Instance.LogCount("dialog", statFamily, "", "", "", "click");
		StatsManager.Instance.LogCount("dialog", statFamily, "lobby", string.Format("day_{0}", SlotsPlayer.instance.dailyBonusTimer.day), "collect", "click", totalCredts);
		StartCoroutine(closeAfterDelay());
	}
	
	// Close after one frame of delay so the click on the button doesn't slam stop the credits rollup immediately.
	protected virtual IEnumerator closeAfterDelay()
	{
		yield return null;
		addCreditsAndClose();
	}
	
	protected void addCreditsAndClose()
	{
		// Don't play the rollup sound, because it aborts the collect button sound.
		SlotsPlayer.addNonpendingFeatureCredits(totalCredts, "dailyBonus");	
		Dialog.close();
	}
	
	// NGUI button callback
	private void connectToFacebook()
	{    
		if (friendsCredits == 0 && SlotsPlayer.isFacebookUser)
		{
			MFSDialog.inviteFacebookNonAppFriends();
			clickClose();
		}  
		else
		{
			SlotsPlayer.facebookLogin();
		}
	}

	private IEnumerator waitToClose()
	{
		yield return new WaitForSeconds(8f);
		clickClose();
	}     

	protected virtual IEnumerator playAnimations()
	{
		anim.Play("intro");

		yield return new WaitForSeconds(0.5f);

		// Unfortunately, these sounds will be played even if the animation is cancelled, because playWithDelay() is fire-and-forget.
		Audio.playWithDelay("DB7DayStreakPaneIn", 1.5f);
		Audio.playWithDelay("DBTotalBonusPaneIn", 1.5f);
		
		if (didSkipAnimation())
		{
			yield break;
		}

		Audio.play("DBFeaturePane1");	
		yield return StartCoroutine(levelBox.rollup(baseAmount));

		if (didSkipAnimation())
		{
			yield break;
		}

		Audio.play("DBFeaturePane2");	
		yield return StartCoroutine(vipBox.rollup(vipCredits));

		if (didSkipAnimation())
		{
			yield break;
		}

		Audio.play("DBFeaturePane3");	
		yield return StartCoroutine(friendBox.rollup(friendsCredits));

		if (didSkipAnimation())
		{
			yield break;
		}

		Audio.play("DBFeaturePane4");	
		yield return StartCoroutine(streakBox.rollup(streakCredits, true));

		if (didSkipAnimation())
		{
			yield break;
		}

		yield return StartCoroutine(SlotUtils.rollup(0L, totalCredts, finalScore, true, 2f, false, false, "DBTallyLoop", "DBDayStreakDayTotal"));
	}
	
	protected virtual bool didSkipAnimation()
	{
		if (!didTapToSkip)
		{
			return false;
		}
		
		// Skipping animation. Jump to the idle animation and immediately set the box values.
		anim.Play("idle");

		levelBox.setValueImmediately(baseAmount);
		vipBox.setValueImmediately(vipCredits);
		friendBox.setValueImmediately(friendsCredits);
		streakBox.setValueImmediately(streakCredits);
		
		finalScore.text = CreditsEconomy.convertCredits(totalCredts);
		
		return true;
	}

	public static void setBonusData(JSON data)
	{
		bonusData = data.getJSON("streak_bonus_pct");
		baseAmountData = data.getJSON("base_amount");
	}

	public override void close()
	{
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		theme = DEFAULT_THEME;
	}
}




