using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Initialization
{
    public class FeatureDependencyPurchaseConfirmation : FeatureDependency
    {
        /// <inheritdoc/>
        public override void init()
        {
            base.init();
            BuyCreditsConfirmationDialogNewHIR.initDependency();
        }
    }
}
