using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class VIPStatusBoostMOTD : DialogBase 
{	
	public Animator vipCardAnimator;
	public Animator godrayAnimator;
	public Animator[] arrowAnimators;
	public UISprite currentLevelMiniSprite;
	public UISprite nextLevelMiniSprite;

	// Set these parts of the VIP card flip animations.
	public VIPNewIcon icon;
	public UISprite progressBar;

	public ButtonHandler closeButton;
	public ButtonHandler topRightCloseButton;

	// MUST BE IN TOP TO BOTTOM ORDER WHEN LINKED ON PREFAB
	// Order:
	//extra coins on purchases
	//extra daily bonus coins
	//extra coins from gifts
	//extra coins sent to friends
	public TextMeshPro[] currentLevelBenefitText;
	public TextMeshPro[] newLevelBenefitText;
	public TextMeshPro timeRemainingText;
	public TextMeshPro currentVIPLevelText;

	private const float TEXT_ROLLUP_TIME = 1.5f;

	// vipCardAnimator - should flow right into the next one
	private const string CARD_ANIMATOR_ENTRY = "VIPLEvelBoostIn";
	 
	// godrayAnimator - should start right after the entry is done.
	private const string GOD_RAY_LOOP = "lightRays";

	// arrowAnimators - should play during their respective rollups
	private const string ARROW_LOOP = "motdArrows";

	VIPLevel levelToShow;
	VIPLevel currentLevel;

	public override void init()
	{
		StatsManager.Instance.LogCount("dialog", "vip_level_upgrade", VIPStatusBoostEvent.getAdjustedLevel().ToString(), genus: "view");

		closeButton.registerEventDelegate(onClickBuy);
		topRightCloseButton.registerEventDelegate(onClickClose);

		int levelToCheck = SlotsPlayer.instance.vipNewLevel + VIPStatusBoostEvent.fakeLevel;
		int modifiedLevel = levelToCheck <= VIPLevel.maxLevel.levelNumber ? levelToCheck : VIPLevel.maxLevel.levelNumber;

		currentLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel);
		levelToShow = VIPLevel.find(modifiedLevel);

		currentLevelMiniSprite.spriteName = string.Format("VIP Icon {0}", currentLevel.levelNumber);
		nextLevelMiniSprite.spriteName = string.Format("VIP Icon {0}", levelToShow.levelNumber);
		currentVIPLevelText.text = "VIP " + levelToShow.name;

		string[] currentLevelBonusText = 
		{
			CommonText.formatNumber(currentLevel.purchaseBonusPct),
			CommonText.formatNumber(currentLevel.dailyBonusPct),
			CommonText.formatNumber(currentLevel.receiveGiftBonusPct),
			CommonText.formatNumber(currentLevel.sendGiftBonusPct)
		};
			
		if (currentLevelBonusText.Length != currentLevelBenefitText.Length)
		{
			Debug.LogWarning("VIPStatusBoostMOTD - Mismatch in length of currentLevelBonusText and currentLevelBenefitText");
		}

		for (int i = 0; i < currentLevelBenefitText.Length; i++)
		{
			currentLevelBenefitText[i].text = currentLevelBonusText[i] + "%";
		}

		VIPStatusBoostEvent.featureTimer.registerLabel(timeRemainingText);
		
		StartCoroutine(startAnimation());
	}

	private IEnumerator startAnimation()
	{
		vipCardAnimator.Play(CARD_ANIMATOR_ENTRY);
		
		StartCoroutine(rollupAllBenefits());

		yield return null;
	}

	private IEnumerator rollupAllBenefits()
	{
		int[] bonusPercents = 
		{
			levelToShow.purchaseBonusPct,
			levelToShow.dailyBonusPct,
			levelToShow.receiveGiftBonusPct,
			levelToShow.sendGiftBonusPct

		};
	
		for (int i = 0; i < newLevelBenefitText.Length; i++)
		{
			arrowAnimators[i].Play(ARROW_LOOP);

			if (i == 0)
			{
				StartCoroutine(SlotUtils.rollup(0,
					bonusPercents[i],
					newLevelBenefitText[i],
					playSound: false,
					specificRollupTime: TEXT_ROLLUP_TIME,
					isCredit: false,
					symbolToAppend: "%"));
			}
			else
			{
				newLevelBenefitText[i].text = CommonText.formatNumber(bonusPercents[i]) + "%";
			}
		}
		
		yield return null;
	}

	private void onClickClose(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "vip_level_upgrade", VIPStatusBoostEvent.getAdjustedLevel().ToString(), family: "close", genus: "click");
		Dialog.close();
	}

	private void onClickBuy(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "vip_level_upgrade", VIPStatusBoostEvent.getAdjustedLevel().ToString(), family:"buy", genus:"click");
		Dialog.close();

		if (!PurchaseFeatureData.isSaleActive && (PurchaseFeatureData.PopcornSale == null || PurchaseFeatureData.PopcornSale.timerRange.isExpired))
		{
			BuyCreditsDialog.showDialog();
		}
	}

	public override void close()
	{
		// Nothing else to do here.
	}

	void Update()
	{
		AndroidUtil.checkBackButton(onClickClose);
	}

	// Might technically be motd
	public static bool showDialog(Dict args = null)
	{
		Scheduler.addDialog("vip_status_boost_motd");
		return true;
	}
}
