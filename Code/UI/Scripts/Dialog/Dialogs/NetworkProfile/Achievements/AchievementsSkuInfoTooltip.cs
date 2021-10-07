using UnityEngine;
using System.Collections;
using TMPro;

/*
Class Name: AchievementsSkuInfoTooltip.cs
Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
Description: This controls the tooltip that shows up on Network level trophies.
*/

public class AchievementsSkuInfoTooltip : MonoBehaviour
{
	[SerializeField] private GameObject goalCompleteTooltip;
	[SerializeField] private GameObject goalIncompleteTooltip;
	[SerializeField] private Animator tooltipAnimator;
	[SerializeField] private TextMeshPro tooltipLabel;
	[SerializeField] private GameObject playNowButton;
	[SerializeField] private TextMeshPro playNowText;
	[SerializeField] private ClickHandler shroudHandler;

	// The background size needs to change to accomdate changes for the HIR-incomplete state.
	[SerializeField] private UISprite incompleteBackgroundSprite;

	private const float TOOLTIP_SCREEN_TIME = 5.0f;
	private const string INTRO_ANIMATION = "intro";
	private const string OFF_ANIMATION = "outro";
	private const float INCOMPLETE_BACKGROUND_SPRITE_WIDTH_HIR = 800f;
	private const float INCOMPLETE_BACKGROUND_SPRITE_WIDTH_SKU = 1240f;

	private string appId = "";
	private NetworkAchievements.Sku skuType;
	private bool isUnlocked = false;
	private bool isRunningRoutine = false;
	private Achievement networkAchievement;

	private bool isTiming = false;
	private bool shouldRestartTimer = false;

	private void toggleShowing(bool shouldShow)
	{
		if (this == null || gameObject == null)
		{
			// If the coroutine is continuing after the object is destroyed, we can NRE somewhere in here.
			return;
		}
		if (shouldShow)
		{
			// Turn on the correct tooltips properly and shroud.
			shroudHandler.gameObject.SetActive(true);
			if (isUnlocked)
			{
				// If it is already completed, just show the Goal Complete default text.
				goalCompleteTooltip.SetActive(true);
				goalIncompleteTooltip.SetActive(false);
			}
			else
			{
				goalCompleteTooltip.SetActive(false);
				goalIncompleteTooltip.SetActive(true);
			}
			tooltipAnimator.Play(INTRO_ANIMATION);
		}
		else
		{
			// Stop the animator.
			tooltipAnimator.Play(OFF_ANIMATION);
			// Hide the elements.
			shroudHandler.gameObject.SetActive(false);
			goalCompleteTooltip.SetActive(false);
			goalIncompleteTooltip.SetActive(false);
		}
	}

	public void show(NetworkAchievements.Sku sku, bool isUnlocked, Achievement networkAchievement)
	{
		setup(sku, isUnlocked, networkAchievement);

		if (isRunningRoutine)
		{
			// if it is already showing, then we just want to tell it to retarter
			shouldRestartTimer = true;
		}
		else
		{
			// Otherwise start up the coroutine
			RoutineRunner.instance.StartCoroutine(showRoutine());
		}
	}


	public void setup(NetworkAchievements.Sku sku, bool isUnlocked, Achievement networkAchievement)
	{
		this.skuType = sku;
		this.isUnlocked = isUnlocked;
		this.networkAchievement = networkAchievement;

		// Iniitalize the shroud.
		shroudHandler.clearAllDelegates();
		shroudHandler.registerEventDelegate(shroudClicked);

		// Otherwise we need to setup the playNow Button.
		switch (sku)
		{
			case NetworkAchievements.Sku.WONKA:
				appId = Data.liveData.getString("XPROMO_URL_WONKA_PLAY", "");
				tooltipLabel.text = Localize.text("complete_the_goal_by_playing_the_game_now", "");
				break;
			case NetworkAchievements.Sku.WOZ:
				appId = Data.liveData.getString("XPROMO_URL_WOZ_PLAY", "");
				tooltipLabel.text = Localize.text("complete_the_goal_by_playing_the_game_now", "");
				break;
			case NetworkAchievements.Sku.BLACK_DIAMOND:
				appId = AppsManager.BDC_SLOTS_ID;
				tooltipLabel.text = Localize.text("complete_the_goal_by_playing_the_game_now", "");
				break;
			default:
			case NetworkAchievements.Sku.HIR:
				appId = AppsManager.HIR_SLOTS_ID;
				tooltipLabel.text = Localize.text("complete_the_goal_in_hir_to_unlock_this_trophy", "");
				break;
		}

		bool isHirIncomplete = (!isUnlocked && sku == NetworkAchievements.Sku.HIR);
		float spriteWidth = isHirIncomplete ? INCOMPLETE_BACKGROUND_SPRITE_WIDTH_HIR : INCOMPLETE_BACKGROUND_SPRITE_WIDTH_SKU;
		playNowButton.SetActive(!isHirIncomplete);
		CommonTransform.setWidth(incompleteBackgroundSprite.transform, spriteWidth);
		// Initialize the play now button.
#if UNITY_WEBGL
		// In WebGL there is no downloading of an app, so this should always be the fallback case.
		playNowText.text = Localize.text("play_now");
#else
		playNowText.text = Localize.text(AppsManager.isBundleIdInstalled(appId) ? "play_now" : "install_now");
#endif

	}

	private void playNowClicked()
	{
		//webgl appID is always empty string
#if !UNITY_WEBGL
		if (!string.IsNullOrEmpty(appId) && appId != AppsManager.HIR_SLOTS_ID)
		{
			// In WebGL there is no downloading of an app, so this should always be the fallback case.
			if (AppsManager.isBundleIdInstalled(appId))
			{
				launchApp();
			}
			else
			{
				openInstallUrl();
			}
		}
#else
		openInstallUrl();
#endif
	}

	private void launchApp()
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "trophy_detail",
			klass: "play",
			family: networkAchievement.id,
			genus: SlotsPlayer.instance.socialMember.zId);
		AppsManager.launchBundle(appId);
	}

	private void openInstallUrl()
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "trophy_detail",
			klass: "download",
			family: networkAchievement.id,
			genus: SlotsPlayer.instance.socialMember.zId);
		Common.openUrlWebGLCompatible(NetworkAchievements.getInstallUrl(skuType));
	}

	private void shroudClicked(Dict args = null)
	{
		// Tell the corutine to finish.
		isTiming = false;
	}

	private IEnumerator showRoutine()
	{
		isRunningRoutine = true;
		// If nothing is active, then start a fresh timer and turn on the correct tooltip.
		isTiming = true;
		float timeElapsed = 0.0f;
		while (isTiming)
		{
			if (goalIncompleteTooltip != null &&
				!goalCompleteTooltip.activeSelf &&
				!goalIncompleteTooltip.activeSelf)
			{
				// If neither of the tooltips is active but we are still here, then we want to turn it back on.
				toggleShowing(true);
			}
			if (shouldRestartTimer == true)
			{
				// If we are telling it torestart, then we need to turn off the tooltips
				// and reset the counters.
				shouldRestartTimer = false;
				timeElapsed = 0.0f;
				toggleShowing(false);
				// Wait a frame for everything to finish.
				yield return null;
			}
			else if (timeElapsed > TOOLTIP_SCREEN_TIME)
			{
				isTiming = false;
				break;
			}

			// Update the timer and wait a frame.
			timeElapsed += Time.deltaTime;
			yield return null;
		}
		isRunningRoutine = false;
		toggleShowing(false);
	}
}
