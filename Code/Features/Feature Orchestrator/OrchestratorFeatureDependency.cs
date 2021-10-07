using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Initialization;

namespace FeatureOrchestrator
{
    public class OrchestratorFeatureDependency : FeatureDependency
    {

        /// <inheritdoc/>
        public override void init()
        {
            base.init();
            Orchestrator.instance.initialize();
        }

        /// <inheritdoc/>
        public override bool isSkipped
        {
            get { return false; }
        }

        public override bool canInitialize
        {
            get
            {
                return base.canInitialize &&
                       Data.hasPlayerData;
            }
        }
    }
}
