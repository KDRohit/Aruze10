using System.Collections;
using Com.HitItRich.IDFA;
using UnityEngine;

namespace Com.Initialization
{
	public class FeatureDependencyIosAdTracking : FeatureDependency
	{
#if (UNITY_IPHONE || UNITY_IOS)
		/// <inheritdoc/>
		public override void init()
		{
			base.init();
			IDFASoftPromptManager.displayIDFADialog(IDFASoftPromptManager.SurfacePoint.GameEntry, () => { });
		}

		/// <inheritdoc/>
		public override bool isSkipped
		{
			get { return Data.hasPlayerData && !ExperimentWrapper.IDFASoftPrompt.isInExperiment; }
		}

		/// <inheritdoc/>
		public override bool canInitialize
		{
			get { return base.canInitialize && Data.hasPlayerData && ExperimentWrapper.IDFASoftPrompt.isInExperiment; }
		}
	
#endif
	}
}