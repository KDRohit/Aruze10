using UnityEngine;
using System.Collections;

namespace Com.Initialization
{
	public class FeatureDependencyElite : FeatureDependency
	{
		/// <inheritdoc/>
		public override void init()
		{
			base.init();
			EliteManager.init(Data.player.getJSON("elite_pass"));
		}

		/// <inheritdoc/>
		public override bool isSkipped
		{
			get { return Data.hasPlayerData && Data.player.getJSON("elite_pass") == null; }
		}

		public override bool canInitialize
		{
			get
			{
				return base.canInitialize &&
				       Data.hasPlayerData &&
				       Data.player.getJSON("elite_pass") != null &&
				       ExperimentWrapper.ElitePass.isInExperiment;
			}
		}
	}
}