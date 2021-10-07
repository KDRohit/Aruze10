//
//  @file      ExperimentManager.cs
//  @authors    Shivanand PB <spb@zynga.com>
//				Nick Reynolds <nreynolds@zynga.com>

//  Class that holds manages experiment defaults,
//  pulls experiments from DAPI. This also handles
//  experiment overrides that are set with admin tool.

using System;
using System.Collections.Generic;
using UnityEngine;
using Com.HitItRich.EUE;
using Com.HitItRich.Feature.BundleSale;
using Com.HitItRich.Feature.VirtualPets;
using PrizePop;
using QuestForTheChest;

public class ExperimentManager : IResetGame, IDependencyInitializer
{
	// Player Experiments are sent down from HiR Server.
	private static Dictionary<string, int> _playerExperiments;

	// Player Experiments are sent down from HiR Server.
	private static Dictionary<string, EosExperiment> _eosExperiments;
	

	private static void resetExperiments()
	{
		_eosExperiments = new Dictionary<string, EosExperiment>()
		{
			//Please maintain alphabetical order
			//if your experiment just has an enabled variable you don't create a new class (just use EosExperiment())
			{ ExperimentWrapper.AgeGate.experimentName, 					new AgeGateExperiment(ExperimentWrapper.AgeGate.experimentName) },
			{ BoardGameExperiment.experimentName, 					        new BoardGameExperiment(BoardGameExperiment.experimentName) },
			{ ExperimentWrapper.BuyPage.experimentName, 					new PurchaseExperiment(ExperimentWrapper.BuyPage.experimentName) },
			{ ExperimentWrapper.BuyPageProgressive.experimentName, 			new EosExperiment(ExperimentWrapper.BuyPageProgressive.experimentName) },
			{ ExperimentWrapper.BuyPageBooster.experimentName, 				new BuyPageBoosterExperiment(ExperimentWrapper.BuyPageBooster.experimentName) },
			{ ExperimentWrapper.BuyPageDrawer.experimentName, 				new BuyPageDrawerExperiment(ExperimentWrapper.BuyPageDrawer.experimentName) },
			{ ExperimentWrapper.BuyPageHyperlink.experimentName,			new BuyPageHyperlinkExperiment(ExperimentWrapper.BuyPageHyperlink.experimentName) },
			{ ExperimentWrapper.BuyPageV2.experimentName, 					new BuyPageV2Experiment(ExperimentWrapper.BuyPageV2.experimentName) },
			{ ExperimentWrapper.BuyPageVersionThree.experimentName, 		new BuyPageVersionThreeExperiment(ExperimentWrapper.BuyPageVersionThree.experimentName) },
			{ ExperimentWrapper.BundleSale.experimentName,                  new BundleSaleExperiment(ExperimentWrapper.BundleSale.experimentName)},
			{ ExperimentWrapper.CasinoFriends.experimentName, 				new CasinoFriendsExperiment(ExperimentWrapper.CasinoFriends.experimentName) },
			{ ExperimentWrapper.CCPA.experimentName,                        new EosExperiment(ExperimentWrapper.CCPA.experimentName) },
			{ ExperimentWrapper.Collections.experimentName,                 new EosVideoExperiment(ExperimentWrapper.Collections.experimentName) },
			{ ExperimentWrapper.DailyBonusNewInstall.experimentName,		new EosExperiment(ExperimentWrapper.DailyBonusNewInstall.experimentName) },
			{ ExperimentWrapper.DailyChallengeQuest.experimentName, 		new EosExperiment(ExperimentWrapper.DailyChallengeQuest.experimentName) },
			{ ExperimentWrapper.DailyChallengeQuest2.experimentName, 		new DailyChallengeQuest2Experiment(ExperimentWrapper.DailyChallengeQuest2.experimentName) },
			{ ExperimentWrapper.DeluxeGames.experimentName,					new EosExperiment(ExperimentWrapper.DeluxeGames.experimentName) },
			{ ExperimentWrapper.DialogTransitions.experimentName,			new DialogTransitionExperiment(ExperimentWrapper.DialogTransitions.experimentName) },
			{ ExperimentWrapper.DynamicBuyPageSurfacing.experimentName, 	new DynamicBuyPageSurfacingExperiment(ExperimentWrapper.DynamicBuyPageSurfacing.experimentName)},
			{ ExperimentWrapper.DynamicVideo.experimentName,				new DynamicVideoExperiment(ExperimentWrapper.DynamicVideo.experimentName) },
			{ ExperimentWrapper.DynamicMotdV2.experimentName,				new DynamicMotdV2Experiment(ExperimentWrapper.DynamicMotdV2.experimentName) },
			{ ExperimentWrapper.ElitePass.experimentName,				    new ElitePassExperiment (ExperimentWrapper.ElitePass.experimentName) },
			{ ExperimentWrapper.EUEFeatureUnlocks.experimentName,			new EosExperiment (ExperimentWrapper.EUEFeatureUnlocks.experimentName) },
			{ ExperimentWrapper.EueFtue.experimentName,						new EueFtueExperiment(ExperimentWrapper.EueFtue.experimentName) },
			{ ExperimentWrapper.FBAuthDialog.experimentName, 			    new FBAuthDialogExperiment(ExperimentWrapper.FBAuthDialog.experimentName) },
			{ ExperimentWrapper.Zis.experimentName,               			new ZisExperiment(ExperimentWrapper.Zis.experimentName) },
			{ ExperimentWrapper.FirstPurchaseOffer.experimentName, 			new FirstPurchaseOfferExperiment(ExperimentWrapper.FirstPurchaseOffer.experimentName) },
			{ ExperimentWrapper.FlashSale.experimentName,					new FlashSaleExperiment(ExperimentWrapper.FlashSale.experimentName) },
			{ ExperimentWrapper.Ftue.experimentName, 						new FtueExperiment(ExperimentWrapper.Ftue.experimentName) },
			{ ExperimentWrapper.GDPRHelpDialog.experimentName, 				new EosExperiment(ExperimentWrapper.GDPRHelpDialog.experimentName) },
			{ ExperimentWrapper.GiftChestOffer.experimentName, 				new GiftChestOfferExperiment(ExperimentWrapper.GiftChestOffer.experimentName) },
			{ ExperimentWrapper.GlobalMaxWager.experimentName, 				new EosExperiment(ExperimentWrapper.GlobalMaxWager.experimentName) },
			{ ExperimentWrapper.HyperEconomy.experimentName, 				new HyperEconomyExperiment(ExperimentWrapper.HyperEconomy.experimentName) },
			{ ExperimentWrapper.IDFASoftPrompt.experimentName, 				new IDFASoftPromptExperiment(ExperimentWrapper.IDFASoftPrompt.experimentName) },
			{ ExperimentWrapper.IncentivizedInviteLite.experimentName, 		new IncentivizedInviteLiteExperiment(ExperimentWrapper.IncentivizedInviteLite.experimentName) },
			{ ExperimentWrapper.IncentivizedUpdate.experimentName, 			new IncentivizedUpdateExperiment(ExperimentWrapper.IncentivizedUpdate.experimentName) },
			{ ExperimentWrapper.IncreaseBigSliceChance.experimentName, 		new EosExperiment(ExperimentWrapper.IncreaseBigSliceChance.experimentName) },
			{ ExperimentWrapper.iOSPrompt.experimentName,					new iOSPromptExperiment(ExperimentWrapper.iOSPrompt.experimentName) },
			{ ExperimentWrapper.LazyLoadBundles.experimentName, 			new LazyLoadBundlesExperiment(ExperimentWrapper.LazyLoadBundles.experimentName) },
			{ ExperimentWrapper.LevelLotto.experimentName, 			        new LottoBlastExperiment(ExperimentWrapper.LevelLotto.experimentName) },
			{ ExperimentWrapper.LifecycleSales.experimentName, 				new LifeCycleSalesExperiment(ExperimentWrapper.LifecycleSales.experimentName) },
			{ ExperimentWrapper.LinkedVipNetwork.experimentName, 			new EosExperiment(ExperimentWrapper.LinkedVipNetwork.experimentName) },
			{ ExperimentWrapper.LoadGameFTUE.experimentName, 				new LoadGameFTUEExperiment(ExperimentWrapper.LoadGameFTUE.experimentName) },
			{ ExperimentWrapper.LobbyV2.experimentName, 					new EosExperiment(ExperimentWrapper.LobbyV2.experimentName) },
			{ ExperimentWrapper.LobbyV3.experimentName, 					new LobbyV3Experiment(ExperimentWrapper.LobbyV3.experimentName) },
			{ ExperimentWrapper.LockedGamesOnInstall.experimentName, 		new EosExperiment(ExperimentWrapper.LockedGamesOnInstall.experimentName) },
			{ ExperimentWrapper.LotteryDayTuning.experimentName, 			new LotteryDayTuningExperiment(ExperimentWrapper.LotteryDayTuning.experimentName) },
			{ ExperimentWrapper.LOZChallenges.experimentName, 				new LOZChallengesExperiment(ExperimentWrapper.LOZChallenges.experimentName) },
			{ ExperimentWrapper.MobileToMobileXPromo.experimentName, 		new MobileToMobileXPromoExperiment(ExperimentWrapper.MobileToMobileXPromo.experimentName) },
			{ ExperimentWrapper.NativeMobileSharing.experimentName, 		new EosExperiment(ExperimentWrapper.NativeMobileSharing.experimentName) },
			{ ExperimentWrapper.NeedCreditsThreeOptions.experimentName, 	new EosExperiment(ExperimentWrapper.NeedCreditsThreeOptions.experimentName) },
			{ ExperimentWrapper.NetworkAchievement.experimentName, 			new NetworkAchievementExperiment(ExperimentWrapper.NetworkAchievement.experimentName) },
			{ ExperimentWrapper.NetworkProfile.experimentName, 				new EueActiveDiscoveryExperiment(ExperimentWrapper.NetworkProfile.experimentName) },
			{ ExperimentWrapper.NewDailyBonus.experimentName, 				new NewDailyBonusExperiment(ExperimentWrapper.NewDailyBonus.experimentName) },
			{ ExperimentWrapper.NewGameMOTDDialogGate.experimentName, 		new NewGameMOTDDialogGateExperiment(ExperimentWrapper.NewGameMOTDDialogGate.experimentName) },
			{ ExperimentWrapper.OutOfCoinsBuyPage.experimentName, 			new OutOfCoinsBuyPageExperiment(ExperimentWrapper.OutOfCoinsBuyPage.experimentName) },
			{ ExperimentWrapper.OutOfCredits.experimentName, 				new PurchaseExperiment(ExperimentWrapper.OutOfCredits.experimentName) },
			{ ExperimentWrapper.OutOfCoinsPriority.experimentName, 			new EosExperiment(ExperimentWrapper.OutOfCoinsPriority.experimentName)},
			{ ExperimentWrapper.PartnerPowerup.experimentName, 				new EosExperiment(ExperimentWrapper.PartnerPowerup.experimentName) },
			{ ExperimentWrapper.PersonalizedContent.experimentName, 		new PersonalizedContentExperiment(ExperimentWrapper.PersonalizedContent.experimentName) },
			{ ExperimentWrapper.PopcornSale.experimentName, 				new PurchaseExperiment(ExperimentWrapper.PopcornSale.experimentName) },
			{ ExperimentWrapper.PopcornVariantTest.experimentName, 			new PopcornVariantExperiment(ExperimentWrapper.PopcornVariantTest.experimentName) },
			{ ExperimentWrapper.Powerups.experimentName, 			        new EosExperiment(ExperimentWrapper.Powerups.experimentName) },
			{ ExperimentWrapper.PostPurchaseChallenge.experimentName,       new PostPurchaseChallengeExperiment(ExperimentWrapper.PostPurchaseChallenge.experimentName) },
			{ ExperimentWrapper.PathToRiches.experimentName, 				new EosExperiment(ExperimentWrapper.PathToRiches.experimentName) },
			{ ExperimentWrapper.PremiumSlice.experimentName,				new PremiumSliceExperiment(ExperimentWrapper.PremiumSlice.experimentName) },
			{ ExperimentWrapper.PrizePop.experimentName, 					new PrizePopExperiment(ExperimentWrapper.PrizePop.experimentName)},
			{ ExperimentWrapper.PushNotifSoftPrompt.experimentName,         new PushNotifSoftPromptExperiment(ExperimentWrapper.PushNotifSoftPrompt.experimentName) },
			{ ExperimentWrapper.QuestForTheChest.experimentName,            new QFCExperiment(ExperimentWrapper.QuestForTheChest.experimentName) },
			{ ExperimentWrapper.ReducedDailyBonusEvent.experimentName, 		new EosExperiment(ExperimentWrapper.ReducedDailyBonusEvent.experimentName) },
			{ ExperimentWrapper.RepriceLevelUpSequence.experimentName,		new RepriceLevelUpSequenceExperiment(ExperimentWrapper.RepriceLevelUpSequence.experimentName) },
			{ ExperimentWrapper.RepriceVideo.experimentName,   				new DynamicVideoExperiment(ExperimentWrapper.RepriceVideo.experimentName) },
			{ ExperimentWrapper.RichPass.experimentName,                    new RichPassExperiment(ExperimentWrapper.RichPass.experimentName) },
			{ ExperimentWrapper.RobustChallengesEos.experimentName, 		new EosExperiment(ExperimentWrapper.RobustChallengesEos.experimentName) },
			{ ExperimentWrapper.RoyalRush.experimentName, 					new RoyalRushExperiment(ExperimentWrapper.RoyalRush.experimentName) },
			{ ExperimentWrapper.SaleBubbleVisuals.experimentName, 			new EosExperiment(ExperimentWrapper.SaleBubbleVisuals.experimentName) },
			{ ExperimentWrapper.SaleDialogLevelGate.experimentName, 		new SaleDialogLevelGateExperiment(ExperimentWrapper.SaleDialogLevelGate.experimentName) },
			{ ExperimentWrapper.SmartBetSelector.experimentName, 			new SmartBetSelectorExperiment(ExperimentWrapper.SmartBetSelector.experimentName) },
			{ ExperimentWrapper.SegmentedDynamicMOTD.experimentName, 		new SegmentedDynamicMOTDExperiment(ExperimentWrapper.SegmentedDynamicMOTD.experimentName) },
			{ ExperimentWrapper.Slotventures.experimentName, 				new SlotventuresExperiment(ExperimentWrapper.Slotventures.experimentName) },
			{ ExperimentWrapper.SoftwareUpdateDialog.experimentName, 		new EosExperiment(ExperimentWrapper.SoftwareUpdateDialog.experimentName) },
			{ ExperimentWrapper.SpecialOutOfCoins.experimentName,			new SpecialOutOfCoinsExperiment(ExperimentWrapper.SpecialOutOfCoins.experimentName) },
			{ ExperimentWrapper.SpinPanelV2.experimentName, 				new SpinPanelV2Experiment(ExperimentWrapper.SpinPanelV2.experimentName) },
			{ ExperimentWrapper.SpinTime.experimentName,					new SpinTimeExperiment(ExperimentWrapper.SpinTime.experimentName) },
			{ ExperimentWrapper.StarterPackEos.experimentName, 				new StarterPackExperiment(ExperimentWrapper.StarterPackEos.experimentName) },
			{ ExperimentWrapper.StreakSale.experimentName,                  new StreakSaleExperiment(ExperimentWrapper.StreakSale.experimentName) },
			{ ExperimentWrapper.SuperStreak.experimentName, 				new SuperStreakExperiment(ExperimentWrapper.SuperStreak.experimentName) },
			{ ExperimentWrapper.UnlockAllGames.experimentName, 				new EosExperiment(ExperimentWrapper.UnlockAllGames.experimentName)},
			{ ExperimentWrapper.UpdatedTOS.experimentName, 					new EosExperiment(ExperimentWrapper.UpdatedTOS.experimentName) },
			{ ExperimentWrapper.VideoSoundToggle.experimentName,			new EosExperiment(ExperimentWrapper.VideoSoundToggle.experimentName) },
			{ ExperimentWrapper.VIPLevelUpEvent.experimentName, 			new VIPLevelUpEventExperiment(ExperimentWrapper.VIPLevelUpEvent.experimentName) },
			{ ExperimentWrapper.VIPLobbyRevamp.experimentName, 				new EosExperiment(ExperimentWrapper.VIPLobbyRevamp.experimentName) },
			{ ExperimentWrapper.VIPPhoneDialogSurfacing.experimentName,		new VIPPhoneDialogSurfacingExperiment(ExperimentWrapper.VIPPhoneDialogSurfacing.experimentName) },
			{ ExperimentWrapper.VirtualPets.experimentName,					new VirtualPetsExperiment(ExperimentWrapper.VirtualPets.experimentName) },
			{ ExperimentWrapper.WatchToEarn.experimentName, 				new WatchToEarnExperiment(ExperimentWrapper.WatchToEarn.experimentName) },
			{ ExperimentWrapper.WeeklyRace.experimentName, 					new WeeklyRaceExperiment(ExperimentWrapper.WeeklyRace.experimentName) },
			{ ExperimentWrapper.WelcomeJourney.experimentName,				new WelcomeJourneyExperiment(ExperimentWrapper.WelcomeJourney.experimentName) },
			{ ExperimentWrapper.OneClickBuy.experimentName,              	new PurchaseExperiment(ExperimentWrapper.OneClickBuy.experimentName) },
			{ ExperimentWrapper.WheelDeal.experimentName,					new WheelDealExperiment(ExperimentWrapper.WheelDeal.experimentName) },
			{ ExperimentWrapper.Win10LiveTile.experimentName, 				new EosExperiment(ExperimentWrapper.Win10LiveTile.experimentName) },
			{ ExperimentWrapper.XPromoWOZSlotsGameUnlock.experimentName, 	new EosExperiment(ExperimentWrapper.XPromoWOZSlotsGameUnlock.experimentName) },
			{ ExperimentWrapper.ZisPhase2.experimentName, 					new ZisPhase2Experiment(ExperimentWrapper.ZisPhase2.experimentName) },
			{ ExperimentWrapper.ZadeXPromo.experimentName, 					new EosExperiment(ExperimentWrapper.ZadeXPromo.experimentName) },
			{ LoLa.LOLA_VERSION_EXPERIMENT_NAME, 							new LoLaExperiment(LoLa.LOLA_VERSION_EXPERIMENT_NAME)},
			{ ServerAction.READ_ONLY_ACTION_EXPERIMENT,						new ServerActionExperiment(ServerAction.READ_ONLY_ACTION_EXPERIMENT) }
		};
	}

	public static Dictionary<string, EosExperiment> EosExperiments
	{
		get
		{
			return _eosExperiments;
		}
	}

	

	// Returns the singleton instance of ExperimentManager
	static public ExperimentManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new ExperimentManager();
			}
			return _instance;
		}
	}

	private static ExperimentManager _instance;
	public static bool initialized { get; private set; }
	public static bool ccpaStatusInitialized { get; private set; }

	static ExperimentManager()
	{
		resetExperiments();
		initialized = false;
		ccpaStatusInitialized = false;
	}

	public static void modifyEosExperiment(string key, JSON data)
	{
		EosExperiment experiment;
		
		if (_eosExperiments.TryGetValue(key, out experiment))
		{
			experiment.populateAll(data, true);
		}
		else
		{
			Debug.LogError("Modifying an experiment not exist on client side, experiment name is: " + key);
		}
	}
	
	public static void populateEos(JSON data)
	{
		if (data != null)
		{

			foreach (string key in data.getKeyList())
			{
				JSON expJSON = data.getJSON(key);
				if (_eosExperiments.ContainsKey(key))
				{
					_eosExperiments[key].populateAll(expJSON);
				}
				/* 
#if UNITY_EDITOR
				else
				{
					Debug.LogError("No experiment defined for: " + key);
				}
#endif
				*/
			}
			initialized = true;

			getCCPAExperiment();

			setFBLoginPlayerPrefs();

		}
		else
		{
			Debug.LogWarning("EOS data is null");
		}
	}

	public static void getCCPAExperiment()
	{
		CCPAStatusAction.getExperiment(onCCPAExpResult);
	}

	private static void onCCPAExpResult(JSON data)
	{
		EosExperiment exp = null;
		if (_eosExperiments.TryGetValue(ExperimentWrapper.CCPA.experimentName, out exp))
		{
			exp.populateAll(data);
			ccpaStatusInitialized = true;
		}
	}

	//Save the FBAuthDialog experiment variable values in PlayerPrefs so they can be checked during the loading screen
	//in order to surface the FB login dialog.
	//The experiment data is not yet available during the loading process when the FB auth dialog needs to be surfaced
	private static void setFBLoginPlayerPrefs()
	{
		if (ExperimentWrapper.FBAuthDialog.isInExperiment)
		{
			PlayerPrefsCache.SetInt(Prefs.FB_AUTH_COUNT, ExperimentWrapper.FBAuthDialog.autopopLobbyVisitCount);
			PlayerPrefsCache.SetInt(Prefs.FB_AUTH_SESSION_NUM, ExperimentWrapper.FBAuthDialog.sessionFrequency);
		}
	}

	#region ISVDependencyInitializer implementation

	// This method should be implemented to return the set of class type definitions that the implementor
	// is dependent upon.
	public Type[] GetDependencies()
	{
		return new Type[] { typeof(AuthManager) };
	}

	// The Experiment Manager relies on getting experiments from the server which polls dapi on its own
	public void Initialize(InitializationManager mgr)
	{
		// nothing to do here, initialization complete
		mgr.InitializationComplete(this);
	}

	// short description of this dependency for debugging purposes
	public string description()
	{
		return "ExperimentManager";
	}

	#endregion ISVDependencyInitializer implementation

	public static EosExperiment GetEosExperiment(string eosExperimentName)
	{
		if (_eosExperiments.TryGetValue(eosExperimentName, out EosExperiment exp))
		{
			return exp;
		}
		else
		{
#if UNITY_EDITOR
			Debug.LogError("Can't find experiment: " + eosExperimentName);
#endif
			return null;
		}
	}

	public static void resetStaticClassData()
	{
		resetExperiments();
		initialized = false;
		ccpaStatusInitialized = false;
	}
}
