using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
    public class LevelLottoMiniGameDialogComponent : ShowDialogComponent
    {
        private CreditPackage creditPackage;
        
        public LevelLottoMiniGameDialogComponent(string keyName, JSON json) : base(keyName, json)
        {
        }

        public override Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
        {
            Dictionary<string, object> result = base.perform(payload, shouldLog);

            if (result == null)
            {
                result = new Dictionary<string, object>();
            }
            result.Add("view", null);

            long payoutFree = System.Convert.ToInt64(args.getWithDefault(D.PAYOUT_CREDITS, 0));
            if (payoutFree > 0)
            {
                Server.handlePendingCreditsCreated("lottoBlastFree", payoutFree);
            }
            
            return result;
        }

        protected override void setupDialogArgs()
        {
            base.setupDialogArgs();

            if (payload != null)
            {
                object freeBonusGameObject = null;
                if(payload.TryGetValue("grantData", out freeBonusGameObject))
                {
                    Dictionary<string, object> freedata = freeBonusGameObject as Dictionary<string, object>;
                    if (freedata != null)
                    {
                        args.merge( D.PAYOUT_CREDITS, freedata["payout"], D.OPTION1, freedata["multiplier"]);
                    }
                }
                
                object deluxeBonusGameObject = null;
                if(payload.TryGetValue("deluxeBGReward", out deluxeBonusGameObject))
                {
                    Dictionary<string, object> deluxeData = deluxeBonusGameObject as Dictionary<string, object>;
                    if (deluxeData != null)
                    {
                        args.merge( D.AMOUNT, deluxeData["payout"], D.OPTION2, deluxeData["multiplier"]);
                    }
                }
            }
            
            XPProgressCounter progress = jsonData.jsonDict["xpProgressData"] as XPProgressCounter;
            if (progress != null)
            {
                args.merge(D.UNLOCK_LEVEL, progress.completeLevel);
            }
            
            FeatureData featureData = jsonData.jsonDict["featureData"] as FeatureData;
            if (featureData != null)
            {
                args.merge(D.OPTION, featureData.seedValueFree, D.VALUE, featureData.seedValuePremium, D.MODE, featureData.variant);
            }

            string packageName = jsonData.getString("packageName", "");
            if (!string.IsNullOrEmpty(packageName))
            {
                creditPackage = getCreditPackageByName(packageName);
                setCollectiblePackKeyForPackageTier(creditPackage);    
            }

            string paytableDeluxeKey = jsonData.getString("deluxeBGPaytableKey", "");
            string paytableFreeKey = jsonData.getString("freeBGPaytableKey", "");
            args.merge(D.PACKAGE, creditPackage, D.PAYTABLE_KEY1, paytableDeluxeKey, D.PAYTABLE_KEY2, paytableFreeKey);
        }

        private CreditPackage getCreditPackageByName(string creditPackageName)
        {
            PurchaseFeatureData featureData = PurchaseFeatureData.LottoBlast;
            if (featureData != null)
            {
                for (int i = 0; i < featureData.bonusGamePackages.Count; i++)
                {
                    if (featureData.bonusGamePackages[i].purchasePackage != null && featureData.bonusGamePackages[i].purchasePackage.keyName.Equals(creditPackageName))
                    {
                        return featureData.bonusGamePackages[i];
                    }
                }

                return featureData.bonusGamePackages[0];
            }

            Bugsnag.LeaveBreadcrumb("Null lotto blast package for key: " + creditPackageName);
            return null;
        }

        //Checks which buypage package is closest to the lotto blast credit package tier
        //and sets the collectableDropKeyName from that package for the lotto blast package.
        private void setCollectiblePackKeyForPackageTier(CreditPackage package)
        {
            if (package == null || package.purchasePackage == null)
            {
                return;
            }
            
            PurchaseFeatureData purcahseData = PurchaseFeatureData.BuyPage;
            for (int i = 0; i < purcahseData.creditPackages.Count; i++)
            {
                if (purcahseData.creditPackages[i].purchasePackage == null)
                {
                    continue;
                }
                
                if (purcahseData.creditPackages[i].purchasePackage.priceTier >= package.purchasePackage.priceTier)
                {
                    //find the closest buypage package
                    CreditPackage targetPackage = purcahseData.creditPackages[i];
                    package.collectableDropKeyName = targetPackage.collectableDropKeyName;
                    break;
                }
            }
        }
        
        public static ProvidableObject createInstance(string keyname, JSON json)
        {
            return new LevelLottoMiniGameDialogComponent(keyname, json);
        }
    }
}