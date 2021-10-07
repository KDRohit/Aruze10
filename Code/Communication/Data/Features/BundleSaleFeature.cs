using Com.Rewardables;
using System.Collections.Generic;
using UnityEngine;

namespace Com.HitItRich.Feature.BundleSale
{
	public class BundleSaleFeature: FeatureBase, IResetGame
	{
		/* login data key */
		public const string LOGIN_DATA_KEY = "bundle_sale";
		//static instance
		public static BundleSaleFeature instance { get; private set; }

		public class BundleItem
		{
			public BundleItem(string type, int duration,string localizeKey)
			{
				buffType = type;
				buffDuration = duration;
				buffLocalizeKey = localizeKey;
			}

			public bool active = false; 
			public string buffType { get; private set; }
			public int buffDuration { get; private set; }
			public string buffLocalizeKey { get; private set; }

			public string getTitle()
			{
				return Localize.text(buffLocalizeKey);
			}
		}

		public int coolDown { get; private set; } 
		public int purchaseLimit { get; private set; }
		public string badgeText { get; private set; }
		public string buttonText { get; private set; }
		public string salePreText { get; private set; }
		public string saleTitle { get; private set; }
		public string coinPackageKey { get; private set; }
		public int purchaseRemaining {get; private set;}
		public int saleBonusPercent { get; private set; }
		
		public int longestDurration { get; private set; }
		
		private int saleCountDown;
		private bool showDialogWhenBuffsExpire;
		private int timesPurchased;
		private int coolDownStartTime;
		private int buffTimerEnd;

		private GameTimerRange saleCountDownTimer = null;
		private GameTimerRange coolDownTimer = null;
		private GameTimerRange buffTimer = null;

		public  CreditPackage purchaseItem  { get; private set; }
		public List<BundleItem> itemsInBundle { get; private set; }

		private static bool didPurchaseThisSession;

		private BundleSaleFeature()
		{

		} 
		public void doPurchase()
		{
			string bundleId = ExperimentWrapper.BundleSale.bundleId;
			purchaseItem.purchasePackage.makePurchase(0, false, -1, "PopcornSalePackage", BundleSaleFeature.instance.saleBonusPercent, bundleSaleId:bundleId);
		}
		
#if !ZYNGA_PRODUCTION
		public void devMockPurchase()
		{
			BundleSaleActions.devMockPurchase();
			onPurchaseSucceeded();
		}

		public void devResetCooldown()
		{
			coolDownStartTime = 0;
			CustomPlayerData.setValue(CustomPlayerData.BUNDLE_SALE_COOLDOWN_START_TIME, 0);
			onCoolDownExpired();
		}
		
		public void devResetPurchaseLimt()
		{
			BundleSaleActions.devResetPurchaseCount();
		}

		public void devEndBuffTimerInSeconds(int seconds)
		{
			if (buffTimer == null || seconds <= 0)
			{
				return;
			}

			buffTimerEnd = GameTimer.currentTime + seconds;
			buffTimer.removeFunction(onBuffTimerEnd);
			buffTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + seconds);
			buffTimer.registerFunction(onBuffTimerEnd);
			
			
		}
		
		public void devEndSaleTimerInSeconds(int seconds)
		{
			if (saleCountDownTimer == null || seconds <= 0)
			{
				return;
			}

			saleCountDownTimer.updateEndTime(seconds);
		}
#endif

		public void onPurchaseSucceeded()
		{
			didPurchaseThisSession = true;
			timesPurchased++;
			CustomPlayerData.setValue(CustomPlayerData.BUNDLE_SALE_PURCHASE_AMOUNT, timesPurchased);

			
			//save start time of cooldown
			coolDownStartTime = GameTimer.currentTime;
			CustomPlayerData.setValue(CustomPlayerData.BUNDLE_SALE_COOLDOWN_START_TIME, GameTimer.currentTime);
			
			
			//start cooldown
			startCoolDown(coolDown);
			
			
			//refresh top nav if we're in the lobby to show deal icon
			if (Overlay.instance.topV2 != null && Overlay.instance.topV2.buyButtonManager != null)
			{
				Overlay.instance.topV2.buyButtonManager.setButtonType();
			}
		}

		public int getCooldownRemaining()
		{
			int timeRemaining = (coolDownStartTime + coolDown) - GameTimer.currentTime;
			if (timeRemaining < 0)
			{
				timeRemaining = -1;
			}
			return timeRemaining;
		}

		public GameTimerRange getSaleTimer()
		{
			return saleCountDownTimer;
		}

		public int getSaleEndTime()
		{
			if (saleCountDownTimer != null)
			{
				return saleCountDownTimer.endTimestamp;
			}

			return -1;
		}

		public int getBuffTimeRemaining()
		{
			int timeRemaining = buffTimerEnd - GameTimer.currentTime;
			if (timeRemaining < 0)
			{
				timeRemaining = -1;
			}
			return timeRemaining;
		}

		private void startCoolDown(int coolDownTime)
		{
			//set timer 
			coolDownTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + coolDownTime);
			coolDownStartTime = GameTimer.currentTime;
			coolDownTimer.registerFunction(onCoolDownExpired);
		}

		private void onCoolDownExpired(Dict args = null, GameTimerRange originalTimer = null)
		{
			coolDownTimer = null;
			timesPurchased = 0;
			CustomPlayerData.setValue(CustomPlayerData.BUNDLE_SALE_PURCHASE_AMOUNT, 0);
			
			//start up the sale again
			setupSaleTimer(saleCountDown);
			
			//refresh top nav if we're in the lobby to show deal icon
			if (Overlay.instance.topV2 != null && Overlay.instance.topV2.buyButtonManager != null)
			{
				Overlay.instance.topV2.buyButtonManager.setButtonType();
			}
		}

		public override bool isEnabled
		{
			get { return base.isEnabled && ExperimentWrapper.BundleSale.isInExperiment; }
		}

		public static void instantiateFeature(JSON data)
		{
			if (instance != null)
			{
				instance.clearEventDelegates();
			}

			instance = new BundleSaleFeature();
			instance.initFeature(data);
		}

		private void onSaleTimerExpired(Dict args = null, GameTimerRange originalTimer = null)
		{
			startCoolDown(coolDown);
		}

		private void setupSaleTimer(int time)
		{
			if (time >= 0)
			{
				int saleEndTime = GameTimer.currentTime + time;
				saleCountDownTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + time);
				saleCountDownTimer.registerFunction(onSaleTimerExpired);
				
				//set the next cooldown time
				coolDownStartTime = saleEndTime;
				CustomPlayerData.setValue(CustomPlayerData.BUNDLE_SALE_COOLDOWN_START_TIME, coolDownStartTime);
			}
		}

		public bool isTimerVisible
		{
			get
			{
				return saleCountDown > 0;
			}
		}

		protected override void registerEventDelegates()
		{
			RewardablesManager.addEventHandler(onRewardSuccess);
		}

		protected override void clearEventDelegates()
		{
			RewardablesManager.removeEventHandler(onRewardSuccess);
		}


		private void onRewardSuccess(Rewardable rewardable)
		{
			if (!showDialogWhenBuffsExpire)
			{
				//in this case we don't need to do anything
				return;
			}
			
			if (rewardable == null || rewardable.feature != LOGIN_DATA_KEY)
			{
				return;
			}

			int endTime = GameTimer.currentTime + longestDurration;
			if (buffTimer == null || buffTimer.endTimestamp < endTime )
			{
				if (buffTimer != null)
				{
					buffTimer.removeFunction(onBuffTimerEnd);	
				}

				buffTimerEnd = endTime;
				buffTimer = new GameTimerRange(GameTimer.currentTime, endTime);
				buffTimer.registerFunction(onBuffTimerEnd);
				CustomPlayerData.setValue(CustomPlayerData.BUNDLE_SALE_BUFF_END, endTime);
				
			}
		}

		private void onBuffTimerEnd(Dict args = null, GameTimerRange originalTimer = null)
		{
			if (isEnabled)
			{
				showDialog();		
			}
		}

		public bool isInCooldown
		{
			get
			{
				return coolDownTimer != null && !coolDownTimer.isExpired;
			}
		}

		protected override void initializeWithData(JSON data)
		{
			coolDown = data.getInt("cooldown_seconds", 0);
			saleCountDown = data.getInt("countdown_seconds", 0);
			purchaseLimit = data.getInt("purchase_limit", 1);
			badgeText = data.getString("badge_text", "");
			buttonText = data.getString("cta_text", "");
			salePreText = data.getString("pre_text","");
			saleTitle = data.getString("title", "");
			coinPackageKey = data.getString("coin_package_key", "");
			showDialogWhenBuffsExpire = data.getBool("show_dialog_when_buffs_expire", false);
			saleBonusPercent = data.getInt("sale_bonus_percent", 0);
			purchaseRemaining = data.getInt("purchase_remaining", 0);
			coolDownStartTime = CustomPlayerData.getInt(CustomPlayerData.BUNDLE_SALE_COOLDOWN_START_TIME, 0);
			timesPurchased = CustomPlayerData.getInt(CustomPlayerData.BUNDLE_SALE_PURCHASE_AMOUNT, 0);
			buffTimerEnd = CustomPlayerData.getInt(CustomPlayerData.BUNDLE_SALE_BUFF_END, 0);
			longestDurration = 0;
			JSON[] items = data.getJsonArray("items");
			itemsInBundle = new List<BundleItem>();
			if (items != null)
			{
				for(int i=0; i< items.Length; i++)
				{
					if (items[i] == null)
					{
						continue;
					}

					int durration = items[i].getInt("duration", 0);
					if (durration > longestDurration)
					{
						longestDurration = durration;
					}
					itemsInBundle.Add(new BundleItem(items[i].getString("buff_type", ""), items[i].getInt("duration", 0),items[i].getString("title", "")));
				}	
			}

			PurchasablePackage package = PurchasablePackage.find(coinPackageKey);
			purchaseItem = new CreditPackage(package,0,false);

			initializeTimers();			
			
		}

		private void initializeTimers()
		{
			//if we're not in cooldown
			if (coolDownStartTime > GameTimer.currentTime)
			{
				//do a check to make sure sale count down data hasn't changed since we saved the cooldown start time
				int newSaleEndTime = coolDownStartTime - GameTimer.currentTime;
				int saleTime = Mathf.Min(newSaleEndTime, saleCountDown);
				
				//set the sale timer with the remaining sale time
				setupSaleTimer(saleTime);
			}
			else if (coolDownStartTime <= GameTimer.currentTime - coolDown)
			{
				//we had a cooldown but it's expired
				coolDownTimer = null;
				timesPurchased = 0;
				CustomPlayerData.setValue(CustomPlayerData.BUNDLE_SALE_PURCHASE_AMOUNT, 0);
				setupSaleTimer(saleCountDown);	
			}
			else
			{
				//we're in active cooldown
				int timeSinceCooldownStarted = GameTimer.currentTime - coolDownStartTime;
				int timeUntilSaleActive = coolDown - timeSinceCooldownStarted;
				startCoolDown(timeUntilSaleActive);
			}
			
			//check if we have an active buff that we bought
			int buffEndTime = CustomPlayerData.getInt(CustomPlayerData.BUNDLE_SALE_BUFF_END, 0);
			if (buffEndTime > GameTimer.currentTime)
			{
				buffTimer = new GameTimerRange(GameTimer.currentTime, buffEndTime);
				buffTimer.registerFunction(onBuffTimerEnd);
			}
		}

		public bool canShow()
		{
			return isEnabled && !isInCooldown && !didPurchaseThisSession && purchaseRemaining > 0;
		}


		public void showDialog()
		{
			BundleSaleDialog.showDialog(Dict.create(D.OPTION, itemsInBundle, D.OPTION1, purchaseItem));
		}

		public static void resetStaticClassData()
		{
			instance = null;
		}
	}
}
