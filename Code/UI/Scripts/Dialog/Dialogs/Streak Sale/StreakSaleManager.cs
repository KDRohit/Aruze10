using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StreakSaleManager
{
    public static GameTimerRange waitToStartTimer { get; private set; }
    public static GameTimerRange endTimer { get; private set; }
    public static string dataUrl;
    public static List<StreakSalePackage> streakSalePackages = new List<StreakSalePackage>();
    public static string configName;
    public static bool streakSaleActive = false;
    public static bool showInCarousel = true;
    public static bool attemptingPurchaseWithCardPack = false;

    public static bool nextItemIsFree = false;
    public static string freeCoinPackage = "";
    public static string freeBonusPct = "";
    public static string freeBaseBonusPct = "";
    public static string freeCardPack = "";

    public static int purchaseIndex = 0;

    public static void init()
    {
        RoutineRunner.instance.StartCoroutine(getData());
    }

    private static IEnumerator getData()
    {
        using (UnityWebRequest webRequest2 = UnityWebRequest.Get(dataUrl))
        {
            yield return webRequest2.SendWebRequest();
            if (!webRequest2.isHttpError && !webRequest2.isNetworkError)
            {
                JSON json = new JSON(webRequest2.downloadHandler.text);
                parsePackagesJson(json);
            }
            else
            {
                Debug.LogError("streak_sale - network error: " + webRequest2.error);
            }
        }
    }

    private static void parsePackagesJson(JSON json)
    {
        JSON configListJson = json.getJSON("configs");
        if (configListJson == null)
        {
            return;
        }
        JSON configJson = configListJson.getJSON(ExperimentWrapper.StreakSale.configKey);
        if (configJson == null)
        {
            Debug.LogError("streak_sale -- ERROR ***** config key " + ExperimentWrapper.StreakSale.configKey + " not found in config list.");
            return;
        }

        PurchaseFeatureData.populateStreakSale(configJson);

        JSON[] packageArray = configJson.getJsonArray("packages");
        int i = 0;

        //Debug.LogError("streak_sale -- StreakSaleManager -> parsePackagesJson: " + json.ToString() );

        foreach (JSON packageEntry in packageArray)
        {
            StreakSalePackage streakSalePackage = new StreakSalePackage();
            streakSalePackage.key = packageEntry.getInt("key", 0);
            streakSalePackage.backgroundStyle = packageEntry.getString("background", "blue");
            streakSalePackage.frameStyle = packageEntry.getString("frame", "none");
            streakSalePackage.nodeArt = packageEntry.getString("node_art", "offer_coin_00");
            streakSalePackage.rewardType = packageEntry.getString("reward_type", "none");
            streakSalePackage.coinPackage = packageEntry.getString("coin_package", "none");
            streakSalePackage.cardPack = packageEntry.getString("card_pack", ""); // an empty string denotes no card pack. 
            streakSalePackage.bonusPercent = packageEntry.getInt("bonus_pct", 0);
            streakSalePackage.baseBonusPercent = packageEntry.getInt("base_bonus_pct", 0);
            streakSalePackage.lockStyle = packageEntry.getString("locked_display", "hide_price");
            streakSalePackage.indexInOfferList = i;
            streakSalePackages.Add(streakSalePackage);
            i++;
        }

        if (purchaseIndex >= streakSalePackages.Count)
        {
            //This streak sale has already been completed. Don't start it.
            streakSaleActive = showInCarousel = false;
            return;
        }

        if (GameTimer.currentTime > ExperimentWrapper.StreakSale.startTime && GameTimer.currentTime > ExperimentWrapper.StreakSale.endTime)
        {
            //Start time and end time are in the past. Do nothing.
        }
        else if (GameTimer.currentTime > ExperimentWrapper.StreakSale.startTime && GameTimer.currentTime < ExperimentWrapper.StreakSale.endTime)
        {
            activate();
        }
        else
        {
            waitToStartTimer = GameTimerRange.createWithTimeRemaining(ExperimentWrapper.StreakSale.startTime - GameTimer.currentTime);
            waitToStartTimer.registerFunction(activate);
        }

    }

    public static void activate(Dict args = null, GameTimerRange originalTimer = null)
    {
        if (PurchaseFeatureData.StreakSale == null)
        {
            Debug.LogError("streak_sale - ERROR - PurchaseFeatureData.StreakSale was null! Aborting streak sale.");
            return;
        }

        streakSaleActive = showInCarousel = true;
        endTimer = GameTimerRange.createWithTimeRemaining(ExperimentWrapper.StreakSale.endTime - GameTimer.currentTime);
        endTimer.registerFunction(end);

        updateBuyButtonManager();
        StreakSaleDialog.showDialog();
    }
    public static void end(Dict args = null, GameTimerRange originalTimer = null)
    {
        streakSaleActive = showInCarousel = false;
        updateBuyButtonManager();

        if (StreakSaleDialog.instance != null) //If a purchase succeeded, then the server callback will trigger this dialog to close.
        {
            Dialog.close(StreakSaleDialog.instance);
        }
    }
    public static void updateBuyButtonManager()
    {
        if (Overlay.instance.topV2 != null && Overlay.instance.topV2.buyButtonManager != null)
        {
            if (!streakSaleActive)
            {
                Overlay.instance.topV2.buyButtonManager.clearBuyButtonTimerLabels();
            }

            Overlay.instance.topV2.buyButtonManager.setButtonType();
        }
    }

}
