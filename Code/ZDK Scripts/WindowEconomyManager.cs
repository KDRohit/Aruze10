// This allows testing the full flow of IAP in the editor
// And on device for now
#if UNITY_EDITOR
#define LOCAL_IAP_TESTING
#define PURCHASE_MULTIPLIER_TESTING // Enable this for local purchase multiplier testing in editor.
#endif
// This allows testing the full flow of IAP in the editor
// And on device for now
using System;
using Zynga.Payments.IAP.Impl;
using Zynga.Payments.IAP;
using Zynga.Core.JsonUtil;

using System.Collections;
using System.Collections.Generic;
using CustomLog;
using UnityEngine;
using Zynga.Zdk;
using Zynga.Zdk.Services.Common;
using Zynga.Core.Util;
using Facebook.Unity;

#if UNITY_WSA_10_0 && NETFX_CORE
using Windows.ApplicationModel;
using Windows.ApplicationModel.Store;
using UnityEngine.UI;
using Microsoft.UnityPlugins;
using Windows.Storage;
using System.Xml;
using System.Globalization;

public class WindowEconomyManager : IDependencyInitializer
{
    private const float PURCHASE_TIMEOUT_SECONDS = 60.0f;
    private const float FAKE_PURCHASE_WAIT_TIME = 2.0f;
    private const string RAINY_DAY_EVENT = "rainy_day_purchased";

    public static bool Initialized = false;
    private static Dictionary<string, PurchasableItem> iapPackages = new Dictionary<string, PurchasableItem>();

    private static Dictionary<string, string> productIds = new Dictionary<string, string>();

    public InitializationManager initMgr;

    private static bool _firstLoad = true;              // This flag is cleared when the first complete load of economy data has completed.

    public static bool FirstLoad
    {
        get { return _firstLoad; }
    }

    public static bool PurchasesEnabled
    {
        get { return _purchasesEnabled; }
    }

    private static bool _purchasesEnabled = false;      // Everything has loaded successfully and it's okay to buy.

    public static bool isPurchaseInProgress
    {
        get { return _purchaseInProgress; }
    }

    private static bool _purchaseInProgress = false;

    public static WindowEconomyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new WindowEconomyManager();
            }
            return _instance;
        }
    }

    private static float purchaseTime = 0;
    private static string purchaseTransactionPending = "";
    private static int purchasesFailedWaiting = 0;
    private static DialogBase clickedDialog = null; // The stack top dialog that was clicked when starting a purchase, so we know what to close when it succeeds.
    private static WindowEconomyManager _instance;
    private Dictionary<string, string> refDict = null;
    private static bool _isLicenseSimulationOn = false;

	private static PurchasableItem productItem;
    // Sends purchase to Provider. 
    // Todo: Move arguments to this function into a dictionary?
    public static void startPurchase(PurchasableItem item, string transactionName, int bonusPercent = 0, string gameToUnlock = "", bool isMultiplierPurchase = false, int buyCreditsPagePackageIndex = -1, string packageClass = "", int bonusSalePercent = 0)
    {

		productItem = item;
		if (_purchaseInProgress)
		{
			if (Data.debugEconomy)
			{
				Debug.LogWarning("EconomyManager::startPurchaseCoroutine - Purchase already in progress.");
			}
			return;
		}

		if (item == null || !_purchasesEnabled)
		{
			// Immediately fail this transaction:
			Debug.LogError("EconomyManager::startPurchase - Invalid purchase setup.");
			//Crittercism.FailTransaction(transactionName);

			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.text("error"),
					D.MESSAGE, Localize.text("products_failed"),
					D.REASON, "window-economy-manager-products-failed"
				),
				true
			);
			StatsManager.Instance.LogCount("debug", "purchasing", "products_failed");

			return;
		}
		
		//Once per session warning after a purchase times out that they shouldn't necessarily make a second purchase right away
		if (purchasesFailedWaiting == 1)
		  {
			  if (Data.debugEconomy)
			  {
				  Debug.LogWarning(string.Format("EconomyManager::startPurchaseCoroutine - Already have {0} purchases waiting.", purchasesFailedWaiting));
			  }

			  GenericDialog.showDialog(
				  Dict.create(
					  D.TITLE, Localize.text("pending"),
					  D.MESSAGE, Localize.text("pending_purchase_warning"),
					  D.OPTION1, Localize.textUpper("yes"),
					  D.OPTION2, Localize.textUpper("no"),
					  D.REASON, "window-economy-manager-pending-purchase",
					  D.CALLBACK, new DialogBase.AnswerDelegate((args) =>
					  {
						  purchasesFailedWaiting++;
						  if ((args[D.ANSWER] as string) == "1")
						  {
							  startPurchase(item, transactionName);
						  }
					  }
					  )
				  ),
				  true
			  );

			  return;
		  }
		// Save the name of this transaction so we can possibly fail it later,
		// then store the dollar value of this transaction so we can track it's impact:
		purchaseTransactionPending = transactionName;
	
		// Remember the player's VIP level when making the purchase,
		// so it can be used on the purchase confirmation screen,
		// just in case the purchase itself puts the player into the
		// next level, but we still need to show the level at the time of purchase.
		BuyCreditsConfirmationDialog.vipNewLevelForPurchase = SlotsPlayer.instance.vipNewLevel;

		// Store the current dialog before showing the "purchase in progress" dialog.
		clickedDialog = Dialog.instance.currentDialog;

		// Immediately show the "purchase is progress" message.
		// This must be done before setting _purchaseInProgress to true,
		// since doing that prevents dialogs from opening during the purchase process.
		GenericDialog.showDialog(
			Dict.create(
				D.TITLE, Localize.text("purchasing"),
				D.MESSAGE, Localize.text("purchase_in_progress"),
				D.REASON, "window-economy-manager-purchase-in-progress",
				D.IS_WAITING, true
			),
			true
		);

		_purchaseInProgress = true;
        
        WindowEconomyManager.MakePurchase(item.ProductId);
        

		// Update the error strings - and then log the attempted purchase with the server:
		Server.makeErrorBaseStrings();

		if (Glb.serverLogPayments)
		{
			// Internal logging for this payment:
			Server.sendLogError(string.Format("[purchase_event=PURCHASE_START] [package={0}]", item.Name));
		}
		RoutineRunner.instance.StartCoroutine(purchaseInProgressRoutine());
	}


    private static IEnumerator purchaseInProgressRoutine()
    {
        while (_purchaseInProgress)
        {
            // Not sure what deltaTime is after the app loses focus for Apple IAP UI 
            purchaseTime += (Time.deltaTime < 1.0f) ? Time.deltaTime : 1.0f;

            if (purchaseTime > PURCHASE_TIMEOUT_SECONDS)
            {
                Debug.LogError("EconomyManager::Update - Purchase timed out.");
                _purchaseInProgress = false;
                ServerAction.clearFastUpdateMode();
                if (clickedDialog != null)
                {
                    clickedDialog.purchaseFailed();
                }
								
								Dialog.close(); // Waiting dialog
								
								// Show the canceled purchase dialog, which allows users to contact support.
								CanceledPurchaseDialog.showDialog();

                GenericDialog.showDialog(
                    Dict.create(
                        D.TITLE, Localize.text("payment_wait"),
                        D.MESSAGE, Localize.text("payment_long_time"),
                        D.REASON, "window-economy-manager-payment-wait"
                    ),
                    true
                );

                StatsManager.Instance.LogCount("debug", "purchasing", "purchase_wait");

                if (Glb.serverLogPayments)
                {
                    Server.sendLogError("[purchase_event=PURCHASE_TIMEOUT] Critical failure.");
                }
                if (!string.IsNullOrEmpty(purchaseTransactionPending))
                {
                   // Crittercism.FailTransaction(purchaseTransactionPending);
                    purchaseTransactionPending = "";
                }
                // This variable is used to show a popup about pending purchases
                purchasesFailedWaiting++;
            }
            else
            {
                // If we are still waiting and the timeout hasn't passed yet, then wait a frame.
                yield return null;
            }
        }
        yield break;
    }

#region ISVDependencyInitializer implementation	
    // This method should be implemented to return the set of class type definitions that the implementor
    // is dependent upon.
    // is dependent upon.
    public System.Type[] GetDependencies()
    {
        // TODO - need replacement?
        // return new System.Type[] { typeof (AuthManager), typeof(MiSocialManager) };
        return new System.Type[] { typeof(AuthManager) };
    }

    // This method should contain the logic required to initialize an object/system.  Once initialization is
    // complete, the implementing class should call the "mgr.InitializationComplete(this)" method to signal
    // that downstream dependencies can be initialized.
    public void Initialize(InitializationManager mgr)
    {

        _purchasesEnabled = false;
        iapPackages = new Dictionary<string, PurchasableItem>();
        initMgr = mgr;

#if UNITY_WSA_10_0 && NETFX_CORE
		 /*WindowEconomyManager.LoadLicenseXMLFile((response) =>
		 {
			 Debug.Log("Invoking load license xml file callback");

			 if (response.Status == CallbackStatus.Failure)
			 {
				 Debug.LogError("Failed to invoke the license xml");
				 return;
			 }

			 if (response.Status == CallbackStatus.Success)
			 {
				 WindowEconomyManager.LoadWindowsCatalog();
			 }

		 }, "Data\\WindowsStoreProxy.xml");*/
		WindowEconomyManager.LoadWindowsCatalog();
#endif

	}

    // short description of this dependency for debugging purposes
    public string description()
    {
        return "WindowEconomyManager";
    }

#endregion ISVDependencyInitializer implementation

    public static PurchasableItem getZyngaEconomyItemByPackageName(string creditPackage)
    {
        if (iapPackages.ContainsKey(creditPackage))
        {
            return iapPackages[creditPackage];
        }

        return null;
    }

    void HandleOnPurchaseCancelled(string key )
    {
        if (key != null)
        {
           string msg = string.Format("NewEconomyManager::OnPurchaseCancel - Package: Name={0}", key);
           Log.log(msg, Color.green);

           StatsManager.Instance.LogCount("debug", "purchasing", "cancel_purchase", key);

           if (Glb.serverLogPayments)
           {
				Server.sendLogError(string.Format("[purchase_event=PURCHASE_CANCELED] [package={0}]", key));
           }
        }
        
        _purchaseInProgress = false;
        if (clickedDialog != null)
        {
            clickedDialog.purchaseCancelled();
            Dialog.close(); // Waiting dialog
        }
				
				// Show the canceled purchase dialog, which allows users to contact support.
				CanceledPurchaseDialog.showDialog();

        if (!string.IsNullOrEmpty(purchaseTransactionPending))
        {
            //Crittercism.EndTransaction(purchaseTransactionPending);
            purchaseTransactionPending = "";
        }
        if (Glb.serverLogPayments)
        {
            Server.sendLogError(string.Format("[purchase_event=PURCHASE_CANCELED] Package is null"));
        }
    }

	void HandleOnPurchaseFailed(string key, Microsoft.UnityPlugins.PurchaseResults result)
	{
		if (result != null)
		{ 
			string msg = string.Format("EconomyManager::OnPurchaseError - Error: itemcode={0} id={2} error={3}", key, result.TransactionId.ToString(), result.ReceiptXml.ToString());
			Log.log(msg, Color.green);
			Debug.LogError(msg);
			StatsManager.Instance.LogCount("debug", "purchasing", "error_purchase", key, result.TransactionId.ToString(), result.ReceiptXml.ToString());
		}
		_purchaseInProgress = false;

		if (clickedDialog != null)
		{
			clickedDialog.purchaseFailed();
		}
		
		Dialog.close(); // Waiting dialog
		
		// Show the canceled purchase dialog, which allows users to contact support.
		CanceledPurchaseDialog.showDialog();
	}

    void HandleOnPurchaseCompleted(object sender, TransactionEventArgs e)
    {
        Transaction transaction = e.Transaction;
        string msg = string.Format("EconomyManager::HandlePurchase - Purchase is: {0}", e.Transaction.ReceiptData);
        //Bugsnag.LeaveBreadcrumb(msg);

        ServerAction.setFastUpdateMode("item_purchased");
        ServerAction.setFastUpdateMode("popcorn_purchased");

        //Must be called on all purchases after they succeed or fail. 
        //InAppPurchase.FinishTransaction(transaction);
        StatsManager.Instance.LogCount("debug", "purchasing", "handle_purchase", e.Transaction.ReceiptData);

    }

    /// <summary>
    /// Callback for what is returned from the server after a windows payment has concluded
    /// </summary>
    void handleWindowsPaymentsCallback(JSON data)
    {
        Debug.Log("IN HANDLE WINDOWS PAYMENTS CALLBACK");
		string errorCode = data.getString("error_code", "0");
		if (errorCode == "2")
		{
			if (Glb.serverLogPayments)
			{
				Server.sendLogError(string.Format("[purchase_event=WINDOWCALLBACK] Receipt is not valid"));
			}
		} else if (errorCode == "1")
		{
			if (Glb.serverLogPayments)
			{
				Server.sendLogError(string.Format("[purchase_event=WINDOWCALLBACK] Receipt has already been used"));
			}
		} else if (errorCode == "0")
		{
			if (Glb.serverLogPayments)
			{
				Server.sendLogError(string.Format("[purchase_event=WINDOWCALLBACK] Item granted"));
			}
		} else
		{
			if (Glb.serverLogPayments)
			{
				Server.sendLogError(string.Format("[purchase_event=WINDOWCALLBACK] unknown error"));
			}
		}

		WindowEconomyManager.fullfillResult(productId, transactionId);
	}
	

	private static void fullfillResult (string productId, Guid transactionId)
	{
		WindowEconomyManager.ReportConsumableFulfillment(productId, transactionId, (response) =>
		{
			if ((Microsoft.UnityPlugins.FulfillmentResult)response.Status == Microsoft.UnityPlugins.FulfillmentResult.ServerError)
			{
				Debug.LogError("Callback failed");
				if (Glb.serverLogPayments)
				{
					Server.sendLogError(string.Format("[purchase_event=FULLFILL_FAILED] Server Error"));
				}
				return;
			}

			if ((Microsoft.UnityPlugins.FulfillmentResult)response.Status == Microsoft.UnityPlugins.FulfillmentResult.NothingToFulfill)
			{
				WindowEconomyManager.Instance.HandleOnPurchaseCancelled(productId);
			}

			if ((Microsoft.UnityPlugins.FulfillmentResult)response.Status == Microsoft.UnityPlugins.FulfillmentResult.PurchasePending)
			{
				Debug.LogError("This payment is pending");
				if (Glb.serverLogPayments)
				{
					Server.sendLogError(string.Format("[purchase_event=FULLFILL_PENDING] Payment is pending"));
				}
			}

			if ((Microsoft.UnityPlugins.FulfillmentResult)response.Status == Microsoft.UnityPlugins.FulfillmentResult.PurchaseReverted)
			{
				Debug.LogError("This payment has been reverted");
				if (Glb.serverLogPayments)
				{
					Server.sendLogError(string.Format("[purchase_event=FULLFILL_REVERTED] Payment is reverted"));
				}
			}
			Debug.Log("consumable has been fullfilled");
		});
	}
    // Wait for global data before syncing up the Meco catalog with our global data purchases.
    // Note: we intentionally only sync the catalog with global data at startup, for safety.
    private IEnumerator waitForGlobalData()
    {
        // The game is completely unloadable if global data doesn't arrive,
        // so it is safe to wait for it, checking each frame for the flag.
        while (!Data.isGlobalDataSet)
        {
            yield return null;
        }

        // CRC - this is our new purchasable package:
        PurchasablePackage.populateAll(Glb.popcornSalePackages);

        string msg = "EconomyManager::waitForGlobalData - Economy catalog successfully synced.";
        Bugsnag.LeaveBreadcrumb(msg);

        // Only update these flags when everything has finished loading:
        _purchasesEnabled = true;
        _firstLoad = false;
    }

    // Takes the  Catalog and populates the dictionary with Credit Package as the key
    public static void populateAllIAP(Dictionary <string,Microsoft.UnityPlugins.ProductListing> productListings )
    {
        string msg = "EconomyManager::populateAllIAP - Begin.";
        Debug.Log(msg);

        foreach (var productListingKey in productListings.Keys)
        {
            Debug.Log("Key: " + productListingKey + " value: " + productListings[productListingKey].Name);

            PurchasableItem item = new PurchasableItem(productListings[productListingKey]);
            if (!iapPackages.ContainsKey(productListingKey))
            {
                iapPackages.Add(productListingKey, item);
            } else
            {
                iapPackages[productListingKey] = item; 
            }

            if (Data.debugEconomy)
            {
                Debug.Log(string.Format("EconomyManager::populateAllIAP - IAP Package received: {0}", productListingKey));
            }
        }
        msg = string.Format("EconomyManager::populateAllIAP - Added: {0} items ", iapPackages.Count);
        Debug.Log(msg);
    }

    // Final Callback sent from server when item finishes purchasing
    public static void creditPurchase(JSON data)
    {
        ServerAction.clearFastUpdateMode();

        if (data == null)
        {
            Debug.LogError("EconomyManager.creditPurchase() callback called with no data!");
            return;
        }

        string type = data.getString("type", "");
        string eventId = data.getString("event", "");

        long creditsAdded = (long)data.getInt("credits", 0);

        int vipAdded = data.getInt("vip_points", 0);
        long vipCredits = (long)data.getInt("vip_credits", 0);
        long baseCredits = data.getLong("premium_credits", 0);
        string gameKey = data.getString("game_key", ""); // Select Game Unlock
        string packageKey = data.getString("popcorn_package_key_name", ""); // This ?might? contain the ID of the credits package we just purchased.
        string feature = data.getString("feature", "");
        string xpromoTarget = data.getString("xpromo_target", "");

        // Percentages for BuyPage v3
        int bonusPercent = data.getInt("bonus_pct", 0);
        int saleBonusPercent = data.getInt("sale_bonus_pct", 0);
        int vipBonusPercent = data.getInt("vip_bonus_pct", 0);

        // SKU game unlock.
        if (feature == "xpromo" && xpromoTarget != "")
        {
            LobbyGame skuGameUnlock = LobbyGame.find(gameKey);

            if (skuGameUnlock != null)
            {
                if (LobbyGame.skuGameUnlock != null &&
                    skuGameUnlock != LobbyGame.skuGameUnlock)
                {
                    Debug.LogError(
                        "SKU Game Unlock: " + gameKey + " from server is different than " +
                        LobbyGame.skuGameUnlock.keyName + " in client.");
                }
            }

            LobbyGame.skuGameUnlock = skuGameUnlock;
        }

        if (!string.IsNullOrEmpty(gameKey))
        {
            // This is a select game unlock event.
            // And this is not a credit purchase, so we want to get out of this function now.
            SlotsPlayer.unlockGame(eventId, gameKey);
            return;
        }

        _purchaseInProgress = false;

        bool didClosePurchaseDialog = true;

        if (clickedDialog != null)
        {
            if (clickedDialog.purchaseSucceeded(data))
            {
                // We don't close the top dialog until the purchase is successful.
                // Do this before closing the waiting dialog so we don't see it sliding off or anything.
                // Closing it while it's not on top of the stack just silently removes it from the stack.
                Dialog.close(clickedDialog);
            }
            else
            {
                // If we're not closing the clicked dialog now, then that dialog is
                // responsible for adding the credits instead of doing it here,
                // because it probably needs to do some fancy presentation first.
                didClosePurchaseDialog = false;
            }
        }
        Dialog.close(); // The waiting dialog

        if (vipAdded > 0)
        {
            SlotsPlayer.instance.addVIPPoints(vipAdded);    // VIP bonus percent is pre-calculated on the backend.
        }

        if (creditsAdded > 0)
        {
            if (didClosePurchaseDialog)
            {
                // Only add the credits now if we did close the purchase dialog,
                // because if we didn't close it, then the purchase dialog is
                // responsible for adding the credits after whatever presentation it's doing.
                SlotsPlayer.addCredits(creditsAdded, "purchase", true, false);
            }

            if (didClosePurchaseDialog)
            {
                // Only show a confirmation dialog if we closed the purchase dialog,
                // because if the purchase dialog is handling special presentation,
                // it is essentially confirmation of this purchase.
                // Or, if there was no purchase dialog open to be able to close, show confirmation here.
                switch (type)
                {
                    default:
                        // Show the standard confirmation dialog.
                        Dict args = Dict.create(
                            D.BONUS_CREDITS, (long)vipCredits,
                            D.TOTAL_CREDITS, (long)creditsAdded,
                            D.PACKAGE_KEY, packageKey,
                            D.VIP_POINTS, vipAdded,
                            D.DATA, data,
                            D.BASE_CREDITS, baseCredits,
                            D.BONUS_PERCENT, bonusPercent,
                            D.SALE_BONUS_PERCENT, saleBonusPercent,
                            D.VIP_BONUS_PERCENT, vipBonusPercent
                        );
                        BuyCreditsConfirmationDialog.showDialog(args);
                        break;
                }
            }
        }

        // Extract some tracking data
        string transactionId = data.getString("transaction_id", "");
        string receiptId = data.getString("receipt_id", "");
        string receiptSig = data.getString("receipt_sig", "");
        string paidCurrency = data.getString("currency", "USD");
        float paidAmount = data.getFloat("amount", 0f);

		if (WindowEconomyManager.Instance != null) 
        {
            WindowEconomyManager.Instance.logSuccessfulWindowsPurchase(transactionId, paidCurrency, (double)paidAmount, type, creditsAdded);
		}
		// Only record these if the paid amount is greater than 0.00
		if (paidAmount > 0f)
        {
            //UAWrapper.Instance.TrackPurchase(paidAmount, paidCurrency, transactionId, receiptId, receiptSig);

            // FB event tracking
            if (FB.IsLoggedIn)
            {
				//SIR-9115
				//FB.LogPurchase(paidAmount, paidCurrency);
            }
        }

        // Record this stat even if the paid amount was 0.00
        StatsManager.Instance.LogCount("debug", "purchasing", "credit_purchase", string.Format("{0}", paidAmount));

        if (purchasesFailedWaiting == 1)
        {
            // If the slow transaction finished without the user trying to start a new transaction,
            // then we never showed the "purcahse pending" dialog that this variable controls, so reset it for next time.
            // If we don't do this, then the next purchase attempt will show the "purchase pending" dialog
            // even though there is no pending purchase.
            purchasesFailedWaiting = 0;
        }

        if (Glb.serverLogPayments || Data.debugEconomy)
        {
            // Verbose logging of all the information we have about this purchase:
            string msg = string.Format("[package={0}] [credits={1}] [vip_points={2}] [vip_credits={3}] [base_credits={4}]",
                data.getString("item_key", packageKey), creditsAdded, vipAdded, vipCredits, baseCredits);

            if (Glb.serverLogPayments)
            {
                Server.sendLogError(string.Format("[purchase_event=PURCHASE_COMPLETED] {0}", msg));
            }
            if (Data.debugEconomy)
            {
                Debug.Log(string.Format("EconomyManager::creditPurchase - {0}", msg));
            }
        }

        if (!string.IsNullOrEmpty(purchaseTransactionPending))
        {
            //Crittercism.EndTransaction(purchaseTransactionPending);
            purchaseTransactionPending = "";
        }

        // if we start any purchase, then we want to turn off the starter pack until the next refresh when the server can
        StarterDialog.didPurchase = true;
    }

	private bool logSuccessfulWindowsPurchase(string transactionId, string localCurrency, double priceInLocalCurrency, string gameCurrency, double amountOfGameCurrencyPurchased)
	{
		if (string.IsNullOrEmpty(transactionId))
		{
			Debug.LogError("Transaction info or transaction Id has not been set during logSuccessfulWindowsPurchase() call.");
			return false;
		}
		
		if (priceInLocalCurrency <= 0)
		{
			Debug.LogErrorFormat("Local currency price '{0}' must be strictly positive.", priceInLocalCurrency);
			return false;
		}
		if (string.IsNullOrEmpty(gameCurrency))
		{
			Debug.LogError("Game currency has not been set during logSuccessfulWindowsPurchase() call.");
			return false;
		}
		if (amountOfGameCurrencyPurchased <= 0)
		{
			Debug.LogErrorFormat("Amount of game currency '{0}' must be strictly positive.", amountOfGameCurrencyPurchased);
			return false;
		}
		Packages.Track.Service.LogPayment((int)amountOfGameCurrencyPurchased, "coin", Zynga.Zdk.Services.Track.Provider.Windows, transactionId, "successful_transaction", localCurrency, priceInLocalCurrency.ToString("F"), 0);
		return true;
	}

    // Retrieve additional data from our item:
    public static string getItemShortCode(PurchasableItem item, string defaultValue)
    {
        if (item.Metadata.ContainsKey("zpayments_grant_code"))
        {
            return (string)item.Metadata["zpayments_grant_code"];
        }
        return defaultValue;
    }

    /** Functions below specifically are used for SIR which takes the credits and multiplies it with the amount that is received from SCAT */
    private static long getSkuDataMultiplier()
    {
        long multiplier = 1;
        GlobalSkuData skuData = null;
        string sku = SkuResources.skuString;
        skuData = GlobalSkuData.find(sku);

        if (skuData != null)
        {
            if (!long.TryParse(skuData.economymultiplier, out multiplier))
            {
                multiplier = 1;
            }
        }
        return multiplier;
    }


	private static string parseXml(string xml, string nodeName, string attribute)
	{
		XmlDocument xmlDoc = new XmlDocument();
		xmlDoc.LoadXml(xml);
		XmlNodeList node = xmlDoc.GetElementsByTagName(nodeName);
		string attributeValue = node[0].Attributes[attribute].Value;

		return attributeValue;
	}
	/////////////////////////////////////////////////////////////
	///Windows section for payments
	///Loading Windows Catalog
	///Requesting a purchase
	/////////////////////////////////////////////////////////////
#if UNITY_WSA_10_0 && NETFX_CORE

	private static string productId = "";
	private static Guid transactionId;


	private static Dictionary<string, string> createWindowsServerAction(Microsoft.UnityPlugins.PurchaseResults response)
	{
		Dictionary<string, string> transactionResponse = new Dictionary<string, string>();
		transactionResponse.Add("receiptXml", response.ReceiptXml);
		transactionResponse.Add("offerId", response.OfferId);
		transactionResponse.Add("transactionId", response.TransactionId.ToString());
		string itemCode = parseXml(response.ReceiptXml, "ProductReceipt", "ProductId");
		transactionResponse.Add("itemCode", itemCode);
		string receiptId = parseXml(response.ReceiptXml, "ProductReceipt", "Id");
		transactionResponse.Add("receiptId", receiptId);
		string transactionTime = parseXml(response.ReceiptXml, "ProductReceipt", "PurchaseDate");
		transactionResponse.Add("transactionTime", transactionTime);
		//string priceAmount = parseXml(response.Result.ReceiptXml, "ProductReceipt", "PurchasePrice");
		//int priceAmount = (int)productItem.PriceAmount;
		PurchasablePackage package = PurchasablePackage.find(itemCode);
		//decimal priceAmount = decimal.Parse(package.priceLocalized.ToString(), NumberStyles.AllowCurrencySymbol | NumberStyles.Number);
		//transactionResponse.Add("usdAmount", priceAmount.ToString());
		transactionResponse.Add("usdAmount", package.priceTier.ToString());
		transactionResponse.Add("clientId", productItem.GameSkuId.ToString());
		int snId = (int)ZdkManager.Instance.Zsession.Snid;
		transactionResponse.Add("snId", snId.ToString());
		transactionResponse.Add("p", clickedDialog.economyTrackingName);
		transactionResponse.Add("g", GameState.currentStateName);

		return transactionResponse;
	}
    private static void MakePurchase(string key)
    {
		WindowEconomyManager.RequestProductPurchase(key, (response) =>
        {
            if (response.Status == CallbackStatus.Success)
            {
				productId = key;
				transactionId = response.Result.TransactionId;
                Debug.Log("Purchase is successful");
                Debug.LogFormat("Transaction id {0}", response.Result.TransactionId);
                Debug.LogFormat("Receipt id {0}", response.Result.ReceiptXml);
                Debug.LogFormat("Offer id {0}", response.Result.OfferId);
				Dictionary<string, string> transactionResponse = new Dictionary<string, string>();
		
				if (response.Result.Status == Microsoft.UnityPlugins.ProductPurchaseStatus.Succeeded)
				{
					Debug.LogFormat("Success sending request to server ");
					transactionResponse = createWindowsServerAction(response.Result);
					WindowsPaymentAction.doWindowPaymentAction(transactionResponse);
					//WindowsPaymentAction.testWindowPaymentAction();

					ServerAction.setFastUpdateMode("item_purchased");
					ServerAction.setFastUpdateMode("popcorn_purchased");

					//Must be called on all purchases after they succeed or fail. 
					StatsManager.Instance.LogCount("debug", "purchasing", "handle_purchase", response.Result.OfferId);

				}
				else if (response.Result.Status == Microsoft.UnityPlugins.ProductPurchaseStatus.AlreadyPurchased)
				{
					transactionResponse = createWindowsServerAction(response.Result);
					Debug.LogFormat("Item already purchased {0}", key);
					if (Glb.serverLogPayments)
					{
						Server.sendLogError(string.Format("[purchase_event=PURCHASE_PENDING] [package={0}]", key));
					}
					WindowsPaymentAction.doWindowPaymentAction(transactionResponse);

				}
				else if (response.Result.Status == Microsoft.UnityPlugins.ProductPurchaseStatus.NotFulfilled)
				{
					Debug.LogFormat("Item not fulfilled {0}", key);
					if (Glb.serverLogPayments)
					{
						Server.sendLogError(string.Format("[purchase_event=PURCHASE_NOTFULFILLED] [package={0}]", key));
					}
					WindowEconomyManager.fullfillResult(key, transactionId);
				}
				else if (response.Result.Status == Microsoft.UnityPlugins.ProductPurchaseStatus.NotPurchased)
				{
					Debug.LogFormat("Item not purchased {0}", key);
					WindowEconomyManager.Instance.HandleOnPurchaseCancelled(productId);
				}
			}
            else if (response.Status == CallbackStatus.Failure)
            {
				if (Glb.serverLogPayments)
				{
					Server.sendLogError(string.Format("[purchase_event=PURCHASE_FAILURE] [package={0}]", key));
				}
				Debug.LogError("Purchase has failed");
				Instance.HandleOnPurchaseFailed(key, response.Result);
            }
            else if (response.Status == CallbackStatus.TimedOut)
            {
				if (Glb.serverLogPayments)
				{
					Server.sendLogError(string.Format("[purchase_event=PURCHASE_TIMEDOUT] [package={0}]", key));
				}
				Debug.LogError("Purchase has timed out");
            }
            else if (response.Status == CallbackStatus.Unknown)
            {
				if (Glb.serverLogPayments)
				{
					Server.sendLogError(string.Format("[purchase_event=PURCHASE_UNKNOWNERROR] [package={0}]", key));
				}
				Debug.LogError("Purchase has unknown error");
            }

			Debug.Log("Make purchase completed.");
        });
    }

    /// <summary>
    /// This function loads the catalog from the windows store
    /// </summary>
    private static void LoadWindowsCatalog()
    {
        Debug.Log("Getting windows catalog");
        WindowEconomyManager.LoadListingInformation((resp) =>
        {

            if (resp.Status == CallbackStatus.Failure)
            {
				if (Glb.serverLogPayments)
				{
					Server.sendLogError(string.Format("[load_windows_catalog_event=LOAD_FAILURE] "));
				}
				Debug.LogErrorFormat("LoadListingInformation {0}", resp.Exception.Message);
                return;
            }

            if (resp.Status == CallbackStatus.Success)
            {
				Debug.Log("Successful load window catalog");
				if (Glb.serverLogPayments)
				{
					Server.sendLogError(string.Format("[load_windows_catalog_event=LOAD_SUCCESS] "));
				}
				Server.registerEventDelegate("item_purchased", creditPurchase, true);
				Server.registerEventDelegate("popcorn_purchased", creditPurchase, true);
				Server.registerEventDelegate("multiplier_purchased", creditPurchase, true);
				Server.registerEventDelegate(RAINY_DAY_EVENT, creditPurchase, true);
				Server.registerEventDelegate("grant_goods_windows", WindowEconomyManager.Instance.handleWindowsPaymentsCallback, true);
                WindowEconomyManager.populateAllIAP(resp.Result.ProductListings);

                if (WindowEconomyManager.Instance.initMgr != null)
                {
                    WindowEconomyManager.Instance.initMgr.InitializationComplete(WindowEconomyManager.Instance);
                }
           
                Initialized = true;
     
                // When startup is complete, we are supposedly guaranteed that there was a successful catalog load.
                // However, we may need to wait for global data to be in place, hence the delay coroutine.
                Packages.Coroutines.Begin(WindowEconomyManager.Instance.waitForGlobalData());
               
            }
            Debug.Log(resp.Result.Description.ToString());
            
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="callback"></param> It is best not to set the callback parameter to null. The reason being  you don't know
    /// when the license loading is finished. Best set it properly to avoid tricky race conditions
    /// <param name="licenseFilePath"></param>
    /// 
    public static void LoadLicenseXMLFile(Action<CallbackResponse> callback, string licenseFilePath = null)
    {

        Utils.RunOnWindowsUIThread(async () =>
        {
            try
            {
                licenseFilePath = (licenseFilePath == null) ? "WindowsStoreProxy.xml" : licenseFilePath;

                StorageFile licenseFile;
                if (System.IO.Path.IsPathRooted(licenseFilePath))
                {
                    licenseFile = await StorageFile.GetFileFromPathAsync(licenseFilePath);
                }
                else
                {
                    Windows.Storage.StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                    licenseFile = await StorageFile.GetFileFromPathAsync(installedLocation.Path + "\\" + licenseFilePath);
                }
                await CurrentAppSimulator.ReloadSimulatorAsync(licenseFile);

                // switch on the license simulation
                _isLicenseSimulationOn = true;

                if (callback != null)
                {
                    Utils.RunOnUnityAppThread(() =>
                    {
                        callback(new CallbackResponse { Exception = null, Status = CallbackStatus.Success });
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogFormat("Error loading license file. License simulator will give incorrect results!", ex.ToString());
                callback(new CallbackResponse { Exception = ex, Status = CallbackStatus.Failure });
                return;
            }
        });
    }


    // Function to request the product from the window store
    public static void RequestProductPurchase(string productId, Action<CallbackResponse<Microsoft.UnityPlugins.PurchaseResults>> OnProductPurchaseFinished)
    {
        Microsoft.UnityPlugins.PurchaseResults result = null;
        Utils.RunOnWindowsUIThread(async () =>
        {
            try
            {

                if (_isLicenseSimulationOn)
                {
                    result = new Microsoft.UnityPlugins.PurchaseResults(await CurrentAppSimulator.RequestProductPurchaseAsync(productId));
                }
                else
                {
                    result = new Microsoft.UnityPlugins.PurchaseResults(await CurrentApp.RequestProductPurchaseAsync(productId));
                }
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("Error purchasing the product {0} ", ex.ToString());
                Utils.RunOnUnityAppThread(() => { if (OnProductPurchaseFinished != null) OnProductPurchaseFinished(new CallbackResponse<Microsoft.UnityPlugins.PurchaseResults> { Exception = ex, Status = CallbackStatus.Failure, Result = null }); });
                return;
            }

            Utils.RunOnUnityAppThread(() => { if (OnProductPurchaseFinished != null) OnProductPurchaseFinished(new CallbackResponse<Microsoft.UnityPlugins.PurchaseResults> { Exception = null, Status = CallbackStatus.Success, Result = result }); });
        });
    }


    //Function is used to get a list of information from the window store
    private static Microsoft.UnityPlugins.ListingInformation _listingInformation = null;
    public static void LoadListingInformation(Action<CallbackResponse<Microsoft.UnityPlugins.ListingInformation>> OnLoadListingFinished)
    {
        Utils.RunOnWindowsUIThread(async () =>
        {
            try
            {
                if (_isLicenseSimulationOn)
                {
                    _listingInformation = new Microsoft.UnityPlugins.ListingInformation(await CurrentAppSimulator.LoadListingInformationAsync());
                }
                else
                {
                    _listingInformation = new Microsoft.UnityPlugins.ListingInformation(await CurrentApp.LoadListingInformationAsync());
                }
            }
            catch (Exception ex)
            {
                if (OnLoadListingFinished != null)
                {
                    Utils.RunOnUnityAppThread(() => { OnLoadListingFinished(new CallbackResponse<Microsoft.UnityPlugins.ListingInformation> { Result = null, Status = CallbackStatus.Failure, Exception = ex }); });
                    return;
                }
            }

            // This must get invoked on the Unity thread.
            // On successful completion, invoke the OnLoadListingFinished event handler
            if (OnLoadListingFinished != null && _listingInformation != null)
            {
                Utils.RunOnUnityAppThread(() => { OnLoadListingFinished(new CallbackResponse<Microsoft.UnityPlugins.ListingInformation> { Result = _listingInformation, Status = CallbackStatus.Success, Exception = null }); });
            }
        });
    }

	public static void ReportConsumableFulfillment(string productId, Guid transactionId,
			Action<CallbackResponse<Microsoft.UnityPlugins.FulfillmentResult>> OnReportConsumableFulfillmentFinished)
	{

		Utils.RunOnWindowsUIThread(async () =>
		{
			Windows.ApplicationModel.Store.FulfillmentResult result = Windows.ApplicationModel.Store.FulfillmentResult.ServerError;
			try
			{

				if (_isLicenseSimulationOn)
				{
					result = await CurrentAppSimulator.ReportConsumableFulfillmentAsync(productId, transactionId);
				}
				else
				{
					result = await CurrentApp.ReportConsumableFulfillmentAsync(productId, transactionId);
				}

			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("Error while reporting consumable fulfillment {0}", ex.ToString());
				Utils.RunOnUnityAppThread(() => { if (OnReportConsumableFulfillmentFinished != null) OnReportConsumableFulfillmentFinished(new CallbackResponse<Microsoft.UnityPlugins.FulfillmentResult> { Status = CallbackStatus.Failure, Exception = ex, Result = Microsoft.UnityPlugins.FulfillmentResult.ServerError }); });
				return;
			}

			// This should not really be throwing exceptions.. If it does, they will be raised on the Unity thread anyways, so game should handle it
			Utils.RunOnUnityAppThread(() =>
			{
				if (OnReportConsumableFulfillmentFinished != null)
					OnReportConsumableFulfillmentFinished(
					new CallbackResponse<Microsoft.UnityPlugins.FulfillmentResult>
					{
						Result = (Microsoft.UnityPlugins.FulfillmentResult)result,
						Exception = null,
						Status = CallbackStatus.Success
					});
			});
		});

	}

#endif
}
#endif
