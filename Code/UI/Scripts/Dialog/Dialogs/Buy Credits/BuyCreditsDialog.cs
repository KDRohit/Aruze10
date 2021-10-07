#undef DEBUG_BUY_CREDITS_PROGRESSIVE

using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;
/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class BuyCreditsDialog : DialogBase , IResetGame
{
	protected enum PreloadTexture
	{
		UNKNOWN = 0,
		BANNER = 1
	}

	public GameObject vipBonusParent;
	public BuyCreditsOptionNewHIR[] options;
	
	private const string DEFAULT_SALE_PATH = "stud/two_for_one_sales/generic";
		
	public static string currentSalePath
	{
		get
		{
			if (string.IsNullOrEmpty(_currentSalePath))
			{
				PurchaseFeatureData featureData = PurchaseFeatureData.BuyPage;
				if (featureData == null || string.IsNullOrEmpty(featureData.imageFolderPath))
				{
					// If the path wasn't set correctly in stud, default to the generic art.
					_currentSalePath = DEFAULT_SALE_PATH;
				}
				else
				{
					_currentSalePath = featureData.imageFolderPath;
				}
			}	
			return _currentSalePath;
		}
	}
	
	private static string _currentSalePath = ""; // The path) of the current sale images in S3.

	// The name (key) of the current sale, used for the sale localization keys.
	public static string currentSaleName
	{
		get
		{
			if (string.IsNullOrEmpty(_currentSaleName))
			{
				string[] tokens = currentSalePath.Split('/');
				if (tokens.Length > 0)
				{
					_currentSaleName = tokens[tokens.Length - 1];
				}
			}
			return _currentSaleName;
		}
	}
	private static string _currentSaleName = "";
	
	public static string currentSaleTitle
	{
		get
		{
			BuyCreditsDialog buyDialog = Dialog.instance.currentDialog as BuyCreditsDialog;
			bool skipOOCTitle = buyDialog != null && ((bool)buyDialog.dialogArgs.getWithDefault(D.SKIP_OOC_TITLE, true));

			if (ExperimentWrapper.OutOfCoinsBuyPage.isEnabled && !skipOOCTitle
			    && SlotBaseGame.instance != null && SlotBaseGame.instance.notEnoughCoinsToBet())
			{
				return Localize.textTitle("out_of_coins_buy_page_title");
			}

			if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
			{
				return Localize.textTitle("first_purchase_offer_title");
			}

			if (PurchaseFeatureData.isSaleActive)
			{
				if (Localize.keyExists(string.Format(SALE_TITLE_LOC_FORMAT, currentSaleName)))
				{
					return Localize.textTitle(string.Format(SALE_TITLE_LOC_FORMAT, currentSaleName));
				}
				else
				{
					// If a localization doesn't exist for this sale, then use the generic text.
					return Localize.textTitle(string.Format(SALE_TITLE_LOC_FORMAT, "generic"));
				}
			}
			if (BuyPageCardEvent.instance.shouldShowHeader)
			{
				// If we have a card event and NO buy page sale, then use the card event title
				return BuyPageCardEvent.instance.buyPageHeaderTitle;
			}

			return Localize.textTitle("play_like_a_high_roller");
		}
	}

	public static string carouselImagePath
	{
		get { return string.Format(SALE_CAROUSEL_IMAGE_PATH, currentSalePath); }
	}

	public static string navIconImagePath
	{
		get { return string.Format(SALE_NAV_IMAGE_PATH, currentSalePath);}
	}

	public static bool hasSeenSale = false;	// Whether we have seen the two for one sale this session.

	// Two For One Sale S3 path formats. These are used with a string.Format() call passing in the currentSaleName
	public const string SALE_NAV_IMAGE_PATH = "{0}/navIcon.png";
	public const string SALE_BANNER_IMAGE_PATH = "{0}/bannerBG.png";
	
	public const string SALE_CAROUSEL_IMAGE_PATH = "{0}/CarouselBG.jpg";
	public const string SALE_TITLE_LOC_FORMAT = "two_for_one_title_{0}";
	public const string SALE_MULTIPLIER_LOC = "get_x_coins_{0}";

	public const string AUDIO_MENU_OPEN = "LevelUpSkipLevel";
	public const string AUDIO_MENU_OPEN_FLOURISH = "DialogCelebrate";

	// When we implement a new buy page, we want to have a different stat fire for it, so in that case we would
	// set this variable on init (before base.init()), otherwise it will always be "buy_page".
	public string dialogStatName = "buy_page";

	/// Initialization
	public override void init()
	{
		// Audio on open buy credits for both HIR and SIR, using public vars so we can assign sounds in editor per sku.
		Audio.play(AUDIO_MENU_OPEN);
		dialogStatName = (string)dialogArgs.getWithDefault(D.DATA, "buy_page");
		StatsManager.Instance.LogCount("dialog", dialogStatName, "", StatsManager.getGameKey(), "view", "view");
	}

	void Update()
	{
		AndroidUtil.checkBackButton(onCloseButtonClicked, "dialog", dialogStatName, "back", StatsManager.getGameKey(), "", "click");
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		cancelAutoClose();
		StatsManager.Instance.LogCount("dialog", dialogStatName, "", StatsManager.getGameKey(), "close", "click");
		if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
		{
			StatsManager.Instance.LogCount(counterName: "dialog", 
				kingdom: "buy_page_v3", 
				klass: "first_purchase_offer",
				genus: "click",
				family: "close");
		}

		SlotBaseGame.logOutOfCoinsPurchaseStat(false);

		Dialog.close();
	}

	/// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}
	
	private static string getBannerPath()
	{
		string bannerPath = "";
		PostPurchaseChallengeCampaign campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
		if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment) //This gets highest priority for surfacing if we're eligible
		{
			bannerPath = "stud/first_purchase/generic/bannerBG.png";
		}
		else if (campaign != null && campaign.isEarlyEndActive)
		{
			bannerPath = campaign.getBannerPath();
		}
		else if (PurchaseFeatureData.isSaleActive && !PurchaseFeatureData.isActiveFromPowerup)
		{
			bannerPath = string.Format(SALE_BANNER_IMAGE_PATH, currentSalePath);
		}
		else if (shouldShowCollectablesBanner())
		{
			bannerPath = string.Format(SALE_BANNER_IMAGE_PATH, BuyPageCardEvent.instance.buyPageHeaderPath);
		}
		else if (CreditSweepstakes.isActive)
		{
			bannerPath = "stud/two_for_one_sales/HIR/megacoinbonanza/bannerBG.png";
		}
		else if (ExperimentWrapper.BuyPageProgressive.jackpotDaysIsActive)
		{
			bannerPath = "stud/two_for_one_sales/HIR/jackpotdays/bannerBG.png";
		}
		else if (ExperimentWrapper.LevelLotto.isInExperiment 
		         && FeatureOrchestrator.Orchestrator.activeFeaturesToDisplay.Contains(ExperimentWrapper.LevelLotto.experimentName))
		{
			bannerPath = ExperimentWrapper.LevelLotto.buyPageBannerPath;
		}
		else if (ExperimentWrapper.BuyPageVersionThree.isInExperiment)
		{
			if (ExperimentWrapper.BuyPageVersionThree.bannerImage != "default")
			{
				bannerPath = ExperimentWrapper.BuyPageVersionThree.bannerImage;
			}
			else
			{
				bannerPath = "stud/two_for_one_sales/HIR/hyperinflation/bannerBG.png";
			}
		}
		else
		{
			bannerPath = "stud/two_for_one_sales/HIR/basiccoins/bannerBG.jpg";
		}
		return bannerPath;
	}

	protected static bool shouldShowCollectablesBanner()
	{
		//Turn on the collectables banner if the feature is active.
		return BuyPageCardEvent.instance.isEnabled;
	}
	
	public static bool showDialog(string motdKey = "", SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW, bool skipOOCTitle = true, string statsName = "buy_page")
	{
		Dict args = Dict.create(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.MOTD_KEY, motdKey,
			D.SKIP_OOC_TITLE, skipOOCTitle,
			D.DATA, statsName
		);

		string dialogKey = "buy_credits_v5";

		string bannerPath = getBannerPath();

		if (bannerPath != "")
		{
			Dialog.instance.showDialogAfterDownloadingTextures(dialogKey, bannerPath, args, false, priority);
		}
		else
		{
			// If there is no banner to load, just show it.
			Scheduler.addDialog(dialogKey, args, priority);
		}
		return true;
	}

	// Preload textures for buy dialogs so they open immediately when the player touches the button to show them.
    public static void preloadDialogTextures()
	{
		string bannerPath = getBannerPath();
		if (bannerPath != "")
		{
			DisplayAsset.preloadTexture(bannerPath);
		}

		// Also do the starter pack dialog while we're at it.
		if (StarterDialog.isActive)
		{
			StarterDialog.preloadDialogTextures();
		}
	}
	
	public static void resetStaticClassData()
	{
		hasSeenSale = false;
	}
}
