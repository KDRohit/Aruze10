namespace Com.HitItRich.Feature.BundleSale
{
    public class BundleSaleActions : ServerAction
    {
        //Dev Actions
        private const string DEV_MOCK_PURCHASE = "bundle_sale_mock_purchase_dev";
        private const string DEV_RESET_BUFFS = "bundle_sale_reset_buffs_dev";
        private const string DEV_RESET_PURCHASECOUNT = "bundle_sale_reset_purchase_count_dev";
        /** Constructor */
        private BundleSaleActions(ActionPriority priority, string type) : base(priority, type) { }
        
#if !ZYNGA_PRODUCTION
        public static void devMockPurchase()
        {
            BundleSaleActions action = new BundleSaleActions(ActionPriority.IMMEDIATE, DEV_MOCK_PURCHASE);
            processPendingActions();
        }

        public static void devBuffReset()
        {
            BundleSaleActions action = new BundleSaleActions(ActionPriority.IMMEDIATE, DEV_RESET_BUFFS);
            processPendingActions();
        }

        public static void devResetPurchaseCount()
        {
            BundleSaleActions action = new BundleSaleActions(ActionPriority.IMMEDIATE, DEV_RESET_PURCHASECOUNT);
            processPendingActions();
        }

#endif
        
    }
}