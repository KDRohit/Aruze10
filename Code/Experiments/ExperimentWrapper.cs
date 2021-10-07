using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.EUE;
using Com.HitItRich.Feature.BundleSale;
using Com.HitItRich.Feature.VirtualPets;
using Com.Scheduler;
using PrizePop;
using UnityEngine;
using QuestForTheChest;

public class ExperimentWrapper : IResetGame
{
	// static variables below are used for dev panel 
	private static Vector2 variantListScroll = Vector2.zero;
	private static string experimentName;
	private static bool loadInProgress = false;
	private static bool showExperimentDetail = false;
	private static string currentVariant;
	private static JSON[] variantsInfo;

	private const string UPDATE_FAIL = "{0} has not been successful, please try again later or contact server dev";
	private const string RELOAD_GAME = "Update has been successful, game will reload shortly to get the new EOS data";
	public const string CLEAR_ALL_EOS_WHITELIST_FORMAT_DEV = "The update in the following experiments is not successful: {0}. If specific experiments still show up in the list, please contact engineers to clean it up. Game will reload shortly";

	//The prefix used by experiment names on EOS
	public const string HIR_EXPERIMENTS_PREFIX = "hir_";
	// for display on the dev panel to verify which variant is being used for each experiment.
	public static void displayVariants(bool active, bool inactive, bool isHiRes, string filter)
	{
		Color oldColor = GUI.color;

		GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
		displayEosExperiments(active, inactive, isHiRes, filter);

		GUILayout.EndVertical();
		GUI.color = oldColor;
	}

	private static void displayEosExperiments(bool showActive, bool showInactive, bool isHiRes, string filter)
	{
		if (showExperimentDetail)
		{
			Color oldColor = GUI.color;

			GUILayout.BeginHorizontal();

			GUI.color = Color.white;

			if (GUILayout.Button("Clear White List", GUILayout.Width(isHiRes ? 300 : 110)))
			{
				ClearEosExperimentWhitelistAction.ProcessResponseEvent += handleClearExperimentWhitelistEvent;
				ClearEosExperimentWhitelistAction.clearExperimentWhitelist(experimentName);
				showExperimentDetail = false;
			}

			if (GUILayout.Button("Cancel", GUILayout.Width(isHiRes ? 300 : 110)))
			{
				showExperimentDetail = false;
				loadInProgress = false;
			}

			GUILayout.EndHorizontal();
			GUILayout.Space(20);

			foreach (JSON json in variantsInfo)
			{
				string name = json.getString("name", "");
				JSON variables = json.getJSON("variables");
				List<string> keys = variables.getKeyList();

				GUILayout.BeginHorizontal();

				GUI.color = name.Equals(currentVariant) ? Color.green : Color.white;

				// display variant name 
				if (GUILayout.Button(name, GUILayout.Width(isHiRes ? 300 : 110)))
				{
					SwitchVariantAction.ProcessResponseEvent += handleSwitchVariantEvent;
					SwitchVariantAction.switchVariant(experimentName, name, variables);
					showExperimentDetail = false;
				}

				GUILayout.BeginVertical();

				for (int i = 0; i < keys.Count; i++)
				{
					string value = variables.getString(keys[i], "");
					GUILayout.BeginHorizontal();
					GUI.color = Color.blue;
					GUILayout.Label(keys[i], GUILayout.Width(isHiRes ? 650 : 250));
					GUI.color = Color.green;
					GUILayout.Label(value, GUILayout.Width(isHiRes ? 650 : 250));
					GUILayout.EndHorizontal();
				}

				GUILayout.Space(30);
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
			GUI.color = oldColor;
		}
		else
		{
			Color oldColor = GUI.color;
			var oldButton = new GUIStyle(GUI.skin.button);
			var oldLabel = new GUIStyle(GUI.skin.label);

			var eosExperiments = ExperimentManager.EosExperiments;

			GUI.enabled = !loadInProgress;

			if (eosExperiments != null && eosExperiments.Count > 0)
			{
				foreach (string key in eosExperiments.Keys)
				{
					EosExperiment eosData = ExperimentManager.GetEosExperiment(key);

					if ((filter != "" && !key.Contains(filter)) ||
						(eosData == null) || (!eosData.isEnabled && !showInactive) ||
						(eosData.isEnabled && !showActive)
					)
					{
						continue;
					}

					GUILayout.BeginHorizontal();

					// green indicates that the experiment is enabled 
					// red indicates that the experiment is disabled, there can be various reasons 
					// 1. not receiving info of the experiment from server side
					// 2. the experiment is listed within control or holdout 
					GUI.color = eosData.isInExperiment ? Color.green : Color.red;

					string variantName = eosData.variantName;

					// if user clicked on the experiment name, the detail of the experiment will be shown
					if (GUILayout.Button(key, GUILayout.Width(isHiRes ? 400 : 210)))
					{
						LookupExperimentAction.ProcessResponseEvent += handleLookupExperimentEvent;
						LookupExperimentAction.lookupExperiment(key, variantName);
						experimentName = key;
						loadInProgress = true;
					}

					GUI.color = Color.white;

					// display the current variant enrolled in
					GUILayout.Label(variantName, GUILayout.Width(isHiRes ? 600 : 350));

					GUILayout.Space(isHiRes ? 20 : 10);

					GUI.color = Color.yellow;
					if (GUILayout.Button("Holdout", GUILayout.Width(isHiRes ? 250 : 100)))
					{
						loadInProgress = true;
						HoldoutExperimentAction.ProcessResponseEvent += handleHoldoutExperimentEvent;
						HoldoutExperimentAction.holdoutExperiment(key);
					}

					GUILayout.EndHorizontal();
				}

				GUI.color = Color.red;
				if (GUILayout.Button("Clear whitelist in all experiments"))
				{
					loadInProgress = true;
					ClearAllEosWhitelistAction.ProcessResponseEvent += handleClearAllExperimentsWhitelistEvent;
					ClearAllEosWhitelistAction.clearAllExperimentWhitelist(new List<string>(eosExperiments.Keys));
				}

				GUI.enabled = true;
			}
			GUI.color = oldColor;

			GUI.skin.button = oldButton;
			GUI.skin.label = oldLabel;
		}
	}

	public static void handleLookupExperimentEvent(bool success, string group, JSON[] items)
	{
		if (success)
		{
			currentVariant = group;
			variantsInfo = items;
			showExperimentDetail = true;
		}
		else
		{
			Data.showIssue(string.Format(UPDATE_FAIL, "Look up variant"));
		}

		LookupExperimentAction.ProcessResponseEvent -= handleLookupExperimentEvent;
	}

	public static void handleSwitchVariantEvent(bool success)
	{
		SwitchVariantAction.ProcessResponseEvent -= handleSwitchVariantEvent;

		if (!success)
		{
			Data.showIssue(string.Format(UPDATE_FAIL, "Switch variant"));
		}

		showExperimentDetail = false;
		loadInProgress = false;
	}

	public static void handleHoldoutExperimentEvent(bool success)
	{
		HoldoutExperimentAction.ProcessResponseEvent -= handleHoldoutExperimentEvent;

		if (!success)
		{
			Data.showIssue(string.Format(UPDATE_FAIL, "Holdout experiment"));
		}

		loadInProgress = false;
	}

	public static IEnumerator reloadWithDelay(string msg, int delayTime)

	{
		yield return new WaitForSeconds(delayTime);
		Glb.resetGame(msg);
	}

	public static void handleClearExperimentWhitelistEvent(bool success)
	{
		ClearEosExperimentWhitelistAction.ProcessResponseEvent -= handleClearExperimentWhitelistEvent;

		if (success)
		{
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, "RELOAD GAME",
					D.MESSAGE, RELOAD_GAME,
					D.CALLBACK, new DialogBase.AnswerDelegate((args) =>
					{
						RoutineRunner.instance.StartCoroutine(reloadWithDelay(RELOAD_GAME, delayTime: 10));
					})
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			);
		}
		else
		{
			Data.showIssue(string.Format(UPDATE_FAIL, "Clear experiment whitelist"));
		}

		loadInProgress = false;
	}

	public static void handleClearAllExperimentsWhitelistEvent(bool totalFailure, string[] failedList)
	{
		ClearAllEosWhitelistAction.ProcessResponseEvent -= handleClearAllExperimentsWhitelistEvent;

		if (totalFailure)
		{
			Data.showIssue(string.Format(UPDATE_FAIL, "Clear all experiments whitelist"));
		}
		else if (failedList.Length == 0)
		{
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, "RELOAD GAME",
					D.MESSAGE, RELOAD_GAME,
					D.CALLBACK, new DialogBase.AnswerDelegate((args) =>
					{
						RoutineRunner.instance.StartCoroutine(reloadWithDelay(RELOAD_GAME, delayTime: 10));
					})
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			);
		}
		else
		{
			string failedListString = string.Join(", ", failedList);
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, "RELOAD GAME",
					D.MESSAGE, string.Format(CLEAR_ALL_EOS_WHITELIST_FORMAT_DEV, failedListString),
					D.CALLBACK, new DialogBase.AnswerDelegate((args) =>
					{
						RoutineRunner.instance.StartCoroutine(reloadWithDelay(string.Format(CLEAR_ALL_EOS_WHITELIST_FORMAT_DEV, failedListString), delayTime: 10));
					})
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			);
		}

		loadInProgress = false;
	}

	public static void resetStaticClassData()
	{
		loadInProgress = false;
		showExperimentDetail = false;
	}

	//--------------------------------------------------------------------------------------------------
	/*
	Variants.
	 enabled : false -- Do not use the age gate.
	 enabled : true -- Use the age gate.
	*/

	public static class AgeGate
	{
		public const string experimentName = "age_gate";

	}

	//--------------------------------------------------------------------------------------------------

	/*
	I'm temporarily leaving this wrapper so we can access the variant resolution.
	The locked lobbies are stored in an array that is accessed by the variant.
	We should remove the array because we don't have to index into it anymore.
	*/

	public static class LockedLobby
	{
		private const int VARIANT_RESOLUTION = 3;

		public static int variant
		{
			get { return VARIANT_RESOLUTION; }
		}
	}

	//--------------------------------------------------------------------------------------------------

	/*
	Variables:
	* enabled (boolean)
	*/

	public static class LobbyV2
	{
		public const string experimentName = "lobby_version_2_mobile";

	}

	//--------------------------------------------------------------------------------------------------

	public class LobbyV3 : IResetGame
	{
		public const string experimentName = "lobby_version_3_mobile";
		private static bool isOverrideActive = false;
		private static bool enableOverride = false;

		public static bool isInFtue
		{
			get
			{
				if (isOverrideActive)
				{
					return enableOverride;
				}

				LobbyV3Experiment exp = ExperimentManager.GetEosExperiment(experimentName) as LobbyV3Experiment;
				if (exp != null)
				{
					return exp.isFtueEnabled;
				}

				return false;
			}
		}

		public static void forceEnabled(bool enabled)
		{
			isOverrideActive = true;
			enableOverride = enabled;
		}

		public static void resetStaticClassData()
		{
			isOverrideActive = false;
			enableOverride = false;
		}
	}

	//--------------------------------------------------------------------------------------------------
	/*
	Variants:
		1. Control (no do not show new tos dialog)
		2. Show the new TOS dialog
	*/

	public static class UpdatedTOS
	{
		public const string experimentName = "tos_update_dialog";
	}

	//--------------------------------------------------------------------------------------------------

	public static class XPromoWOZSlotsGameUnlock
	{
		/*
			Variants.
			1. Control - Don't show the offer or unlock a game.
			2. Show the feature and unlocks a game if WOZ slots is installed.
		*/
		public const string experimentName = "xpromo_woz_slots_gameunlock";
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public static class ZadeXPromo
	{
		/*
			Variants.
			1. Control - Do not show the Zade XPromo ads (neither carousel nor ooc).
			2. Show the zade xpromo feature.
		 */

		public const string experimentName = "xpromo_zade";
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public static class SoftwareUpdateDialog
	{
		/*
			Variants.
			1. Control - Do not show the Software Update Dialog.
			2. Show the Software Update Dialog.
		 */
#if ZYNGA_KINDLE || UNITY_EDITOR
		public const string experimentName = "hirm_update_kindle";
#elif ZYNGA_IOS
		public const string experimentName = "hirm_update_ios";
#else
		public const string experimentName = "hirm_update_kindle";
#endif
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class PathToRiches
	{
		public const string experimentName = "ptr_drop_rate";

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}


	//--------------------------------------------------------------------------------------------------

	public class MobileToMobileXPromo
	{
		public const string experimentName = "xpromo_creative_v2";


		public static string getArtCampaign()
		{
			return experimentData == null ? "" : experimentData.getArtCampaign();
		}

		public static string[] getAllArtInCampaign()
		{
			return experimentData == null ? null : experimentData.getAllArtInCampaign();
		}

		public static string getDialogArt()
		{
			return experimentData == null ? "" : experimentData.getDialogArt();
		}

		public static string getPromoGame()
		{
			return experimentData == null ? "" : experimentData.getPromoGame();
		}

		public static int autoSwapRotationTimes
		{
			get
			{
				return experimentData == null ? 0 : experimentData.autoSwapRotationTimes;
			}
		}

		public static bool enablePlay
		{
			get
			{
				return experimentData != null && experimentData.enablePlay;
			}
		}

		public static string recipient
		{
			get
			{
				return experimentData == null ? "" : experimentData.recipient;
			}
		}

		public static int autoPopCooldown
		{
			get
			{
				return experimentData == null ? 0 : experimentData.autoPopCooldown;
			}
		}

		private static string getPromoGameFromArt(string campaign)
		{
			return experimentData == null ? "" : experimentData.getPromoGameFromArt(campaign);
		}

		public static int dialogMaxViewToSwap
		{
			get
			{
				return experimentData == null ? 0 : experimentData.dialogMaxViewToSwap;
			}
		}

		public static string playUrl
		{
			get
			{
				return experimentData == null ? "" : experimentData.playUrl;
			}
		}
		public static string installUrl
		{
			get
			{
				return experimentData == null ? "" : experimentData.installUrl;
			}
		}

		public static int OOCMaxViews
		{
			get { return experimentData == null ? 0 : experimentData.OOCMaxViews; }
		}

		public static bool shouldOOCAutoPop
		{
			get { return experimentData != null && experimentData.shouldOOCAutoPop; }
		}
		public static int RTLMaxViews
		{
			get { return experimentData == null ? 0 : experimentData.RTLMaxViews; }
		}
		public static bool shouldRTLAutoPop
		{
			get { return experimentData != null && experimentData.shouldRTLAutoPop; }
		}
		private static MobileToMobileXPromoExperiment _experimentData;
		public static MobileToMobileXPromoExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as MobileToMobileXPromoExperiment;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	/*
	Variants:
		1. Control
		2. On. Show progressive jackpot on buy page.
	*/

	public static class BuyPageProgressive
	{
		public const string experimentName = "buypage_progressive";
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}

		public static bool jackpotDaysIsActive
		{
			get
			{
				int maxBonusPercentage = PurchaseFeatureData.findBuyCreditsSalePercentage(); //Feature shouldn't be active if a sale is also active
				return (isInExperiment && SlotsPlayer.instance.jackpotDaysTimeRemaining.timeRemaining > 0 && maxBonusPercentage <= 0);
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	/*
	Variants:
		1. Control 
		2. On. First time users enter wonka01 first.
	*/

	public static class LoadGameFTUE
	{
		public const string experimentName = "ftue_quick_play";

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}

		public static string gameKey
		{
			get
			{
				LoadGameFTUEExperiment exp = ExperimentManager.GetEosExperiment(experimentName) as LoadGameFTUEExperiment;
				if (exp != null)
				{
					return exp.gameKey;
				}

				return "wonka01";
			}
		}
	}

	//--------------------------------------------------------------------------------------------------


	/*
	Variants:
		1. Control 
		2. On. First time users enter wonka01 first.
	*/

	public class StarterPackEos : IResetGame
	{
		public const string experimentName = "starter_pack_increase";

		public static string creditPackageName
		{
			get { return experimentData == null ? "" : experimentData.creditPackageName; }
		}

		public static string packageOfferString
		{
			get { return experimentData == null ? "" : experimentData.packageOfferString; }
		}

		public static int bonusPercent
		{
			get { return experimentData == null ? 0 : experimentData.bonusPercent; }
		}
		public static int strikethroughAmount
		{
			get { return experimentData == null ? 0 : experimentData.strikethroughAmount; }
		}

		// HIR only
		public static string artPackage
		{
			get { return experimentData == null ? "" : experimentData.artPackage; }
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}


		public static StarterPackExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as StarterPackExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------


	/*
	Variants:
		1. Control 
		2. On. First time users enter wonka01 first.
	*/

	public static class Ftue
	{
		public const string experimentName = "ftue";
	}

	//--------------------------------------------------------------------------------------------------

	public static class NeedCreditsThreeOptions
	{

		/* 
		   Variants:
			enabled: Use this EOS experiment to set the STUD data for the OOC dialog.
		 */
		public const string experimentName = "out_of_coins";


		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------
	/* 
	 * Variants
	 * 0. Control. Don't surface Linked VIP Network.
	 * 1. Don't surface Linked VIP Network.
	 * 2. Surface Linked VIP Network.
	 */

	public static class LinkedVipNetwork
	{
		public const string experimentName = "linked_vip";

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}

	}

	//--------------------------------------------------------------------------------------------------

	public static class PopcornVariantTest
	{
		/* 
		   PopcornVariantTest -- Experiment to test the popcorn sale template variants. If the user is in the control then we ignore the variables coming down from this and use what was defined in STUD.

		*/

		public const string experimentName = "popcorn_asset_test";

		public static string template
		{
			get
			{
				PopcornVariantExperiment exp = ExperimentManager.GetEosExperiment(experimentName) as PopcornVariantExperiment;
				if (exp != null)
				{
					return exp.template;
				}

				return "";
			}
		}
		public static string theme
		{
			get
			{
				PopcornVariantExperiment exp = ExperimentManager.GetEosExperiment(experimentName) as PopcornVariantExperiment;
				if (exp != null)
				{
					return exp.theme;
				}

				return "";
			}
		}

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	// --------------------------------------------------------------------------------------------------


	public class SegmentedDynamicMOTD
	{
		/* 
		   SegmentedDynamicMOTD -- The Dynamic MOTD will now be controlled via data through EOS, so that we can target specific indiviuals/groups simultaneously.
		*/

		public const string experimentName = "segmented_dynamic_motd";

		public static void setDialogData(MOTDDialogData dialogData)
		{
			SegmentedDynamicMOTDExperiment exp = ExperimentManager.GetEosExperiment(experimentName) as SegmentedDynamicMOTDExperiment;
			if (exp != null)
			{
				dialogData.keyName = exp.keyName;
				dialogData.sortIndex = exp.sortIndex;
				dialogData.appearance = exp.appearance;
				dialogData.locTitle = exp.locTitle;
				dialogData.locBodyText = exp.locBodyText;
				dialogData.imageBackground = exp.imageBackground;
				dialogData.locAction1 = exp.locAction1;
				dialogData.commandAction1 = exp.commandAction1;
				dialogData.locAction2 = exp.locAction2;
				dialogData.commandAction2 = exp.commandAction2;
				dialogData.shouldShowAppEntry = exp.shouldShowAppEntry;
				dialogData.shouldShowVip = exp.shouldShowVip;
				dialogData.shouldShowRTL = exp.shouldShowRTL;
				dialogData.maxViews = exp.maxViews;
				dialogData.statName = exp.statName;
				dialogData.cooldown = exp.cooldown;
				dialogData.audioPackKey = exp.audioPackKey;
				dialogData.soundClose = exp.soundClose;
				dialogData.soundOpen = exp.soundOpen;
				dialogData.soundOk = exp.soundOk;
				dialogData.soundMusic = exp.soundMusic;
			}
			else
			{
				dialogData.keyName = "";
				dialogData.sortIndex = 99999;
				dialogData.appearance = "";
				dialogData.locTitle = "";
				dialogData.locBodyText = "";
				dialogData.imageBackground = "";
				dialogData.locAction1 = "";
				dialogData.commandAction1 = "";
				dialogData.locAction2 = "";
				dialogData.commandAction2 = "";
				dialogData.shouldShowAppEntry = false;
				dialogData.shouldShowVip = false;
				dialogData.shouldShowRTL = false;
				dialogData.maxViews = 0;
				dialogData.statName = "dynamic_motd";
				dialogData.cooldown = Common.SECONDS_PER_HOUR;

				// new audio pack stuff
				dialogData.audioPackKey = "";
				dialogData.soundClose = "";
				dialogData.soundOpen = "";
				dialogData.soundOk = "";
				dialogData.soundMusic = "";
			}

		}

		public static int uniqueId
		{
			get
			{
				return experimentData == null ? 0 : experimentData.uniqueId;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}

		public static SegmentedDynamicMOTDExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as SegmentedDynamicMOTDExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	// this is the old deprecated version just containing 'enabled' field, superseded by DailyChallengeQuest2
	public class DailyChallengeQuest
	{
		/* 
		DailyChallengeQuest - Controls surfacing of the Daily Challenege Lite feature
		https://eos.zynga.com/development/#/experiment/5003512/edit/sir_quest_daily_challenge_lite
		*/
		public const string experimentName = "quest_daily_challenge";


		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class Win10LiveTile : IResetGame
	{
		/* 
		Win10LiveTile - Controls params for the Windows 10 Live Tile feature
		https://eos.zynga.com/development/#/experiment/5002366/edit/hir_live_tiles
		*/

		public const string experimentName = "live_tiles";
	}

	//--------------------------------------------------------------------------------------------------

	// Use login data to instantiate the dialog, instead of EOS. Because login data only gets changed when the challenge ends.
	// But EOS data can change during the challenge.
	public class DailyChallengeQuest2
	{
		/* 
		DailyChallengeQuest2 - Controls params for the Daily Challenge Lite 2 feature
		https://eos.zynga.com/development/#/experiment/5003512/edit/sir_quest_daily_challenge_lite2
		*/

		public const string experimentName = "quest_daily_challenge_lite2";
		public static string bigWinMotdText
		{
			get
			{
				DailyChallengeQuest2Experiment exp = ExperimentManager.GetEosExperiment(experimentName) as DailyChallengeQuest2Experiment;
				if (exp != null)
				{
					return exp.bigWinMotdText;
				}
				return "";

			}
		}

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}

	}

	//--------------------------------------------------------------------------------------------------
	
	public class PushNotifSoftPrompt
	{
		public const string experimentName = "push_notif_soft_prompt";
		public static bool isIncentivizedPromptEnabled
		{
			get
			{
				return experimentData == null ? false : experimentData.incentivizedPromptEnabled;
			}
		}
		
		public static PushNotifSoftPromptExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as PushNotifSoftPromptExperiment;
			}
		}
		
		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static int maxViews
		{
			get
			{
				return experimentData == null ? 0 : experimentData.maxViews;
			}
		}

		public static int cooldown
		{
			get
			{
				return experimentData == null ? System.Int32.MaxValue : experimentData.cooldown;
			}
		}
	}
	
	//--------------------------------------------------------------------------------------------------

	/*
	Variants:
	1. Control - Not on
	2. constant_low Feature on low incentives, no reward escalations
	3. constant_high Feature on high incentives, no reward escalations
	4. escalating_low Feature on low incentives, reward escalations
	5. escalating_high Feature on high incentives, reward escalations
	*/

	public class IncentivizedInviteLite : IResetGame
	{
		public const string experimentName = "incentivized_invite_lite";
		public static int baseIncentiveAmount
		{
			get { return experimentData == null ? 0 : experimentData.baseIncentiveAmount; }
		}
		public static int rewardSchedule
		{
			get { return experimentData == null ? 0 : experimentData.rewardSchedule; }
		}
		public static int maxCollects
		{
			get { return experimentData == null ? 0 : experimentData.maxCollects; }
		}
		public static int maxEscalations
		{
			get { return experimentData == null ? 0 : experimentData.maxEscalations; }
		}
		public static int installsUntilNextEscalation
		{
			get { return experimentData == null ? 0 : experimentData.installsUntilNextEscalation; }
		}


		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static long getIncentiveAmountForNextInstall(int numInvites = 0)
		{
			int totalIncentiveAmount = baseIncentiveAmount;
			if (numInvites > 0 && rewardSchedule > 0 && installsUntilNextEscalation > 0)
			{
				int escalations = (numInvites - 1) / installsUntilNextEscalation;
				if (maxEscalations > 0)
				{
					escalations = Mathf.FloorToInt(Mathf.Min(escalations, maxEscalations));
				}
				int escalationIncentive = escalations * rewardSchedule;
				totalIncentiveAmount += escalationIncentive;
			}
			return totalIncentiveAmount;
		}

		public static IncentivizedInviteLiteExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as IncentivizedInviteLiteExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class HyperEconomy
	{
		/* 
			HyperEconomy - Controls the economy mulitplier on HIR (only). As well as whether to show the intro to the new economy.
		*/

		public const string experimentName = "hyper_economy";
		public static HyperEconomyExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as HyperEconomyExperiment;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		// Need this getter instead of a direct variable, to force experimentData to be set.
		public static bool isIntroEnabled
		{
			get
			{
				return (experimentData != null && experimentData.isIntroEnabled);
			}
		}

		public static bool isShowingRepricedVisuals
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment && experimentData.isShowingRepricedVisuals);
			}
		}
	}

	//---------------------------------------------------------------------------------------------------

	public class BuyPageVersionThree
	{
		/* 
			BuyPageVersionThree - Controls the surfacing of the updated buy page (and the buy page confirmation page with this)
		*/
		public const string experimentName = "buy_page_v3";

		public static string bannerImage
		{
			get
			{
				return experimentData == null ? "default" : experimentData.bannerImage;
			}
		}


		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static BuyPageVersionThreeExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as BuyPageVersionThreeExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class RobustChallengesEos
	{
		public const string experimentName = "challenge_campaigns";


		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}

		public static string variantName
		{
			get
			{
				return experimentData == null ? "" : experimentData.variantName;
			}
		}

		private static EosExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName);
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class LOZChallenges : IResetGame
	{
		public const string experimentName = "challenge_loz";
		public static int levelLock
		{
			get
			{
				return experimentData == null ? 0 : experimentData.levelLock;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static LOZChallengesExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as LOZChallengesExperiment;
			}
		}

	}

	//--------------------------------------------------------------------------------------------------
	public class LockedGamesOnInstall : IResetGame
	{
		// these are lola games that are unlocked by experiment
		// if the user is in the variant with enabled = false, the game is unlocked for all
		// if enabled = true, the game is locked by level set in scat
		public const string experimentName = "lola_unlock_by_level";

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}


	}

	//--------------------------------------------------------------------------------------------------

	/*
	Variants:
	1. Control - Not on
	2. Feature on
	*/

	public class DeluxeGames
	{

		public const string experimentName = "deluxe_games";

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class NativeMobileSharing
	{

		/* 
		   Variants:
		   enabled = false -- use the single package out of credits dialog.
		   enabled = true -- use the three package out of credits dialog (if configured).
		 */
		public const string experimentName = "mobile_native_sharing";

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------
	public class IncreaseBigSliceChance
	{
		/* 
		   Variants:
		   enabled = false -- no big slice frenzy
		   enabled = true -- enable big slice frenzy
		 */
		public const string experimentName = "big_slice_increase_chance";   //tbd should be big_slice_frenzy if SCAT is up to date?

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class BuyPageV2
	{
		public const string experimentName = "buy_page_v2";
		public static bool shouldShowNewArt
		{
			get
			{
				return experimentData == null ? false : experimentData.shouldShowNewArt;
			}
		}
		public static bool shouldShowNewNumbers
		{
			get
			{
				return experimentData == null ? false : experimentData.shouldShowNewNumbers;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static BuyPageV2Experiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as BuyPageV2Experiment;
			}
		}

	}

	//--------------------------------------------------------------------------------------------------

	public class VIPPhoneDialogSurfacing
	{
		public const string experimentName = "vip_phone_number";
		public static int coinRewardAmount
		{
			get
			{
				return experimentData == null ? 0 : experimentData.coinRewardAmount;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static VIPPhoneDialogSurfacingExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as VIPPhoneDialogSurfacingExperiment;
			}
		}

	}

	//--------------------------------------------------------------------------------------------------
	public class OutOfCoinsBuyPage
	{
		public const string experimentName = "ooc_buy_page";

		public static string variantName
		{
			get
			{
				return experimentData == null ? "" : experimentData.variantName;
			}
		}

		public static bool isEnabled
		{
			get
			{
				return experimentData != null && experimentData.isEnabled;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static bool shouldShowSale
		{
			get
			{
				return experimentData != null && experimentData.showSale;
			}
		}

		public static OutOfCoinsBuyPageExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as OutOfCoinsBuyPageExperiment; }
		}

		public static bool showIntermediaryDialog
		{
			get
			{
				// This is just a break between running out of coins and a sale dialog. So check if we're showing sale
				return (isInExperiment && experimentData.showIntermediaryDialog);
			}
		}
	}

	public class OutOfCoinsPriority
	{
		public const string experimentName = "out_of_coins_sale_priority";

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class LotteryDayTuning
	{
		public const string experimentName = "lottery_day_tuning";

		public static int levelLock
		{
			get
			{
				return experimentData == null ? 0 : experimentData.levelLock;
			}
		}

		public static string keyName
		{
			get
			{
				return experimentData == null ? "" : experimentData.keyName;
			}
		}

		public static string scaleFactor
		{
			get
			{
				return experimentData == null ? "" : experimentData.scaleFactor;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static LotteryDayTuningExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as LotteryDayTuningExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class WatchToEarn
	{
		public const string experimentName = "w2e_main";

		public static bool shouldShowDailyBonusCollect
		{
			get
			{
				return experimentData == null ? false : experimentData.shouldShowDailyBonusCollect;
			}
		}
		public static bool shouldShowOutOfCredits
		{
			get
			{
				return experimentData == null ? false : experimentData.shouldShowOutOfCredits;
			}
		}

		public static bool useUnityAds
		{
			get
			{
				return experimentData == null ? false : experimentData.useUnityAds;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static WatchToEarnExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as WatchToEarnExperiment;
			}
		}

	}

	//--------------------------------------------------------------------------------------------------

	public class SuperStreak
	{
		public const string experimentName = "super_streak";
		public static float multiplier
		{
			get
			{
				return experimentData == null ? 1 : experimentData.multiplier;
			}
		}

		public static bool isEnabled
		{
			get
			{
				return experimentData != null && experimentData.isEnabled;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static SuperStreakExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as SuperStreakExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class UnlockAllGames
	{
		public const string experimentName = "unlock_all_games";

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	/*
	Variants:
	1. Control - Not on
	2. booster_on - Enabled
	*/

	public class BuyPageBooster
	{
		public const string experimentName = "buy_page_booster";

		// Start Time of the experiment
		public static int startTimeInSecs
		{
			get
			{
				return experimentData == null ? 0 : experimentData.startTimeInSecs;
			}
		}
		// End Time of the experiment
		public static int endTimeInSecs
		{
			get
			{
				return experimentData == null ? 0 : experimentData.endTimeInSecs;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static BuyPageBoosterExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as BuyPageBoosterExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class PartnerPowerup
	{
		public const string experimentName = "co_op_challenge";
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}


	public class NetworkProfile : IResetGame
	{
		public const string experimentName = "loyalty_lounge_profile";
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
		
		public static bool activeDiscoveryEnabled
		{
			get
			{
				EueActiveDiscoveryExperiment exp = ExperimentManager.GetEosExperiment(experimentName) as EueActiveDiscoveryExperiment;
				return exp == null ? false : exp.activeDiscoveryEnabled;
			}
		}
		
		public static int activeDiscoveryLevel
		{
			get
			{
				EueActiveDiscoveryExperiment exp = ExperimentManager.GetEosExperiment(experimentName) as EueActiveDiscoveryExperiment;
				return exp == null ? 1 : exp.activeDiscoveryLevel;
			}
		}
	}

	//--------------------------------------------------------
	public class NewGameMOTDDialogGate
	{
		public const string experimentName = "new_game_dialog_gate";
		public static int unlockLevel
		{
			get
			{
				return experimentData == null ? 0 : experimentData.unlockLevel;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static NewGameMOTDDialogGateExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as NewGameMOTDDialogGateExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------
	public class SaleDialogLevelGate
	{
		public const string experimentName = "sale_dialog_popping_gate";

		private static bool playerMeetsLevelRequirement
		{
			get
			{
				return experimentData != null && experimentData.playerMeetsLevelRequirement;
			}

		}
		public static bool isLockingSaleDialogs
		{
			get
			{
				return (isInExperiment && !experimentData.playerMeetsLevelRequirement);
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static SaleDialogLevelGateExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as SaleDialogLevelGateExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class VIPLobbyRevamp
	{
		// use new lobby revamp assets for main lobby, and the VIP
		public const string experimentName = "vip_lobby_revamp";
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	/*
	Temporarily increases a users VIP level when using select features. Features controlled in EOS, See VIPStatusBoostEvent for more.
	*/
	public class VIPLevelUpEvent
	{
		public const string experimentName = "vip_status_boost";
		public static int startTime
		{
			get
			{
				return experimentData == null ? 0 : experimentData.startTime;
			}
		}
		public static int endTime
		{
			get
			{
				return experimentData == null ? 0 : experimentData.endTime;
			}
		}
		public static int boostAmount
		{
			get
			{
				return experimentData == null ? -1 : experimentData.boostAmount;
			}
			set
			{
				if (experimentData != null)
				{
					experimentData.boostAmount = value;
				}
			}
		}
		public static string featureList
		{
			get
			{
				return experimentData == null ? "" : experimentData.featureList;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static VIPLevelUpEventExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as VIPLevelUpEventExperiment;
			}
		}

	}

	public class WheelDeal
	{
		public const string experimentName = "wheel_deal";

		public static string keyName
		{
			get
			{
				return experimentData == null ? "" : experimentData.keyName;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static WheelDealExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as WheelDealExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	/*
	A special offer surface in the inbox
	*/
	public class GiftChestOffer
	{
		public const string experimentName = "gift_chest_offer";
		public static int cooldown
		{
			get
			{
				return experimentData == null ? int.MaxValue : experimentData.cooldown;
			}
		}
		public static int bonusPercent
		{
			get
			{
				return experimentData == null ? 0 : experimentData.bonusPercent;
			}
		}
		
		public static int maxViews
		{
			get
			{
				return experimentData == null ? 0 : experimentData.maxViews;
			}
		}

		public static string coinPackage
		{
			get
			{
				return experimentData == null ? "coin_package_1" : experimentData.package;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static GiftChestOfferExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as GiftChestOfferExperiment;
			}
		}

	}
	// -------------------------------------------------------------------------------------------------
	public class ZisPhase2
	{
		public const string experimentName = "zis_phase_2";

		public static bool isInExperiment
		{
			get
			{
				//return true;
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static ZisPhase2Experiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as ZisPhase2Experiment;
			}
		}
	}
	//--------------------------------------------------------------------------------------------------

	public class PersonalizedContent
	{
		public const string experimentName = "personalized_content";
		
		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}

		public static string getRecommandedGameName(string index)
		{
			return (experimentData != null) ? experimentData.getRecommendedGameName(index) : "";
		}
		
		public static string getFavoriateGameName(string index)
		{
			return (experimentData != null) ? experimentData.getFavoriateGameName(index) : "";
		}
		
		private static PersonalizedContentExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as PersonalizedContentExperiment;
			}
		}
	}

	public class RoyalRush
	{
		public const string experimentName = "royal_rush";

		public static bool isAutoSpinEnabled
		{
			get { return experimentData != null && experimentData.isAutoSpinEnabled; }
		}

		public static bool isPausingInCollections
		{
			get { return experimentData != null && experimentData.isPausingInCollections; }
		}

		public static bool isPausingInLevelUps
		{
			get { return experimentData != null && experimentData.isPausingInLevelUps; }
		}

		public static bool isPausingInQFC
		{
			get { return experimentData != null && experimentData.isPausingInQFC; }
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static RoyalRushExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as RoyalRushExperiment;
			}
		}

	}

	//--------------------------------------------------------------------------------------------------	
	public class SmartBetSelector
	{
		public const string experimentName = "smart_bet_selector";

		public static int jackpotModifier

		{
			get
			{
				return experimentData == null ? 0 : experimentData.jackpotModifier;
			}
		}
		public static int bigSliceModifier
		{
			get
			{
				return experimentData == null ? 0 : experimentData.bigSliceModifier;
			}
		}
		public static int mysteryModifier
		{
			get
			{
				return experimentData == null ? 0 : experimentData.mysteryModifier;
			}
		}

		public static int nonTopperModifier
		{
			get
			{
				return experimentData == null ? 1 : experimentData.nonTopperModifier;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static SmartBetSelectorExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as SmartBetSelectorExperiment;
			}
		}

		public static int[] jackpotIncrements
		{
			get
			{
				return experimentData == null ? new int[] { 1, 1, 1 } : experimentData.jackpotIncrements;
			}
		}

		public static int[] mysteryIncrements
		{
			get
			{
				return experimentData == null ? new int[] { 1, 1, 1 } : experimentData.mysteryIncrements;
			}
		}

		public static int[] bigSliceIncrements
		{
			get
			{
				return experimentData == null ? new int[] { 1, 1, 1 } : experimentData.bigSliceIncrements;
			}
		}
	}


	//--------------------------------------------------------------------------------------------------	
	public class FirstPurchaseOffer : IResetGame
	{
		public const string experimentName = "first_purchase_offer";
		public static bool didPurchase = false;

		private static FirstPurchaseOfferExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as FirstPurchaseOfferExperiment;
			}
		}

		public static List<FirstPurchaseOfferData> firstPurchaseOffersList
		{
			get
			{
				return experimentData == null ? new List<FirstPurchaseOfferData>() : experimentData.firstPurchaseOffersList;
			}
		}

		public static int bestSalePercent
		{
			get
			{
				return null == experimentData ? 0 : experimentData.bestSalePercent;

			}
		}


		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment && !didPurchase;
			}
		}

		public static void resetStaticClassData()
		{
			didPurchase = false;
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class NetworkAchievement
	{
		public const string experimentName = "network_achievement";

		public static int hirTrophyVersion
		{
			get
			{
				return experimentData == null ? 0 : experimentData.hirTrophyVersion;
			}
		}
		public static int bdcTrophyVersion
		{
			get
			{
				return experimentData == null ? 0 : experimentData.bdcTrophyVersion;
			}
		}

		public static int wozTrophyVersion
		{
			get
			{
				return experimentData == null ? 0 : experimentData.wozTrophyVersion;
			}
		}

		public static int wonkaTrophyVersion
		{
			get
			{
				return experimentData == null ? 0 : experimentData.wonkaTrophyVersion;
			}
		}
		public static int networkTrophyVersion
		{
			get
			{
				return experimentData == null ? 0 : experimentData.networkTrophyVersion;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static NetworkAchievementExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as NetworkAchievementExperiment;
			}
		}

		public static bool enableTrophyRewards
		{
			get
			{
				return experimentData == null ? false : experimentData.isInExperiment && experimentData.enableTrophyV15;
			}
		}
		
		public static bool activeDiscoveryEnabled
		{
			get
			{
				return experimentData == null ? false : experimentData.activeDiscoveryEnabled;
			}
		}
		
		public static int activeDiscoveryLevel
		{
			get
			{
				return experimentData == null ? 1 : experimentData.activeDiscoveryLevel;
			}
		}

	}

	//--------------------------------------------------------------------------------------------------

	public class OutOfCredits
	{
		public const string experimentName = "out_of_credits";
		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}

		public static PurchaseExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as PurchaseExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------			
	public class GlobalMaxWager
	{
		public const string experimentName = "global_max_wager";
		public static string variantName
		{
			get
			{
				return experimentData == null ? "" : experimentData.variantName;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static EosExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName);
			}
		}
	}

	//--------------------------------------------------------------------------------------------------
	public class NewDailyBonus : IResetGame
	{
		public const string experimentName = "new_daily_bonus";

		public static string bonusKeyName
		{
			get
			{
				return experimentData == null ? "none" : experimentData.bonusKeyName;
			}
		}

		public static int dailyStreakEndingReminder
		{
			get
			{
				return experimentData == null ? NewDailyBonusExperiment.DAILY_STREAK_REMINDER : experimentData.dailyStreakEndingReminder;
			}
		}

		public static string notifLocKeyDay1To6
		{
			get
			{
				return experimentData == null ? "" : experimentData.notifLocKeyDay1To6;
			}
		}

		public static string notifLocKeyDay7
		{
			get
			{
				return experimentData == null ? "" : experimentData.notifLocKeyDay7;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static NewDailyBonusExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as NewDailyBonusExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------
	public class DynamicBuyPageSurfacing : IResetGame
	{
		public const string experimentName = "dynamic_buy_page_surfacing";
		public static int cooldown
		{
			get
			{
				return experimentData == null ? 0 : experimentData.cooldown;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static DynamicBuyPageSurfacingExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as DynamicBuyPageSurfacingExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	/*
	Variants:
	1. Control - Not on
	2. Feature on
	*/

	public class GDPRHelpDialog
	{

		public const string experimentName = "gdpr_help_dialog";
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}


				return false;
			}
		}
	}
	//--------------------------------------------------------------------------------------------------
	public class SaleBubbleVisuals
	{
		public const string experimentName = "sale_bubble_visuals";
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------
	public class Slotventures
	{
		public const string experimentName = "challenge_slotventures";
		public static int levelLock
		{
			get
			{
				return experimentData == null ? 10 : experimentData.levelLock;
			}
		}

		public static bool isEUE
		{
			get
			{
				return experimentData == null ? false : experimentData.isEUE;
			}
		}

		public static bool useDirectToMachine
		{
			get
			{
				return experimentData == null ? false : experimentData.useDirectToMachine;
			}
		}

		public static bool useDirectToLobby
		{
			get
			{
				return experimentData == null ? false : experimentData.useDirectToLobby;
			}
		}

		public static int maxDirectToLobbyLoads
		{
			get
			{
				return experimentData == null ? 0 : experimentData.maxDirectToLobbyLoads;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}

		private static SlotventuresExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as SlotventuresExperiment;
			}
		}

		public static string videoUrl
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoUrl;
			}
		}

		public static string videoSummaryPath
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoSummaryPath;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class PostPurchaseChallenge
	{
		public const string experimentName = "challenge_post_purchase";

		private static PostPurchaseChallengeExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as PostPurchaseChallengeExperiment;
			}
		}

		public static bool isInExperiment
		{
			get { return experimentData != null && experimentData.isInExperiment; }
		}

		public static string theme
		{
			get
			{
				string currentTheme = experimentData != null ? experimentData.theme : "";

				//Default to Pinata theme if there isn't one currently set or if the bundle for the current theme doesn't exist on the client
				if (string.IsNullOrEmpty(currentTheme) || !AssetBundleManager.isValidBundle("post_purchase_challenge/" + currentTheme.ToLower()))
				{
					return "Pinata";
				}

				return currentTheme;
			}
		}

		public static string bannerInactivePath
		{
			get { return experimentData != null ? experimentData.bannerInactivePath : ""; }
		}

		public static string bannerActivePath
		{
			get { return experimentData != null ? experimentData.bannerActivePath : ""; }
		}

		public static int[] purchaseIndexBonusAmounts
		{
			get { return experimentData != null ? experimentData.purchaseIndexBonusAmounts : null; }
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class PopcornSale
	{
		public const string experimentName = "popcorn_sale";

		public static bool hasCardPackDropsConfigured
		{
			get { return experimentData != null && experimentData.hasCardPackDropsConfigured; }
		}

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					/*


					*/
					return exp.isInExperiment;
				}

				return false;
			}
		}

		public static PurchaseExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as PurchaseExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class BuyPage
	{
		public const string experimentName = "buy_page";
		public static bool hasCardPackDropsConfigured = false;
		public static bool isInExperiment

		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					/*
									int packageIterator = 1;
						string packageString = "package_{0}_collectible_pack";
						string compareString  = _experimentData.getEosVarWithDefault(string.Format(packageString, packageIterator), null);

						while (compareString != null)
						{
							if (compareString != "nothing")
							{
								hasCardPackDropsConfigured = true;
							}

							packageIterator++;
							compareString  = experimentData.getEosVarWithDefault(string.Format(packageString, packageIterator), null);
						}
					*/
					return exp.isInExperiment;
				}

				return false;
			}
		}

		public static PurchaseExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as PurchaseExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------
	public class IncentivizedUpdate : IResetGame
	{
		public const string experimentName = "incentivized_update";

		public static string minClient
		{
			get
			{
				return experimentData == null ? "" : experimentData.minClient;
			}
		}
		public static string iLink
		{
			get
			{
				return experimentData == null ? "" : experimentData.iLink;
			}
		}
		public static int coins
		{
			get
			{
				return experimentData == null ? 0 : experimentData.coins;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static IncentivizedUpdateExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as IncentivizedUpdateExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------
	public class LazyLoadBundles : IResetGame
	{
		public const string experimentName = "lazy_load_bundles";
		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static LazyLoadBundlesExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as LazyLoadBundlesExperiment;
			}
		}

		public static bool hasBundle(string bundleName)
		{
			return experimentData == null ? false : experimentData.hasBundle(bundleName);
		}
	}

	//--------------------------------------------------------------------------------------------------

	/*
	Experiment to control using the V2 version of the spin panel along with the dynamic reel scaling
	*/
	public class SpinPanelV2
	{
		public const string experimentName = "spin_panel_v2";
		private static bool autoSpinOptions = false;
		private static float _autoSpinHoldDuration = 3.0f;


		public static float autoSpinHoldDuration
		{
			get
			{
				return experimentData == null ? 3.0f : experimentData.autoSpinHoldDuration;
			}
		}

		public static float autoSpinTextCycleTime
		{
			get
			{
				return experimentData == null ? 5.0f : experimentData.autoSpinTextCycleTime;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		public static bool hasAutoSpinOptions
		{
			get
			{
				return experimentData != null && experimentData.autoSpinOptions;
			}
		}

		public static int[] autoSpinOptionsCount
		{
			get
			{
				return experimentData != null ? experimentData.getAutoSpinOptionsCount() : new int[0];
			}
		}

		private static SpinPanelV2Experiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as SpinPanelV2Experiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class CasinoFriends
	{
		public const string experimentName = "casino_friends";
		public static bool isInExperiment
		{
			get { return experimentData != null && experimentData.isInExperiment; }
		}

		private static CasinoFriendsExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as CasinoFriendsExperiment;
			}
		}

		public static bool activeDiscoveryEnabled
		{
			get
			{
				return experimentData == null ? false : experimentData.activeDiscoveryEnabled;
			}
		}
		
		public static int activeDiscoveryLevel
		{
			get
			{
				return experimentData == null ? 1 : experimentData.activeDiscoveryLevel;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------
	// Basically the same as starter pack, but the experiment has some settings for target audience...
	//
	public class LifecycleSales
	{
		public const string experimentName = "lifecycle_sales";

		public static string creditPackageName
		{
			get
			{
				return experimentData == null ? null : experimentData.creditPackageName;
			}
		}
		public static int bonusPercent
		{
			get
			{
				return experimentData == null ? 0 : experimentData.bonusPercent;
			}
		}
		public static int strikethroughAmount
		{
			get
			{
				return experimentData == null ? 0 : experimentData.strikethroughAmount;
			}
		}


		// HIR only
		public static string dialogImage
		{
			get
			{
				return experimentData == null ? "" : experimentData.dialogImage;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return (experimentData != null && experimentData.isInExperiment);
			}
		}

		private static LifeCycleSalesExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as LifeCycleSalesExperiment;
			}
		}


	}

	//--------------------------------------------------------------------------------------------------

	public class ReducedDailyBonusEvent
	{
		public const string experimentName = "reduced_bonus_collection";

		public static bool isInExperiment
		{
			get { return experimentData != null && experimentData.isInExperiment; }
		}

		private static EosExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName);
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class DynamicVideo : IResetGame
	{

		public const string experimentName = "dynamic_video_motd";

		public static string url
		{
			get { return experimentData == null ? "" : experimentData.url; }
		}

		public static string buttonText
		{
			get { return experimentData == null ? "" : experimentData.buttonText; }
		}

		public static string action
		{
			get { return experimentData == null ? "" : experimentData.action; }
		}

		public static string statName
		{
			get { return experimentData == null ? "" : experimentData.statName; }
		}

		public static int closeButtonDelay
		{
			get { return experimentData == null ? 0 : experimentData.closeButtonDelay; }
		}

		public static int skipButtonDelay
		{
			get { return experimentData == null ? 0 : experimentData.skipButtonDelay; }
		}

		public static string imagePath
		{
			get { return experimentData == null ? "" : experimentData.imagePath; }
		}

		private static DynamicVideoExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as DynamicVideoExperiment;
			}
		}


		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}

		public static int uniqueId
		{
			get { return experimentData == null ? 0 : experimentData.url.GetHashCode(); }
		}
	}

	//--------------------------------------------------------------------------------------------------
	public class WeeklyRace
	{
		public const string experimentName = "weekly_race";

		private static bool disableTextMask = false;
		private static bool dailyRivalShowInGame = false;
		private static bool hasDailyRivals = false;

		private static WeeklyRaceExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as WeeklyRaceExperiment; }
		}

		public static bool isInExperiment
		{
			get { return experimentData != null && experimentData.isInExperiment; }
		}

		public static bool isTextMaskEnabled
		{
			get { return experimentData != null && !experimentData.disableTextMask; }
		}

		public static bool showRivalMOTDInGame
		{
			get { return experimentData != null && experimentData.dailyRivalShowInGame; }
		}

		public static bool isDailyRivalsEnabled
		{
			get { return experimentData != null && experimentData.hasDailyRivals; }
		}
		
		public static bool activeDiscoveryEnabled
		{
			get { return experimentData == null ? false : experimentData.activeDiscoveryEnabled; }
		}
		
		public static int activeDiscoveryLevel
		{
			get { return experimentData == null ? 1 : experimentData.activeDiscoveryLevel; }
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class DynamicMotdV2
	{
		public const string experimentName = "dynamic_motd_v2";

		private static DynamicMotdV2Experiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as DynamicMotdV2Experiment;
			}
		}

		public static string config
		{
			get { return experimentData == null ? "" : experimentData.config; }
		}

		public static string variant
		{
			get { return experimentData == null ? "" : experimentData.variant; }
		}

		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------
	public class RepriceLevelUpSequence
	{
		public const string experimentName = "reprice_level_up_sequence";

		private static RepriceLevelUpSequenceExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as RepriceLevelUpSequenceExperiment; }
		}

		public static bool isInExperiment
		{
			get { return experimentData != null && experimentData.isInExperiment; }
		}

		public static int timeoutLength
		{
			get
			{
				return experimentData == null ? 0 : experimentData.timeoutLength;
			}
		}

		public static bool isToasterEnabled
		{
			get
			{
				return experimentData == null ? false : experimentData.useToaster;
			}
		}

		public static int toasterTimeout
		{
			get
			{
				return experimentData == null ? 0 : experimentData.toasterTimeout;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class RepriceVideo
	{
		public const string experimentName = "reprice_video";

		public static string url
		{
			get { return experimentData == null ? "" : experimentData.url; }
		}

		public static string buttonText
		{
			get { return experimentData == null ? "" : experimentData.buttonText; }
		}

		public static string action
		{
			get { return experimentData == null ? "" : experimentData.action; }
		}

		public static string statName
		{
			get { return experimentData == null ? "" : experimentData.statName; }
		}

		public static int closeButtonDelay
		{
			get { return experimentData == null ? 0 : experimentData.closeButtonDelay; }
		}

		public static int skipButtonDelay
		{
			get { return experimentData == null ? 0 : experimentData.skipButtonDelay; }
		}

		public static string imagePath
		{
			get { return experimentData == null ? "" : experimentData.imagePath; }
		}
		private static DynamicVideoExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as DynamicVideoExperiment;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}
	}
	//-------------------------------------------------------------------------------------------------
	public class OneClickBuy
	{
		public const string experimentName = "one_click_buy";

		public static PurchaseExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as PurchaseExperiment;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}
	}
	//--------------------------------------------------------------------------------------------------

	public class WelcomeJourney
	{
		public const string experimentName = "welcome_journey";

		public static List<int> rewards
		{
			get
			{

				if (isInExperiment && experimentData.dailyRewards != null)
				{
					//copy list so user can't manipulate the data
					return new List<int>(experimentData.dailyRewards);
				}

				return null;
			}
		}

		private static WelcomeJourneyExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as WelcomeJourneyExperiment;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}

		public static bool isLapsedPlayer
		{
			get
			{
				return experimentData != null && (experimentData.isLapsed || CustomPlayerData.getBool("was_welcome_back_lapsed", false));
			}
		}
	}

	//--------------------------------------------------------------------------------------------------
	// Control to modulate rollup and reel stop time values
	//
	// Values < 100 for each will lead to faster rollup and stop times 
	public class SpinTime
	{
		public const string experimentName = "spin_time";
		private static SpinTimeExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as SpinTimeExperiment;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}

		public static int reelStopTimePercentage
		{
			get { return experimentData == null ? 100 : experimentData.reelStopTimePercentage; }
		}

		public static int rollupTimePercentage
		{
			get { return experimentData == null ? 100 : experimentData.rollupTimePercentage; }
		}

		public static float rollupTimeModifier
		{
			get
			{
				return experimentData == null ? 0 : experimentData.rollupTimeModifier;
			}
		}

		public static float payoutRatioModifier
		{
			get
			{
				return experimentData == null ? 0 : experimentData.payoutRatioModifier;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class BuyPageHyperlink
	{
		private const string LINK_FORMAT = " <link=\"cs_url:{0}\">{1}</link>";
		public const string experimentName = "buy_page_hyperlink";

		private static BuyPageHyperlinkExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as BuyPageHyperlinkExperiment;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}

		public static string getLinkForBuyPage()
		{
			BuyPageHyperlinkExperiment expData = experimentData;
			if (expData != null)
			{
				return string.Format(LINK_FORMAT, expData.buyLink, expData.buyText);
			}

			return "";
		}

		public static string getLinkForPopcornSale()
		{
			BuyPageHyperlinkExperiment expData = experimentData;
			if (expData != null)
			{
				return string.Format(LINK_FORMAT, expData.popcornLink, expData.popcornText);
			}

			return "";
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class DailyBonusNewInstall
	{
		public const string experimentName = "daily_bonus_new_install";

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class VideoSoundToggle
	{
		public const string experimentName = "video_sound_toggle";
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class DialogTransitions
	{
		public const string experimentName = "dialog_transitions";
		public static DialogTransitionExperiment getExperimentData()
		{
			return ExperimentManager.GetEosExperiment(experimentName) as DialogTransitionExperiment;
		}

	}

	//--------------------------------------------------------------------------------------------------

	public class Powerups
	{
		public const string experimentName = "powerups";
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class FBAuthDialog
	{
		public const string experimentName = "auth_dialog_popup";

		private static FBAuthDialogExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as FBAuthDialogExperiment; }
		}

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}

		//On which login count should we show the FB login dialog
		//For eg. if autopopLobbyVisitCount = 2, then the second time the user loads into the game,
		//the FB login dialog is shown during loading into the app
		public static int autopopLobbyVisitCount
		{
			get { return experimentData == null ? 1 : experimentData.autopopLoginCount; }
		}

		//Number of consecutive logins should we show the FB login dialog
		//For eg. if sessionFrequency=2, we show the FB login in the next two logins if the
		//player hasn't connected to FB yet
		public static int sessionFrequency
		{
			get { return experimentData == null ? 1 : experimentData.sessionFrequency; }
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class FlashSale
	{
		public const string experimentName = "flash_sale";
		public const int FLASH_SALE_DEFAULT_STARTING_PACKAGE_COUNT = 1000;
		public const int FLASH_SALE_DEFAULT_DURATION = 120;
		public const float FLASH_SALE_DEFAULT_SPEED_PARAMETER = 0.8f;
		public const int FLASH_SALE_DEFAULT_COOLDOWN = 60;
		public const int FLASH_SALE_DEFAULT_BONUES_PERCENTAGE = 30;
		public const int FLASH_SALE_DEFAULT_MIN_WAIT_TIME = 10;
		public const int FLASH_SALE_DEFAULT_MAX_WAIT_TIME = 20;
		public const string FLASH_SALE_DEFAULT_COIN_PACKAGE = "coin_package_5";
		public const bool FLASH_SALE_DEFAULT_9_TO_10_FILTER = false;

		private static FlashSaleExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as FlashSaleExperiment; }
		}
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
		public static int startingPackageCount
		{
			get { return experimentData == null ? FLASH_SALE_DEFAULT_STARTING_PACKAGE_COUNT : experimentData.startingPackageCount; }
		}
		public static bool enabled
		{
			get { return experimentData == null ? false : experimentData.enabled; }
		}
		public static int duration
		{
			get { return experimentData == null ? FLASH_SALE_DEFAULT_DURATION : experimentData.duration; }
		}
		public static float speedParameter
		{
			get { return experimentData == null ? FLASH_SALE_DEFAULT_SPEED_PARAMETER : experimentData.speedParameter; }
		}
		public static int cooldown
		{
			get { return experimentData == null ? FLASH_SALE_DEFAULT_COOLDOWN : experimentData.cooldown; }
		}
		public static int bonusPercentage
		{
			get { return experimentData == null ? FLASH_SALE_DEFAULT_BONUES_PERCENTAGE : experimentData.bonusPercentage; }
		}
		public static int minWaitTime
		{
			get { return experimentData == null ? FLASH_SALE_DEFAULT_MIN_WAIT_TIME : experimentData.minWaitTime; }
		}
		public static int maxWaitTime
		{
			get { return experimentData == null ? FLASH_SALE_DEFAULT_MAX_WAIT_TIME : experimentData.maxWaitTime; }
		}
		public static string package
		{
			get { return experimentData == null ? FLASH_SALE_DEFAULT_COIN_PACKAGE : experimentData.package; }
		}
		public static bool filterNineAmToTenPm
		{
			get { return experimentData == null ? FLASH_SALE_DEFAULT_9_TO_10_FILTER : experimentData.filterNineAmToTenPm; }
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class StreakSale
	{
		public const string experimentName = "streak_sale";

		public static StreakSaleExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as StreakSaleExperiment; }
		}
		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}

		public static bool enabled
		{
			get { return experimentData == null ? false : experimentData.enabled; }
		}

		public static string configKey
		{
			get { return experimentData == null ? "" : experimentData.configKey; }
		}

		public static int startTime
		{
			get { return experimentData == null ? int.MaxValue : experimentData.startTime; }
		}

		public static int endTime
		{
			get { return experimentData == null ? int.MaxValue : experimentData.endTime; }
		}

		public static string bottomText
		{
			get { return experimentData == null ? "" : experimentData.bottomText; }
		}

		public static string bgImagePath
		{
			get { return experimentData == null ? "" : experimentData.bgImagePath; }
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class Zis
	{
		public const string experimentName = "zis";

		private static ZisExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as ZisExperiment; }
		}

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}
	}


	//--------------------------------------------------------------------------------------------------

	public static class QuestForTheChest
	{
		public const string experimentName = "quest_for_the_chest";

		private static QFCExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as QFCExperiment;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}

		public static string videoUrl
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoUrl;
			}
		}

		public static string videoSummaryPath
		{
			get
			{
				return experimentData == null ? "" : string.Format(experimentData.videoSummaryPath, themeWithoutSpaces);
			}
		}

		public static string theme
		{
			get
			{
				string currentTheme = experimentData == null ? "" : experimentData.theme;

				//Default to Candy theme if there isn't one currently set or if the bundle for the current theme doesn't exist on the client
				//Using Candy Kingdom because it was the first theme and should exist on any client that supports the feature
				string bundleName = currentTheme.Replace(' ', '_'); //Remove this once we update existing theme names with spaces in them to not have them, and no longer use spaces in theme names.

				if (string.IsNullOrEmpty(currentTheme) || !AssetBundleManager.isValidBundle("quest_for_the_chest/" + bundleName))
				{
					return "candy kingdom";
				}

				return currentTheme;
			}
		}

		public static string themeWithoutSpaces
		{
			get
			{
				return System.Text.RegularExpressions.Regex.Replace(theme, @"\s+", "");
			}
		}

		public static string toasterTimeTrigger
		{
			get
			{
				return experimentData == null ? "" : experimentData.toasterTimeTrigger;
			}
		}

		public static string toasterKeyTrigger
		{
			get
			{
				return experimentData == null ? "" : experimentData.toasterKeyTrigger;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public static class CCPA
	{
		public const string experimentName = "ccpa_all_games";
		private static readonly HashSet<string> validVariants;
		static CCPA()
		{
			validVariants = new HashSet<string>();
			validVariants.Add("OptedOut");
			validVariants.Add("OptedIn");
			validVariants.Add("InCaNoStatus");
		}

		private static EosExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as EosExperiment;
			}
		}

		public static string getLocKey()
		{
			if (isEnabled())
			{
				switch (experimentData.variantName)
				{
					case "OptedOut":
						return "ccpa_data_request_opted_out";


					default:
						return "ccpa_data_request";

				}
			}

			return "";
		}

		public static bool isEnabled()
		{
			return ExperimentManager.ccpaStatusInitialized && experimentData != null && validVariants.Contains(experimentData.variantName);
		}
	}

	public static class RichPass
	{
		public const string experimentName = "rich_pass";

		public static RichPassExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as RichPassExperiment; }
		}

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}
				return false;
			}
		}

		public static string videoUrl
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoUrl;
			}
		}

		public static string package
		{
			get { return experimentData == null ? "" : experimentData.packageKey; }
		}

		public static string videoSummaryPath
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoSummaryPath;
			}
		}

		public static int passValueAmount
		{
			get
			{
				return experimentData == null ? 0 : experimentData.passValueAmount;
			}
		}

		public static bool showInGameCounter(string gameKey)
		{
			return (experimentData != null && experimentData.shouldDisplayCounterinGame(gameKey));
		}

		public static string dialogBgPath()
		{
			return experimentData == null ? "" : experimentData.dialogBgPath;
		}
	}

	//--------------------------------------------------------------------------------------------------

	public static class ElitePass
	{
		public const string experimentName = "elite_pass";

		public static ElitePassExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as ElitePassExperiment; }
		}

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}
				return false;
			}
		}

		public static bool showSpinsInToaster
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (experimentData != null)
				{
					return experimentData.showSpinsInToaster;
				}

				return true;
			}
		}

		public static string videoUrl
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoUrl;
			}
		}

		public static string videoSummaryPath
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoSummaryPath;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public class BuyPageDrawer
	{
		public const string experimentName = "buypage_drawer";

		public static BuyPageDrawerExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as BuyPageDrawerExperiment; }
		}

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}
				return false;
			}
		}

		public static List<int> delays
		{
			get
			{
				return experimentData == null ? null : experimentData.delaysList;
			}
		}

		public static string[] priorityList
		{
			get
			{
				return experimentData == null ? new string[0] : experimentData.priorityOrderList;
			}
		}

		public static int maxItemsToRotate
		{
			get
			{
				return experimentData == null ? 0 : experimentData.maxItemsToRotate;
			}
		}
	}

	public static class PremiumSlice
	{
		public const string experimentName = "premium_slice";

		public static PremiumSliceExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as PremiumSliceExperiment; }
		}

		public static long cooldownHours
		{
			get { return experimentData == null ? -1 : experimentData.cooldownHours; }
		}

		public static long cooldownDailyBonusSpins
		{
			get { return experimentData == null ? -1 : experimentData.cooldownDailySpins; }
		}
		
		public static bool showPriceUnderCTAButton
		{
			get { return experimentData == null ? false : experimentData.showPriceUnderCTAButton; }
		}

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}
				return false;
			}
		}
	}

	public static class PrizePop
	{
		public const string experimentName = "prize_pop";

		public static PrizePopExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as PrizePopExperiment; }
		}

		public static int startTime
		{
			get { return experimentData == null ? int.MaxValue : experimentData.startTime; }
		}

		public static int endTime
		{
			get { return experimentData == null ? int.MaxValue : experimentData.endTime; }
		}

		public static int startPicks
		{
			get { return experimentData == null ? 0 : experimentData.startPicks; }
		}

		public static string videoUrl
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoUrl;
			}
		}

		public static string videoSummaryPath
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoSummaryPath;
			}
		}

		public static string theme
		{
			get
			{
				string currentTheme = experimentData == null ? "" : experimentData.theme;

#if !ZYNGA_PRODUCTION
				if (Data.debugMode)
				{
					currentTheme = SlotsPlayer.getPreferences().GetString(DebugPrefs.PRIZE_POP_THEME_OVERRIDE, currentTheme);
				}
#endif

				//Default to Carnival theme if there isn't one currently set or if the bundle for the current theme doesn't exist on the client
				//Using Carnival because it was the first theme and should exist on any client that supports the feature
				if (string.IsNullOrEmpty(currentTheme) || !AssetBundleManager.isValidBundle("features/prize_pop/" + currentTheme.ToLower()))
				{
					return "Carnival";
				}

				return currentTheme;
			}
		}

		public static int endingSoonTrigger
		{
			get
			{
				return experimentData == null ? 0 : experimentData.endingSoonTrigger;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}
				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public static class EUEFeatureUnlocks
	{
		public const string experimentName = "eue_feature_unlocks";

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}
				return false;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------

	public static class EueFtue
	{
		public const string experimentName = "eue_challenge_slotventures";

		public static EueFtueExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as EueFtueExperiment; }
		}

		public static int maxLevel
		{
			get
			{
				return experimentData == null ? 5 : experimentData.maxLevel;
			}
		}
		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------
	public static class LevelLotto
	{
		public const string experimentName = "lotto_blast";

		public static LottoBlastExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as LottoBlastExperiment;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName);
				if (exp != null)
				{
					return exp.isInExperiment;
				}
				return false;
			}
		}

		public static string buyPageBannerPath
		{
			get
			{
				return experimentData != null ? experimentData.buyPageBannerPath : "";
			}
		}

		public static bool[] showBuffOnPackages
		{
			get
			{
				return experimentData != null ? experimentData.showTripleXpBuff : new[] { false, false, false, false, false, false };
			}
		}
		
		public static LottoBlastExperiment.DialogCloseAction dialogCloseAction
		{
			get
			{
				return experimentData != null ? experimentData.dialogCloseAction : LottoBlastExperiment.DialogCloseAction.NONE;
			}
		}

		public static int tripleXPDuration
		{
			get
			{
				return experimentData != null ? experimentData.tripleXPDuration : 0;
			}
		}

		public static string buffKeyname
		{
			get
			{
				return experimentData != null ? experimentData.buffKeyname : "";
			}
		}
	}
	//--------------------------------------------------------------------------------------------------

	public static class Collections
	{
		public const string experimentName = "collections";

		public static EosVideoExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as EosVideoExperiment; }
		}

		public static bool isInExperiment
		{
			get
			{
				EosExperiment exp = ExperimentManager.GetEosExperiment(experimentName) as EosVideoExperiment;
				if (exp != null)
				{
					return exp.isInExperiment;
				}
				return false;
			}
		}

		public static string videoUrl
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoUrl;
			}
		}

		public static string videoSummaryPath
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoSummaryPath;
			}
		}
	}
	
	//--------------------------------------------------------------------------------------------------
	public class iOSPrompt
	{
		public const string experimentName = "ios_app_tracking_prompt";
		
		public static iOSPromptExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as iOSPromptExperiment; }
		}

		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}

		public static bool pollInClient
		{
			get
			{
				return experimentData != null && experimentData.pollInClient;
			}
			
		}
	}

	//--------------------------------------------------------------------------------------------------
	public class IDFASoftPrompt
	{
		public const string experimentName = "idfa_soft_prompt";
		
		public static IDFASoftPromptExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as IDFASoftPromptExperiment; }
		}

		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}

		public static bool showSoftPrompt
		{
			get
			{
				return experimentData != null && experimentData.showSoftPrompt;
			}
		}
		
		public static bool showLocationEntry
		{
			get
			{
				return experimentData != null && experimentData.showLocationEntry;
			}
		}
		
		public static bool showLocationW2E
		{
			get
			{
				return experimentData != null && experimentData.showLocationW2E;
			}
		}
		
		public static int softPromptMaxViews
		{
			get
			{
				return experimentData != null ? experimentData.softPromptMaxViews : 0;
			}
		}
		
		public static int showEntryCoolDown
		{
			get
			{
				return experimentData != null ? experimentData.showEntryCoolDown : 0;
			}
		}
		
		public static int showW2ECoolDown
		{
			get
			{
				return experimentData != null ? experimentData.showW2ECoolDown : 0;
			}
		}
	}
	//--------------------------------------------------------------------------------------------------

	public static class VirtualPets
	{
		public const string experimentName = "virtual_pet";

		public static VirtualPetsExperiment experimentData
		{
			get { return ExperimentManager.GetEosExperiment(experimentName) as VirtualPetsExperiment; }
		}
		
		public static string videoUrl
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoUrl;
			}
		}

		public static string videoSummaryPath
		{
			get
			{
				return experimentData == null ? "" : experimentData.videoSummaryPath;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				VirtualPetsExperiment exp = ExperimentManager.GetEosExperiment(experimentName) as VirtualPetsExperiment;
				if (exp != null)
				{
					return exp.isInExperiment;
				}

				return false;
			}
		}

		public static SortedDictionary<int, string> specialTreatPrices
		{
			get { return experimentData == null ? null : experimentData.specialTreatPrices; }
		}
		
		public static string[] treatsOrder
		{
			get { return experimentData == null ? new string[0] : experimentData.treatOrder; }
		}

		public static int idleTime
		{
			get { return experimentData == null ? 10 : experimentData.idleTime; }
		}
	}
	
	//--------------------------------------------------------------------------------------------------
	public static class BundleSale
	{
		public const string experimentName = "bundle_sale";
		
		public static BundleSaleExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as BundleSaleExperiment;
			}
		}

		public static bool isInExperiment
		{
			get
			{
				return experimentData != null && experimentData.isInExperiment;
			}
		}

		public static string bundleId
		{
			get
			{
				return experimentData != null ? experimentData.bundleId : "";
			}
		}
	}
	
	//--------------------------------------------------------------------------------------------------
	public static class SpecialOutOfCoins
	{
		public const string experimentName = "special_ooc";
		
		public static SpecialOutOfCoinsExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(experimentName) as SpecialOutOfCoinsExperiment;
			}
		}
	}
	
	//--------------------------------------------------------------------------------------------------
	public static class BoardGame
	{
		public static BoardGameExperiment experimentData
		{
			get
			{
				return ExperimentManager.GetEosExperiment(BoardGameExperiment.experimentName) as BoardGameExperiment;
			}
		}
	}
}
